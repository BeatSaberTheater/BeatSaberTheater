using BeatSaberTheater.Download;
using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Components;
using Reactive.Yoga;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2
{
    internal class VideoResultListCell : ListCell<YTResult>
    {
        // Todo: make me fancy, with picture, and more info
        protected override GameObject Construct()
        {
            return new Layout()
                {
                    LayoutModifier = new YogaModifier() { Size = new YogaVector() { x = YogaValue.Stretch, y = 10.pt() } },
                    Children =
                    {
                        new WebImage { Src = $"https://i.ytimg.com/vi/{Item.ID}/hqdefault.jpg" }.AsFlexItem(0, size: new YogaVector() { x = 20 }),
                        new Label() { Text = Item.Title }
                            .AsFlexItem(1)
                    }
                }
                .AsFlexGroup(gap: new YogaVector() { x = 2.pt() })
                .Use();
        }
    }
}