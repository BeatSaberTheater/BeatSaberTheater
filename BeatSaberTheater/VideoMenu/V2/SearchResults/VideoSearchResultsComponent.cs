using BeatSaberTheater.Download;
using BeatSaberTheater.Services;
using BeatSaberTheater.Util;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Components;
using Reactive.Yoga;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using FlexDirection = Reactive.Yoga.FlexDirection;
using Justify = Reactive.Yoga.Justify;
using Label = Reactive.BeatSaber.Components.Label;
using ScrollArea = Reactive.Components.Basic.ScrollArea;

namespace BeatSaberTheater.VideoMenu.V2.SearchResults;

internal class VideoSearchResultsComponent : ReactiveComponent, IDisposable
{
    private Label _loadingLabel = null!;
    private BsButton _downloadButton = null!;
    private BsButton _refineButton = null!;
    private BsButton _backButton = null!;
    private ListView<YTResult, VideoResultListCell> _resultsView = null!;

    private readonly Action _onSelect;

    // internal result store
    private readonly ObservableValue<IReadOnlyList<YTResult>> _results = Remember<IReadOnlyList<YTResult>>(new List<YTResult>());
    private int _selectedIndex = -1;
    public BeatmapLevel CurrentLevel { get; set; } = null!;
    public SearchService SearchService { get; set; } = null!;
    public TheaterCoroutineStarter CoroutineStarter { get; set; } = null!;

    private readonly Action _onBack;
    private readonly Action _onDownload;
    private readonly Action _onRefine;
    private Coroutine? _searchLoadingCoroutine;
    // private Coroutine? _updateSearchResultsCoroutine;

    public VideoSearchResultsComponent(Action onSelect, Action onBack, Action onDownload, Action onRefine)
    {
        _onBack = onBack;
        _onDownload = onDownload;
        _onRefine = onRefine;
        _onSelect = onSelect;
    }

    protected override void OnStart()
    {
        if (SearchService == null)
        {
            Plugin._log.Error("Search service is not set!");
        }
        else
        {
            SearchService.SearchProgress += SearchServiceOnSearchProgress;
            SearchService.SearchFinished += SearchServiceOnSearchFinished;

            OnQueryAction($"{CurrentLevel.songAuthorName} {CurrentLevel.songName}");
        }
    }

    private void SearchServiceOnSearchFinished()
    {
        Plugin._log.Debug("Search finished");
        if (_searchLoadingCoroutine != null)
        {
            CoroutineStarter.StopCoroutine(_searchLoadingCoroutine);
        }

        SetLoading(false);
    }

    private void SearchServiceOnSearchProgress(YTResult obj)
    {
        // Todo: add beatmap details to YTResult so we can filter unwanted search results here
        // mayhaps even tag YTResults with a specific search request id
        Plugin._log.Debug($"Search progress: {obj.ToString()}");

        if (!_results.Value.Any(x => x.ID == obj.ID))
        {
            // Todo: prevent expensive list copying
            _results.Value = _results.Value.ToList().Append(obj).ToList();
            _resultsView.Items = _results.Value;
        }
    }

    public bool IsLoading { get; private set; }

    protected override GameObject Construct()
    {
        return new Layout
            {
                Children =
                {
                    new Label { Text = "Loading Results...", Alignment = TextAlignmentOptions.Center }.AsFlexItem().Bind(ref _loadingLabel),
                    new Layout()
                        {
                            Children =
                            {
                                new ScrollArea()
                                    {
                                        ScrollContent = new ListView<YTResult, VideoResultListCell>()
                                            {
                                                Enabled = true, WithinLayoutIfDisabled = true, Items = _results.Value,
                                            }
                                            .AsFlexGroup(direction: FlexDirection.Column, gap: 2f, constrainVertical: false)
                                            .AsFlexItem(1)
                                            .Bind(ref _resultsView)
                                    }
                                    .Export(out var scrollArea)
                                    .AsFlexItem(flexGrow: 1),
                                new Scrollbar()
                                    .AsFlexItem()
                                    .With(x => scrollArea.Scrollbar = x)
                            }
                        }
                        .AsFlexGroup(gap: 1f)
                        .AsFlexItem(flexGrow: 1),
                    new Layout
                    {
                        Children =
                        {
                            new BsButton { Text = "Go Back", OnClick = () => _onBack?.Invoke() }.AsFlexItem().Bind(ref _backButton),
                            new BsButton { Text = "Download", OnClick = () => _onDownload?.Invoke() }.AsFlexItem().Bind(ref _downloadButton),
                            new BsButton { Text = "Refine Search", OnClick = () => _onRefine?.Invoke() }.AsFlexItem().Bind(ref _refineButton)
                        }
                    }.AsFlexGroup(FlexDirection.Row, gap: new YogaVector(2, 0), justifyContent: Justify.Center).AsFlexItem()
                }
            }
            .AsFlexGroup(FlexDirection.Column, gap: new YogaVector(0, 3))
            .Use();
    }

    public void ResetSearchView()
    {
        _results.Value = (List<YTResult>)new List<YTResult>();
        _selectedIndex = -1;
        SetDownloadInteractable(false);
        SetLoading(false);
    }

    public void SetLoading(bool loading)
    {
        IsLoading = loading;
        if (_loadingLabel != null) _loadingLabel.Enabled = loading;
    }

    public void SetLoadingText(string text)
    {
        if (_loadingLabel != null)
        {
            _loadingLabel.Text = text;
        }
    }

    public void SetNoResultsMessage(string message)
    {
        SetLoading(false);
        SetLoadingText(message);
    }

    public void SetDownloadInteractable(bool interactable)
    {
        if (_downloadButton != null) _downloadButton.Interactable = interactable;
    }

    public void SelectIndex(int idx)
    {
        if (idx < 0 || idx >= _results.Value.Count) _selectedIndex = -1;
        else _selectedIndex = idx;
        SetDownloadInteractable(_selectedIndex != -1);
    }

    public YTResult? GetSelectedResult()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _results.Value.Count) return null;
        return _results.Value[_selectedIndex];
    }

    public void Dispose()
    {
        if (SearchService == null)
        {
            Plugin._log.Error("Search service is not set!");
        }
        else
        {
            SearchService.SearchProgress -= SearchServiceOnSearchProgress;
            SearchService.SearchFinished -= SearchServiceOnSearchFinished;
            if (_searchLoadingCoroutine != null) CoroutineStarter.StopCoroutine(_searchLoadingCoroutine);
            // if (_updateSearchResultsCoroutine != null) coroutineStarter.StopCoroutine(_updateSearchResultsCoroutine);
        }
    }

    // ---- Search handling (root mirrors original coroutines and SearchService events) ----

    public void OnQueryAction(string query)
    {
        // _searchText.Value = query;
        // _activeSection.Value = VideoMenuSection.Results;
        ResetSearchView();
        _searchLoadingCoroutine = CoroutineStarter.StartCoroutine(SearchLoadingCoroutine());
        SearchService.Search(query);
    }

    private IEnumerator SearchLoadingCoroutine()
    {
        int count = 0;
        const string loadingText = "Searching for videos, please wait";
        SetLoading(true);

        while (IsLoading)
        {
            var periods = string.Empty;
            count++;
            for (var i = 0; i < count; i++) periods += ".";
            if (count == 3) count = 0;

            SetLoadingText(loadingText + periods);
            yield return new WaitForSeconds(0.5f);
        }
    }
}