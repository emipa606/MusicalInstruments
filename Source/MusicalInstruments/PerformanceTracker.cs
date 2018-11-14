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
    public class Performance
    {
        private const int SmallEnsembleCutoff = 6;

        public List<Pawn> Musicians;
        public float Quality;

        public void CalculateQuality()
        {
            if (Musicians.Any())
            {
                float f;

                if(Musicians.Count == 1)
                {
                    f = 1f;
                } else if(Musicians.Count >= SmallEnsembleCutoff)
                {
                    f = Musicians.Count / 2f;
                }
                else
                {
                    float x = (Musicians.Count - 1) / (float)(SmallEnsembleCutoff - 1);
                    f = 1f + x * Musicians.Count / 2f - x;
                }

#if DEBUG

                Verse.Log.Message(String.Format("s={0},f={1}", Musicians.Count, f));

#endif 

                Quality = Musicians.Select(x => PerformanceTracker.GetMusicQuality(x, x.carryTracker.CarriedThing)).Sum() / f;

            }

            else Quality = 0f;
            
        }

    }


    public static class PerformanceTracker
    {
        private const float Radius = 9f;
        private static Dictionary<int, Performance> Performances = new Dictionary<int, Performance>();

        private static string LogMusician(Pawn musician)
        {
            return String.Format("{0}({1} skill) on {2}", musician.LabelShort, musician.skills.GetSkill(SkillDefOf.Artistic).Level, musician.carryTracker.CarriedThing.LabelShort);
        }

        public static void StartPlaying(Pawn musician, Thing venue)
        {
            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash))
                Performances[hash] = new Performance() { Musicians = new List<Pawn>(), Quality = 0f };

            Performances[hash].Musicians.Add(musician);
            Performances[hash].CalculateQuality();

            ApplyThoughts(venue);

#if DEBUG

            Verse.Log.Message(String.Format("Musicians: {0}", String.Join(", ", Performances[hash].Musicians.Select(x => LogMusician(x)).ToArray())));
            Verse.Log.Message(String.Format("Quality: {0}", Performances[hash].Quality));

#endif
        }

        public static void StopPlaying(Pawn musician, Thing venue)
        {
            int hash = venue.GetHashCode();

            ApplyThoughts(venue);

            Performances[hash].Musicians.Remove(musician);
            Performances[hash].CalculateQuality();
        }

        public static bool HasPerformance(Thing venue)
        {
            int hash = venue.GetHashCode();

            if (!Performances.ContainsKey(hash))
                return false;
            return Performances[hash].Musicians.Any();
        }

        public static float GetPerformanceQuality(Thing venue)
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

        public static float GetMusicQuality(Pawn musician, Thing instrument)
        {
            int artSkill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;
            int luck = ((JobDriver_MusicPlay)musician.jobs.curDriver).Luck;
            bool isInspired = musician.Inspired ? musician.Inspiration.def == InspirationDefOf.Inspired_Creativity : false;
            QualityCategory instrumentQuality = QualityCategory.Normal;
            instrument.TryGetQuality(out instrumentQuality);
            float instrumentCondition = (float)instrument.HitPoints / instrument.MaxHitPoints;
            CompMusicalInstrument instrumentComp = instrument.TryGetComp<CompMusicalInstrument>();
            float easiness = instrumentComp.Props.easiness;
            float expressiveness = instrumentComp.Props.expressiveness;

            float quality = (easiness + (expressiveness * ((artSkill + luck + (isInspired ? 0 : -3)) / 5.0f))) * ((float)instrumentQuality / 3.0f + 0.1f) * instrumentCondition;

            return quality - 0.3f;
        }

        public static void ApplyThoughts(Thing venue)
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