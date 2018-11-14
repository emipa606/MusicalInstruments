using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Verse;
using Verse.AI;

using RimWorld;

namespace MusicalInstruments
{
    [DefOf]
    public static class JobDefOf_MusicPlay
    {
        public static JobDef MusicPlay;

        static JobDefOf_MusicPlay()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_MusicPlay));
        }
    }
}
