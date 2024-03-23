using Verse;

namespace MusicalInstruments;

public class Performer : IExposable
{
    public Thing Instrument;
    public Pawn Musician;

    public void ExposeData()
    {
        Scribe_References.Look(ref Musician, "MusicalInstruments.Musician");
        Scribe_References.Look(ref Instrument, "MusicalInstruments.Instrument");
    }
}