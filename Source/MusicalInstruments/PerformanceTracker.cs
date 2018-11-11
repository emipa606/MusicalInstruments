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
    public class Performance
    {
        public List<Pawn> Musicians;
        public float Quality;

        public void CalculateQuality()
        {
            if (Musicians.Any())
            {
                float quality = 0f;
                foreach (Pawn musician in Musicians)
                {
                    quality += PerformanceTracker.GetMusicQuality(musician, musician.carryTracker.CarriedThing);
                }

                float f = (float)Musicians.Count;
                f -= (f - 1) * .2f;

                Quality = quality / f;
            }
            else Quality = 0f;
            
        }

    }

    public static class PerformanceTracker
    {

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

#if DEBUG

            Verse.Log.Message(String.Format("Musicians: {0}", String.Join(", ", Performances[hash].Musicians.Select(x => LogMusician(x)).ToArray())));
            Verse.Log.Message(String.Format("Quality: {0}", Performances[hash].Quality));

#endif
        }

        public static void StopPlaying(Pawn musician, Thing venue)
        {
            int hash = venue.GetHashCode();

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
            bool isInspired = musician.Inspired ? musician.Inspiration.def == InspirationDefOf.Inspired_Creativity : false;
            QualityCategory instrumentQuality = QualityCategory.Normal;
            instrument.TryGetQuality(out instrumentQuality);
            float instrumentCondition = (float)instrument.HitPoints / instrument.MaxHitPoints;
            CompMusicalInstrument instrumentComp = instrument.TryGetComp<CompMusicalInstrument>();
            float easiness = instrumentComp.Props.easiness;
            float expressiveness = instrumentComp.Props.expressiveness;

            float quality = (easiness + (expressiveness * (artSkill / 6.0f) * (isInspired ? 2.0f : 1.0f))) * ((float)instrumentQuality / 3.0f + 0.1f) * instrumentCondition;

            return quality - 0.3f;
        }
    }
}
