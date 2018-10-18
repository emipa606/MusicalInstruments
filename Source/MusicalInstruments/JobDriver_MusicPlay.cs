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

        [TweakValue("MusicalInstruments.XOffset", -0.5f, 0.5f)]
        private static float InstrumentXOffset = .0f;

        [TweakValue("MusicalInstruments.ZOffset", -0.5f, 0.5f)]
        private static float InstrumentZOffset = .0f;

        [TweakValue("MusicalInstruments.Behind", 0f, 100f)]
        private static bool Behind = false;

        [TweakValue("MusicalInstruments.Flip", 0f, 100f)]
        private static bool Flip = false;

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

            return true;
        }


        // this function does three things:
        // it adds generic delegate functions to globalFailConditions (inherited from IJobEndable) via `This.EndOn...` extensions
        // it also yield returns a collection of toils: some generic, some custom
        // it also interacts with the JoyUtility static class so the pawns get joy
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);

            //Verse.Log.Message(String.Format("Gather Spot ID = {0}", TargetA.Thing.GetHashCode()));

            Pawn musician = this.pawn;

            this.FailOnDestroyedNullOrForbidden(TargetIndex.C);

            Thing instrument = this.TargetC.Thing;

            Thing venue = this.TargetA.Thing;

            if (instrument.ParentHolder != musician.inventory)
            {
                // go to where instrument is
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.OnCell).FailOnSomeonePhysicallyInteracting(TargetIndex.C);
                // pick up instrument
                yield return Toils_Haul.StartCarryThing(TargetIndex.C);
            }
            else
            {
                yield return Toils_Misc.TakeItemFromInventoryToCarrier(musician, TargetIndex.C);

            }

            
            // go to the sitting / standing spot
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            // custom toil.
            Toil play = new Toil();

            play.activeSkill = delegate
            {
                return SkillDefOf.Artistic;
            };

            play.initAction = delegate
            {
                PerformanceTracker.StartPlaying(musician, venue);
            };



            play.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(this.ClosestGatherSpotParentCell);
                JoyUtility.JoyTickCheckEnd(musician, JoyTickFullJoyAction.GoToNextToil, 0.25f * PerformanceTracker.GetPerformanceQuality(venue), null);
                musician.skills.Learn(SkillDefOf.Artistic, 0.1f);


                if (this.ticksLeftThisToil % 250 == 249)
                {
                    ThrowMusicNotes(musician.DrawPos, this.Map);
                }

         
            };

            play.handlingFacing = true;
            play.defaultCompleteMode = ToilCompleteMode.Delay;
            play.defaultDuration = this.job.def.joyDuration;

            play.AddFinishAction(delegate
            {
                PerformanceTracker.StopPlaying(musician, venue);
            });

            play.socialMode = RandomSocialMode.Quiet;

            yield return play;

            yield return Toils_General.PutCarriedThingInInventory();

        }

        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        {
            IntVec3 closestGatherSpotParentCell = this.ClosestGatherSpotParentCell;

            behind = Behind;
            flip = Flip;

            drawPos += new Vector3(InstrumentXOffset, .0f, InstrumentZOffset);
            return true;

            //return ModifyCarriedThingDrawPosWorker(ref drawPos, ref behind, ref flip, closestGatherSpotParentCell, this.pawn);
            //return false;
        }

        //public static bool ModifyCarriedThingDrawPosWorker(ref Vector3 drawPos, ref bool behind, ref bool flip, IntVec3 placeCell, Pawn pawn)
        //{
        //    if (pawn.pather.Moving)
        //    {
        //        return false;
        //    }
        //    Thing carriedThing = pawn.carryTracker.CarriedThing;
        //    if (carriedThing == null || !carriedThing.IngestibleNow)
        //    {
        //        return false;
        //    }
        //    if (placeCell.IsValid && placeCell.AdjacentToCardinal(pawn.Position) && placeCell.HasEatSurface(pawn.Map) && carriedThing.def.ingestible.ingestHoldUsesTable)
        //    {
        //        drawPos = new Vector3((float)placeCell.x + 0.5f, drawPos.y, (float)placeCell.z + 0.5f);
        //        return true;
        //    }
        //    if (carriedThing.def.ingestible.ingestHoldOffsetStanding != null)
        //    {
        //        HoldOffset holdOffset = carriedThing.def.ingestible.ingestHoldOffsetStanding.Pick(pawn.Rotation);
        //        if (holdOffset != null)
        //        {
        //            drawPos += holdOffset.offset;
        //            behind = holdOffset.behind;
        //            flip = holdOffset.flip;
        //            return true;
        //        }
        //    }
        //    return false;
        //}

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
