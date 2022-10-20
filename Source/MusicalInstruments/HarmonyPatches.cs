using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MusicalInstruments;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("com.dogproblems.rimworldmods.musicalinstruments");

        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

//patch for warning if too few joy types
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

//patch for listing available joy types
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

//patch to keep one instrument in colonist inventory on return from caravan
[HarmonyPatch(typeof(Pawn_InventoryTracker), "get_FirstUnloadableThing")]
internal class PatchFirstUnloadableThing
{
    private static readonly List<ThingDefCount> tmpDrugsToKeep = new List<ThingDefCount>();

    private static bool Prefix(Pawn_InventoryTracker __instance, ref ThingCount __result)
    {
        if (__instance.innerContainer.Count == 0)
        {
            __result = default;
            return false;
        }

        tmpDrugsToKeep.Clear();

        if (__instance.pawn.drugs?.CurrentPolicy != null)
        {
            var currentPolicy = __instance.pawn.drugs.CurrentPolicy;
            for (var i = 0; i < currentPolicy.Count; i++)
            {
                if (currentPolicy[i].takeToInventory > 0)
                {
                    tmpDrugsToKeep.Add(new ThingDefCount(currentPolicy[i].drug, currentPolicy[i].takeToInventory));
                }
            }
        }

        Thing bestInstrument = null;

        if (!__instance.pawn.NonHumanlikeOrWildMan() && !__instance.pawn.WorkTagIsDisabled(WorkTags.Artistic))
        {
            var artSkill = __instance.pawn.skills.GetSkill(SkillDefOf.Artistic).levelInt;

            IEnumerable<Thing> heldInstruments = __instance.innerContainer
                .Where(PerformanceManager.IsInstrument)
                .Where(x => !x.TryGetComp<CompMusicalInstrument>().Props.isBuilding)
                .OrderByDescending(x => x.TryGetComp<CompMusicalInstrument>().WeightedSuitability(artSkill));

            if (heldInstruments.Any())
            {
                bestInstrument = heldInstruments.FirstOrDefault();
            }
        }

        if (tmpDrugsToKeep.Any() || bestInstrument != null)
        {
            foreach (var thing in __instance.innerContainer)
            {
                if (thing.def.IsDrug)
                {
                    var num = -1;

                    for (var k = 0; k < tmpDrugsToKeep.Count; k++)
                    {
                        if (thing.def != tmpDrugsToKeep[k].ThingDef)
                        {
                            continue;
                        }

                        num = k;
                        break;
                    }

                    if (num < 0)
                    {
                        __result = new ThingCount(thing,
                            thing.stackCount);
                        return false;
                    }

                    if (thing.stackCount > tmpDrugsToKeep[num].Count)
                    {
                        __result = new ThingCount(thing,
                            thing.stackCount - tmpDrugsToKeep[num].Count);
                        return false;
                    }

                    tmpDrugsToKeep[num] = new ThingDefCount(tmpDrugsToKeep[num].ThingDef,
                        tmpDrugsToKeep[num].Count - thing.stackCount);
                }
                else if (PerformanceManager.IsInstrument(thing))
                {
                    if (bestInstrument == null)
                    {
                        __result = new ThingCount(thing,
                            thing.stackCount);
                        return false;
                    }

                    if (bestInstrument.GetHashCode() == thing.GetHashCode())
                    {
                        continue;
                    }

                    __result = new ThingCount(thing,
                        thing.stackCount);
                    return false;
                }
                else
                {
                    __result = new ThingCount(thing,
                        thing.stackCount);
                    return false;
                }
            }

            __result = default;
            return false;
        }

        __result = new ThingCount(__instance.innerContainer[0], __instance.innerContainer[0].stackCount);
        return false;
    }
}

//patch to enable music joy type while on caravan
[HarmonyPatch(typeof(Caravan_NeedsTracker), "TrySatisfyJoyNeed")]
internal class PatchTrySatisfyJoyNeed
{
    public static float MusicQuality { get; set; }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.opcode != OpCodes.Callvirt || !instruction.operand.ToString().Contains("GainJoy"))
            {
                continue;
            }

            // do something
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldloc_2);
            yield return new CodeInstruction(OpCodes.Call,
                typeof(PatchTrySatisfyJoyNeed).GetMethod("ApplyThoughts"));
        }
    }

    public static void ApplyThoughts(Pawn listener, JoyKindDef joyKindDef)
    {
        if (joyKindDef != JoyKindDefOf_Music.Music)
        {
            return;
        }

        var thought = PerformanceManager.GetThoughtDef(MusicQuality);

        if (thought == null)
        {
            return;
        }

        var caravan = listener.GetCaravan();

        var audience = new List<Pawn>();

        foreach (var pawn in caravan.pawns)
        {
            if (!pawn.NonHumanlikeOrWildMan() && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) &&
                pawn.Awake())
            {
                audience.Add(pawn);
            }
        }
#if DEBUG
            Verse.Log.Message(string.Format("Giving memory of {0} to {1} pawns (caravan)", thought.stages[0].label, audience.Count()));
#endif

        foreach (var audienceMember in audience)
        {
            audienceMember.needs.mood.thoughts.memories.TryGainMemory(thought);
        }
    }
}

//patch to enable music joy type while on caravan
[HarmonyPatch(typeof(Caravan_NeedsTracker), "GetAvailableJoyKindsFor", typeof(Pawn), typeof(List<JoyKindDef>))]
internal class PatchGetAvailableJoyKindsFor
{
    private static void Postfix(Pawn p, List<JoyKindDef> outJoyKinds, ref Caravan ___caravan)
    {
        if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) || !p.Awake())
        {
            return;
        }

        if (p.needs.joy.tolerances.BoredOf(JoyKindDefOf_Music.Music))
        {
            return;
        }

        var pawnsTmp = new List<Pawn>();
        pawnsTmp.AddRange(___caravan.pawns);


        while (pawnsTmp.TryRandomElement(out var musician))
        {
            if (PerformanceManager.IsPotentialCaravanMusician(musician, out var quality))
            {
                outJoyKinds.Add(JoyKindDefOf_Music.Music);
                PatchTrySatisfyJoyNeed.MusicQuality = quality;

#if DEBUG
                    Verse.Log.Message(string.Format("Checking caravanner {0} for music availability : yes", p.Label));
#endif
                return;
            }

            pawnsTmp.Remove(musician);
        }

#if DEBUG
            Verse.Log.Message(string.Format("Checking caravanner {0} for music availability: no", p.Label));
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
[HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor", typeof(Pawn), typeof(PawnGenerationRequest))]
internal class PatchGenerateGearFor
{
    private static void Postfix(Pawn pawn, PawnGenerationRequest request)
    {
#if DEBUG
            Verse.Log.Message(string.Format("Trying to generate an instrument for {0}", pawn.Label));

#endif

        if (Current.ProgramState != ProgramState.Playing)
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

        if (pawn.Faction == null)
        {
#if DEBUG
                Verse.Log.Message("null faction, exit");
#endif
            return;
        }

        if (pawn.Faction.IsPlayer)
        {
#if DEBUG
                Verse.Log.Message("player faction, exit");
#endif
            return;
        }

        if (pawn.Faction.PlayerRelationKind == FactionRelationKind.Hostile)
        {
#if DEBUG
                Verse.Log.Message("hostile faction, exit");
#endif
            return;
        }

#if DEBUG
            Verse.Log.Message("continuing...");
#endif
        var artLevel = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
        var techLevel = request.Faction?.def.techLevel ?? TechLevel.Neolithic;
        ThingDef instrumentDef;
        ThingDef stuffDef;

        if (artLevel > 12 || artLevel > 8 && Rand.Chance(.75f))
        {
            if (!TryGetHardInstrument(techLevel, out instrumentDef, out stuffDef))
            {
                return;
            }

            var instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
            pawn.inventory.TryAddItemNotForSale(instrument);
        }
        else if (artLevel > 4 && Rand.Chance(.75f))
        {
            if (!TryGetEasyInstrument(techLevel, out instrumentDef, out stuffDef))
            {
                return;
            }

            var instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
            pawn.inventory.TryAddItemNotForSale(instrument);
        }
    }

    private static bool TryGetEasyInstrument(TechLevel techLevel, out ThingDef instrumentDef, out ThingDef stuffDef)
    {
        ThingDef frameDrum = null;
        ThingDef guitar = null;
        ThingDef lightLeather = null;
        ThingDef wood = null;
        ThingDef plasteel = null;

        instrumentDef = null;
        stuffDef = null;

        var neolithic = techLevel <= TechLevel.Neolithic;
        var spacer = techLevel >= TechLevel.Spacer;

        try
        {
            frameDrum = ThingDef.Named("FrameDrum");
        }
        catch
        {
            // ignored
        }

        try
        {
            guitar = ThingDef.Named("Guitar");
        }
        catch
        {
            // ignored
        }

        try
        {
            lightLeather = ThingDef.Named("Leather_Light");
        }
        catch
        {
            // ignored
        }

        try
        {
            wood = ThingDef.Named("WoodLog");
        }
        catch
        {
            // ignored
        }

        try
        {
            plasteel = ThingDef.Named("Plasteel");
        }
        catch
        {
            // ignored
        }

        if (guitar != null && !neolithic)
        {
            if (plasteel != null && spacer)
            {
                instrumentDef = guitar;
                stuffDef = plasteel;
                return true;
            }

            if (wood != null)
            {
                instrumentDef = guitar;
                stuffDef = wood;
                return true;
            }
        }

        if (frameDrum == null || lightLeather == null)
        {
            return false;
        }

        instrumentDef = frameDrum;
        stuffDef = lightLeather;
        return true;
    }

    private static bool TryGetHardInstrument(TechLevel techLevel, out ThingDef instrumentDef, out ThingDef stuffDef)
    {
        ThingDef ocarina = null;
        ThingDef violin = null;
        ThingDef jade = null;
        ThingDef wood = null;
        ThingDef plasteel = null;

        instrumentDef = null;
        stuffDef = null;

        var neolithic = techLevel <= TechLevel.Neolithic;
        var spacer = techLevel >= TechLevel.Spacer;

        try
        {
            ocarina = ThingDef.Named("Ocarina");
        }
        catch
        {
            // ignored
        }

        try
        {
            violin = ThingDef.Named("Violin");
        }
        catch
        {
            // ignored
        }

        try
        {
            jade = ThingDef.Named("Jade");
        }
        catch
        {
            // ignored
        }

        try
        {
            wood = ThingDef.Named("WoodLog");
        }
        catch
        {
            // ignored
        }

        try
        {
            plasteel = ThingDef.Named("Plasteel");
        }
        catch
        {
            // ignored
        }

        if (violin != null && !neolithic)
        {
            if (plasteel != null && spacer)
            {
                instrumentDef = violin;
                stuffDef = plasteel;
                return true;
            }

            if (wood != null)
            {
                instrumentDef = violin;
                stuffDef = wood;
                return true;
            }
        }

        if (ocarina == null || jade == null)
        {
            return false;
        }

        instrumentDef = ocarina;
        stuffDef = jade;
        return true;
    }
}