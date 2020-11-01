using System.Collections.Generic;
using System.Linq;

using Verse;
using Verse.AI;

namespace MusicalInstruments
{
    class JobDriver_TakeInstrument : JobDriver
    {
        private const TargetIndex InstrumentInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
                        
            LocalTargetInfo target = job.GetTarget(InstrumentInd);

            // try to reserve an instrument to take
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            //lets try this
            job.count = 1;

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            _ = this.FailOnDestroyedOrNull(InstrumentInd);

            Toil gotoThing = new Toil
            {
                initAction = delegate
                {
                    pawn.pather.StartPath(TargetThingA, PathEndMode.ClosestTouch);
                },

                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            _ = gotoThing.FailOnDespawnedNullOrForbidden(InstrumentInd);

            yield return gotoThing;

            Toil dropInstruments = new Toil
            {
                initAction = delegate
                {
                    List<Thing> instruments = pawn.inventory.innerContainer.Where(x => PerformanceManager.IsInstrument(x)).ToList();

                    foreach (Thing instrument in instruments)
                    {
                        _ = pawn.inventory.innerContainer.TryDrop(instrument, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing result);
                    }
                }
            };

            yield return dropInstruments;

            yield return Toils_Haul.TakeToInventory(InstrumentInd, 1);
        }

    }
}
