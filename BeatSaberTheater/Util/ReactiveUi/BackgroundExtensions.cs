using HMUI;
using Reactive.BeatSaber.Components;
using Reactive.Components;
using UnityEngine;

namespace BeatSaberTheater.Util.ReactiveUi;

public static class BackgroundExtensions
{
    public static T AsBeatSaberBackground<T>(
        this T holder)
        where T : IComponentHolder<Image>
    {
        return holder.AsBackground(color: new Color(0, 0, 0), gradientColor0: new Color(255, 255, 255, 0.75f), gradientColor1: new Color(255, 255, 255, 0.5f),
            gradientDirection: ImageView.GradientDirection.Horizontal, type: UnityEngine.UI.Image.Type.Sliced);
    }
}