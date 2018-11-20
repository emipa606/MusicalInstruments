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


        private static List<CompMusicSpot> workingSpots = new List<CompMusicSpot>();

        public override Job TryGiveJob(Pawn pawn)
        {
            return this.TryGiveJobInt(pawn, null);
        }

        public override Job TryGiveJobInPartyArea(Pawn pawn, IntVec3 partySpot)
        {
            return this.TryGiveJobInt(pawn, (CompMusicSpot x) => PartyUtility.InPartyArea(x.parent.Position, partySpot, pawn.Map));
        }

        public override Job TryGiveJobWhileInBed(Pawn pawn)
        {
            Room room = pawn.GetRoom();
            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();

            // get music spots in pawn's current room
            List<CompMusicSpot> localMusicSpots = pm.ListActiveMusicSpots().Where(x => room.Cells.Contains(x.parent.Position)).ToList();

            // if no music spots then give up
            if (localMusicSpots.Count == 0)
            {
                return null;
            }

            workingSpots = localMusicSpots;

            // pick a random one
            CompMusicSpot CompMusicSpot;
            while (workingSpots.TryRandomElement(out CompMusicSpot))
            {
                workingSpots.Remove(CompMusicSpot);

                // is a performance currently in progress
                if (pawn.Map.GetComponent<PerformanceManager>().HasPerformance(CompMusicSpot.parent))
                {
                    Job job = new Job(def.jobDef, CompMusicSpot.parent, pawn.CurrentBed());
                }
            }

            return null;
        }

        private Job TryGiveJobInt(Pawn pawn, Predicate<CompMusicSpot> musicSpotValidator)
        {
            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();

            // if no music spots then give up
            if (pm.ListActiveMusicSpots().Count == 0)
            {
                return null;
            }
            // load all music spots on map into list
            workingSpots.Clear();
            for (int i = 0; i < pm.ListActiveMusicSpots().Count; i++)
            {
                workingSpots.Add(pm.ListActiveMusicSpots()[i]);
            }

            // pick a random one
            CompMusicSpot CompMusicSpot;
            while (workingSpots.TryRandomElement(out CompMusicSpot))
            {
                // remove from list
                workingSpots.Remove(CompMusicSpot);
                // check zones etc
                if (!CompMusicSpot.parent.IsForbidden(pawn))
                {
                    // see if there's a safe path to get there
                    if (pawn.CanReach(CompMusicSpot.parent, PathEndMode.Touch, Danger.None, false, TraverseMode.ByPawn))
                    {
                        // prisoners seperated from colonists
                        if (CompMusicSpot.parent.IsSociallyProper(pawn))
                        {
                            // only friendly factions
                            if (CompMusicSpot.parent.IsPoliticallyProper(pawn))
                            {
                                // check passed in predicate - i.e. parties
                                if (musicSpotValidator == null || musicSpotValidator(CompMusicSpot))
                                {
                                    
                                    // is a performance currently in progress
                                    if (pawn.Map.GetComponent<PerformanceManager>().HasPerformance(CompMusicSpot.parent))
                                    {
                                        
                                        
                                        // find a place to sit or stand, or return null if there aren't any                                  

                                    
                                        Job job;

                                        IntVec3 standingSpot;
                                        if (TryFindChairNear(CompMusicSpot.parent.Position, pawn, out Thing chair))
                                        {
                                            job = new Job(this.def.jobDef, CompMusicSpot.parent, chair);
                                            return job;
                                        }                                        
                                        else if (TryFindSitSpotOnGroundNear(CompMusicSpot.parent.Position, pawn, out standingSpot))
                                        {
                                            job = new Job(this.def.jobDef, CompMusicSpot.parent, standingSpot);
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
