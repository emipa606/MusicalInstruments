using RimWorld;
using Verse;

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