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
    class JoyGiver_MusicPlay : JoyGiver
    {
        private static readonly List<ThingDef> allInstruments = new List<ThingDef> {
            ThingDef.Named("FrameDrum"),
            ThingDef.Named("Ocarina"),
            ThingDef.Named("Guitar"),
            ThingDef.Named("Violin")
        };

        private static readonly WorkTypeDef art = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Where(wtd => wtd.defName == "Art").SingleOrDefault();

        private static List<CompGatherSpot> workingSpots = new List<CompGatherSpot>();

        private const float GatherRadius = 3.9f;

        private static readonly int NumRadiusCells = GenRadial.NumCellsInRadius(GatherRadius);

        private static readonly List<IntVec3> RadialPatternMiddleOutward = (from c in GenRadial.RadialPattern.Take(JoyGiver_MusicPlay.NumRadiusCells)
                                                                            orderby Mathf.Abs((c - IntVec3.Zero).LengthHorizontal - 1.95f)
                                                                            select c).ToList<IntVec3>();

        private static List<ThingDef> nurseableDrugs = new List<ThingDef>();

        public override Job TryGiveJob(Pawn pawn)
        {
            return this.TryGiveJobInt(pawn, null);
        }

        public override Job TryGiveJobInPartyArea(Pawn pawn, IntVec3 partySpot)
        {
            return this.TryGiveJobInt(pawn, (CompGatherSpot x) => PartyUtility.InPartyArea(x.parent.Position, partySpot, pawn.Map));
        }

        private Job TryGiveJobInt(Pawn pawn, Predicate<CompGatherSpot> gatherSpotValidator)
        {
            // if no gathering sports then give up
            if (pawn.Map.gatherSpotLister.activeSpots.Count == 0)
            {
                return null;
            }
            // load all social areas on map into list
            JoyGiver_MusicPlay.workingSpots.Clear();
            for (int i = 0; i < pawn.Map.gatherSpotLister.activeSpots.Count; i++)
            {
                JoyGiver_MusicPlay.workingSpots.Add(pawn.Map.gatherSpotLister.activeSpots[i]);
            }

            // pick a random one
            CompGatherSpot compGatherSpot;
            while (JoyGiver_MusicPlay.workingSpots.TryRandomElement(out compGatherSpot))
            {
                // remove from list
                JoyGiver_MusicPlay.workingSpots.Remove(compGatherSpot);
                // check zones etc
                if (!compGatherSpot.parent.IsForbidden(pawn))
                {
                    // see if there's a safe path to get there
                    if (pawn.CanReach(compGatherSpot.parent, PathEndMode.Touch, Danger.None, false, TraverseMode.ByPawn))
                    {
                        // prisoners seperated from colonists
                        if (compGatherSpot.parent.IsSociallyProper(pawn))
                        {
                            // only friendly factions
                            if (compGatherSpot.parent.IsPoliticallyProper(pawn))
                            {
                                // check passed in predicate - i.e. parties
                                if (gatherSpotValidator == null || gatherSpotValidator(compGatherSpot))
                                {
                                    // find a place to sit or stand, or return null if there aren't any

                                    Job job;
                                
                                    IntVec3 standingSpot;
                                    if (!JoyGiver_MusicPlay.TryFindSitSpotOnGroundNear(compGatherSpot.parent.Position, pawn, out standingSpot))
                                    {
                                        return null;
                                    }
                                    job = new Job(this.def.jobDef, compGatherSpot.parent, standingSpot);
                    
                                    Thing instrument;

                                    if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.story.WorkTypeIsDisabled(art) &&
                                        JoyGiver_MusicPlay.TryFindInstrumentToPlay(compGatherSpot.parent.Position, pawn, out instrument))
                                    {

                                        job.targetC = instrument;
                                    }
                                    else return null;

      

                                    job.count = 1;

                                    return job;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static bool TryFindInstrumentToPlay(IntVec3 center, Pawn musician, out Thing instrument)
        {
            instrument = null;

            foreach(Thing inventoryThing in musician.inventory.innerContainer) {
                if (allInstruments.Contains(inventoryThing.def)) {
                    instrument = inventoryThing;
                    return true;
                }

            }

            foreach (ThingDef instrumentDef in JoyGiver_MusicPlay.allInstruments.InRandomOrder()) {

                List<Thing> list = musician.Map.listerThings.ThingsOfDef(instrumentDef);
                Predicate<Thing> validator = (Thing t) => musician.CanReserve(t, 1, -1, null, false) && !t.IsForbidden(musician);
                instrument = GenClosest.ClosestThing_Global_Reachable(center, musician.Map, list, PathEndMode.OnCell, TraverseParms.For(musician, Danger.Deadly, TraverseMode.ByPawn, false), 40f, validator, null);
                if (instrument != null) return true;
            }

            return false;
        }

        

        private static bool TryFindSitSpotOnGroundNear(IntVec3 center, Pawn sitter, out IntVec3 result)
        {
            for (int i = 0; i < 30; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[Rand.Range(1, JoyGiver_MusicPlay.NumRadiusCells)];
                if (sitter.CanReserveAndReach(intVec, PathEndMode.OnCell, Danger.None, 1, -1, null, false) && intVec.GetEdifice(sitter.Map) == null && GenSight.LineOfSight(center, intVec, sitter.Map, true, null, 0, 0))
                {
                    result = intVec;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            return false;
        }
    }
}
