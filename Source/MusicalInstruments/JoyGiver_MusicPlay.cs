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

        private static List<CompGatherSpot> workingSpots = new List<CompGatherSpot>();


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
            //quit if no available instrument / quit roll for low skill without instrument
            PerformanceManager pm = pawn.Map.GetComponent<PerformanceManager>();
            int skill = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;

            if (pm.HeldInstrument(pawn) == null && (!pm.AnyAvailableMapInstruments(pawn) || (skill < 3 && Verse.Rand.Chance(.75f))))
                return null;



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
                                    if (!PerformanceManager.TryFindSitSpotOnGroundNear(compGatherSpot.parent.Position, pawn, out standingSpot))
                                    {
                                        return null;
                                    }
                                    job = new Job(this.def.jobDef, compGatherSpot.parent, standingSpot);
                    
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
                    }
                }
            }
            return null;
        }


    }
}
