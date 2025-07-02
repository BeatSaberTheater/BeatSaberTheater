using System;
using BeatSaberTheater.Harmony.Signals;
using HarmonyLib;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace BeatSaberTheater.Harmony.Patches;

[HarmonyAfter("com.kyle1413.BeatSaber.SongCore")]
[HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.CheckIfBeatmapLevelDataExists))]
[UsedImplicitly]
public class StandardLevelDetailViewRefreshContent
{
    public static Action<MapRequirementsUpdateSignal>? OnMapRequirementsUpdate;

    [UsedImplicitly]
    private static void Postfix(StandardLevelDetailView __instance)
    {
        try
        {
            OnMapRequirementsUpdate?.Invoke(new MapRequirementsUpdateSignal
            {
                StandardLevelDetailView = __instance
            });
        }
        catch (Exception e)
        {
            Plugin._log.Error(e);
        }
    }
}