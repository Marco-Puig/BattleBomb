using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// What happens when one element lands on a mark another element left (D41) — the elemental
    /// depth Castle Crashers never had. The vocabulary is deliberately tiny: hurt more, hold them
    /// still, and whether the mark is spent doing it.
    /// </summary>
    public readonly struct ReactionSpec
    {
        /// <summary>Extra damage, as a share of the hit that triggered it.</summary>
        public readonly float BurstDamageFraction;

        /// <summary>Steps the target is held. The anchor pattern (D19's chain-stun) lives here.</summary>
        public readonly int StunSteps;

        /// <summary>Whether triggering spends the carried mark, or leaves it burning.</summary>
        public readonly bool ConsumesStatus;

        public ReactionSpec(float burstDamageFraction, int stunSteps, bool consumesStatus)
        {
            BurstDamageFraction = Mathf.Max(0f, burstDamageFraction);
            StunSteps = Mathf.Max(0, stunSteps);
            ConsumesStatus = consumesStatus;
        }
    }

    /// <summary>One authored pair: an element already marking the target, met by an incoming one.</summary>
    public readonly struct ReactionEntry
    {
        public readonly ElementId Carried;
        public readonly ElementId Incoming;
        public readonly ReactionSpec Spec;

        public ReactionEntry(ElementId carried, ElementId incoming, in ReactionSpec spec)
        {
            Carried = carried;
            Incoming = incoming;
            Spec = spec;
        }

        public bool IsValid => !Carried.IsNone && !Incoming.IsNone;
    }

    /// <summary>
    /// Every authored reaction pair (D41). Directional on purpose — soaking a shocked target and
    /// shocking a soaked one are different moments, and a table that assumed symmetry could not
    /// express that.
    /// </summary>
    /// <remarks>
    /// <b>This ships empty.</b> The machinery is built and tested now; which elements pair, and
    /// which of them owns D19's chain-stun, waits on the roster (O11) — because Water and Electric
    /// are exactly the contested names. An empty table is inert, not broken: every reaction lookup
    /// simply finds nothing, which is the correct behaviour for a game with one element.
    /// </remarks>
    public sealed class ReactionTable
    {
        private readonly List<ReactionEntry> _entries = new List<ReactionEntry>();

        public static ReactionTable Empty { get; } = new ReactionTable(null);

        public ReactionTable(IReadOnlyList<ReactionEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsValid || Contains(entries[i].Carried, entries[i].Incoming))
                {
                    continue;
                }

                _entries.Add(entries[i]);
            }
        }

        public int Count => _entries.Count;

        public bool IsEmpty => _entries.Count == 0;

        public IReadOnlyList<ReactionEntry> Entries => _entries;

        public bool TryFind(ElementId carried, ElementId incoming, out ReactionSpec spec)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Carried == carried && _entries[i].Incoming == incoming)
                {
                    spec = _entries[i].Spec;
                    return true;
                }
            }

            spec = default;
            return false;
        }

        private bool Contains(ElementId carried, ElementId incoming) =>
            TryFind(carried, incoming, out _);
    }
}
