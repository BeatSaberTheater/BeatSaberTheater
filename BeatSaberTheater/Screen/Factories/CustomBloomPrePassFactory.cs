using BeatSaberTheater.Screen.Interfaces;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Screen.Factories;

internal class CustomBloomPrePassFactory : ICustomBloomPrePassFactory
{
    private DiContainer _container;

    public CustomBloomPrePassFactory(DiContainer container)
    {
        _container = container;
    }

    public CustomBloomPrePass Create(GameObject parent)
    {
        var curvedSurface = parent.AddComponent<CustomBloomPrePass>();

        // Let Zenject inject dependencies into the new component
        _container.Inject(curvedSurface);

        return curvedSurface;
    }
}