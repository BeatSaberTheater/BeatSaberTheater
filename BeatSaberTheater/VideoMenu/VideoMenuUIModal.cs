using BeatSaberTheater.VideoMenu.V2;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Components;
using Reactive.Yoga;
using System;
using TMPro;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu
{
    internal class VideoMenuUIModal(VideoMenuComponent component) : ModalBase, IComponentHolder<VideoMenuComponent>
    {
#pragma warning disable CS0108, CS0114
        public VideoMenuComponent Component { get; } = component;
#pragma warning restore CS0108, CS0114

        public event Action? OnSave;

        private BsButton _backButton = null!;
        private BsButton _saveButton = null!;

        protected override GameObject Construct()
        {
            var obj = new Background()
                {
                    LayoutModifier = new YogaModifier() { Size = new YogaVector() { x = YogaValue.Percent(100), y = YogaValue.Percent(100), } },
                    Children =
                    {
                        new Layout()
                            {
                                Children =
                                {
                                    new Layout()
                                        {
                                            Children =
                                            {
                                                new Layout()
                                                    {
                                                        Children =
                                                        {
                                                            new BsButton()
                                                            {
                                                                Text = "Back",
                                                                Skew = 0,
                                                                OnClick = () =>
                                                                {
                                                                    Close(false);
                                                                }
                                                            }.Bind(ref _backButton)
                                                        }
                                                    }
                                                    .AsFlexItem(flex: 1),
                                                new Label() { Text = "Theater", Alignment = TextAlignmentOptions.Center }.AsFlexItem(flex: 1),
                                                new Layout()
                                                {
                                                    Children =
                                                    {
                                                        new BsButton()
                                                        {
                                                            Text = "Save",
                                                            Skew = 0,
                                                            OnClick = () =>
                                                            {
                                                                OnSave?.Invoke();
                                                                Close(true);
                                                            }
                                                        }.Bind(ref _saveButton)
                                                    }
                                                }.AsFlexItem(flex: 1)
                                            }
                                        }
                                        .AsFlexGroup()
                                        .AsFlexItem(maxSize: new YogaVector() { y = 10f }, flex: 1),
                                    new Layout() { Children = { Component.AsFlexItem(flex: 1), } }.AsFlexGroup(padding: new YogaFrame(4.pt()))
                                        .AsFlexItem(flex: 1)
                                }
                            }.AsFlexGroup(FlexDirection.Column)
                            .AsFlexItem(1)
                    }
                }
                .AsFlexGroup()
                // Todo: figure out how to add like a background blur shader effect? Would be cool
                .AsBlurBackground(12F, color: ColorUtility.TryParseHtmlString("#FFFFFFFE", out var c) ? c : new Color(0, 0, 0, 0.0f))
                .Use();

            obj.layer = 5;
            _saveButton._underline.Color = Color.green;
            return obj;
        }
    }
}