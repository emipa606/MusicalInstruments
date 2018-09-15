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
    public static class JoyKindDefOf_Music
    {
        public static JoyKindDef Music;

        static JoyKindDefOf_Music()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JoyKindDefOf_Music));
        }
    }
}
