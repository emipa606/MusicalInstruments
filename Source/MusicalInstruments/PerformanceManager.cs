﻿using System;
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
            ThingDef.Named("Violin"),
            ThingDef.Named("ElectronicOrgan")
        };


        private const float Radius = 9f;
        private const float GatherRadius = 3.9f;

        private static readonly int NumRadiusCells = GenRadial.NumCellsInRadius(GatherRadius);

        private static readonly List<IntVec3> RadialPatternMiddleOutward = (from c in GenRadial.RadialPattern.Take(NumRadiusCells)
                                                                            orderby Mathf.Abs((c - IntVec3.Zero).LengthHorizontal - 1.95f)
                                                                            select c).ToList<IntVec3>();




        private static string LogMusician(Pawn musician, Thing instrument)
        {
            return String.Format("{0}({1} skill) on {2}", musician.LabelShort, musician.skills.GetSkill(SkillDefOf.Artistic).Level, instrument.LabelShort);
        }

        public static bool IsInstrument(Thing thing)
        {
            return allInstrumentDefs.Contains(thing.def);

        }

        public static bool RadiusCheck(Thing thing1, Thing thing2)
        {
            return (thing1.GetRoom().GetHashCode() == thing2.GetRoom().GetHashCode() &&
                    thing1.Position.DistanceTo(thing2.Position) < Radius);

        }

   
        // non-static

        private Dictionary<int, Performance> Performances;
        private Dictionary<int, int> WorkPerformanceTimestamps;
        private List<CompMusicSpot> ActiveMusicSpots;   //don't need to serialize this as the CompMusicSpot automatically registers/deregisters itself

        public PerformanceManager(Map map) : base(map)
        {
            Performances = new Dictionary<int, Performance>();
            WorkPerformanceTimestamps = new Dictionary<int, int>();
            ActiveMusicSpots = new List<CompMusicSpot>();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look<int, Performance>(ref Performances, "MusicalInstruments.Performances", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look<int, int>(ref WorkPerformanceTimestamps, "MusicalInstruments.WorkPerformanceTimestamps", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                //try to avoid breaking saves from old versions
                if (Performances == null)
                    Performances = new Dictionary<int, Performance>();
                if (WorkPerformanceTimestamps == null)
                    WorkPerformanceTimestamps = new Dictionary<int, int>();
            }

        }


        private IEnumerable<Thing> AvailableMapInstruments(Pawn musician, Thing venue)
        {
            int skill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;

            return allInstrumentDefs.SelectMany(x => map.listerThings.ThingsOfDef(x))
                                    .Where(x => musician.CanReserveAndReach(x, PathEndMode.Touch, Danger.None))
                                    .Where(x => !x.IsForbidden(musician))
                                    .Where(x => x.TryGetComp<CompPowerTrader>() == null || x.TryGetComp<CompPowerTrader>().PowerOn)
                                    .Where(x => !x.TryGetComp<CompMusicalInstrument>().Props.isBuilding || RadiusCheck(x, venue))
                                    .OrderByDescending(x => x.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill))
                                    .ThenByDescending(x => x.TryGetComp<CompQuality>().Quality);
        }


        public bool AnyAvailableMapInstruments(Pawn musician, Thing venue)
        {
            return AvailableMapInstruments(musician, venue).Any();
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

        public List<CompMusicSpot> ListActiveMusicSpots()
        {
            return ActiveMusicSpots;
        }

        public void RegisterActivatedMusicSpot(CompMusicSpot spot)
        {
            if (!ActiveMusicSpots.Contains(spot))
                ActiveMusicSpots.Add(spot);
            //Verse.Log.Message(String.Format("{0} activated, count={1}", spot.parent.Label, ActiveMusicSpots.Count));
        }

        public void RegisterDeactivatedMusicSpot(CompMusicSpot spot)
        {
            if (ActiveMusicSpots.Contains(spot))
                ActiveMusicSpots.Remove(spot);
            //Verse.Log.Message(String.Format("{0} deactivated, count={1}", spot.parent.Label, ActiveMusicSpots.Count));
        }

        public bool CanPlayForWorkNow(Pawn musician)
        {
            int hash = musician.GetHashCode();
            if (!WorkPerformanceTimestamps.ContainsKey(hash)) return true;
            int ticksSince = Find.TickManager.TicksGame - WorkPerformanceTimestamps[hash];
            return (ticksSince >= 30000);
        }

        public void StartPlaying(Pawn musician, Thing instrument, Thing venue, bool isWork)
        {
            int venueHash = venue.GetHashCode();

            foreach(int otherVenueHash in Performances.Keys)
            {
                if(RadiusCheck(Performances[otherVenueHash].Venue, venue))
                {
                    venueHash = otherVenueHash;
                    break;
                }
            }

            if (!Performances.ContainsKey(venueHash))
                Performances[venueHash] = new Performance(venue);

            int musicianHash = musician.GetHashCode();

            if (!Performances[venueHash].Performers.ContainsKey(musicianHash))
            {
                Performances[venueHash].Performers[musicianHash] = new Performer() { Musician = musician, Instrument = instrument };
                Performances[venueHash].CalculateQuality();

                if (isWork)
                    WorkPerformanceTimestamps[musician.GetHashCode()] = Find.TickManager.TicksGame;

            }

#if DEBUG

            Verse.Log.Message(String.Format("Musicians: {0}", String.Join(", ", Performances[venueHash].Performers.Select(x => LogMusician(x.Value.Musician, x.Value.Instrument)).ToArray())));
            Verse.Log.Message(String.Format("Quality: {0}", Performances[venueHash].Quality));

#endif
        }

        public void StopPlaying(Pawn musician, Thing venue)
        {
            int venueHash = venue.GetHashCode();
            int musicianHash = musician.GetHashCode();

            if (Performances.ContainsKey(venueHash))
            {
                if (Performances[venueHash].Performers.ContainsKey(musicianHash))
                {
                    Performances[venueHash].Performers.Remove(musicianHash);

                    if (!Performances[venueHash].Performers.Any())
                        Performances.Remove(venueHash);
                    else
                        Performances[venueHash].CalculateQuality();
                }
            }
            else
            {
                foreach (int otherVenueHash in Performances.Keys)
                {
                    if (Performances[otherVenueHash].Performers.Keys.Contains(musicianHash))
                    {
                        Performances[otherVenueHash].Performers.Remove(musicianHash);
                        Performances[otherVenueHash].CalculateQuality();
                    }
                }
            }
        }

        public bool HasPerformance(Thing venue)
        {
            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash))
                return false;
            return Performances[hash].Performers.Any();
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

            //IntVec3 centre = venue.Position;
            //int roomHash = venue.GetRoom().GetHashCode();

            List<Pawn> audience = venue.Map.mapPawns.FreeColonistsAndPrisoners.Where(x => RadiusCheck(venue, x)  && x.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)).ToList();

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
        
        public bool TryFindInstrumentToPlay(Thing venue, Pawn musician, out Thing instrument, bool isWork = false)
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


            IEnumerable<Thing> mapInstruments = AvailableMapInstruments(musician, venue);

            

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



        public bool TryFindSitSpotOnGroundNear(IntVec3 center, Pawn sitter, out IntVec3 result)
        {
            for (int i = 0; i < 30; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[Rand.Range(1, NumRadiusCells)];
                if (sitter.CanReserveAndReach(intVec, PathEndMode.OnCell, Danger.None, 1, -1, null, false) && intVec.GetEdifice(map) == null && GenSight.LineOfSight(center, intVec, map, true, null, 0, 0))
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
