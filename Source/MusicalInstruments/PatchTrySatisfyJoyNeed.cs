using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MusicalInstruments;

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
        Log.Message(string.Format("Giving memory of {0} to {1} pawns (caravan)", thought.stages[0].label,
            audience.Count()));
#endif

        foreach (var audienceMember in audience)
        {
            audienceMember.needs.mood.thoughts.memories.TryGainMemory(thought);
        }
    }
}