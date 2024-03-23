using Verse;

namespace MusicalInstruments;

public class CompProperties_MusicSpot : CompProperties
{
    public readonly bool canBeDisabled = true;

    public CompProperties_MusicSpot()
    {
        compClass = typeof(CompMusicSpot);
    }
}