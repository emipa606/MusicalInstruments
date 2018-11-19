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

        private static List<CompGatherSpot> workingSpots = new List<CompGatherSpot>();

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
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

            return pawn.Map.gatherSpotLister.activeSpots.Select(x => (Thing)x.parent);
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (!pawn.Map.GetComponent<PerformanceManager>().CanPlayForWorkNow(pawn))
                return null;

            CompGatherSpot compGatherSpot = thing.TryGetComp<CompGatherSpot>();

            if (compGatherSpot == null)
                return null;

            Job job;

            IntVec3 standingSpot;
            if (!PerformanceManager.TryFindSitSpotOnGroundNear(compGatherSpot.parent.Position, pawn, out standingSpot))
            {
                return null;
            }

            job = new Job(JobDefOf_MusicPlayWork.MusicPlayWork, compGatherSpot.parent, standingSpot);

            Thing instrument;

            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.story.WorkTypeIsDisabled(art) &&
                PerformanceManager.TryFindInstrumentToPlay(compGatherSpot.parent.Position, pawn, out instrument))
            {

                job.targetC = instrument;
            }
            else return null;



            job.count = 1;

            return job;
        }
    }
}
