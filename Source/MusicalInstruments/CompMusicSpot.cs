using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MusicalInstruments;

[StaticConstructorOnStartup]
public class CompMusicSpot : ThingComp
{
    private static readonly Texture2D MusicSpotIcon = ContentFinder<Texture2D>.Get("UI/Icons/MusicSpot");
    private static readonly Texture2D AllowRecreationIcon = ContentFinder<Texture2D>.Get("UI/Icons/AllowRecreation");
    private bool active = true;
    private bool allowRecreation = true;

    private CompProperties_MusicSpot Props => (CompProperties_MusicSpot)props;

    public bool Active
    {
        get => active || !Props.canBeDisabled;
        private set
        {
            var actualValue = value || !Props.canBeDisabled;

            if (actualValue == active)
            {
                return;
            }

            active = actualValue;
            if (!parent.Spawned)
            {
                return;
            }

            var pm = parent.Map.GetComponent<PerformanceManager>();
            if (active)
            {
                pm.RegisterActivatedMusicSpot(this);
            }
            else
            {
                pm.RegisterDeactivatedMusicSpot(this);
            }
        }
    }

    public bool AllowRecreation
    {
        get => allowRecreation && IsInstrument();
        private set
        {
            var actualValue = value && IsInstrument();

            allowRecreation = actualValue;
        }
    }

    private bool IsActive()
    {
        return Active;
    }

    private bool RecreationAllowed()
    {
        return AllowRecreation;
    }

    private bool IsInstrument()
    {
        return !(Props.canBeDisabled || parent.def.defName == "MusicSpot");
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref active, "MusicalInstruments.MusicSpotIsActive");
        Scribe_Values.Look(ref allowRecreation, "MusicalInstruments.MusicSpotAllowRecreation");
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (parent.Faction != Faction.OfPlayer && !respawningAfterLoad)
        {
            active = false;
        }

        if (!Active)
        {
            return;
        }

        var pm = parent.Map.GetComponent<PerformanceManager>();
        pm.RegisterActivatedMusicSpot(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        if (!Active)
        {
            return;
        }

        var pm = map.GetComponent<PerformanceManager>();
        pm.RegisterDeactivatedMusicSpot(this);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (Props.canBeDisabled)
        {
            var com = new Command_Toggle
            {
                hotKey = KeyBindingDefOf.Misc5,
                defaultLabel = "MuIn.MusicSpot".Translate(),
                icon = MusicSpotIcon,
                isActive = IsActive,
                toggleAction = delegate { Active = !Active; },
                defaultDesc = Active
                    ? "MuIn.MusicSpot.Active".Translate()
                    : "MuIn.MusicSpot.Inactive".Translate()
            };
            yield return com;
        }
        else if (IsInstrument())
        {
            var com = new Command_Toggle
            {
                hotKey = KeyBindingDefOf.Misc5,
                defaultLabel = "MuIn.AllowRec".Translate(),
                icon = AllowRecreationIcon,
                isActive = RecreationAllowed,
                toggleAction = delegate { AllowRecreation = !AllowRecreation; },
                defaultDesc = AllowRecreation
                    ? "MuIn.AllowRec.Active".Translate()
                    : "MuIn.AllowRec.Inactive".Translate()
            };
            yield return com;
        }
    }
}