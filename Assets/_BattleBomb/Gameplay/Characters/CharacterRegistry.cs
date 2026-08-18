using System.Collections.Generic;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// The scene's characters in ascending <see cref="Core.Players.PlayerId"/> order. Stepping
    /// walks this list, so the simulation's outcome never depends on scene load order (D10).
    /// </summary>
    public sealed class CharacterRegistry
    {
        private static readonly System.Comparison<CharacterActor> ByPlayerId =
            (a, b) => a.PlayerId.Value.CompareTo(b.PlayerId.Value);

        private readonly List<CharacterActor> _actors = new List<CharacterActor>();

        /// <summary>
        /// Sorted on every read because a PlayerInput assigns its player index after registration —
        /// an order frozen at Register time could be stale.
        /// </summary>
        public IReadOnlyList<CharacterActor> Ordered
        {
            get
            {
                _actors.Sort(ByPlayerId);
                return _actors;
            }
        }

        public void Register(CharacterActor actor)
        {
            if (actor == null || _actors.Contains(actor))
            {
                return;
            }

            _actors.Add(actor);
        }

        public void Unregister(CharacterActor actor) => _actors.Remove(actor);
    }
}
