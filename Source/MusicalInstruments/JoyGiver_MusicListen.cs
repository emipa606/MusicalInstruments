using System;
using System.Collections.Generic;
using System.Linq;

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
            return TryGiveJobInt(pawn, null);
        }

        public override Job TryGiveJobInGatheringArea(Pawn pawn, IntVec3 gatheringSpot)
        {
            return TryGiveJobInt(pawn, (CompMusicSpot x) => GatheringsUtility.InGatheringArea(x.parent.Position, gatheringSpot, pawn.Map));
        }

        public override Job TryGiveJobWhileInBed(Pawn pawn)
        {
#if DEBUG
            Verse.Log.Message("TryGiveJobWhileInBed " + pawn.Label);
#endif

            Room room = pawn.GetRoom();
            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();

            // get music spots in pawn's current room
            List<CompMusicSpot> localMusicSpots = pm.ListActiveMusicSpots().Where(x => room.Cells.Contains(x.parent.Position)).ToList();

            // if no music spots then give up
            if (localMusicSpots.Count == 0)
            {
                return null;
            }

#if DEBUG
            Verse.Log.Message(string.Format("{0} local spots", localMusicSpots.Count));
#endif

            workingSpots = localMusicSpots;

            // pick a random one
            while (workingSpots.TryRandomElement(out CompMusicSpot CompMusicSpot))
            {
                workingSpots.Remove(CompMusicSpot);

                // is a performance currently in progress
                if (pawn.Map.GetComponent<PerformanceManager>().HasPerformance(CompMusicSpot.parent))
                {
#if DEBUG
                    Verse.Log.Message("Found performance");
#endif

                    Job job = new Job(def.jobDef, CompMusicSpot.parent, pawn.CurrentBed());
                    return job;
                }
            }

            return null;
        }

        private Job TryGiveJobInt(Pawn pawn, Predicate<CompMusicSpot> musicSpotValidator)
        {
#if DEBUG
            Verse.Log.Message(string.Format("{0} trying to listen to music", pawn.LabelShort));
#endif

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
            while (workingSpots.TryRandomElement(out CompMusicSpot CompMusicSpot))
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
#if DEBUG
                                        Verse.Log.Message("Found performance to listen to");
#endif
                                        // find a place to sit or stand, or return null if there aren't any                                  


                                        Job job;

                                        if (pm.TryFindChairNear(CompMusicSpot, pawn, out Thing chair))
                                        {
#if DEBUG
                                            Verse.Log.Message("Found chair");
#endif
                                            job = new Job(def.jobDef, CompMusicSpot.parent, chair);
                                            return job;
                                        }
                                        else if (pm.TryFindSitSpotOnGroundNear(CompMusicSpot, pawn, out IntVec3 standingSpot))
                                        {
#if DEBUG
                                            Verse.Log.Message("Found standing spot");
#endif
                                            job = new Job(def.jobDef, CompMusicSpot.parent, standingSpot);
                                            return job;
                                        }
                                        else
                                        {
#if DEBUG
                                            Verse.Log.Message("Failed to find chair or standing spot");
#endif
                                            return null;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }

#if DEBUG
            Verse.Log.Message("Failed to find performance");
#endif
            return null;
        }

    }
}
