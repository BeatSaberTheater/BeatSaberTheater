using System.Linq;
using BeatSaberTheater.Util;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Screen;

public static class SongPreviewPlayerUpdater
{
    private static AudioSource? _activeAudioSource;
    private static int _channelCount;
    private static int _activeChannel;
    private static AudioClip? _currentAudioClip;

    private static readonly LoggingService _loggingService;
    private static readonly PlaybackController _playbackController;
    private static readonly SongPreviewPlayerLoader _playbackLoader;

    static SongPreviewPlayerUpdater(LoggingService loggingService, PlaybackController playbackController,
        SongPreviewPlayerLoader playbackLoader)
    {
        _loggingService = loggingService;
        _playbackController = playbackController;
        _playbackLoader = playbackLoader;
    }

    public static void SetFields(SongPreviewPlayer.AudioSourceVolumeController[] audioSourceControllers,
        int channelCount, int activeChannel,
        AudioClip? audioClip, float startTime, float timeToDefault, bool isDefault)
    {
        _playbackLoader.AudioSourceControllers = audioSourceControllers;
        _channelCount = channelCount;
        _activeChannel = activeChannel;
        _currentAudioClip = audioClip;
        UpdatePlaybackController(startTime, timeToDefault, isDefault);
    }

    private static void UpdatePlaybackController(float startTime, float timeToDefault,
        bool isDefault)
    {
        if (_currentAudioClip == null)
        {
            _loggingService.Warn("SongPreviewPlayer AudioClip was null");
            return;
        }

        if (_playbackLoader.AudioSourceControllers == null)
        {
            _loggingService.Warn("Audiosources null in when updating playback controller");
            return;
        }

        if (_activeChannel < 0 || _activeChannel > _channelCount - 1)
        {
            _loggingService.Warn($"No SongPreviewPlayer audio channel active ({_activeChannel})");
            return;
        }

        if (_currentAudioClip.name == "LevelCleared" || _currentAudioClip.name.EndsWith(".egg"))
            //Prevents preview from playing when new highscore is reached
            isDefault = true;

        _activeAudioSource = _playbackLoader.AudioSourceControllers[_activeChannel].audioSource;
        _loggingService.Debug(
            $"SongPreviewPatch -- channel {_activeChannel} -- startTime {startTime} -- timeRemaining {timeToDefault} -- audioclip {_currentAudioClip.name}");
        _playbackController.UpdateSongPreviewPlayer(_activeAudioSource, startTime, timeToDefault,
            isDefault);
    }
}