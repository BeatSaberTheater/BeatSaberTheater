using System;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using TMPro;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2;

internal class NoVideoComponent(Action onSearch) : ReactiveComponent
{
    private Label _messageLabel = null!;
    private BsButton _searchButton = null!;
    private string _message = "No video configured";

    private readonly Action _onSearch = onSearch;

    protected override GameObject Construct()
    {
        return new Layout
        {
            Children =
            {
                new Label { Text = _message, FontSize = 4f, Alignment = TextAlignmentOptions.Center }.AsFlexItem().Bind(ref _messageLabel),
                new BsButton { Text = "Search", OnClick = () => _onSearch?.Invoke() }.AsFlexItem().Bind(ref _searchButton)
            }
        }
        .AsFlexGroup(FlexDirection.Column, justifyContent: Justify.Center)
        .Use();
    }

    public void SetMessage(string message)
    {
        _message = message;
        if (_messageLabel != null) _messageLabel.Text = message;
    }

    public void SetSearchButtonActive(bool active)
    {
        if (_searchButton != null) _searchButton.Interactable = active;
    }
}
