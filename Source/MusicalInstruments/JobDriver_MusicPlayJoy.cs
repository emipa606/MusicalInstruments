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
    public class JobDriver_MusicPlayJoy : JobDriver_MusicPlayBase
    {

        protected override Toil GetPlayToil(Pawn musician, Thing instrument, Thing venue)
        {
            // custom toil.
            Toil play = new Toil();

            CompProperties_MusicalInstrument props = instrument.TryGetComp<CompMusicalInstrument>().Props;

            play.initAction = delegate
            {
                pawn.Map.GetComponent<PerformanceManager>().StartPlaying(musician, instrument, venue, false);
            };



            play.tickAction = delegate
            {

                if (props.isBuilding)
                {
                    pawn.rotationTracker.FaceTarget(TargetC);
                    pawn.GainComfortFromCellIfPossible();
                }
                else
                {
                    pawn.rotationTracker.FaceCell(ClosestMusicSpotParentCell);
                }

                if (ticksLeftThisToil % 100 == 99)
                {
                    ThrowMusicNotes(musician.DrawPos, this.Map);
                }

                JoyUtility.JoyTickCheckEnd(musician, JoyTickFullJoyAction.GoToNextToil, 1f, null);

            };

            play.handlingFacing = true;
            play.defaultCompleteMode = ToilCompleteMode.Delay;
            play.defaultDuration = this.job.def.joyDuration;

            play.AddFinishAction(delegate
            {
                pawn.Map.GetComponent<PerformanceManager>().StopPlaying(musician, venue);

                if(pawn.carryTracker.CarriedThing != null)
                {
                    if (!pawn.carryTracker.innerContainer.TryTransferToContainer(pawn.carryTracker.CarriedThing, pawn.inventory.innerContainer, true))
                    {
                        Thing thing;
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, pawn.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out thing, null);
                    }
                }

            });

            play.socialMode = RandomSocialMode.Quiet;

            return play;
        }

    }
}
