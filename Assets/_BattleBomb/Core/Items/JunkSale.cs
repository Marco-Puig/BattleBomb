using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// What a "clear the junk" bulk sell found, or paid — <see cref="Inventory.PreviewJunk"/> and
    /// <see cref="Inventory.SellJunk"/> report the same totals for the same threshold, so the
    /// confirmation screen and the sale itself never disagree.
    /// </summary>
    public readonly struct JunkSale
    {
        /// <summary>How many bag stacks qualified.</summary>
        public readonly int Stacks;

        /// <summary>The stacks' combined item count. Gear sits one per stack, so this matches
        /// <see cref="Stacks"/> for every sale the sweep can actually make — the two are kept
        /// apart because the qualifying rule, not the arithmetic, is what excludes deep stacks.</summary>
        public readonly int Pieces;

        /// <summary>What the sale pays, or would pay, in total.</summary>
        public readonly int Coins;

        public JunkSale(int stacks, int pieces, int coins)
        {
            Stacks = Mathf.Max(0, stacks);
            Pieces = Mathf.Max(0, pieces);
            Coins = Mathf.Max(0, coins);
        }

        /// <summary>Nothing qualified — the confirmation screen has nothing to show.</summary>
        public bool IsEmpty => Pieces == 0;
    }
}
