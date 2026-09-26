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

        /// <summary>This player's hands are on this machine — a device, a script, a replay — so their menus open
        /// on this display (HANDOFF-M8 planning decision 15). False for a remote player, and for nobody.</summary>
        public bool IsLocal(PlayerId playerId) =>
            _sources.TryGetValue(playerId.Value, out IPlayerCommandSource source) && !(source is IRemotePlayerSource);

        /// <summary>How many players this display is for: two on the couch; one alone, or online.</summary>
        public int LocalCount
        {
            get
            {
                int count = 0;
                foreach (IPlayerCommandSource source in _sources.Values)
                {
                    if (!(source is IRemotePlayerSource))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Which button pictures this player's prompts show. Keyboard for a source that
        /// is not a device — a test script, a replay, later a remote peer.</summary>
        public InputFamily FamilyOf(PlayerId playerId) =>
            _sources.TryGetValue(playerId.Value, out IPlayerCommandSource source)
                && source is IInputDeviceReport report
                ? report.Family
                : InputFamily.Keyboard;

        /// <summary>The device this player last pressed, for the front door's seating (D57).</summary>
        public int LastDeviceOf(PlayerId playerId) =>
            _sources.TryGetValue(playerId.Value, out IPlayerCommandSource source)
                && source is IInputDeviceReport report
                ? report.LastDeviceId
                : SeatAssignment.NoDevice;

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
