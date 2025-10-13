using BeatSaberTheater;
using BeatSaberTheater.Download;
using BeatSaberTheater.Util;
using HMUI;
using Reactive;
using Reactive.BeatSaber;
using Reactive.BeatSaber.Components;
using Reactive.Components;
using Reactive.Yoga;
using System;
using System.Collections.Generic;
using UnityEngine;

public class VideoResultListCellData
{
    public event Action<VideoResultListCellData, bool>? ButtonSelectedChanged = null!;
    public event Action<string>? UpdateSelectionStateChanged = null!;

    public YTResult Data { get; set; } = null!;
    public bool IsSelected { get; set; }

    public void ChangeSelected(bool selected)
    {
        try
        {
            IsSelected = selected;
            ButtonSelectedChanged?.Invoke(this, selected);
        }
        catch (Exception ex)
        {
            Plugin._log.Error(ex.Message);
        }
    }

    public void UpdateSelectionState(string selectedId)
    {
        try
        {
            UpdateSelectionStateChanged?.Invoke(selectedId);
        }
        catch (Exception ex)
        {
            Plugin._log.Error(ex.Message);
        }
    }
}

namespace BeatSaberTheater.VideoMenu.V2.SearchResults
{
    internal class VideoResultListCell : ListCell<VideoResultListCellData>
    {
        private AeroButton _button = null!;
        private INotifyValueChanged<VideoResultListCellData> _item = Remember<VideoResultListCellData>(null!);
        private ObservableValue<string> _currentlySelectedId = new("");

        public override GameObject Construct()
        {
            _item = ObservableItem;

            var descriptionLabels = new List<ILayoutItem>
            {
                new Label()
                    {
                        RaycastTarget = false,
                        Text = $"{TheaterFileHelpers.FilterEmoji(_item.Value.Data.Author)} | {_item.Value.Data.GetQualityString()}",
                        FontSize = 2.5f,
                        Color = Color.white.ColorWithAlpha(0.75f)
                    }
                    .Animate(_item, (label, data) => label.Text = $"{TheaterFileHelpers.FilterEmoji(data.Data.Author)} | {data.Data.GetQualityString()}"),
                new Label()
                    {
                        RaycastTarget = false,
                        Text = _item.Value.Data.IsStillImage() ? " | " : "",
                        FontSize = 2.5f,
                        Color = Color.white.ColorWithAlpha(0.75f)
                    }
                    .Animate(_item, (label, data) => label.Text = data.Data.IsStillImage() ? " | " : ""),
                new Label()
                    {
                        RaycastTarget = false,
                        Text = _item.Value.Data.IsStillImage() ? " Likely a still image" : "",
                        FontSize = 2.5f,
                        Color = Color.yellow.ColorWithAlpha(0.75f)
                    }
                    .Animate(_item, (label, data) => label.Text = data.Data.IsStillImage() ? " Likely a still image" : ""),
            };

            var obj = new Layout()
                {
                    LayoutModifier = new YogaModifier() { Size = new YogaVector() { x = YogaValue.Stretch, y = 8.pt() } },
                    Children =
                    {
                        new AeroButton()
                            {
                                RaycastTarget = true,
                                Interactable = true,
                                Latching = true,
                                Colors = new SimpleColorSet()
                                {
                                    ActiveColor = BeatSaberStyle.ControlButtonColorSet.ActiveColor,
                                    HoveredColor = BeatSaberStyle.ControlButtonColorSet.HoveredColor,
                                    Color = BeatSaberStyle.ControlButtonColorSet.Color,
                                    NotInteractableColor = BeatSaberStyle.ControlButtonColorSet.NotInteractableColor
                                },
                                GradientColors0 = new SimpleColorSet()
                                {
                                    ActiveColor = BeatSaberStyle.ControlButtonColorSet.ActiveColor,
                                    HoveredColor = BeatSaberStyle.ControlButtonColorSet.HoveredColor,
                                    Color = BeatSaberStyle.ControlButtonColorSet.Color,
                                    NotInteractableColor = BeatSaberStyle.ControlButtonColorSet.NotInteractableColor
                                },
                                OnStateChanged = b =>
                                {
                                    _item.Value.ChangeSelected(b);
                                },
                                LayoutModifier = new YogaModifier()
                                {
                                    PositionType = PositionType.Absolute, Size = new YogaVector() { x = 100.pct(), y = 100.pct(), }
                                }
                            }
                            .Animate(_item, (button, data) =>
                            {
                                button.Active = data.Data.ID == _currentlySelectedId.Value;
                            })
                            .Animate(_currentlySelectedId, (button, data) =>
                            {
                                button.Active = data == ObservableItem.Value.Data.ID;
                            })
                            .Bind(ref _button),
                        new WebImage { PreserveAspect = true, Src = $"https://i.ytimg.com/vi/{_item.Value.Data.ID}/hqdefault.jpg", RaycastTarget = false, }
                            .Animate(_item, (image, data) => image.Src = $"https://i.ytimg.com/vi/{data.Data.ID}/hqdefault.jpg")
                            .AsFlexItem(1, maxSize: new YogaVector() { x = 10 }, size: new YogaVector() { x = 10 }),
                        new Layout()
                            {
                                Children =
                                {
                                    new Label()
                                        {
                                            Text =
                                                $"[{TheaterFileHelpers.SecondsToString(_item.Value.Data.Duration)}] {TheaterFileHelpers.FilterEmoji(_item.Value.Data.Title)}",
                                            FontSize = 3.5f,
                                            RaycastTarget = false,
                                        }
                                        .Animate(_item,
                                            (label, data) =>
                                                label.Text =
                                                    $"[{TheaterFileHelpers.SecondsToString(data.Data.Duration)}] {TheaterFileHelpers.FilterEmoji(data.Data.Title)}"
                                        )
                                        .AsFlexItem(1),
                                    new Layout()
                                        {
                                            Children =
                                            {
                                                descriptionLabels[0].AsFlexItem(0),
                                                descriptionLabels[1].AsFlexItem(0),
                                                descriptionLabels[2].AsFlexItem(0)
                                            }
                                        }
                                        .AsFlexItem(1)
                                        .AsFlexGroup(FlexDirection.Row, Justify.FlexStart, Align.Center)
                                }
                            }
                            .AsFlexItem(1)
                            .AsFlexGroup(FlexDirection.Column, Justify.FlexStart, Align.FlexStart, overflow: Overflow.Hidden)
                    }
                }
                .AsFlexGroup(gap: new YogaVector() { x = 2.pt() }, overflow: Overflow.Hidden)
                .Use();

            _item.Value.UpdateSelectionStateChanged += ItemOnUpdateSelectionStateChanged;
            _button.Image.GradientDirection = ImageView.GradientDirection.Horizontal;
            return obj;
        }

        private void ItemOnUpdateSelectionStateChanged(string obj)
        {
            _currentlySelectedId.Value = obj;
        }
    }
}