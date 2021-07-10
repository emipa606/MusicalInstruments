using Verse;

namespace MusicalInstruments
{
    public class CompMusicalInstrument : ThingComp
    {
        public CompProperties_MusicalInstrument Props => (CompProperties_MusicalInstrument) props;

        public float WeightedSuitability(int musicianSkill)
        {
            var f = musicianSkill / 20f;

            return (Props.easiness * (1 - f)) + (Props.expressiveness * f);
        }
    }
}