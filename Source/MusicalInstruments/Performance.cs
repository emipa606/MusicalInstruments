using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MusicalInstruments;

public class Performance : IExposable
{
    private const int SmallEnsembleCutoff = 6;
    public Dictionary<int, Performer> Performers;
    public float Quality;

    public Thing Venue;

    private List<int> WorkingKeysPerformers;
    private List<Performer> WorkingValuesPerformers;


    public Performance()
    {
        Venue = null;
        Performers = null;
        Quality = 0f;

        WorkingKeysPerformers = [];
        WorkingValuesPerformers = [];
    }

    public Performance(Thing venue)
    {
        Venue = venue;
        Performers = new Dictionary<int, Performer>();
        Quality = 0f;

        WorkingKeysPerformers = [];
        WorkingValuesPerformers = [];
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref Venue, "MusicalInstruments.Venue");
        Scribe_Collections.Look(ref Performers, "MusicalInstruments.Performers", LookMode.Value, LookMode.Deep,
            ref WorkingKeysPerformers, ref WorkingValuesPerformers);
        Scribe_Values.Look(ref Quality, "MusicalInstruments.Quality");

        if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs)
        {
            return;
        }

        //try to avoid breaking saves from old versions
        Performers ??= new Dictionary<int, Performer>();
    }

    public void CalculateQuality()
    {
        if (Performers.Any())
        {
            float f;

            switch (Performers.Count)
            {
                case 1:
                    f = 1f;
                    break;
                case >= SmallEnsembleCutoff:
                    f = Performers.Count / 2f;
                    break;
                default:
                {
                    var x = (Performers.Count - 1) / (float)(SmallEnsembleCutoff - 1);
                    f = 1f + (x * Performers.Count / 2f) - x;
                    break;
                }
            }

#if DEBUG
                Verse.Log.Message($"s={Performers.Count},f={f}");

#endif

            Quality = Performers.Select(x => GetMusicQuality(x.Value.Musician, x.Value.Instrument)).Sum() / f;
        }

        else
        {
            Quality = 0f;
        }
    }


    public static float GetMusicQuality(Pawn musician, Thing instrument, int? luck = null)
    {
        if (musician == null || instrument == null)
        {
            return 0f;
        }

        var artSkill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;
        int luckActual;

        if (luck.HasValue)
        {
            luckActual = luck.Value;
        }
        else
        {
            var driver = musician.jobs.curDriver;
            luckActual = driver is JobDriver_MusicPlayBase playBase ? playBase.Luck : 0;
        }

        var isInspired = musician.Inspired && musician.Inspiration.def == InspirationDefOf.Inspired_Creativity;
        _ = instrument.TryGetQuality(out var instrumentQuality);
        var instrumentCondition = (float)instrument.HitPoints / instrument.MaxHitPoints;
        var instrumentComp = instrument.TryGetComp<CompMusicalInstrument>();
        var easiness = instrumentComp.Props.easiness;
        var expressiveness = instrumentComp.Props.expressiveness;

        var quality = (easiness + (expressiveness * ((artSkill + luckActual + (isInspired ? 0 : -3)) / 5.0f))) *
                      (((float)instrumentQuality / 3.0f) + 0.1f) * instrumentCondition;

        return quality - 0.3f;
    }
}