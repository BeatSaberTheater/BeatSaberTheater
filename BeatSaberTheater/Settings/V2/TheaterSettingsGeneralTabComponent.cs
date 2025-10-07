using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;
using Label = Reactive.Components.Basic.Label;

namespace BeatSaberTheater.Settings.V2;

public class TheaterSettingsGeneralTabComponent : ReactiveComponent
{
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
}