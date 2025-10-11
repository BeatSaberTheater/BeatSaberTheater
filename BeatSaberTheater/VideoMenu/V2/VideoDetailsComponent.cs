using System;
using BeatSaberTheater.Util;
using BeatSaberTheater.Video.Config;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using TMPro;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2;

internal class VideoDetailsComponent(Action onSearch, Action<int> applyOffset, Action onPreview, Action onDeleteConfig, Action onDeleteVideo) : ReactiveComponent
{
    private Label _title = null!;
    private WebImage _thumbnail = null!;
    private Label _author = null!;
    private Label _duration = null!;
    private Label _status = null!;
    private BsButton _deleteConfig = null!;
    private BsButton _deleteVideo = null!;
    private BsButton _previewButton = null!;
    private Toggle _customizeOffset = null!;
    private Label _offsetLabel = null!;
    private Label _offsetValue = null!;

    private readonly Action _onSearch = onSearch;
    private readonly Action<int> _applyOffset = applyOffset;
    private readonly Action _onPreview = onPreview;
    private readonly Action _onDeleteConfig = onDeleteConfig;
    private readonly Action _onDeleteVideo = onDeleteVideo;

    private VideoConfig? _currentVideoLocal;
    // private LevelDetailViewController? _levelDetailMenu;

    protected override GameObject Construct()
    {
        return new Layout
        {
            Children =
            {
                // Title + delete config
                new Layout
                {
                    Children =
                    {
                        new Label { Text = "Video Title", Alignment = TextAlignmentOptions.Center }.AsFlexItem().Bind(ref _title),
                        new BsButton { Text = "🗑", OnClick = () => _onDeleteConfig?.Invoke() }.AsFlexItem().Bind(ref _deleteConfig)
                    }
                }.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween).AsFlexItem(),

                // Thumbnail + info
                new Layout
                {
                    Children =
                    {
                        new WebImage().AsFlexItem(size: new YogaVector(40, 25)).Bind(ref _thumbnail),
                        new Layout
                        {
                            Children =
                            {
                                new Label { Text = "Author" }.AsFlexItem().Bind(ref _author),
                                new Label { Text = "Duration" }.AsFlexItem().Bind(ref _duration),
                                new Label { Text = "Not downloaded" }.AsFlexItem().Bind(ref _status),
                                new BsButton { Text = "Delete Video", OnClick = () => _onDeleteVideo?.Invoke() }.AsFlexItem().Bind(ref _deleteVideo)
                            }
                        }.AsFlexGroup(FlexDirection.Column).AsFlexItem()
                    }
                }.AsFlexGroup(FlexDirection.Row).AsFlexItem(),

                // Offset controls
                new Layout
                {
                    Children =
                    {
                        new Label { Text = "Video Offset", Alignment = TextAlignmentOptions.Center }.AsFlexItem().Bind(ref _offsetLabel),
                        new Layout
                        {
                            Children =
                            {
                                CreateOffsetButton("---", -1000),
                                CreateOffsetButton("--", -100),
                                CreateOffsetButton("-", -20),
                                new Label { Text = "0", Alignment = TextAlignmentOptions.Center }.AsFlexItem(size: new YogaVector(16,0)).Bind(ref _offsetValue),
                                CreateOffsetButton("+", 20),
                                CreateOffsetButton("++", 100),
                                CreateOffsetButton("+++", 1000)
                            }
                        }.AsFlexGroup(FlexDirection.Row, justifyContent: Justify.Center, gap: new YogaVector(1,0)).AsFlexItem()
                    }
                }.AsFlexGroup(FlexDirection.Column).AsFlexItem(),

                // Customize offset (label + toggle)
                new Layout
                {
                    Children =
                    {
                        new Label { Text = "Customize offset" }.AsFlexItem(),
                        new Toggle().AsFlexItem().Bind(ref _customizeOffset)
                    }
                }.AsFlexGroup(FlexDirection.Row, Justify.SpaceBetween).AsFlexItem(),

                new BsButton { Text = "Preview", OnClick = () => _onPreview?.Invoke() }.AsFlexItem().Bind(ref _previewButton)
            }
        }
        .AsFlexGroup(FlexDirection.Column, gap: new YogaVector(0, 2))
        .Use();
    }

    public void SetVideo(VideoConfig video, BeatmapLevel? level)
    {
        _currentVideoLocal = video;
        _title.Text = TheaterFileHelpers.FilterEmoji(video.title ?? "Untitled Video");
        _author.Text = "Author: " + TheaterFileHelpers.FilterEmoji(video.author ?? "Unknown Author");
        _duration.Text = "Duration: " + TheaterFileHelpers.SecondsToString(video.duration);
        _offsetValue.Text = $"{video.offset:n0} ms";
    }

    public void SetThumbnail(string? url)
    {
        if (_thumbnail == null) return;
        if (url == null)
        {
            // leave it to root to call SetThumbnailFromCover if necessary
            return;
        }

        _thumbnail.Src = url;
    }

    public void UpdateStatusText(VideoConfig videoConfig)
    {
        if (videoConfig == null) return;

        switch (videoConfig.DownloadState)
        {
            case DownloadState.Downloaded:
                _status.Text = "Downloaded";
                _status.Color = Color.green;
                break;
            case DownloadState.Preparing:
                _status.Text = "Preparing download...";
                _status.Color = Color.yellow;
                _previewButton.Interactable = false;
                break;
            case DownloadState.Downloading:
                _status.Text = $"Downloading ({Convert.ToInt32(videoConfig.DownloadProgress * 100)}%)";
                _status.Color = Color.yellow;
                _previewButton.Interactable = false;
                break;
            case DownloadState.DownloadingVideo:
            case DownloadState.DownloadingAudio:
            case DownloadState.Converting:
                _status.Text = videoConfig.DownloadState == DownloadState.Converting ? (videoConfig.ConvertingProgress.HasValue ? $"Converting ({videoConfig.ConvertingProgress:##}%)" : "Converting...") : $"Downloading ({Convert.ToInt32(videoConfig.DownloadProgress * 100)}%)";
                _status.Color = Color.yellow;
                _previewButton.Interactable = false;
                break;
            case DownloadState.NotDownloaded:
            case DownloadState.Cancelled:
                _status.Text = videoConfig.ErrorMessage ?? "Not downloaded";
                _status.Color = Color.red;
                _previewButton.Interactable = false;
                break;
            default:
                _status.Text = "Unknown";
                break;
        }
    }

    public void SetButtonState(bool enabled, bool libsAvailable)
    {
        _previewButton.Interactable = enabled;
        _deleteConfig.Interactable = enabled;
        _deleteVideo.Interactable = enabled;
        // underline color handling can be performed externally/left for later
    }

    public void SetCustomizeOffsetVisibility(bool visible)
    {
        // Toggle is represented in layout; we enable/disable it here:
        _customizeOffset.SetActive(visible);
    }

    public void SetOffsetText(string text)
    {
        _offsetValue.Text = text;
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
        return new BsButton { Text = label, OnClick = () => _applyOffset?.Invoke(delta) }.AsFlexItem(size: new YogaVector(10, 0));
    }
}
