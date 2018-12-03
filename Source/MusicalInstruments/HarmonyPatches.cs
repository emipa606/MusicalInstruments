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
                __result += String.Format("\n   {0} ({1})", label, exampleInstrument.def.label);

        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor", new Type[] { typeof(Pawn), typeof(PawnGenerationRequest) })]
    class PatchGenerateGearFor
    {
        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            if (pawn.NonHumanlikeOrWildMan()) return;

            try
            {
#if DEBUG

                Verse.Log.Message(String.Format("Trying to generate an instrument for {0}", pawn.Label));
                Verse.Log.Message(String.Format("Humanlike? {0}", !pawn.NonHumanlikeOrWildMan() ? "YES" : "NO"));
                Verse.Log.Message(String.Format("Non-player? {0}", request.Context == PawnGenerationContext.NonPlayer ? "YES" : "NO"));
                Verse.Log.Message(String.Format("Non-hostile? {0}", request.Faction == null || request.Faction.PlayerRelationKind != FactionRelationKind.Hostile ? "YES" : "NO"));

#endif

                if (request.Context == PawnGenerationContext.NonPlayer && request.Faction == null || request.Faction.PlayerRelationKind != FactionRelationKind.Hostile)
                {
                    int artLevel = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
                    bool neolithic = request.Faction != null && request.Faction.def.techLevel <= TechLevel.Neolithic;
                    bool spacer = request.Faction != null && request.Faction.def.techLevel >= TechLevel.Spacer;

#if DEBUG

                    Verse.Log.Message(String.Format("Art level = {0}", artLevel));
                    Verse.Log.Message(String.Format("Neolithic? {0}", neolithic ? "YES" : "NO"));
                    Verse.Log.Message(String.Format("Spacer? {0}", spacer ? "YES" : "NO"));

#endif

                    if (artLevel > 12 || (artLevel > 8 && Verse.Rand.Chance(.75f)))
                    {
#if DEBUG
                        Verse.Log.Message(">>>Giving hard instrument");
#endif

                        Thing instrument = ThingMaker.MakeThing(ThingDef.Named(neolithic ? "Ocarina" : "Violin"), ThingDef.Named(neolithic ? "Jade" : spacer ? "Plasteel" : "WoodLog"));
                        pawn.inventory.TryAddItemNotForSale(instrument);
                    }
                    else if (artLevel > 4 && Verse.Rand.Chance(.75f))
                    {
#if DEBUG
                        Verse.Log.Message(">>>Giving easy instrument");
#endif

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
