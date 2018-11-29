using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Verse;
using Verse.AI;

using RimWorld;

using Harmony;
using System.Reflection;


namespace MusicalInstruments
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.dogproblems.rimworldmods.musicalinstruments");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(JoyUtility), "JoyKindsOnMapTempList", new Type[] { typeof(Map) })]
    class PatchJoyKindsOnMapTempList
    {
        static void Prefix(Map map, ref List<JoyKindDef> ___tempKindList)
        {
            PerformanceManager pm = map.GetComponent<PerformanceManager>();

            if (pm.MusicJoyKindAvailable())
                ___tempKindList.Add(JoyKindDefOf_Music.Music);
        }
    }

    [HarmonyPatch(typeof(JoyUtility), "JoyKindsOnMapString", new Type[] { typeof(Map) })]
    class PatchJoyKindsOnMapString
    {
        static void Postfix(ref string __result, Map map)
        {
            PerformanceManager pm = map.GetComponent<PerformanceManager>();

            if (pm.MusicJoyKindAvailable())
                __result += String.Format("\n   {0}", JoyKindDefOf_Music.Music.LabelCap);

        }
    }
}
