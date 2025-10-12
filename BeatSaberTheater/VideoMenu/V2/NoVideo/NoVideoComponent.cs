using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using System;
using TMPro;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2.NoVideo;

internal class NoVideoComponent(Action onSearch, Action onPresetsSearch) : ReactiveComponent
{
    private Label _messageLabel = null!;
    private BsButton _searchButton = null!;
    private BsButton _presetsButton = null!;
    private string _message = "No video configured";

    protected override GameObject Construct()
    {
        return new Layout
            {
                Children =
                {
                    new Label { Text = _message, FontSize = 4f, Alignment = TextAlignmentOptions.Center }.AsFlexItem().Bind(ref _messageLabel),
                    new Layout()
                        {
                            Children =
                            {
                                new BsButton { Text = "Search", OnClick = () => onSearch?.Invoke() }
                                    .AsFlexItem()
                                    .Bind(ref _searchButton),
                                new BsButton { Text = "Browse Presets", OnClick = () => onPresetsSearch?.Invoke() }
                                    .AsFlexItem()
                                    .Bind(ref _presetsButton),
                            }
                        }.AsFlexItem()
                        .AsFlexGroup(FlexDirection.Row, gap: new YogaVector() { x = 2.pt() }, constrainVertical: false),
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