﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Verse;
using Verse.AI;

using RimWorld;

namespace MusicalInstruments
{
    [StaticConstructorOnStartup]
    public class CompMusicSpot : ThingComp
    {
        private bool active = true;

        public static readonly Texture2D MusicSpotIcon = ContentFinder<Texture2D>.Get("UI/Icons/MusicSpot");

        public CompProperties_MusicSpot Props
        {
            get
            {
                return (CompProperties_MusicSpot)this.props;
            }
        }

        public bool Active
        {
            get
            {   
                return active || !Props.canBeDisabled;
            }
            set
            {
                bool actualValue = value || !Props.canBeDisabled;

                if (actualValue == active) return;
                active = actualValue;
                if(parent.Spawned)
                {
                    PerformanceManager pm = parent.Map.GetComponent<PerformanceManager>();
                    if (active)
                        pm.RegisterActivatedMusicSpot(this);
                    else
                        pm.RegisterDeactivatedMusicSpot(this);
                }
            }
        }

        public bool IsActive() { return Active; }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.active, "MusicalInstruments.MusicSpotIsActive", false, false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
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
                Command_Toggle com = new Command_Toggle();
                com.hotKey = KeyBindingDefOf.Misc5;
                com.defaultLabel = "Music spot";
                com.icon = MusicSpotIcon;
                com.isActive = new Func<bool>(IsActive);
                com.toggleAction = delegate
                {
                    Active = !Active;
                };
                if (Active)
                {
                    com.defaultDesc = "Active - colonists will play music here";
                }
                else
                {
                    com.defaultDesc = "Inactive - colonists will not play music here";
                }
                yield return com;
            }
        }

    }
}
