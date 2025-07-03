using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BeatSaberTheater.Harmony.Patches;
using BeatSaberTheater.Screen;
using BeatSaberTheater.Util;
using BS_Utils.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;
using Zenject;

namespace BeatSaberTheater.Environment;

public class LightManager : MonoBehaviour
{
    private AsyncGPUReadbackRequest? _readbackRequest;
    private readonly Stopwatch _readbackRequestStopwatch = new();

    private GameObject _lightGameObject = null!;
    private DirectionalLight _light = null!;
    private List<RenderTexture> _downscaleTextures = null!;
    private Color _color;
    private MaterialLightWithId? _menuFloorLight;
    private InstancedMaterialLightWithId? _menuFogRing;
    private bool _menuReferencesSet;
    private CustomVideoPlayer _videoPlayer = null!;

    private const int INITIAL_DOWNSCALING_SIZE = 128;
    private const float DIRECTIONAL_LIGHT_INTENSITY_MENU = 1.1f;

    private const float DIRECTIONAL_LIGHT_INTENSITY_GAMEPLAY = 2.0f;

    //TODO: Radius should ideally depend on screen size and maybe distance
    private const int LIGHT_RADIUS = 250;
    private const int LIGHT_X_ROTATION = 15;
    private const float MENU_FLOOR_INTENSITY = 0.7f;
    private const float MENU_DARKENING_INTENSITY = 0.6f;
    private const float MENU_FOG_INTENSITY = 0.35f;
    private const float MAX_BYTE_AS_FLOAT = byte.MaxValue;

    [Inject] private readonly LoggingService _loggingService = null!;

    internal void Startup(CustomVideoPlayer videoPlayer)
    {
        _videoPlayer = videoPlayer;
        if (_lightGameObject == null) CreateLight();

        _videoPlayer.AddFrameReadyEventHandler(ProcessFrame);
        _videoPlayer.stopped += VideoStopped;
        _videoPlayer.AddEasingUpdateEventHandler(OnFadeUpdate);
        Events.LevelSelected += OnLevelSelected;
        BSEvents.menuSceneLoaded += OnMenuSceneLoaded;
        BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
    }

    #region Unity Event Functions

    private void Awake()
    {
        var textureCount = (int)Math.Ceiling(Math.Log(INITIAL_DOWNSCALING_SIZE, 2)) + 1;
        _downscaleTextures = new List<RenderTexture>(textureCount);
        for (var i = 1; i <= textureCount; i++)
        {
            var size = (int)Math.Pow(2, textureCount - i);
            _downscaleTextures.Add(new RenderTexture(size, size, 0));
        }
    }

    private void OnDisable()
    {
        _videoPlayer.RemoveFrameReadyEventHandler(ProcessFrame);
        _videoPlayer.stopped -= VideoStopped;
        _videoPlayer.RemoveEasingUpdateEventHandler(OnFadeUpdate);
        Events.LevelSelected -= OnLevelSelected;
        BSEvents.menuSceneLoaded -= OnMenuSceneLoaded;
        BSEvents.lateMenuSceneLoadedFresh -= OnMenuSceneLoadedFresh;

        VideoStopped();
    }

    #endregion

    #region Event Handlers

    private void OnLevelSelected(LevelSelectedArgs levelSelectedArgs)
    {
        if (_menuReferencesSet) return;

        try
        {
            GetMenuReferences();
        }
        catch (Exception e)
        {
            _loggingService.Error(e);
        }
    }

    internal void OnGameSceneLoaded()
    {
        var euler = _lightGameObject.transform.eulerAngles;
        euler.x = LIGHT_X_ROTATION;
        _light.intensity = DIRECTIONAL_LIGHT_INTENSITY_GAMEPLAY;

        switch (TheaterFileHelpers.GetEnvironmentName())
        {
            case "BillieEnvironment":
                //Tone down lighting on this env a bit, since clouds get pretty bright
                _light.intensity = 1.2f;
                euler.x = 42;
                break;
            case "BTSEnvironment":
                //Same as with Billie, clouds are too bright
                _light.intensity = 1.6f;
                euler.x = 55;
                break;
            case "LizzoEnvironment":
                //Background objects behind player too bright
                _light.intensity = 1f;
                euler.x = 42;
                break;
        }

        _lightGameObject.transform.eulerAngles = euler;
    }

    private void OnMenuSceneLoaded()
    {
        var euler = _lightGameObject.transform.eulerAngles;
        euler.x = LIGHT_X_ROTATION;
        _lightGameObject.transform.eulerAngles = euler;
        _light.intensity = DIRECTIONAL_LIGHT_INTENSITY_MENU;
    }

    private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO scenesTransitionSetupDataSo)
    {
        try
        {
            if (_lightGameObject == null) CreateLight();
            GetMenuReferences();
        }
        catch (Exception e)
        {
            _loggingService.Error(e);
        }
    }

    #endregion

    private void GetMenuReferences()
    {
        _menuFloorLight = Resources.FindObjectsOfTypeAll<MaterialLightWithId>()
            .FirstOrDefault(x => x.gameObject.name == "BasicMenuGround");
        _menuFogRing = Resources.FindObjectsOfTypeAll<InstancedMaterialLightWithId>()
            .FirstOrDefault(x => x.gameObject.name == "MenuFogRing");
        _menuReferencesSet = true;
    }

    private void OnFadeUpdate(float f)
    {
        UpdateColor(_color);
    }

    private void CreateLight()
    {
        var screen = _videoPlayer.GetFirstScreen();
        if (screen != null)
        {
            _lightGameObject = new GameObject("CinemaDirectionalLight");
            _lightGameObject.transform.parent = screen.transform;
            _lightGameObject.transform.forward = -screen.transform.forward;
            var euler = _lightGameObject.transform.eulerAngles;
            euler.x = LIGHT_X_ROTATION;
            _lightGameObject.transform.eulerAngles = euler;

            _light = _lightGameObject.AddComponent<DirectionalLight>();
            _light.radius = LIGHT_RADIUS;
            _light.intensity = DIRECTIONAL_LIGHT_INTENSITY_MENU;
            _light.color = Color.black;
        }
    }

    private void VideoStopped()
    {
        UpdateColor(Color.black);
    }

    private void ProcessFrame(VideoPlayer source, long frameIdx)
    {
        if (_light == null || !source.isPlaying) return;

        var lowResTex = Downscale(source.texture);

        // Don't start a new readback until the currently running one is finished
        // Not sure if this is necessary, but it does prevent callbacks arriving out-of-order
        if (_readbackRequest is { done: false }) return;

        _readbackRequestStopwatch.Restart();
        _readbackRequest = AsyncGPUReadback.Request(lowResTex, 0, req =>
        {
            var pixelData = req.GetData<uint>();
            var byteArray = BitConverter.GetBytes(pixelData[0]);
            var color = new Color(byteArray[0] / MAX_BYTE_AS_FLOAT, byteArray[1] / MAX_BYTE_AS_FLOAT,
                byteArray[2] / MAX_BYTE_AS_FLOAT);
            UpdateColor(color);
        });
    }

    private void UpdateColor(Color color)
    {
        _color = color;
        _light.color = _color * _videoPlayer.ScreenColor;

        if (TheaterFileHelpers.GetEnvironmentName() != "MainMenu") return;

        //Darken the base menu lighting
        var baseColor = MenuColorPatch.BaseColor *
                        (Color.white - _videoPlayer.ScreenColor * MENU_DARKENING_INTENSITY);
        baseColor.a = 1;

        if (_menuFloorLight != null)
        {
            var colors = new[] { baseColor, _light.color * MENU_FLOOR_INTENSITY };
            var combinedColor = AddColors(colors);
            _menuFloorLight.ColorWasSet(combinedColor);
        }

        if (_menuFogRing != null)
        {
            var colors = new[] { baseColor, _light.color * MENU_FOG_INTENSITY };
            var combinedColor = AddColors(colors);
            _menuFogRing.ColorWasSet(combinedColor);
        }
    }

    private static Color AddColors(params Color[] aColors)
    {
        var result = new Color(0, 0, 0, 0);
        result = aColors.Aggregate(result, (current, c) => current + c);
        result.a = 1;
        return result;
    }

    private RenderTexture Downscale(Texture tex)
    {
        // Blit the video texture into a texture of size INITIAL_DOWNSCALING_SIZE^2
        Graphics.Blit(tex, _downscaleTextures[0]);

        // Blit into textures of decreasing size with the last one being a single pixel to get the average color
        for (var i = 0; i < _downscaleTextures.Count - 1; i++)
            Graphics.Blit(_downscaleTextures[i], _downscaleTextures[i + 1]);

        return _downscaleTextures[_downscaleTextures.Count - 1];
    }
}