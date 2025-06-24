using Zenject;

namespace BeatSaberTheater.Installers;

// An installer is where related bindings are grouped together. A binding sets up an object for injection.
// Zenject will handle object creation and figure out what needs to be injected automatically.
// It's recommended to check the Zenject documentation to learn more about dependency injection and why it exists.
// https://github.com/Mathijs-Bakker/Extenject?tab=readme-ov-file#what-is-dependency-injection

// This particular installer relates to bindings that are used during Beat Saber's initialization, and are made
// available in any context, whether that be in the menu, or during a map.
// It is related to the PCAppInit installer in the base game.

internal class AppInstaller : Installer
{
    private readonly PluginConfig pluginConfig;

    public AppInstaller(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
    }

    public override void InstallBindings()
    {
        // This allows the same instance of PluginConfig to be injected into in any class anywhere in the plugin
        Container.BindInstance(pluginConfig).AsSingle();

        // This will create a single instance of the type ExampleController and implement its interfaces
        // The BindInterfacesTo shortcut is useful since you don't want to write out and remember every base type:
        // Container.Bind(typeof(IInitializable, typeof(IDisposable)).To<ExampleController>().AsSingle();
        Container.BindInterfacesTo<ExampleController>().AsSingle();
    }
}