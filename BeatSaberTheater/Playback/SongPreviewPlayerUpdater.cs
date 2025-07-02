using System;
using BeatSaberTheater.Harmony.Patches;
using BeatSaberTheater.Harmony.Signals;
using BeatSaberTheater.Util;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Playback;

public class SongPreviewPlayerUpdater : IInitializable, IDisposable
{
    private readonly LoggingService _loggingService;
    private readonly PlaybackController _playbackController;
    private readonly SongPreviewPlayerLoader _playbackLoader;

    private AudioSource? _activeAudioSource;
    private int _channelCount;
    private int _activeChannel;
    private AudioClip? _currentAudioClip;

    public SongPreviewPlayerUpdater(LoggingService loggingService, PlaybackController playbackController,
        SongPreviewPlayerLoader playbackLoader)
    {
        _loggingService = loggingService;
        _playbackController = playbackController;
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
        _playbackController.UpdateSongPreviewPlayer(_activeAudioSource, startTime, timeToDefault, isDefault);
    }

    public void Initialize()
    {
        SongPreviewPatch.OnCrossfade = SetFields;
    }

    public void Dispose()
    {
        SongPreviewPatch.OnCrossfade = null;
    }
}