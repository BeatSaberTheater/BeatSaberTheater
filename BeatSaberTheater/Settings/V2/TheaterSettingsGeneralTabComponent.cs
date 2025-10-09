using System;
using BeatSaberTheater.Util.ReactiveUi;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;
using Label = Reactive.Components.Basic.Label;
using Range = Reactive.BeatSaber.Components.Range;

namespace BeatSaberTheater.Settings.V2;

internal class TheaterSettingsGeneralTabComponent(PluginConfig? _config) : ReactiveComponent
{
	private Toggle _enableTheater = null!;
	private TextDropdown<VideoQuality.Mode> _mode = null!;
	private TextDropdown<VideoFormats.Format> _format = null!;
	private Toggle _forceBigMirror = null!;
	private Toggle _disableCustomPlatforms = null!;
	private Toggle _rotate90360maps = null!;
	private Toggle _showSongCover = null!;
	private Slider _downloadTimeoutSeconds = null!;
	private Slider _searchTimeoutSeconds = null!;

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
								new Toggle() { }
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
								new TextDropdown<VideoQuality.Mode>()
									{
										Items =
										{
											{ VideoQuality.Mode.Q1080P, "1080p" },
											{ VideoQuality.Mode.Q720P, "720p" },
											{ VideoQuality.Mode.Q480P, "480p" },
										}
									}
									.AsFlexItem()
									.Bind(ref _mode)
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
								new TextDropdown<VideoFormats.Format>()
									{
										Items =
										{
											{ VideoFormats.Format.Mp4, "Mp4" },
											{ VideoFormats.Format.Webm, "Webm" },
										}
									}
									.AsFlexItem()
									.Bind(ref _format)
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
								new Toggle() { }
									.AsFlexItem()
									.Bind(ref _forceBigMirror)
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
								new Toggle() { }
									.AsFlexItem()
									.Bind(ref _disableCustomPlatforms)
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
								new Toggle() { }
									.AsFlexItem()
									.Bind(ref _rotate90360maps)
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
								new Toggle() { }
									.AsFlexItem()
									.Bind(ref _showSongCover)
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
										Text = "Download Timeout"
									}
									.AsFlexItem(),
								new Slider()
									{
										ValueStep = 1,
										ValueRange = new Range(1, 300)
									}
									.AsFlexItem()
									.Bind(ref _downloadTimeoutSeconds)
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
										Text = "Search Timeout"
									}
									.AsFlexItem(),
								new Slider()
									{
										ValueStep = 1,
										ValueRange = new Range(1, 300)
									}
									.AsFlexItem()
									.Bind(ref _searchTimeoutSeconds)
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
		_enableTheater.Active = _config?.PluginEnabled ?? false;
		_format.Select(_config?.Format ?? VideoFormats.Format.Mp4);
		_mode.Select(_config?.QualityMode ?? VideoQuality.Mode.Q1080P);
		_forceBigMirror.Active = _config?.ForceDisableEnvironmentOverrides ?? false;
		_disableCustomPlatforms.Active = _config?.DisableCustomPlatforms ?? false;
		_rotate90360maps.Active = _config?.Enable360Rotation ?? false;
		_showSongCover.Active = _config?.CoverEnabled ?? false;
		_downloadTimeoutSeconds.Value = _config?.DownloadTimeoutSeconds ?? 0;
		_searchTimeoutSeconds.Value = _config?.SearchTimeoutSeconds ?? 0;

		if (_config != null)
		{
			ReactiveUiHelpers.SetupPropertyBinding(_enableTheater, _config, x => x.PluginEnabled);
			ReactiveUiHelpers.SetupPropertyBinding(_format, _config, x => x.Format);
			ReactiveUiHelpers.SetupPropertyBinding(_mode, _config, x => x.QualityMode);
			ReactiveUiHelpers.SetupPropertyBinding(_forceBigMirror, _config, x => x.ForceDisableEnvironmentOverrides);
			ReactiveUiHelpers.SetupPropertyBinding(_disableCustomPlatforms, _config, x => x.DisableCustomPlatforms);
			ReactiveUiHelpers.SetupPropertyBinding(_rotate90360maps, _config, x => x.Enable360Rotation);
			ReactiveUiHelpers.SetupPropertyBinding(_showSongCover, _config, x => x.CoverEnabled);
			ReactiveUiHelpers.SetupPropertyBinding(_downloadTimeoutSeconds, _config, x => x.DownloadTimeoutSeconds, x => Convert.ToInt32(x));
			ReactiveUiHelpers.SetupPropertyBinding(_searchTimeoutSeconds, _config, x => x.SearchTimeoutSeconds, x => Convert.ToInt32(x));
		}
	}
}

public static class IntExtensions
{
	public static int? TryParseIntDirect(this string value)
	{
		if (int.TryParse(value, out var i))
		{
			return i;
		}
		else
		{
			return null;
		}
	}
}