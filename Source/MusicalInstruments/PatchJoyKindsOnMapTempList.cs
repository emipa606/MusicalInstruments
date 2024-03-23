using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MusicalInstruments;

[HarmonyPatch(typeof(JoyUtility), "JoyKindsOnMapTempList", typeof(Map))]
internal class PatchJoyKindsOnMapTempList
{
    private static void Prefix(Map map, ref List<JoyKindDef> ___tempKindList)
    {
        var pm = map.GetComponent<PerformanceManager>();
        if (pm.MusicJoyKindAvailable(out _))
        {
            ___tempKindList.Add(JoyKindDefOf_Music.Music);
        }
    }
}