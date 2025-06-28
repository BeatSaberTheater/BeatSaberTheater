using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberTheater.Util;
using Zenject;

namespace BeatSaberTheater.Settings;

internal class MenuButtonManager : IInitializable
{
    private readonly MenuButtons _menuButtons;
    private readonly MainFlowCoordinator _mainFlowCoordinator;
    private readonly TheaterSettingsFlowCoordinator _theaterSettingsFlowCoordinator;
    private readonly MenuButton _menuButton;
    private readonly LoggingService _loggingService;

    public MenuButtonManager(MenuButtons menuButtons, MainFlowCoordinator mainFlowCoordinator,
        TheaterSettingsFlowCoordinator theaterSettingsFlowCoordinator, LoggingService loggingService)
    {
        _menuButtons = menuButtons;
        _mainFlowCoordinator = mainFlowCoordinator;
        _theaterSettingsFlowCoordinator = theaterSettingsFlowCoordinator;
        _loggingService = loggingService;

        _menuButton = new MenuButton("Theater", ShowFlowCoordinator);
    }

    public void Initialize()
    {
        _menuButtons.RegisterButton(_menuButton);
    }

    private void ShowFlowCoordinator()
    {
        _loggingService.Debug($"FlowCoordinator is null: {_theaterSettingsFlowCoordinator == null}");
        _loggingService.Debug($"MainFlowCoordinator is null: {_mainFlowCoordinator == null}");
        _mainFlowCoordinator?.PresentFlowCoordinator(_theaterSettingsFlowCoordinator);
    }
}