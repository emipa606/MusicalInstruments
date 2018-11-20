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

        protected override Toil GetPlayToil(Pawn musician, Thing venue)
        {
            // custom toil.
            Toil play = new Toil();

            play.initAction = delegate
            {
                pawn.Map.GetComponent<PerformanceManager>().StartPlaying(musician, venue, false);
            };



            play.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(this.ClosestMusicSpotParentCell);
                JoyUtility.JoyTickCheckEnd(musician, JoyTickFullJoyAction.GoToNextToil, 1f, null);

                if (this.ticksLeftThisToil % 100 == 99)
                {
                    ThrowMusicNotes(musician.DrawPos, this.Map);
                    //pawn.Map.GetComponent<PerformanceManager>().ApplyThoughts(venue);
                }


            };

            play.handlingFacing = true;
            play.defaultCompleteMode = ToilCompleteMode.Delay;
            play.defaultDuration = this.job.def.joyDuration;

            play.AddFinishAction(delegate
            {
                pawn.Map.GetComponent<PerformanceManager>().StopPlaying(musician, venue);
            });

            play.socialMode = RandomSocialMode.Quiet;

            return play;
        }

    }
}
