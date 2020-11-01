using System;
using System.Collections.Generic;

using Verse;
using Verse.AI;

using RimWorld;

namespace MusicalInstruments
{
    class JobDriver_MusicListen : JobDriver
    {
        private const TargetIndex MusicSpotParentInd = TargetIndex.A;

        private const TargetIndex ChairOrSpotOrBedInd = TargetIndex.B;

        private Thing MusicSpotParent => TargetA.Thing;

        private bool HasChairOrBed => TargetB.HasThing;

        private Thing ChairOrBed => TargetB.Thing;

        private bool IsInBed => HasChairOrBed && ChairOrBed is Building_Bed && JobInBedUtility.InBedOrRestSpotNow(pawn, TargetB);

        private IntVec3 ClosestMusicSpotParentCell => MusicSpotParent.OccupiedRect().ClosestCellTo(pawn.Position);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = base.pawn;
            LocalTargetInfo target = base.job.GetTarget(ChairOrSpotOrBedInd);
            Job job = base.job;
            bool errorOnFailed2 = errorOnFailed;
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed2))
            {
#if DEBUG
                Verse.Log.Message("Couldn't reserve spot " + pawn.Label);
#endif
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
#if DEBUG
            Verse.Log.Message(string.Format("MakeNewToils, HasChairOrBed={0}, IsInBed={1}", HasChairOrBed, IsInBed));
            //Verse.Log.Message(String.Format("pawn.CurJob = {0}", pawn.CurJob == null ? "null" : pawn.CurJob.def.LabelCap ));
            //Verse.Log.Message(String.Format("pawn.getPosture = {0}", pawn.GetPosture().ToString()));
            //Verse.Log.Message(String.Format("current bed hashcode = {0}, target hashcode = {1}", pawn.CurrentBed() == null ? 0 : pawn.CurrentBed().GetHashCode(), HasChairOrBed ? ChairOrBed.GetHashCode() : 0));
#endif

            this.EndOnDespawnedOrNull(MusicSpotParentInd);

            //Verse.Log.Message(String.Format("Gather Spot ID = {0}", MusicSpotParent.GetHashCode()));

            if (HasChairOrBed)
            {
                this.EndOnDespawnedOrNull(ChairOrSpotOrBedInd);
            }

            Pawn listener = pawn;
            
            Thing venue = MusicSpotParent;


            if (!HasChairOrBed)
            {
#if DEBUG
                Verse.Log.Message("goto cell");
#endif
                yield return Toils_Goto.GotoCell(ChairOrSpotOrBedInd, PathEndMode.OnCell);
            }
            else if (!IsInBed)
            {
#if DEBUG
                Verse.Log.Message("goto chair");
#endif
                yield return Toils_Goto.GotoThing(ChairOrSpotOrBedInd, PathEndMode.OnCell);
            }
            else
            {
#if DEBUG
                Verse.Log.Message("goto bed");
#endif
                yield return Toils_Bed.ClaimBedIfNonMedical(ChairOrSpotOrBedInd, TargetIndex.None);
                yield return Toils_Bed.GotoBed(ChairOrSpotOrBedInd);
            }

            // custom toil.
            Toil listen;

            if(IsInBed)
            {
                this.KeepLyingDown(ChairOrSpotOrBedInd);
                listen = Toils_LayDown.LayDown(ChairOrSpotOrBedInd, true, false, true, true);
                listen.AddFailCondition(() => !listen.actor.Awake());
            }
            else
            {
                listen = new Toil();
            }
            

            listen.tickAction = delegate
            {
                if(!HasChairOrBed)
                {
                    pawn.rotationTracker.FaceCell(ClosestMusicSpotParentCell);
                }

                JoyUtility.JoyTickCheckEnd(listener, JoyTickFullJoyAction.GoToNextToil, 1f + Math.Abs(pawn.Map.GetComponent<PerformanceManager>().GetPerformanceQuality(venue)), null);
            };

            listen.handlingFacing = !HasChairOrBed;
            listen.defaultCompleteMode = ToilCompleteMode.Delay;
            listen.defaultDuration = job.def.joyDuration;

            listen.AddEndCondition(delegate 
            {
                return pawn.Map.GetComponent<PerformanceManager>().HasPerformance(venue) ? JobCondition.Ongoing : JobCondition.Incompletable;
            });

            listen.AddFinishAction(delegate
            {
                JoyUtility.TryGainRecRoomThought(pawn);
            });
            listen.socialMode = RandomSocialMode.Quiet;
            yield return listen;
        }
    }
}
