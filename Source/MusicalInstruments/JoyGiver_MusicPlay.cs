using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MusicalInstruments;

internal class JoyGiver_MusicPlay : JoyGiver
{
    private static readonly List<CompMusicSpot> workingSpots = new List<CompMusicSpot>();

    public override Job TryGiveJob(Pawn pawn)
    {
        return TryGiveJobInt(pawn, null);
    }

    public override Job TryGiveJobInGatheringArea(Pawn pawn, IntVec3 gatherSpot, float maxRadius = -1f)
    {
        return TryGiveJobInt(pawn, x => GatheringsUtility.InGatheringArea(x.parent.Position, gatherSpot, pawn.Map));
    }

    private Job TryGiveJobInt(Pawn pawn, Predicate<CompMusicSpot> musicSpotValidator)
    {
        //quit roll for low skill without instrument
        var pm = pawn.Map.GetComponent<PerformanceManager>();
        var skill = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;

        if (PerformanceManager.HeldInstrument(pawn) == null && skill < 3 && Rand.Chance(.75f))
        {
            return null;
        }

        // if no music spots then give up
        if (pm.ListActiveMusicSpots().Count == 0)
        {
            return null;
        }

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
            !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) ||
            !pawn.Awake() ||
            pawn.WorkTagIsDisabled(WorkTags.Artistic))
        {
            return null;
        }

        // load all music spots on map into list
        workingSpots.Clear();
        for (var i = 0; i < pm.ListActiveMusicSpots().Count; i++)
        {
            workingSpots.Add(pm.ListActiveMusicSpots()[i]);
        }

        // pick a random one
        while (workingSpots.TryRandomElement(out var CompMusicSpot))
        {
            // remove from list
            _ = workingSpots.Remove(CompMusicSpot);
            // check zones etc
            if (CompMusicSpot.parent.IsForbidden(pawn))
            {
                continue;
            }

            // see if there's a safe path to get there
            if (!pawn.CanReach(CompMusicSpot.parent, PathEndMode.Touch, Danger.None))
            {
                continue;
            }

            // prisoners seperated from colonists
            if (!CompMusicSpot.parent.IsSociallyProper(pawn))
            {
                continue;
            }

            // only friendly factions
            if (!CompMusicSpot.parent.IsPoliticallyProper(pawn))
            {
                continue;
            }

            // check passed in predicate - i.e. parties
            if (musicSpotValidator != null && !musicSpotValidator(CompMusicSpot))
            {
                continue;
            }

            //check for an instrument
            if (PerformanceManager.HeldInstrument(pawn) == null &&
                !pm.AnyAvailableMapInstruments(pawn, CompMusicSpot.parent))
            {
                continue;
            }

            if (!pm.TryFindInstrumentToPlay(CompMusicSpot.parent, pawn, out var instrument))
            {
                continue;
            }
            // find a place to sit or stand, or return null if there aren't any

            if (!pm.TryFindStandingSpotOrChair(CompMusicSpot, pawn, instrument,
                    out var chairOrSpot))
            {
                continue;
            }

            var job = new Job(def.jobDef, CompMusicSpot.parent, chairOrSpot,
                instrument)
            {
                count = 1
            };

            return job;
        }

        return null;
    }
}