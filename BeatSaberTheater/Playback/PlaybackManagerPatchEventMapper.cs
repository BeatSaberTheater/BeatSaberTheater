﻿using System;
using System.Collections;
using BeatSaberTheater.Harmony.Patches;
using BeatSaberTheater.Harmony.Signals;
using BeatSaberTheater.Util;
using SongCore;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Playback;

public class PlaybackManagerPatchEventMapper : IInitializable, IDisposable
{
    private readonly TheaterCoroutineStarter _coroutineStarter;
    private readonly LoggingService _loggingService;
    private readonly PlaybackManager _playbackManager;

    private AudioSource? _activeAudioSource;
    private int _channelCount;
    private int _activeChannel;
    private AudioClip? _currentAudioClip;

    public PlaybackManagerPatchEventMapper(TheaterCoroutineStarter coroutineStarter, LoggingService loggingService,
        PlaybackManager playbackManager)
    {
        _coroutineStarter = coroutineStarter;
        _loggingService = loggingService;
        _playbackManager = playbackManager;
    }

    private void SetFields(SongPreviewPlayerSignal signal)
    {
        _channelCount = signal.ChannelCount;
        _activeChannel = signal.ActiveChannel;
        _currentAudioClip = signal.AudioClip;
        UpdatePlaybackManager(signal.AudioSourceControllers, signal.StartTime, signal.TimeToDefault, signal.IsDefault);
    }

    private void UpdateMapRequirements(MapRequirementsUpdateSignal signal)
    {
        try
        {
            var videoConfig = _playbackManager.GetVideoConfig();
            if (videoConfig == null) return;

            if (signal.StandardLevelDetailView._beatmapLevel.hasPrecalculatedData) return;

            var songData =
                Collections.GetCustomLevelSongData(
                    Collections.GetCustomLevelHash(signal.StandardLevelDetailView._beatmapLevel.levelID));
            if (songData == null) return;

            var diffData = Collections.GetCustomLevelSongDifficultyData(signal.StandardLevelDetailView.beatmapKey);
            Events.SetExtraSongData(songData, diffData);

            if (diffData?.HasTheaterRequirement() != true) return;

            if (videoConfig?.IsPlayable == true ||
                videoConfig?.forceEnvironmentModifications == true)
            {
                _loggingService.Debug("Requirement fulfilled");
                return;
            }

            _loggingService.Info("Theater requirement not met for " +
                                 signal.StandardLevelDetailView._beatmapLevel.songName);
            signal.StandardLevelDetailView._actionButton.interactable = false;
            signal.StandardLevelDetailView._practiceButton.interactable = false;
        }
        catch (Exception e)
        {
            _loggingService.Error(e);
        }
    }

    private void UpdatePlaybackManager(SongPreviewPlayer.AudioSourceVolumeController[] audioSourceControllers,
        float startTime, float timeToDefault, bool isDefault)
    {
        if (_currentAudioClip == null)
        {
            _loggingService.Warn("SongPreviewPlayer AudioClip was null");
            return;
        }

        if (_activeChannel < 0 || _activeChannel > _channelCount - 1)
        {
            _loggingService.Warn($"No SongPreviewPlayer audio channel active ({_activeChannel})");
            return;
        }

        if (_currentAudioClip.name == "LevelCleared" || _currentAudioClip.name.EndsWith(".egg"))
            isDefault = true;

        _activeAudioSource = audioSourceControllers[_activeChannel].audioSource;
        _loggingService.Debug(
            $"SongPreviewPatch -- channel {_activeChannel} -- startTime {startTime} -- timeRemaining {timeToDefault} -- audioclip {_currentAudioClip.name}");
        _playbackManager.UpdateSongPreviewPlayer(audioSourceControllers, _activeAudioSource, startTime, timeToDefault,
            isDefault);
    }

    private IEnumerator WaitThenStartVideoPlaybackCoroutine()
    {
        //Have to wait two frames, since Chroma waits for one and we have to make sure we run after Chroma without directly interacting with it.
        //Chroma probably waits a frame to make sure the lights are all registered before accessing the LightManager.
        //If we run before Chroma, the prop groups will get different IDs than usual due to the changed z-positions.
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        //Turns out CustomPlatforms runs even later and undoes some of the scene modifications Cinema does. Waiting for a specific duration is more of a temporary fix.
        //TODO Find a better way to implement this. The problematic coroutine in CustomPlatforms is CustomFloorPlugin.EnvironmentHider+<InternalHideObjectsForPlatform>
        yield return new WaitForSeconds(InstalledMods.CustomPlatforms ? 0.75f : 0.05f);

        // EnvironmentController.ModifyGameScene(PlaybackController.Instance.VideoConfig);
    }

    private void WaitThenStartVideoPlayback()
    {
        _loggingService.Debug("Starting video playback delay");
        _coroutineStarter.StartCoroutine(WaitThenStartVideoPlaybackCoroutine());
    }

    public void Initialize()
    {
        LightSwitchEventEffectStart.DelayPlaybackStart = WaitThenStartVideoPlayback;
        SongPreviewPatch.OnCrossfade = SetFields;
        StandardLevelDetailViewRefreshContent.OnMapRequirementsUpdate = UpdateMapRequirements;
    }

    public void Dispose()
    {
        LightSwitchEventEffectStart.DelayPlaybackStart = null;
        SongPreviewPatch.OnCrossfade = null;
        StandardLevelDetailViewRefreshContent.OnMapRequirementsUpdate = null;
    }
}