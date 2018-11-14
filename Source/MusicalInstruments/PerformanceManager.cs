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
        private const float Radius = 9f;
        private Dictionary<int, Performance> Performances;
        
        public PerformanceManager(Map map) : base(map)
        {
            Performances = new Dictionary<int, Performance>();
        }

        public override void FinalizeInit()
        {
            //foreach(CompGatherSpot gatherSpot in map.gatherSpotLister.activeSpots)
            //{
            //    foreach(Pawn pawn in map.mapPawns.FreeColonistsAndPrisoners)
            //    {
            //        if (pawn.CurJobDef == JobDefOf_MusicPlay.MusicPlay && pawn.CurJob.targetA.Thing.GetHashCode() == gatherSpot.parent.GetHashCode())
            //        {
            //            StartPlaying(pawn, gatherSpot.parent);
            //        }
            //    }
            //}
        }

        private static string LogMusician(Pawn musician)
        {
            return String.Format("{0}({1} skill) on {2}", musician.LabelShort, musician.skills.GetSkill(SkillDefOf.Artistic).Level, musician.carryTracker.CarriedThing.LabelShort);
        }

        public void StartPlaying(Pawn musician, Thing venue)
        {
            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash))
                Performances[hash] = new Performance() { Venue = venue, Musicians = new List<Pawn>(), Quality = 0f };

            Performances[hash].Musicians.Add(musician);
            Performances[hash].CalculateQuality();

            ApplyThoughts(venue);

#if DEBUG

            Verse.Log.Message(String.Format("Musicians: {0}", String.Join(", ", Performances[hash].Musicians.Select(x => LogMusician(x)).ToArray())));
            Verse.Log.Message(String.Format("Quality: {0}", Performances[hash].Quality));

#endif
        }

        public void StopPlaying(Pawn musician, Thing venue)
        {
            int hash = venue.GetHashCode();

            ApplyThoughts(venue);

            Performances[hash].Musicians.Remove(musician);
            Performances[hash].CalculateQuality();
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
            float quality = Performances[venue.GetHashCode()].Quality;

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
