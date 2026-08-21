using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Players;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.World;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>What one row of the item dropdown does.</summary>
    internal enum ItemAction
    {
        Equip = 0,
        QuickUse = 1,
        Upgrade = 2,
        Combine = 3,
        Sell = 4,
        Lock = 5,
    }

    /// <summary>
    /// One player's chest screen (D42): the Item Sack's thumbnail grid with the selected item's
    /// detail beside it, and the Hero tab's allocation and worn loadout. Reads simulation state
    /// and sends requests — it never mutates anything itself (M6 planning decision 2).
    ///
    /// Navigated entirely by <see cref="PlayerCommand"/>: the stick moves, Light confirms, Heavy
    /// goes back and finally closes. That keeps the menu inside rule 3's one-input-path promise
    /// and makes two players on two screen halves need no extra machinery.
    /// </summary>
    internal sealed partial class ChestScreen : MonoBehaviour
    {
        private const int GridColumnsWide = 8;
        private const int GridColumnsSplit = 4;
        private const int GridRows = 5;

        /// <summary>Pieces the shopkeeper offers per visit (D43's "3–4 rolled gear pieces").</summary>
        private const int ShopStockCount = 4;

        /// <summary>Frames a held stick waits before it starts repeating, and between repeats.</summary>
        private const float RepeatDelay = 0.32f;
        private const float RepeatRate = 0.09f;

        private static readonly ItemSlot[] FilterSlots =
        {
            ItemSlot.Helmet, ItemSlot.Weapon, ItemSlot.Pet, ItemSlot.Equipment, ItemSlot.Consumable,
        };

        private static readonly string[] FilterNames =
        {
            "All", "Weapons", "Armor", "Pets", "Equipment", "Consumables",
        };

        private PlayerInventory _bag;

        /// <summary>Read-only: the aggregated sheet the Hero tab shows (§3 — UI observes).</summary>
        private Gameplay.Characters.CharacterActor _sheetSource;
        private int _playerId;
        private InteractionKind _kind;
        private bool _split;

        private readonly ChestNavigation _nav = new ChestNavigation();

        /// <summary>Ignore input until every menu button has been let go at least once.</summary>
        private bool _swallowUntilRelease = true;
        private string _flash = string.Empty;
        private float _flashUntil;
        private float _repeatAt;
        private Vector2 _lastMove;

        private readonly List<int> _visible = new List<int>();
        private readonly List<UpgradeTarget> _upgradeTargets = new List<UpgradeTarget>();

        /// <summary>The dropdown's rows for the selected item, rebuilt whenever it changes.</summary>
        private readonly List<ItemAction> _menu = new List<ItemAction>();

        /// <summary>The shopkeeper's rack for this visit (D43) — empty at a chest.</summary>
        private readonly List<ItemInstance> _stock = new List<ItemInstance>();

        private Text _header;
        private Text _tabStrip;
        private Text _filterStrip;
        private Text _detail;
        private Text _actionStrip;
        private Text _hint;
        private RectTransform _gridRoot;
        private readonly List<Image> _cells = new List<Image>();
        private readonly List<Text> _cellLabels = new List<Text>();
        private RectTransform _heroRoot;
        private Text _heroLeft;
        private Text _heroRight;

        private int Columns => _split ? GridColumnsSplit : GridColumnsWide;

        private ChestLayout Layout() => new ChestLayout(
            _visible.Count, Columns, FilterNames.Length, _menu.Count, _upgradeTargets.Count, _stock.Count);

        internal void Bind(
            PlayerInventory bag, Gameplay.Characters.CharacterActor sheetSource,
            int playerId, InteractionKind kind, bool split)
        {
            _bag = bag;
            _sheetSource = sheetSource;
            _playerId = playerId;
            _kind = kind;
            _split = split;
            if (kind == InteractionKind.Shopkeeper)
            {
                // Rolled once per visit, so coming back later is worth doing (D43).
                Host?.RollStock(_stock, ShopStockCount);
            }

            Build();
            Refresh();
        }

        /// <summary>One navigation step, from this player's live command (D42).</summary>
        internal void Tick(in PlayerCommand command, float deltaTime)
        {
            if (_bag == null)
            {
                return;
            }

            // The press that opened this screen must not also act inside it. Without this, the
            // opening Light arrives here on the same step and immediately dives into the action
            // row — which is what made Heavy look like it would not close the screen during
            // Michael's M6 pass: it was faithfully backing out of a level he never chose.
            if (_swallowUntilRelease)
            {
                if (command.IsHeld(CommandButtons.Light)
                    || command.IsHeld(CommandButtons.Heavy)
                    || command.IsHeld(CommandButtons.Pause))
                {
                    return;
                }

                _swallowUntilRelease = false;
            }

            StepCursor(command, deltaTime);

            if (command.WasPressed(CommandButtons.Pause))
            {
                // Escape / Start always leaves, from anywhere in the screen — no unwinding
                // through focus levels first. Getting out must never be a puzzle.
                Host?.RequestClose(_playerId);
                return;
            }

            if (command.WasPressed(CommandButtons.Light))
            {
                Confirm();
            }
            else if (command.WasPressed(CommandButtons.Heavy))
            {
                Cancel();
            }

            if (Time.unscaledTime > _flashUntil && _flash.Length > 0)
            {
                _flash = string.Empty;
                Refresh();
            }
        }

        // ── Navigation ───────────────────────────────────────────────────────────────

        private void StepCursor(in PlayerCommand command, float deltaTime)
        {
            Vector2 move = command.Move;
            bool fresh = Mathf.Abs(move.x) > 0.5f && Mathf.Abs(_lastMove.x) <= 0.5f
                || Mathf.Abs(move.y) > 0.5f && Mathf.Abs(_lastMove.y) <= 0.5f;
            _lastMove = move;

            if (Mathf.Abs(move.x) <= 0.5f && Mathf.Abs(move.y) <= 0.5f)
            {
                _repeatAt = 0f;
                return;
            }

            if (fresh)
            {
                _repeatAt = Time.unscaledTime + RepeatDelay;
            }
            else if (Time.unscaledTime < _repeatAt)
            {
                return;
            }
            else
            {
                _repeatAt = Time.unscaledTime + RepeatRate;
            }

            int dx = Mathf.Abs(move.x) > 0.5f ? (move.x > 0f ? 1 : -1) : 0;
            int dy = Mathf.Abs(move.y) > 0.5f ? (move.y > 0f ? 1 : -1) : 0;
            Navigate(dx, dy);
            Refresh();
        }

        private void Navigate(int dx, int dy)
        {
            CollectVisible();
            _nav.Move(dx, dy, Layout());
        }

        private void Cancel()
        {
            switch (_nav.Cancel())
            {
                case ChestOutcome.CombineCancelled:
                    Flash("Combine cancelled.");
                    break;

                case ChestOutcome.Close:
                    // Nothing left to back out of: Heavy closes the chest.
                    Host?.RequestClose(_playerId);
                    break;

                default:
                    Refresh();
                    break;
            }
        }

        internal ChestScreenHost Host { get; set; }

        internal PlayerInventory Bag => _bag;

        /// <summary>The X in the corner, and any other pointer route out.</summary>
        internal void CloseFromPointer() => Host?.RequestClose(_playerId);

        /// <summary>The bag moved — possibly under the partner's hand (D51). Repaint from the
        /// new truth, and drop a combine whose first pick no longer exists.</summary>
        internal void OnBagChanged()
        {
            _nav.ClampPendingCombine(_bag.Inventory.Items.Count);
            Refresh();
        }

        // ── Actions ──────────────────────────────────────────────────────────────────

        private void Confirm()
        {
            CollectVisible();
            switch (_nav.Confirm(Layout()))
            {
                case ChestOutcome.AllocateStat:
                    StatId[] stats = { StatId.Strength, StatId.Hp, StatId.Mana, StatId.Speed };
                    Flash(_bag.RequestAllocate(stats[_nav.Cursor]) ? "Point spent." : "No points to spend.");
                    break;

                case ChestOutcome.RunMenuAction:
                    RunMenuAction();
                    break;

                case ChestOutcome.RunUpgrade:
                    RunUpgrade();
                    break;

                case ChestOutcome.RunBuy:
                    RunBuy();
                    break;
            }

            Refresh();
        }

        private void RunMenuAction()
        {
            CollectVisible();
            if (_visible.Count == 0 || _nav.Action >= _menu.Count)
            {
                return;
            }

            int bagIndex = _visible[_nav.Cursor];
            ItemInstance item = _bag.Inventory.Items[bagIndex].Item;

            switch (_menu[_nav.Action])
            {
                case ItemAction.Equip:
                    Flash(_bag.RequestEquip(bagIndex) ? "Equipped." : "Cannot equip — check the level.");
                    break;

                case ItemAction.QuickUse:
                    Flash(_bag.RequestQuickConsumable(item.DefinitionId)
                        ? "Quick-use set." : "Cannot set.");
                    break;

                case ItemAction.Upgrade:
                    // Deepening moves to the right panel, where the stats already are — so the
                    // choice is made looking at the numbers it changes (Michael, M6 pass).
                    ItemUpgrade.Targets(item, _upgradeTargets);
                    _nav.OpenUpgrade();
                    return;

                case ItemAction.Combine:
                    RunCombine(bagIndex);
                    break;

                case ItemAction.Sell:
                    int coins = _bag.RequestSell(bagIndex);
                    Flash(coins > 0 ? $"Sold for {coins}." : "Locked — release it first.");
                    break;

                case ItemAction.Lock:
                    _bag.RequestLock(bagIndex, !item.Locked);
                    Flash(item.Locked ? "Released." : "Locked.");
                    break;
            }

            CollectVisible();
            _nav.FinishMenuAction(_visible.Count);
        }

        /// <summary>Buying from the rack (D43): the money leaves, the piece joins the sack, and
        /// the slot on the rack empties so the same item cannot be bought twice.</summary>
        private void RunBuy()
        {
            if (_nav.StockCursor < 0 || _nav.StockCursor >= _stock.Count)
            {
                return;
            }

            ItemInstance item = _stock[_nav.StockCursor];
            int price = _bag.Inventory.Prices.BuyPrice(item);
            if (!Host.Buy(_bag, item, price))
            {
                Flash(_bag.Inventory.IsFull ? "The sack is full." : $"Not enough coin ({price}).");
                return;
            }

            _stock.RemoveAt(_nav.StockCursor);
            _nav.FinishBuy(_stock.Count);

            Flash($"Bought for {price}.");
        }

        private void RunCombine(int bagIndex)
        {
            if (_nav.PendingCombine < 0)
            {
                _nav.BeginCombine(bagIndex);
                Flash("Pick the duplicate to combine with.");
                return;
            }

            if (_nav.PendingCombine == bagIndex)
            {
                _nav.CancelCombine();
                Flash("Combine cancelled.");
                return;
            }

            if (_bag.RequestCombine(_nav.PendingCombine, bagIndex, out CombineResult result))
            {
                Flash(result.Promoted
                    ? $"UPGRADED! {result.Item.DisplayName}"
                    : $"Rerolled: {result.Item.DisplayName}");
            }
            else
            {
                Flash("These two cannot combine.");
            }

            _nav.CancelCombine();
        }

        /// <summary>
        /// Spends one point into the stat under the cursor in the right panel. Focus stays here
        /// afterwards: a player deepening an item usually wants to keep going, and the panel
        /// shows the cost doubling as they do.
        /// </summary>
        private void RunUpgrade()
        {
            CollectVisible();
            if (_visible.Count == 0 || _nav.UpgradeCursor >= _upgradeTargets.Count)
            {
                return;
            }

            int bagIndex = _visible[_nav.Cursor];
            int price = _bag.Inventory.Prices.UpgradeCost(_bag.Inventory.Items[bagIndex].Item);

            if (_bag.RequestUpgrade(bagIndex, _upgradeTargets[_nav.UpgradeCursor]))
            {
                Flash($"Deepened for {price}.");
                ItemUpgrade.Targets(_bag.Inventory.Items[bagIndex].Item, _upgradeTargets);
                _nav.FinishUpgrade(ItemUpgrade.CanUpgrade(_bag.Inventory.Items[bagIndex].Item));
                return;
            }

            Flash(_bag.Wallet.CanAfford(price)
                ? "The capacity is spent."
                : $"Not enough coin ({price}).");
        }

        private void Flash(string message)
        {
            _flash = message;
            _flashUntil = Time.unscaledTime + 2.5f;
            Refresh();
        }

        // ── Model ────────────────────────────────────────────────────────────────────

        private void CollectVisible()
        {
            _visible.Clear();
            IReadOnlyList<ItemStack> items = _bag.Inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (MatchesFilter(items[i].Item))
                {
                    _visible.Add(i);
                }
            }

            _nav.ClampCursor(_visible.Count);
        }

        private bool MatchesFilter(in ItemInstance item)
        {
            switch (_nav.Filter)
            {
                case 0: return true;
                case 1: return item.Slot == ItemSlot.Weapon;
                case 2: return item.Slot == ItemSlot.Helmet
                    || item.Slot == ItemSlot.Chest
                    || item.Slot == ItemSlot.Boots;
                case 3: return item.Slot == ItemSlot.Pet;
                case 4: return item.Slot == ItemSlot.Equipment;
                default: return item.Slot == ItemSlot.Consumable;
            }
        }

        /// <summary>
        /// The dropdown's rows for this item. Only what the item can actually do appears — a
        /// potion has no Combine, a maxed item has no Upgrade — so a row on screen is always a
        /// row that works.
        /// </summary>
        private void BuildMenu(int bagIndex)
        {
            _menu.Clear();
            if (bagIndex < 0)
            {
                return;
            }

            ItemInstance item = _bag.Inventory.Items[bagIndex].Item;
            _menu.Add(item.IsConsumable ? ItemAction.QuickUse : ItemAction.Equip);

            if (ItemUpgrade.CanUpgrade(item))
            {
                _menu.Add(ItemAction.Upgrade);
            }

            if (!item.IsConsumable)
            {
                _menu.Add(ItemAction.Combine);
            }

            _menu.Add(ItemAction.Sell);
            _menu.Add(ItemAction.Lock);

            _nav.ClampAction(_menu.Count);
        }

        /// <summary>The row's label, with the number that makes the choice concrete.</summary>
        private string LabelFor(ItemAction action, in ItemInstance item)
        {
            switch (action)
            {
                case ItemAction.Equip: return "Equip";
                case ItemAction.QuickUse: return "Set as quick-use";
                case ItemAction.Upgrade:
                    return $"Upgrade  ({_bag.Inventory.Prices.UpgradeCost(item)})";
                case ItemAction.Combine:
                    return _nav.PendingCombine >= 0 ? "Combine with this" : "Combine…";
                case ItemAction.Sell:
                    return $"Sell  ({_bag.Inventory.Prices.SellPrice(item)})";
                default: return item.Locked ? "Unlock" : "Lock";
            }
        }

        private static string NameOf(in ItemInstance item, in UpgradeTarget target)
        {
            if (!target.IsAffix)
            {
                return target.CoreStat.ToString();
            }

            return target.AffixIndex < item.AffixCount
                ? item.Affixes[target.AffixIndex].Id.ToString()
                : "?";
        }
    }
}
