using Verse;

namespace MusicalInstruments;

public class CompProperties_MusicSpot : CompProperties
{
    public bool canBeDisabled = true;

    public CompProperties_MusicSpot()
    {
        compClass = typeof(CompMusicSpot);
    }
}