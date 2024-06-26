﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace MusicalInstruments;

[HarmonyPatch(typeof(JoyUtility), "JoyKindsOnMapString", typeof(Map))]
internal class PatchJoyKindsOnMapString
{
    private static void Postfix(ref string __result, Map map)
    {
        var pm = map.GetComponent<PerformanceManager>();

        string label = JoyKindDefOf_Music.Music.LabelCap;


        //yuck
        if (!__result.Contains(label) && pm.MusicJoyKindAvailable(out var exampleInstrument))
        {
            __result += $"\n   {label} ({exampleInstrument.def.label})";
        }
    }
}