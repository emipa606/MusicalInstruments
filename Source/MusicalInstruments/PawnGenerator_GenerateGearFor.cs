using HarmonyLib;
using RimWorld;
using Verse;

namespace MusicalInstruments;

[HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor", typeof(Pawn), typeof(PawnGenerationRequest))]
internal class PawnGenerator_GenerateGearFor
{
    private static void Postfix(Pawn pawn, PawnGenerationRequest request)
    {
        if (Current.ProgramState != ProgramState.Playing)
        {
            return;
        }

        if (pawn.NonHumanlikeOrWildMan())
        {
            return;
        }

        if (pawn.Faction == null)
        {
            return;
        }

        if (pawn.Faction.IsPlayer)
        {
            return;
        }

        if (pawn.Faction.PlayerRelationKind == FactionRelationKind.Hostile)
        {
            return;
        }

        var artLevel = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
        var techLevel = request.Faction?.def.techLevel ?? TechLevel.Neolithic;
        ThingDef instrumentDef;
        ThingDef stuffDef;

        switch (artLevel)
        {
            case > 12:
            case > 8 when Rand.Chance(.75f):
            {
                if (!TryGetHardInstrument(techLevel, out instrumentDef, out stuffDef))
                {
                    return;
                }

                var instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
                pawn.inventory.TryAddItemNotForSale(instrument);
                break;
            }
            case > 4 when Rand.Chance(.75f):
            {
                if (!TryGetEasyInstrument(techLevel, out instrumentDef, out stuffDef))
                {
                    return;
                }

                var instrument = ThingMaker.MakeThing(instrumentDef, stuffDef);
                pawn.inventory.TryAddItemNotForSale(instrument);
                break;
            }
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