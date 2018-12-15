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
#if DEBUG
            Verse.Log.Message(String.Format("Trying to generate an instrument for {0}", pawn.Label));

#endif

            if(Current.ProgramState != ProgramState.Playing)
            {
#if DEBUG
                Verse.Log.Message("World generation phase, exit");
#endif
                return;
            }

            if (pawn.NonHumanlikeOrWildMan())
            {
#if DEBUG
                Verse.Log.Message("NonHumanlikeOrWildMan, exit");
#endif
                return;
            }

            if(pawn.Faction == null)
            {
#if DEBUG
                Verse.Log.Message("null faction, exit");
#endif
                return;
            }

            if(pawn.Faction.IsPlayer)
            {
#if DEBUG
                Verse.Log.Message("player faction, exit");
#endif
                return;
            }

            if(pawn.Faction.PlayerRelationKind == FactionRelationKind.Hostile)
            {
#if DEBUG
                Verse.Log.Message("hostile faction, exit");
#endif
                return;
            }

#if DEBUG
            Verse.Log.Message("continuing...");
#endif
            int artLevel = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
            TechLevel techLevel = request.Faction == null ? TechLevel.Neolithic : request.Faction.def.techLevel;
            ThingDef instrumentDef;
            ThingDef stuffDef;
                       
            if (artLevel > 12 || (artLevel > 8 && Verse.Rand.Chance(.75f)))
            {

                if (TryGetHardInstrument(techLevel, out instrumentDef, out stuffDef))
                {
                    Thing instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
                    pawn.inventory.TryAddItemNotForSale(instrument);
                }
            }
            else if (artLevel > 4 && Verse.Rand.Chance(.75f))
            {
                if (TryGetEasyInstrument(techLevel, out instrumentDef, out stuffDef))
                {
                    Thing instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
                    pawn.inventory.TryAddItemNotForSale(instrument);
                }

            }               

        }

        static bool TryGetEasyInstrument(TechLevel techLevel, out ThingDef instrumentDef, out ThingDef stuffDef)
        {
            ThingDef frameDrum = null;
            ThingDef guitar = null;
            ThingDef lightLeather = null;
            ThingDef wood = null;
            ThingDef plasteel = null;

            instrumentDef = null;
            stuffDef = null;

            bool neolithic = techLevel <= TechLevel.Neolithic;
            bool spacer = techLevel >= TechLevel.Spacer;

            try { frameDrum = ThingDef.Named("FrameDrum"); }
            catch { }

            try { guitar = ThingDef.Named("Guitar"); }
            catch { }

            try { lightLeather = ThingDef.Named("Leather_Light"); }
            catch { }

            try { wood = ThingDef.Named("WoodLog"); }
            catch { }

            try { plasteel = ThingDef.Named("Plasteel"); }
            catch { }

            if (guitar != null && !neolithic)
            {
                if (plasteel != null && spacer)
                {
                    instrumentDef = guitar;
                    stuffDef = plasteel;
                    return true;
                }
                else if (wood != null)
                {
                    instrumentDef = guitar;
                    stuffDef = wood;
                    return true;
                }
            }

            if (frameDrum != null && lightLeather != null)
            {
                instrumentDef = frameDrum;
                stuffDef = lightLeather;
                return true;
            }

            return false;
        }

        static bool TryGetHardInstrument(TechLevel techLevel, out ThingDef instrumentDef, out ThingDef stuffDef)
        {
            ThingDef ocarina = null;
            ThingDef violin = null;
            ThingDef jade = null;
            ThingDef wood = null;
            ThingDef plasteel = null;

            instrumentDef = null;
            stuffDef = null;

            bool neolithic = techLevel <= TechLevel.Neolithic;
            bool spacer = techLevel >= TechLevel.Spacer;

            try { ocarina = ThingDef.Named("Ocarina"); }
            catch { }

            try { violin = ThingDef.Named("Violin"); }
            catch { }

            try { jade = ThingDef.Named("Jade"); }
            catch { }

            try { wood = ThingDef.Named("WoodLog"); }
            catch { }

            try { plasteel = ThingDef.Named("Plasteel"); }
            catch { }

            if (violin != null && !neolithic)
            {
                if (plasteel != null && spacer)
                {
                    instrumentDef = violin;
                    stuffDef = plasteel;
                    return true;
                }
                else if (wood != null)
                {
                    instrumentDef = violin;
                    stuffDef = wood;
                    return true;
                }
            }

            if (ocarina != null && jade != null)
            {
                instrumentDef = ocarina;
                stuffDef = jade;
                return true;
            }

            return false;
        }

    }
}
