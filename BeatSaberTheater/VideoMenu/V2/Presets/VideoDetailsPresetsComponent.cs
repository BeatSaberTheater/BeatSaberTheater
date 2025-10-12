using Reactive;
using Reactive.BeatSaber.Components;
using Reactive.Yoga;
using UnityEngine;

namespace BeatSaberTheater.VideoMenu.V2.Presets
{
    internal class VideoDetailsPresetsComponent : ReactiveComponent
    {
        protected override GameObject Construct()
        {
            return new Layout()
                {
                    Children = { new Label()
                    {
                        Text = "Work in Progress :-)"
                    } }
                }.AsFlexGroup(alignItems: Align.Center, justifyContent: Justify.Center)
                .Use();
        }
    }
}