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
    public abstract class JobDriver_MusicPlayBase : JobDriver
    {
        //[TweakValue("MusicalInstruments.XOffset", -0.5f, 0.5f)]
        //private static float InstrumentXOffset = .0f;

        //[TweakValue("MusicalInstruments.ZOffset", -0.5f, 0.5f)]
        //private static float InstrumentZOffset = .0f;

        //[TweakValue("MusicalInstruments.Behind", 0f, 100f)]
        //private static bool Behind = false;

        //[TweakValue("MusicalInstruments.Flip", 0f, 100f)]
        //private static bool Flip = false;

        protected const TargetIndex GatherSpotParentInd = TargetIndex.A;

        protected const TargetIndex StandingSpotInd = TargetIndex.B;

        protected const TargetIndex InstrumentInd = TargetIndex.C;

        public int Luck { get; }

        public JobDriver_MusicPlayBase() : base()
        {
            Luck = Verse.Rand.Range(-3, 2);
        }

        protected Thing GatherSpotParent
        {
            get
            {
                return this.job.GetTarget(GatherSpotParentInd).Thing;
            }
        }

        protected IntVec3 ClosestGatherSpotParentCell
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
