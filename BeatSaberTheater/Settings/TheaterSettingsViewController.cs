using System;
using BeatSaberTheater.Settings.V2;
using BeatSaberTheater.Util;
using HMUI;
using Zenject;

namespace BeatSaberTheater.Settings;

internal class TheaterSettingsViewController
	: ViewController
{
	[Inject] private readonly PluginConfig _config = null!;
	[Inject] private readonly LoggingService _loggingService = null!;

	private TheaterSettingsViewComponent _viewComponent = null!;

	private void Awake()
	{
		_viewComponent = new TheaterSettingsViewComponent(_config);
		_viewComponent.Use(transform);
	}

	private const float FADE_DURATION = 0.2f;

	private void SetSettingsTexture()
	{
		// PlaybackController.Instance.VideoPlayer.SetStaticTexture(
		//     FileHelpers.LoadPNGFromResources("BeatSaberTheater.Resources.beat-saber-logo-landscape.png"));
	}

	protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
	{
		base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
		if (!_config.PluginEnabled) return;

		// PlaybackController.Instance.StopPlayback();
		// PlaybackController.Instance.VideoPlayer.FadeIn(FADE_DURATION);
		SetSettingsTexture();

		// if (!_config.TransparencyEnabled) PlaybackController.Instance.VideoPlayer.ShowScreenBody();
	}

	protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
	{
		base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
		try
		{
			//Throws NRE if the settings menu is open while the plugin gets disabled (e.g. by closing the game)
			// PlaybackController.Instance.VideoPlayer.FadeOut(FADE_DURATION);
			// PlaybackController.Instance.VideoPlayer.SetDefaultMenuPlacement();
		}
		catch (Exception e)
		{
			_loggingService.Debug(e);
		}
	}
}