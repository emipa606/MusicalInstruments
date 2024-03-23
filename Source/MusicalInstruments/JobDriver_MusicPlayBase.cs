using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MusicalInstruments;

public abstract class JobDriver_MusicPlayBase : JobDriver
{
    protected const TargetIndex MusicSpotParentInd = TargetIndex.A;

    protected const TargetIndex StandingSpotOrChairInd = TargetIndex.B;

    protected const TargetIndex InstrumentInd = TargetIndex.C;

    private int luck;

    public JobDriver_MusicPlayBase()
    {
        luck = Rand.Range(-3, 2);
    }

    public int Luck => luck;

    protected Thing MusicSpotParent => job.GetTarget(MusicSpotParentInd).Thing;

    protected IntVec3 ClosestMusicSpotParentCell => MusicSpotParent.OccupiedRect().ClosestCellTo(pawn.Position);

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        var pawn1 = pawn;

        var target = job.GetTarget(StandingSpotOrChairInd);

        // try to reserve a place to sit or stand
        if (!pawn1.Reserve(target, job, 1, -1, null, errorOnFailed))
        {
            return false;
        }

        target = job.GetTarget(InstrumentInd);

        // try to reserve an instrument to play
        return pawn1.Reserve(target, job, 1, -1, null, errorOnFailed);
    }

    public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
    {
        var instrument = TargetC.Thing;
        var props = (CompProperties_MusicalInstrument)instrument.TryGetComp<CompMusicalInstrument>().props;

        var rotation = pawn.Rotation;

        if (rotation == Rot4.North)
        {
            if (!pawn.pather.Moving)
            {
                drawPos += new Vector3(0f - props.xOffsetFacing, 0f, props.zOffsetFacing);
            }

            return true;
        }

        if (rotation == Rot4.East)
        {
            if (pawn.pather.Moving)
            {
                return true;
            }

            drawPos += new Vector3(props.xOffset, 0f, props.zOffset);
            return true;
        }

        if (rotation == Rot4.South)
        {
            if (pawn.pather.Moving)
            {
                return true;
            }

            flip = !props.vertical;

            drawPos += new Vector3(props.xOffsetFacing, 0f, props.zOffsetFacing);
            return true;
        }

        if (rotation != Rot4.West)
        {
            return false;
        }

        if (!pawn.pather.Moving)
        {
            drawPos += new Vector3(0f - props.xOffset, 0f, props.zOffset);
        }

        flip = !props.vertical;


        return true;
    }

    protected void ThrowMusicNotes(Vector3 loc, Map map)
    {
        if (!loc.ToIntVec3().ShouldSpawnMotesAt(map))
        {
            return;
        }

        var moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_MusicNotes"));
        moteThrown.Scale = 1.0f;
        moteThrown.exactPosition = loc + new Vector3(0f, 0f, 0.5f);
        moteThrown.SetVelocity(Rand.Range(-10, 10), Rand.Range(0.4f, 0.6f));
        _ = GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
    }


    protected abstract Toil GetPlayToil(Pawn musician, Thing instrument, Thing venue);

    protected bool PowerMissing(Thing instrument)
    {
        var propsinstrument = instrument.TryGetComp<CompMusicalInstrument>().Props;
        if (!propsinstrument.isBuilding)
        {
            return false;
        }

        var compPower = instrument.TryGetComp<CompPowerTrader>();

        return compPower is { PowerOn: false };
    }

    // this function does three things:
    // it adds generic delegate functions to globalFailConditions (inherited from IJobEndable) via `This.EndOn...` extensions
    // it also yields returns a collection of toils: some generic, some custom
    // it also interacts with the JoyUtility static class so the pawns get joy

    protected override IEnumerable<Toil> MakeNewToils()
    {
        _ = this.EndOnDespawnedOrNull(MusicSpotParentInd);

        //Verse.Log.Message(String.Format("Gather Spot ID = {0}", TargetA.Thing.GetHashCode()));

        var musician = pawn;

        _ = this.FailOnDestroyedNullOrForbidden(InstrumentInd);

        var instrument = TargetC.Thing;

        var venue = TargetA.Thing;

        var props = instrument.TryGetComp<CompMusicalInstrument>().Props;

        if (props.isBuilding)
        {
            _ = this.FailOn(() => PowerMissing(instrument));

            // go to where instrument is
            yield return Toils_Goto.GotoThing(InstrumentInd, PathEndMode.InteractionCell)
                .FailOnSomeonePhysicallyInteracting(InstrumentInd);

            yield return GetPlayToil(musician, instrument, venue);
        }
        else
        {
            if (instrument.ParentHolder != musician.inventory)
            {
                // go to where instrument is
                yield return Toils_Goto.GotoThing(InstrumentInd, PathEndMode.OnCell)
                    .FailOnSomeonePhysicallyInteracting(InstrumentInd);

                //drop other instruments if any
                var heldInstruments = pawn.inventory.innerContainer.Where(PerformanceManager.IsInstrument)
                    .ToList();

                if (heldInstruments.Any())
                {
                    var dropInstruments = new Toil
                    {
                        initAction = delegate
                        {
                            foreach (var heldInstrument in heldInstruments)
                            {
                                _ = pawn.inventory.innerContainer.TryDrop(heldInstrument, pawn.Position, pawn.Map,
                                    ThingPlaceMode.Near, out _);
                            }
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
            yield return Toils_Goto.GotoCell(StandingSpotOrChairInd, PathEndMode.OnCell);

            yield return GetPlayToil(musician, instrument, venue);

            //yield return Toils_General.PutCarriedThingInInventory();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref luck, "MusicalInstruments.Luck");
    }
}