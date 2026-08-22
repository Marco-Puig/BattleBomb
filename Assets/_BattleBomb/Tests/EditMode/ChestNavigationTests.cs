using BattleBomb.UI.Chest;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The chest screen's focus machine, pinned (M6 close-out note 3). Every transition the
    /// screen can make lives in <see cref="ChestNavigation"/>, so a route a player cannot
    /// leave — M6's real defect — would fail here before anyone opened the editor.
    /// </summary>
    public sealed class ChestNavigationTests
    {
        private static ChestLayout Layout(
            int visible = 12, int columns = 8, int menu = 5, int upgrades = 2, int stock = 0) =>
            new ChestLayout(visible, columns, ChestNavigation.FilterCount, menu, upgrades, stock);

        [Test]
        public void A_fresh_screen_starts_on_the_grid_of_the_item_sack()
        {
            var nav = new ChestNavigation();

            Assert.That(nav.Tab, Is.EqualTo(ChestTab.ItemSack));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));
            Assert.That(nav.PendingCombine, Is.EqualTo(-1));
        }

        [Test]
        public void Opening_the_menu_needs_a_visible_item()
        {
            var nav = new ChestNavigation();

            Assert.That(nav.Confirm(Layout(visible: 0)), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));

            Assert.That(nav.Confirm(Layout()), Is.EqualTo(ChestOutcome.MenuOpened));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu));
            Assert.That(nav.Action, Is.Zero);
        }

        [Test]
        public void Heavy_backs_out_one_level_at_a_time_and_finally_closes()
        {
            var nav = new ChestNavigation();
            nav.Confirm(Layout());
            nav.OpenUpgrade();
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Upgrade));

            Assert.That(nav.Cancel(), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu), "Upgrade backs out to the menu.");

            Assert.That(nav.Cancel(), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "The menu backs out to the grid.");

            Assert.That(nav.Cancel(), Is.EqualTo(ChestOutcome.Close),
                "With nothing left to back out of, Heavy closes the chest — the M6 pass bug.");
        }

        [Test]
        public void A_pending_combine_is_cancelled_before_anything_else_backs_out()
        {
            var nav = new ChestNavigation();
            nav.Confirm(Layout());
            nav.BeginCombine(3);

            Assert.That(nav.Cancel(), Is.EqualTo(ChestOutcome.CombineCancelled));
            Assert.That(nav.PendingCombine, Is.EqualTo(-1));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu), "Cancelling the combine costs no focus level.");
        }

        [Test]
        public void A_pending_combine_keeps_the_cursor_inside_the_grid()
        {
            var nav = new ChestNavigation();
            nav.BeginCombine(0);

            nav.Move(0, 1, Layout(visible: 2, stock: 4));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid),
                "The filters are not the question while a pick is open, and leaving for them "
                + "would strand the pick behind a screen that no longer mentions it.");

            nav.Move(0, -1, Layout(visible: 2, stock: 4));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "Nor is the shopkeeper's rack.");

            Assert.That(nav.Cancel(), Is.EqualTo(ChestOutcome.CombineCancelled));
            nav.Move(0, 1, Layout(visible: 2, stock: 4));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Filters),
                "With the pick released the grid's edges open again.");
        }

        [Test]
        public void Off_the_top_of_the_grid_is_the_filter_row_then_the_tabs()
        {
            var nav = new ChestNavigation();

            nav.Move(0, 1, Layout());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Filters));

            nav.Move(0, 1, Layout());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Tabs));

            nav.Move(0, -1, Layout());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "Down from the tabs lands on the grid.");
        }

        [Test]
        public void Off_the_bottom_reaches_the_rack_only_when_it_has_stock()
        {
            var nav = new ChestNavigation();
            nav.Move(0, -1, Layout(visible: 4, columns: 8, stock: 0));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "No rack: the cursor stays put.");

            nav.Move(0, -1, Layout(visible: 4, columns: 8, stock: 3));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Stock));
            Assert.That(nav.StockCursor, Is.Zero);

            nav.Move(1, 0, Layout(visible: 4, columns: 8, stock: 3));
            nav.Move(1, 0, Layout(visible: 4, columns: 8, stock: 3));
            nav.Move(1, 0, Layout(visible: 4, columns: 8, stock: 3));
            Assert.That(nav.StockCursor, Is.EqualTo(2), "The rack cursor clamps at the last piece.");
        }

        [Test]
        public void The_grid_cursor_moves_by_a_column_sideways_and_a_row_vertically()
        {
            var nav = new ChestNavigation();
            ChestLayout layout = Layout(visible: 20, columns: 8);

            nav.Move(1, 0, layout);
            nav.Move(1, 0, layout);
            Assert.That(nav.Cursor, Is.EqualTo(2));

            nav.Move(0, -1, layout);
            Assert.That(nav.Cursor, Is.EqualTo(10), "Down is one full row of eight.");

            nav.Move(-1, 0, Layout(visible: 1, columns: 8));
            Assert.That(nav.Cursor, Is.Zero, "Fewer items than the cursor: it clamps to the last one.");
        }

        [Test]
        public void Switching_tab_resets_the_cursor_and_any_combine_in_progress()
        {
            var nav = new ChestNavigation();
            nav.Move(1, 0, Layout());
            nav.Move(0, 1, Layout());
            nav.Move(0, 1, Layout());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Tabs));

            // A pick cannot be opened from the tabs by any route a player has — the grid holds
            // the cursor while one is pending — but the reset stays, so no path can carry a
            // stale bag index into a tab that indexes something else.
            nav.BeginCombine(1);

            Assert.That(nav.Confirm(Layout()), Is.EqualTo(ChestOutcome.TabSwitched));
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.Hero));
            Assert.That(nav.Cursor, Is.Zero);
            Assert.That(nav.PendingCombine, Is.EqualTo(-1));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Tabs));
        }

        [Test]
        public void The_hero_tab_is_one_short_list_whose_confirm_allocates()
        {
            var nav = new ChestNavigation();
            nav.Move(0, 1, Layout());
            nav.Move(0, 1, Layout());
            nav.Confirm(Layout());
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.Hero));

            nav.Move(0, -1, Layout());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));
            nav.Move(0, -1, Layout());
            nav.Move(0, -1, Layout());
            nav.Move(0, -1, Layout());
            nav.Move(0, -1, Layout());
            Assert.That(nav.Cursor, Is.EqualTo(ChestNavigation.HeroRows - 1), "Four stats: the list clamps.");

            Assert.That(nav.Confirm(Layout()), Is.EqualTo(ChestOutcome.AllocateStat));
        }

        [Test]
        public void The_menu_and_the_upgrade_list_wrap_vertically()
        {
            var nav = new ChestNavigation();
            nav.Confirm(Layout(menu: 3));

            nav.Move(0, 1, Layout(menu: 3));
            Assert.That(nav.Action, Is.EqualTo(2), "Up from the first row wraps to the last.");

            nav.OpenUpgrade();
            nav.Move(0, -1, Layout(upgrades: 2));
            nav.Move(0, -1, Layout(upgrades: 2));
            Assert.That(nav.UpgradeCursor, Is.Zero, "Two downs over two rows is back where it started.");
        }

        [Test]
        public void Finishing_a_menu_action_returns_to_a_clamped_grid()
        {
            var nav = new ChestNavigation();
            for (int i = 0; i < 5; i++)
            {
                nav.Move(1, 0, Layout(visible: 6));
            }

            nav.Confirm(Layout(visible: 6));
            nav.FinishMenuAction(visibleCount: 2);

            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));
            Assert.That(nav.Cursor, Is.EqualTo(1), "Selling shrank the sack; the cursor follows.");
        }

        [Test]
        public void An_exhausted_upgrade_leaves_the_panel_and_an_empty_rack_leaves_the_shop()
        {
            var nav = new ChestNavigation();
            nav.Confirm(Layout());
            nav.OpenUpgrade();
            nav.FinishUpgrade(canStillUpgrade: true);
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Upgrade), "Points left: stay and keep spending.");
            nav.FinishUpgrade(canStillUpgrade: false);
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));

            nav.Move(0, -1, Layout(visible: 4, columns: 8, stock: 1));
            nav.FinishBuy(stockCount: 0);
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "Nothing left to buy.");
        }

        [Test]
        public void Filters_cycle_and_reset_the_cursor()
        {
            var nav = new ChestNavigation();
            nav.Move(1, 0, Layout());
            nav.Move(0, 1, Layout());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Filters));

            nav.Move(-1, 0, Layout());
            Assert.That(nav.Filter, Is.EqualTo(ChestNavigation.FilterCount - 1), "Left from 'All' wraps to the end.");
            Assert.That(nav.Cursor, Is.Zero);

            Assert.That(nav.Confirm(Layout()), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "Confirming a filter returns to the grid.");
        }
    }
}
