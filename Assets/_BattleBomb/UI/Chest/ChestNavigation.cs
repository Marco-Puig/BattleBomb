using UnityEngine;

namespace BattleBomb.UI.Chest
{
    /// <summary>Which tab of the chest screen is showing (D42).</summary>
    public enum ChestTab
    {
        ItemSack = 0,
        Hero = 1,
    }

    /// <summary>
    /// Which side of the shopkeeper's counter is showing (D43). The rack and the sack cannot
    /// share the panel — four rack rows and a five-row grid do not both fit the half — so the
    /// shop is two modes rather than one crowded screen. A plain chest never leaves
    /// <see cref="Sell"/>, which is the ordinary sack layout.
    /// </summary>
    public enum ShopMode
    {
        Buy = 0,
        Sell = 1,
    }

    /// <summary>Where the cursor currently lives.</summary>
    public enum ChestFocus
    {
        Tabs = 0,
        Filters = 1,
        Grid = 2,

        /// <summary>The dropdown that opens on the selected item: equip, upgrade, combine, sell, lock.</summary>
        Menu = 3,

        /// <summary>The stat list in the right panel, once "Upgrade" is chosen (Michael, M6 pass).</summary>
        Upgrade = 4,

        /// <summary>The shopkeeper's rack, on the shop screen only (D43).</summary>
        Stock = 5,

        /// <summary>The junk sweep and its rank threshold, on the shop screen only.</summary>
        Junk = 6,

        /// <summary>The worn loadout in the hero panel — where gear is deepened without taking
        /// it off first (Michael, 2026-08-23).</summary>
        Loadout = 7,

        /// <summary>The four allocatable stats under the loadout.</summary>
        Stats = 8,
    }

    /// <summary>What the screen must do after a confirm or a cancel — the model decides, the
    /// screen acts. <see cref="None"/> means the model already did everything (a focus move).</summary>
    public enum ChestOutcome
    {
        None = 0,
        TabSwitched = 1,
        MenuOpened = 2,
        RunMenuAction = 3,
        RunUpgrade = 4,
        RunBuy = 5,
        AllocateStat = 6,
        CombineCancelled = 7,
        Close = 8,
        RunSellJunk = 9,
        ModeSwitched = 10,
        OpenWornMenu = 11,
        RunWornAction = 12,
    }

    /// <summary>The sizes the cursor is clamped against this step. Rebuilt by the screen from
    /// live state before every move, so the model never holds a stale count.</summary>
    public readonly struct ChestLayout
    {
        public readonly int VisibleCount;
        public readonly int Columns;
        public readonly int FilterCount;
        public readonly int MenuCount;
        public readonly int UpgradeCount;
        public readonly int StockCount;

        /// <summary>Standing at a shopkeeper rather than a chest: the mode tabs and the junk
        /// sweep exist, and the rack is somewhere the cursor can go.</summary>
        public readonly bool IsShop;

        /// <summary>How far up the ladder the junk threshold may be pushed.</summary>
        public readonly int JunkRankCount;

        /// <summary>The hero panel is drawn beside the sack rather than behind a tab, so the two
        /// are neighbours the cursor can walk between. Solo at a chest, and nowhere else.</summary>
        public readonly bool HeroBeside;

        public ChestLayout(
            int visibleCount, int columns, int filterCount, int menuCount, int upgradeCount, int stockCount)
            : this(visibleCount, columns, filterCount, menuCount, upgradeCount, stockCount, false, 0, false)
        {
        }

        public ChestLayout(
            int visibleCount, int columns, int filterCount, int menuCount, int upgradeCount,
            int stockCount, bool isShop, int junkRankCount, bool heroBeside = false)
        {
            HeroBeside = heroBeside;
            VisibleCount = Mathf.Max(0, visibleCount);
            Columns = Mathf.Max(1, columns);
            FilterCount = Mathf.Max(1, filterCount);
            MenuCount = Mathf.Max(0, menuCount);
            UpgradeCount = Mathf.Max(0, upgradeCount);
            StockCount = Mathf.Max(0, stockCount);
            IsShop = isShop;
            JunkRankCount = Mathf.Max(0, junkRankCount);
        }
    }

    /// <summary>
    /// The chest screen's focus machine (D42), lifted out of the MonoBehaviour so every
    /// transition is a plain method under EditMode tests. M6's one real defect was a screen
    /// with no way out; this is the object that makes "no way out" a failing test rather than
    /// a mega-pass discovery. The screen owns the lists; this owns where the cursor is.
    /// </summary>
    public sealed class ChestNavigation
    {
        /// <summary>All / Weapons / Armor / Pets / Equipment / Consumables.</summary>
        public const int FilterCount = 6;

        /// <summary>Strength, Hp, Mana, Speed — the Hero tab's whole list (D32).</summary>
        public const int HeroRows = 4;

        /// <summary>The lowest rank the junk threshold can name — one rung is always sellable.</summary>
        public const int MinJunkRank = 1;

        public ChestTab Tab { get; private set; } = ChestTab.ItemSack;
        public ChestFocus Focus { get; private set; } = ChestFocus.Grid;

        /// <summary>
        /// Starts on <see cref="ShopMode.Sell"/> — the ordinary sack layout — so a plain chest,
        /// which never switches, behaves exactly as it always has. The shop screen moves it to
        /// <see cref="ShopMode.Buy"/> when it binds, which is the tab the design opens on.
        /// </summary>
        public ShopMode Mode { get; private set; } = ShopMode.Sell;

        /// <summary>The rank the junk sweep sells below. Rusty, as the design has it.</summary>
        public int JunkRank { get; private set; } = 2;
        public int Filter { get; private set; }
        public int Cursor { get; private set; }
        public int Action { get; private set; }
        public int UpgradeCursor { get; private set; }
        public int StockCursor { get; private set; }
        public int PendingCombine { get; private set; } = -1;

        /// <summary>Which worn slot the cursor is on, indexed as the hero panel fills them:
        /// helmet, chest, boots, weapon, pet, then the two equipment slots.</summary>
        public int LoadoutCursor { get; private set; }

        /// <summary>Which of the four stats the cursor is on. Separate from <see cref="Cursor"/>,
        /// which stays a bag index — one number serving both put the grid's highlight on a stat
        /// row's number every time the cursor crossed into the hero panel.</summary>
        public int StatCursor { get; private set; }

        /// <summary>
        /// The worn slots as the hero panel arranges them: two columns of armour and arms, with
        /// the equipment pair across the bottom. The hole is real — the right column is one short
        /// — and vertical moves step over it rather than stopping on nothing.
        /// </summary>
        private static readonly int[,] LoadoutGrid =
        {
            { 0, 3 },
            { 1, 4 },
            { 2, -1 },
            { 5, 6 },
        };

        private const int LoadoutRows = 4;

        /// <summary>How many worn slots the hero panel draws.</summary>
        public const int LoadoutSlots = 7;

        private static int LoadoutAt(int row, int column) =>
            row < 0 || row >= LoadoutRows || column < 0 || column > 1
                ? -1
                : LoadoutGrid[row, column];

        private static int LoadoutRowOf(int slot)
        {
            for (int row = 0; row < LoadoutRows; row++)
            {
                if (LoadoutGrid[row, 0] == slot || LoadoutGrid[row, 1] == slot)
                {
                    return row;
                }
            }

            return 0;
        }

        private static int LoadoutColumnOf(int slot)
        {
            for (int row = 0; row < LoadoutRows; row++)
            {
                if (LoadoutGrid[row, 1] == slot)
                {
                    return 1;
                }
            }

            return 0;
        }

        public void Move(int dx, int dy, in ChestLayout layout)
        {
            if (Focus == ChestFocus.Loadout)
            {
                MoveInLoadout(dx, dy, layout);
                return;
            }

            if (Focus == ChestFocus.Stats)
            {
                MoveInStats(dx, dy, layout);
                return;
            }

            if (Tab == ChestTab.Hero)
            {
                MoveInHero(dx, dy);
                return;
            }

            if (Focus == ChestFocus.Junk)
            {
                MoveInJunk(dx, dy, layout);
                return;
            }

            if (layout.IsShop && Mode == ShopMode.Buy)
            {
                MoveInBuy(dx, dy, layout);
                return;
            }

            switch (Focus)
            {
                case ChestFocus.Tabs:
                    if (dx != 0 && !layout.IsShop)
                    {
                        SwitchTab();
                    }
                    else if (dy < 0)
                    {
                        Focus = ChestFocus.Grid;
                    }

                    break;

                case ChestFocus.Filters:
                    if (dx != 0)
                    {
                        Filter = (Filter + dx + layout.FilterCount) % layout.FilterCount;
                        Cursor = 0;
                    }
                    else if (dy > 0)
                    {
                        // A shopkeeper has no hero half, so nothing sits above the filters.
                        if (!layout.IsShop)
                        {
                            Focus = ChestFocus.Tabs;
                        }
                    }
                    else
                    {
                        Focus = ChestFocus.Grid;
                    }

                    break;

                case ChestFocus.Grid:
                    MoveInGrid(dx, dy, layout);
                    break;

                case ChestFocus.Menu:
                    if (dy != 0 && layout.MenuCount > 0)
                    {
                        Action = (Action - dy + layout.MenuCount) % layout.MenuCount;
                    }

                    break;

                case ChestFocus.Upgrade:
                    if (dy != 0 && layout.UpgradeCount > 0)
                    {
                        UpgradeCursor = (UpgradeCursor - dy + layout.UpgradeCount) % layout.UpgradeCount;
                    }

                    break;

                case ChestFocus.Stock:
                    if (dx != 0)
                    {
                        StockCursor = Mathf.Clamp(StockCursor + dx, 0, Mathf.Max(0, layout.StockCount - 1));
                    }
                    else if (dy > 0)
                    {
                        Focus = ChestFocus.Grid;
                    }

                    break;
            }
        }

        public ChestOutcome Confirm(in ChestLayout layout)
        {
            switch (Focus)
            {
                case ChestFocus.Loadout:
                    return ChestOutcome.OpenWornMenu;

                case ChestFocus.Stats:
                    StatCursor = Mathf.Clamp(StatCursor, 0, HeroRows - 1);
                    return ChestOutcome.AllocateStat;

                case ChestFocus.Tabs:
                    if (layout.IsShop)
                    {
                        return ChestOutcome.None;
                    }

                    SwitchTab();
                    return ChestOutcome.TabSwitched;

                case ChestFocus.Filters:
                    Focus = ChestFocus.Grid;
                    return ChestOutcome.None;

                case ChestFocus.Grid:
                    if (layout.VisibleCount == 0)
                    {
                        return ChestOutcome.None;
                    }

                    Focus = ChestFocus.Menu;
                    Action = 0;
                    return ChestOutcome.MenuOpened;

                case ChestFocus.Menu:
                    return ChestOutcome.RunMenuAction;

                case ChestFocus.Upgrade:
                    return ChestOutcome.RunUpgrade;

                case ChestFocus.Junk:
                    return ChestOutcome.RunSellJunk;

                default:
                    return ChestOutcome.RunBuy;
            }
        }

        /// <summary>
        /// The dedicated button flips the counter, rather than a third tab doing it. In split
        /// co-op the tab row is already carrying sack-versus-hero, and stealing it for buy-versus-
        /// sell would cost the partner their stat sheet for as long as they stood at the rack.
        /// </summary>
        public ChestOutcome SwitchMode(in ChestLayout layout)
        {
            if (!layout.IsShop)
            {
                return ChestOutcome.None;
            }

            SetMode(Mode == ShopMode.Buy ? ShopMode.Sell : ShopMode.Buy, layout);
            return ChestOutcome.ModeSwitched;
        }

        /// <summary>Puts the cursor somewhere the new mode actually draws — the modes share no
        /// focus states beyond the tabs and the sweep.</summary>
        public void SetMode(ShopMode mode, in ChestLayout layout)
        {
            Mode = mode;
            PendingCombine = -1;
            Action = 0;
            if (mode == ShopMode.Buy)
            {
                EnterRack(layout);
                return;
            }

            Focus = ChestFocus.Grid;
            ClampCursor(layout.VisibleCount);
        }

        /// <summary>
        /// LB / RB (D57): the other half of whatever this screen has two of. At a shopkeeper
        /// that is the counter's side; in couch co-op, the sack-or-hero tab, landing in its body;
        /// solo at a chest, where both halves are already on screen, the cursor crosses to the
        /// other one. A combine pick and an open item menu are answered before anything moves.
        /// </summary>
        public ChestOutcome CycleTab(int direction, in ChestLayout layout)
        {
            if (direction == 0 || PendingCombine >= 0
                || Focus == ChestFocus.Menu || Focus == ChestFocus.Upgrade)
            {
                return ChestOutcome.None;
            }

            if (layout.IsShop)
            {
                return SwitchMode(layout);
            }

            if (layout.HeroBeside)
            {
                Focus = Focus == ChestFocus.Loadout || Focus == ChestFocus.Stats
                    ? ChestFocus.Grid
                    : ChestFocus.Loadout;
                return ChestOutcome.None;
            }

            SwitchTab();
            Focus = Tab == ChestTab.Hero ? ChestFocus.Loadout : ChestFocus.Grid;
            return ChestOutcome.TabSwitched;
        }

        /// <summary>B: the combine first, then one focus level, then the door.</summary>
        public ChestOutcome Cancel()
        {
            if (PendingCombine >= 0)
            {
                PendingCombine = -1;
                return ChestOutcome.CombineCancelled;
            }

            if (Focus == ChestFocus.Upgrade)
            {
                Focus = ChestFocus.Menu;
                return ChestOutcome.None;
            }

            if (Focus == ChestFocus.Stats)
            {
                Focus = ChestFocus.Loadout;
                return ChestOutcome.None;
            }

            if (Focus == ChestFocus.Loadout)
            {
                // Solo the sack is next door; behind a tab there is only the tab row above.
                Focus = Tab == ChestTab.Hero ? ChestFocus.Tabs : ChestFocus.Grid;
                return ChestOutcome.None;
            }

            if (Focus == ChestFocus.Junk)
            {
                Focus = Mode == ShopMode.Buy ? ChestFocus.Stock : ChestFocus.Grid;
                return ChestOutcome.None;
            }

            if (Focus == ChestFocus.Menu)
            {
                Focus = OnWorn ? ChestFocus.Loadout : ChestFocus.Grid;
                return ChestOutcome.None;
            }

            if (Focus == ChestFocus.Stock)
            {
                // Buy mode draws no grid, so there is no level under the rack to back out to.
                if (Mode == ShopMode.Buy)
                {
                    return ChestOutcome.Close;
                }

                Focus = ChestFocus.Grid;
                return ChestOutcome.None;
            }

            return ChestOutcome.Close;
        }

        /// <summary>"Upgrade" chosen in the menu: focus moves to the list of stat targets.</summary>
        public void OpenUpgrade()
        {
            Focus = ChestFocus.Upgrade;
            UpgradeCursor = 0;
        }

        /// <summary>A worn slot was confirmed: its verb list opens.</summary>
        public void OpenWornMenu()
        {
            Focus = ChestFocus.Menu;
            Action = 0;
        }

        /// <summary>Whether the menu or the upgrade list is acting on a worn piece rather than a
        /// bagged one — the screen needs it to know which request to send.</summary>
        public bool OnWorn { get; private set; }

        public void SetOnWorn(bool worn) => OnWorn = worn;

        /// <summary>Any other menu row ran: back to the grid, cursor inside whatever is left.</summary>
        public void FinishMenuAction(int visibleCount)
        {
            ClampCursor(visibleCount);
            Focus = OnWorn ? ChestFocus.Loadout : ChestFocus.Grid;
        }

        /// <summary>A point was spent. Stay if there is more to spend — a player deepening an item
        /// usually keeps going — otherwise leave rather than strand a dead cursor.</summary>
        public void FinishUpgrade(bool canStillUpgrade)
        {
            if (!canStillUpgrade)
            {
                Focus = OnWorn ? ChestFocus.Loadout : ChestFocus.Grid;
            }
        }

        /// <summary>A bought-out rack strands the cursor. In Buy mode the only other thing on the
        /// panel is the sweep, so it goes there; a chest still falls back to the grid.</summary>
        public void FinishBuy(int stockCount)
        {
            StockCursor = Mathf.Clamp(StockCursor, 0, Mathf.Max(0, stockCount - 1));
            if (stockCount == 0)
            {
                Focus = Mode == ShopMode.Buy ? ChestFocus.Junk : ChestFocus.Grid;
            }
        }

        public void BeginCombine(int bagIndex) => PendingCombine = bagIndex;

        public void CancelCombine() => PendingCombine = -1;

        /// <summary>Puts the cursor on a known cell — the screen jumping it onto the first
        /// duplicate the moment a combine begins, so the pick is one press away.</summary>
        public void SelectCell(int index, int visibleCount) =>
            Cursor = Mathf.Clamp(index, 0, Mathf.Max(0, visibleCount - 1));

        /// <summary>The sack changed under us: keep the cursor inside what is left.</summary>
        public void ClampCursor(int visibleCount)
        {
            if (Cursor >= visibleCount)
            {
                Cursor = Mathf.Max(0, visibleCount - 1);
            }
        }

        public void ClampAction(int menuCount) => Action = Mathf.Clamp(Action, 0, Mathf.Max(0, menuCount - 1));

        /// <summary>The hero tab's own row. Its body is the loadout and the stat list, which have
        /// their own focus states, so only the tab strip itself lands here.</summary>
        private void MoveInHero(int dx, int dy)
        {
            if (dx != 0)
            {
                SwitchTab();
            }
            else if (dy < 0)
            {
                Focus = ChestFocus.Loadout;
            }
        }

        /// <summary>
        /// The worn slots. Sideways off the left edge is the sack, which is the gesture that makes
        /// the hero panel reachable at all in solo — before this it could only be entered through
        /// a tab row that solo does not draw, so worn gear could not be deepened without taking it
        /// off first (Michael, 2026-08-23).
        /// </summary>
        private void MoveInLoadout(int dx, int dy, in ChestLayout layout)
        {
            LoadoutCursor = Mathf.Clamp(LoadoutCursor, 0, LoadoutSlots - 1);
            int row = LoadoutRowOf(LoadoutCursor);
            int column = LoadoutColumnOf(LoadoutCursor);

            if (dx != 0)
            {
                int target = LoadoutAt(row, column + dx);
                if (target >= 0)
                {
                    LoadoutCursor = target;
                }
                else if (dx < 0 && layout.HeroBeside)
                {
                    Focus = ChestFocus.Grid;
                }

                return;
            }

            if (dy == 0)
            {
                return;
            }

            int step = dy > 0 ? -1 : 1;
            for (int next = row + step; next >= 0 && next < LoadoutRows; next += step)
            {
                int target = LoadoutAt(next, column);
                if (target >= 0)
                {
                    LoadoutCursor = target;
                    return;
                }
            }

            if (dy > 0)
            {
                if (!layout.HeroBeside)
                {
                    Focus = ChestFocus.Tabs;
                }

                return;
            }

            Focus = ChestFocus.Stats;
            StatCursor = 0;
        }

        /// <summary>The four allocatable stats, sitting under the loadout in the same panel.</summary>
        private void MoveInStats(int dx, int dy, in ChestLayout layout)
        {
            if (dx < 0 && layout.HeroBeside)
            {
                Focus = ChestFocus.Grid;
                return;
            }

            if (dy == 0)
            {
                return;
            }

            int next = StatCursor - dy;
            if (next < 0)
            {
                Focus = ChestFocus.Loadout;
                return;
            }

            StatCursor = Mathf.Clamp(next, 0, HeroRows - 1);
        }

        private void MoveInGrid(int dx, int dy, in ChestLayout layout)
        {
            ClampCursor(layout.VisibleCount);
            if (layout.VisibleCount == 0)
            {
                if (dy > 0)
                {
                    Focus = ChestFocus.Filters;
                }
                else if (dx > 0 && layout.HeroBeside)
                {
                    Focus = ChestFocus.Loadout;
                }

                return;
            }

            if (dx != 0)
            {
                bool atRightEdge = Cursor % layout.Columns == layout.Columns - 1
                    || Cursor == layout.VisibleCount - 1;
                if (dx > 0 && atRightEdge && layout.HeroBeside && PendingCombine < 0)
                {
                    Focus = ChestFocus.Loadout;
                    return;
                }

                Cursor = Mathf.Clamp(Cursor + dx, 0, layout.VisibleCount - 1);
                return;
            }

            int next = Cursor - dy * layout.Columns;
            if (PendingCombine >= 0 && (next < 0 || next >= layout.VisibleCount))
            {
                // Mid-combine the grid is the whole question, and everything in it is an answer.
                // Walking off either edge into the filters or the rack would leave a pick open
                // behind a screen that no longer mentions it; B is the way out.
                return;
            }

            if (next < 0)
            {
                Focus = ChestFocus.Filters;
                return;
            }

            if (next >= layout.VisibleCount)
            {
                if (layout.IsShop)
                {
                    // Below the sack, at a shopkeeper, is the sweep that empties it.
                    Focus = ChestFocus.Junk;
                }

                return;
            }

            Cursor = next;
        }

        /// <summary>
        /// Buy mode is a short vertical list, not a grid: the rack is four rows, and below them
        /// sits the junk sweep. Sideways does nothing here, which is why the threshold stepper is
        /// drawn with left and right arrows rather than the up and down pair the design sketched —
        /// the affordance has to match the stick that works.
        /// </summary>
        private void MoveInBuy(int dx, int dy, in ChestLayout layout)
        {
            if (dy < 0)
            {
                if (StockCursor < layout.StockCount - 1)
                {
                    StockCursor++;
                }
                else
                {
                    Focus = ChestFocus.Junk;
                }
            }
            else if (dy > 0 && StockCursor > 0)
            {
                StockCursor--;
            }
        }

        /// <summary>Sideways walks the threshold up and down the ladder; up leaves for the body
        /// of whichever mode is showing.</summary>
        private void MoveInJunk(int dx, int dy, in ChestLayout layout)
        {
            if (dx != 0)
            {
                JunkRank = Mathf.Clamp(
                    JunkRank + dx, MinJunkRank, Mathf.Max(MinJunkRank, layout.JunkRankCount));
                return;
            }

            if (dy > 0)
            {
                if (Mode == ShopMode.Buy)
                {
                    EnterRack(layout);
                    StockCursor = Mathf.Max(0, layout.StockCount - 1);
                    return;
                }

                Focus = ChestFocus.Grid;
            }
        }

        private void EnterRack(in ChestLayout layout)
        {
            Focus = ChestFocus.Stock;
            StockCursor = Mathf.Clamp(StockCursor, 0, Mathf.Max(0, layout.StockCount - 1));
        }

        private void SwitchTab()
        {
            Tab = Tab == ChestTab.ItemSack ? ChestTab.Hero : ChestTab.ItemSack;
            Cursor = 0;
            Action = 0;
            LoadoutCursor = 0;
            StatCursor = 0;
            PendingCombine = -1;
            Focus = ChestFocus.Tabs;
        }
    }
}
