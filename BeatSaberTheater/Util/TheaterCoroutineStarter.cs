using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Util;

public class TheaterCoroutineStarter : MonoBehaviour, IInitializable
{
    private static TheaterCoroutineStarter? _instance;

    public static TheaterCoroutineStarter Instance
    {
        get
        {
            if (_instance == null)
            {
                Plugin._log.Debug("Creating new CoroutineStarter");
                var gameObject = new GameObject();
                _instance = gameObject.AddComponent<TheaterCoroutineStarter>();
                gameObject.name = typeof(TheaterCoroutineStarter).ToString();
                DontDestroyOnLoad(gameObject);
            }

            var result = _instance;
            return result;
        }
    }

    public void Initialize()
    {
    }
}