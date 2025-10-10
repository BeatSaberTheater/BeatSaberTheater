using System.Collections;
using JetBrains.Annotations;
using Reactive;
using Reactive.BeatSaber;
using Reactive.BeatSaber.Components;
using Reactive.Components;
using Reactive.Yoga;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberTheater.Components
{
    [PublicAPI]
    public class TextArea : ReactiveComponent
    {
        #region UI Props

        public string Text
        {
            get => _text;
            set
            {
                SetTextSilent(value);
                NotifyPropertyChanged();
            }
        }

        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value;
                RefreshText();
                NotifyPropertyChanged();
            }
        }

        public Sprite? Icon
        {
            get => _icon.Sprite;
            set
            {
                _icon.Enabled = value != null;
                _icon.Sprite = value;
            }
        }

        public bool ShowClearButton
        {
            get => _showClearButton;
            set
            {
                _showClearButton = value;
                RefreshClearButton();
            }
        }

        public bool ShowCaret
        {
            get => _showCaret;
            set
            {
                _showCaret = value;
                RefreshCaret();
            }
        }

        public bool RaycastTarget
        {
            get => _backgroundButton.RaycastTarget;
            set => _backgroundButton.RaycastTarget = value;
        }

        private string _text = string.Empty;
        private string _placeholder = "Enter";
        private bool _showClearButton = true;
        private bool _showCaret;

        public void SetTextSilent(string text)
        {
            _text = text;
            RefreshText();
            RefreshClearButton();
        }

        #endregion

        #region Setup

        public bool Focused
        {
            get => _focused;
            set => SetInputEnabled(value);
        }

        private static readonly Color placeholderColor = BeatSaberStyle.InactiveTextColor;
        private static readonly Color textColor = Color.white;

        private bool _focused;

        private void RefreshText()
        {
            var showPlaceholder = Text.Length == 0;
            _label.Text = showPlaceholder ? _placeholder : Text;
            _label.Color = showPlaceholder ? placeholderColor : textColor;
            RefreshCaretPos();
        }

        private void RefreshClearButton()
        {
            _clearButton.Enabled = _showClearButton && !_focused && Text.Length > 0;
        }

        private void SetInputEnabled(bool enabled)
        {
            _focused = enabled;
            RefreshCaret();
            RefreshClearButton();
            NotifyPropertyChanged(nameof(Focused));
        }

        #endregion

        #region Caret

        private void RefreshCaret()
        {
            _caret.Enabled = _focused && ShowCaret;
            if (!ShowCaret) return;
            if (_focused)
            {
                StartCoroutine(CaretAnimationCoroutine());
            }
            else
            {
                StopAllCoroutines();
            }
        }

        private void RefreshCaretPos()
        {
            var transform = _caret.ContentTransform;
            var pos = transform.anchoredPosition;

            // item is Label here
            var measuredSize = _label.Measure(0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined);

            pos.x = Text.Length == 0 ? 0 : measuredSize.x;
            transform.anchoredPosition = pos;
        }

        private IEnumerator CaretAnimationCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.4f);
                _caret.Enabled = !_caret.Enabled;
            }
            // ReSharper disable once IteratorNeverReturns
        }

        #endregion

        #region Construct

        private ButtonBase _clearButton = null!;
        private ButtonBase _backgroundButton = null!;
        private Label _label = null!;
        private Reactive.BeatSaber.Components.Image _icon = null!;
        private Reactive.BeatSaber.Components.Image _caret = null!;

        protected override GameObject Construct()
        {
            var labelColor = Remember(placeholderColor);

            return new AeroButtonLayout
            {
                OnClick = () => SetInputEnabled(true),
                Children = {
                    //icon
                    new Reactive.BeatSaber.Components.Image {
                        Sprite = null,
                        PreserveAspect = true,
                        Skew = BeatSaberStyle.Skew,
                        Color = BeatSaberStyle.SecondaryTextColor
                    }.AsFlexItem(
                        size: new() { x = 4f },
                        margin: new() { left = 1f }
                    ).Bind(ref _icon),

                    // Wrapping in an absolute container to make overflowed text go back
                    new Layout {
                        Children = {
                            // Text
                            new Label {
                                Alignment = TextAlignmentOptions.CaplineRight,
                                Overflow = TextOverflowModes.Overflow,
                                FontStyle = FontStyles.Italic,
                                Color = BeatSaberStyle.InactiveTextColor,
                                FontSize = 4f,
                            }.With(x => {
                                    // Caret
                                    new Reactive.BeatSaber.Components.Image {
                                        ContentTransform = {
                                            anchorMin = new(0f, 0.5f),
                                            anchorMax = new(0f, 0.5f),
                                            sizeDelta = new() { x = 0.6f, y = 4f },
                                            pivot = new(0f, 0.5f)
                                        },

                                        Sprite = GameResources.CaretIcon,
                                        Skew = BeatSaberStyle.Skew,
                                        Enabled = false
                                    }.Bind(ref _caret).Use(x.Content);
                                }
                            ).AsFlexItem(margin: new() { right = 0.8f, left = 0.8f }).Bind(ref _label)
                        }
                    }.AsFlexGroup().AsFlexItem(flexGrow: 1f, flexShrink: 1f).WithNativeComponent(out RectMask2D _),

                    // Clear button
                    new BackgroundButton {
                        Enabled = false,

                        Image = {
                            Sprite = BeatSaberResources.Sprites.background,
                            Material = GameResources.UINoGlowMaterial
                        },
                        Colors = new SimpleColorSet {
                            Color = Color.black.ColorWithAlpha(0.5f),
                            HoveredColor = Color.black.ColorWithAlpha(0.8f)
                        },
                        OnClick = HandleClearButtonClicked,

                        Children = {
                            new Reactive.BeatSaber.Components.Image {
                                Sprite = BeatSaberResources.Sprites.crossIcon
                            }.AsFlexItem(flexGrow: 1f)
                        }
                    }.AsFlexItem(
                        size: new() { x = 4f },
                        aspectRatio: 1f,
                        alignSelf: Align.Center,
                        margin: new() { right = 1f }
                    ).AsFlexGroup(padding: 0.7f).Bind(ref _clearButton),
                }
            }.AsFlexGroup(padding: 1f, gap: 1f).WithListener(
                x => x.IsHovered,
                x => labelColor.Value = !x ? placeholderColor : placeholderColor.ColorWithAlpha(0.5f)
            ).Animate(
                labelColor,
                (_, y) => _label.Color = Text.Length > 0 ? textColor : y
            ).Bind(ref _backgroundButton).Use();
        }

        #endregion

        #region Callbacks

        private void HandleClearButtonClicked()
        {
            Text = string.Empty;
            RefreshText();
            RefreshClearButton();
        }

        #endregion
    }
}
