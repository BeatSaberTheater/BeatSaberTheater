using System;
using HarmonyLib;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace BeatSaberTheater.Harmony.Patches;

[HarmonyBefore("com.noodle.BeatSaber.ChromaCore", "com.noodle.BeatSaber.Chroma")]
[HarmonyPatch(typeof(LightSwitchEventEffect), nameof(LightSwitchEventEffect.Start))]
[UsedImplicitly]
internal static class LightSwitchEventEffectStart
{
    public static Action? DelayPlaybackStart;

    [UsedImplicitly]
    private static void Prefix(LightSwitchEventEffect __instance)
    {
        DelayPlaybackStart?.Invoke();
    }
}