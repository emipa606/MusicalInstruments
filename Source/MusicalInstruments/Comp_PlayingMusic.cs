using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace MusicalInstruments;

internal class Comp_PlayingMusic : ThingComp
{
    public static readonly Dictionary<Pawn, Comp_PlayingMusic> Notebook = new();
    private Pawn currentPlayer;
    private Sustainer soundPlaying;
    private CompProp_PlayingMusic Props => (CompProp_PlayingMusic)props;

    public void StartPlaying(Pawn player)
    {
        if (Notebook.TryGetValue(player, out var otherComp) && otherComp != this)
        {
            otherComp.currentPlayer = null;
            otherComp.soundPlaying = null;
        }

        currentPlayer = player;
        Notebook[player] = this;
    }

    public void StopPlaying(Pawn pawn)
    {
        currentPlayer = null;

        if (Notebook.TryGetValue(pawn, out var comp) && comp == this)
        {
            Notebook.Remove(pawn);
        }
    }

    public override void CompTick()
    {
        if (currentPlayer != null)
        {
            if (Props.soundPlayInstrument != null && soundPlaying == null)
            {
                soundPlaying = Props.soundPlayInstrument.TrySpawnSustainer(
                    SoundInfo.InMap(new TargetInfo(currentPlayer.Position, currentPlayer.Map),
                        MaintenanceType.PerTick));
            }
        }
        else
        {
            soundPlaying = null;
        }

        soundPlaying?.Maintain();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }

        var commandAction = new Command_Action
        {
            defaultLabel = $"DEV: Toggle is playing, status: {currentPlayer != null}",
            action = delegate
            {
                currentPlayer = currentPlayer == null ? PawnsFinder.AllMaps_FreeColonists.FirstOrDefault() : null;
            }
        };
        yield return commandAction;
    }
}