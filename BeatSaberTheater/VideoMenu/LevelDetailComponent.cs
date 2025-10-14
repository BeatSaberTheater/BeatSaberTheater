using System;
using BeatSaberTheater.Util.ReactiveUi;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu;

public class LevelDetailComponent(Transform parent, Action ButtonPressed) : ReactiveComponent
{
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
        return new Layout()
        {
            WithinLayoutIfDisabled = true,
            LayoutModifier = new YogaModifier() { Margin = new YogaFrame() { left = 0.pt(), right = 0.pt() } },
            Children =
                {
                    new BsButton()
                        {
                            WithinLayoutIfDisabled = false,
                            Skew = 0,
                            Text = "Video Options",
                            OnClick = () =>
                            {
                                ButtonPressed.Invoke();
                            }
                        }
                        .AsFlexItem(1)
                        .Bind(ref _button)
                }
        }
            .AsFlexGroup(FlexDirection.Row, Justify.SpaceAround, constrainVertical: false, padding: new YogaFrame(0, YogaValue.Point(2)))
            // .AsBeatSaberBackground()
            .Use(parent);
    }
}