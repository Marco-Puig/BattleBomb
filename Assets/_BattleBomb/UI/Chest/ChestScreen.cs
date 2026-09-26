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
    /// Navigated entirely by <see cref="PlayerCommand"/> through <see cref="MenuPress"/> (D57):
    /// the stick moves, A confirms, B backs out a level and finally closes, Start leaves from
    /// anywhere, X and Y are one-press shortcuts, and the shoulders switch halves. That keeps the
    /// menu inside rule 3's one-input-path promise and makes two players on two screen halves
    /// need no extra machinery.
    /// </summary>
    internal sealed partial class ChestScreen : MonoBehaviour
    {
        private const int GridColumnsWide = 8;
        private const int GridColumnsSplit = 4;
        private const int GridRows = 5;

        /// <summary>The rack's rows — the simulation's rack size (D43), which the layout is drawn for.</summary>
        private const int ShopStockCount = Gameplay.Simulation.SimulationDriver.RackSize;

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

        /// <summary>This player's menu actions (HANDOFF-M8 planning decision 11) — the screen asks, it never
        /// calls the bag itself.</summary>
        private IPlayerRequests _requests;

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

        /// <summary>A copy of the simulation's rack for this visit (D43, HANDOFF-M8 planning decision 12),
        /// taken by <see cref="CollectVisible"/> — empty at a chest.</summary>
        private readonly List<ItemInstance> _stock = new List<ItemInstance>();

        private Text _sackTitle;
        private Text _coin;
        private Text _sackMeterText;
        private Image _sackMeter;
        private Text _tabSack;
        private Text _tabHero;
        private Text _sortText;
        private PromptRow _promptRow;
        private readonly List<Prompt> _prompts = new List<Prompt>();
        private InputFamily _family;
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
            _stock.Count, IsShop, JunkRankCeiling, HeroBeside, GridRows);

        internal void Bind(
            PlayerInventory bag, Gameplay.Characters.CharacterActor sheetSource,
            int playerId, InteractionKind kind, bool split)
        {
            _bag = bag;
            _sheetSource = sheetSource;
            _playerId = playerId;
            _kind = kind;
            _split = split;
            _requests = Host != null ? Host.RequestsFor(playerId) : null;
            if (kind == InteractionKind.Shopkeeper)
            {
                // The simulation rolled this visit's rack as the shop opened (D43); CollectVisible copies it.
                CollectVisible();
                _nav.SetMode(ShopMode.Buy, Layout());
            }

            _family = Host != null ? Host.FamilyFor(playerId) : InputFamily.Keyboard;
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

            // The icons follow the hands: a player who picks up a controller mid-menu sees A B X Y
            // on the next step, even before pressing anything the screen reacts to.
            InputFamily family = Host != null ? Host.FamilyFor(_playerId) : InputFamily.Keyboard;
            if (family != _family)
            {
                _family = family;
                Refresh();
            }

            // The press that opened this screen must not also act inside it. It matters more now
            // than in M6: the opening press is X, which is Light to the fight and Option — sell —
            // to this screen.
            if (_swallowUntilRelease)
            {
                if (MenuPress.AnyHeld(command))
                {
                    // A stick held through the opening press is not a push either: it counts as
                    // already down, and moves nothing until it is let go and pushed again.
                    _lastMove = command.Move;
                    _repeatAt = float.MaxValue;
                    return;
                }

                _swallowUntilRelease = false;
            }

            StepCursor(command, deltaTime);
            MenuPress press = MenuPress.From(command);

            if (press.Pause)
            {
                // Start leaves from anywhere. Escape now backs out a level at a time, so the pad's
                // system button is what keeps M6's promise that getting out is never a puzzle.
                Host?.RequestClose(_playerId);
                return;
            }

            if (press.Tab != 0)
            {
                CollectVisible();
                _nav.CycleTab(press.Tab, Layout());
                Refresh();
                return;
            }

            // A guest's last action is still on its way to the host and back (D61): A, X and Y wait for
            // its answer, so no second action is aimed at a bag that is about to change under it.
            bool actionsWait = Waiting;

            if (press.Option && !actionsWait)
            {
                RunOption();
                Refresh();
                return;
            }

            if (press.Lock && !actionsWait)
            {
                RunLock();
                Refresh();
                return;
            }

            if (press.Confirm && !actionsWait)
            {
                Confirm();
            }
            else if (press.Back)
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
                    // Nothing left to back out of: B closes the chest.
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

        /// <summary>A guest's action still waiting for the host's answer.</summary>
        private bool Waiting => _requests != null && _requests.Pending;

        /// <summary>
        /// Hands an action to this player's requests (HANDOFF-M8 planning decision 11) and runs
        /// <paramref name="answered"/> with what happened. On the couch that is now, inside this call, so
        /// the code after it reads exactly as it did when the screen called the bag itself; on a guest it
        /// is a round trip later, after the host's copy of the bag has arrived — so an answer reads the bag
        /// as it now is, never as it was when the button went down.
        /// </summary>
        private void Send(PlayerRequest request, System.Action<RequestOutcome> answered)
        {
            if (_requests == null)
            {
                return;
            }

            _requests.Send(request, outcome =>
            {
                // A guest's screen can close while its request is on the wire.
                if (this == null || _bag == null)
                {
                    return;
                }

                answered?.Invoke(outcome);
                if (outcome.Refusal == RequestRefusal.StaleSack)
                {
                    Flash("The sack moved — try again.");
                }
            });
        }

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

        /// <summary>The rack moved — a purchase, or on a guest the host's rack arriving.</summary>
        internal void OnRackChanged() => Refresh();

        // ── Actions ──────────────────────────────────────────────────────────────────

        private void Confirm()
        {
            CollectVisible();
            switch (_nav.Confirm(Layout()))
            {
                case ChestOutcome.AllocateStat:
                    StatId[] stats = { StatId.Strength, StatId.Hp, StatId.Mana, StatId.Speed };
                    Send(PlayerRequest.Allocate(stats[_nav.StatCursor]),
                        outcome => Flash(outcome.Ok ? "Point spent." : "No points to spend."));
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
            QualityRank below = JunkThreshold;
            Send(PlayerRequest.SellJunk(below), outcome =>
            {
                if (!outcome.Ok)
                {
                    Flash($"Nothing below {below}.");
                    return;
                }

                CollectVisible();
                _nav.ClampCursor(_visible.Count);
                Flash($"Cleared {outcome.B} for {outcome.C}.");
            });
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
                    Send(PlayerRequest.Equip(bagIndex),
                        outcome => Flash(outcome.Ok ? "Equipped." : "Cannot equip — check the level."));
                    break;

                case ItemAction.QuickUse:
                    Send(PlayerRequest.QuickConsumable(item.DefinitionId),
                        outcome => Flash(outcome.Ok ? "Quick-use set." : "Cannot set."));
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
                    SellAt(bagIndex);
                    break;

                case ItemAction.Lock:
                    ToggleLockAt(bagIndex);
                    break;
            }

            CollectVisible();
            _nav.FinishMenuAction(_visible.Count);
        }

        private void SellAt(int bagIndex)
        {
            Send(PlayerRequest.Sell(bagIndex),
                outcome => Flash(outcome.Ok ? $"Sold for {outcome.A}." : "Locked — release it first."));
        }

        private void ToggleLockAt(int bagIndex)
        {
            bool wasLocked = _bag.Inventory.Items[bagIndex].Item.Locked;
            Send(PlayerRequest.Lock(bagIndex, !wasLocked), _ => Flash(wasLocked ? "Released." : "Locked."));
        }

        private void ToggleWornLock()
        {
            ItemInstance worn = WornItem;
            if (worn.IsEmpty)
            {
                Flash("Nothing in that slot.");
                return;
            }

            HeroPanel.Slot slot = WornSlot;
            bool wasLocked = worn.Locked;
            Send(PlayerRequest.LockWorn(slot.Which, slot.EquipmentIndex, !wasLocked),
                _ => Flash(wasLocked ? "Released." : "Locked."));
        }

        /// <summary>
        /// X (D57): the one-press verb for what the cursor is on. Mid-combine it grinds the whole
        /// pile; on a sack item it sells — instantly, by Michael's choice (2026-09-24), with the
        /// lock as the only safety. At the rack A already buys in one press, so X there would be a
        /// second button for the same job, which the map rules out.
        /// </summary>
        private void RunOption()
        {
            if (_nav.PendingCombine >= 0)
            {
                // X from the pick's one-row Combine menu too: leave it, or the full menu of the
                // rerolled item would be up and the next A would equip it.
                RunCombineAll();
                _nav.FinishMenuAction(_visible.Count);
                return;
            }

            if (_nav.Focus != ChestFocus.Grid)
            {
                return;
            }

            CollectVisible();

            // Only ever the item drawn under the cursor (F5): X sells instantly with no undo, so a
            // cursor the view has not caught up with sells nothing.
            if (_visible.Count == 0 || !_nav.IsCursorDrawn(Layout()))
            {
                return;
            }

            SellAt(_visible[_nav.Cursor]);
            CollectVisible();
            _nav.ClampCursor(_visible.Count);
        }

        /// <summary>Y (D57): lock or release the item under the cursor, in the sack or worn.</summary>
        private void RunLock()
        {
            if (_nav.PendingCombine >= 0)
            {
                return;
            }

            if (_nav.Focus == ChestFocus.Loadout)
            {
                ToggleWornLock();
                return;
            }

            if (_nav.Focus != ChestFocus.Grid)
            {
                return;
            }

            CollectVisible();
            if (_visible.Count > 0 && _nav.IsCursorDrawn(Layout()))
            {
                ToggleLockAt(_visible[_nav.Cursor]);
            }
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
                    Send(PlayerRequest.Unequip(slot.Which, slot.EquipmentIndex),
                        outcome => Flash(outcome.Ok ? "Taken off." : "The sack is full."));
                    break;

                default:
                    ToggleWornLock();
                    break;
            }

            _nav.FinishMenuAction(_visible.Count);
        }

        /// <summary>Buying from the rack (D43): the screen names the slot and the simulation does the rest —
        /// the piece, its price, the money and the room (HANDOFF-M8 planning decision 12).</summary>
        private void RunBuy()
        {
            if (_nav.StockCursor < 0 || _nav.StockCursor >= _stock.Count)
            {
                return;
            }

            int price = _bag.Inventory.Prices.BuyPrice(_stock[_nav.StockCursor]);
            Send(PlayerRequest.Buy(_nav.StockCursor), outcome =>
            {
                if (!outcome.Ok)
                {
                    Flash(_bag.Inventory.IsFull ? "The sack is full." : $"Not enough coin ({price}).");
                    return;
                }

                CollectVisible();
                _nav.FinishBuy(_stock.Count);
                Flash($"Bought for {outcome.A}.");
            });
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

            Send(PlayerRequest.CombineAll(anchor), outcome =>
            {
                if (!outcome.Ok)
                {
                    RecollectOnto(anchor);
                    Flash("Nothing left to combine.");
                    return;
                }

                RecollectOnto(_bag.Inventory.Items.Count - 1);
                Flash(outcome.B > 0
                    ? $"Combined {outcome.A} — {outcome.B} UPGRADED!"
                    : $"Combined {outcome.A}.");
            });
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

            Send(PlayerRequest.Combine(first, bagIndex), outcome =>
            {
                if (outcome.Ok)
                {
                    // The reroll lands at the end of the bag; the cursor follows it, since seeing
                    // what the gamble returned is the whole reason the gamble was taken.
                    int landed = _bag.Inventory.Items.Count - 1;
                    RecollectOnto(landed);
                    string name = landed >= 0 ? _bag.Inventory.Items[landed].Item.DisplayName : string.Empty;
                    Flash(outcome.A != 0 ? $"UPGRADED! {name}" : $"Rerolled: {name}");
                    return;
                }

                RecollectOnto(bagIndex);
                Flash("These two cannot combine.");
            });
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

            Send(PlayerRequest.Upgrade(bagIndex, _upgradeTargets[_nav.UpgradeCursor]), outcome =>
            {
                if (outcome.Ok && bagIndex < _bag.Inventory.Items.Count)
                {
                    Flash($"Deepened for {price}.");
                    ItemUpgrade.Targets(_bag.Inventory.Items[bagIndex].Item, _upgradeTargets);
                    _nav.FinishUpgrade(ItemUpgrade.CanUpgrade(_bag.Inventory.Items[bagIndex].Item));
                    return;
                }

                Flash(_bag.Wallet.CanAfford(price)
                    ? "The capacity is spent."
                    : $"Not enough coin ({price}).");
            });
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

            Send(PlayerRequest.UpgradeWorn(slot.Which, slot.EquipmentIndex, _upgradeTargets[_nav.UpgradeCursor]), outcome =>
            {
                if (outcome.Ok)
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
            });
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
            CollectRack();
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

        /// <summary>The rack as the simulation holds it — copied every time, never kept, so a screen rebuilt
        /// mid-visit, or a guest's screen after the host's rack arrives, draws the one rack there is.</summary>
        private void CollectRack()
        {
            _stock.Clear();
            if (!IsShop || Host == null)
            {
                return;
            }

            IReadOnlyList<ItemInstance> rack = Host.RackFor(_playerId);
            for (int i = 0; i < rack.Count; i++)
            {
                _stock.Add(rack[i]);
            }
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
