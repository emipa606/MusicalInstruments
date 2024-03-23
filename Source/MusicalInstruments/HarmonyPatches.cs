using System.Reflection;
using HarmonyLib;
using Verse;

namespace MusicalInstruments;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
    static HarmonyPatches()
    {
        new Harmony("com.dogproblems.rimworldmods.musicalinstruments").PatchAll(Assembly.GetExecutingAssembly());
    }
}