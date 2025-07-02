using UnityEngine;

namespace BeatSaberTheater.Screen.Interfaces;

internal interface ICustomBloomPrePassFactory
{
    CustomBloomPrePass Create(GameObject parent);
}