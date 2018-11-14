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

        //[TweakValue("MusicalInstruments.XOffset", -0.5f, 0.5f)]
        //private static float InstrumentXOffset = .0f;

        //[TweakValue("MusicalInstruments.ZOffset", -0.5f, 0.5f)]
        //private static float InstrumentZOffset = .0f;

        //[TweakValue("MusicalInstruments.Behind", 0f, 100f)]
        //private static bool Behind = false;

        //[TweakValue("MusicalInstruments.Flip", 0f, 100f)]
        //private static bool Flip = false;

        private const TargetIndex GatherSpotParentInd = TargetIndex.A;

        private const TargetIndex StandingSpotInd = TargetIndex.B;

        private const TargetIndex InstrumentInd = TargetIndex.C;

        public int Luck { get; }

        public JobDriver_MusicPlay() : base ()
        {
            Luck = Verse.Rand.Range(-3, 2);
        }

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
            this.EndOnDespawnedOrNull(GatherSpotParentInd, JobCondition.Incompletable);

            //Verse.Log.Message(String.Format("Gather Spot ID = {0}", TargetA.Thing.GetHashCode()));

            Pawn musician = this.pawn;

            this.FailOnDestroyedNullOrForbidden(InstrumentInd);

            Thing instrument = this.TargetC.Thing;

            Thing venue = this.TargetA.Thing;

            if (instrument.ParentHolder != musician.inventory)
            {
                // go to where instrument is
                yield return Toils_Goto.GotoThing(InstrumentInd, PathEndMode.OnCell).FailOnSomeonePhysicallyInteracting(InstrumentInd);

                //drop other instruments if any
                List<Thing> heldInstruments = pawn.inventory.innerContainer.Where(x => JoyGiver_MusicPlay.IsInstrument(x)).ToList();

                if(heldInstruments.Any())
                {
                    Toil dropInstruments = new Toil();
                    dropInstruments.initAction = delegate
                    {
                        Thing result;

                        foreach (Thing heldInstrument in heldInstruments)
                        {
                            pawn.inventory.innerContainer.TryDrop(heldInstrument, pawn.Position, pawn.Map, ThingPlaceMode.Near, out result);
                        }
                    };

                    yield return dropInstruments;
                }

                // pick up instrument
                yield return Toils_Haul.StartCarryThing(InstrumentInd);
            }
            else
            {
                //get instrument out ready to play
                yield return Toils_Misc.TakeItemFromInventoryToCarrier(musician, InstrumentInd);

            }
            
            // go to the sitting / standing spot
            yield return Toils_Goto.GotoCell(StandingSpotInd, PathEndMode.OnCell);

            // custom toil.
            Toil play = new Toil();

            play.initAction = delegate
            {
                pawn.Map.GetComponent<PerformanceManager>().StartPlaying(musician, venue);
            };



            play.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(this.ClosestGatherSpotParentCell);
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

            yield return play;

            yield return Toils_General.PutCarriedThingInInventory();

        }

        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        {
            Thing instrument = this.TargetC.Thing;
            CompProperties_MusicalInstrument props = (CompProperties_MusicalInstrument)(instrument.TryGetComp<CompMusicalInstrument>().props);

            Rot4 rotation = pawn.Rotation;
                       
            if (rotation == Rot4.North)
            {
                behind = true;

                if (!pawn.pather.Moving)
                {
                    drawPos += new Vector3(0f - props.xOffsetFacing, 0f, props.zOffset);
                }
                return true;
            }
            else if (rotation == Rot4.East)
            {
                if (!pawn.pather.Moving)
                {
                    flip = true;
                    drawPos += new Vector3(props.xOffset, 0f, props.zOffset);
                }
                return true;
            }
            else if (rotation == Rot4.South)
            {
                if (!pawn.pather.Moving)
                {
                    flip = true;
                    drawPos += new Vector3(props.xOffsetFacing, 0f, props.zOffset);
                }
                return true;
            }
            else if (rotation == Rot4.West)
            {
                if (!pawn.pather.Moving)
                {
                    drawPos += new Vector3(0f - props.xOffset, 0f, props.zOffset);
                }
                return true;
            }

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

    }
}
