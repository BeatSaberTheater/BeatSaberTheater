using System;
using BeatSaberTheater.Screen;
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
    [UsedImplicitly]
    public static void Postfix(SongPreviewPlayer __instance, AudioClip audioClip, float startTime, bool isDefault)
    {
        try
        {
            SongPreviewPlayerUpdater.SetFields(__instance._audioSourceControllers, __instance._channelsCount,
                __instance._activeChannel, audioClip, startTime, __instance._timeToDefaultAudioTransition, isDefault);
        }
        catch (Exception e)
        {
            Plugin._log.Error(e);
        }
    }
}