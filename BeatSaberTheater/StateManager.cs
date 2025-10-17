using BeatSaberTheater.Playback;
using BeatSaberTheater.State;
using BeatSaberTheater.Util;
using BeatSaberTheater.Video;
using System;
using Zenject;

namespace BeatSaberTheater;

public class StateManager : IInitializable, IDisposable
{
    private readonly PlaybackManager _playbackManager;
    private readonly TheaterState _state;
    private readonly VideoLoader _videoLoader;

    public StateManager(PlaybackManager playbackManager, TheaterState state, VideoLoader videoLoader)
    {
        _playbackManager = playbackManager;
        _state = state;
        _videoLoader = videoLoader;
    }

    public void Initialize()
    {
        Events.LevelSelected += SetSelectedLevel;
    }

    public void Dispose()
    {
        Events.LevelSelected -= SetSelectedLevel;
    }

    public void SetSelectedLevel(LevelSelectedArgs levelSelectedArgs)
    {
        _state.SetCurrentLevel(levelSelectedArgs.BeatmapLevel);

        var currentLevelIsPlaylistSong = InstalledMods.BeatSaberPlaylistsLib && _state.CurrentLevel.IsPlaylistLevel();
        if (InstalledMods.BeatSaberPlaylistsLib && currentLevelIsPlaylistSong)
            _state.SetCurrentLevel(_state.CurrentLevel.GetLevelFromPlaylistIfAvailable());

        _playbackManager.StopPreview(true);

        if (_state.CurrentVideoConfig?.NeedsToSave == true) _videoLoader.SaveVideoConfig(_state.CurrentVideoConfig);
        if (_state.CurrentLevel == null)
        {
            _state.SetCurrentVideoConfig(null);
            _playbackManager.SetSelectedLevel(null, null);
            return;
        }

        _state.SetCurrentVideoConfig(_videoLoader.GetConfigForLevel(_state.CurrentLevel));

        _videoLoader.SetupFileSystemWatcher(_state.CurrentLevel);
        _playbackManager.SetSelectedLevel(_state.CurrentLevel, _state.CurrentVideoConfig);
    }
}
