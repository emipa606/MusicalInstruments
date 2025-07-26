using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MusicalInstruments;

[HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.FirstUnloadableThing), MethodType.Getter)]
internal class Pawn_InventoryTracker_FirstUnloadableThing
{
    private static readonly List<ThingDefCount> tmpDrugsToKeep = [];

    private static bool Prefix(Pawn_InventoryTracker __instance, ref ThingCount __result)
    {
        if (__instance.innerContainer.Count == 0)
        {
            __result = default;
            return false;
        }

        tmpDrugsToKeep.Clear();

        if (__instance.pawn.drugs?.CurrentPolicy != null)
        {
            var currentPolicy = __instance.pawn.drugs.CurrentPolicy;
            for (var i = 0; i < currentPolicy.Count; i++)
            {
                if (currentPolicy[i].takeToInventory > 0)
                {
                    tmpDrugsToKeep.Add(new ThingDefCount(currentPolicy[i].drug, currentPolicy[i].takeToInventory));
                }
            }
        }

        Thing bestInstrument = null;

        if (!__instance.pawn.NonHumanlikeOrWildMan() && !__instance.pawn.WorkTagIsDisabled(WorkTags.Artistic))
        {
            var artSkill = __instance.pawn.skills.GetSkill(SkillDefOf.Artistic).levelInt;

            IEnumerable<Thing> heldInstruments = __instance.innerContainer
                .Where(PerformanceManager.IsInstrument)
                .Where(x => !x.TryGetComp<CompMusicalInstrument>().Props.isBuilding)
                .OrderByDescending(x => x.TryGetComp<CompMusicalInstrument>().WeightedSuitability(artSkill));

            if (heldInstruments.Any())
            {
                bestInstrument = heldInstruments.FirstOrDefault();
            }
        }

        if (tmpDrugsToKeep.Any() || bestInstrument != null)
        {
            foreach (var thing in __instance.innerContainer)
            {
                if (thing.def.IsDrug)
                {
                    var num = -1;

                    for (var k = 0; k < tmpDrugsToKeep.Count; k++)
                    {
                        if (thing.def != tmpDrugsToKeep[k].ThingDef)
                        {
                            continue;
                        }

                        num = k;
                        break;
                    }

                    if (num < 0)
                    {
                        __result = new ThingCount(thing,
                            thing.stackCount);
                        return false;
                    }

                    if (thing.stackCount > tmpDrugsToKeep[num].Count)
                    {
                        __result = new ThingCount(thing,
                            thing.stackCount - tmpDrugsToKeep[num].Count);
                        return false;
                    }

                    tmpDrugsToKeep[num] = new ThingDefCount(tmpDrugsToKeep[num].ThingDef,
                        tmpDrugsToKeep[num].Count - thing.stackCount);
                }
                else if (PerformanceManager.IsInstrument(thing))
                {
                    if (bestInstrument == null)
                    {
                        __result = new ThingCount(thing,
                            thing.stackCount);
                        return false;
                    }

                    if (bestInstrument.GetHashCode() == thing.GetHashCode())
                    {
                        continue;
                    }

                    __result = new ThingCount(thing,
                        thing.stackCount);
                    return false;
                }
                else
                {
                    __result = new ThingCount(thing,
                        thing.stackCount);
                    return false;
                }
            }

            __result = default;
            return false;
        }

        __result = new ThingCount(__instance.innerContainer[0], __instance.innerContainer[0].stackCount);
        return false;
    }
}