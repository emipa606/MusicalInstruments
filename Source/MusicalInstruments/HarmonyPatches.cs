using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Verse;
using Verse.AI;

using RimWorld;
using RimWorld.Planet;

using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;


namespace MusicalInstruments
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("com.dogproblems.rimworldmods.musicalinstruments");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    //patch for warning if too few joy types
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

    //patch for listing available joy types
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

    //patch to keep one instrument in colonist inventory on return from caravan
    [HarmonyPatch(typeof(Pawn_InventoryTracker), "get_FirstUnloadableThing")]
    class PatchFirstUnloadableThing
    {
        private static List<ThingDefCount> tmpDrugsToKeep = new List<ThingDefCount>();

        static bool Prefix(Pawn_InventoryTracker __instance, ref ThingCount __result)
        {
            if (__instance.innerContainer.Count == 0)
            {
                __result = default(ThingCount);
                return false;
            }

            tmpDrugsToKeep.Clear();

            if (__instance.pawn.drugs != null && __instance.pawn.drugs.CurrentPolicy != null)
            {
                DrugPolicy currentPolicy = __instance.pawn.drugs.CurrentPolicy;
                for (int i = 0; i < currentPolicy.Count; i++)
                {
                    if (currentPolicy[i].takeToInventory > 0)
                    {
                        tmpDrugsToKeep.Add(new ThingDefCount(currentPolicy[i].drug, currentPolicy[i].takeToInventory));
                    }
                }
            }

            Thing bestInstrument = null;

            if(!__instance.pawn.NonHumanlikeOrWildMan() && !__instance.pawn.WorkTagIsDisabled(WorkTags.Artistic))
            {
                int artSkill = __instance.pawn.skills.GetSkill(SkillDefOf.Artistic).levelInt;

                IEnumerable<Thing> heldInstruments = __instance.innerContainer.Where(x => PerformanceManager.IsInstrument(x))
                                                                .Where(x => !x.TryGetComp<CompMusicalInstrument>().Props.isBuilding)
                                                                .OrderByDescending(x => x.TryGetComp<CompMusicalInstrument>().WeightedSuitability(artSkill));

                if(heldInstruments.Any())
                    bestInstrument = heldInstruments.FirstOrDefault();
            }

            if (tmpDrugsToKeep.Any() || bestInstrument != null)
            {
                for (int j = 0; j < __instance.innerContainer.Count; j++)
                {
                    if (__instance.innerContainer[j].def.IsDrug)
                    {
                        int num = -1;

                        for (int k = 0; k < tmpDrugsToKeep.Count; k++)
                        {
                            if (__instance.innerContainer[j].def == tmpDrugsToKeep[k].ThingDef)
                            {
                                num = k;
                                break;
                            }
                        }
                        if (num < 0)
                        {
                            __result = new ThingCount(__instance.innerContainer[j], __instance.innerContainer[j].stackCount);
                            return false;
                        }
                        if (__instance.innerContainer[j].stackCount > tmpDrugsToKeep[num].Count)
                        {
                            __result = new ThingCount(__instance.innerContainer[j], __instance.innerContainer[j].stackCount - tmpDrugsToKeep[num].Count);
                            return false;
                        }
                        tmpDrugsToKeep[num] = new ThingDefCount(tmpDrugsToKeep[num].ThingDef, tmpDrugsToKeep[num].Count - __instance.innerContainer[j].stackCount);
                    }
                    else if(PerformanceManager.IsInstrument(__instance.innerContainer[j]))
                    {
                        if (bestInstrument == null)
                        {
                            __result = new ThingCount(__instance.innerContainer[j], __instance.innerContainer[j].stackCount);
                            return false;
                        }

                        if(bestInstrument.GetHashCode() != __instance.innerContainer[j].GetHashCode())
                        {
                            __result = new ThingCount(__instance.innerContainer[j], __instance.innerContainer[j].stackCount);
                            return false;
                        }
                    }
                    else
                    {
                        __result = new ThingCount(__instance.innerContainer[j], __instance.innerContainer[j].stackCount);
                        return false;
                    }

                }
                __result = default(ThingCount);
                return false;
            }
            else
            {
                __result = new ThingCount(__instance.innerContainer[0], __instance.innerContainer[0].stackCount);
                return false;
            }
        }

    }

    //patch to enable music joy type while on caravan
    [HarmonyPatch(typeof(Caravan_NeedsTracker), "TrySatisfyJoyNeed")]
    class PatchTrySatisfyJoyNeed
    {
        public static float MusicQuality { get; set; }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("GainJoy"))
                {
                    // do something
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchTrySatisfyJoyNeed).GetMethod("ApplyThoughts"));

                }
            }
        }

        public static void ApplyThoughts(Pawn listener, JoyKindDef joyKindDef)
        {
            if (joyKindDef != JoyKindDefOf_Music.Music)
                return;

            ThoughtDef thought = PerformanceManager.GetThoughtDef(MusicQuality);

            if (thought == null)
                return;

            Caravan caravan = CaravanUtility.GetCaravan(listener);

            List<Pawn> audience = new List<Pawn>();

            foreach (Pawn pawn in caravan.pawns)
            {
                if (!pawn.NonHumanlikeOrWildMan() && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) && pawn.Awake())
                    audience.Add(pawn);
            }
#if DEBUG
            Verse.Log.Message(String.Format("Giving memory of {0} to {1} pawns (caravan)", thought.stages[0].label, audience.Count()));
#endif        

            foreach (Pawn audienceMember in audience)
            {
                audienceMember.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }
    }

    //patch to enable music joy type while on caravan
    [HarmonyPatch(typeof(Caravan_NeedsTracker), "GetAvailableJoyKindsFor", new Type[] { typeof(Pawn), typeof(List<JoyKindDef>)})]
    class PatchGetAvailableJoyKindsFor
    {
        static void Postfix(Pawn p, List<JoyKindDef> outJoyKinds, ref Caravan ___caravan)
        {
            if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) || !p.Awake()) return;

            if (p.needs.joy.tolerances.BoredOf(JoyKindDefOf_Music.Music)) return;

            float quality;

            List<Pawn> pawnsTmp = new List<Pawn>();
            pawnsTmp.AddRange(___caravan.pawns);

            Pawn musician;

            while(pawnsTmp.TryRandomElement(out musician))
            {
                if(PerformanceManager.IsPotentialCaravanMusician(musician, out quality))
                {
                    outJoyKinds.Add(JoyKindDefOf_Music.Music);
                    PatchTrySatisfyJoyNeed.MusicQuality = quality;

#if DEBUG
                    Verse.Log.Message(String.Format("Checking caravanner {0} for music availability : yes", p.Label));
#endif
                    return;
                }
                else
                {
                    pawnsTmp.Remove(musician);
                }
            }

#if DEBUG
            Verse.Log.Message(String.Format("Checking caravanner {0} for music availability: no", p.Label));
#endif

        }

    }

    //[HarmonyPatch(typeof(CompUsable), "get_FloatMenuOptionLabel")]
    //class PatchFloatMenuOptionLabel
    //{
    //    static bool Prefix(CompUsable __instance, Pawn pawn, ref string __result)
    //    {
    //        CompMusicalInstrument otherComp = __instance.parent.TryGetComp<CompMusicalInstrument>();

    //        if(otherComp == null)
    //        {
    //            __result = __instance.Props.useLabel;
    //        }
    //        else
    //        {
    //            __result = String.Format(__instance.Props.useLabel, __instance.parent.LabelCap);
    //        }

    //        return false;
    //    }
    //}

    //patch to allow non-colonist pawns to spawn with an instrument in their inventory, if appropriate
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
