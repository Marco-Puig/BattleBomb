using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// What one "combine all" pass spent, and what it won — the whole of what
    /// <see cref="Inventory.CombineAll"/> reports. Both halves matter to the chest screen: the
    /// pairs are what the pile cost, and the promotions are the only part of D44 a player calls
    /// a win.
    /// </summary>
    public readonly struct CombineRun
    {
        /// <summary>How many pairs the run consumed.</summary>
        public readonly int Combines;

        /// <summary>How many of those rerolls came back a rank up (D44's 2% shot). Never more
        /// than <see cref="Combines"/> — a pair that refused to combine cannot have promoted.</summary>
        public readonly int Promotions;

        public CombineRun(int combines, int promotions)
        {
            Combines = Mathf.Max(0, combines);
            Promotions = Mathf.Max(0, promotions);
        }

        /// <summary>Nothing combined — the press found no pair and there is nothing to announce.</summary>
        public bool IsEmpty => Combines == 0;
    }
}
