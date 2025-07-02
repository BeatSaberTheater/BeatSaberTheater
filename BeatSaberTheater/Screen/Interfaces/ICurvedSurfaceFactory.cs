using UnityEngine;

namespace BeatSaberTheater.Screen.Interfaces;

public interface ICurvedSurfaceFactory
{
    CurvedSurface Create(GameObject parent);
}