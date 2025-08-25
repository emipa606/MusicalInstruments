using Verse;

namespace MusicalInstruments;

internal class CompProp_PlayingMusic : CompProperties
{
    public SoundDef soundPlayInstrument;

    public CompProp_PlayingMusic()
    {
        compClass = typeof(Comp_PlayingMusic);
    }
}