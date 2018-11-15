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
    class JobDriver_TakeInstrument : JobDriver
    {
        private const TargetIndex InstrumentInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
                        
            LocalTargetInfo target = this.job.GetTarget(InstrumentInd);

            // try to reserve an instrument to take
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed)) return false;

            //lets try this
            this.job.count = 1;

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(InstrumentInd);

            Toil gotoThing = new Toil();

            gotoThing.initAction = delegate
            {
                this.pawn.pather.StartPath(this.TargetThingA, PathEndMode.ClosestTouch);
            };

            gotoThing.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            gotoThing.FailOnDespawnedNullOrForbidden(InstrumentInd);

            yield return gotoThing;

            Toil dropInstruments = new Toil();

            dropInstruments.initAction = delegate
            {
                List<Thing> instruments = pawn.inventory.innerContainer.Where(x => PerformanceManager.IsInstrument(x)).ToList();
                Thing result;

                foreach (Thing instrument in instruments)
                {
                    pawn.inventory.innerContainer.TryDrop(instrument, pawn.Position, pawn.Map, ThingPlaceMode.Near, out result);
                }
            };

            yield return dropInstruments;

            yield return Toils_Haul.TakeToInventory(InstrumentInd, 1);
        }

    }
}
