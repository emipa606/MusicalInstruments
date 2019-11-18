using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Verse;
using Verse.AI;

using RimWorld;

namespace MusicalInstruments
{


    public class WorkGiver_MusicPlay : WorkGiver_Scanner
    {

        private static readonly WorkTypeDef art = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Where(wtd => wtd.defName == "Art").SingleOrDefault();

        private static List<CompMusicSpot> workingSpots = new List<CompMusicSpot>();

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                //Verse.Log.Message("PotentialWorkThingRequest");
                //Unsatisfactory... need to narrow this down as I can't figure out how to get it to only use PotentialWorkThingsGlobal
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();

            if (!pm.CanPlayForWorkNow(pawn))
                return null;

            IEnumerable<Thing> things = pm.ListActiveMusicSpots().Select(x => (Thing)x.parent);

            //we also need to check for availibilty of an instrument here
            if (PerformanceManager.HeldInstrument(pawn) == null)
                things = things.Where(x => pm.AnyAvailableMapInstruments(pawn, x));

            //Verse.Log.Message(String.Format("PotentialWorkThingsGlobal for {0}: {1} things", pawn.Label, things.Count()));

            return things;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) ||
                !pawn.Awake() ||
                pawn.story.WorkTypeIsDisabled(art))
                return false;

            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();

            if (!pm.CanPlayForWorkNow(pawn))
                return false;

            CompMusicSpot compMusicSpot = t.TryGetComp<CompMusicSpot>();
            CompMusicalInstrument instrumentComp = t.TryGetComp<CompMusicalInstrument>();
            CompPowerTrader powerComp = t.TryGetComp<CompPowerTrader>();

            if (compMusicSpot == null)
                return false;

            if (!compMusicSpot.Active || instrumentComp == null)
                return false;

            IntVec3 standingSpot;
            if (!pm.TryFindSitSpotOnGroundNear(compMusicSpot, pawn, out standingSpot))
            {
                return false;
            }

            Thing instrument;

            LocalTargetInfo chairOrSpot = null;

            if (forced &&
                instrumentComp != null &&
                instrumentComp.Props.isBuilding &&
                pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None) &&
                (powerComp == null || powerComp.PowerOn))
            {
                if (!pm.TryFindStandingSpotOrChair(compMusicSpot, pawn, t, out chairOrSpot))
                    return false;
            }
            else if (pm.TryFindInstrumentToPlay(compMusicSpot.parent, pawn, out instrument, true))
            {

#if DEBUG
                Verse.Log.Message(String.Format("{0} chose to play {1}", pawn.LabelShort, instrument.LabelShort));
#endif

                if (!pm.TryFindStandingSpotOrChair(compMusicSpot, pawn, instrument, out chairOrSpot))
                    return false;
            }
            else return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            //Verse.Log.Message(String.Format("Trying to play at {0}", thing.Label));

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) ||
                !pawn.Awake() ||
                pawn.story.WorkTypeIsDisabled(art))
                return null;

            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();

            if (!pm.CanPlayForWorkNow(pawn))
                return null;

            CompMusicSpot compMusicSpot = thing.TryGetComp<CompMusicSpot>();

            if (compMusicSpot == null)
                return null;

            if (!compMusicSpot.Active)
                return null;

            Job job;

            IntVec3 standingSpot;
            if (!pm.TryFindSitSpotOnGroundNear(compMusicSpot, pawn, out standingSpot))
            {
                return null;
            }

            job = new Job(JobDefOf_MusicPlayWork.MusicPlayWork, compMusicSpot.parent); //, standingSpot);

            Thing instrument;


            CompMusicSpot musicSpotComp = thing.TryGetComp<CompMusicSpot>();
            CompMusicalInstrument instrumentComp = thing.TryGetComp<CompMusicalInstrument>();
            CompPowerTrader powerComp = thing.TryGetComp<CompPowerTrader>();

            LocalTargetInfo chairOrSpot = null;

            if (forced &&
                instrumentComp != null &&
                instrumentComp.Props.isBuilding &&
                pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.None) &&
                (powerComp == null || powerComp.PowerOn))
            {
                if (!pm.TryFindStandingSpotOrChair(musicSpotComp, pawn, thing, out chairOrSpot))
                    return null;

                job.targetB = chairOrSpot;
                job.targetC = thing;
            }
            else if (pm.TryFindInstrumentToPlay(compMusicSpot.parent, pawn, out instrument, true))
            {

#if DEBUG
                Verse.Log.Message(String.Format("{0} chose to play {1}", pawn.LabelShort, instrument.LabelShort));
#endif

                if (!pm.TryFindStandingSpotOrChair(musicSpotComp, pawn, instrument, out chairOrSpot))
                    return null;

                job.targetB = chairOrSpot;
                job.targetC = instrument;
            }
            else {
#if DEBUG
                Verse.Log.Message(String.Format("{0} couldn't find an instrument", pawn.LabelShort));
#endif
                return null;
            }




            job.count = 1;

            return job;
        }


    }
}
