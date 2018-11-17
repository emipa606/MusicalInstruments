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
    public class Performance : IExposable
    {
        private const int SmallEnsembleCutoff = 6;

        public Thing Venue;
        public Dictionary<int, Pawn> Musicians;
        public float Quality;

        private List<int> WorkingKeysMusicians;
        private List<Pawn> WorkingValuesMusicians;


        public Performance()
        {
            Venue = null;
            Musicians = null;
            Quality = 0f;

            WorkingKeysMusicians = new List<int>();
            WorkingValuesMusicians = new List<Pawn>();
        }

        public Performance(Thing venue)
        {
            Venue = venue;
            Musicians = new Dictionary<int, Pawn>();
            Quality = 0f;

            WorkingKeysMusicians = new List<int>();
            WorkingValuesMusicians = new List<Pawn>();
        }

        public void ExposeData()
        {
            Scribe_References.Look<Thing>(ref Venue, "MusicalInstruments.Venue");
            Scribe_Collections.Look<int, Pawn>(ref Musicians, "MusicalInstruments.Musicians", LookMode.Value, LookMode.Reference, ref WorkingKeysMusicians, ref WorkingValuesMusicians);
            Scribe_Values.Look<float>(ref Quality, "MusicalInstruments.Quality");
        }

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

                Quality = Musicians.Select(x => GetMusicQuality(x.Value, x.Value.carryTracker.CarriedThing)).Sum() / f;

            }

            else Quality = 0f;
            
        }


        private static float GetMusicQuality(Pawn musician, Thing instrument)
        {
            if (instrument == null) return 0f;

            int artSkill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;
            int luck = ((JobDriver_MusicPlayBase)musician.jobs.curDriver).Luck;
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


    }

}
