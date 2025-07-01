using UnityEngine;

namespace BeatSaberTheater.Models;

public interface ICurvedSurfaceFactory
{
    CurvedSurface Create(GameObject parent);
}