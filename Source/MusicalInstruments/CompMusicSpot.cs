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
    [StaticConstructorOnStartup]
    public class CompMusicSpot : ThingComp
    {
        private bool active = true;
        private bool allowRecreation = true;

        public static readonly Texture2D MusicSpotIcon = ContentFinder<Texture2D>.Get("UI/Icons/MusicSpot");
        public static readonly Texture2D AllowRecreationIcon = ContentFinder<Texture2D>.Get("UI/Icons/AllowRecreation");

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

        public bool AllowRecreation
        {
            get
            {
                return allowRecreation && IsInstrument();
            }
            set
            {
                bool actualValue = value && IsInstrument();

                if (actualValue == allowRecreation) return;
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
            Scribe_Values.Look<bool>(ref this.active, "MusicalInstruments.MusicSpotIsActive", false, false);
            Scribe_Values.Look<bool>(ref this.allowRecreation, "MusicalInstruments.MusicSpotAllowRecreation", false, false);
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
                    com.defaultDesc = "Active: colonists will play music here.";
                }
                else
                {
                    com.defaultDesc = "Inactive: colonists will not play music here.";
                }
                yield return com;
            } else if (IsInstrument())
            {
                Command_Toggle com = new Command_Toggle();
                com.hotKey = KeyBindingDefOf.Misc5;
                com.defaultLabel = "Allow recreation";
                com.icon = AllowRecreationIcon;
                com.isActive = new Func<bool>(RecreationAllowed);
                com.toggleAction = delegate
                {
                    AllowRecreation = !AllowRecreation;
                };
                if (AllowRecreation)
                {
                    com.defaultDesc = "Recreation allowed: colonists can play this instrument for any reason.";
                }
                else
                {
                    com.defaultDesc = "Recreation disallowed: colonists can only play this instrument when performing.";
                }
                yield return com;


            }
        }

    }
}
