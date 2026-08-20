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
        private Inventory _inventory;
        private XpLedger _ledger;

        public Inventory Inventory => _inventory ?? (_inventory = new Inventory());

        public XpLedger Ledger => _ledger;

        public int Level => _ledger.Level;

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

        /// <summary>A grabbed drop lands here (D30). Returns whether it auto-equipped.</summary>
        public bool Take(in ItemInstance item) => Inventory.Add(item, Level);

        internal void SetLedger(in XpLedger ledger) => _ledger = ledger;
    }
}
