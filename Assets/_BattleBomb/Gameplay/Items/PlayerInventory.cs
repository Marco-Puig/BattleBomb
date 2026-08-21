using BattleBomb.Core.Items;
using BattleBomb.Core.Progression;
using UnityEngine;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// One player's bag, loadout, and ladder place — a thin owner around Core's
    /// <see cref="Core.Items.Inventory"/> and <see cref="XpLedger"/>. Gameplay owns the state;
    /// the rules all live in Core under tests (the 2019 antidote, by construction).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Header("XP curve (D24)")]
        [Tooltip("XP to leave level 1; the polynomial grows it from there.")]
        [SerializeField] private float _xpBase = 40f;

        [Tooltip("Polynomial exponent — a long haul to 99, never a frozen bar.")]
        [SerializeField] private float _xpExponent = 1.5f;

        [Tooltip("Each prestige cycle multiplies every level's price by this.")]
        [SerializeField] private float _prestigeCostMultiplier = 1.25f;

        [SerializeField] private int _maxLevel = 99;
        [SerializeField] private int _pointsPerLevel = 1;

        private Inventory _inventory;
        private XpLedger _ledger;
        private Wallet _wallet;

        public Inventory Inventory => _inventory ?? (_inventory = new Inventory());

        public XpLedger Ledger => _ledger;

        public int Level => _ledger.Level;

        /// <summary>This player's money (D43) — theirs alone, like the loot that made it.</summary>
        public Wallet Wallet => _wallet;

        public XpCurve Curve => new XpCurve(
            _xpBase, _xpExponent, _prestigeCostMultiplier, _maxLevel, _pointsPerLevel);

        private void OnEnable()
        {
            if (_inventory == null)
            {
                _inventory = new Inventory();
            }

            if (_ledger.Level < 1)
            {
                _ledger = XpLedger.Fresh;
            }
        }

        /// <summary>
        /// A grabbed drop lands here (D30). The sack may refuse it outright (D43), and auto-sell
        /// may have paid for the room — either way the caller learns which from the result.
        /// </summary>
        public AddResult Take(in ItemInstance item)
        {
            AddResult result = Inventory.Add(item, Level);
            if (result.CoinsEarned > 0)
            {
                _wallet = _wallet.Earned(result.CoinsEarned);
            }

            return result;
        }

        /// <summary>Sells one bagged stack outright, banking what it fetched (D43).</summary>
        public int Sell(int bagIndex)
        {
            int coins = Inventory.Sell(bagIndex);
            if (coins > 0)
            {
                _wallet = _wallet.Earned(coins);
            }

            return coins;
        }

        /// <summary>A kill's reward (task 48): every living player earns the full amount.</summary>
        public void Earn(float xp) => _ledger = _ledger.Earn(xp, Curve);

        /// <summary>
        /// D24's reset, made real: the ledger resets and banks its permanent point, then D36
        /// re-locks the stash — every worn piece above level one returns to the bag, no
        /// grandfather clause. The caller refreshes the actor's stats after.
        /// </summary>
        public bool TryPrestige()
        {
            if (!_ledger.CanPrestige(Curve))
            {
                return false;
            }

            _ledger = _ledger.Prestige(Curve);
            Inventory.ReturnOverLevelGear(_ledger.Level);
            return true;
        }

        internal void SetLedger(in XpLedger ledger) => _ledger = ledger;
    }
}
