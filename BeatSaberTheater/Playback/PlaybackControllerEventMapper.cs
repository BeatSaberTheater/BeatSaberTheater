using System;
using BeatSaberTheater.Harmony.Patches;
using BeatSaberTheater.Harmony.Signals;
using BeatSaberTheater.Util;
using SongCore;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Playback;

public class PlaybackControllerEventMapper : IInitializable, IDisposable
{
    private readonly LoggingService _loggingService;
    private readonly PlaybackManager _playbackManager;
    private readonly SongPreviewPlayerLoader _playbackLoader;

    private AudioSource? _activeAudioSource;
    private int _channelCount;
    private int _activeChannel;
    private AudioClip? _currentAudioClip;

    public PlaybackControllerEventMapper(LoggingService loggingService, PlaybackManager playbackManager,
        SongPreviewPlayerLoader playbackLoader)
    {
        _loggingService = loggingService;
        _playbackManager = playbackManager;
        _playbackLoader = playbackLoader;
    }

    public void SetFields(SongPreviewPlayerSignal signal)
    {
        _playbackLoader.AudioSourceControllers = signal.AudioSourceControllers;
        _channelCount = signal.ChannelCount;
        _activeChannel = signal.ActiveChannel;
        _currentAudioClip = signal.AudioClip;
        UpdatePlaybackController(signal.StartTime, signal.TimeToDefault, signal.IsDefault);
    }

    public void UpdateMapRequirements(MapRequirementsUpdateSignal signal)
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

    private void UpdatePlaybackController(float startTime, float timeToDefault, bool isDefault)
    {
        if (_currentAudioClip == null)
        {
            _loggingService.Warn("SongPreviewPlayer AudioClip was null");
            return;
        }

        if (_playbackLoader.AudioSourceControllers == null)
        {
            _loggingService.Warn("Audiosources null when updating playback controller");
            return;
        }

        if (_activeChannel < 0 || _activeChannel > _channelCount - 1)
        {
            _loggingService.Warn($"No SongPreviewPlayer audio channel active ({_activeChannel})");
            return;
        }

        if (_currentAudioClip.name == "LevelCleared" || _currentAudioClip.name.EndsWith(".egg"))
            isDefault = true;

        _activeAudioSource = _playbackLoader.AudioSourceControllers[_activeChannel].audioSource;
        _loggingService.Debug(
            $"SongPreviewPatch -- channel {_activeChannel} -- startTime {startTime} -- timeRemaining {timeToDefault} -- audioclip {_currentAudioClip.name}");
        _playbackManager.UpdateSongPreviewPlayer(_activeAudioSource, startTime, timeToDefault, isDefault);
    }

    public void Initialize()
    {
        SongPreviewPatch.OnCrossfade = SetFields;
        StandardLevelDetailViewRefreshContent.OnMapRequirementsUpdate = UpdateMapRequirements;
    }

    public void Dispose()
    {
        SongPreviewPatch.OnCrossfade = null;
        StandardLevelDetailViewRefreshContent.OnMapRequirementsUpdate = null;
    }
}