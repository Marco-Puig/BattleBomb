using BattleBomb.Core.Players;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// Anything that can produce one player's <see cref="PlayerCommand"/> for a simulation step: a
    /// local device, a replay, an AI stand-in, or later a remote peer. The simulation only ever sees
    /// this interface (§4).
    /// </summary>
    public interface IPlayerCommandSource
    {
        PlayerId PlayerId { get; }

        /// <summary>Produces this player's command for <paramref name="frame"/>.</summary>
        PlayerCommand Sample(int frame);
    }
}
