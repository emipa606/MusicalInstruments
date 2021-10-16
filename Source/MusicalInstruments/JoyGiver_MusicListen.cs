using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MusicalInstruments
{
    internal class JoyGiver_MusicListen : JoyGiver
    {
        private const float GatherRadius = 3.9f;

        private static readonly int NumRadiusCells = GenRadial.NumCellsInRadius(GatherRadius);

        private static readonly List<IntVec3> RadialPatternMiddleOutward =
            (from c in GenRadial.RadialPattern.Take(NumRadiusCells)
                orderby Mathf.Abs((c - IntVec3.Zero).LengthHorizontal - 1.95f)
                select c).ToList();


        private static List<CompMusicSpot> workingSpots = new List<CompMusicSpot>();

        public override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJobInt(pawn, null);
        }

        public override Job TryGiveJobInGatheringArea(Pawn pawn, IntVec3 gatheringSpot, float maxRadius = -1f)
        {
            return TryGiveJobInt(pawn,
                x => GatheringsUtility.InGatheringArea(x.parent.Position, gatheringSpot, pawn.Map));
        }

        public override Job TryGiveJobWhileInBed(Pawn pawn)
        {
#if DEBUG
            Verse.Log.Message("TryGiveJobWhileInBed " + pawn.Label);
#endif

            var room = pawn.GetRoom();
            var pm = pawn.Map.GetComponent<PerformanceManager>();

            // get music spots in pawn's current room
            var localMusicSpots = pm.ListActiveMusicSpots().Where(x => room.Cells.Contains(x.parent.Position)).ToList();

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
            while (workingSpots.TryRandomElement(out var CompMusicSpot))
            {
                workingSpots.Remove(CompMusicSpot);

                // is a performance currently in progress
                if (!pawn.Map.GetComponent<PerformanceManager>().HasPerformance(CompMusicSpot.parent))
                {
                    continue;
                }
#if DEBUG
                    Verse.Log.Message("Found performance");
#endif

                var job = new Job(def.jobDef, CompMusicSpot.parent, pawn.CurrentBed());
                return job;
            }

            return null;
        }

        private Job TryGiveJobInt(Pawn pawn, Predicate<CompMusicSpot> musicSpotValidator)
        {
#if DEBUG
            Verse.Log.Message(string.Format("{0} trying to listen to music", pawn.LabelShort));
#endif

            var pm = pawn.Map.GetComponent<PerformanceManager>();

            // if no music spots then give up
            if (pm.ListActiveMusicSpots().Count == 0)
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
                workingSpots.Remove(CompMusicSpot);
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

                // is a performance currently in progress
                if (!pawn.Map.GetComponent<PerformanceManager>()
                    .HasPerformance(CompMusicSpot.parent))
                {
                    continue;
                }
#if DEBUG
                                        Verse.Log.Message("Found performance to listen to");
#endif
                // find a place to sit or stand, or return null if there aren't any                                  


                Job job;

                if (pm.TryFindChairNear(CompMusicSpot, pawn, out var chair))
                {
#if DEBUG
                                            Verse.Log.Message("Found chair");
#endif
                    job = new Job(def.jobDef, CompMusicSpot.parent, chair);
                    return job;
                }

                if (!pm.TryFindSitSpotOnGroundNear(CompMusicSpot, pawn, out var standingSpot))
                {
                    return null;
                }
#if DEBUG
                                            Verse.Log.Message("Found standing spot");
#endif
                job = new Job(def.jobDef, CompMusicSpot.parent, standingSpot);
                return job;
#if DEBUG
                                            Verse.Log.Message("Failed to find chair or standing spot");
#endif
            }

#if DEBUG
            Verse.Log.Message("Failed to find performance");
#endif
            return null;
        }
    }
}