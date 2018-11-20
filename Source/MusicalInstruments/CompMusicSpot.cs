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
    public class CompMusicSpot : ThingComp
    {
        private bool active = true;

        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (value == active) return;
                active = value;
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
                PerformanceManager pm = this.parent.Map.GetComponent<PerformanceManager>();
                pm.RegisterDeactivatedMusicSpot(this);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Toggle com = new Command_Toggle();
            com.hotKey = KeyBindingDefOf.Designator_RotateLeft;
            com.defaultLabel = "Music spot";
            com.icon = TexCommand.GatherSpotActive;
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
