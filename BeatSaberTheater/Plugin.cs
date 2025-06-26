using System.IO;
using BeatSaberMarkupLanguage.GameplaySetup;
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
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace BeatSaberTheater;

[Plugin(RuntimeOptions.DynamicInit)]
[NoEnableDisable]
internal class Plugin
{
    internal const string Capability = "Theater";
    private static bool _enabled;

    internal static IpaLogger Log { get; private set; } = null!;

    public static bool Enabled
    {
        get => _enabled && SettingsStore.Instance.PluginEnabled;
        private set => _enabled = value;
    }

    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);

        // Creates an instance of PluginConfig used by IPA to load and store config values
        var pluginConfig = ipaConfig.Generated<PluginConfig>();

        // Instructs SiraUtil to use this installer during Beat Saber's initialization
        // The PluginConfig is used as a constructor parameter for AppInstaller, so pass it to zenjector.Install()
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);

        // Instructs SiraUtil to use this installer when the main menu initializes
        zenjector.Install<VideoMenuInstaller>(Location.Menu);

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }

    [OnEnable]
    [UsedImplicitly]
    public void OnEnable()
    {
        Enabled = true;
        BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        // EnvironmentController.Init();
        Collections.RegisterCapability(Capability);
        if (File.Exists(Path.Combine(UnityGame.InstallPath, "dxgi.dll")))
            Log.Warn(
                "dxgi.dll is present, video may fail to play. To fix this, delete the file dxgi.dll from your main Beat Saber folder (not in Plugins).");

        //No need to index maps if the filter isn't going to be applied anyway
        // if (InstalledMods.BetterSongList) Loader.SongsLoadedEvent += VideoLoader.IndexMaps;
    }

    [OnDisable]
    [UsedImplicitly]
    public void OnDisable()
    {
        Enabled = false;
        BSEvents.lateMenuSceneLoadedFresh -= OnMenuSceneLoadedFresh;
        // Loader.SongsLoadedEvent -= VideoLoader.IndexMaps;
        SettingsUI.RemoveMenu();

        //TODO Destroying and re-creating the PlaybackController messes up the VideoMenu without any exceptions in the log. Investigate.
        //PlaybackController.Destroy();

        // EnvironmentController.Disable();
        // VideoLoader.StopFileSystemWatcher();
        Collections.DeregisterCapability(Capability);
    }

    private static void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO scenesTransition)
    {
        PlaybackController.Create();

        SettingsUI.CreateMenu();
        // SongPreviewPlayerController.Init();
        // AddBetterSongListFilter();
    }
}