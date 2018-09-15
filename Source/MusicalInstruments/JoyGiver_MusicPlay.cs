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
                                    //Thing chair;

                                    //if (compGatherSpot.parent.def.surfaceType == SurfaceType.Eat) {

                                    //    Thing chairByTable;
                                    //    if (!JoyGiver_MusicPlay.TryFindChairBesideTable(compGatherSpot.parent, pawn, out chairByTable)) {
                                    //        return null;
                                    //    }
                                    //    job = new Job(this.def.jobDef, compGatherSpot.parent, chairByTable);
                                    //} else if (JoyGiver_MusicPlay.TryFindChairNear(compGatherSpot.parent.Position, pawn, out chair)) {

                                    //    job = new Job(this.def.jobDef, compGatherSpot.parent, chair);
                                    //} else {
                                    IntVec3 standingSpot;
                                    if (!JoyGiver_MusicPlay.TryFindSitSpotOnGroundNear(compGatherSpot.parent.Position, pawn, out standingSpot))
                                    {
                                        return null;
                                    }
                                    job = new Job(this.def.jobDef, compGatherSpot.parent, standingSpot);
                                    //}

                                    Thing instrument;

                                    if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.story.WorkTypeIsDisabled(art) &&
                                        JoyGiver_MusicPlay.TryFindInstrumentToPlay(compGatherSpot.parent.Position, pawn, out instrument))
                                    {

                                        job.targetC = instrument;
                                        //job.count = Mathf.Min(drug.stackCount, drug.def.ingestible.maxNumToIngestAtOnce);
                                    }
                                    else return null;

                                    // also try to find some drugs to take

                                    //Thing drug;

                                    //if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && 
                                    //    JoyGiver_MusicPlay.TryFindIngestibleToNurse(compGatherSpot.parent.Position, pawn, out drug)) {

                                    //    job.targetC = drug;
                                    //    job.count = Mathf.Min(drug.stackCount, drug.def.ingestible.maxNumToIngestAtOnce);
                                    //}

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

        
        private static bool TryFindIngestibleToNurse(IntVec3 center, Pawn ingester, out Thing ingestible)
        {
            if (ingester.IsTeetotaler())
            {
                ingestible = null;
                return false;
            }
            if (ingester.drugs == null)
            {
                ingestible = null;
                return false;
            }
            JoyGiver_MusicPlay.nurseableDrugs.Clear();
            DrugPolicy currentPolicy = ingester.drugs.CurrentPolicy;
            for (int i = 0; i < currentPolicy.Count; i++)
            {
                if (currentPolicy[i].allowedForJoy && currentPolicy[i].drug.ingestible.nurseable)
                {
                    JoyGiver_MusicPlay.nurseableDrugs.Add(currentPolicy[i].drug);
                }
            }
            JoyGiver_MusicPlay.nurseableDrugs.Shuffle<ThingDef>();
            for (int j = 0; j < JoyGiver_MusicPlay.nurseableDrugs.Count; j++)
            {
                List<Thing> list = ingester.Map.listerThings.ThingsOfDef(JoyGiver_MusicPlay.nurseableDrugs[j]);
                if (list.Count > 0)
                {
                    Predicate<Thing> validator = (Thing t) => ingester.CanReserve(t, 1, -1, null, false) && !t.IsForbidden(ingester);
                    ingestible = GenClosest.ClosestThing_Global_Reachable(center, ingester.Map, list, PathEndMode.OnCell, TraverseParms.For(ingester, Danger.Deadly, TraverseMode.ByPawn, false), 40f, validator, null);
                    if (ingestible != null)
                    {
                        return true;
                    }
                }
            }
            ingestible = null;
            return false;
        }

        private static bool TryFindChairBesideTable(Thing table, Pawn sitter, out Thing chair)
        {
            for (int i = 0; i < 30; i++)
            {
                IntVec3 c = table.RandomAdjacentCellCardinal();
                Building edifice = c.GetEdifice(table.Map);
                if (edifice != null && edifice.def.building.isSittable && sitter.CanReserve(edifice, 1, -1, null, false))
                {
                    chair = edifice;
                    return true;
                }
            }
            chair = null;
            return false;
        }

        private static bool TryFindChairNear(IntVec3 center, Pawn sitter, out Thing chair)
        {
            for (int i = 0; i < JoyGiver_MusicPlay.RadialPatternMiddleOutward.Count; i++)
            {
                IntVec3 c = center + JoyGiver_MusicPlay.RadialPatternMiddleOutward[i];
                Building edifice = c.GetEdifice(sitter.Map);
                if (edifice != null && edifice.def.building.isSittable && sitter.CanReserve(edifice, 1, -1, null, false) && !edifice.IsForbidden(sitter) && GenSight.LineOfSight(center, edifice.Position, sitter.Map, true, null, 0, 0))
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
