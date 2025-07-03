using BeatSaberTheater.Environment.Interfaces;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Environment.Factories;

public class LightManagerFactory : ILightManagerFactory
{
    private DiContainer _container;

    public LightManagerFactory(DiContainer container)
    {
        _container = container;
    }

    public LightManager Create(GameObject parent)
    {
        var component = parent.AddComponent<LightManager>();

        // Let Zenject inject dependencies into the new component
        _container.Inject(component);

        return component;
    }
}