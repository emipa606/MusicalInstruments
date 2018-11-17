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
    public class PerformanceManager : MapComponent
    {
        // static

        private static readonly List<ThingDef> allInstrumentDefs = new List<ThingDef> {
            ThingDef.Named("FrameDrum"),
            ThingDef.Named("Ocarina"),
            ThingDef.Named("Guitar"),
            ThingDef.Named("Violin")
        };


        private const float Radius = 9f;
        private const float GatherRadius = 3.9f;

        private static readonly int NumRadiusCells = GenRadial.NumCellsInRadius(GatherRadius);

        private static readonly List<IntVec3> RadialPatternMiddleOutward = (from c in GenRadial.RadialPattern.Take(NumRadiusCells)
                                                                            orderby Mathf.Abs((c - IntVec3.Zero).LengthHorizontal - 1.95f)
                                                                            select c).ToList<IntVec3>();




        private static string LogMusician(Pawn musician)
        {
            return String.Format("{0}({1} skill) on {2}", musician.LabelShort, musician.skills.GetSkill(SkillDefOf.Artistic).Level, musician.carryTracker.CarriedThing.LabelShort);
        }

        public static bool IsInstrument(Thing thing)
        {
            return allInstrumentDefs.Contains(thing.def);

        }



        public static bool TryFindInstrumentToPlay(IntVec3 center, Pawn musician, out Thing instrument, bool isWork = false)
        {
            instrument = null;

            Thing heldInstrument = null;

            foreach (Thing inventoryThing in musician.inventory.innerContainer)
            {
                if (IsInstrument(inventoryThing))
                {
                    heldInstrument = inventoryThing;
                    break;
                }
            }

            int skill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;


            //if (!isWork && heldInstrument == null && skill < 3 && Verse.Rand.Chance(0.75f))
            //    return false;

            IEnumerable<Thing> mapInstruments = allInstrumentDefs.SelectMany(x => musician.Map.listerThings.ThingsOfDef(x))
                                                        .OrderByDescending(x => x.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill))
                                                        .ThenByDescending(x => x.TryGetComp<CompQuality>().Quality);

            if (!mapInstruments.Any())
            {
                instrument = heldInstrument;
                return (instrument != null);
            }

            Thing bestInstrument = mapInstruments.First();

            if (heldInstrument == null)
            {
                instrument = bestInstrument;
                return true;
            }

            bool swap = false;

            float rand = Verse.Rand.Range(0f, 1f);

            if (rand > .9f)
            {
                swap = true;
            }
            else if (rand > 0.4f)
            {
                swap = heldInstrument.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill) < bestInstrument.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill);
            }

            instrument = swap ? bestInstrument : heldInstrument;

            return true;
        }



        public static bool TryFindSitSpotOnGroundNear(IntVec3 center, Pawn sitter, out IntVec3 result)
        {
            for (int i = 0; i < 30; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[Rand.Range(1, NumRadiusCells)];
                if (sitter.CanReserveAndReach(intVec, PathEndMode.OnCell, Danger.None, 1, -1, null, false) && intVec.GetEdifice(sitter.Map) == null && GenSight.LineOfSight(center, intVec, sitter.Map, true, null, 0, 0))
                {
                    result = intVec;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            return false;
        }

        // non-static

        private Dictionary<int, Performance> Performances;
        private Dictionary<int, int> WorkPerformanceTimestamps;

        public PerformanceManager(Map map) : base(map)
        {
            Performances = new Dictionary<int, Performance>();
            WorkPerformanceTimestamps = new Dictionary<int, int>();
        }

        public bool AnyMapInstruments()
        {
            return allInstrumentDefs.SelectMany(x => map.listerThings.ThingsOfDef(x)).Any();
        }

        public Thing HeldInstrument(Pawn musician)
        {
            foreach (Thing inventoryThing in musician.inventory.innerContainer)
            {
                if (IsInstrument(inventoryThing))
                {
                    return inventoryThing;
                }
            }

            return null;
        }

        public bool CanPlayForWorkNow(Pawn musician)
        {
            int hash = musician.GetHashCode();
            if (!WorkPerformanceTimestamps.ContainsKey(hash)) return true;
            int ticksSince = Find.TickManager.TicksGame - WorkPerformanceTimestamps[hash];
            return (ticksSince >= 30000);
        }

        public void StartPlaying(Pawn musician, Thing venue, bool isWork)
        {
            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash))
                Performances[hash] = new Performance() { Venue = venue, Musicians = new List<Pawn>(), Quality = 0f };

            Performances[hash].Musicians.Add(musician);
            Performances[hash].CalculateQuality();

            //ApplyThoughts(venue);

            if (isWork)
                WorkPerformanceTimestamps[musician.GetHashCode()] = Find.TickManager.TicksGame;

#if DEBUG

            Verse.Log.Message(String.Format("Musicians: {0}", String.Join(", ", Performances[hash].Musicians.Select(x => LogMusician(x)).ToArray())));
            Verse.Log.Message(String.Format("Quality: {0}", Performances[hash].Quality));

#endif
        }

        public void StopPlaying(Pawn musician, Thing venue)
        {
            int hash = venue.GetHashCode();

            //ApplyThoughts(venue);

            if (Performances.ContainsKey(hash))
            {
                if(Performances[hash].Musicians.Contains(musician))
                    Performances[hash].Musicians.Remove(musician);
                Performances[hash].CalculateQuality();
            }
        }

        public bool HasPerformance(Thing venue)
        {
            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash))
                return false;
            return Performances[hash].Musicians.Any();
        }

        public float GetPerformanceQuality(Thing venue)
        {

            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash))
            {
                //Verse.Log.Error(String.Format("Gather spot #{0} has no performance.", hash));
                return 0f;
            }
            else
            {
                //Verse.Log.Error(String.Format("Performance quality of gather spot #{0} = {1}.", hash, performances[hash].Quality));
                return Performances[hash].Quality;
            }


        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 100 == 99)
            {
                foreach(int hash in Performances.Keys)
                {
                    ApplyThoughts(Performances[hash].Venue);
                }
            }
        }

        public void ApplyThoughts(Thing venue)
        {
            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash)) return;

            float quality = Performances[hash].Quality;

            if (quality >= 0f && quality < .5f) return;

            IntVec3 centre = venue.Position;
            int roomHash = venue.GetRoom().GetHashCode();

            List<Pawn> audience = venue.Map.mapPawns.FreeColonistsAndPrisoners.Where(x => centre.DistanceTo(x.Position) < Radius && roomHash == x.GetRoom().GetHashCode() && x.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)).ToList();

            if (!audience.Any()) return;

            ThoughtDef thought;
            if (quality < 0f)
            {
                thought = ThoughtDef.Named("BadMusic");
            }
            else if (quality >= 2f)
            {
                thought = ThoughtDef.Named("GreatMusic");
            }
            else
            {
                thought = ThoughtDef.Named("NiceMusic");
            };

#if DEBUG

            Verse.Log.Message(String.Format("Giving memory of {0} to {1} pawns", thought.stages[0].label, audience.Count));

#endif

            foreach (Pawn audienceMember in audience)
            {
                audienceMember.needs.mood.thoughts.memories.TryGainMemory(thought);
            }


        }
    }
}
