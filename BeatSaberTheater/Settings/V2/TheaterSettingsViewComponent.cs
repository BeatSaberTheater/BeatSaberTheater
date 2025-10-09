using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;

namespace BeatSaberTheater.Settings.V2;

internal class TheaterSettingsViewComponent(PluginConfig config) : ReactiveComponent
{
	private enum TheaterSettingsTabs
	{
		General,
		Visuals
	}

	protected override GameObject Construct()
	{
		var currentTab = Remember(TheaterSettingsTabs.General);
		return new Layout()
		{
			Children =
				{
					new Layout()
						{
							Children =
							{
								new BsButton()
									{
										Text = "General",
										OnClick = (() =>
										{
											currentTab.Value = TheaterSettingsTabs.General;
										})
									}
									.AsFlexItem(),
								new BsButton()
								{
									Text = "Visuals",
									OnClick = (() =>
									{
										currentTab.Value = TheaterSettingsTabs.Visuals;
									})
								}.AsFlexItem()
							}
						}
						.AsFlexItem(maxSize: new YogaVector { y = 10f }, flex: 1)
						.AsFlexGroup(FlexDirection.Row, Justify.Center, gap: new YogaVector(2f, 0)),

					new Layout()
					{
						Children =
						{
							new Layout()
								{
									Enabled = currentTab == TheaterSettingsTabs.General,
									WithinLayoutIfDisabled = false,
									Children =
									{
										new TheaterSettingsGeneralTabComponent(config)
									}
								}
								.Animate(currentTab, (layout, tabs) => { layout.Enabled = tabs == TheaterSettingsTabs.General; })
								.AsFlexGroup(FlexDirection.Column)
								.AsFlexItem(flex: 1, maxSize: new YogaVector() { x = YogaValue.Percent(75) }),

							new Layout()
								{
									Enabled = currentTab == TheaterSettingsTabs.Visuals,
									WithinLayoutIfDisabled = false,
									Children =
									{
										new TheaterSettingsVisualsTabComponent(config)
									}
								}
								.Animate(currentTab, (layout, tabs) => { layout.Enabled = tabs == TheaterSettingsTabs.Visuals; })
								.AsFlexGroup(FlexDirection.Column)
								.AsFlexItem(flex: 1, maxSize: new YogaVector() { x = YogaValue.Percent(75) })
						}
					}
					.AsFlexGroup(FlexDirection.Row, Justify.Center, Align.FlexStart)
						.AsFlexItem(flex: 1)
				}
		}.AsFlexGroup(FlexDirection.Column)
			.Use();
	}
}