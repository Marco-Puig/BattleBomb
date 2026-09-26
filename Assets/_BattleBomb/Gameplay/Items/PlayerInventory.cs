using System;
using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// One player's bag, loadout, wallet, and ladder place — a thin owner around Core's
    /// <see cref="Core.Items.Inventory"/>, <see cref="XpLedger"/>, and <see cref="Wallet"/>.
    /// Gameplay owns the state; the rules all live in Core under tests (the 2019 antidote, by
    /// construction).
    ///
    /// Every mutation the chest screen can ask for is a <c>Request</c> method here, and every one
    /// that changes anything raises <see cref="Changed"/>. The UI sends requests and redraws from
    /// the event — it never reaches in (M6 planning decision 2, and the M4 close-out's complaint
    /// about manual RefreshStats choreography).
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

        [Tooltip("Driver supplying the loot catalog a combine rerolls from. Empty finds it.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("The couch's sack and wallet (D51). Empty finds the one in the scene.")]
        [SerializeField] private SharedStash _stash;

        private Inventory _inventory;
        private XpLedger _ledger;

        /// <summary>Raised after anything in the bag, loadout, wallet, or ladder moved.</summary>
        public event Action Changed;

        public Inventory Inventory => _inventory ?? (_inventory = new Inventory(Stash.Sack));

        public XpLedger Ledger => _ledger;

        public int Level => _ledger.Level;

        /// <summary>The couch's money (D51) — shared, like the sack that made it.</summary>
        public Wallet Wallet => Stash.Wallet;

        /// <summary>The stash this loadout sits over. Found late because Player objects enable
        /// before the Simulation object does on some load orders.</summary>
        public SharedStash Stash
        {
            get
            {
                if (_stash == null)
                {
                    _stash = FindAnyObjectByType<SharedStash>();
                }

                if (_stash == null)
                {
                    // No stash in the scene: a private one, so a bare test scene still works.
                    var go = new GameObject("Shared Stash");
                    _stash = go.AddComponent<SharedStash>();
                }

                return _stash;
            }
        }

        public XpCurve Curve => new XpCurve(
            _xpBase, _xpExponent, _prestigeCostMultiplier, _maxLevel, _pointsPerLevel);

        private void OnEnable()
        {
            if (_inventory == null)
            {
                _inventory = new Inventory(Stash.Sack);
            }

            if (_ledger.Level < 1)
            {
                _ledger = XpLedger.Fresh;
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            Stash.Changed += OnStashChanged;
        }

        private void OnDisable()
        {
            if (_stash != null)
            {
                _stash.Changed -= OnStashChanged;
            }
        }

        /// <summary>The partner moved money or the sack was reloaded — that is a change to
        /// this player's view too (D51), so the sheet and the screen both hear it.</summary>
        private void OnStashChanged() => Changed?.Invoke();

        /// <summary>
        /// A grabbed drop lands here (D30). The sack may refuse it outright (D43), and auto-sell
        /// may have paid for the room — either way the caller learns which from the result.
        /// </summary>
        public AddResult Take(in ItemInstance item)
        {
            AddResult result = Inventory.Add(item, Level);
            if (result.CoinsEarned > 0)
            {
                Stash.Earn(result.CoinsEarned);
            }

            if (result.Taken)
            {
                Stash.NotifyChanged();
            }

            return result;
        }

        /// <summary>A kill's reward (task 48): every living player earns the full amount.</summary>
        public void Earn(float xp)
        {
            _ledger = _ledger.Earn(xp, Curve);
            Changed?.Invoke();
        }

        // ── Requests: everything the chest screen can ask for ────────────────────────

        public bool RequestEquip(int bagIndex, int equipmentIndex = 0)
        {
            if (!Inventory.TryEquip(bagIndex, Level, equipmentIndex))
            {
                return false;
            }

            Stash.NotifyChanged();
            return true;
        }

        public bool RequestUnequip(ItemSlot slot, int equipmentIndex = 0)
        {
            if (!Inventory.Unequip(slot, equipmentIndex))
            {
                return false;
            }

            Stash.NotifyChanged();
            return true;
        }

        /// <summary>Sells one bagged stack outright, banking what it fetched (D43).</summary>
        public int RequestSell(int bagIndex)
        {
            int coins = Inventory.Sell(bagIndex);
            if (coins > 0)
            {
                Stash.Earn(coins);
                Stash.NotifyChanged();
            }

            return coins;
        }

        /// <summary>"Clear the junk": sells every unlocked, non-consumable stack below the given
        /// rank in one pass, banking the total.</summary>
        public JunkSale RequestSellJunk(QualityRank below)
        {
            JunkSale sale = Inventory.SellJunk(below);
            if (sale.Coins > 0)
            {
                Stash.Earn(sale.Coins);
                Stash.NotifyChanged();
            }

            return sale;
        }

        /// <summary>What <see cref="RequestSellJunk"/> would sell, so the confirmation screen
        /// does not have to reach through both the loadout and Core to ask.</summary>
        public JunkSale PreviewJunk(QualityRank below) => Inventory.PreviewJunk(below);

        public bool RequestLock(int bagIndex, bool locked)
        {
            if (!Inventory.SetLock(bagIndex, locked))
            {
                return false;
            }

            Stash.NotifyChanged();
            return true;
        }

        public bool RequestLockWorn(ItemSlot slot, int equipmentIndex, bool locked)
        {
            if (!Inventory.SetWornLock(slot, equipmentIndex, locked))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// D44's deepening, money first: the price is checked and taken before the point is
        /// spent, and a refused upgrade never takes the coins.
        /// </summary>
        public bool RequestUpgrade(int bagIndex, in UpgradeTarget target)
        {
            if (bagIndex < 0 || bagIndex >= Inventory.Items.Count)
            {
                return false;
            }

            int price = Inventory.Prices.UpgradeCost(Inventory.Items[bagIndex].Item);
            if (price <= 0 || !Stash.CanAfford(price))
            {
                return false;
            }

            if (!Inventory.TryUpgradeBagged(bagIndex, target))
            {
                return false;
            }

            Stash.TrySpend(price);
            Stash.NotifyChanged();
            return true;
        }

        public bool RequestUpgradeWorn(ItemSlot slot, int equipmentIndex, in UpgradeTarget target)
        {
            ItemInstance worn = Inventory.Loadout.Worn(slot, equipmentIndex);
            if (worn.IsEmpty)
            {
                return false;
            }

            int price = Inventory.Prices.UpgradeCost(worn);
            if (price <= 0 || !Stash.CanAfford(price))
            {
                return false;
            }

            if (!Inventory.TryUpgradeWorn(slot, equipmentIndex, target))
            {
                return false;
            }

            Stash.TrySpend(price);
            Stash.NotifyChanged();
            return true;
        }

        /// <summary>D44's gamble. The reroll comes from the driver's catalog and its own stream.</summary>
        public bool RequestCombine(int firstIndex, int secondIndex, out CombineResult result)
        {
            result = CombineResult.Refused;
            if (_driver == null)
            {
                return false;
            }

            _driver.RunCombine(Inventory, firstIndex, secondIndex, out result);
            if (!result.Combined)
            {
                return false;
            }

            Stash.NotifyChanged();
            return true;
        }

        /// <summary>The same gamble, taken to the end of the pile in one press (D44).</summary>
        public bool RequestCombineAll(int anchorIndex, out CombineRun run)
        {
            run = default;
            if (_driver == null)
            {
                return false;
            }

            _driver.RunCombineAll(Inventory, anchorIndex, out run);
            if (run.IsEmpty)
            {
                return false;
            }

            Stash.NotifyChanged();
            return true;
        }

        public bool RequestQuickConsumable(int definitionId)
        {
            if (!Inventory.AssignQuickConsumable(definitionId))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public bool RequestQuickEquipment(int equipmentIndex)
        {
            if (!Inventory.AssignQuickEquipment(equipmentIndex))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>D32's allocation, from the Hero tab.</summary>
        public bool RequestAllocate(StatId stat)
        {
            if (_ledger.UnspentPoints <= 0)
            {
                return false;
            }

            _ledger = _ledger.Spend(stat);
            Changed?.Invoke();
            return true;
        }

        public void SetAutoEquip(bool value)
        {
            if (Inventory.AutoEquip == value)
            {
                return;
            }

            Inventory.AutoEquip = value;
            Stash.NotifyChanged();
        }

        public void SetAutoSell(bool value)
        {
            if (Inventory.AutoSell == value)
            {
                return;
            }

            Inventory.AutoSell = value;
            Stash.NotifyChanged();
        }

        /// <summary>
        /// D24's reset, made real: the ledger resets and banks its permanent point, then D36
        /// re-locks the stash — every worn piece above level one returns to the bag, no
        /// grandfather clause.
        /// </summary>
        public bool TryPrestige()
        {
            if (!_ledger.CanPrestige(Curve))
            {
                return false;
            }

            _ledger = _ledger.Prestige(Curve);
            Inventory.ReturnOverLevelGear(_ledger.Level);
            Stash.NotifyChanged();
            return true;
        }

        internal void SetLedger(in XpLedger ledger)
        {
            _ledger = ledger;
            Changed?.Invoke();
        }

        /// <summary>
        /// The guest's copy of this player (D61, HANDOFF-M8 Task 99): what the host holds for them, laid over
        /// this machine's copy in one go — worn gear, quick slot and ledger always; the sack, wallet and auto
        /// flags too when <paramref name="full"/> (the guest's own player). The sack takes the host's revision,
        /// so a request sent from it names the sack the host holds. One arrival, one <see cref="Changed"/>.
        /// </summary>
        internal void ApplyMirror(SaveGame state, int revision, bool full, IReadOnlyList<ItemSpec> catalog)
        {
            CharacterSave character = state != null && state.Characters.Length > 0 ? state.Characters[0] : null;
            if (character == null)
            {
                return;
            }

            _ledger = SaveMapper.RestoreCharacter(character, Inventory, catalog);
            if (full)
            {
                SaveMapper.RestoreSack(state, Stash.Sack, catalog, revision);

                // Raises the stash's Changed, which this bag passes on as its own.
                Stash.Restore(Stash.Sack, SaveMapper.RestoreWallet(state));
                return;
            }

            Changed?.Invoke();
        }

        /// <summary>Debug entry point: money in, without a sale behind it. Dies with the debug
        /// grant when real content arrives.</summary>
        public void GrantCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Stash.Earn(amount);
            Stash.NotifyChanged();
        }

        /// <summary>Buying from the shopkeeper (D43): the price leaves the wallet, the item
        /// lands in the sack — and a sack that refuses it refunds nothing, so the check comes
        /// first. Internal since the rack moved into the simulation (HANDOFF-M8 planning decision
        /// 12): only the driver, which holds the rack and prices it, may name an item and a price.</summary>
        internal bool RequestBuy(in ItemInstance item, int price)
        {
            if (item.IsEmpty || price < 0 || !Stash.CanAfford(price) || Inventory.IsFull)
            {
                return false;
            }

            AddResult result = Inventory.Add(item, Level);
            if (!result.Taken)
            {
                return false;
            }

            Stash.TrySpend(price);
            if (result.CoinsEarned > 0)
            {
                Stash.Earn(result.CoinsEarned);
            }

            Stash.NotifyChanged();
            return true;
        }
    }
}
