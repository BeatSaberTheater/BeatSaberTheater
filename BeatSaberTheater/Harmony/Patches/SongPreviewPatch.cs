using System;
using BeatSaberTheater.Harmony.Signals;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace BeatSaberTheater.Harmony.Patches;

[HarmonyPatch(typeof(SongPreviewPlayer), nameof(SongPreviewPlayer.CrossfadeTo), typeof(AudioClip), typeof(float),
    typeof(float), typeof(float), typeof(bool), typeof(Action))]
[UsedImplicitly]
public class SongPreviewPatch
{
    public static Action<SongPreviewPlayerSignal>? OnCrossfade;

    [UsedImplicitly]
    public static void Postfix(SongPreviewPlayer __instance, AudioClip audioClip, float startTime, bool isDefault)
    {
        try
        {
            OnCrossfade?.Invoke(new SongPreviewPlayerSignal
            {
                AudioSourceControllers = __instance._audioSourceControllers,
                ChannelCount = __instance._channelsCount,
                ActiveChannel = __instance._activeChannel,
                AudioClip = audioClip,
                StartTime = startTime,
                TimeToDefault = __instance._timeToDefaultAudioTransition,
                IsDefault = isDefault
            });
        }
        catch (Exception e)
        {
            Plugin._log.Error(e);
        }
    }
}