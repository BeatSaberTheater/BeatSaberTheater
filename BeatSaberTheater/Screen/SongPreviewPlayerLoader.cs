using System.Linq;
using BeatSaberTheater.Util;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Screen;

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