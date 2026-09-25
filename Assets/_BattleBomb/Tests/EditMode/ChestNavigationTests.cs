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

        /// <summary>Solo at a chest: the hero panel is drawn beside the sack, so the cursor can
        /// walk between them.</summary>
        private static ChestLayout Solo(int visible = 12) =>
            new ChestLayout(
                visible, 8, ChestNavigation.FilterCount, 5, 2, 0, false, 0, heroBeside: true);

        /// <summary>Standing at a shopkeeper: the counter has two sides and the sweep exists.</summary>
        private static ChestLayout Shop(int visible = 12, int stock = 4, int junkRanks = 4) =>
            new ChestLayout(
                visible, 8, ChestNavigation.FilterCount, 5, 2, stock, true, junkRanks);

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
        public void Off_the_bottom_of_the_sack_reaches_the_sweep_only_at_a_shop()
        {
            var chest = new ChestNavigation();
            chest.Move(0, -1, Layout(visible: 4, columns: 8));
            Assert.That(chest.Focus, Is.EqualTo(ChestFocus.Grid), "A chest has nothing under the sack.");

            var shop = new ChestNavigation();
            shop.Move(0, -1, Shop(visible: 4));
            Assert.That(shop.Focus, Is.EqualTo(ChestFocus.Junk));
        }

        [Test]
        public void The_rack_is_a_vertical_list_that_ends_at_the_sweep()
        {
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop(stock: 3));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Stock));
            Assert.That(nav.StockCursor, Is.Zero);

            nav.Move(0, -1, Shop(stock: 3));
            Assert.That(nav.StockCursor, Is.EqualTo(1));
            nav.Move(0, -1, Shop(stock: 3));
            Assert.That(nav.StockCursor, Is.EqualTo(2), "The rack cursor clamps at the last roll.");

            nav.Move(0, -1, Shop(stock: 3));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Junk), "Under the last roll is the sweep.");

            nav.Move(0, 1, Shop(stock: 3));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Stock));
            Assert.That(nav.StockCursor, Is.EqualTo(2), "Back onto the row it left from.");
        }

        [Test]
        public void The_sweep_threshold_walks_sideways_and_clamps_to_the_ladder()
        {
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop(stock: 0));
            nav.Move(0, -1, Shop(stock: 0));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Junk), "A bare rack drops straight to the sweep.");
            Assert.That(nav.JunkRank, Is.EqualTo(2), "Rusty, as the design has it.");

            for (int i = 0; i < 5; i++)
            {
                nav.Move(1, 0, Shop(junkRanks: 4));
            }

            Assert.That(nav.JunkRank, Is.EqualTo(4), "The threshold stops at the ceiling.");

            for (int i = 0; i < 8; i++)
            {
                nav.Move(-1, 0, Shop(junkRanks: 4));
            }

            Assert.That(nav.JunkRank, Is.EqualTo(ChestNavigation.MinJunkRank));
        }

        [Test]
        public void Confirming_on_the_sweep_asks_the_screen_to_run_it()
        {
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop(stock: 0));
            nav.Move(0, -1, Shop(stock: 0));

            Assert.That(nav.Confirm(Shop()), Is.EqualTo(ChestOutcome.RunSellJunk));
        }

        [Test]
        public void The_counter_flips_sides_and_a_chest_has_no_counter_to_flip()
        {
            var chest = new ChestNavigation();
            Assert.That(chest.Mode, Is.EqualTo(ShopMode.Sell), "A chest is the sack layout, always.");
            Assert.That(chest.SwitchMode(Layout()), Is.EqualTo(ChestOutcome.None));
            Assert.That(chest.Mode, Is.EqualTo(ShopMode.Sell));

            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Stock));

            Assert.That(nav.SwitchMode(Shop()), Is.EqualTo(ChestOutcome.ModeSwitched));
            Assert.That(nav.Mode, Is.EqualTo(ShopMode.Sell));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "The sack has the cursor now.");
        }

        [Test]
        public void A_shopkeeper_has_no_hero_tab_to_reach()
        {
            // The counter is about the rack, so the screen never offers the doll (Michael,
            // 2026-08-23). Reaching it would show a half the shop screen does not build.
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Sell, Shop());

            nav.Move(0, 1, Shop());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Filters), "Above the grid is still the filters.");

            nav.Move(0, 1, Shop());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Filters), "There is no tab row above them.");
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.ItemSack));

            var chest = new ChestNavigation();
            chest.Move(0, 1, Layout());
            chest.Move(0, 1, Layout());
            Assert.That(
                chest.Focus, Is.EqualTo(ChestFocus.Tabs),
                "A chest still has one — it is the shop that drops it, not the screen.");
        }

        [Test]
        public void The_top_of_the_rack_stays_on_the_rack()
        {
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop(stock: 3));

            nav.Move(0, 1, Shop(stock: 3));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Stock), "Buy mode has nothing above the rack.");
            Assert.That(nav.StockCursor, Is.Zero);
        }

        [Test]
        public void Heavy_backs_out_of_the_sweep_then_closes_the_shop()
        {
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop(stock: 0));
            nav.Move(0, -1, Shop(stock: 0));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Junk));

            Assert.That(nav.Cancel(), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Stock), "The sweep backs out to the rack.");

            Assert.That(
                nav.Cancel(), Is.EqualTo(ChestOutcome.Close),
                "Buy mode draws no grid under the rack, so there is no level left to unwind.");
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
        public void The_hero_tab_opens_on_the_worn_gear_with_the_stats_under_it()
        {
            var nav = new ChestNavigation();
            nav.Move(0, 1, Layout());
            nav.Move(0, 1, Layout());
            nav.Confirm(Layout());
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.Hero));

            nav.Move(0, -1, Layout());
            Assert.That(
                nav.Focus, Is.EqualTo(ChestFocus.Loadout),
                "The hero tab opens on the loadout, which is what a player came here to change.");

            for (int i = 0; i < 4; i++)
            {
                nav.Move(0, -1, Layout());
            }

            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Stats), "Under the gear are the stats.");

            for (int i = 0; i < 6; i++)
            {
                nav.Move(0, -1, Layout());
            }

            Assert.That(
                nav.StatCursor, Is.EqualTo(ChestNavigation.HeroRows - 1), "Four stats: the list clamps.");
            Assert.That(nav.Confirm(Layout()), Is.EqualTo(ChestOutcome.AllocateStat));

            Assert.That(nav.Cursor, Is.Zero, "The bag cursor was never the stat cursor.");
        }

        [Test]
        public void Solo_walks_sideways_between_the_sack_and_the_worn_gear()
        {
            // Before this the hero panel could only be entered through a tab row solo does not
            // draw, so worn gear could not be deepened at all (Michael, 2026-08-23).
            var nav = new ChestNavigation();
            ChestLayout solo = Solo(visible: 12);

            for (int i = 0; i < 7; i++)
            {
                nav.Move(1, 0, solo);
            }

            Assert.That(nav.Cursor, Is.EqualTo(7), "Still inside the row.");

            nav.Move(1, 0, solo);
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Loadout), "Off the right edge is the gear.");

            nav.Move(-1, 0, solo);
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "And left again is the sack.");

            var chest = new ChestNavigation();
            for (int i = 0; i < 9; i++)
            {
                chest.Move(1, 0, Layout());
            }

            Assert.That(
                chest.Focus, Is.EqualTo(ChestFocus.Grid),
                "Behind a tab there is nothing to the right to walk into.");
        }

        [Test]
        public void The_loadout_steps_over_the_hole_in_its_short_column()
        {
            var nav = new ChestNavigation();
            ChestLayout solo = Solo();
            for (int i = 0; i < 8; i++)
            {
                nav.Move(1, 0, solo);
            }

            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Loadout));
            Assert.That(nav.LoadoutCursor, Is.Zero, "The helmet.");

            nav.Move(1, 0, solo);
            Assert.That(nav.LoadoutCursor, Is.EqualTo(3), "Across to the weapon.");

            nav.Move(0, -1, solo);
            Assert.That(nav.LoadoutCursor, Is.EqualTo(4), "Down to the pet.");

            nav.Move(0, -1, solo);
            Assert.That(
                nav.LoadoutCursor, Is.EqualTo(6),
                "The right column has no third row, so the move steps over it to the equipment.");
        }

        [Test]
        public void A_worn_slot_confirms_into_its_own_verb_list_and_backs_out_to_itself()
        {
            var nav = new ChestNavigation();
            ChestLayout solo = Solo();
            for (int i = 0; i < 8; i++)
            {
                nav.Move(1, 0, solo);
            }

            Assert.That(nav.Confirm(solo), Is.EqualTo(ChestOutcome.OpenWornMenu));

            nav.SetOnWorn(true);
            nav.OpenWornMenu();
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu));

            Assert.That(nav.Cancel(), Is.EqualTo(ChestOutcome.None));
            Assert.That(
                nav.Focus, Is.EqualTo(ChestFocus.Loadout),
                "A worn verb list backs out onto the slot it opened from, not into the sack.");
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

            var shop = new ChestNavigation();
            shop.SetMode(ShopMode.Buy, Shop(stock: 1));
            shop.FinishBuy(stockCount: 0);
            Assert.That(
                shop.Focus, Is.EqualTo(ChestFocus.Junk),
                "A bought-out rack leaves the cursor on the only other thing on the panel.");
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

        [Test]
        public void The_shoulders_flip_the_counter_at_a_shop()
        {
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop());

            Assert.That(nav.CycleTab(1, Shop()), Is.EqualTo(ChestOutcome.ModeSwitched));
            Assert.That(nav.Mode, Is.EqualTo(ShopMode.Sell));
            Assert.That(nav.CycleTab(-1, Shop()), Is.EqualTo(ChestOutcome.ModeSwitched));
            Assert.That(nav.Mode, Is.EqualTo(ShopMode.Buy));
        }

        [Test]
        public void The_shoulders_switch_the_tab_in_couch_co_op_and_land_in_its_body()
        {
            var nav = new ChestNavigation();

            Assert.That(nav.CycleTab(1, Layout()), Is.EqualTo(ChestOutcome.TabSwitched));
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.Hero));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Loadout),
                "A shoulder press is a jump to the other half, not a trip to the tab strip.");

            nav.CycleTab(-1, Layout());
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.ItemSack));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));
        }

        [Test]
        public void Solo_the_shoulders_carry_the_cursor_between_the_sack_and_the_gear()
        {
            var nav = new ChestNavigation();

            Assert.That(nav.CycleTab(1, Solo()), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Loadout));
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.ItemSack), "Solo has no tabs to change.");

            nav.CycleTab(1, Solo());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));
        }

        [Test]
        public void The_shoulders_wait_while_a_pick_or_a_menu_is_open()
        {
            var nav = new ChestNavigation();
            nav.BeginCombine(2);
            Assert.That(nav.CycleTab(1, Solo()), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "A combine pick is modal.");

            nav.CancelCombine();
            nav.Confirm(Solo());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu));
            Assert.That(nav.CycleTab(1, Solo()), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu), "An open item menu is answered first.");
        }
    }
}
