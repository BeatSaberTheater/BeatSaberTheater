using BeatSaberTheater.Download;
using BeatSaberTheater.Services;
using BeatSaberTheater.Util;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Components;
using Reactive.Yoga;
using System;
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
    private ListView<VideoResultListCellData, VideoResultListCell> _resultsView = null!;

    private readonly Action _onSelect;

    // internal result store
    private readonly ObservableValue<IReadOnlyList<VideoResultListCellData>> _results =
        Remember<IReadOnlyList<VideoResultListCellData>>(new List<VideoResultListCellData>());

    private readonly ObservableValue<bool> _isLoading = Remember(false);

    private int _selectedIndex = -1;
    public BeatmapLevel CurrentLevel { get; set; } = null!;
    public SearchService SearchService { get; set; } = null!;
    public TheaterCoroutineStarter CoroutineStarter { get; set; } = null!;

    private readonly Action _onBack;
    private readonly Action _onDownload;
    private readonly Action _onRefine;
    // private Coroutine? _updateSearchResultsCoroutine;

    private ObservableValue<VideoResultListCellData?> _currentlySelectedSearchResult = Remember<VideoResultListCellData?>(null!);

    public VideoSearchResultsComponent(Action onSelect, Action onBack, Action onDownload, Action onRefine)
    {
        _onBack = onBack;
        _onDownload = onDownload;
        _onRefine = onRefine;
        _onSelect = onSelect;
    }

    protected override void OnStart()
    {
        SearchService.SearchProgress += SearchServiceOnSearchProgress;
        SearchService.SearchFinished += SearchServiceOnSearchFinished;

        OnQueryAction($"{CurrentLevel.songAuthorName} {CurrentLevel.songName}");
    }

    private void SearchServiceOnSearchFinished()
    {
        SetLoading(false);
    }

    private void SearchServiceOnSearchProgress(YTResult obj)
    {
        // Todo: add beatmap details to YTResult so we can filter unwanted search results here
        // mayhaps even tag YTResults with a specific search request id
        Plugin._log.Debug($"Search progress: {obj.ToString()}");

        if (!_results.Value.Any(x => x.Data.ID == obj.ID))
        {
            // Todo: prevent expensive list copying
            VideoResultListCellData videoResultListCellData = new VideoResultListCellData() { Data = obj, IsSelected = false, };
            _results.Value = _results.Value.Append(videoResultListCellData).ToList();
            videoResultListCellData.ButtonSelectedChanged += VideoResultListCellDataOnButtonSelectedChanged;
        }
    }

    private void VideoResultListCellDataOnButtonSelectedChanged(VideoResultListCellData videoResultListCellData, bool b)
    {
        if (b)
        {
            _currentlySelectedSearchResult.Value = videoResultListCellData;
            // Deselect everything but the thing that was just enabled
            foreach (var item in _results.Value)
            {
                item.UpdateSelectionState(b ? videoResultListCellData.Data.ID : "");
            }
        }

        if (!b && videoResultListCellData.Data.ID == _currentlySelectedSearchResult.Value?.Data.ID)
        {
            _currentlySelectedSearchResult.Value = null!;
            
            foreach (var item in _results.Value)
            {
                item.UpdateSelectionState("");
            }
        }
    }

    protected override GameObject Construct()
    {
        return new Layout
            {
                Children =
                {
                    new Layout()
                        {
                            Children =
                            {
                                new Spinner()
                                    {
                                        Enabled = _isLoading,
                                        LayoutModifier = new YogaModifier() { Size = new YogaVector() { x = 3.pt(), y = 3.pt() } }
                                    }
                                    .Animate(_isLoading, (spinner, b) => spinner.Enabled = b)
                                    .AsFlexItem(alignSelf: Align.Center),
                                new Label { Text = "Loading Results...", Alignment = TextAlignmentOptions.Center }.AsFlexItem().Bind(ref _loadingLabel),
                            }
                        }.AsFlexGroup(FlexDirection.Row, Justify.FlexStart, alignContent: Align.Center, gap: 2f)
                        .AsFlexItem(),
                    new Layout()
                        {
                            Children =
                            {
                                new ScrollArea()
                                    {
                                        HideScrollbarWhenNothingToScroll = true,
                                        ScrollContent = new ListView<VideoResultListCellData, VideoResultListCell>()
                                            {
                                                Enabled = true, WithinLayoutIfDisabled = true, Items = _results.Value,
                                            }
                                            .Animate(_results, (view, list) =>
                                            {
                                                view.Items = list;
                                            })
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
        _results.Value = new List<VideoResultListCellData>();
        _selectedIndex = -1;
        SetDownloadInteractable(false);
        SetLoading(false);
    }

    public void SetLoading(bool loading)
    {
        _isLoading.Value = loading;
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
        return _results.Value[_selectedIndex].Data;
    }

    public void Dispose()
    {
        SearchService.SearchProgress -= SearchServiceOnSearchProgress;
        SearchService.SearchFinished -= SearchServiceOnSearchFinished;
        // if (_updateSearchResultsCoroutine != null) coroutineStarter.StopCoroutine(_updateSearchResultsCoroutine);
    }

    // ---- Search handling (root mirrors original coroutines and SearchService events) ----
    public void OnQueryAction(string query)
    {
        // _searchText.Value = query;
        // _activeSection.Value = VideoMenuSection.Results;
        ResetSearchView();
        SetLoading(true);
        SearchService.Search(query);
    }
}