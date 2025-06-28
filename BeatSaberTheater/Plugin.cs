using System.IO;
using BeatSaberTheater.Harmony;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using BeatSaberTheater.Installers;
using BeatSaberTheater.Screen;
using BS_Utils.Utilities;
using IPA.Utilities;
using JetBrains.Annotations;
using SongCore;
using Zenject;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace BeatSaberTheater;

[Plugin(RuntimeOptions.DynamicInit)]
[NoEnableDisable]
internal class Plugin
{
    internal const string Capability = "Theater";

    private PluginConfig _config { get; set; }
    private HarmonyPatchController? _harmonyPatchController;
    internal static IpaLogger _log { get; private set; } = null!;
    internal static DiContainer _menuContainer = null!;

    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        _log = ipaLogger;
        zenjector.UseLogger(_log);

        // Creates an instance of PluginConfig used by IPA to load and store config values
        _config = ipaConfig.Generated<PluginConfig>();

        // Instructs SiraUtil to use this installer during Beat Saber's initialization
        // The PluginConfig is used as a constructor parameter for AppInstaller, so pass it to zenjector.Install()
        zenjector.Install<AppInstaller>(Location.App, _config);

        // Instructs SiraUtil to use this installer when the main menu initializes
        zenjector.Install<SettingsMenuInstaller>(Location.Menu);
        zenjector.Install<VideoMenuInstaller>(Location.Menu);

        _log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }

    [OnEnable]
    [UsedImplicitly]
    public void OnEnable()
    {
        _config.PluginEnabled = true;
        BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        _harmonyPatchController = new HarmonyPatchController();
        ApplyHarmonyPatches();
        // EnvironmentController.Init();
        Collections.RegisterCapability(Capability);
        if (File.Exists(Path.Combine(UnityGame.InstallPath, "dxgi.dll")))
            _log.Warn(
                "dxgi.dll is present, video may fail to play. To fix this, delete the file dxgi.dll from your main Beat Saber folder (not in Plugins).");

        //No need to index maps if the filter isn't going to be applied anyway
        // if (InstalledMods.BetterSongList) Loader.SongsLoadedEvent += VideoLoader.IndexMaps;
    }

    [OnDisable]
    [UsedImplicitly]
    public void OnDisable()
    {
        _config.PluginEnabled = false;
        BSEvents.lateMenuSceneLoadedFresh -= OnMenuSceneLoadedFresh;
        // Loader.SongsLoadedEvent -= VideoLoader.IndexMaps;

        //TODO Destroying and re-creating the PlaybackController messes up the VideoMenu without any exceptions in the log. Investigate.
        //PlaybackController.Destroy();

        // EnvironmentController.Disable();
        // VideoLoader.StopFileSystemWatcher();
        Collections.DeregisterCapability(Capability);
    }

    private void ApplyHarmonyPatches()
    {
        _harmonyPatchController?.PatchAll();
    }

    private static void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO scenesTransition)
    {
        PlaybackController.Create();

        // SongPreviewPlayerController.Init();
        // AddBetterSongListFilter();
    }
}