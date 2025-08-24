using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BeatSaberTheater.Playback;
using BeatSaberTheater.Screen;
using BeatSaberTheater.Util;
using BeatSaberTheater.Video.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;
using Scene = BeatSaberTheater.Util.Scene;

namespace BeatSaberTheater.Environment;

public class EnvironmentManipulator
{
    private const float CLONED_OBJECT_Z_OFFSET = 200f;
    private const string CLONED_OBJECT_NAME_SUFFIX = " (TheaterClone)";
    private const string HIDE_CINEMA_SCREEN_OBJECT_NAME = "HideCinemaScreen";
    private const string HIDE_THEATER_SCREEN_OBJECT_NAME = "HideTheaterScreen";

    private static bool _environmentModified;
    private static string _currentEnvironmentName = "MainMenu";
    internal static bool IsScreenHidden { get; private set; }

    private readonly Dictionary<string, Action<VideoConfig?>> _environmentHandlers;
    private List<EnvironmentObject>? _environmentObjectList;

    private IEnumerable<EnvironmentObject> EnvironmentObjects
    {
        get
        {
            if (_environmentObjectList != null && _environmentObjectList.Any()) return _environmentObjectList;

            //Cache the state of all GameObjects
            _environmentObjectList = new List<EnvironmentObject>(10000);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            _loggingService.Debug($"Resource call finished after {stopwatch.ElapsedMilliseconds} ms");
            var activeScene = SceneManager.GetActiveScene();
            var currentEnvironmentScene = SceneManager.GetSceneByName(_currentEnvironmentName);
            var pcInitScene = SceneManager.GetSceneByName("PCInit"); //This scene is used by CustomPlatforms
            foreach (var gameObject in gameObjects)
            {
                //Relevant GameObjects are mostly in "GameCore" or the scene of the current environment, so filter out everything else
                if (gameObject.scene != activeScene && gameObject.scene != currentEnvironmentScene &&
                    gameObject.scene != pcInitScene) continue;

                _environmentObjectList.Add(new EnvironmentObject(gameObject, false));
            }

            stopwatch.Stop();
            _loggingService.Debug(
                $"Created environment object list in {stopwatch.ElapsedMilliseconds} ms, items: {_environmentObjectList.Count}");

            return _environmentObjectList;
        }
    }

    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly LoggingService _loggingService = null!;
    [Inject] private readonly PlaybackManager _playbackManager = null!;

    public EnvironmentManipulator()
    {
        _environmentHandlers = new Dictionary<string, Action<VideoConfig?>>
        {
            ["NiceEnvironment"] = HandleNiceEnvironment,
            ["BigMirrorEnvironment"] = HandleNiceEnvironment,
            ["BTSEnvironment"] = HandleBtsEnvironment,
            ["OriginsEnvironment"] = HandleOriginsEnvironment,
            ["KDAEnvironment"] = HandleKdaEnvironment,
            ["RocketEnvironment"] = HandleRocketEnvironment,
            ["DragonsEnvironment"] = HandleDragonsEnvironment,
            ["Dragons2Environment"] = HandleDragons2Environment,
            ["LinkinParkEnvironment"] = HandleLinkinParkEnvironment,
            ["KaleidoscopeEnvironment"] = HandleKaleidoscopeEnvironment,
            ["GlassDesertEnvironment"] = HandleGlassDesertEnvironment,
            ["InterscopeEnvironment"] = HandleInterscopeEnvironment,
            ["CrabRaveEnvironment"] = HandleCrabRaveEnvironment,
            ["MonstercatEnvironment"] = HandleCrabRaveEnvironment,
            ["SkrillexEnvironment"] = HandleSkrillexEnvironment,
            ["PyroEnvironment"] = HandlePyroEnvironment,
            ["LizzoEnvironment"] = HandleLizzoEnvironment,
            ["WeaveEnvironment"] = HandleWeaveEnvironment,
            ["EDMEnvironment"] = HandleEdmEnvironment
        };
    }

    public void Init()
    {
        SceneManager.activeSceneChanged += SceneChanged;
    }

    public void Disable()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
        _environmentObjectList?.Clear();
    }

    private void SceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
    {
        _loggingService.Debug($"Scene changed from {arg0.name} to {arg1.name}");
        var sceneName = arg1.name;
        switch (sceneName)
        {
            case "BeatmapLevelEditorWorldUi":
                Reset();
                _playbackManager.GameSceneLoaded();
                break;
            case "MainMenu" or "PCInit" or "EmptyTransition":
                Reset();
                break;
        }
    }

    private void Reset()
    {
        IsScreenHidden = false;

        _environmentModified = false;
        _environmentObjectList?.Clear();
    }

    public void ModifyGameScene(VideoConfig? videoConfig)
    {
        // Move back to the DontDestroyOnLoad scene
        Object.DontDestroyOnLoad(_playbackManager);

        if (!_config.PluginEnabled || videoConfig == null ||
            (!videoConfig.IsPlayable && (videoConfig.forceEnvironmentModifications == null ||
                                         videoConfig.forceEnvironmentModifications == false)))
            return;

        // Make sure the environment is only modified once, since the trigger for this functions runs multiple times
        if (_environmentModified) return;

        _environmentModified = true;
        _currentEnvironmentName = TheaterFileHelpers.GetEnvironmentName();
        _loggingService.Debug("Loaded environment: " + _currentEnvironmentName);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        CreateAdditionalScreens(videoConfig);
        PrepareClonedScreens(videoConfig);
        CloneObjects(videoConfig);

        try
        {
            if (videoConfig.disableDefaultModifications == null ||
                videoConfig.disableDefaultModifications.Value == false) DefaultSceneModifications(videoConfig);
        }
        catch (Exception e)
        {
            _loggingService.Error(e);
        }

        try
        {
            VideoConfigSceneModifications(videoConfig);
        }
        catch (Exception e)
        {
            _loggingService.Error(e);
        }

        stopwatch.Stop();
        _loggingService.Debug($"Modified environment in {stopwatch.ElapsedMilliseconds} ms");
    }

    private void CreateAdditionalScreens(VideoConfig videoConfig)
    {
        if (videoConfig.additionalScreens == null) return;

        var videoPlayerScreen = _playbackManager.GetVideoPlayerFirstScreen();
        if (videoPlayerScreen != null)
        {
            var i = 0;
            foreach (var _ in videoConfig.additionalScreens)
            {
                var clone = Object.Instantiate(videoPlayerScreen, videoPlayerScreen.transform.parent);
                clone.name += $" ({i++.ToString()})";
            }
        }
    }

    private List<EnvironmentObject> SelectObjectsFromScene(EnvironmentModification modification,
        bool selectByCloneFrom)
    {
        modification = TranslateNameForBackwardsCompatibility(modification);
        var name = selectByCloneFrom ? modification.cloneFrom! : modification.name;
        var parentName = modification.parentName;
        if (!selectByCloneFrom && modification.cloneFrom != null) name += CLONED_OBJECT_NAME_SUFFIX;

        IEnumerable<EnvironmentObject>? environmentObjects = null;
        try
        {
            environmentObjects = EnvironmentObjects.Where(x =>
                x.name == name &&
                (parentName == null || x.transform.parent.name == parentName));
        }
        catch (Exception e)
        {
            _loggingService.Warn(e);
        }

        var environmentObjectList = (environmentObjects ?? Array.Empty<EnvironmentObject>()).ToList();
        return environmentObjectList;
    }

    private EnvironmentModification TranslateNameForBackwardsCompatibility(EnvironmentModification modification)
    {
        var selectByCloneFrom = modification.cloneFrom != null;
        var name = selectByCloneFrom ? modification.cloneFrom! : modification.name;
        var newName = name;

        switch (_currentEnvironmentName)
        {
            case "BigMirrorEnvironment":
            {
                newName = name switch
                {
                    "GlowLineL" => "NeonTubeDirectionalL",
                    "GlowLineL2" => "NeonTubeDirectionalFL",
                    "GlowLineR" => "NeonTubeDirectionalR",
                    "GlowLineR2" => "NeonTubeDirectionalFR",
                    _ => name
                };

                if (modification.parentName == "Buildings") modification.parentName = "Environment";

                break;
            }
            case "NiceEnvironment":
            {
                newName = name switch
                {
                    "TrackLaneRing(Clone)" => "SmallTrackLaneRing(Clone)",
                    _ => name
                };
                break;
            }
        }

        if (selectByCloneFrom)
            modification.cloneFrom = newName;
        else
            modification.name = newName;

        return modification;
    }

    private void VideoConfigSceneModifications(VideoConfig? config)
    {
        if (config == null) return;

        if (!config.IsPlayable &&
            (config.forceEnvironmentModifications == null || config.forceEnvironmentModifications == false)) return;

        if (config.additionalScreens != null)
        {
            var i = 0;
            foreach (var screenConfig in config.additionalScreens)
            {
                var clone = _playbackManager.FindVideoPlayerScreen(screen => screen.name.EndsWith("(" + i + ")"));
                if (clone is null)
                {
                    _loggingService.Error($"Couldn't find a screen ending with {"(" + i + ")"}");
                    continue;
                }

                if (screenConfig.position.HasValue) clone.transform.position = screenConfig.position.Value;

                if (screenConfig.rotation.HasValue) clone.transform.eulerAngles = screenConfig.rotation.Value;

                if (screenConfig.scale.HasValue) clone.transform.localScale = screenConfig.scale.Value;

                i++;
            }
        }

        if (config.environment == null || config.environment.Length == 0) return;

        foreach (var environmentModification in config.environment)
        {
            var selectedObjectsList = SelectObjectsFromScene(environmentModification, false);
            if (!selectedObjectsList.Any())
            {
                _loggingService.Error(
                    $"Failed to find object: name={environmentModification.name}, parentName={environmentModification.parentName ?? "null"}, cloneFrom={environmentModification.cloneFrom ?? "null"}");
                continue;
            }

            foreach (var environmentObject in selectedObjectsList)
            {
                if (_currentEnvironmentName == "PyroEnvironment" &&
                    environmentObject.name.StartsWith("LightGroup") &&
                    environmentModification.position.HasValue &&
                    Math.Abs(environmentModification.position.Value.y - 1.99f) < 0.1f)
                    //Fixes configs that were made before Pyro changes
                    continue;

                if (environmentModification.active.HasValue)
                    environmentObject.SetActive(environmentModification.active.Value);

                if (environmentModification.position.HasValue)
                    environmentObject.transform.position = environmentModification.position.Value;

                if (environmentModification.rotation.HasValue)
                    environmentObject.transform.eulerAngles = environmentModification.rotation.Value;

                if (environmentModification.scale.HasValue)
                    environmentObject.transform.localScale = environmentModification.scale.Value;
            }
        }
    }

    #region Object Cloning

    private void PrepareClonedScreens(VideoConfig videoConfig)
    {
        var screenCount = _playbackManager.gameObject.transform.childCount;

        if (screenCount <= 1) return;

        _loggingService.Debug($"Screens found: {screenCount}");
        foreach (Transform screen in _playbackManager.gameObject.transform)
        {
            if (!screen.name.StartsWith("TheaterScreen")) return;

            if (screen.name.Contains("Clone"))
            {
                _playbackManager.AddScreenToVideoPlayer(screen.gameObject);
                screen.GetComponent<Renderer>().material =
                    _playbackManager.GetVideoPlayerFirstScreen()?.GetComponent<Renderer>().material;
                Object.Destroy(screen.Find("TheaterDirectionalLight").gameObject);
            }

            screen.gameObject.GetComponent<CustomBloomPrePass>().enabled = false;
            _loggingService.Debug("Disabled bloom prepass");
        }

        _playbackManager.UpdateVideoPlayerPlacement(videoConfig, Scene.SoloGameplay);
        _playbackManager.SetVideoPlayerScreenShaderParameters(videoConfig);
    }

    private void CloneObjects(VideoConfig? config)
    {
        if (config?.environment == null || config.environment.Length == 0) return;

        _loggingService.Debug("Cloning objects");
        var cloneCounter = 0;
        foreach (var objectToBeCloned in config.environment)
        {
            if (objectToBeCloned.cloneFrom == null) continue;

            var environmentObjectList = SelectObjectsFromScene(objectToBeCloned, true);
            if (!environmentObjectList.Any())
            {
                _loggingService.Error(
                    $"Failed to find object while cloning: name={objectToBeCloned.cloneFrom}, parentName={objectToBeCloned.parentName ?? "null"}");
                continue;
            }

            var originalObject = environmentObjectList.Last().gameObject;
            CloneObject(originalObject, objectToBeCloned, config);
            cloneCounter++;
        }

        _loggingService.Debug("Cloned " + cloneCounter + " objects");
    }

    private EnvironmentObject CloneObject(GameObject originalObject, EnvironmentModification objectToBeCloned,
        VideoConfig? config, bool disableZOffset = false)
    {
        var lightManager = EnvironmentObjects.LastOrDefault(x => x.name == "LightWithIdManager");
        if (lightManager == null) _loggingService.Error("Failed to find LightWithIdManager. Cannot clone lights.");

        var clone = Object.Instantiate(originalObject, originalObject.transform.parent);

        //Move the new object far away to prevent changing the prop IDs that chroma assigns, but only if "mergePropGroups" is not set
        var position = clone.transform.position;
        var zOffset = disableZOffset ? 0 :
            config?.mergePropGroups == null || config.mergePropGroups == false ? CLONED_OBJECT_Z_OFFSET : 0;
        var newPosition = new Vector3(position.x, position.y, position.z + zOffset);
        clone.transform.position = newPosition;

        //If the object has no position specified, add a position that reverts the z-offset
        objectToBeCloned.position ??= position;

        if (!clone.name.EndsWith(CLONED_OBJECT_NAME_SUFFIX))
            clone.name = (objectToBeCloned.name ?? originalObject.transform.name) + CLONED_OBJECT_NAME_SUFFIX;

        try
        {
            RegisterLights(clone, lightManager?.transform.GetComponent<LightWithIdManager>());
            RegisterMirror(clone);
            RegisterSpectrograms(clone);
        }
        catch (Exception e)
        {
            _loggingService.Error(e);
        }

        var cloneEnvironmentObject = new EnvironmentObject(clone, true);
        objectToBeCloned.gameObjectClone = cloneEnvironmentObject;
        _environmentObjectList?.Add(cloneEnvironmentObject);
        return cloneEnvironmentObject;
    }

    private void RegisterLights(GameObject clone, LightWithIdManager? lightWithIdManager)
    {
        if (lightWithIdManager == null) return;

        RegisterLight(clone.GetComponent<LightWithIdMonoBehaviour>(), lightWithIdManager);
        foreach (Transform child in clone.transform)
            RegisterLight(child.GetComponent<LightWithIdMonoBehaviour>(), lightWithIdManager);
    }

    private void RegisterLight(LightWithIdMonoBehaviour? newLight, LightWithIdManager lightWithIdManager)
    {
        if (newLight != null) lightWithIdManager.RegisterLight(newLight);
    }

    private void RegisterMirror(GameObject clone)
    {
        var mirror = clone.GetComponent<Mirror>();
        if (mirror == null) return;

        _loggingService.Debug("Cloned a mirror surface");
        var originalMirrorRenderer = mirror._mirrorRenderer;
        var originalMaterial = mirror._mirrorMaterial;
        var clonedMirrorRenderer = Object.Instantiate(originalMirrorRenderer);
        var clonedMaterial = Object.Instantiate(originalMaterial);
        mirror._mirrorRenderer = clonedMirrorRenderer;
        mirror._mirrorMaterial = clonedMaterial;
    }

    private static void RegisterSpectrograms(GameObject clone)
    {
        // Hierarchy looks like this:
        // "Spectrograms" (one, this one has the Spectrogram component) --> "Spectrogram" (multiple, this is what we're cloning) -->
        // "Spectrogram0" + "Spectrogram1" (contain the MeshRenderers that need to be registered in the Spectrogram component)
        var parent = clone.transform.parent;
        var component = parent.gameObject.GetComponent<Spectrogram>();
        if (parent.name != "Spectrograms" || component == null) return;

        var spectrogramMeshRenderers = clone.GetComponentsInChildren<MeshRenderer>();
        var meshRendererList = component._meshRenderers.ToList();
        meshRendererList.AddRange(spectrogramMeshRenderers);
        component._meshRenderers = meshRendererList.ToArray();
    }

    #endregion

    #region Scene Modification

    private void DefaultSceneModifications(VideoConfig? videoConfig)
    {
        ApplyGlobalFixes();

        _loggingService.Debug($"Applying scene modifications for {_currentEnvironmentName}");
        if (_environmentHandlers.TryGetValue(_currentEnvironmentName, out var handler)) handler(videoConfig);
    }

    private void ApplyGlobalFixes()
    {
        // Hide screen if requested
        if (EnvironmentObjects.FirstOrDefault(x => x.name == HIDE_CINEMA_SCREEN_OBJECT_NAME
                                                   || x.name == HIDE_THEATER_SCREEN_OBJECT_NAME) != null)
        {
            _playbackManager.DisableVideoPlayerScreens();
            IsScreenHidden = true;
            _loggingService.Info("Hiding video screen due to custom platform");
        }

        // Remove front lights
        var frontLights = EnvironmentObjects.LastOrDefault(x =>
            (x.name == "FrontLights" || x.name == "FrontLight") && x.activeInHierarchy);
        frontLights?.SetActive(false);

        // Disable extra directional light
        var directionalLight =
            EnvironmentObjects.LastOrDefault(x => x.name == "DirectionalLight" && x.parentName == "CoreLighting");
        directionalLight?.SetActive(false);
    }

    #region Environment Handlers

    private void HandleNiceEnvironment(VideoConfig? videoConfig)
    {
        MoveDoubleColorLasers();
        MoveRotatingLasers(20f);
    }

    private void HandleBtsEnvironment(VideoConfig? videoConfig)
    {
        HideObject("MagicDoorSprite");
        AdjustPillars(videoConfig);
        UpdateMovementEffect();
    }

    private void HandleOriginsEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 12f);
    }

    private void HandleKdaEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 18f);
    }

    private void HandleRocketEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 16f);
    }

    private void HandleDragonsEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 25f);
    }

    private void HandleDragons2Environment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 25f);
    }

    private void HandleLinkinParkEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 16f);
    }

    private void HandleKaleidoscopeEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 25f);
    }

    private void HandleGlassDesertEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 20f);
    }

    private void HandleInterscopeEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 20f);
    }

    private void HandleCrabRaveEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 16f);
    }

    private void HandleSkrillexEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 16f);
    }

    private void HandlePyroEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 16f);
    }

    private void HandleLizzoEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 16f);
    }

    private void HandleWeaveEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 25f);
    }

    private void HandleEdmEnvironment(VideoConfig? videoConfig)
    {
        RepositionSymmetricObjects("Spectrogram", 25f);
    }

    #endregion

    #region Shared Helpers

    private void HideObject(string name)
    {
        var obj = EnvironmentObjects.LastOrDefault(x => x.name == name && x.activeInHierarchy);
        obj?.SetActive(false);
    }

    private void RepositionSymmetricObjects(string name, float offsetX)
    {
        var objs = EnvironmentObjects.Where(x => x.name == name && x.activeInHierarchy);
        foreach (var obj in objs)
        {
            var pos = obj.transform.position;
            obj.transform.position = new Vector3(pos.x < 0 ? -offsetX : offsetX, pos.y, pos.z);
        }
    }

    private void MoveRotatingLasers(float newX)
    {
        var rotatingLaserPairs =
            EnvironmentObjects.Where(x => x.name.Contains("RotatingLasersPair") && x.activeInHierarchy);
        foreach (var laser in rotatingLaserPairs)
        foreach (Transform child in laser.transform)
        {
            var pos = child.transform.position;
            child.transform.position = new Vector3(pos.x < 0 ? -newX : newX, pos.y, pos.z);
        }
    }

    private void MoveDoubleColorLasers()
    {
        var doubleColorLasers =
            EnvironmentObjects.Where(x => x.name.Contains("DoubleColorLaser") && x.activeInHierarchy);
        foreach (var laser in doubleColorLasers)
        {
            var pos = laser.transform.position;
            laser.transform.position = new Vector3(pos.x < 0 ? -20f : 20f, pos.y, pos.z);
        }
    }

    private void AdjustPillars(VideoConfig? videoConfig)
    {
        var pillarPair = EnvironmentObjects.LastOrDefault(x => x.name == "PillarPair" && x.activeInHierarchy);
        if (pillarPair != null)
        {
            var pos = pillarPair.transform.position;
            pillarPair.transform.position = new Vector3(pos.x, pos.y, -90f);
            pillarPair.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
        }
    }

    private void UpdateMovementEffect()
    {
        var movementEffect = EnvironmentObjects.LastOrDefault(x => x.name == "Movement" && x.activeInHierarchy);
        if (movementEffect != null)
        {
            var pos = movementEffect.transform.position;
            movementEffect.transform.position = new Vector3(pos.x, pos.y, -100f);
        }
    }

    #endregion

    #endregion
}