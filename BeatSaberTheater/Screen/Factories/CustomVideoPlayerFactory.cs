using BeatSaberTheater.Screen.Interfaces;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Screen.Factories;

public class CustomVideoPlayerFactory : ICustomVideoPlayerFactory
{
    private DiContainer _container;

    public CustomVideoPlayerFactory(DiContainer container)
    {
        _container = container;
    }

    public CustomVideoPlayer Create(GameObject parent)
    {
        var component = parent.AddComponent<CustomVideoPlayer>();

        // Let Zenject inject dependencies into the new component
        _container.Inject(component);

        return component;
    }
}