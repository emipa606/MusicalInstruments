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
    public class JobDriver_MusicPlayWork : JobDriver_MusicPlayBase
    {

        protected override Toil GetPlayToil(Pawn musician, Thing instrument, Thing venue)
        {

            // custom toil.
            Toil play = new Toil();

            CompProperties_MusicalInstrument props = instrument.TryGetComp<CompMusicalInstrument>().Props;

            play.initAction = delegate
            {
                pawn.Map.GetComponent<PerformanceManager>().StartPlaying(musician, instrument, venue, true);
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

            };

            play.handlingFacing = true;
            play.defaultCompleteMode = ToilCompleteMode.Delay;
            play.defaultDuration = 4000;

            play.AddFinishAction(delegate
            {
                pawn.Map.GetComponent<PerformanceManager>().StopPlaying(musician, venue);
            });

            play.socialMode = RandomSocialMode.Quiet;

            return play;
        }

    }
}
