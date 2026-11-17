using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MusicalInstruments;

[HarmonyPatch(typeof(JoyUtility), nameof(JoyUtility.JoyKindsOnMapTempList), typeof(Map))]
internal static class JoyUtility_JoyKindsOnMapTempList
{
    public static void Prefix(Map map, ref List<JoyKindDef> ___tempKindList)
    {
        var pm = map.GetComponent<PerformanceManager>();
        if (pm.MusicJoyKindAvailable(out _))
        {
            ___tempKindList.Add(JoyKindDefOf_Music.Music);
        }
    }
}