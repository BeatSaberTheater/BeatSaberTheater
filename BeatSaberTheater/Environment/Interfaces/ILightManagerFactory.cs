using UnityEngine;

namespace BeatSaberTheater.Environment.Interfaces;

internal interface ILightManagerFactory
{
    LightManager Create(GameObject parent);
}