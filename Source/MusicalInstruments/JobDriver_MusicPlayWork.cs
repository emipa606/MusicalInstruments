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
                List<Thing> heldInstruments = pawn.inventory.innerContainer.Where(x => PerformanceManager.IsInstrument(x)).ToList();

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
                this.pawn.skills.Learn(SkillDefOf.Artistic, 0.1f, false);
                //JoyUtility.JoyTickCheckEnd(musician, JoyTickFullJoyAction.GoToNextToil, 1f, null);

                if (this.ticksLeftThisToil % 100 == 99)
                {
                    ThrowMusicNotes(musician.DrawPos, this.Map);
                    //pawn.Map.GetComponent<PerformanceManager>().ApplyThoughts(venue);
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

            yield return play;

            yield return Toils_General.PutCarriedThingInInventory();

        }



    }
}
