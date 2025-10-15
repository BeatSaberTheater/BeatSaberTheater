using System;
using System.Collections;
using System.Collections.Generic;
using BeatmapEditor3D.DataModels;
using BeatSaberTheater.Download;
using BeatSaberTheater.Playback;
using BeatSaberTheater.Services;
using BeatSaberTheater.Util;
using BeatSaberTheater.Video;
using BeatSaberTheater.Video.Config;
using BeatSaberTheater.VideoMenu.V2.Details;
using BeatSaberTheater.VideoMenu.V2.NoVideo;
using BeatSaberTheater.VideoMenu.V2.Presets;
using BeatSaberTheater.VideoMenu.V2.SearchResults;
using Newtonsoft.Json;
using Reactive;
using Reactive.Yoga;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.VideoMenu.V2;

internal enum VideoMenuSection
{
    NoVideo,
    Details,
    Results,
    Presets
}

internal class VideoMenuComponent : ReactiveComponent, IDisposable
{
    // Children
    private NoVideoComponent _noVideo = null!;
    private VideoDetailsComponent _details = null!;
    private VideoSearchResultsComponent _results = null!;

    // Reactive state
    private readonly ObservableValue<VideoMenuSection> _activeSection = Remember(VideoMenuSection.NoVideo);
    private readonly ObservableValue<VideoConfig?> _currentVideo = new(null);
    private readonly ObservableValue<string> _searchText = new(string.Empty);

    // private bool _videoMenuInitialized;

    // private bool _videoMenuActive;
    // private string? _thumbnailURL;
    private readonly List<YTResult> _searchResults = new();

    // coroutines
    private BeatmapLevel _currentLevel;

    private readonly TheaterCoroutineStarter coroutineStarter = null!;
    private readonly DownloadService downloadService = null!;
    private readonly LoggingService loggingService = null!;
    private readonly PlaybackManager playbackManager = null!;
    private readonly PluginConfig pluginConfig = null!;
    private readonly SearchService searchService = null!;
    private readonly VideoLoader videoLoader = null!;

    public VideoMenuComponent(DiContainer container, BeatmapLevel currentLevel)
    {
        _currentLevel = currentLevel;
        videoLoader = container.Resolve<VideoLoader>();
        searchService = container.Resolve<SearchService>();
        pluginConfig = container.Resolve<PluginConfig>();
        playbackManager = container.Resolve<PlaybackManager>();
        loggingService = container.Resolve<LoggingService>();
        downloadService = container.Resolve<DownloadService>();
        coroutineStarter = container.Resolve<TheaterCoroutineStarter>();

        // update children enable state when active section changes
        _activeSection.ValueChangedEvent += section => UpdateSectionVisibility(section);
        UpdateSectionVisibility(_activeSection.Value);

        // subscribe to services (mirrors original Initialize wiring)
        downloadService.DownloadProgress += OnDownloadProgress;
        downloadService.DownloadFinished += OnDownloadFinished;
        VideoLoader.ConfigChanged += OnConfigChanged;

        _results.SearchService = searchService;
        _results.CurrentLevel = currentLevel;
        _results.CoroutineStarter = coroutineStarter;
    }

    protected override GameObject Construct()
    {
        return new Layout()
        {
            LayoutModifier = new YogaModifier() { Margin = new YogaFrame() { left = 2.pt(), right = 2.pt() } },
            Children =
                {
                    new Layout()
                        {
                            Enabled = _activeSection.Value == VideoMenuSection.NoVideo,
                            Children = { new NoVideoComponent(OnSearchClicked, OnPresetsSearchClicked).AsFlexItem().Bind(ref _noVideo), }
                        }
                        .Animate(_activeSection, (layout, section) => { layout.Enabled = section == VideoMenuSection.NoVideo; })
                        .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false)
                        .AsFlexItem(1),
                    new Layout()
                        {
                            Enabled = _activeSection.Value == VideoMenuSection.Details,
                            Children =
                            {
                                new VideoDetailsComponent(OnSearchClicked, ApplyOffset, OnPreviewAction, OnDeleteConfigAction, OnDeleteVideoAction)
                                    .AsFlexItem().Bind(ref _details),
                            }
                        }
                        .Animate(_activeSection, (layout, section) => { layout.Enabled = section == VideoMenuSection.Details; })
                        .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false)
                        .AsFlexItem(1),
                    new Layout()
                        {
                            Enabled = _activeSection.Value == VideoMenuSection.Results,
                            Children =
                            {
                                new VideoSearchResultsComponent(OnSelectResult, OnBackAction, OnDownloadAction, OnRefineAction)
                                    .AsFlexItem(1).Bind(ref _results)
                            }
                        }
                        .Animate(_activeSection, (layout, section) => { layout.Enabled = section == VideoMenuSection.Results; })
                        .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false)
                        .AsFlexItem(1),
                    new Layout()
                        {
                            Enabled = _activeSection.Value == VideoMenuSection.Presets,
                            Children = { new VideoDetailsPresetsComponent() }
                        }
                        .Animate(_activeSection, (layout, section) => { layout.Enabled = section == VideoMenuSection.Presets; })
                        .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false)
                        .AsFlexItem(1)
                }
        }
            .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false, padding: new YogaFrame(0, YogaValue.Point(2)))
            .AsFlexItem(1)
            .Use();
    }

    public void Dispose()
    {
        // Unsubscribe everything safely
        downloadService.DownloadProgress -= OnDownloadProgress;
        downloadService.DownloadFinished -= OnDownloadFinished;
        VideoLoader.ConfigChanged -= OnConfigChanged;
        Events.LevelSelected -= OnLevelSelected;

        // if (_searchLoadingCoroutine != null) coroutineStarter.StopCoroutine(_searchLoadingCoroutine);
        // if (_updateSearchResultsCoroutine != null) coroutineStarter.StopCoroutine(_updateSearchResultsCoroutine);
    }

    private void UpdateSectionVisibility(VideoMenuSection section)
    {
        if (_noVideo != null) _noVideo.Enabled = section == VideoMenuSection.NoVideo;
        if (_details != null) _details.Enabled = section == VideoMenuSection.Details;
        if (_results != null) _results.Enabled = section == VideoMenuSection.Results;
    }

    // ---- External-style methods mirroring original VideoMenuUI behavior ----

    public VideoConfig? GetCurrentVideoConfig()
    {
        return _currentVideo.Value;
    }

    public void ResetVideoMenu()
    {
        // Mirror original behavior: choose view based on availability
        if (_currentVideo == null || !downloadService.LibrariesAvailable())
        {
            _noVideo.SetMessage(!downloadService.LibrariesAvailable()
                ? "Libraries not found. Please reinstall Theater.\r\nMake sure you unzip the files from the Libs folder into 'Beat Saber\\Libs'."
                : !pluginConfig.PluginEnabled
                    ? "Theater is disabled.\r\nYou can re-enable it on the left side of the main menu."
                    : _currentLevel == null
                        ? "No level selected"
                        : "No video configured");

            _activeSection.Value = VideoMenuSection.NoVideo;
            _noVideo.SetSearchButtonActive(downloadService.LibrariesAvailable() && _currentLevel != null && !VideoLoader.IsDlcSong(_currentLevel));
            return;
        }

        // has video -> show details
        SetupVideoDetails();
    }

    public void HandleDidSelectEditorBeatmap(BeatmapDataModel beatmapData, string originalPath)
    {
        if (pluginConfig.PluginEnabled) return;

        playbackManager.StopPreview(true);
        if (_currentVideo.Value?.NeedsToSave == true) videoLoader.SaveVideoConfig(_currentVideo.Value);

        _currentVideo.Value = videoLoader.GetConfigForEditorLevel(beatmapData, originalPath);
        videoLoader.SetupFileSystemWatcher(originalPath);
        playbackManager.SetSelectedLevel(null, _currentVideo.Value);
    }

    public void HandleDidSelectLevel(BeatmapLevel? level)
    {
        // similar to original: stop preview, save pending config etc.
        playbackManager.StopPreview(true);

        if (_currentVideo.Value?.NeedsToSave == true) videoLoader.SaveVideoConfig(_currentVideo.Value);

        _currentLevel = level!;
        if (_currentLevel == null)
        {
            _currentVideo.Value = null;
            ResetVideoMenu();
            return;
        }

        _currentVideo.Value = videoLoader.GetConfigForLevel(_currentLevel);
        videoLoader.SetupFileSystemWatcher(_currentLevel);
        playbackManager.SetSelectedLevel(_currentLevel, _currentVideo.Value);

        // prepare default search text similar to original
        _searchText.Value = _currentLevel.songName + (!string.IsNullOrEmpty(_currentLevel.songAuthorName) ? " " + _currentLevel.songAuthorName : "");
        SetupVideoDetails();
    }

    private void OnLevelSelected(LevelSelectedArgs levelSelectedArgs)
    {
        if (levelSelectedArgs.BeatmapData != null)
        {
            loggingService.Debug("Level selected from VideoMenuUI");
            HandleDidSelectEditorBeatmap(levelSelectedArgs.BeatmapData, levelSelectedArgs.OriginalPath!);
            return;
        }

        HandleDidSelectLevel(levelSelectedArgs.BeatmapLevel);
    }

    private void OnConfigChanged(VideoConfig? config)
    {
        _currentVideo.Value = config;
        SetupVideoDetails();
    }

    private void OnDownloadProgress(VideoConfig vc)
    {
        if (_currentVideo.Value == vc)
        {
            Plugin._log.Info("Got download progress: " + vc.DownloadProgress);
            _details.UpdateStatusText(vc);
            _details.SetupLevelDetailView(vc);
        }
    }

    private void OnDownloadFinished(VideoConfig vc)
    {
        if (_currentVideo.Value != vc) return;

        if (vc.ErrorMessage != null)
        {
            SetupVideoDetails();
            return;
        }

        playbackManager.PrepareVideo(vc);
        if (_currentLevel != null) videoLoader.RemoveConfigFromCache(_currentLevel);
        SetupVideoDetails();
        _details.RefreshLevelDetailMenu();
    }

    public void SetupVideoDetails()
    {
        if (_currentVideo.Value == null || !downloadService.LibrariesAvailable())
        {
            ResetVideoMenu();
            return;
        }

        // If the map doesn't have video mapping and only environment modifications:
        if (_currentVideo.Value.videoID == null && _currentVideo.Value.videoUrl == null)
        {
            if (_currentVideo.Value.forceEnvironmentModifications == true)
            {
                _noVideo.SetMessage(
                    "This map uses Theater to modify the environment\r\nwithout displaying a video.\r\n\r\nNo configuration options available.");
                _noVideo.SetSearchButtonActive(false);
                _activeSection.Value = VideoMenuSection.NoVideo;
            }
            else
            {
                ResetVideoMenu();
            }

            return;
        }

        // Display details
        _details.SetVideo(_currentVideo.Value, _currentLevel);
        _details.UpdateStatusText(_currentVideo.Value);
        _details.SetButtonState(true, downloadService.LibrariesAvailable());

        _activeSection.Value = VideoMenuSection.Details;

        // offset controls visibility mirrors original CustomizeOffset property behavior
        if (_currentVideo.Value.userSettings?.customOffset == true || !(_currentVideo.Value.configByMapper ?? false))
        {
            _details.SetCustomizeOffsetVisibility(_currentVideo.Value.userSettings?.customOffset == true);
        }
        else
        {
            _details.SetCustomizeOffsetVisibility(false);
        }
    }

    private IEnumerator UpdateSearchResults(YTResult result)
    {
        var title = $"[{TheaterFileHelpers.SecondsToString(result.Duration)}] {TheaterFileHelpers.FilterEmoji(result.Title)}";
        var description = TheaterFileHelpers.FilterEmoji(result.Author);

        try
        {
            var stillImage = result.IsStillImage();
            var descriptionAddition = stillImage ? "Likely a still image" : result.GetQualityString() ?? "";
            if (descriptionAddition.Length > 0) description += "   |   " + descriptionAddition;
        }
        catch (Exception e)
        {
            loggingService.Warn(e);
        }

        // download thumbnail
        string? thumbUrl = $"https://i.ytimg.com/vi/{result.ID}/mqdefault.jpg";
        Sprite? sprite = null;
        using (var uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(thumbUrl))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityEngine.Networking.UnityWebRequest.Result.ConnectionError &&
                uwr.result != UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                var tex = ((UnityEngine.Networking.DownloadHandlerTexture)uwr.downloadHandler).texture;
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
            }
            else
            {
                loggingService.Debug(uwr.error);
            }
        }

        // enable download button if something selected (mirrors original logic)
        _results.SetDownloadInteractable(false);
    }

    // ---- Actions from child components / UI commands (mirrors original UI actions) ----

    private void OnSearchClicked()
    {
        // show keyboard in original; we expose OnQueryAction for actual query
        // Here we route to Results view with current search text
        // OnQueryAction(_searchText.Value ?? string.Empty);
        _activeSection.Value = VideoMenuSection.Results;
    }

    private void OnPresetsSearchClicked()
    {
        _activeSection.Value = VideoMenuSection.Presets;
    }

    private void OnSelectResult()
    {
        // user selected a result -> show details and let them download/preview
        // Download/preview will operate based on selected result in _results
        var selected = _results.GetSelectedResult();
        if (selected == null || _currentLevel is null) return;

        var config = new VideoConfig(selected, VideoLoader.GetTheaterLevelPath(_currentLevel)) { NeedsToSave = true };
        videoLoader.AddConfigToCache(config, _currentLevel!);
        searchService.StopSearch();
        downloadService.StartDownload(config, pluginConfig.QualityMode, pluginConfig.Format);
        _currentVideo.Value = config;

        SetupVideoDetails();
    }

    private void OnDownloadAction()
    {
        // Triggered by search results download button
        var selected = _results.GetSelectedResult();
        if (selected == null || _currentLevel == null)
        {
            loggingService.Error("No selection or level on download request");
            return;
        }

        _results.SetDownloadInteractable(false);
        var config = new VideoConfig(selected, VideoLoader.GetTheaterLevelPath(_currentLevel)) { NeedsToSave = true };
        videoLoader.AddConfigToCache(config, _currentLevel);
        searchService.StopSearch();
        downloadService.StartDownload(config, pluginConfig.QualityMode, pluginConfig.Format);
        _currentVideo.Value = config;
        SetupVideoDetails();
    }

    private void OnPreviewAction()
    {
        playbackManager.StartPreview().Start();
        _details.SetButtonState(true, downloadService.LibrariesAvailable());
    }

    private void OnDeleteVideoAction()
    {
        var cur = _currentVideo.Value;
        if (cur == null)
        {
            loggingService.Warn("delete video requested but current video is null");
            return;
        }

        playbackManager.StopPreview(true);

        switch (cur.DownloadState)
        {
            case DownloadState.Preparing:
            case DownloadState.Downloading:
            case DownloadState.DownloadingAudio:
            case DownloadState.DownloadingVideo:
                downloadService.CancelDownload(cur);
                break;
            case DownloadState.NotDownloaded:
            case DownloadState.Cancelled:
                cur.DownloadProgress = 0;
                searchService.StopSearch();
                downloadService.StartDownload(cur, pluginConfig.QualityMode, pluginConfig.Format);
                cur.NeedsToSave = true;
                videoLoader.AddConfigToCache(cur, _currentLevel!);
                break;
            default:
                videoLoader.DeleteVideo(cur);
                playbackManager.StopAndUnloadVideo();
                SetupVideoDetails();
                _details.RefreshLevelDetailMenu();
                break;
        }

        if (_currentVideo.Value != null)
            _details.UpdateStatusText(_currentVideo.Value);
    }

    private void OnDeleteConfigAction()
    {
        var cur = _currentVideo.Value;
        if (cur == null || _currentLevel == null)
        {
            loggingService.Warn("Failed to delete config: Either currentVideo or currentLevel is null");
            return;
        }

        playbackManager.StopPreview(true);
        playbackManager.StopPlayback();
        playbackManager.HideVideoPlayer();

        if (cur.IsDownloading) downloadService.CancelDownload(cur);

        videoLoader.DeleteVideo(cur);
        var success = videoLoader.DeleteConfig(cur, _currentLevel);
        if (success) _currentVideo.Value = null;

        _details.HideLevelDetailMenu();
        ResetVideoMenu();
    }

    private void OnBackAction()
    {
        // go back to details
        _activeSection.Value = VideoMenuSection.NoVideo;
        // SetupVideoDetails();
    }

    private void OnRefineAction()
    {
        // show keyboard/emit event in original. Here just show results with current search text
        // OnQueryAction(_searchText.Value ?? string.Empty);
    }

    // ---- Utility ----

    private void ApplyOffset(int offset)
    {
        if (_currentVideo.Value == null) return;
        _currentVideo.Value.offset += offset;
        _currentVideo.Value.NeedsToSave = true;
        playbackManager.ApplyOffset(offset);
    }
}