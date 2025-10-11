using System;
using System.Collections;
using System.Collections.Generic;
using BeatmapEditor3D.DataModels;
using BeatSaberTheater.Download;
using BeatSaberTheater.Playback;
using BeatSaberTheater.Services;
using BeatSaberTheater.Util;
using BeatSaberTheater.Util.ReactiveUi;
using BeatSaberTheater.Video;
using BeatSaberTheater.Video.Config;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2;

internal enum VideoMenuSection
{
    NoVideo,
    Details,
    Results
}

internal class VideoMenuComponent(TheaterCoroutineStarter coroutineStarter,
        DownloadService downloadService,
        LoggingService loggingService,
        PlaybackManager playbackManager,
        PluginConfig pluginConfig,
        SearchService searchService,
        VideoLoader videoLoader) : ReactiveComponent
{
    // Children
    private NoVideoComponent _noVideo = null!;
    private VideoDetailsComponent _details = null!;
    private VideoSearchResultsComponent _results = null!;

    // Reactive state
    private readonly ObservableValue<VideoMenuSection> _activeSection = new(VideoMenuSection.NoVideo);
    private readonly ObservableValue<VideoConfig?> _currentVideo = new(null);
    private readonly ObservableValue<string> _searchText = new(string.Empty);

    // Original-like state
    private BeatmapLevel? _currentLevel;
    private bool _videoMenuInitialized;
    // private bool _videoMenuActive;
    // private string? _thumbnailURL;
    private readonly List<YTResult> _searchResults = new();

    // coroutines
    private Coroutine? _searchLoadingCoroutine;
    private Coroutine? _updateSearchResultsCoroutine;

    protected override GameObject Construct()
    {
        return new Background()
        {
            WithinLayoutIfDisabled = true,
            LayoutModifier = new YogaModifier() { Margin = new YogaFrame() { left = 2.pt(), right = 2.pt() } },
            Children =
            {
                new NoVideoComponent(OnSearchClicked).AsFlexItem().Bind(ref _noVideo),
                new VideoDetailsComponent(OnSearchClicked, ApplyOffset, OnPreviewAction, OnDeleteConfigAction, OnDeleteVideoAction)
                    .AsFlexItem().Bind(ref _details),
                new VideoSearchResultsComponent(OnSelectResult, OnBackAction, OnDownloadAction, OnRefineAction)
                    .AsFlexItem().Bind(ref _results)
            }
        }
        .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false, padding: new YogaFrame(0, YogaValue.Point(2)))
        .AsBeatSaberBackground()
        .Use();
    }

    protected override void OnInitialize()
    {
        // update children enable state when active section changes
        _activeSection.ValueChangedEvent += section => UpdateSectionVisibility(section);
        UpdateSectionVisibility(_activeSection.Value);

        // subscribe to services (mirrors original Initialize wiring)
        searchService.SearchProgress += SearchProgress;
        searchService.SearchFinished += SearchFinished;
        downloadService.DownloadProgress += OnDownloadProgress;
        downloadService.DownloadFinished += OnDownloadFinished;
        VideoLoader.ConfigChanged += OnConfigChanged;
        Events.LevelSelected += OnLevelSelected;
    }

    public void Initialize()
    {
        if (_videoMenuInitialized) return;
        _videoMenuInitialized = true;

        // default to no video UI
        _activeSection.Value = VideoMenuSection.NoVideo;
        // _videoMenuActive = false;
    }

    public void Dispose()
    {
        // Unsubscribe everything safely
        searchService.SearchProgress -= SearchProgress;
        searchService.SearchFinished -= SearchFinished;
        downloadService.DownloadProgress -= OnDownloadProgress;
        downloadService.DownloadFinished -= OnDownloadFinished;
        VideoLoader.ConfigChanged -= OnConfigChanged;
        Events.LevelSelected -= OnLevelSelected;

        if (_searchLoadingCoroutine != null) coroutineStarter.StopCoroutine(_searchLoadingCoroutine);
        if (_updateSearchResultsCoroutine != null) coroutineStarter.StopCoroutine(_updateSearchResultsCoroutine);
    }

    private void UpdateSectionVisibility(VideoMenuSection section)
    {
        if (_noVideo != null) _noVideo.Enabled = section == VideoMenuSection.NoVideo;
        if (_details != null) _details.Enabled = section == VideoMenuSection.Details;
        if (_results != null) _results.Enabled = section == VideoMenuSection.Results;
    }

    // ---- External-style methods mirroring original VideoMenuUI behavior ----

    public void ResetVideoMenu()
    {
        // Mirror original behavior: choose view based on availability
        if (_currentVideo == null || !downloadService.LibrariesAvailable())
        {
            _noVideo.SetMessage(!downloadService.LibrariesAvailable()
                ? "Libraries not found. Please reinstall Theater.\r\nMake sure you unzip the files from the Libs folder into 'Beat Saber\\Libs'."
                : !pluginConfig.PluginEnabled ? "Theater is disabled.\r\nYou can re-enable it on the left side of the main menu." :
                _currentLevel == null ? "No level selected" : "No video configured");

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

        _currentLevel = level;
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
                _noVideo.SetMessage("This map uses Theater to modify the environment\r\nwithout displaying a video.\r\n\r\nNo configuration options available.");
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
        _details.SetThumbnail(_currentVideo.Value.videoID != null ? $"https://i.ytimg.com/vi/{_currentVideo.Value.videoID}/hqdefault.jpg" : null);
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

    // ---- Search handling (root mirrors original coroutines and SearchService events) ----

    public void OnQueryAction(string query)
    {
        _searchText.Value = query;
        _activeSection.Value = VideoMenuSection.Results;

        ResetSearchView();
        _results.SetLoading(true);
        _searchLoadingCoroutine = coroutineStarter.StartCoroutine(SearchLoadingCoroutine());

        searchService.Search(query);
    }

    private void SearchProgress(YTResult result)
    {
        // dedupe as original
        if (_searchResults.Contains(result)) return;

        _searchResults.Add(result);
        _updateSearchResultsCoroutine = coroutineStarter.StartCoroutine(UpdateSearchResults(result));
    }

    private void SearchFinished()
    {
        if (_searchResults.Count == 0)
        {
            _results.SetLoading(false);
            _results.SetNoResultsMessage("No search results found.\r\nUse the Refine Search button in the bottom right to choose a different search query.");
            return;
        }

        _results.SetLoading(false);
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

        _results.AddSearchResult(result, title, description, sprite);

        // enable download button if something selected (mirrors original logic)
        _results.SetDownloadInteractable(false);
    }

    private IEnumerator SearchLoadingCoroutine()
    {
        int count = 0;
        const string loadingText = "Searching for videos, please wait";
        _results.SetLoading(true);

        while (_results.IsLoading)
        {
            var periods = string.Empty;
            count++;
            for (var i = 0; i < count; i++) periods += ".";
            if (count == 3) count = 0;

            _results.SetLoadingText(loadingText + periods);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void ResetSearchView()
    {
        if (_searchLoadingCoroutine != null) coroutineStarter.StopCoroutine(_searchLoadingCoroutine);
        if (_updateSearchResultsCoroutine != null) coroutineStarter.StopCoroutine(_updateSearchResultsCoroutine);

        _searchResults.Clear();
        _results.ResetSearchView();
    }

    // ---- Actions from child components / UI commands (mirrors original UI actions) ----

    private void OnSearchClicked()
    {
        // show keyboard in original; we expose OnQueryAction for actual query
        // Here we route to Results view with current search text
        OnQueryAction(_searchText.Value ?? string.Empty);
    }

    private void OnSelectResult()
    {
        // user selected a result -> show details and let them download/preview
        // Download/preview will operate based on selected result in _results
        var selected = _results.GetSelectedResult();
        if (selected == null || _currentLevel is null) return;

        var config = new VideoConfig(selected, VideoLoader.GetTheaterLevelPath(_currentLevel))
        {
            NeedsToSave = true
        };
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
        _activeSection.Value = VideoMenuSection.Details;
        SetupVideoDetails();
    }

    private void OnRefineAction()
    {
        // show keyboard/emit event in original. Here just show results with current search text
        OnQueryAction(_searchText.Value ?? string.Empty);
    }

    // ---- Utility ----

    private void ApplyOffset(int offset)
    {
        if (_currentVideo.Value == null) return;
        _currentVideo.Value.offset += offset;
        _details.SetOffsetText($"{_currentVideo.Value.offset:n0} ms");
        _currentVideo.Value.NeedsToSave = true;
        playbackManager.ApplyOffset(offset);
    }
}
