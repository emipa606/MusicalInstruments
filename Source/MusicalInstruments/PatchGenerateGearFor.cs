using HarmonyLib;
using RimWorld;
using Verse;

namespace MusicalInstruments;

[HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor", typeof(Pawn), typeof(PawnGenerationRequest))]
internal class PatchGenerateGearFor
{
    private static void Postfix(Pawn pawn, PawnGenerationRequest request)
    {
#if DEBUG
        Log.Message($"Trying to generate an instrument for {pawn.Label}");

#endif

        if (Current.ProgramState != ProgramState.Playing)
        {
#if DEBUG
            Log.Message("World generation phase, exit");
#endif
            return;
        }

        if (pawn.NonHumanlikeOrWildMan())
        {
#if DEBUG
            Log.Message("NonHumanlikeOrWildMan, exit");
#endif
            return;
        }

        if (pawn.Faction == null)
        {
#if DEBUG
            Log.Message("null faction, exit");
#endif
            return;
        }

        if (pawn.Faction.IsPlayer)
        {
#if DEBUG
            Log.Message("player faction, exit");
#endif
            return;
        }

        if (pawn.Faction.PlayerRelationKind == FactionRelationKind.Hostile)
        {
#if DEBUG
            Log.Message("hostile faction, exit");
#endif
            return;
        }

#if DEBUG
        Log.Message("continuing...");
#endif
        var artLevel = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
        var techLevel = request.Faction?.def.techLevel ?? TechLevel.Neolithic;
        ThingDef instrumentDef;
        ThingDef stuffDef;

        if (artLevel > 12 || artLevel > 8 && Rand.Chance(.75f))
        {
            if (!TryGetHardInstrument(techLevel, out instrumentDef, out stuffDef))
            {
                return;
            }

            var instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
            pawn.inventory.TryAddItemNotForSale(instrument);
        }
        else if (artLevel > 4 && Rand.Chance(.75f))
        {
            if (!TryGetEasyInstrument(techLevel, out instrumentDef, out stuffDef))
            {
                return;
            }

            var instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
            pawn.inventory.TryAddItemNotForSale(instrument);
        }
    }

    private static bool TryGetEasyInstrument(TechLevel techLevel, out ThingDef instrumentDef, out ThingDef stuffDef)
    {
        ThingDef frameDrum = null;
        ThingDef guitar = null;
        ThingDef lightLeather = null;
        ThingDef wood = null;
        ThingDef plasteel = null;

        instrumentDef = null;
        stuffDef = null;

        var neolithic = techLevel <= TechLevel.Neolithic;
        var spacer = techLevel >= TechLevel.Spacer;

        try
        {
            frameDrum = ThingDef.Named("FrameDrum");
        }
        catch
        {
            // ignored
        }

        try
        {
            guitar = ThingDef.Named("Guitar");
        }
        catch
        {
            // ignored
        }

        try
        {
            lightLeather = ThingDef.Named("Leather_Light");
        }
        catch
        {
            // ignored
        }

        try
        {
            wood = ThingDef.Named("WoodLog");
        }
        catch
        {
            // ignored
        }

        try
        {
            plasteel = ThingDef.Named("Plasteel");
        }
        catch
        {
            // ignored
        }

        if (guitar != null && !neolithic)
        {
            if (plasteel != null && spacer)
            {
                instrumentDef = guitar;
                stuffDef = plasteel;
                return true;
            }

            if (wood != null)
            {
                instrumentDef = guitar;
                stuffDef = wood;
                return true;
            }
        }

        if (frameDrum == null || lightLeather == null)
        {
            return false;
        }

        instrumentDef = frameDrum;
        stuffDef = lightLeather;
        return true;
    }

    private static bool TryGetHardInstrument(TechLevel techLevel, out ThingDef instrumentDef, out ThingDef stuffDef)
    {
        ThingDef ocarina = null;
        ThingDef violin = null;
        ThingDef jade = null;
        ThingDef wood = null;
        ThingDef plasteel = null;

        instrumentDef = null;
        stuffDef = null;

        var neolithic = techLevel <= TechLevel.Neolithic;
        var spacer = techLevel >= TechLevel.Spacer;

        try
        {
            ocarina = ThingDef.Named("Ocarina");
        }
        catch
        {
            // ignored
        }

        try
        {
            violin = ThingDef.Named("Violin");
        }
        catch
        {
            // ignored
        }

        try
        {
            jade = ThingDef.Named("Jade");
        }
        catch
        {
            // ignored
        }

        try
        {
            wood = ThingDef.Named("WoodLog");
        }
        catch
        {
            // ignored
        }

        try
        {
            plasteel = ThingDef.Named("Plasteel");
        }
        catch
        {
            // ignored
        }

        if (violin != null && !neolithic)
        {
            if (plasteel != null && spacer)
            {
                instrumentDef = violin;
                stuffDef = plasteel;
                return true;
            }

            if (wood != null)
            {
                instrumentDef = violin;
                stuffDef = wood;
                return true;
            }
        }

        if (ocarina == null || jade == null)
        {
            return false;
        }

        instrumentDef = ocarina;
        stuffDef = jade;
        return true;
    }
}