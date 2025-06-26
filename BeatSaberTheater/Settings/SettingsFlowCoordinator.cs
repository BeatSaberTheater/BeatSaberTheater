using System;
using BeatSaberMarkupLanguage;
using BeatSaberTheater.Settings;
using BeatSaberTheater.Util;
using HMUI;
using Zenject;

namespace BeatSaberTheater;

public class SettingsFlowCoordinator(LoggingService _loggingService) : FlowCoordinator, IInitializable
{
    private SettingsController? _controller;

    public void Awake()
    {
        if (!_controller) _controller = BeatSaberUI.CreateViewController<SettingsController>();
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        try
        {
            if (!firstActivation) return;

            SetTitle("Cinema Settings");
            showBackButton = true;
            ProvideInitialViewControllers(_controller);
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }

    protected override void BackButtonWasPressed(ViewController viewController)
    {
        BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
    }

    public void Initialize()
    {
    }
}