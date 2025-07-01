using Zenject;
using UnityEngine;

namespace BeatSaberTheater.Models;

public class CurvedSurfaceFactory : ICurvedSurfaceFactory
{
    private DiContainer _container;

    public CurvedSurfaceFactory(DiContainer container)
    {
        _container = container;
    }

    public CurvedSurface Create(GameObject parent)
    {
        var curvedSurface = parent.AddComponent<CurvedSurface>();

        // Let Zenject inject dependencies into the new component
        _container.Inject(curvedSurface);

        return curvedSurface;
    }
}