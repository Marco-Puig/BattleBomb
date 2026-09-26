using System;
using BattleBomb.Core.Items;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// Where one player's menu actions go (HANDOFF-M8 planning decision 11). On the couch and on the host
    /// the answer comes inside <see cref="Send"/>; on a guest it comes a round trip later, after the host's
    /// copy of the bag has arrived. A screen writes the code after an action once, in the callback, and it
    /// reads the same both ways.
    /// </summary>
    public interface IPlayerRequests
    {
        /// <summary>An earlier request is still waiting for its answer. Never true on the couch.</summary>
        bool Pending { get; }

        /// <summary>Runs or sends <paramref name="request"/> for this player; <paramref name="answered"/>
        /// (may be null) hears what happened. The sender stamps the player and the sack revision.</summary>
        void Send(PlayerRequest request, Action<RequestOutcome> answered);
    }
}
