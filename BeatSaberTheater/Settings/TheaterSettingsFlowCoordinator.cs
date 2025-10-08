using System;
using BeatSaberMarkupLanguage;
using HMUI;
using Zenject;

namespace BeatSaberTheater.Settings;

public class TheaterSettingsFlowCoordinator : FlowCoordinator
{
    [Inject] private readonly TheaterSettingsViewController
        _viewController = null!;

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle("Theater Settings");
            showBackButton = true;
        }

        if (addedToHierarchy) ProvideInitialViewControllers(_viewController);
    }

    protected override void BackButtonWasPressed(ViewController viewController)
    {
        BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
    }
}