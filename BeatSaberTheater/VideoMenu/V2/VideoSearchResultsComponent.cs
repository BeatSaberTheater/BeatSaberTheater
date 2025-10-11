using System;
using System.Collections.Generic;
using BeatSaberTheater.Download;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using TMPro;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2;

internal class VideoSearchResultsComponent(Action onSelect, Action onBack, Action onDownload, Action onRefine) : ReactiveComponent
{
    private Label _loadingLabel = null!;
    private BsButton _downloadButton = null!;
    private BsButton _refineButton = null!;
    private BsButton _backButton = null!;

    private readonly Action _onSelect = onSelect;
    private readonly Action _onBack = onBack;
    private readonly Action _onDownload = onDownload;
    private readonly Action _onRefine = onRefine;

    // internal result store
    private readonly List<YTResult> _results = new();
    private int _selectedIndex = -1;
    public bool IsLoading { get; private set; }

    protected override GameObject Construct()
    {
        return new Layout
        {
            Children =
            {
                new Label { Text = "Loading Results...", Alignment = TextAlignmentOptions.Center }.AsFlexItem().Bind(ref _loadingLabel),

                // result area - simplified placeholder; you can replace with a proper list later
                new Layout
                {
                    Children =
                    {
                        new Label { Text = "Results will appear here" }.AsFlexItem()
                    }
                }.AsFlexGroup(FlexDirection.Column).AsFlexItem(flex: 1),

                new Layout
                {
                    Children =
                    {
                        new BsButton { Text = "Go Back", OnClick = () => _onBack?.Invoke() }.AsFlexItem().Bind(ref _backButton),
                        new BsButton { Text = "Download", OnClick = () => _onDownload?.Invoke() }.AsFlexItem().Bind(ref _downloadButton),
                        new BsButton { Text = "Refine Search", OnClick = () => _onRefine?.Invoke() }.AsFlexItem().Bind(ref _refineButton)
                    }
                }.AsFlexGroup(FlexDirection.Row, gap: new YogaVector(2,0), justifyContent: Justify.Center).AsFlexItem()
            }
        }
        .AsFlexGroup(FlexDirection.Column, gap: new YogaVector(0, 3))
        .Use();
    }

    public void AddSearchResult(YTResult result, string title, string description, Sprite? sprite)
    {
        _results.Add(result);
        // In a fuller implementation you would add an interactive cell to a UITable or BsList.
        // For now we simply log and allow selection by index through SelectIndex.
        Plugin._log.Debug($"Search result added: {title}");
    }

    public void ResetSearchView()
    {
        _results.Clear();
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
        if (idx < 0 || idx >= _results.Count) _selectedIndex = -1;
        else _selectedIndex = idx;
        SetDownloadInteractable(_selectedIndex != -1);
    }

    public YTResult? GetSelectedResult()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _results.Count) return null;
        return _results[_selectedIndex];
    }
}
