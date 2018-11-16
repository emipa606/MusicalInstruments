using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MusicalInstruments
{
    class CompMusicalInstrument : ThingComp
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
            if (musicianSkill < 8)
                return Props.easiness + Props.expressiveness * .5f;
            return Props.easiness * .5f + Props.expressiveness;

        }
    }
}
