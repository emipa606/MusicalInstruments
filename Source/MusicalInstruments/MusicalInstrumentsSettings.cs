using Verse;

namespace MusicalInstruments;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class MusicalInstrumentsSettings : ModSettings
{
    public bool PlayMusic = true;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref PlayMusic, "CheckboxValue", true);
    }
}