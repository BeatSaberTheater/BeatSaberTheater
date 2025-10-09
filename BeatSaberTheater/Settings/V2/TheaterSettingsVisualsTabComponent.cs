using System;
using BeatSaberTheater.Util.ReactiveUi;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;
using Range = Reactive.BeatSaber.Components.Range;

namespace BeatSaberTheater.Settings.V2;

internal class TheaterSettingsVisualsTabComponent(PluginConfig _config) : ReactiveComponent
{
	private Toggle _curvedScreen = null!;
	private Toggle _transparentScreen = null!;
	private Slider _bloomIntensity = null!;
	private Slider _cornerRoundness = null!;

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
							Text = "Curved Screen"
						}.AsFlexItem(),
						new Toggle { }
							.AsFlexItem()
							.Bind(ref _curvedScreen)
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
							Text = "Transparent Screen"
						}.AsFlexItem(),
						new Toggle()
							.AsFlexItem()
							.Bind(ref _transparentScreen)
					}
				}
				.AsFlexItem()
				.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),
				
				new Layout()
				{
					Children =
					{
						new Label
							{
								Text = "Bloom Intensity"
							}
							.AsFlexItem(),
						new Slider()
							{
								ValueRange = new Range(0, 150),
								ValueStep = 1,
							}
							.AsFlexItem()
							.Bind(ref _bloomIntensity)
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
							Text = "Corner Roundness"
						}.AsFlexItem(),
						new Slider()
						{
							ValueRange = new Range(0, 100),
							ValueStep = 1
						}
						.AsFlexItem()
						.Bind(ref _cornerRoundness)
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
		_curvedScreen.Active = _config.CurvedScreen;
		_transparentScreen.Active = _config.TransparencyEnabled;
		_bloomIntensity.Value = _config.BloomIntensity;
		_cornerRoundness.Value = _config.CornerRoundness;
		
		ReactiveUiHelpers.SetupPropertyBinding(_curvedScreen, _config, x => x.CurvedScreen);
		ReactiveUiHelpers.SetupPropertyBinding(_transparentScreen, _config, x => x.TransparencyEnabled);
		ReactiveUiHelpers.SetupPropertyBinding(_bloomIntensity, _config, x => x.BloomIntensity, x => Convert.ToInt32(x));
		ReactiveUiHelpers.SetupPropertyBinding(_cornerRoundness, _config, x => x.CornerRoundness, x => Convert.ToInt32(x));
	}
}