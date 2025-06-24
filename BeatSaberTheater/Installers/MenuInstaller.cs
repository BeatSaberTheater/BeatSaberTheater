using Zenject;
using BeatSaberTheater.Menu;

namespace BeatSaberTheater.Installers;

// This particular installer relates to bindings that are used in the main menu. It is related to the
// MainSettingsMenuViewControllersInstaller installer in the base game, and its InstallBindings is called when the
// game first loads into the main menu, and after settings are applied, which causes an internal reload of the game.

internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        // This will create a single instance of the type SettingsMenuManager and implement its interfaces
        // The BindInterfacesTo shortcut is useful since you don't want to write out and remember every base type:
        // Container.Bind(typeof(IInitializable, typeof(IDisposable)).To<SettingsMenuManager>().AsSingle();
        // Is the same as:
        Container.BindInterfacesTo<SettingsMenuManager>().AsSingle();

        // This will create a single instance of ExampleSettingsMenu, and lets it be injected into other types
        Container.Bind<ExampleSettingsMenu>().AsSingle();
    }
}