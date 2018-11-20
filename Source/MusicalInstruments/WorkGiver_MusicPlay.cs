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

            //we also need to check for availibilty of an instrument here...?
            if (pm.HeldInstrument(pawn) == null && !pm.AnyAvailableMapInstruments(pawn))
                return null;
            IEnumerable<Thing> things = pm.ListActiveMusicSpots().Select(x => (Thing)x.parent);

            //Verse.Log.Message(String.Format("PotentialWorkThingsGlobal for {0}: {1} things", pawn.Label, things.Count()));

            return things;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            //Verse.Log.Message(String.Format("Trying to play at {0}", thing.Label));

            if (!pawn.Map.GetComponent<PerformanceManager>().CanPlayForWorkNow(pawn))
                return null;

            CompMusicSpot compMusicSpot = thing.TryGetComp<CompMusicSpot>();

            if (compMusicSpot == null)
                return null;

            if (!compMusicSpot.Active)
                return null;

            Job job;

            IntVec3 standingSpot;
            if (!PerformanceManager.TryFindSitSpotOnGroundNear(compMusicSpot.parent.Position, pawn, out standingSpot))
            {
                return null;
            }

            job = new Job(JobDefOf_MusicPlayWork.MusicPlayWork, compMusicSpot.parent, standingSpot);

            Thing instrument;

            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.story.WorkTypeIsDisabled(art) &&
                PerformanceManager.TryFindInstrumentToPlay(compMusicSpot.parent.Position, pawn, out instrument))
            {

                job.targetC = instrument;
            }
            else return null;



            job.count = 1;

            return job;
        }
    }
}
