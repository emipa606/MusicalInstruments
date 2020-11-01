using System;
using System.Collections.Generic;

using UnityEngine;

using Verse;

using RimWorld;

namespace MusicalInstruments
{
    [StaticConstructorOnStartup]
    public class CompMusicSpot : ThingComp
    {
        private bool active = true;
        private bool allowRecreation = true;

        public static readonly Texture2D MusicSpotIcon = ContentFinder<Texture2D>.Get("UI/Icons/MusicSpot");
        public static readonly Texture2D AllowRecreationIcon = ContentFinder<Texture2D>.Get("UI/Icons/AllowRecreation");

        public CompProperties_MusicSpot Props => (CompProperties_MusicSpot)props;

        public bool Active
        {
            get => active || !Props.canBeDisabled;
            set
            {
                bool actualValue = value || !Props.canBeDisabled;

                if (actualValue == active)
                {
                    return;
                }

                active = actualValue;
                if (parent.Spawned)
                {
                    PerformanceManager pm = parent.Map.GetComponent<PerformanceManager>();
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
        }

        public bool AllowRecreation
        {
            get => allowRecreation && IsInstrument();
            set
            {
                bool actualValue = value && IsInstrument();

                if (actualValue == allowRecreation)
                {
                    return;
                }

                allowRecreation = actualValue;
            }
        }

        public bool IsActive() { return Active; }

        public bool RecreationAllowed() { return AllowRecreation; }

        public bool IsInstrument()
        {
            return !(Props.canBeDisabled || parent.def.defName == "MusicSpot");
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref active, "MusicalInstruments.MusicSpotIsActive", false, false);
            Scribe_Values.Look<bool>(ref allowRecreation, "MusicalInstruments.MusicSpotAllowRecreation", false, false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent.Faction != Faction.OfPlayer && !respawningAfterLoad)
            {
                active = false;
            }
            if (Active)
            {
                PerformanceManager pm = parent.Map.GetComponent<PerformanceManager>();
                pm.RegisterActivatedMusicSpot(this);
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (Active)
            {
                PerformanceManager pm = map.GetComponent<PerformanceManager>();
                pm.RegisterDeactivatedMusicSpot(this);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Props.canBeDisabled)
            {
                Command_Toggle com = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Misc5,
                    defaultLabel = "Music spot",
                    icon = MusicSpotIcon,
                    isActive = new Func<bool>(IsActive),
                    toggleAction = delegate
                    {
                        Active = !Active;
                    }
                };
                com.defaultDesc = Active ? "Active: colonists will play music here." : "Inactive: colonists will not play music here.";
                yield return com;
            } else if (IsInstrument())
            {
                Command_Toggle com = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Misc5,
                    defaultLabel = "Allow recreation",
                    icon = AllowRecreationIcon,
                    isActive = new Func<bool>(RecreationAllowed),
                    toggleAction = delegate
                    {
                        AllowRecreation = !AllowRecreation;
                    }
                };
                com.defaultDesc = AllowRecreation
                    ? "Recreation allowed: colonists can play this instrument for any reason."
                    : "Recreation disallowed: colonists can only play this instrument when performing.";
                yield return com;


            }
        }

    }
}
