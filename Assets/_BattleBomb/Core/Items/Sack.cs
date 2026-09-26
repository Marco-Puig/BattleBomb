using System.Collections.Generic;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// The bag itself (D43's 200 slots), separated from any one player's loadout so the couch
    /// can share it (D51). Holds the stacks, the carry rules, the price book auto-sell pays
    /// from, and the two settings — all of which are per save, not per player, which is why
    /// they live here and not on <see cref="Inventory"/>. The rules for moving items in and
    /// out stay on <see cref="Inventory"/>, whose instances each wrap one loadout over this.
    /// </summary>
    public sealed class Sack
    {
        /// <summary>The raw list, for <see cref="Inventory"/> to store into. Nothing else
        /// in Core should touch this directly — going around <see cref="Inventory"/> skips
        /// every rule it enforces: capacity, stack limits, level locks, which piece auto-sell
        /// picks.</summary>
        internal List<ItemStack> Entries { get; } = new List<ItemStack>();

        /// <summary>The carry limits (D43). Authored per game.</summary>
        public SackRules Rules { get; set; } = SackRules.Default;

        /// <summary>The rulebook every coin flows through — auto-sell needs it to pay out.</summary>
        public PriceBook Prices { get; set; } = PriceBook.Default;

        /// <summary>D30: off by default — grabbed loot lands in the bag unless the player opts in.</summary>
        public bool AutoEquip { get; set; }

        /// <summary>D43: off by default — at the cap, a pickup sells the worst unlocked piece.</summary>
        public bool AutoSell { get; set; }

        public IReadOnlyList<ItemStack> Items => Entries;

        /// <summary>Slots in use: one per stack, however deep the stack is (D43).</summary>
        public int SlotsUsed => Entries.Count;

        public bool IsFull => Entries.Count >= Rules.Capacity;

        /// <summary>
        /// Moves every time what the sack holds changes — a stack added, removed, split, locked or
        /// deepened. A menu action that names a place in the sack carries the number its player was
        /// looking at, so the host can refuse one aimed at a sack that has changed since (HANDOFF-M8
        /// planning decision 11). The auto flags do not move it: they change no place.
        /// </summary>
        public int Revision { get; private set; }

        internal void Touch() => Revision++;

        /// <summary>The guest's copy of its sack takes the host's number (Task 99), so what it asks for
        /// names the sack the host holds.</summary>
        internal void AdoptRevision(int revision) => Revision = revision;
    }
}
