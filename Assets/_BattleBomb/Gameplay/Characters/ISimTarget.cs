using UnityEngine;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// Something a player's attack can find: a training dummy or an enemy. The registry orders by
    /// the body's name (D10), and hit resolution skips whatever is depleted.
    /// </summary>
    public interface ISimTarget
    {
        /// <summary>The component identity — event payloads and deterministic naming hang off it.</summary>
        Component Body { get; }

        Vector3 Position { get; }

        bool IsDepleted { get; }
    }
}
