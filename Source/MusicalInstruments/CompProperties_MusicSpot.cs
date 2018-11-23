using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace MusicalInstruments
{
    public class CompProperties_MusicSpot : CompProperties
    {
        public bool canBeDisabled = true;

        public CompProperties_MusicSpot()
        {
            this.compClass = typeof(CompMusicSpot);
        }
    }
}
