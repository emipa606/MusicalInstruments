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
    class JoyGiver_MusicListen : JoyGiver
    {

        private const float GatherRadius = 3.9f;

        private static readonly int NumRadiusCells = GenRadial.NumCellsInRadius(GatherRadius);

        private static readonly List<IntVec3> RadialPatternMiddleOutward = (from c in GenRadial.RadialPattern.Take(NumRadiusCells)
                                                                            orderby Mathf.Abs((c - IntVec3.Zero).LengthHorizontal - 1.95f)
                                                                            select c).ToList();


        private static List<CompGatherSpot> workingSpots = new List<CompGatherSpot>();

        public override Job TryGiveJob(Pawn pawn)
        {
            return this.TryGiveJobInt(pawn, null);
        }

        public override Job TryGiveJobInPartyArea(Pawn pawn, IntVec3 partySpot)
        {
            return this.TryGiveJobInt(pawn, (CompGatherSpot x) => PartyUtility.InPartyArea(x.parent.Position, partySpot, pawn.Map));
        }

        public override Job TryGiveJobWhileInBed(Pawn pawn)
        {
            Room room = pawn.GetRoom();

            // get gather spots in pawn's current room
            List<CompGatherSpot> localGatherSpots = pawn.Map.gatherSpotLister.activeSpots.Where(x => room.Cells.Contains(x.parent.Position)).ToList();

            // if no gathering spots then give up
            if (localGatherSpots.Count == 0)
            {
                return null;
            }

            workingSpots = localGatherSpots;

            // pick a random one
            CompGatherSpot compGatherSpot;
            while (workingSpots.TryRandomElement(out compGatherSpot))
            {
                workingSpots.Remove(compGatherSpot);

                // is a performance currently in progress
                if (pawn.Map.GetComponent<PerformanceManager>().HasPerformance(compGatherSpot.parent))
                {
                    Job job = new Job(def.jobDef, compGatherSpot.parent, pawn.CurrentBed());
                }
            }

            return null;
        }

        private Job TryGiveJobInt(Pawn pawn, Predicate<CompGatherSpot> gatherSpotValidator)
        {
            // if no gathering spots then give up
            if (pawn.Map.gatherSpotLister.activeSpots.Count == 0)
            {
                return null;
            }
            // load all social areas on map into list
            workingSpots.Clear();
            for (int i = 0; i < pawn.Map.gatherSpotLister.activeSpots.Count; i++)
            {
                workingSpots.Add(pawn.Map.gatherSpotLister.activeSpots[i]);
            }

            // pick a random one
            CompGatherSpot compGatherSpot;
            while (workingSpots.TryRandomElement(out compGatherSpot))
            {
                // remove from list
                workingSpots.Remove(compGatherSpot);
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
                                    
                                    // is a performance currently in progress
                                    if (pawn.Map.GetComponent<PerformanceManager>().HasPerformance(compGatherSpot.parent))
                                    {
                                        
                                        
                                        // find a place to sit or stand, or return null if there aren't any                                  

                                    
                                        Job job;

                                        IntVec3 standingSpot;
                                        if (TryFindChairNear(compGatherSpot.parent.Position, pawn, out Thing chair))
                                        {
                                            job = new Job(this.def.jobDef, compGatherSpot.parent, chair);
                                            return job;
                                        }                                        
                                        else if (TryFindSitSpotOnGroundNear(compGatherSpot.parent.Position, pawn, out standingSpot))
                                        {
                                            job = new Job(this.def.jobDef, compGatherSpot.parent, standingSpot);
                                            return job;
                                        }
                                        else return null;
                                    }
 
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static bool TryFindChairNear(IntVec3 center, Pawn sitter, out Thing chair)
        {
            for (int i = 0; i < RadialPatternMiddleOutward.Count; i++)
            {
                IntVec3 c = center + RadialPatternMiddleOutward[i];
                Building edifice = c.GetEdifice(sitter.Map);
                if (edifice != null && edifice.def.building.isSittable && sitter.CanReserve(edifice) && !edifice.IsForbidden(sitter) && GenSight.LineOfSight(center, edifice.Position, sitter.Map, skipFirstCell: true))
                {
                    chair = edifice;
                    return true;
                }
            }
            chair = null;
            return false;
        }

        private static bool TryFindSitSpotOnGroundNear(IntVec3 center, Pawn sitter, out IntVec3 result)
        {
            for (int i = 0; i < 30; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[Rand.Range(1, NumRadiusCells)];
                if (sitter.CanReserveAndReach(intVec, PathEndMode.OnCell, Danger.None) && intVec.GetEdifice(sitter.Map) == null && GenSight.LineOfSight(center, intVec, sitter.Map, skipFirstCell: true))
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
