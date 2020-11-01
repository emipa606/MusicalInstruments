using System;
using System.Collections.Generic;

using Verse;
using Verse.AI;

using RimWorld;

namespace MusicalInstruments
{
    class JoyGiver_MusicPlay : JoyGiver
    {

        private static readonly List<CompMusicSpot> workingSpots = new List<CompMusicSpot>();

        public override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJobInt(pawn, null);
        }

        public override Job TryGiveJobInGatheringArea(Pawn pawn, IntVec3 gatherSpot)
        {
            return TryGiveJobInt(pawn, (CompMusicSpot x) => GatheringsUtility.InGatheringArea(x.parent.Position, gatherSpot, pawn.Map));
        }

        private Job TryGiveJobInt(Pawn pawn, Predicate<CompMusicSpot> musicSpotValidator)
        {
            //quit roll for low skill without instrument
            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();
            int skill = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;

            if (PerformanceManager.HeldInstrument(pawn) == null && skill < 3 && Verse.Rand.Chance(.75f))
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
            JoyGiver_MusicPlay.workingSpots.Clear();
            for (int i = 0; i < pm.ListActiveMusicSpots().Count; i++)
            {
                JoyGiver_MusicPlay.workingSpots.Add(pm.ListActiveMusicSpots()[i]);
            }

            // pick a random one
            while (JoyGiver_MusicPlay.workingSpots.TryRandomElement(out CompMusicSpot CompMusicSpot))
            {
                // remove from list
                _ = JoyGiver_MusicPlay.workingSpots.Remove(CompMusicSpot);
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

                                    //check for an instrument
                                    if (PerformanceManager.HeldInstrument(pawn) != null || pm.AnyAvailableMapInstruments(pawn, CompMusicSpot.parent))
                                    {

                                        if (pm.TryFindInstrumentToPlay(CompMusicSpot.parent, pawn, out Thing instrument))
                                        {
                                            // find a place to sit or stand, or return null if there aren't any

                                            if (pm.TryFindStandingSpotOrChair(CompMusicSpot, pawn, instrument, out LocalTargetInfo chairOrSpot))
                                            {
                                                Job job = new Job(def.jobDef, CompMusicSpot.parent, chairOrSpot, instrument)
                                                {
                                                    count = 1
                                                };

                                                return job;

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }


    }
}
