using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using BeatSaberTheater.Installers;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace BeatSaberTheater;

[Plugin(RuntimeOptions.DynamicInit)]
[NoEnableDisable]
internal class Plugin
{
    internal const string CAPABILITY = "Theater";
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
        zenjector.Install<MenuInstaller>(Location.Menu);

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }
}