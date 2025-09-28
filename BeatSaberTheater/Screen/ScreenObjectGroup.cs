using UnityEngine;

namespace BeatSaberTheater.Screen;

public class ScreenObjectGroup(GameObject screen, CurvedSurface curvedSurface, CustomBloomPrePass customBloomPrePass)
{
    public GameObject Screen { get; set; } = screen;
    public CurvedSurface CurvedSurface { get; set; } = curvedSurface;
    public CustomBloomPrePass CustomBloomPrePass { get; set; } = customBloomPrePass;
}