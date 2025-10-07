using Zenject;
using BeatSaberTheater.Settings;
using BeatSaberTheater.Settings.V2;

namespace BeatSaberTheater.Installers;

// This particular installer relates to bindings that are used in the main menu. It is related to the
// MainSettingsMenuViewControllersInstaller installer in the base game, and its InstallBindings is called when the
// game first loads into the main menu, and after settings are applied, which causes an internal reload of the game.

internal class SettingsMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<TheaterSettingsViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<TheaterSettingsFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
    }
}