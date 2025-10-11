using System;
using BeatSaberTheater.Util.ReactiveUi;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu;

public class LevelDetailComponent(Transform parent) : ReactiveComponent
{
    public event Action? ButtonPressed;
    private Label _label = null!;
    private BsButton _button = null!;

    public void SetLabelText(string text, Color color, bool active)
    {
        _label.Text = text;
        _label.Enabled = active;
        _label.Color = color.ColorWithAlpha(0.75f);
    }

    public void SetButtonState(string text, Color color, bool active)
    {
        _button.Interactable = active;
        _button._underline.Color = color;
        _button.Enabled = active;
        _button.Text = text;
    }

    protected override GameObject Construct()
    {
        return new Background()
            {
                WithinLayoutIfDisabled = true,
                LayoutModifier = new YogaModifier() { Margin = new YogaFrame() { left = 2.pt(), right = 2.pt() } },
                Children =
                {
                    new Label() { WithinLayoutIfDisabled = false, Text = "Video available", Color = Color.white.ColorWithAlpha(0.75f) }
                        .AsFlexItem(flex: 0)
                        .Bind(ref _label),
                    new BsButton()
                        {
                            WithinLayoutIfDisabled = false,
                            Text = "Download",
                            OnClick = () =>
                            {
                                Plugin._log.Info("Downloading video...");
                                ButtonPressed?.Invoke();
                            }
                        }
                        .AsFlexItem(0)
                        .Bind(ref _button)
                }
            }
            .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false, padding: new YogaFrame(0, YogaValue.Point(2)))
            .AsBeatSaberBackground()
            .Use(parent);
    }
}