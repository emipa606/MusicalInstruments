using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MusicalInstruments
{
    class CompProperties_MusicalInstrument : CompProperties
    {
        public float easiness = 0f;

        public float expressiveness = 0f;

        public CompProperties_MusicalInstrument()
        {
            this.compClass = typeof(CompMusicalInstrument);
        }
    }
}
