using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MusicalInstruments
{
    [StaticConstructorOnStartup]
    public class CompMusicSpot : ThingComp
    {
        public static readonly Texture2D MusicSpotIcon = ContentFinder<Texture2D>.Get("UI/Icons/MusicSpot");
        public static readonly Texture2D AllowRecreationIcon = ContentFinder<Texture2D>.Get("UI/Icons/AllowRecreation");
        private bool active = true;
        private bool allowRecreation = true;

        public CompProperties_MusicSpot Props => (CompProperties_MusicSpot) props;

        public bool Active
        {
            get => active || !Props.canBeDisabled;
            set
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
            set
            {
                var actualValue = value && IsInstrument();

                allowRecreation = actualValue;
            }
        }

        public bool IsActive()
        {
            return Active;
        }

        public bool RecreationAllowed()
        {
            return AllowRecreation;
        }

        public bool IsInstrument()
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

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
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
                    defaultLabel = "Music spot",
                    icon = MusicSpotIcon,
                    isActive = IsActive,
                    toggleAction = delegate { Active = !Active; },
                    defaultDesc = Active
                        ? "Active: colonists will play music here."
                        : "Inactive: colonists will not play music here."
                };
                yield return com;
            }
            else if (IsInstrument())
            {
                var com = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Misc5,
                    defaultLabel = "Allow recreation",
                    icon = AllowRecreationIcon,
                    isActive = RecreationAllowed,
                    toggleAction = delegate { AllowRecreation = !AllowRecreation; },
                    defaultDesc = AllowRecreation
                        ? "Recreation allowed: colonists can play this instrument for any reason."
                        : "Recreation disallowed: colonists can only play this instrument when performing."
                };
                yield return com;
            }
        }
    }
}