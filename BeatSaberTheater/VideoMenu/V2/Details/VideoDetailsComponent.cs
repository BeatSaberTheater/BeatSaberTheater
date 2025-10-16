using BeatSaberTheater.Util;
using BeatSaberTheater.Video.Config;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using System;
using TMPro;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2.Details;

internal class VideoDetailsComponent(Action onSearch, Action<int> applyOffset, Action onPreview, Action onDeleteConfig, Action onDeleteVideo)
    : ReactiveComponent
{
    // private Label _status = null!;
    private BsButton _deleteConfig = null!;

    // private BsButton _deleteVideo = null!;
    // private BsButton _previewButton = null!;
    // private Toggle _customizeOffset = null!;

    private readonly Action _onSearch = onSearch;
    private readonly Action _onPreview = onPreview;
    private readonly Action _onDeleteConfig = onDeleteConfig;
    private readonly Action _onDeleteVideo = onDeleteVideo;

    // Note: We should figure out a way how to bind to a sub-property of the videoConfig
    // Maybe have it implement NotifyPropertyChange on all its properties, and listen to events?
    // that way we can bind subproperties from the config to our fields
    private readonly ObservableValue<VideoConfig?> _videoConfig = Remember<VideoConfig?>(null);
    private readonly ObservableValue<BeatmapLevel?> _beatmapLevel = Remember<BeatmapLevel?>(null);

    // private LevelDetailViewController? _levelDetailMenu;

    protected override GameObject Construct()
    {
        return new Layout()
            {
                Children =
                {
                    // Title + delete config
                    new Layout
                    {
                        LayoutModifier = new YogaModifier() { MaxSize = new YogaVector() { y = 8.pt() } },
                        Children =
                        {
                            new Layout()
                                {
                                    Children =
                                    {
                                        new Label { Text = "Video Title", FontSize = 3.5f, }
                                            .Animate(_videoConfig, (label, config) => label.Text = TheaterFileHelpers.FilterEmoji(config?.title ?? ""))
                                            .AsFlexItem(flex: 0, alignSelf: Align.FlexStart),
                                        new Label() { Text = "Author | Duration", FontSize = 2.5f, Color = Color.white.ColorWithAlpha(0.75f) }
                                            .Animate(_videoConfig,
                                                (label, config) =>
                                                    label.Text =
                                                        $"{TheaterFileHelpers.FilterEmoji(config?.author ?? "")} | {TheaterFileHelpers.SecondsToString(config?.duration ?? 0)}")
                                            .AsFlexItem(flex: 0, alignSelf: Align.FlexStart)
                                    }
                                }.AsFlexItem(1)
                                .AsFlexGroup(FlexDirection.Column),
                            new BsButton { Text = "🗑", OnClick = () => _onDeleteConfig?.Invoke() }
                                .AsFlexItem()
                                .Bind(ref _deleteConfig)
                        }
                    }.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween).AsFlexItem(1),

                    // Thumbnail + info
                    new Layout
                        {
                            Children =
                            {
                                new Layout()
                                    {
                                        Children =
                                        {
                                            new WebImage()
                                                {
                                                    LayoutModifier = new YogaModifier() { Size = new YogaVector() { y = 100.pct(), x = 100.pct() } },
                                                    PreserveAspect = false
                                                }
                                                .Animate(_videoConfig,
                                                    (image, config) =>
                                                        image.Src = config?.videoID != null ? $"https://i.ytimg.com/vi/{config.videoID}/hqdefault.jpg" : null)
                                                .AsFlexItem(1, alignSelf: Align.Center),
                                            new Label() { WithinLayoutIfDisabled = false, Alignment = TextAlignmentOptions.Center}
                                                .Animate(_videoConfig, (label, config) =>
                                                {
                                                    if (config != null)
                                                    {
                                                        label.Text = config.DownloadState switch
                                                        {
                                                            DownloadState.Preparing => "Preparing",
                                                            DownloadState.Downloading =>
                                                                $"Downloading ({Convert.ToInt32(config.DownloadProgress * 100)}%)",
                                                            DownloadState.DownloadingVideo =>
                                                                $"Downloading video ({Convert.ToInt32(config.DownloadProgress * 100)}%)",
                                                            DownloadState.DownloadingAudio =>
                                                                $"Downloading audio ({Convert.ToInt32(config.DownloadProgress * 100)}%)",
                                                            DownloadState.Converting => (config.ConvertingProgress.HasValue)
                                                                ? (config.ConvertingProgress.HasValue
                                                                    ? $"Converting ({config.ConvertingProgress:##}%)"
                                                                    : "Converting...")
                                                                : $"Downloading ({Convert.ToInt32(config.DownloadProgress * 100)}%)",
                                                            DownloadState.Downloaded => "Preview",
                                                            _ => config.ErrorMessage ?? "Not downloaded"
                                                        };

                                                        label.Color = config.DownloadState switch
                                                        {
                                                            DownloadState.Downloading => Color.yellow,
                                                            DownloadState.DownloadingVideo => Color.yellow,
                                                            DownloadState.DownloadingAudio => Color.yellow,
                                                            DownloadState.Converting => Color.yellow,
                                                            _ => Color.white
                                                        };

                                                        label.Enabled = config?.DownloadState != DownloadState.Downloaded;
                                                    }
                                                })
                                                .AsFlexItem(),
                                            new BsPrimaryButton()
                                                {
                                                    Skew = 0,
                                                    Text = "Preview",
                                                    RichText = true,
                                                    Enabled = false,
                                                    WithinLayoutIfDisabled = false
                                                }
                                                .Animate(_videoConfig, (button, config) =>
                                                {
                                                    if (config != null)
                                                    {
                                                        button.Enabled = config?.DownloadState == DownloadState.Downloaded;
                                                    }
                                                })
                                                .AsFlexItem()
                                        }
                                    }
                                    .AsFlexGroup(FlexDirection.Column, overflow: Overflow.Hidden, gap: 1)
                                    .AsFlexItem(1),

                                // Options section
                                // Now should only contain offset, but in the future may have additional settings
                                // such as level/stage, position, screen position, screen size, etc...
                                new Layout()
                                    {
                                        Children =
                                        {
                                            new Layout()
                                                {
                                                    LayoutModifier = new YogaModifier() { Size = new YogaVector() { x = YogaValue.Stretch } },
                                                    Children =
                                                    {
                                                        new Label { Text = "Offset" }
                                                            .AsFlexItem(),
                                                        new Layout
                                                            {
                                                                Children =
                                                                {
                                                                    CreateOffsetButton("---", -1000),
                                                                    CreateOffsetButton("--", -100),
                                                                    CreateOffsetButton("-", -20),
                                                                    new Label { Text = "0", Alignment = TextAlignmentOptions.Center }
                                                                        .Animate(_videoConfig, (label, config) => label.Text = $"{(config?.offset ?? 0):n0} ms")
                                                                        .AsFlexItem(0),
                                                                    CreateOffsetButton("+", 20),
                                                                    CreateOffsetButton("++", 100),
                                                                    CreateOffsetButton("+++", 1000)
                                                                }
                                                            }
                                                            .AsFlexGroup(FlexDirection.Row, alignItems: Align.Center, justifyContent: Justify.FlexEnd,
                                                                gap: new YogaVector(1, 0))
                                                            .AsFlexItem(1)
                                                    }
                                                }.AsFlexItem(0)
                                                .AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween),
                                            new Layout()
                                                {
                                                    LayoutModifier = new YogaModifier() { Size = new YogaVector() { x = YogaValue.Stretch } },
                                                    Children =
                                                    {
                                                        new Label { Text = "Level" }
                                                            .AsFlexItem(),
                                                        new Layout { Children = { new TextDropdown<string>() { Items = { { "BigMirror", "Big Mirror" } } } } }
                                                            .AsFlexGroup(FlexDirection.Row, alignItems: Align.Center, justifyContent: Justify.FlexEnd,
                                                                gap: new YogaVector(1, 0))
                                                            .AsFlexItem(1)
                                                    }
                                                }.AsFlexItem(0)
                                                .AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween)
                                        }
                                    }
                                    .AsFlexGroup(FlexDirection.Column, gap: 1)
                                    .AsFlexItem(1)

                                // new Layout
                                //     {
                                //         Children =
                                //         {
                                //             new Label { Text = "Not downloaded" }
                                //                 .AsFlexItem()
                                //                 .Bind(ref _status),
                                //             new BsButton { Text = "Delete Video", OnClick = () => _onDeleteVideo?.Invoke() }
                                //                 .AsFlexItem()
                                //                 .Bind(ref _deleteVideo)
                                //         }
                                //     }
                                //     .AsFlexGroup(FlexDirection.Column)
                                //     .AsFlexItem(1)
                            }
                        }
                        .AsFlexGroup(FlexDirection.Row, gap: 2)
                        .AsFlexItem(1),
                    new Layout
                    {
                        Children =
                        {
                            new BsButton { Text = "Go Back" }.AsFlexItem(),
                            new BsButton { Text = "Download" }.AsFlexItem(),
                            new BsButton { Text = "Refine Search" }.AsFlexItem()
                        }
                    }.AsFlexGroup(FlexDirection.Row, gap: new YogaVector(2, 0), justifyContent: Justify.Center).AsFlexItem()

                    // Customize offset (label + toggle)
                    // new Layout { Children = { new Label { Text = "Customize offset" }.AsFlexItem(), new Toggle().AsFlexItem().Bind(ref _customizeOffset) } }
                    //     .AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween).AsFlexItem(),
                    // new BsButton { Text = "Preview", OnClick = () => _onPreview?.Invoke() }.AsFlexItem().Bind(ref _previewButton)
                }
            }
            .AsFlexGroup(FlexDirection.Column, gap: 2)
            .AsFlexItem(1)
            .Use();
    }

    public void SetVideo(VideoConfig video, BeatmapLevel? level)
    {
        _videoConfig.Value = video;
        _beatmapLevel.Value = level;
    }

    public void UpdateStatusText(VideoConfig videoConfig)
    {
        if (videoConfig == null) return;

        // Todo: This is a hack to force refresh of the value. Could be made nicer
        // if we were to subscribe on the actual offset value instead.
        _videoConfig.Value = _videoConfig.Value;
    }

    public void SetButtonState(bool enabled, bool libsAvailable)
    {
        // _previewButton.Interactable = enabled;
        _deleteConfig.Interactable = enabled;
        // _deleteVideo.Interactable = enabled;
        // underline color handling can be performed externally/left for later
    }

    public void SetCustomizeOffsetVisibility(bool visible)
    {
        // Toggle is represented in layout; we enable/disable it here:
        // _customizeOffset.SetActive(visible);
    }

    public void RefreshLevelDetailMenu()
    {
        // placeholder, original had LevelDetailViewController usage
    }

    public void SetupLevelDetailView(VideoConfig videoConfig)
    {
        // Mirror original: show/hide level detail menu and set text; left as placeholder
    }

    public void HideLevelDetailMenu()
    {
        // placeholder
    }

    private BsButton CreateOffsetButton(string label, int delta)
    {
        return new BsButton
            {
                Text = label,
                OnClick = () =>
                {
                    if (_videoConfig.Value != null)
                    {
                        _videoConfig.Value.offset += delta;

                        // Todo: This is a hack to force refresh of the value. Could be made nicer
                        // if we were to subscribe on the actual offset value instead.
                        _videoConfig.Value = _videoConfig.Value;
                        applyOffset?.Invoke(delta);
                    }
                }
            }
            .AsFlexItem(0);
    }
}