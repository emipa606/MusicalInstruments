﻿using System;
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
        private const TargetIndex MusicSpotParentInd = TargetIndex.A;

        private const TargetIndex ChairOrSpotOrBedInd = TargetIndex.B;

        private Thing MusicSpotParent => job.GetTarget(TargetIndex.A).Thing;

        private bool HasChairOrBed => job.GetTarget(TargetIndex.B).HasThing;


        private IntVec3 ClosestMusicSpotParentCell => MusicSpotParent.OccupiedRect().ClosestCellTo(pawn.Position);

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
            this.EndOnDespawnedOrNull(MusicSpotParentInd);

            //Verse.Log.Message(String.Format("Gather Spot ID = {0}", TargetA.Thing.GetHashCode()));

            Thing chairOrBed = null;

            if (HasChairOrBed)
            {
                this.EndOnDespawnedOrNull(ChairOrSpotOrBedInd);
                chairOrBed = this.TargetB.Thing;
            }

            Pawn listener = this.pawn;
            
            Thing venue = this.TargetA.Thing;

            if(!HasChairOrBed)
            {
                yield return Toils_Goto.GotoCell(ChairOrSpotOrBedInd, PathEndMode.OnCell);
            }
            else if(!(chairOrBed is Building_Bed))
                yield return Toils_Goto.GotoThing(ChairOrSpotOrBedInd, PathEndMode.OnCell);

            // custom toil.
            Toil listen = new Toil();

            listen.tickAction = delegate
            {
                if(!HasChairOrBed)
                    this.pawn.rotationTracker.FaceCell(this.ClosestMusicSpotParentCell);
                JoyUtility.JoyTickCheckEnd(listener, JoyTickFullJoyAction.GoToNextToil, 1f + Math.Abs(pawn.Map.GetComponent<PerformanceManager>().GetPerformanceQuality(venue)), null);
            };

            listen.handlingFacing = !HasChairOrBed;
            listen.defaultCompleteMode = ToilCompleteMode.Delay;
            listen.defaultDuration = this.job.def.joyDuration;

            listen.AddEndCondition(delegate 
            {
                if (pawn.Map.GetComponent<PerformanceManager>().HasPerformance(venue))
                    return JobCondition.Ongoing;
                else
                    return JobCondition.Incompletable;
            });

            listen.AddFinishAction(delegate
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });
            listen.socialMode = RandomSocialMode.Quiet;
            yield return listen;
        }
    }
}
