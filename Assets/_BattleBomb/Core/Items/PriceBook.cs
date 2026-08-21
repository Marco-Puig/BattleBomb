using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// Every price in the game, derived from one small rulebook (D43/D44). Selling is the only
    /// faucet, so the sell curve inherits the ladder's endless scaling: each rank multiplies,
    /// required level multiplies on top, and a Godly player selling Godly castoffs earns on the
    /// Godly scale. The shop charges a premium over what it pays, and the upgrade sink prices
    /// maxing an item at roughly its own sell value times <see cref="UpgradeShare"/>.
    /// </summary>
    public readonly struct PriceBook
    {
        /// <summary>What a rank-zero, level-one item sells for — the smallest coin in the game.</summary>
        public readonly float SellBase;

        /// <summary>Each rank up the ladder multiplies sell value by this.</summary>
        public readonly float SellRankFactor;

        /// <summary>Required level's multiplier per level, on top of the rank curve.</summary>
        public readonly float SellLevelFactor;

        /// <summary>The shop sells at this multiple of what it pays (D43).</summary>
        public readonly float ShopPremium;

        /// <summary>Fully deepening an item costs about this share of its own sell price (D44).</summary>
        public readonly float UpgradeShare;

        public PriceBook(
            float sellBase, float sellRankFactor, float sellLevelFactor,
            float shopPremium, float upgradeShare)
        {
            SellBase = Mathf.Max(0f, sellBase);
            SellRankFactor = Mathf.Max(1f, sellRankFactor);
            SellLevelFactor = Mathf.Max(0f, sellLevelFactor);
            ShopPremium = Mathf.Max(1f, shopPremium);
            UpgradeShare = Mathf.Max(0f, upgradeShare);
        }

        /// <summary>The paper numbers (HANDOFF-M6) — authored data overrides them per scene.</summary>
        public static PriceBook Default => new PriceBook(
            sellBase: 3f,
            sellRankFactor: 2f,
            sellLevelFactor: 0.04f,
            shopPremium: 3f,
            upgradeShare: 2f);

        /// <summary>What the shopkeeper (or the auto-sell setting) pays. Never below one coin.</summary>
        public int SellPrice(QualityRank rank, int requiredLevel) =>
            Mathf.Max(1, Mathf.RoundToInt(RawSell(rank, requiredLevel)));

        public int SellPrice(in ItemInstance item) => SellPrice(item.Quality, item.RequiredLevel);

        /// <summary>What the shopkeeper charges for the same item.</summary>
        public int BuyPrice(QualityRank rank, int requiredLevel) =>
            Mathf.Max(1, Mathf.RoundToInt(RawSell(rank, requiredLevel) * ShopPremium));

        public int BuyPrice(in ItemInstance item) => BuyPrice(item.Quality, item.RequiredLevel);

        /// <summary>
        /// The next capacity point's price (D44): steps double, and the whole run sums to about
        /// <see cref="UpgradeShare"/> × the item's sell price regardless of capacity — so a
        /// two-point Shiny and a four-point Godly both cost "about two castoffs" to max, in
        /// their own coin. Zero when nothing is left to buy.
        /// </summary>
        public int UpgradeCost(QualityRank rank, int requiredLevel, int capacity, int spent)
        {
            if (capacity <= 0 || spent < 0 || spent >= capacity)
            {
                return 0;
            }

            float steps = Mathf.Pow(2f, capacity) - 1f;
            float baseCost = UpgradeShare * RawSell(rank, requiredLevel) / steps;
            return Mathf.Max(1, Mathf.RoundToInt(baseCost * Mathf.Pow(2f, spent)));
        }

        public int UpgradeCost(in ItemInstance item) =>
            UpgradeCost(item.Quality, item.RequiredLevel, item.UpgradeCapacity, item.UpgradesSpent);

        private float RawSell(QualityRank rank, int requiredLevel) =>
            SellBase
            * Mathf.Pow(SellRankFactor, (int)rank)
            * (1f + SellLevelFactor * Mathf.Max(1, requiredLevel));
    }
}
