using BeatSaberTheater.Video.Config;

namespace BeatSaberTheater.State;

public class TheaterState
{
    public VideoConfig? CurrentVideoConfig { get; private set; }
    public BeatmapLevel? CurrentLevel { get; private set; }

    public void SetCurrentVideoConfig(VideoConfig? videoConfig)
    {
        CurrentVideoConfig = videoConfig;
    }

    public void SetCurrentLevel(BeatmapLevel? level)
    {
        CurrentLevel = level;
    }
}