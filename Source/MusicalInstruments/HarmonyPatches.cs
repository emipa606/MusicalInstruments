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

            Thing exampleInstrument;

            if (pm.MusicJoyKindAvailable(out exampleInstrument))
                ___tempKindList.Add(JoyKindDefOf_Music.Music);
        }
    }

    [HarmonyPatch(typeof(JoyUtility), "JoyKindsOnMapString", new Type[] { typeof(Map) })]
    class PatchJoyKindsOnMapString
    {
        static void Postfix(ref string __result, Map map)
        {
            PerformanceManager pm = map.GetComponent<PerformanceManager>();

            string label = JoyKindDefOf_Music.Music.LabelCap;

            Thing exampleInstrument;

            //yuck
            if (!__result.Contains(label) && pm.MusicJoyKindAvailable(out exampleInstrument))
                __result += String.Format("\n   {0} (1)", label, exampleInstrument.LabelCap);

        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor", new Type[] { typeof(Pawn), typeof(PawnGenerationRequest) })]
    class PatchGenerateGearFor
    {
        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            try
            {
                if (request.Context == PawnGenerationContext.NonPlayer && !request.KindDef.isFighter && request.Faction != null && request.Faction.PlayerRelationKind != FactionRelationKind.Hostile)
                {
                    int artLevel = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
                    bool neolithic = request.Faction != null && request.Faction.def.techLevel <= TechLevel.Neolithic;
                    bool spacer = request.Faction != null && request.Faction.def.techLevel >= TechLevel.Spacer;

                    if (pawn.skills.GetSkill(SkillDefOf.Artistic).Level > 10 && Verse.Rand.Chance(.5f))
                    {
                        Thing instrument = ThingMaker.MakeThing(ThingDef.Named(neolithic ? "Ocarina" : "Violin"), ThingDef.Named(neolithic ? "Jade" : spacer ? "Plasteel" : "WoodLog"));
                        pawn.inventory.TryAddItemNotForSale(instrument);
                    }
                    else if (pawn.skills.GetSkill(SkillDefOf.Artistic).Level > 5 && Verse.Rand.Chance(.5f))
                    {
                        Thing instrument = ThingMaker.MakeThing(ThingDef.Named(neolithic ? "FrameDrum" : "Guitar"), ThingDef.Named(neolithic ? "Leather_Light" : spacer ? "Plasteel" : "WoodLog"));
                        pawn.inventory.TryAddItemNotForSale(instrument);
                    }
                }
            }
            catch
            {
                Verse.Log.Warning(String.Format("Failed to generate instrument for {0}", pawn == null ? "NULL" : pawn.LabelShort));
            }
        }
    }
}
