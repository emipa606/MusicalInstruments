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
    public class JobDriver_MusicPlay : JobDriver
    {
        private const TargetIndex GatherSpotParentInd = TargetIndex.A;

        private const TargetIndex StandingSpotInd = TargetIndex.B;

        private const TargetIndex InstrumentInd = TargetIndex.C;

        private Thing GatherSpotParent
        {
            get
            {
                return this.job.GetTarget(GatherSpotParentInd).Thing;
            }
        }

        private IntVec3 ClosestGatherSpotParentCell
        {
            get
            {
                return this.GatherSpotParent.OccupiedRect().ClosestCellTo(this.pawn.Position);
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;

            LocalTargetInfo target = job.GetTarget(StandingSpotInd);

            // try to reserve a place to sit or stand
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed)) return false;

            target = this.job.GetTarget(InstrumentInd);

            // try to reserve an instrument to play
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed)) return false;


            //if (this.HasDrink)
            //{
            //    // also try to reserve drink, if target is set by the JoyGiver
            //    pawn = this.pawn;
            //    target = this.job.GetTarget(OptionalIngestibleInd);
            //    job = this.job;
            //    if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
            //    {
            //        return false;
            //    }
            //}
            return true;
        }


        // this function does three things:
        // it adds generic delegate functions to globalFailConditions (inherited from IJobEndable) via `This.EndOn...` extensions
        // it also yield returns a collection of toils: some generic, some custom
        // it also interacts with the JoyUtility static class so the pawns get joy
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);
            //if (this.HasChair) {
            //    // if the pawn is standing instead of sitting on a chair, TargetIndex.B has no end/fail condition 
            //    // doesn't seem to matter
            //    this.EndOnDespawnedOrNull(TargetIndex.B, JobCondition.Incompletable);
            //}

            //if (this.HasDrink)
            //{
            //    this.FailOnDestroyedNullOrForbidden(TargetIndex.C);

            //    // go to where drugs are
            //    yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell).FailOnSomeonePhysicallyInteracting(TargetIndex.C);
            //    // pick up drugs
            //    yield return Toils_Haul.StartCarryThing(TargetIndex.C, false, false, false);
            //}

            this.FailOnDestroyedNullOrForbidden(TargetIndex.C);


            Thing instrument = this.TargetC.Thing;

            if (instrument.ParentHolder != this.pawn.inventory)
            {
                // go to where instrument is
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell).FailOnSomeonePhysicallyInteracting(TargetIndex.C);
                // pick up instrument
                yield return Toils_Haul.StartCarryThing(TargetIndex.C);
            }
            else
            {
                yield return Toils_Misc.TakeItemFromInventoryToCarrier(this.pawn, TargetIndex.C);

            }

            
            // go to the sitting / standing spot
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            // custom toil.
            Toil play = new Toil();
            play.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(this.ClosestGatherSpotParentCell);
                JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.GoToNextToil, 0.3f, null);
                

                if(this.ticksLeftThisToil % 1000 == 500)
                {
                    Verse.Log.Message(String.Format("ticks left = {0}", this.ticksLeftThisToil));
                    ThrowMusicNotes(this.pawn.DrawPos, this.Map);
                    //MoteMaker.ThrowText(this.pawn.DrawPos, this.Map, "TEST");
                    List<Pawn> audience = new List<Pawn>();

                    foreach(Pawn audiencePawn in Map.mapPawns.FreeColonistsAndPrisoners)
                    {
                        if (audiencePawn.Position.DistanceTo(pawn.Position) < 8) audience.Add(audiencePawn);
                    }

                    Verse.Log.Message(String.Format("musician: {0}, audience: {1}", pawn.Name.ToString(), String.Join(", ", audience.Select(p => p.Name.ToString()).ToArray())));
                }

         
            };

            play.handlingFacing = true;
            play.defaultCompleteMode = ToilCompleteMode.Delay;
            play.defaultDuration = this.job.def.joyDuration;
            play.AddFinishAction(delegate
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });
            play.socialMode = RandomSocialMode.Quiet;
            yield return play;

            yield return Toils_General.PutCarriedThingInInventory();

            // custom toil.
            //Toil chew = new Toil();
            //chew.tickAction = delegate
            //{
            //    this.pawn.rotationTracker.FaceCell(this.ClosestGatherSpotParentCell);
            //    this.pawn.GainComfortFromCellIfPossible();
            //    JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.GoToNextToil, 1f, null);
            //};
            //chew.handlingFacing = true;
            //chew.defaultCompleteMode = ToilCompleteMode.Delay;
            //chew.defaultDuration = this.job.def.joyDuration;
            //chew.AddFinishAction(delegate
            //{
            //    JoyUtility.TryGainRecRoomThought(this.pawn);
            //});
            //chew.socialMode = RandomSocialMode.SuperActive;
            //// this is called even if TargetIndex.C is empty - again, doesn't seem to matter
            ////Toils_Ingest.AddIngestionEffects(chew, this.pawn, TargetIndex.C, TargetIndex.None);
            //yield return chew;

            // think this is just a clean-up: code in this function mostly only applies to food-type ingestibles
            //if (this.HasDrink) {
            //    yield return Toils_Ingest.FinalizeIngest(this.pawn, TargetIndex.C);
            //}
        }

        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        {
            IntVec3 closestGatherSpotParentCell = this.ClosestGatherSpotParentCell;
            return JobDriver_Ingest.ModifyCarriedThingDrawPosWorker(ref drawPos, ref behind, ref flip, closestGatherSpotParentCell, this.pawn);
        }

        protected void ThrowMusicNotes(Vector3 loc, Map map)
        {
            if (!loc.ToIntVec3().ShouldSpawnMotesAt(map)) return;

            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_MusicNotes"));
            moteThrown.Scale = 1.0f;
            moteThrown.exactPosition = loc + new Vector3(0f, 0f, 0.5f);
            moteThrown.SetVelocity((float)Rand.Range(-10, 10), Rand.Range(0.4f, 0.6f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);

        }
    }
}
