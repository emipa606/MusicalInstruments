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
    class JobDriver_MusicListen : JobDriver
    {
        private const TargetIndex GatherSpotParentInd = TargetIndex.A;

        private const TargetIndex ChairOrSpotInd = TargetIndex.B;

        private Thing GatherSpotParent => job.GetTarget(TargetIndex.A).Thing;

        private bool HasChair => job.GetTarget(TargetIndex.B).HasThing;


        private IntVec3 ClosestGatherSpotParentCell => GatherSpotParent.OccupiedRect().ClosestCellTo(pawn.Position);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = base.pawn;
            LocalTargetInfo target = base.job.GetTarget(TargetIndex.B);
            Job job = base.job;
            bool errorOnFailed2 = errorOnFailed;
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed2))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A);

            //Verse.Log.Message(String.Format("Gather Spot ID = {0}", TargetA.Thing.GetHashCode()));

            if (HasChair)
            {
                this.EndOnDespawnedOrNull(TargetIndex.B);
            }

            Pawn listener = this.pawn;

            Thing instrument = this.TargetC.Thing;

            Thing venue = this.TargetA.Thing;

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            // custom toil.
            Toil listen = new Toil();

            listen.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(this.ClosestGatherSpotParentCell);
                JoyUtility.JoyTickCheckEnd(listener, JoyTickFullJoyAction.GoToNextToil, PerformanceTracker.GetPerformanceQuality(venue), null);
            };

            listen.handlingFacing = true;
            listen.defaultCompleteMode = ToilCompleteMode.Delay;
            listen.defaultDuration = this.job.def.joyDuration;

            listen.AddFinishAction(delegate
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });
            listen.socialMode = RandomSocialMode.Quiet;
            yield return listen;
        }
    }
}
