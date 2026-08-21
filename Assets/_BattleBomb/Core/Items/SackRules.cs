using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// D43's limits on what one player can carry. Worn gear is outside the count — the sack is
    /// what you are hauling, not what you are wearing — and a consumable stack costs one slot
    /// however deep it is.
    /// </summary>
    public readonly struct SackRules
    {
        public readonly int Capacity;
        public readonly int StackLimit;

        public SackRules(int capacity, int stackLimit)
        {
            Capacity = Mathf.Max(1, capacity);
            StackLimit = Mathf.Max(1, stackLimit);
        }

        public static SackRules Default => new SackRules(200, 5);
    }

    /// <summary>
    /// What taking an item did. A refused pickup (<see cref="Taken"/> false) leaves the drop in
    /// the world — D43 has no overflow valve, and the red X is presentation's job.
    /// </summary>
    public readonly struct AddResult
    {
        public readonly bool Taken;
        public readonly bool Equipped;

        /// <summary>Coins the auto-sell setting earned making room, zero when it did not fire.</summary>
        public readonly int CoinsEarned;

        public AddResult(bool taken, bool equipped, int coinsEarned)
        {
            Taken = taken;
            Equipped = equipped;
            CoinsEarned = Mathf.Max(0, coinsEarned);
        }

        /// <summary>The sack was full and nothing could be sold to make room.</summary>
        public static AddResult Refused => default;
    }
}
