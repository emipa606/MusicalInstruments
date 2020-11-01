using Verse;

namespace MusicalInstruments
{
    public class CompProperties_MusicalInstrument : CompProperties
    {
        public float easiness = 0f;

        public float expressiveness = 0f;

        public float xOffset = 0f;

        public float zOffset = 0f;

        public float xOffsetFacing = 0f;

        public bool isBuilding = false;

        public bool isWindInstrument = false;

        public CompProperties_MusicalInstrument()
        {
            compClass = typeof(CompMusicalInstrument);
        }
    }
}
