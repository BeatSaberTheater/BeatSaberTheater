using UnityEngine;

namespace BeatSaberTheater.Harmony.Signals;

public class SongPreviewPlayerSignal
{
    public SongPreviewPlayer.AudioSourceVolumeController[] AudioSourceControllers = [];
    public int ChannelCount;
    public int ActiveChannel;
    public AudioClip? AudioClip;
    public float StartTime;
    public float TimeToDefault;
    public bool IsDefault;
}