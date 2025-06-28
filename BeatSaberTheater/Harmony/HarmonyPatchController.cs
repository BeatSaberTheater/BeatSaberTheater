using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BeatSaberTheater.Harmony;

public class HarmonyPatchController
{
    private List<PatchClassProcessor>? _patchClassProcessorList;
    private HarmonyLib.Harmony _harmonyInstance = null!;
    private const string HARMONY_ID = "com.github.kevga.cinema";

    private void InitPatches()
    {
        _harmonyInstance = new HarmonyLib.Harmony(HARMONY_ID);

        _patchClassProcessorList = new List<PatchClassProcessor>();
        AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()).Do<Type>(type =>
            {
                if (type.FullName?.StartsWith("BeatSaberTheater.Harmony.Patches") ?? false)
                    _patchClassProcessorList.Add(_harmonyInstance.CreateClassProcessor(type));
            }
        );
    }

    internal void PatchAll()
    {
        InitPatches();

        _patchClassProcessorList?.ForEach(patchClassProcessor =>
        {
            try
            {
                patchClassProcessor.Patch();
            }
            catch (Exception e)
            {
                Plugin._log.Error(e);
            }
        });
    }

    internal void UnpatchAll()
    {
        _harmonyInstance.UnpatchSelf();
    }
}