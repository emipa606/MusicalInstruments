using Verse;

namespace MusicalInstruments;

public class CompProperties_MusicalInstrument : CompProperties
{
    public readonly float easiness = 0f;

    public readonly float expressiveness = 0f;

    public readonly bool isBuilding = false;

    public readonly float xOffset = 0f;

    public readonly float xOffsetFacing = 0f;

    public readonly float zOffset = 0f;

    public readonly float zOffsetFacing = 0f;

    public bool isWindInstrument = false;

    public bool vertical;

    public CompProperties_MusicalInstrument()
    {
        compClass = typeof(CompMusicalInstrument);
    }
}