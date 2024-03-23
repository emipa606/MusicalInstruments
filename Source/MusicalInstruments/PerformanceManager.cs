using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MusicalInstruments;

public class PerformanceManager(Map map) : MapComponent(map)
{
    private const float Radius = 9f;

    private const float GatherRadius = 3.9f;
    // static

    private static readonly int MinTicksBetweenWorkPerfomances = 30000;

    private static readonly List<ThingDef> allInstrumentDefs =
        ThingCategoryDef.Named("MusicalInstruments").childThingDefs;

    private static readonly int NumRadiusCells = GenRadial.NumCellsInRadius(GatherRadius);

    private static readonly List<IntVec3> RadialPatternMiddleOutward =
        (from c in GenRadial.RadialPattern.Take(NumRadiusCells)
            orderby Mathf.Abs((c - IntVec3.Zero).LengthHorizontal - 1.95f)
            select c).ToList();

    private readonly List<CompMusicSpot>
        ActiveMusicSpots =
            []; //don't need to serialize this as the CompMusicSpot automatically registers/deregisters itself

    // non-static

    private Dictionary<int, Performance> Performances = new Dictionary<int, Performance>();
    private Dictionary<int, int> WorkPerformanceTimestamps = new Dictionary<int, int>();


    private static string LogMusician(Pawn musician, Thing instrument)
    {
        return
            $"{musician.LabelShort}({musician.skills.GetSkill(SkillDefOf.Artistic).Level} skill) on {instrument.LabelShort}";
    }

    public static bool IsInstrument(Thing thing)
    {
        return allInstrumentDefs.Contains(thing.def);
    }

    public static Thing HeldInstrument(Pawn musician)
    {
        if (musician.carryTracker.CarriedThing != null && IsInstrument(musician.carryTracker.CarriedThing))
        {
            return musician.carryTracker.CarriedThing;
        }

        foreach (var inventoryThing in musician.inventory.innerContainer)
        {
            if (IsInstrument(inventoryThing))
            {
                return inventoryThing;
            }
        }

        return null;
    }

    public static bool IsPotentialCaravanMusician(Pawn pawn, out float quality)
    {
        quality = 0;

        if (!pawn.IsColonist)
        {
            return false;
        }

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
            !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) ||
            !pawn.Awake() ||
            pawn.WorkTagIsDisabled(WorkTags.Artistic))
        {
            return false;
        }

        var heldInstrument = HeldInstrument(pawn);

        if (heldInstrument == null)
        {
            return false;
        }

        quality = Performance.GetMusicQuality(pawn, heldInstrument, Rand.Range(-3, 2));

        return true;
    }

    public static bool RadiusAndRoomCheck(Thing thing1, Thing thing2)
    {
        if (thing1 == null || thing2 == null)
        {
            return false;
        }

        var room1 = thing1.GetRoom();
        var room2 = thing2.GetRoom();

        return room1 != null && room2 != null && room1.GetHashCode() == room2.GetHashCode() &&
               thing1.Position.DistanceTo(thing2.Position) < Radius;
    }

    public static ThoughtDef GetThoughtDef(float quality)
    {
        return quality < 0f
            ? ThoughtDef.Named("BadMusic")
            : quality >= 2f
                ? ThoughtDef.Named("GreatMusic")
                : quality >= .5f
                    ? ThoughtDef.Named("NiceMusic")
                    : null;
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref Performances, "MusicalInstruments.Performances", LookMode.Value, LookMode.Deep);
        Scribe_Collections.Look(ref WorkPerformanceTimestamps, "MusicalInstruments.WorkPerformanceTimestamps",
            LookMode.Value, LookMode.Value);

        if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs)
        {
            return;
        }

        //try to avoid breaking saves from old versions
        if (Performances == null)
        {
            Performances = new Dictionary<int, Performance>();
        }

        if (WorkPerformanceTimestamps == null)
        {
            WorkPerformanceTimestamps = new Dictionary<int, int>();
        }
    }


    private IEnumerable<Thing> AvailableMapInstruments(Pawn musician, Thing venue, bool buildingOnly = false,
        bool isWork = false)
    {
        var instruments = allInstrumentDefs.SelectMany(x => map.listerThings.ThingsOfDef(x))
            .Where(x => x.TryGetComp<CompPowerTrader>() == null || x.TryGetComp<CompPowerTrader>().PowerOn);

        if (buildingOnly)
        {
            instruments = instruments.Where(x => x.TryGetComp<CompMusicalInstrument>().Props.isBuilding);
        }

        if (!isWork)
        {
            instruments = instruments.Where(x =>
                !x.TryGetComp<CompMusicalInstrument>().Props.isBuilding ||
                x.TryGetComp<CompMusicSpot>().AllowRecreation);
        }

        if (venue != null)
        {
            instruments = instruments.Where(x =>
                !x.TryGetComp<CompMusicalInstrument>().Props.isBuilding || RadiusAndRoomCheck(x, venue));
        }

        if (musician != null)
        {
            instruments = instruments.Where(x => musician.CanReserveAndReach(x, PathEndMode.Touch, Danger.None))
                .Where(x => !x.IsForbidden(musician))
                .OrderByDescending(x =>
                    x.TryGetComp<CompMusicalInstrument>()
                        .WeightedSuitability(musician.skills.GetSkill(SkillDefOf.Artistic).Level))
                .ThenByDescending(x => x.TryGetComp<CompQuality>().Quality);
        }

        return instruments;
    }

    public bool MusicJoyKindAvailable(out Thing exampleInstrument)
    {
        exampleInstrument = null;
        if (!ActiveMusicSpots.Any())
        {
            return false;
        }

        var allInstruments = AvailableMapInstruments(null, null);

        if (allInstruments.Any())
        {
            exampleInstrument = allInstruments.First();
            return true;
        }

        foreach (var pawn in map.mapPawns.FreeColonists)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) || !pawn.Awake() ||
                pawn.WorkTagIsDisabled(WorkTags.Artistic))
            {
                continue;
            }

            var heldInstrument = HeldInstrument(pawn);
            if (heldInstrument == null)
            {
                continue;
            }

            exampleInstrument = heldInstrument;
            return true;
        }

        return false;
    }


    public bool AnyAvailableMapInstruments(Pawn musician, Thing venue)
    {
        return AvailableMapInstruments(musician, venue).Any();
    }

    public List<CompMusicSpot> ListActiveMusicSpots()
    {
        return ActiveMusicSpots;
    }

    public void RegisterActivatedMusicSpot(CompMusicSpot spot)
    {
        if (!ActiveMusicSpots.Contains(spot))
        {
            ActiveMusicSpots.Add(spot);
        }

        //Verse.Log.Message(String.Format("{0} activated, count={1}", spot.parent.Label, ActiveMusicSpots.Count));
    }

    public void RegisterDeactivatedMusicSpot(CompMusicSpot spot)
    {
        if (ActiveMusicSpots.Contains(spot))
        {
            _ = ActiveMusicSpots.Remove(spot);
        }

        //Verse.Log.Message(String.Format("{0} deactivated, count={1}", spot.parent.Label, ActiveMusicSpots.Count));
    }

    public bool CanPlayForWorkNow(Pawn musician)
    {
        var hash = musician.GetHashCode();
        if (!WorkPerformanceTimestamps.TryGetValue(hash, out var timestamp))
        {
            return true;
        }

        var ticksSince = Find.TickManager.TicksGame - timestamp;
        return ticksSince >= MinTicksBetweenWorkPerfomances;
    }

    public void StartPlaying(Pawn musician, Thing instrument, Thing venue, bool isWork)
    {
        var venueHash = venue.GetHashCode();

        foreach (var otherVenueHash in Performances.Keys)
        {
            if (!RadiusAndRoomCheck(Performances[otherVenueHash].Venue, venue))
            {
                continue;
            }

            venueHash = otherVenueHash;
            break;
        }

        if (!Performances.ContainsKey(venueHash))
        {
            Performances[venueHash] = new Performance(venue);
        }

        var musicianHash = musician.GetHashCode();

        if (Performances[venueHash].Performers.ContainsKey(musicianHash))
        {
            return;
        }

        Performances[venueHash].Performers[musicianHash] =
            new Performer { Musician = musician, Instrument = instrument };
        Performances[venueHash].CalculateQuality();

        if (Performances[venueHash].Quality >= 2f)
        {
            TaleRecorder.RecordTale(TaleDef.Named("PlayedMusic"), musician, instrument.def);
        }

        if (isWork)
        {
            WorkPerformanceTimestamps[musician.GetHashCode()] = Find.TickManager.TicksGame;
        }

#if DEBUG
            Verse.Log.Message(
                $"Musicians: {string.Join(", ", Performances[venueHash].Performers.Select(x => LogMusician(x.Value.Musician, x.Value.Instrument)).ToArray())}");
            Verse.Log.Message($"Quality: {Performances[venueHash].Quality}");
#endif
    }

    public void StopPlaying(Pawn musician, Thing venue)
    {
        _ = venue.GetHashCode();
        var musicianHash = musician.GetHashCode();

#if DEBUG
            Verse.Log.Message(
                $"StopPlaying: Musician: {musician.LabelShort}, Venue: {venue.LabelShort}. {Performances.Count} performances currently");
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

        var ongoingPerformanceHashes = new List<int>();

        foreach (var otherVenueHash in Performances.Keys)
        {
            if (!Performances[otherVenueHash].Performers.Keys.Contains(musicianHash))
            {
                continue;
            }

            Performances[otherVenueHash].Performers.Remove(musicianHash);
            if (!Performances[otherVenueHash].Performers.Any())
            {
                continue;
            }

            Performances[otherVenueHash].CalculateQuality();
            ongoingPerformanceHashes.Add(otherVenueHash);
        }

        Performances = Performances.Where(x => ongoingPerformanceHashes.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

#if DEBUG
            Verse.Log.Message($"Done, now ({Performances.Count}) performances on map.");
#endif

        //       }
    }

    public bool HasPerformance(Thing venue)
    {
        var hash = venue.GetHashCode();

        return Performances.ContainsKey(hash) && Performances[hash].Performers.Any();
    }

    public float GetPerformanceQuality(Thing venue)
    {
        var hash = venue.GetHashCode();

        return !Performances.TryGetValue(hash, out var performance)
            ?
            //Verse.Log.Error(String.Format("Gather spot #{0} has no performance.", hash));
            0f
            : performance.Quality;
    }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % 100 != 99)
        {
            return;
        }

        foreach (var hash in Performances.Keys)
        {
            ApplyThoughts(Performances[hash].Venue);
        }
    }

    public void ApplyThoughts(Thing venue)
    {
        var hash = venue.GetHashCode();

        if (!Performances.TryGetValue(hash, out var performance))
        {
            return;
        }

        var quality = performance.Quality;

        if (quality is >= 0f and < .5f)
        {
            return;
        }

        var audience = map.mapPawns.AllPawnsSpawned.Where(x => x.RaceProps.Humanlike &&
                                                               (x.Faction is { IsPlayer: true } ||
                                                                x.HostFaction is { IsPlayer: true }) &&
                                                               x.health.capacities.CapableOf(PawnCapacityDefOf
                                                                   .Hearing) &&
                                                               x.Awake() &&
                                                               RadiusAndRoomCheck(venue, x));
        if (!audience.Any())
        {
            return;
        }

        var thought = GetThoughtDef(quality);

        if (thought == null)
        {
            return;
        }
#if DEBUG
            Verse.Log.Message($"Giving memory of {thought.stages[0].label} to {audience.Count()} pawns");

#endif

        foreach (var audienceMember in audience)
        {
            audienceMember.needs.mood.thoughts.memories.TryGainMemory(thought);
        }
    }

    public bool TryFindInstrumentToPlay(Thing venue, Pawn musician, out Thing instrument, bool isWork = false)
    {
        instrument = null;

        Thing heldInstrument = null;

        foreach (var inventoryThing in musician.inventory.innerContainer)
        {
            if (!IsInstrument(inventoryThing))
            {
                continue;
            }

            heldInstrument = inventoryThing;
            break;
        }

        var skill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;

        //                                                                          visitors only play their own instruments, unless building type

        var mapInstruments = AvailableMapInstruments(musician, venue,
            musician.Faction is not { IsPlayer: true }, isWork);


        if (!mapInstruments.Any())
        {
            instrument = heldInstrument;
            return instrument != null;
        }

        var bestInstrument = mapInstruments.First();

        if (heldInstrument == null)
        {
            instrument = bestInstrument;
            return true;
        }

        var swap = false;

        var buildingSwapModifier = bestInstrument.TryGetComp<CompMusicalInstrument>().Props.isBuilding ? .2f : 0f;

        var rand = Rand.Range(0f, 1f);

        if (rand > .9f - buildingSwapModifier)
        {
            swap = true;
        }
        else if (rand > .4f - buildingSwapModifier)
        {
            swap = heldInstrument.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill) <
                   bestInstrument.TryGetComp<CompMusicalInstrument>().WeightedSuitability(skill);
        }

        instrument = swap ? bestInstrument : heldInstrument;

        return true;
    }

    public bool TryFindStandingSpotOrChair(CompMusicSpot musicSpot, Pawn musician, Thing instrument,
        out LocalTargetInfo target)
    {
        IntVec3 standingSpot;

        target = null;

        var pm = musician.Map.GetComponent<PerformanceManager>();

        var comp = instrument.TryGetComp<CompMusicalInstrument>();

        if (comp.Props.isBuilding)
        {
            if (pm.TryFindChairAt(comp, musician, out var chair))
            {
                target = chair;
                return true;
            }

            if (!pm.TryFindSpotAt(comp, musician, out standingSpot))
            {
                return false;
            }

            target = standingSpot;
            return true;
        }

        if (!pm.TryFindSitSpotOnGroundNear(musicSpot, musician, out standingSpot))
        {
            return false;
        }

        target = standingSpot;
        return true;
    }

    public bool TryFindSitSpotOnGroundNear(CompMusicSpot spot, Pawn sitter, out IntVec3 result)
    {
        var center = spot.parent.Position;

        for (var i = 0; i < 30; i++)
        {
            var intVec = center + GenRadial.RadialPattern[Rand.Range(1, NumRadiusCells)];
            if (!sitter.CanReserveAndReach(intVec, PathEndMode.OnCell, Danger.None) ||
                intVec.GetEdifice(map) != null || !GenSight.LineOfSight(center, intVec, map, true))
            {
                continue;
            }

            result = intVec;
            return true;
        }

        result = IntVec3.Invalid;
        return false;
    }

    public bool TryFindChairNear(CompMusicSpot spot, Pawn sitter, out Thing chair)
    {
        var center = spot.parent.Position;

        var comp = spot.parent.TryGetComp<CompMusicalInstrument>();

        foreach (var intVec3 in RadialPatternMiddleOutward)
        {
            var c = center + intVec3;

            if (comp != null && comp.Props.isBuilding && c == comp.parent.InteractionCell)
            {
                continue;
            }

            var edifice = c.GetEdifice(map);
            if (edifice == null || !edifice.def.building.isSittable ||
                !sitter.CanReserveAndReach(edifice, PathEndMode.OnCell, Danger.None) ||
                edifice.IsForbidden(sitter) || !GenSight.LineOfSight(center, edifice.Position, map, true))
            {
                continue;
            }

            chair = edifice;
            return true;
        }

        chair = null;
        return false;
    }

    public bool TryFindChairAt(CompMusicalInstrument instrument, Pawn sitter, out Thing chair)
    {
        chair = null;

        if (!instrument.Props.isBuilding)
        {
            return false;
        }

        var edifice = instrument.parent.InteractionCell.GetEdifice(map);
        if (edifice == null || !edifice.def.building.isSittable ||
            !sitter.CanReserveAndReach(edifice, PathEndMode.OnCell, Danger.None) || edifice.IsForbidden(sitter))
        {
            return false;
        }

        chair = edifice;
        return true;
    }

    public bool TryFindSpotAt(CompMusicalInstrument instrument, Pawn sitter, out IntVec3 result)
    {
        result = IntVec3.Invalid;

        if (!instrument.Props.isBuilding)
        {
            return false;
        }

        var intVec = instrument.parent.InteractionCell;

        if (!sitter.CanReserveAndReach(intVec, PathEndMode.OnCell, Danger.None) || intVec.GetEdifice(map) != null)
        {
            return false;
        }

        result = intVec;
        return true;
    }
}