using UnityEngine;

namespace BattleBomb.UI.Chest
{
    /// <summary>Which tab of the chest screen is showing (D42).</summary>
    public enum ChestTab
    {
        ItemSack = 0,
        Hero = 1,
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

        public ChestLayout(
            int visibleCount, int columns, int filterCount, int menuCount, int upgradeCount, int stockCount)
        {
            VisibleCount = Mathf.Max(0, visibleCount);
            Columns = Mathf.Max(1, columns);
            FilterCount = Mathf.Max(1, filterCount);
            MenuCount = Mathf.Max(0, menuCount);
            UpgradeCount = Mathf.Max(0, upgradeCount);
            StockCount = Mathf.Max(0, stockCount);
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

        public ChestTab Tab { get; private set; } = ChestTab.ItemSack;
        public ChestFocus Focus { get; private set; } = ChestFocus.Grid;
        public int Filter { get; private set; }
        public int Cursor { get; private set; }
        public int Action { get; private set; }
        public int UpgradeCursor { get; private set; }
        public int StockCursor { get; private set; }
        public int PendingCombine { get; private set; } = -1;

        public void Move(int dx, int dy, in ChestLayout layout)
        {
            if (Tab == ChestTab.Hero)
            {
                MoveInHero(dx, dy);
                return;
            }

            switch (Focus)
            {
                case ChestFocus.Tabs:
                    if (dx != 0)
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
                        Focus = ChestFocus.Tabs;
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
            if (Tab == ChestTab.Hero)
            {
                if (Focus == ChestFocus.Tabs)
                {
                    SwitchTab();
                    return ChestOutcome.TabSwitched;
                }

                Cursor = Mathf.Clamp(Cursor, 0, HeroRows - 1);
                return ChestOutcome.AllocateStat;
            }

            switch (Focus)
            {
                case ChestFocus.Tabs:
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

                default:
                    return ChestOutcome.RunBuy;
            }
        }

        /// <summary>Heavy: the combine first, then one focus level, then the door.</summary>
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

            if (Focus == ChestFocus.Menu || Focus == ChestFocus.Stock)
            {
                Focus = ChestFocus.Grid;
                return ChestOutcome.None;
            }

            return ChestOutcome.Close;
        }

        /// <summary>"Upgrade" chosen in the menu: focus moves to the right panel's stat list.</summary>
        public void OpenUpgrade()
        {
            Focus = ChestFocus.Upgrade;
            UpgradeCursor = 0;
        }

        /// <summary>Any other menu row ran: back to the grid, cursor inside whatever is left.</summary>
        public void FinishMenuAction(int visibleCount)
        {
            ClampCursor(visibleCount);
            Focus = ChestFocus.Grid;
        }

        /// <summary>A point was spent. Stay if there is more to spend — a player deepening an item
        /// usually keeps going — otherwise leave rather than strand a dead cursor.</summary>
        public void FinishUpgrade(bool canStillUpgrade)
        {
            if (!canStillUpgrade)
            {
                Focus = ChestFocus.Grid;
            }
        }

        public void FinishBuy(int stockCount)
        {
            StockCursor = Mathf.Clamp(StockCursor, 0, Mathf.Max(0, stockCount - 1));
            if (stockCount == 0)
            {
                Focus = ChestFocus.Grid;
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
            if (Tab == ChestTab.Hero)
            {
                return;
            }

            if (Cursor >= visibleCount)
            {
                Cursor = Mathf.Max(0, visibleCount - 1);
            }
        }

        public void ClampAction(int menuCount) => Action = Mathf.Clamp(Action, 0, Mathf.Max(0, menuCount - 1));

        private void MoveInHero(int dx, int dy)
        {
            if (dy != 0)
            {
                Cursor = Mathf.Clamp(Cursor - dy, 0, HeroRows - 1);
            }
            else if (dx != 0 && Focus == ChestFocus.Tabs)
            {
                SwitchTab();
            }

            if (dy > 0 && Cursor == 0 && Focus != ChestFocus.Tabs)
            {
                Focus = ChestFocus.Tabs;
            }
            else if (dy < 0 && Focus == ChestFocus.Tabs)
            {
                Focus = ChestFocus.Grid;
            }
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

                return;
            }

            if (dx != 0)
            {
                Cursor = Mathf.Clamp(Cursor + dx, 0, layout.VisibleCount - 1);
                return;
            }

            int next = Cursor - dy * layout.Columns;
            if (PendingCombine >= 0 && (next < 0 || next >= layout.VisibleCount))
            {
                // Mid-combine the grid is the whole question, and everything in it is an answer.
                // Walking off either edge into the filters or the rack would leave a pick open
                // behind a screen that no longer mentions it; Heavy is the way out.
                return;
            }

            if (next < 0)
            {
                Focus = ChestFocus.Filters;
                return;
            }

            if (next >= layout.VisibleCount)
            {
                if (layout.StockCount > 0)
                {
                    Focus = ChestFocus.Stock;
                    StockCursor = 0;
                }

                return;
            }

            Cursor = next;
        }

        private void SwitchTab()
        {
            Tab = Tab == ChestTab.ItemSack ? ChestTab.Hero : ChestTab.ItemSack;
            Cursor = 0;
            Action = 0;
            PendingCombine = -1;
            Focus = ChestFocus.Tabs;
        }
    }
}
