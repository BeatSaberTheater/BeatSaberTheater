using System.Linq;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Playback;

public class SongPreviewPlayerLoader : IInitializable
{
    public SongPreviewPlayer? SongPreviewPlayer;
    public SongPreviewPlayer.AudioSourceVolumeController[]? AudioSourceControllers;

    public void Initialize()
    {
        AudioSourceControllers = null;
        SongPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().LastOrDefault();
    }
}