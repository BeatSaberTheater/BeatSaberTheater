using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BeatSaberTheater.Download;
using BeatSaberTheater.Screen;
using BeatSaberTheater.Util;
using BeatSaberTheater.Video;
using BS_Utils.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Zenject;
using Scene = BeatSaberTheater.Util.Scene;

namespace BeatSaberTheater.Playback;

public class PlaybackController : MonoBehaviour
{
    public bool IsPreviewPlaying { get; private set; }
    public VideoConfig? VideoConfig;

    private AudioSource? _activeAudioSource;
    private Scene _activeScene = Scene.Other;
    private DateTime _audioSourceStartTime;
    private BeatmapLevel? _currentLevel;
    private float _lastKnownAudioSourceTime;
    private float _offsetAfterPrepare;
    private Stopwatch? _playbackDelayStopwatch;
    private IEnumerator? _prepareVideoCoroutine;
    private float _previewStartTime;
    private DateTime _previewSyncStartTime;
    private float _previewTimeRemaining;
    private bool _previewWaitingForPreviewPlayer;
    private bool _previewWaitingForVideoPlayer = true;
    private SettingsManager? _settingsManager;
    private AudioTimeSyncController? _timeSyncController;

    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly LoggingService _loggingService = null!;

    [Inject] private readonly SongPreviewPlayerLoader _playbackLoader = null!;

    // [Inject] private readonly VideoMenuUI _videoMenu = null!;
    [Inject] [NonSerialized] internal CustomVideoPlayer _videoPlayer = null!;

    #region Monobehaviour Functions

    private void Start()
    {
        _videoPlayer.Player.frameReady += FrameReady;
        _videoPlayer.Player.sendFrameReadyEvents = true;
        _videoPlayer.Player.prepareCompleted += OnPrepareComplete;
        BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        BSEvents.menuSceneLoaded += OnMenuSceneLoaded;
        Events.DifficultySelected += DifficultySelected;
    }

    private void OnDestroy()
    {
        _videoPlayer.Player.frameReady -= FrameReady;
        // BSEvents.gameSceneActive -= GameSceneActive;
        // BSEvents.gameSceneLoaded -= GameSceneLoaded;
        // BSEvents.songPaused -= PauseVideo;
        // BSEvents.songUnpaused -= ResumeVideo;
        BSEvents.lateMenuSceneLoadedFresh -= OnMenuSceneLoadedFresh;
        BSEvents.menuSceneLoaded -= OnMenuSceneLoaded;
        // VideoLoader.ConfigChanged -= OnConfigChanged;
        _videoPlayer.Player.prepareCompleted -= OnPrepareComplete;
        Events.DifficultySelected -= DifficultySelected;
    }

    #endregion

    public void StopPlayback()
    {
        _videoPlayer.Stop();
        StopAllCoroutines();
    }

    #region Event handlers

    private void DifficultySelected(ExtraSongDataArgs extraSongDataArgs)
    {
        if (VideoConfig == null) return;

        var difficultyData = extraSongDataArgs.SelectedDifficultyData;
        var songData = extraSongDataArgs.SongData;

        //If there is any difficulty that has a Theater suggestion but the current one doesn't, disable playback. The current difficulty most likely has the suggestion missing on purpose.
        //If there are no difficulties that have the suggestion set, play the video. It might be a video added by the user.
        //Otherwise, if the map is WIP, disable playback even when no difficulty has the suggestion, to convince the mapper to add it.
        if (difficultyData?.HasTheater() == false && songData?.HasTheaterInAnyDifficulty() == true)
            VideoConfig.PlaybackDisabledByMissingSuggestion = true;
        else if (VideoConfig.IsWIPLevel && difficultyData?.HasTheater() == false)
            VideoConfig.PlaybackDisabledByMissingSuggestion = true;
        else
            VideoConfig.PlaybackDisabledByMissingSuggestion = false;

        if (VideoConfig.PlaybackDisabledByMissingSuggestion)
        {
            _videoPlayer.FadeOut(0.1f);
        }
        else
        {
            if (!_videoPlayer.IsPlaying) StartSongPreview();
        }
    }

    public void FrameReady(VideoPlayer videoPlayer, long frame)
    {
        if (_activeAudioSource == null || VideoConfig == null) return;

        var audioSourceTime = _activeAudioSource.time;

        if (_videoPlayer.IsFading) return;

        var playerTime = _videoPlayer.Player.time;
        var referenceTime = GetReferenceTime();
        if (_videoPlayer.VideoDuration > 0) referenceTime %= _videoPlayer.VideoDuration;
        var error = referenceTime - playerTime;

        if (!_activeAudioSource.isPlaying) return;

        if (frame % 120 == 0)
            _loggingService.Debug("Frame: " + frame + " - Player: " +
                                  TheaterFileHelpers.FormatFloat((float)playerTime) +
                                  " - AudioSource: " +
                                  TheaterFileHelpers.FormatFloat(audioSourceTime) + " - Error (ms): " +
                                  Math.Round(error * 1000));

        if (VideoConfig.endVideoAt.HasValue)
        {
            if (referenceTime >= VideoConfig.endVideoAt - 1f)
            {
                var brightness = Math.Max(0f, VideoConfig.endVideoAt.Value - referenceTime);
                _videoPlayer.SetBrightness(brightness);
            }
        }
        else if (referenceTime >= _videoPlayer.Player.length - 1f && VideoConfig.loop != true)
        {
            var brightness = Math.Max(0f, _videoPlayer.Player.length - referenceTime);
            _videoPlayer.SetBrightness((float)brightness);
        }

        if (Math.Abs(audioSourceTime - _lastKnownAudioSourceTime) > 0.3f && _videoPlayer.IsPlaying)
        {
            _loggingService.Debug("Detected AudioSource seek, resyncing...");
            ResyncVideo();
        }

        //Sync if the error exceeds a threshold, but not if the video is close to the looping point
        if (Math.Abs(error) > 0.3f && Math.Abs(_videoPlayer.VideoDuration - playerTime) > 0.5f &&
            _videoPlayer.IsPlaying)
            //Audio can intentionally go out of sync when the level fails for example. Don't resync the video in that case.
            if (_timeSyncController != null && !_timeSyncController.forcedNoAudioSync)
            {
                _loggingService.Debug(
                    $"Detected desync (reference {referenceTime}, actual {playerTime}), resyncing...");
                ResyncVideo();
            }

        if (audioSourceTime > 0) _lastKnownAudioSourceTime = audioSourceTime;
    }

    private void OnMenuSceneLoaded()
    {
        _loggingService.Info("MenuSceneLoaded");
        _activeScene = Scene.Menu;
        _videoPlayer.Hide();
        StopAllCoroutines();
        _previewWaitingForPreviewPlayer = true;
        gameObject.SetActive(true);
        SceneChanged();
    }

    private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO? scenesTransition)
    {
        OnMenuSceneLoaded();
        if (_settingsManager == null)
        {
            StartCoroutine(OnMenuSceneLoadedFreshCoroutine());
        }
        else
        {
            _videoPlayer.VolumeScale = _settingsManager.settings.audio.volume;
            _videoPlayer.ScreenManager.OnGameSceneLoadedFresh();
        }
    }

    private IEnumerator OnMenuSceneLoadedFreshCoroutine()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        yield return new WaitUntil(() => Plugin._menuContainer != null);
        _settingsManager = Plugin._menuContainer.Resolve<SettingsManager>();
        _videoPlayer.VolumeScale = _settingsManager.settings.audio.volume;
        _videoPlayer.ScreenManager.OnGameSceneLoadedFresh();
    }

    private void OnPrepareComplete(VideoPlayer player)
    {
        if (_offsetAfterPrepare > 0)
        {
            var offset = (DateTime.Now - _audioSourceStartTime).TotalSeconds + _offsetAfterPrepare;
            _loggingService.Info($"Adjusting offset after prepare to {offset}");
            _videoPlayer.Player.time = offset;
        }

        _offsetAfterPrepare = 0;
        _videoPlayer.ClearTexture();

        if (_activeScene != Scene.Menu) return;

        _previewWaitingForVideoPlayer = false;
        StartSongPreview();
    }

    private void SceneChanged()
    {
        _videoPlayer.ScreenManager.SetShaderParameters(VideoConfig);
    }

    #endregion

    #region Harmony Patch Hooks

    public void UpdateSongPreviewPlayer(AudioSource? activeAudioSource, float startTime, float timeRemaining,
        bool isDefault)
    {
        _activeAudioSource = activeAudioSource;
        _lastKnownAudioSourceTime = 0;
        if (_activeAudioSource == null) _loggingService.Debug("Active AudioSource null in SongPreviewPlayer update");

        if (IsPreviewPlaying)
        {
            if (isDefault)
            {
                StopPreview(false);
                return;
            }

            _previewWaitingForPreviewPlayer = true;
            _loggingService.Debug($"Ignoring SongPreviewPlayer update");
            return;
        }

        if (isDefault)
        {
            StopPreview(true);
            _videoPlayer.FadeOut();
            _previewWaitingForPreviewPlayer = true;

            _loggingService.Debug("SongPreviewPlayer reverting to default loop");
            return;
        }

        //This allows the short preview for the practice offset to play
        if (!_previewWaitingForPreviewPlayer && Math.Abs(timeRemaining - 2.5f) > 0.001f)
        {
            StopPreview(true);
            _videoPlayer.FadeOut();

            _loggingService.Debug("Unexpected SongPreviewPlayer update, ignoring.");
            return;
        }

        if (_activeScene != Scene.Menu) return;

        if (_currentLevel != null && _currentLevel.songDuration < startTime)
        {
            _loggingService.Debug("Song preview start time was greater than song duration. Resetting start time to 0");
            startTime = 0;
        }

        _previewStartTime = startTime;
        _previewTimeRemaining = timeRemaining;
        _previewSyncStartTime = DateTime.Now;
        _previewWaitingForPreviewPlayer = false;
        StartSongPreview();
    }

    #endregion

    #region Video Playback

    private void PlayVideo(float startTime)
    {
        if (VideoConfig == null)
        {
            _loggingService.Warn("VideoConfig null in PlayVideo");
            return;
        }

        _videoPlayer.IsSyncing = false;

        // Always hide screen body in the menu, since the drawbacks of the body being visible are large
        if (VideoConfig.TransparencyEnabled && _config.TransparencyEnabled && _activeScene != Scene.Menu)
            _videoPlayer.ShowScreenBody();
        else
            _videoPlayer.HideScreenBody();

        var totalOffset = VideoConfig.GetOffsetInSec();
        var songSpeed = 1f;
        if (BS_Utils.Plugin.LevelData.IsSet)
        {
            songSpeed = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.gameplayModifiers.songSpeedMul;

            if (BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData?.practiceSettings != null)
            {
                songSpeed = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.practiceSettings.songSpeedMul;
                if (totalOffset + startTime < 0) totalOffset /= songSpeed * VideoConfig.PlaybackSpeed;
            }
        }

        _videoPlayer.PlaybackSpeed = songSpeed * VideoConfig.PlaybackSpeed;
        totalOffset += startTime; //This must happen after song speed adjustment

        if (songSpeed * VideoConfig.PlaybackSpeed < 1f && totalOffset > 0f)
        {
            //Unity crashes if the playback speed is less than 1 and the video time at the start of playback is greater than 0
            _loggingService.Warn("Video playback disabled to prevent Unity crash");
            _videoPlayer.Hide();
            StopPlayback();
            VideoConfig = null;
            return;
        }

        //Video seemingly always lags behind. A fixed offset seems to work well enough
        if (!IsPreviewPlaying) totalOffset += 0.0667f;

        if (VideoConfig.endVideoAt != null && totalOffset > VideoConfig.endVideoAt)
            totalOffset = VideoConfig.endVideoAt.Value;

        //This will fail if the video is not prepared yet
        if (_videoPlayer.VideoDuration > 0) totalOffset %= _videoPlayer.VideoDuration;

        //This fixes an issue where the Unity video player sometimes ignores a change in the .time property if the time is very small and the player is currently playing
        if (Math.Abs(totalOffset) < 0.001f)
        {
            totalOffset = 0;
            _loggingService.Debug("Set very small offset to 0");
        }

        _loggingService.Debug(
            $"Total offset: {totalOffset}, startTime: {startTime}, songSpeed: {songSpeed}, player time: {_videoPlayer.Player.time}");

        StopAllCoroutines();

        if (_activeAudioSource != null && _activeAudioSource.time > 0)
            _lastKnownAudioSourceTime = _activeAudioSource.time;

        if (totalOffset < 0)
        {
            if (!IsPreviewPlaying)
                //Negate the offset to turn it into a positive delay
                StartCoroutine(PlayVideoDelayedCoroutine(-totalOffset));
            else
                //In menus we don't need to wait, instead the preview player starts earlier
                _videoPlayer.Play();
        }
        else
        {
            _videoPlayer.Play();
            if (!_videoPlayer.Player.isPrepared)
            {
                _audioSourceStartTime = DateTime.Now;
                _offsetAfterPrepare = totalOffset;
            }
            else
            {
                _videoPlayer.Player.time = totalOffset;
            }
        }
    }

    //TODO Using a stopwatch will not work properly when seeking in the map (e.g. IntroSkip, PracticePlugin)
    private IEnumerator PlayVideoDelayedCoroutine(float delayStartTime)
    {
        _loggingService.Debug("Waiting for " + delayStartTime + " seconds before playing video");
        _playbackDelayStopwatch ??= new Stopwatch();
        _playbackDelayStopwatch.Start();
        _videoPlayer.Pause();
        _videoPlayer.Hide();
        _videoPlayer.Player.time = 0;
        var ticksUntilStart = delayStartTime * TimeSpan.TicksPerSecond;
        yield return new WaitUntil(() => _playbackDelayStopwatch.ElapsedTicks >= ticksUntilStart);
        _loggingService.Debug("Elapsed ms: " + _playbackDelayStopwatch.ElapsedMilliseconds);
        _playbackDelayStopwatch.Stop();
        _playbackDelayStopwatch.Reset();

        if (_activeAudioSource != null && _activeAudioSource.time > 0)
            _lastKnownAudioSourceTime = _activeAudioSource.time;

        _videoPlayer.Play();
    }

    private IEnumerator PlayVideoAfterAudioSourceCoroutine(bool preview)
    {
        float startTime;

        if (!preview)
        {
            _loggingService.Debug("Waiting for ATSC to be ready");

            try
            {
                if (TheaterFileHelpers.IsInEditor() && SceneManager.GetActiveScene().name != "GameCore")
                {
                    var songPreviewPlayer = Plugin.gameCoreContainer.Resolve<SongPreviewPlayer>();
                    if (songPreviewPlayer._audioSourceControllers.Any())
                    {
                        _activeAudioSource = songPreviewPlayer._audioSourceControllers.First().audioSource;
                        _loggingService.Debug("Got ATSC from SongPreviewPlayer");
                    }
                }
                else
                {
                    var atsc = Plugin.gameCoreContainer.Resolve<AudioTimeSyncController>();
                    _activeAudioSource = atsc._audioSource;
                    _loggingService.Debug("Got ATSC from ATSC");
                }
            }
            catch
            {
                _loggingService.Debug("Failed to get AudioSource from DiContainer");
            }

            if (_activeAudioSource == null)
            {
                if (TheaterFileHelpers.IsInEditor() && SceneManager.GetActiveScene().name != "GameCore")
                {
                    // _editorTimeSyncController = Resources.FindObjectsOfTypeAll<BeatmapEditorAudioTimeSyncController>()
                    //     .FirstOrDefault(atsc => atsc.name == "BeatmapEditorAudioTimeSyncController");
                    _activeAudioSource = Resources.FindObjectsOfTypeAll<AudioSource>()
                        .FirstOrDefault(audioSource =>
                            audioSource.name == "SongPreviewAudioSource(Clone)" &&
                            audioSource.transform.parent == null);
                }
                else
                {
                    yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().Any());

                    //Hierarchy: Wrapper/StandardGameplay/GameplayCore/SongController
                    _timeSyncController = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>()
                        .FirstOrDefault(atsc => atsc.transform.parent.parent.name.Contains("StandardGameplay"));

                    if (_timeSyncController == null)
                    {
                        _loggingService.Warn(
                            "Could not find ATSC the usual way. Did the object hierarchy change? Current scene name is " +
                            SceneManager.GetActiveScene().name);

                        //This throws an exception if we still don't find the ATSC
                        _timeSyncController = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().Last();
                        _loggingService.Warn("Selected ATSC: " + _timeSyncController.name);
                    }

                    _activeAudioSource = _timeSyncController._audioSource;
                }
            }
        }

        if (_activeAudioSource != null)
        {
            _lastKnownAudioSourceTime = 0;
            _loggingService.Debug($"Waiting for AudioSource {_activeAudioSource.name} to start playing");
            yield return new WaitUntil(() => _activeAudioSource.isPlaying);
            startTime = _activeAudioSource.time;
        }
        else
        {
            _loggingService.Warn("Active AudioSource was null, cannot wait for it to start");
            StopPreview(true);
            yield break;
        }

        PlayVideo(startTime);
    }

    #endregion

    #region Video Prepare

    public void PrepareVideo(VideoConfig video)
    {
        _previewWaitingForVideoPlayer = true;

        if (_prepareVideoCoroutine != null) StopCoroutine(_prepareVideoCoroutine);

        _videoPlayer.ClearTexture();

        _prepareVideoCoroutine = PrepareVideoCoroutine(video);
        StartCoroutine(_prepareVideoCoroutine);
    }

    private IEnumerator PrepareVideoCoroutine(VideoConfig video)
    {
        VideoConfig = video;

        _videoPlayer.Pause();
        if (VideoConfig.DownloadState != DownloadState.Downloaded)
        {
            _loggingService.Debug("Video is not downloaded, stopping prepare");
            _videoPlayer.FadeOut();
            yield break;
        }

        _videoPlayer.LoopVideo(video.loop == true);
        _videoPlayer.ScreenManager.SetShaderParameters(video);
        _videoPlayer.SetBloomIntensity(video.bloom);

        if (video.VideoPath == null)
        {
            _loggingService.Debug("Video path was null, stopping prepare");
            yield break;
        }

        var videoPath = video.VideoPath;
        _loggingService.Info($"Loading video: {videoPath}");

        if (video.videoFile != null)
        {
            var videoFileInfo = new FileInfo(videoPath);
            var timeout = new DownloadTimeout(0.25f);
            if (_videoPlayer.Url != videoPath)
                yield return new WaitUntil(() =>
                    !TheaterFileHelpers.IsFileLocked(videoFileInfo) || timeout.HasTimedOut);

            timeout.Stop();
            if (timeout.HasTimedOut && TheaterFileHelpers.IsFileLocked(videoFileInfo))
                _loggingService.Warn("Video file locked: " + videoPath);
        }

        _videoPlayer.Url = videoPath;
        _videoPlayer.Prepare();
    }

    #endregion

    #region Video Preview

    public void SetAudioSourcePanning(float pan)
    {
        try
        {
            if (_playbackLoader.AudioSourceControllers == null) return;

            // If resetting the panning back to neutral (0f), set all audio sources.
            // Otherwise only change the active channel.
            if (pan == 0f || _activeAudioSource == null)
                foreach (var sourceVolumeController in _playbackLoader.AudioSourceControllers)
                    sourceVolumeController.audioSource.panStereo = pan;
            else
                _activeAudioSource.panStereo = pan;
        }
        catch (Exception e)
        {
            _loggingService.Warn(e);
        }
    }

    public async void StartPreview()
    {
        if (VideoConfig == null || _currentLevel == null)
        {
            _loggingService.Warn("No video or level selected in OnPreviewAction");
            return;
        }

        if (IsPreviewPlaying)
        {
            _loggingService.Debug("Stopping preview");
            StopPreview(true);
        }
        else
        {
            _loggingService.Debug("Starting preview");
            IsPreviewPlaying = true;

            if (_videoPlayer.IsPlaying) StopPlayback();

            if (!_videoPlayer.IsPrepared) _loggingService.Debug("Video not prepared yet");

            //Start the preview at the point the video kicks in
            var startTime = 0f;
            if (VideoConfig.offset < 0) startTime = -VideoConfig.GetOffsetInSec();

            if (_playbackLoader.SongPreviewPlayer == null)
            {
                _loggingService.Error("Failed to get reference to SongPreviewPlayer during preview");
                return;
            }

            try
            {
                _loggingService.Debug($"Preview start time: {startTime}, offset: {VideoConfig.GetOffsetInSec()}");
                var audioClip = await VideoLoader.GetAudioClipForLevel(_currentLevel);
                if (audioClip != null)
                    _playbackLoader.SongPreviewPlayer.CrossfadeTo(audioClip, -5f, startTime,
                        _currentLevel.songDuration, null);
                else
                    _loggingService.Error("AudioClip for level failed to load");
            }
            catch (Exception e)
            {
                _loggingService.Error(e);
                IsPreviewPlaying = false;
                return;
            }

            //+1.0 is hard right. only pan "mostly" right, because for some reason the video player audio doesn't
            //pan hard left either. Also, it sounds a bit more comfortable.
            SetAudioSourcePanning(0.9f);
            StartCoroutine(PlayVideoAfterAudioSourceCoroutine(true));
            _videoPlayer.PanStereo = -1f; // -1 is hard left
            _videoPlayer.Unmute();
        }
    }

    private void StartSongPreview()
    {
        if (!_config.PluginEnabled || VideoConfig is not { IsPlayable: true }) return;

        if (_previewWaitingForPreviewPlayer || _previewWaitingForVideoPlayer || IsPreviewPlaying) return;

        if (_currentLevel != null && VideoLoader.IsDlcSong(_currentLevel)) return;

        var delay = DateTime.Now.Subtract(_previewSyncStartTime);
        var delaySeconds = (float)delay.TotalSeconds;

        _loggingService.Debug($"Starting song preview playback with a delay of {delaySeconds}");

        var timeRemaining = _previewTimeRemaining - delaySeconds;
        if (timeRemaining > 1 || _previewTimeRemaining == 0)
            PlayVideo(_previewStartTime + delaySeconds);
        else
            _loggingService.Debug(
                $"Not playing song preview, because delay was too long. Remaining preview time: {_previewTimeRemaining}");
    }

    public void StopPreview(bool stopPreviewMusic)
    {
        if (!IsPreviewPlaying) return;
        _loggingService.Debug($"Stopping preview (stop audio source: {stopPreviewMusic}");

        _videoPlayer.FadeOut();
        StopAllCoroutines();

        if (stopPreviewMusic && _playbackLoader.SongPreviewPlayer != null)
        {
            _playbackLoader.SongPreviewPlayer.CrossfadeToDefault();
            _videoPlayer.Mute();
        }

        IsPreviewPlaying = false;

        SetAudioSourcePanning(0f); //0f is neutral
        _videoPlayer.Mute();

        // _videoMenu.SetButtonState(true);
    }

    #endregion

    private float GetReferenceTime(float? referenceTime = null, float? playbackSpeed = null)
    {
        if (_activeAudioSource == null || VideoConfig == null) return 0;

        float time;
        if (referenceTime == null && _activeAudioSource.time == 0)
            time = _lastKnownAudioSourceTime;
        else
            time = referenceTime ?? _activeAudioSource.time;
        var speed = playbackSpeed ?? VideoConfig.PlaybackSpeed;
        return time * speed + VideoConfig.offset / 1000f;
    }

    public void ResyncVideo(float? referenceTime = null, float? playbackSpeed = null)
    {
        if (_activeAudioSource == null || VideoConfig == null || !VideoConfig.IsPlayable) return;

        var newTime = GetReferenceTime(referenceTime, playbackSpeed);

        if (newTime < 0)
        {
            _videoPlayer.Hide();
            StopAllCoroutines();
            StartCoroutine(PlayVideoDelayedCoroutine(-newTime));
        }
        else if (newTime > _videoPlayer.VideoDuration && _videoPlayer.VideoDuration > 0)
        {
            newTime %= _videoPlayer.VideoDuration;
        }

        if (Math.Abs(_videoPlayer.Player.time - newTime) < 0.2f) return;

        if (playbackSpeed.HasValue) _videoPlayer.PlaybackSpeed = playbackSpeed.Value;
        _videoPlayer.Player.time = newTime;
    }

    public void SetSelectedLevel(BeatmapLevel? level, VideoConfig? config)
    {
        _previewWaitingForPreviewPlayer = true;
        _previewWaitingForVideoPlayer = true;

        _currentLevel = level;
        VideoConfig = config;
        _loggingService.Debug($"Selected Level: {level?.levelID ?? "null"}");

        if (VideoConfig == null)
        {
            _videoPlayer.FadeOut();
            StopAllCoroutines();
            return;
        }

        PrepareVideo(VideoConfig);
        if (level != null && VideoLoader.IsDlcSong(level)) _videoPlayer.FadeOut();
    }
}