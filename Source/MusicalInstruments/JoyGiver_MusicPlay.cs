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


        private static readonly WorkTypeDef art = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Where(wtd => wtd.defName == "Art").SingleOrDefault();

        private static List<CompMusicSpot> workingSpots = new List<CompMusicSpot>();


        public override Job TryGiveJob(Pawn pawn)
        {
            return this.TryGiveJobInt(pawn, null);
        }

        public override Job TryGiveJobInPartyArea(Pawn pawn, IntVec3 partySpot)
        {
            return this.TryGiveJobInt(pawn, (CompMusicSpot x) => PartyUtility.InPartyArea(x.parent.Position, partySpot, pawn.Map));
        }

        private Job TryGiveJobInt(Pawn pawn, Predicate<CompMusicSpot> musicSpotValidator)
        {
            //quit if no available instrument / quit roll for low skill without instrument
            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();
            int skill = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;

            if (pm.HeldInstrument(pawn) == null && (!pm.AnyAvailableMapInstruments(pawn) || (skill < 3 && Verse.Rand.Chance(.75f))))
                return null;



            // if no music spots then give up
            if (pm.ListActiveMusicSpots().Count == 0)
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
            CompMusicSpot CompMusicSpot;
            while (JoyGiver_MusicPlay.workingSpots.TryRandomElement(out CompMusicSpot))
            {
                // remove from list
                JoyGiver_MusicPlay.workingSpots.Remove(CompMusicSpot);
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
                                    // find a place to sit or stand, or return null if there aren't any

                                    Job job;
                                
                                    IntVec3 standingSpot;
                                    if (!pm.TryFindSitSpotOnGroundNear(CompMusicSpot.parent.Position, pawn, out standingSpot))
                                    {
                                        return null;
                                    }
                                    job = new Job(this.def.jobDef, CompMusicSpot.parent, standingSpot);
                    
                                    Thing instrument;

                                    if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.story.WorkTypeIsDisabled(art) &&
                                        pm.TryFindInstrumentToPlay(CompMusicSpot.parent, pawn, out instrument))
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


    }
}
