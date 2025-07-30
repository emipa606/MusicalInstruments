using RimWorld;
using Verse;
using Verse.AI;

namespace MusicalInstruments;

public class JobDriver_MusicPlayJoy : JobDriver_MusicPlayBase
{
    private int tickInterval;

    protected override Toil GetPlayToil(Pawn musician, Thing instrument, Thing venue)
    {
        // custom toil.
        var play = new Toil();

        var props = instrument.TryGetComp<CompMusicalInstrument>().Props;

        play.initAction = delegate
        {
            pawn.Map.GetComponent<PerformanceManager>().StartPlaying(musician, instrument, venue, false);
        };


        play.tickIntervalAction = delegate(int delta)
        {
            tickInterval += delta;
            if (props.isBuilding)
            {
                pawn.rotationTracker.FaceTarget(TargetC);
                pawn.GainComfortFromCellIfPossible(delta);
            }
            else
            {
                pawn.rotationTracker.FaceCell(ClosestMusicSpotParentCell);
            }

            if (tickInterval > 100)
            {
                ThrowMusicNotes(musician.DrawPos, Map);
                tickInterval = 0;
            }

            JoyUtility.JoyTickCheckEnd(musician, delta, JoyTickFullJoyAction.GoToNextToil);
        };

        play.handlingFacing = true;
        play.defaultCompleteMode = ToilCompleteMode.Delay;
        play.defaultDuration = job.def.joyDuration;

        play.AddFinishAction(delegate
        {
            pawn.Map.GetComponent<PerformanceManager>().StopPlaying(musician, venue);

            if (pawn.carryTracker.CarriedThing == null)
            {
                return;
            }

            if (!pawn.carryTracker.innerContainer.TryTransferToContainer(pawn.carryTracker.CarriedThing,
                    pawn.inventory.innerContainer))
            {
                _ = pawn.carryTracker.TryDropCarriedThing(pawn.Position,
                    pawn.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out _);
            }
        });

        play.socialMode = RandomSocialMode.Quiet;

        return play;
    }
}