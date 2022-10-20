using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MusicalInstruments;

public class WorkGiver_MusicPlay : WorkGiver_Scanner
{
    private static readonly List<CompMusicSpot> workingSpots = new List<CompMusicSpot>();

    //public override ThingRequest PotentialWorkThingRequest =>
    //Verse.Log.Message("PotentialWorkThingRequest");
    //Unsatisfactory... need to narrow this down as I can't figure out how to get it to only use PotentialWorkThingsGlobal
    //ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        var pm = pawn.Map.GetComponent<PerformanceManager>();

        if (!pm.CanPlayForWorkNow(pawn))
        {
            return null;
        }

        var things = pm.ListActiveMusicSpots().Select(x => (Thing)x.parent);

        //we also need to check for availibilty of an instrument here
        if (PerformanceManager.HeldInstrument(pawn) == null)
        {
            things = things.Where(x => pm.AnyAvailableMapInstruments(pawn, x));
        }

        //Verse.Log.Message(String.Format("PotentialWorkThingsGlobal for {0}: {1} things", pawn.Label, things.Count()));

        return things;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
            !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) ||
            !pawn.Awake() ||
            pawn.WorkTagIsDisabled(WorkTags.Artistic))
        {
            return false;
        }

        var pm = pawn.Map.GetComponent<PerformanceManager>();

        if (!pm.CanPlayForWorkNow(pawn))
        {
            return false;
        }

        var compMusicSpot = t.TryGetComp<CompMusicSpot>();
        var instrumentComp = t.TryGetComp<CompMusicalInstrument>();
        var powerComp = t.TryGetComp<CompPowerTrader>();

        if (compMusicSpot == null)
        {
            return false;
        }

        if (!compMusicSpot.Active || instrumentComp == null)
        {
            return false;
        }

        if (!pm.TryFindSitSpotOnGroundNear(compMusicSpot, pawn, out _))
        {
            return false;
        }


        if (forced &&
            instrumentComp.Props.isBuilding &&
            pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None) &&
            (powerComp == null || powerComp.PowerOn))
        {
            if (!pm.TryFindStandingSpotOrChair(compMusicSpot, pawn, t, out _))
            {
                return false;
            }
        }
        else if (pm.TryFindInstrumentToPlay(compMusicSpot.parent, pawn, out var instrument, true))
        {
#if DEBUG
                Verse.Log.Message(string.Format("{0} chose to play {1}", pawn.LabelShort, instrument.LabelShort));
#endif

            if (!pm.TryFindStandingSpotOrChair(compMusicSpot, pawn, instrument, out _))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        //Verse.Log.Message(String.Format("Trying to play at {0}", thing.Label));

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
            !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) ||
            !pawn.Awake() ||
            pawn.WorkTagIsDisabled(WorkTags.Artistic))
        {
            return null;
        }

        var pm = pawn.Map.GetComponent<PerformanceManager>();

        if (!pm.CanPlayForWorkNow(pawn))
        {
            return null;
        }

        var compMusicSpot = thing.TryGetComp<CompMusicSpot>();

        if (compMusicSpot == null)
        {
            return null;
        }

        if (!compMusicSpot.Active)
        {
            return null;
        }

        if (!pm.TryFindSitSpotOnGroundNear(compMusicSpot, pawn, out _))
        {
            return null;
        }

        var job = new Job(JobDefOf_MusicPlayWork.MusicPlayWork, compMusicSpot.parent);


        var musicSpotComp = thing.TryGetComp<CompMusicSpot>();
        var instrumentComp = thing.TryGetComp<CompMusicalInstrument>();
        var powerComp = thing.TryGetComp<CompPowerTrader>();

        LocalTargetInfo chairOrSpot;

        if (forced &&
            instrumentComp != null &&
            instrumentComp.Props.isBuilding &&
            pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.None) &&
            (powerComp == null || powerComp.PowerOn))
        {
            if (!pm.TryFindStandingSpotOrChair(musicSpotComp, pawn, thing, out chairOrSpot))
            {
                return null;
            }

            job.targetB = chairOrSpot;
            job.targetC = thing;
        }
        else if (pm.TryFindInstrumentToPlay(compMusicSpot.parent, pawn, out var instrument, true))
        {
#if DEBUG
                Verse.Log.Message(string.Format("{0} chose to play {1}", pawn.LabelShort, instrument.LabelShort));
#endif

            if (!pm.TryFindStandingSpotOrChair(musicSpotComp, pawn, instrument, out chairOrSpot))
            {
                return null;
            }

            job.targetB = chairOrSpot;
            job.targetC = instrument;
        }
        else
        {
#if DEBUG
                Verse.Log.Message(string.Format("{0} couldn't find an instrument", pawn.LabelShort));
#endif
            return null;
        }


        job.count = 1;

        return job;
    }
}