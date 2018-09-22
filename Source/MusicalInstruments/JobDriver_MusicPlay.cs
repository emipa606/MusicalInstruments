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

            return true;
        }


        // this function does three things:
        // it adds generic delegate functions to globalFailConditions (inherited from IJobEndable) via `This.EndOn...` extensions
        // it also yield returns a collection of toils: some generic, some custom
        // it also interacts with the JoyUtility static class so the pawns get joy
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);

            Pawn musician = this.pawn;

            this.FailOnDestroyedNullOrForbidden(TargetIndex.C);


            Thing instrument = this.TargetC.Thing;

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
            play.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(this.ClosestGatherSpotParentCell);
                JoyUtility.JoyTickCheckEnd(musician, JoyTickFullJoyAction.GoToNextToil, 0.2f, null);
                

                if(this.ticksLeftThisToil % 250 == 249)
                {
                    ThrowMusicNotes(musician.DrawPos, this.Map);
                    float musicQuality = GetMusicQuality(musician, instrument);
                    musician.skills.Learn(SkillDefOf.Artistic, 5.0f);


                    List<Pawn> audience = new List<Pawn>();

                    foreach(Pawn audiencePawn in Map.mapPawns.FreeColonistsAndPrisoners)
                    {
                        if (audiencePawn.Position.DistanceTo(pawn.Position) < 8 && audiencePawn != musician)
                        {
                            audiencePawn.needs.joy.GainJoy(musicQuality * 2.5f, JoyKindDefOf_Music.Music);
                            audience.Add(audiencePawn);
                        }
                    }


                    Verse.Log.Message(String.Format("musician: {0}, quality: {1}, audience: {2}", musician.Name.ToString(), musicQuality, String.Join(", ", audience.Select(p => p.Name.ToString()).ToArray())));
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

        }

        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        {
            IntVec3 closestGatherSpotParentCell = this.ClosestGatherSpotParentCell;
            //return JobDriver_Ingest.ModifyCarriedThingDrawPosWorker(ref drawPos, ref behind, ref flip, closestGatherSpotParentCell, this.pawn);
            return false;
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

        protected float GetMusicQuality(Pawn musician, Thing instrument)
        {
            int artSkill = musician.skills.GetSkill(SkillDefOf.Artistic).Level;
            bool isInspired = musician.Inspired ? musician.Inspiration.def == InspirationDefOf.Inspired_Creativity : false;
            QualityCategory instrumentQuality = QualityCategory.Normal;
            instrument.TryGetQuality(out instrumentQuality);
            float instrumentCondition = (float)instrument.HitPoints / instrument.MaxHitPoints;
            CompMusicalInstrument instrumentComp = instrument.TryGetComp<CompMusicalInstrument>();
            float easiness = instrumentComp.Props.easiness;
            float expressiveness = instrumentComp.Props.expressiveness;


            return (easiness + (expressiveness * (artSkill / 10.0f) * (isInspired ? 2.0f : 1.0f))) * ((float)instrumentQuality / 3.0f + 0.1f) * instrumentCondition;
        }
    }
}
