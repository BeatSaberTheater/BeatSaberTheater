using System;
using System.Linq.Expressions;
using System.Reflection;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;
using Label = Reactive.Components.Basic.Label;

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
	private InputField _downloadTimeoutSeconds = null!;
	private InputField _searchTimeoutSeconds = null!;

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
								new InputField()
									{
										TextApplicationContract = s => int.TryParse(s, out _),
										Keyboard = new KeyboardModal<Keyboard, InputField>
										{
											Offset = new(0f, 32f)
										}
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
								new InputField()
									{
										TextApplicationContract = s => int.TryParse(s, out _),
										// Todo: Create our own Numbers-Only keyboard?
										Keyboard = new KeyboardModal<Keyboard, InputField>
										{
											Offset = new(0f, 32f)
										}
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

	private void SetupPropertyBinding<T>(ReactiveComponent component, Expression<Func<PluginConfig, T>> property,
		Expression<Func<object, object>>? conversion = null)
	{
		component.PropertyChangedEvent += (_, o) =>
		{
			if (_config is not null)
			{
				var memberExpression = property.Body as MemberExpression;
				var field = memberExpression?.Member as FieldInfo;
				var prop = memberExpression?.Member as PropertyInfo;

				var valueToWrite = conversion != null ? conversion.Compile()(o) : o;
				if (field != null)
				{
					Plugin._log.Debug("field Value -> " + valueToWrite);
					field.SetValue(_config, valueToWrite);
				}

				else if (prop != null)
				{
					Plugin._log.Debug("prop Value -> " + valueToWrite);
					prop.SetValue(_config, valueToWrite);
				}
				else
				{
					Plugin._log.Error("Could not find property to write: " + property);
				}
			}
		};
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

		// Todo: figure out how to bind initial values to inputfields
		_downloadTimeoutSeconds.Text = _config?.DownloadTimeoutSeconds.ToString() ?? "0";
		_searchTimeoutSeconds.Text = _config?.SearchTimeoutSeconds.ToString() ?? "0";

		SetupPropertyBinding(_enableTheater, x => x.PluginEnabled);
		SetupPropertyBinding(_format, x => x.Format);
		SetupPropertyBinding(_mode, x => x.QualityMode);
		SetupPropertyBinding(_forceBigMirror, x => x.ForceDisableEnvironmentOverrides);
		SetupPropertyBinding(_disableCustomPlatforms, x => x.DisableCustomPlatforms);
		SetupPropertyBinding(_rotate90360maps, x => x.Enable360Rotation);
		SetupPropertyBinding(_showSongCover, x => x.CoverEnabled);
		SetupPropertyBinding(_downloadTimeoutSeconds, x => x.DownloadTimeoutSeconds,
			o => o.ToString().TryParseIntDirect() == null ? 0 : int.Parse(o.ToString()));
		SetupPropertyBinding(_searchTimeoutSeconds, x => x.SearchTimeoutSeconds, o => o.ToString().TryParseIntDirect() == null ? 0 : int.Parse(o.ToString()));
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