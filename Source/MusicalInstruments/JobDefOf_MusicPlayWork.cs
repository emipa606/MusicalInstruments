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
    public static class JobDefOf_MusicPlayWork
    {
        public static JobDef MusicPlayWork;

        static JobDefOf_MusicPlayWork()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_MusicPlayWork));
        }
    }
}