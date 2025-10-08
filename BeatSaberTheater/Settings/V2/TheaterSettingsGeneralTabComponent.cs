using Reactive;
using Reactive.Components;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;
using Label = Reactive.Components.Basic.Label;

namespace BeatSaberTheater.Settings.V2;

internal class TheaterSettingsGeneralTabComponent(PluginConfig? _config) : ReactiveComponent
{
	private Toggle _enableTheater = null!;

	protected override GameObject Construct()
	{
		return new Layout()
		{
			Children =
				{
					new Layout()
						{
							Children =
							{
								new Label()
									{
										Text = "Enable Theater"
									}
									.AsFlexItem(),
								new Toggle() {}
									.AsFlexItem()
									.Bind(ref _enableTheater)
							}
						}
						.AsFlexItem()
						.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),

					new Layout()
						{
							Children =
							{
								new Label()
									{
										Text = "Download Quality"
									}
									.AsFlexItem(),
								new TextDropdown<string>()
									{
										Items =
										{
											{ "1080p" ,"1080p" },
											{ "720p" ,"720p" },
											{ "480p" ,"480p" },
										}
									}
									.AsFlexItem()
							}
						}
						.AsFlexItem()
						.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),

					new Layout()
						{
							Children =
							{
								new Label()
									{
										Text = "Format"
									}
									.AsFlexItem(),
								new TextDropdown<string>()
									{
										Items =
										{
											{ "Mp4" ,"Mp4" },
											{ "Webm", "Webm" },
										}
									}
									.AsFlexItem()
							}
						}
						.AsFlexItem()
						.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),

					new Layout()
						{
							Children =
							{
								new Label()
									{
										Text = "Force \"Big Mirror\" Environment"
									}
									.AsFlexItem(),
								new Toggle() {}
									.AsFlexItem()
							}
						}
						.AsFlexItem()
						.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),

					new Layout()
						{
							Children =
							{
								new Label()
									{
										Text = "Disable CustomPlatforms"
									}
									.AsFlexItem(),
								new Toggle() {}
									.AsFlexItem()
							}
						}
						.AsFlexItem()
						.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),

					new Layout()
						{
							Children =
							{
								new Label()
									{
										Text = "Rotate in 90/360 maps"
									}
									.AsFlexItem(),
								new Toggle() {}
									.AsFlexItem()
							}
						}
						.AsFlexItem()
						.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),

					new Layout()
						{
							Children =
							{
								new Label()
									{
										Text = "Show Song Cover on Screen"
									}
									.AsFlexItem(),
								new Toggle() {}
									.AsFlexItem()
							}
						}
						.AsFlexItem()
						.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),
				}
		}
			.AsFlexGroup(FlexDirection.Column, gap: new YogaVector(0, 2), justifyContent: Justify.Center)
			.AsFlexItem()
			.Use();
	}

	protected override void OnStart()
	{
		Plugin._log.Debug($"_config is null: {_config is null}");
		_enableTheater.Active = _config?.PluginEnabled ?? false;

		_enableTheater.PropertyChangedEvent += (label, toggled) =>
		{
			Plugin._log.Debug($"Callback data: {toggled}");
			Plugin._log.Debug($"_config is null: {_config is null}");
			Plugin._log.Debug($"Setting theater enabled to {toggled}");
			if (_config is not null)
				_config.PluginEnabled = (bool)toggled;
		};
	}
}