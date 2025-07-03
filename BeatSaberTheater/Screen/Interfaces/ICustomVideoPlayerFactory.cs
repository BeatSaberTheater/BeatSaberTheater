using UnityEngine;

namespace BeatSaberTheater.Screen.Interfaces;

internal interface ICustomVideoPlayerFactory
{
    CustomVideoPlayer Create(GameObject parent);
}