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
        Unequip = 6,
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

        /// <summary>
        /// The highest rung the junk sweep's threshold may be pushed to, so the sweep can clear
        /// everything below Clean and no further. The design draws the stepper without a confirm
        /// step, and one press that could sell a Legendary is a different feature from one that
        /// clears trash. Raising this is a single edit if Michael wants a longer reach.
        /// </summary>
        private const int JunkRankCeiling = (int)QualityRank.Clean;

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

        private Text _sackTitle;
        private Text _coin;
        private Text _sackMeterText;
        private Image _sackMeter;
        private Text _tabSack;
        private Text _tabHero;
        private Text _sortText;
        private Text _hint;
        private readonly List<Image> _filterChips = new List<Image>();
        private readonly List<Text> _filterLabels = new List<Text>();

        private RectTransform _gridRoot;
        private readonly List<ItemCell> _cells = new List<ItemCell>();
        private float _cellSize = 96f;
        private float _cellStride = 104f;
        private float _gridLeft;

        private ComparePanel _compare;
        private ActionPopover _popover;
        private readonly List<string> _popLabels = new List<string>();
        private readonly List<int> _popPrices = new List<int>();

        private RectTransform _heroRoot;
        private HeroPanel _heroPanel;

        /// <summary>
        /// Eight, always. The sack panel is half the display solo and the player's whole half in
        /// local co-op — the same width either way (UI Pass 01's "two halves, never resized"), so
        /// there is nothing to reflow. <see cref="GridColumnsSplit"/> is the narrow-aspect and
        /// mobile fallback, which no layout reaches yet.
        /// </summary>
        private int Columns => GridColumnsWide;

        /// <summary>Standing at the shopkeeper rather than a chest: the counter has two sides
        /// and the junk sweep is on the panel.</summary>
        private bool IsShop => _kind == InteractionKind.Shopkeeper;

        /// <summary>Showing the rack rather than the sack.</summary>
        private bool Buying => IsShop && _nav.Mode == ShopMode.Buy;

        /// <summary>The hero panel is drawn beside the sack, so the cursor can walk into it.
        /// Solo at a chest: split puts it behind a tab, and a shopkeeper has no hero half.</summary>
        private bool HeroBeside => !_split && !IsShop;

        /// <summary>The worn slot under the loadout cursor.</summary>
        private HeroPanel.Slot WornSlot =>
            HeroPanel.Order[Mathf.Clamp(_nav.LoadoutCursor, 0, HeroPanel.Order.Length - 1)];

        private ItemInstance WornItem =>
            _bag.Inventory.Loadout.Worn(WornSlot.Which, WornSlot.EquipmentIndex);

        /// <summary>The rank the sweep currently sells below.</summary>
        private QualityRank JunkThreshold => (QualityRank)_nav.JunkRank;

        private ChestLayout Layout() => new ChestLayout(
            _visible.Count, Columns, FilterNames.Length, _menu.Count, _upgradeTargets.Count,
            _stock.Count, IsShop, JunkRankCeiling, HeroBeside);

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
                CollectVisible();
                _nav.SetMode(ShopMode.Buy, Layout());
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

            if (command.WasPressed(CommandButtons.Magic))
            {
                // Mid-combine this is the bulk verb; otherwise, at a shop, it flips the counter.
                // Heavy was the obvious button for "combine all" and is the one thing it cannot
                // be: Heavy is how a combine is abandoned, and M6's one real defect was a screen
                // with no way out (Michael suggested Heavy, 2026-08-23).
                if (_nav.PendingCombine >= 0)
                {
                    RunCombineAll();
                    Refresh();
                    return;
                }

                if (IsShop)
                {
                    CollectVisible();
                    _nav.SwitchMode(Layout());
                    Refresh();
                    return;
                }
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
            int pending = _nav.PendingCombine;
            switch (_nav.Cancel())
            {
                case ChestOutcome.CombineCancelled:
                    RecollectOnto(pending);
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

        /// <summary>
        /// The bag moved — possibly under the partner's hand (D51). Repaint from the new truth,
        /// and drop any combine in progress: the pick is a bag index, and selling or equipping
        /// anything above it slides every index below down one. Holding the number through that
        /// would gamble away an item the player never pointed at.
        /// </summary>
        internal void OnBagChanged()
        {
            if (_nav.PendingCombine >= 0)
            {
                _nav.CancelCombine();
                Flash("Combine cancelled — the sack moved.");
                return;
            }

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
                    Flash(_bag.RequestAllocate(stats[_nav.StatCursor])
                        ? "Point spent." : "No points to spend.");
                    break;

                case ChestOutcome.MenuOpened:
                    _nav.SetOnWorn(false);
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

                case ChestOutcome.RunSellJunk:
                    RunSellJunk();
                    break;

                case ChestOutcome.OpenWornMenu:
                    if (WornItem.IsEmpty)
                    {
                        Flash("Nothing in that slot.");
                        break;
                    }

                    _nav.SetOnWorn(true);
                    BuildWornMenu(WornItem);
                    _nav.OpenWornMenu();
                    break;
            }

            Refresh();
        }

        /// <summary>
        /// The sweep: every unlocked piece below the threshold, sold in one press. Consumables are
        /// spared — a stack of potions is not junk, which is the same call auto-sell makes when it
        /// picks what to give up — and worn gear was never in the sack to begin with.
        /// </summary>
        private void RunSellJunk()
        {
            JunkSale sale = _bag.RequestSellJunk(JunkThreshold);
            if (sale.IsEmpty)
            {
                Flash($"Nothing below {JunkThreshold}.");
                return;
            }

            CollectVisible();
            _nav.ClampCursor(_visible.Count);
            Flash($"Cleared {sale.Pieces} for {sale.Coins}.");
        }

        private void RunMenuAction()
        {
            if (_nav.OnWorn)
            {
                RunWornAction();
                return;
            }

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

        /// <summary>
        /// The verbs a worn piece offers. Deepening is the one that matters: gear could only be
        /// upgraded out of the bag before, so improving something you were wearing meant taking it
        /// off, upgrading it and putting it back on (Michael, 2026-08-23).
        /// </summary>
        private void BuildWornMenu(in ItemInstance worn)
        {
            _menu.Clear();
            if (ItemUpgrade.CanUpgrade(worn))
            {
                _menu.Add(ItemAction.Upgrade);
            }

            _menu.Add(ItemAction.Unequip);
            _menu.Add(ItemAction.Lock);
            _nav.ClampAction(_menu.Count);
        }

        private void RunWornAction()
        {
            ItemInstance worn = WornItem;
            if (worn.IsEmpty || _nav.Action >= _menu.Count)
            {
                return;
            }

            HeroPanel.Slot slot = WornSlot;
            switch (_menu[_nav.Action])
            {
                case ItemAction.Upgrade:
                    ItemUpgrade.Targets(worn, _upgradeTargets);
                    _nav.OpenUpgrade();
                    return;

                case ItemAction.Unequip:
                    Flash(_bag.RequestUnequip(slot.Which, slot.EquipmentIndex)
                        ? "Taken off." : "The sack is full.");
                    break;

                default:
                    _bag.RequestLockWorn(slot.Which, slot.EquipmentIndex, !worn.Locked);
                    Flash(worn.Locked ? "Released." : "Locked.");
                    break;
            }

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

        /// <summary>
        /// Grinds the whole pile in one press. An unpromoted reroll comes back at the same rank
        /// and rejoins the pile, so a stack of four is three combines ending in one piece rather
        /// than two ending in two — the run keeps going until fewer than two of that exact
        /// definition and rank are left. A promotion leaves the pile and is never re-gambled.
        /// </summary>
        private void RunCombineAll()
        {
            int anchor = _nav.PendingCombine;
            if (anchor < 0)
            {
                return;
            }

            // Released before the bag moves, exactly as a single combine does: combining raises
            // Changed, and OnBagChanged reads a still-pending index as a sack that shifted.
            _nav.CancelCombine();

            if (!_bag.RequestCombineAll(anchor, out CombineRun run))
            {
                RecollectOnto(anchor);
                Flash("Nothing left to combine.");
                return;
            }

            RecollectOnto(_bag.Inventory.Items.Count - 1);
            Flash(run.Promotions > 0
                ? $"Combined {run.Combines} — {run.Promotions} UPGRADED!"
                : $"Combined {run.Combines}.");
        }

        private void RunCombine(int bagIndex)
        {
            if (_nav.PendingCombine < 0)
            {
                _nav.BeginCombine(bagIndex);

                // The grid has just collapsed to this item and its duplicates; put the cursor on
                // the first duplicate rather than back on the pick, so the answer is one press away.
                CollectVisible();
                int anchorCell = _visible.IndexOf(bagIndex);
                _nav.SelectCell(anchorCell == 0 ? 1 : 0, _visible.Count);
                Flash("Pick the duplicate to combine with.");
                return;
            }

            if (_nav.PendingCombine == bagIndex)
            {
                _nav.CancelCombine();
                RecollectOnto(bagIndex);
                Flash("Combine cancelled.");
                return;
            }

            // The pick is released before the bag is touched: combining raises Changed, and
            // OnBagChanged reads a still-pending index as a sack that moved under it.
            int first = _nav.PendingCombine;
            _nav.CancelCombine();

            if (_bag.RequestCombine(first, bagIndex, out CombineResult result))
            {
                // The reroll lands at the end of the bag; the cursor follows it, since seeing
                // what the gamble returned is the whole reason the gamble was taken.
                RecollectOnto(_bag.Inventory.Items.Count - 1);
                Flash(result.Promoted
                    ? $"UPGRADED! {result.Item.DisplayName}"
                    : $"Rerolled: {result.Item.DisplayName}");
                return;
            }

            RecollectOnto(bagIndex);
            Flash("These two cannot combine.");
        }

        /// <summary>Rebuilds the grid and puts the cursor back on a known bag index — where the
        /// eye already is when a combine ends and the whole sack returns to the screen.</summary>
        private void RecollectOnto(int bagIndex)
        {
            CollectVisible();
            _nav.SelectCell(_visible.IndexOf(bagIndex), _visible.Count);
        }

        /// <summary>
        /// Spends one point into the stat under the cursor in the right panel. Focus stays here
        /// afterwards: a player deepening an item usually wants to keep going, and the panel
        /// shows the cost doubling as they do.
        /// </summary>
        private void RunUpgrade()
        {
            if (_nav.UpgradeCursor >= _upgradeTargets.Count)
            {
                return;
            }

            if (_nav.OnWorn)
            {
                RunUpgradeWorn();
                return;
            }

            CollectVisible();
            if (_visible.Count == 0)
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

        /// <summary>The same spend, against the piece the player is wearing.</summary>
        private void RunUpgradeWorn()
        {
            ItemInstance worn = WornItem;
            if (worn.IsEmpty)
            {
                return;
            }

            HeroPanel.Slot slot = WornSlot;
            int price = _bag.Inventory.Prices.UpgradeCost(worn);

            if (_bag.RequestUpgradeWorn(slot.Which, slot.EquipmentIndex, _upgradeTargets[_nav.UpgradeCursor]))
            {
                Flash($"Deepened for {price}.");
                ItemInstance next = WornItem;
                ItemUpgrade.Targets(next, _upgradeTargets);
                _nav.FinishUpgrade(ItemUpgrade.CanUpgrade(next));
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
            IReadOnlyList<ItemStack> items = _bag.Inventory.Items;
            if (_nav.PendingCombine >= 0 && _nav.PendingCombine < items.Count)
            {
                // Only duplicates can combine, so only duplicates are shown — the category
                // filter set aside, since the pick is not a category question (Michael, M7 pass).
                _bag.Inventory.CombineChoices(_nav.PendingCombine, _visible);
                _nav.ClampCursor(_visible.Count);
                return;
            }

            _visible.Clear();
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

            if (_nav.PendingCombine >= 0)
            {
                // One question is open, so the menu asks only it. Every other row acts on the
                // item under the cursor, and two of them — Equip and Sell — would shift the very
                // index the pending pick is stored as.
                _menu.Add(ItemAction.Combine);
                _nav.ClampAction(_menu.Count);
                return;
            }

            ItemInstance item = _bag.Inventory.Items[bagIndex].Item;
            _menu.Add(item.IsConsumable ? ItemAction.QuickUse : ItemAction.Equip);

            if (ItemUpgrade.CanUpgrade(item))
            {
                _menu.Add(ItemAction.Upgrade);
            }

            if (!item.IsConsumable && _bag.Inventory.HasCombinePartner(bagIndex))
            {
                // Nothing to pair with means nothing the row could do: a Combine that opens onto
                // an empty grid is the dead end this list exists to avoid.
                _menu.Add(ItemAction.Combine);
            }

            _menu.Add(ItemAction.Sell);
            _menu.Add(ItemAction.Lock);

            _nav.ClampAction(_menu.Count);
        }

        /// <summary>The row's label, with the number that makes the choice concrete.</summary>
        private string LabelFor(ItemAction action, in ItemInstance item, int bagIndex)
        {
            switch (action)
            {
                case ItemAction.Equip: return "Equip";
                case ItemAction.QuickUse: return "Set as quick-use";
                case ItemAction.Upgrade:
                    return $"Upgrade  ({_bag.Inventory.Prices.UpgradeCost(item)})";
                case ItemAction.Combine:
                    if (_nav.PendingCombine < 0)
                    {
                        return "Combine…";
                    }

                    return _nav.PendingCombine == bagIndex ? "Cancel combine" : "Combine with this";
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
