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

        [TweakValue("MusicalInstruments.MinTicksBetweenWorkPerfomances", 0, 60000)]
        private static int MinTicksBetweenWorkPerfomances = 30000;

        private static readonly List<ThingDef> allInstrumentDefs = ThingCategoryDef.Named("MusicalInstruments").childThingDefs;  

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

        public static bool RadiusAndRoomCheck(Thing thing1, Thing thing2)
        {

            if (thing1 == null || thing2 == null)
                return false;

            Room room1 = thing1.GetRoom();
            Room room2 = thing2.GetRoom();

            if (room1 == null || room2 == null)
                return false;

            return (room1.GetHashCode() == room2.GetHashCode() &&
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


        private IEnumerable<Thing> AvailableMapInstruments(Pawn musician, Thing venue, bool buildingOnly = false)
        {
            IEnumerable<Thing> instruments = allInstrumentDefs.SelectMany(x => map.listerThings.ThingsOfDef(x))
                                                              .Where(x => x.TryGetComp<CompPowerTrader>() == null || x.TryGetComp<CompPowerTrader>().PowerOn);

            if (buildingOnly)
                instruments = instruments.Where(x => x.TryGetComp<CompMusicalInstrument>().Props.isBuilding);

            if (venue != null)
                instruments = instruments.Where(x => !x.TryGetComp<CompMusicalInstrument>().Props.isBuilding || RadiusAndRoomCheck(x, venue));

            if(musician != null)
                instruments = instruments.Where(x => musician.CanReserveAndReach(x, PathEndMode.Touch, Danger.None))
                                         .Where(x => !x.IsForbidden(musician))
                                         .OrderByDescending(x => x.TryGetComp<CompMusicalInstrument>().WeightedSuitability(musician.skills.GetSkill(SkillDefOf.Artistic).Level))
                                         .ThenByDescending(x => x.TryGetComp<CompQuality>().Quality);

            return instruments;
        }

        public bool MusicJoyKindAvailable(out Thing exampleInstrument)
        {
            exampleInstrument = null;
            if (!ActiveMusicSpots.Any()) return false;

            IEnumerable<Thing> allInstruments = AvailableMapInstruments(null, null);

            if (allInstruments.Any())
            {
                exampleInstrument = allInstruments.First();
                return true;
            }

            foreach(Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                    pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) &&
                    !pawn.story.WorkTypeIsDisabled(JoyGiver_MusicPlay.Art))
                {
                    Thing heldInstrument = HeldInstrument(pawn);
                    if (heldInstrument != null)
                    {
                        exampleInstrument = heldInstrument;
                        return true;
                    }
                }                        
            }

            return false;
        }


        public bool AnyAvailableMapInstruments(Pawn musician, Thing venue)
        {
            return AvailableMapInstruments(musician, venue).Any();
        }

        public Thing HeldInstrument(Pawn musician)
        {
            if (musician.carryTracker.CarriedThing != null && IsInstrument(musician.carryTracker.CarriedThing))
                return musician.carryTracker.CarriedThing;

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
            return (ticksSince >= MinTicksBetweenWorkPerfomances);
        }

        public void StartPlaying(Pawn musician, Thing instrument, Thing venue, bool isWork)
        {
            int venueHash = venue.GetHashCode();

            foreach(int otherVenueHash in Performances.Keys)
            {
                if(RadiusAndRoomCheck(Performances[otherVenueHash].Venue, venue))
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

#if DEBUG
            Verse.Log.Message(String.Format("StopPlaying: Musician: {0}, Venue: {1}. {2} performances currently", musician.LabelShort, venue.LabelShort, Performances.Count));
#endif

//            if (Performances.ContainsKey(venueHash))
//            {
//                if (Performances[venueHash].Performers.ContainsKey(musicianHash))
//                {
//                    Performances[venueHash].Performers.Remove(musicianHash);

//                    if (!Performances[venueHash].Performers.Any())
//                        Performances.Remove(venueHash);
//                    else
//                        Performances[venueHash].CalculateQuality();
//                }

//#if DEBUG
//                Verse.Log.Message(String.Format("Done, now ({0}) performances on map.", Performances.Count));
//#endif

//            }
//            else
//            {

//#if DEBUG
//                Verse.Log.Message("StopPlaying continuing for all performances on map...");
//#endif

            List<int> ongoingPerformanceHashes = new List<int>();

            foreach (int otherVenueHash in Performances.Keys)
            {
                if (Performances[otherVenueHash].Performers.Keys.Contains(musicianHash))
                {
                    Performances[otherVenueHash].Performers.Remove(musicianHash);
                    if (Performances[otherVenueHash].Performers.Any())
                    {
                        Performances[otherVenueHash].CalculateQuality();
                        ongoingPerformanceHashes.Add(otherVenueHash);
                    }
                }
            }

            Performances = Performances.Where(x => ongoingPerformanceHashes.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);

#if DEBUG
            Verse.Log.Message(String.Format("Done, now ({0}) performances on map.", Performances.Count));
#endif

     //       }
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

            IEnumerable<Pawn> audience = map.mapPawns.AllPawnsSpawned.Where(x => x.RaceProps.Humanlike &&
                                                                                 ((x.Faction != null && x.Faction.IsPlayer) || (x.HostFaction != null && x.HostFaction.IsPlayer)) &&
                                                                                 x.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) && 
                                                                                 RadiusAndRoomCheck(venue, x));
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

            Verse.Log.Message(String.Format("Giving memory of {0} to {1} pawns", thought.stages[0].label, audience.Count()));

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

            //                                                                          visitors only play their own instruments, unless building type

            IEnumerable<Thing> mapInstruments = AvailableMapInstruments(musician, venue, (musician.Faction == null || !musician.Faction.IsPlayer));

            

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

            float buildingSwapModifier = bestInstrument.TryGetComp<CompMusicalInstrument>().Props.isBuilding ? .2f : 0f;

            float rand = Verse.Rand.Range(0f, 1f);

            if (rand > .9f - buildingSwapModifier)
            {
                swap = true;
            }
            else if (rand > .4f - buildingSwapModifier)
            {
                swap = heldInstrument.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill) < bestInstrument.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill);
            }

            instrument = swap ? bestInstrument : heldInstrument;

            return true;
        }

        public bool TryFindStandingSpotOrChair(CompMusicSpot musicSpot, Pawn musician, Thing instrument, out LocalTargetInfo target)
        {
            IntVec3 standingSpot;
            Thing chair;

            target = null;

            PerformanceManager pm = musician.Map.GetComponent<PerformanceManager>();

            CompMusicalInstrument comp = instrument.TryGetComp<CompMusicalInstrument>();

            if (comp.Props.isBuilding)
            {
                if (pm.TryFindChairAt(comp, musician, out chair))
                {
                    target = chair;
                    return true;
                }
                else if (pm.TryFindSpotAt(comp, musician, out standingSpot))
                {
                    target = standingSpot;
                    return true;
                }
            }
            else
            {
                if (pm.TryFindSitSpotOnGroundNear(musicSpot, musician, out standingSpot))
                {
                    target = standingSpot;
                    return true;
                }
            }

            return false;
        }

        public bool TryFindSitSpotOnGroundNear(CompMusicSpot spot, Pawn sitter, out IntVec3 result)
        {
            IntVec3 center = spot.parent.Position;

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

        public bool TryFindChairNear(CompMusicSpot spot, Pawn sitter, out Thing chair)
        {
            IntVec3 center = spot.parent.Position;

            CompMusicalInstrument comp = spot.parent.TryGetComp<CompMusicalInstrument>();

            for (int i = 0; i < RadialPatternMiddleOutward.Count; i++)
            {
                IntVec3 c =  center + RadialPatternMiddleOutward[i];

                if (comp != null && comp.Props.isBuilding && c == comp.parent.InteractionCell)
                    continue;

                Building edifice = c.GetEdifice(map);
                if (edifice != null && edifice.def.building.isSittable && sitter.CanReserveAndReach(edifice, PathEndMode.OnCell, Danger.None) && !edifice.IsForbidden(sitter) && GenSight.LineOfSight(center, edifice.Position, map, skipFirstCell: true))
                {
                    chair = edifice;
                    return true;
                }
            }
            chair = null;
            return false;
        }

        public bool TryFindChairAt(CompMusicalInstrument instrument, Pawn sitter, out Thing chair)
        {
            chair = null;

            if (!instrument.Props.isBuilding) return false;

            Building edifice = instrument.parent.InteractionCell.GetEdifice(map);
            if (edifice != null && edifice.def.building.isSittable && sitter.CanReserveAndReach(edifice, PathEndMode.OnCell, Danger.None) && !edifice.IsForbidden(sitter))
            {
                chair = edifice;
                return true;
            }

            return false;

        }

        public bool TryFindSpotAt(CompMusicalInstrument instrument, Pawn sitter, out IntVec3 result)
        {
            result = IntVec3.Invalid;

            if (!instrument.Props.isBuilding) return false;

            IntVec3 intVec = instrument.parent.InteractionCell;

            if (sitter.CanReserveAndReach(intVec, PathEndMode.OnCell, Danger.None, 1, -1, null, false) && intVec.GetEdifice(map) == null)
            {
                result = intVec;
                return true;
            }

            return false;

        }

    }
}
