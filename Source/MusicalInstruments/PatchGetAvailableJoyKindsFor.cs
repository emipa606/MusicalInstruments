using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MusicalInstruments;

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
                Log.Message($"Checking caravanner {p.Label} for music availability : yes");
#endif
                return;
            }

            pawnsTmp.Remove(musician);
        }

#if DEBUG
        Log.Message($"Checking caravanner {p.Label} for music availability: no");
#endif
    }
}