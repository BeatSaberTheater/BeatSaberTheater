using UnityEngine;

namespace BeatSaberTheater.Util;

public class CoroutineStarter : MonoBehaviour
{
    private static CoroutineStarter? _instance;

    public static CoroutineStarter Instance
    {
        get
        {
            if (_instance == null)
            {
                Plugin.Log.Debug("Creating new CoroutineStarter");
                var gameObject = new GameObject();
                _instance = gameObject.AddComponent<CoroutineStarter>();
                gameObject.name = typeof(CoroutineStarter).ToString();
                DontDestroyOnLoad(gameObject);
            }

            var result = _instance;
            return result;
        }
    }
}