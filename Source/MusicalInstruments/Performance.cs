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
    public class Performer : IExposable
    {
        public Pawn Musician;
        public Thing Instrument;

        public void ExposeData()
        {
            Scribe_References.Look<Pawn>(ref Musician, "MusicalInstruments.Musician");
            Scribe_References.Look<Thing>(ref Instrument, "MusicalInstruments.Instrument");
        }
    }

    public class Performance : IExposable
    {
        private const int SmallEnsembleCutoff = 6;

        public Thing Venue;
        public Dictionary<int, Performer> Performers;
        public float Quality;

        private List<int> WorkingKeysPerformers;
        private List<Performer> WorkingValuesPerformers;


        public Performance()
        {
            Venue = null;
            Performers = null;
            Quality = 0f;

            WorkingKeysPerformers = new List<int>();
            WorkingValuesPerformers = new List<Performer>();
        }

        public Performance(Thing venue)
        {
            Venue = venue;
            Performers = new Dictionary<int, Performer>();
            Quality = 0f;

            WorkingKeysPerformers = new List<int>();
            WorkingValuesPerformers = new List<Performer>();
        }

        public void ExposeData()
        {
            Scribe_References.Look<Thing>(ref Venue, "MusicalInstruments.Venue");
            Scribe_Collections.Look<int, Performer>(ref Performers, "MusicalInstruments.Performers", LookMode.Value, LookMode.Deep, ref WorkingKeysPerformers, ref WorkingValuesPerformers);
            Scribe_Values.Look<float>(ref Quality, "MusicalInstruments.Quality");

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                //try to avoid breaking saves from old versions
                if (Performers == null)
                    Performers = new Dictionary<int, Performer>();
            }
        }

        public void CalculateQuality()
        {
            if (Performers.Any())
            {
                float f;

                if(Performers.Count == 1)
                {
                    f = 1f;
                } else if(Performers.Count >= SmallEnsembleCutoff)
                {
                    f = Performers.Count / 2f;
                }
                else
                {
                    float x = (Performers.Count - 1) / (float)(SmallEnsembleCutoff - 1);
                    f = 1f + x * Performers.Count / 2f - x;
                }

#if DEBUG

                Verse.Log.Message(String.Format("s={0},f={1}", Performers.Count, f));

#endif 

                Quality = Performers.Select(x => GetMusicQuality(x.Value.Musician, x.Value.Instrument)).Sum() / f;

            }

            else Quality = 0f;
            
        }


        private static float GetMusicQuality(Pawn musician, Thing instrument)
        {
            if (musician == null || instrument == null) return 0f;

            int artSkill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;
            JobDriver driver = musician.jobs.curDriver;
            int luck = (driver != null && driver is JobDriver_MusicPlayBase ? ((JobDriver_MusicPlayBase)driver).Luck : 0);
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
