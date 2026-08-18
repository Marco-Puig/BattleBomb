using System.Collections.Generic;
using BattleBomb.Core.Players;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// The one place players are resolved from. Replaces tag lookups, which cannot express two local
    /// players and could never express a remote one (§4).
    /// </summary>
    /// <remarks>
    /// Deliberately not a singleton (§9): a run owns an instance and hands it to whatever needs it.
    /// </remarks>
    public sealed class PlayerRegistry
    {
        private readonly Dictionary<int, IPlayerCommandSource> _sources = new Dictionary<int, IPlayerCommandSource>();

        public int Count => _sources.Count;

        public IEnumerable<IPlayerCommandSource> Sources => _sources.Values;

        /// <summary>Registers a source, replacing any source already held for that player.</summary>
        public void Register(IPlayerCommandSource source)
        {
            if (source == null)
            {
                throw new System.ArgumentNullException(nameof(source));
            }

            _sources[source.PlayerId.Value] = source;
        }

        public bool Unregister(PlayerId playerId) => _sources.Remove(playerId.Value);

        public bool TryGet(PlayerId playerId, out IPlayerCommandSource source) =>
            _sources.TryGetValue(playerId.Value, out source);

        public bool IsRegistered(PlayerId playerId) => _sources.ContainsKey(playerId.Value);

        /// <summary>
        /// Samples every registered player for one step. A player with no source contributes nothing
        /// rather than stalling the step.
        /// </summary>
        public void SampleAll(int frame, IDictionary<int, PlayerCommand> into)
        {
            if (into == null)
            {
                throw new System.ArgumentNullException(nameof(into));
            }

            into.Clear();
            foreach (KeyValuePair<int, IPlayerCommandSource> entry in _sources)
            {
                into[entry.Key] = entry.Value.Sample(frame);
            }
        }

        public void Clear() => _sources.Clear();
    }
}
