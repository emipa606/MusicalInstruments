using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MusicalInstruments;

internal class JobDriver_TakeInstrument : JobDriver
{
    private const TargetIndex InstrumentInd = TargetIndex.A;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        var pawn1 = pawn;

        var target = job.GetTarget(InstrumentInd);

        // try to reserve an instrument to take
        if (!pawn1.Reserve(target, job, 1, -1, null, errorOnFailed))
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

        var gotoThing = new Toil
        {
            initAction = delegate { pawn.pather.StartPath(TargetThingA, PathEndMode.ClosestTouch); },

            defaultCompleteMode = ToilCompleteMode.PatherArrival
        };
        _ = gotoThing.FailOnDespawnedNullOrForbidden(InstrumentInd);

        yield return gotoThing;

        var dropInstruments = new Toil
        {
            initAction = delegate
            {
                var instruments = pawn.inventory.innerContainer.Where(PerformanceManager.IsInstrument)
                    .ToList();

                foreach (var instrument in instruments)
                {
                    _ = pawn.inventory.innerContainer.TryDrop(instrument, pawn.Position, pawn.Map,
                        ThingPlaceMode.Near, out _);
                }
            }
        };

        yield return dropInstruments;

        yield return Toils_Haul.TakeToInventory(InstrumentInd, 1);
    }
}