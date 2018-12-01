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

            string label = JoyKindDefOf_Music.Music.LabelCap;

            //yuck
            if (!__result.Contains(label) && pm.MusicJoyKindAvailable())
                __result += String.Format("\n   {0}", label);

        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor", new Type[] { typeof(Pawn), typeof(PawnGenerationRequest) })]
    class PatchGenerateGearFor
    {
        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            if (request.Context == PawnGenerationContext.NonPlayer && !request.KindDef.isFighter && request.Faction.PlayerRelationKind != FactionRelationKind.Hostile)
            {
                int artLevel = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
                bool neolithic = request.Faction.def.techLevel <= TechLevel.Neolithic;
                bool spacer = request.Faction.def.techLevel >= TechLevel.Spacer;

                if (pawn.skills.GetSkill(SkillDefOf.Artistic).Level > 12 && Verse.Rand.Chance(.3f))
                {
                    Thing instrument = ThingMaker.MakeThing(ThingDef.Named(neolithic ? "Ocarina" : "Violin"), ThingDef.Named(neolithic ? "Jade" : spacer ? "Plasteel" : "WoodLog"));
                    pawn.inventory.TryAddItemNotForSale(instrument);
                }
                else if (pawn.skills.GetSkill(SkillDefOf.Artistic).Level > 6 && Verse.Rand.Chance(.3f))
                {
                    Thing instrument = ThingMaker.MakeThing(ThingDef.Named(neolithic ? "FrameDrum" : "Guitar"), ThingDef.Named(neolithic ? "Leather_Light" : spacer ? "Plasteel" : "WoodLog"));
                    pawn.inventory.TryAddItemNotForSale(instrument);
                }
            }
        }
    }
}
