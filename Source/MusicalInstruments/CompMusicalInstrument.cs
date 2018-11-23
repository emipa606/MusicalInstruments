using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MusicalInstruments
{
    public class CompMusicalInstrument : ThingComp
    {
        public CompProperties_MusicalInstrument Props
        {
            get
            {
                return (CompProperties_MusicalInstrument)this.props;
            }
        }

        public float WeightedSuitability(int musicianSkill)
        {
            float f = musicianSkill / 20f;

            return Props.easiness * (1-f) + Props.expressiveness * f;

        }
    }
}
