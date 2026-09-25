# F5 — Chest Grid Scrolling — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every stack in the sack — up to 200 — can be reached and seen: the chest grid scrolls to keep the cursor's row on screen, the item menu always sits on the item it names, and X (and Y) never act on a cell that is not drawn. Solo, couch and shop.

**Architecture:** The scroll state is one number in `ChestNavigation` — `TopRow`, the first grid row the screen draws — kept by `KeepCursorInView`, which moves the view only when the cursor would leave the drawn rows and never past the sack's last row. `ChestLayout` learns how many rows are drawn (`Rows`; zero = every row, which is how every layout built before F5 still reads). The screen draws cell *n* as visible item `TopRow × Columns + n`, anchors the popover to that cell, refuses X and Y when `IsCursorDrawn` is false, and shows a thin scroll bar beside the grid when the sack has more rows than fit. The logic lives in the navigation model, not the drawing, so M9's chest restyle redraws the grid without redoing it.

**Tech Stack:** Unity 6.5 (6000.5.8f1), uGUI, NUnit EditMode + PlayMode suites, the Unity MCP bridge.

**Source of scope:** the orchestrator's brief (2026-09-25), from G13's quality review (finding 3): the sack holds 200 stacks (`SackRules.Default`) but the grid draws 40 cells and never scrolls, so past the 40th stack the cursor walks into cells that are not drawn, `GridAnchor` clamps the popover onto cell 39 while it names another item, and — X selling instantly, with no undo — a player can sell a stack they cannot see.

**Written against:** `main` at `5abd671` (after F3). Re-read every file before editing it; the line numbers below are from that commit.

---

## Before you start

- [ ] `editor_status` answers; `editor_stop` if in play mode. Check no scene is dirty before any synchronous `run_tests` (a save prompt wedges the bridge).
- [ ] Baseline: EditMode **689**, PlayMode **34** (F3's gates).
- [ ] After every PlayMode run, delete `Assets/InitTestScene*.unity` and their `.meta`s.
- [ ] Write C# with the Write/Edit tools; keep each file's line endings (the chest files are CRLF in the working tree, LF in git).

**Single fixture:** `run_tests` with `filter_type: testName`, `filter: <FixtureName>`.

**Commit convention:** one commit per task, subject `F5.<n>: <what>`. In the team setup the sim holder sends `DONE` per task and the orchestrator commits (`docs/team/PROTOCOL.md`).

---

## File map

- Modify `Assets/_BattleBomb/UI/Chest/ChestNavigation.cs` — `ChestLayout.Rows`; `ChestNavigation.TopRow`, `KeepCursorInView`, `IsCursorDrawn`; `Move` keeps the view (F5.1).
- Modify `Assets/_BattleBomb/Tests/EditMode/ChestNavigationTests.cs` — five scroll cases (F5.1).
- Modify `Assets/_BattleBomb/UI/Chest/ChestScreen.cs` — `Layout()` passes the drawn rows; X and Y act only on a drawn cursor (F5.2).
- Modify `Assets/_BattleBomb/UI/Chest/ChestScreen.Visuals.cs` — the repaint keeps the view, draws from `TopRow`, anchors the popover to the drawn cell, and draws the scroll bar (F5.2).
- Modify `Assets/_BattleBomb/Tests/PlayMode/LootLoopSmokeTests.cs` — one end-to-end case at 45 stacks (F5.2).

---

### Task F5.1: The view follows the cursor

The model only. Nothing draws from `TopRow` yet, so this task changes no behaviour on screen and is safe to commit on its own.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/ChestNavigation.cs` (`ChestLayout`, `Move`, new members)
- Test: `Assets/_BattleBomb/Tests/EditMode/ChestNavigationTests.cs`

- [ ] **Step 1: Write the failing tests**

In `ChestNavigationTests.cs`, add beside the `Shop(...)` helper:

```csharp
        /// <summary>A sack with more stacks than the five drawn rows of eight hold (F5).</summary>
        private static ChestLayout Scrolling(int visible) =>
            new ChestLayout(visible, 8, ChestNavigation.FilterCount, 5, 2, 0, false, 0, false, rows: 5);
```

and, directly after `Solo_nothing_sits_above_the_filter_row` (one blank line between each):

```csharp
        [Test]
        public void Past_the_last_drawn_row_the_view_follows_the_cursor_down()
        {
            var nav = new ChestNavigation();
            for (int i = 0; i < 4; i++)
            {
                nav.Move(0, -1, Scrolling(45));
            }

            Assert.That(nav.Cursor, Is.EqualTo(32));
            Assert.That(nav.TopRow, Is.Zero, "The view moved while the cursor was still on screen.");

            nav.Move(0, -1, Scrolling(45));
            Assert.That(nav.Cursor, Is.EqualTo(40));
            Assert.That(nav.TopRow, Is.EqualTo(1), "The cursor left the drawn rows and the view stayed put.");
            Assert.That(nav.IsCursorDrawn(Scrolling(45)), Is.True);
        }

        [Test]
        public void Back_up_the_view_follows_only_once_the_cursor_reaches_its_top()
        {
            var nav = new ChestNavigation();
            for (int i = 0; i < 5; i++)
            {
                nav.Move(0, -1, Scrolling(45));
            }

            nav.Move(0, 1, Scrolling(45));
            Assert.That(nav.TopRow, Is.EqualTo(1), "The view moved while the cursor was still on screen.");

            for (int i = 0; i < 4; i++)
            {
                nav.Move(0, 1, Scrolling(45));
            }

            Assert.That(nav.Cursor, Is.Zero);
            Assert.That(nav.TopRow, Is.Zero);
        }

        [Test]
        public void A_full_sack_scrolls_to_its_last_row_and_no_further()
        {
            var nav = new ChestNavigation();
            nav.SelectCell(199, 200);
            nav.KeepCursorInView(Scrolling(200));

            Assert.That(nav.TopRow, Is.EqualTo(20), "25 rows of eight with five drawn: the last view starts at row 20.");
            Assert.That(nav.IsCursorDrawn(Scrolling(200)), Is.True);
        }

        [Test]
        public void A_sack_that_shrinks_under_the_cursor_pulls_the_view_back()
        {
            var nav = new ChestNavigation();
            nav.SelectCell(199, 200);
            nav.KeepCursorInView(Scrolling(200));

            nav.ClampCursor(45);
            Assert.That(nav.IsCursorDrawn(Scrolling(45)), Is.False,
                "Until the view catches up, the cursor's new cell is not on screen, so X must not act.");

            nav.KeepCursorInView(Scrolling(45));
            Assert.That(nav.Cursor, Is.EqualTo(44));
            Assert.That(nav.TopRow, Is.EqualTo(1));
            Assert.That(nav.IsCursorDrawn(Scrolling(45)), Is.True);
        }

        [Test]
        public void Without_a_row_limit_nothing_scrolls()
        {
            var nav = new ChestNavigation();
            for (int i = 0; i < 5; i++)
            {
                nav.Move(0, -1, Layout(visible: 45));
            }

            Assert.That(nav.Cursor, Is.EqualTo(40));
            Assert.That(nav.TopRow, Is.Zero, "A layout that names no row limit draws every row.");
            Assert.That(nav.IsCursorDrawn(Layout(visible: 45)), Is.True);
        }
```

- [ ] **Step 2: Watch them fail** — `recompile`: compile errors (`ChestLayout` has no `rows` parameter; `ChestNavigation` has no `TopRow`, `KeepCursorInView`, `IsCursorDrawn`).

- [ ] **Step 3: The layout says how many rows are drawn**

In `ChestNavigation.cs`, `ChestLayout`: add, after `HeroBeside`'s field:

```csharp
        /// <summary>How many grid rows the screen draws at once; past them the grid scrolls (F5).
        /// Zero draws every row, which is how every layout built before F5 still reads.</summary>
        public readonly int Rows;
```

change the nine-parameter constructor's signature to end `bool heroBeside = false, int rows = 0)`, and add to its body:

```csharp
            Rows = Mathf.Max(0, rows);
```

(the six-parameter constructor chains to it unchanged, so it reads as zero).

- [ ] **Step 4: The model keeps the view**

In `ChestNavigation`, after `StatCursor`'s property:

```csharp
        /// <summary>
        /// The first grid row the screen draws (F5). The sack holds up to 200 stacks and the grid
        /// draws five rows of eight, so the view scrolls to keep the cursor's row on screen. Held
        /// here rather than in the drawing, so a restyled grid redraws without redoing it.
        /// </summary>
        public int TopRow { get; private set; }
```

Rename the existing `public void Move(int dx, int dy, in ChestLayout layout)` to `private void Step(int dx, int dy, in ChestLayout layout)` — body unchanged — and put in its place:

```csharp
        public void Move(int dx, int dy, in ChestLayout layout)
        {
            Step(dx, dy, layout);
            KeepCursorInView(layout);
        }
```

and, after `ClampCursor`:

```csharp
        /// <summary>
        /// Scrolls the fewest rows that bring the cursor's row on screen, and never past the
        /// sack's last row, so the view only moves when the cursor would leave it. The screen also
        /// calls this on every repaint, for the cursor moves it makes itself: a combine's jump, a
        /// reroll landing at the end of the bag, a sack that shrank under the cursor.
        /// </summary>
        public void KeepCursorInView(in ChestLayout layout)
        {
            if (layout.Rows <= 0)
            {
                TopRow = 0;
                return;
            }

            int row = Cursor / layout.Columns;
            if (row < TopRow)
            {
                TopRow = row;
            }
            else if (row >= TopRow + layout.Rows)
            {
                TopRow = row - layout.Rows + 1;
            }

            int filledRows = (layout.VisibleCount + layout.Columns - 1) / layout.Columns;
            TopRow = Mathf.Clamp(TopRow, 0, Mathf.Max(0, filledRows - layout.Rows));
        }

        /// <summary>Whether the cursor's cell is one the screen draws. X sells instantly with no
        /// undo, so X and Y act on nothing else (F5).</summary>
        public bool IsCursorDrawn(in ChestLayout layout)
        {
            if (layout.Rows <= 0)
            {
                return true;
            }

            int row = Cursor / layout.Columns;
            return row >= TopRow && row < TopRow + layout.Rows;
        }
```

- [ ] **Step 5: Run them** — `recompile`; `run_tests` EditMode `ChestNavigationTests`: all pass (the existing 29 + 5 = 34). Full EditMode: **694**.

- [ ] **Step 6: Commit** — subject `F5.1: the chest's view follows the cursor`. Body: the sack holds 200 stacks and the grid draws 40; the navigation model now owns which rows are drawn and scrolls them only when the cursor would leave, and says whether the cursor's cell is on screen. Nothing draws from it until F5.2.

---

### Task F5.2: The grid draws the view, and nothing acts off it

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.cs` (`Layout`, `RunOption`, `RunLock`)
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.Visuals.cs` (`Refresh`, `BuildGrid`, `LayOutGrid`, `RefreshSack`, `GridAnchor`, new `RefreshScrollBar`)
- Test: `Assets/_BattleBomb/Tests/PlayMode/LootLoopSmokeTests.cs`

- [ ] **Step 1: Write the failing test**

In `LootLoopSmokeTests.cs` add `using System.Reflection;`, and after `X_sells_and_Y_locks_the_item_under_the_cursor` (one blank line before):

```csharp
        /// <summary>
        /// F5: past the fortieth stack the grid scrolls. The cursor's item is drawn — in the last
        /// row, and only there — its menu opens on it, and X sells exactly it. Before F5 the cursor
        /// walked into cells that were never drawn and the menu opened on cell 39.
        /// </summary>
        [UnityTest]
        public IEnumerator Past_the_fifth_row_the_grid_scrolls_and_every_verb_lands_on_what_is_drawn()
        {
            for (int i = 0; _bag.Inventory.Items.Count < 45 && i < 90; i++)
            {
                _bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 1.5f + i * 0.05f));
            }

            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(45), "The rolls did not land as 45 separate stacks.");

            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            for (int row = 0; row < 5; row++)
            {
                yield return Push(Vector2.down);
            }

            Component screen = OpenChestScreen();
            Assert.That(CursorCells(screen), Is.EqualTo(new[] { "Cell 4x0" }),
                "Five rows down, the cursor's item must be drawn in the grid's last row, and only there.");

            ItemInstance underCursor = _bag.Inventory.Items[40].Item;

            yield return Press(CommandButtons.Confirm);
            RectTransform popover = Named(screen, "Popover", "GridArea");
            RectTransform cell = Named(screen, "Cell 4x0", "Grid");
            Assert.That(popover.anchoredPosition.y,
                Is.EqualTo(cell.anchoredPosition.y - cell.sizeDelta.y - 8f).Within(0.5f),
                "The item's menu did not open under the item it names.");

            yield return Press(CommandButtons.Back);
            yield return Press(CommandButtons.Option);
            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(44), "X sold nothing.");
            Assert.That(Holds(_bag.Inventory.Items, underCursor), Is.False,
                "X sold something other than the item drawn under the cursor.");
        }
```

and with the helpers:

```csharp
        /// <summary>One push of the stick: a single menu step, released well before the repeat.</summary>
        private IEnumerator Push(Vector2 direction)
        {
            _input.Set(direction, CommandButtons.None);
            yield return SimulationFrames(3);
            _input.Release();
            yield return SimulationFrames(2);
        }

        private static Component OpenChestScreen()
        {
            System.Type type = typeof(ChestScreenHost).Assembly.GetType("BattleBomb.UI.Chest.ChestScreen");
            Object[] found = Object.FindObjectsByType(type, FindObjectsInactive.Exclude);
            Assert.That(found, Has.Length.EqualTo(1), "Expected exactly one open chest screen.");
            return (Component)found[0];
        }

        /// <summary>The grid cells drawing the cursor's ring — where the screen itself shows the
        /// cursor. The screen is internal to the UI assembly, so this reads it by reflection.</summary>
        private static List<string> CursorCells(Component screen)
        {
            const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            var cells = (IList)screen.GetType().GetField("_cells", Hidden).GetValue(screen);
            var names = new List<string>();
            foreach (object cell in cells)
            {
                var ring = (Image)cell.GetType().GetField("_focusRing", Hidden).GetValue(cell);
                if (ring.enabled)
                {
                    names.Add(((RectTransform)cell.GetType().GetProperty("Root", Hidden).GetValue(cell)).name);
                }
            }

            return names;
        }

        private static RectTransform Named(Component screen, string name, string parent)
        {
            foreach (RectTransform rect in screen.GetComponentsInChildren<RectTransform>(false))
            {
                if (rect.name == name && rect.parent != null && rect.parent.name == parent)
                {
                    return rect;
                }
            }

            Assert.Fail($"No active '{name}' under '{parent}' on the chest screen.");
            return null;
        }

        private static bool Holds(IReadOnlyList<ItemStack> items, ItemInstance item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Item.Equals(item))
                {
                    return true;
                }
            }

            return false;
        }
```

- [ ] **Step 2: Watch it fail** — `recompile`; `run_tests` PlayMode `async_tests: true`, `filter_type: testName`, `filter: LootLoopSmokeTests`. **Expected:** the new case fails at the first assertion — `Expected: < "Cell 4x0" > But was: <empty>` — because today no drawn cell holds cursor index 40. Every other case passes. Delete `Assets/InitTestScene*`.

- [ ] **Step 3: The screen tells the model how many rows it draws, and keeps the view**

`ChestScreen.cs`, `Layout()` (l.149) passes the drawn rows:

```csharp
        private ChestLayout Layout() => new ChestLayout(
            _visible.Count, Columns, FilterNames.Length, _menu.Count, _upgradeTargets.Count,
            _stock.Count, IsShop, JunkRankCeiling, HeroBeside, GridRows);
```

`ChestScreen.Visuals.cs`, `Refresh()` (l.403): directly after `CollectVisible();` add

```csharp
            _nav.KeepCursorInView(Layout());
```

- [ ] **Step 4: The grid draws from the view**

`RefreshSack`, the cell loop (l.640–664) becomes:

```csharp
            float cellSize = LayOutGrid();
            int first = _nav.TopRow * Columns;
            for (int cell = 0; cell < _cells.Count; cell++)
            {
                int index = first + cell;
                if (index >= _visible.Count)
                {
                    _cells[cell].SetEmpty();
                    continue;
                }

                ItemStack stack = items[_visible[index]];
                bool selected = index == _nav.Cursor && _nav.Focus == ChestFocus.Grid;
                bool pending = _visible[index] == _nav.PendingCombine;

                _cells[cell].Set(
                    stack.Item,
                    stack.Count,
                    Host != null ? Host.IconFor(stack.Item.DefinitionId) : null,
                    selected,
                    // Worn gear lives in the loadout, never in the sack, so no sack cell can
                    // carry the design's "W" badge. The cell keeps the capability for the
                    // hero doll and the shop rack, which do compare against worn pieces.
                    worn: false,
                    pending,
                    cellSize);
            }

            RefreshScrollBar();
```

`GridAnchor` (l.735) becomes:

```csharp
        /// <summary>Where the cursor's cell is drawn: the view has scrolled
        /// <see cref="ChestNavigation.TopRow"/> rows past the sack's first (F5).</summary>
        private Vector2 GridAnchor()
        {
            int cell = Mathf.Clamp(_nav.Cursor - _nav.TopRow * Columns, 0, Mathf.Max(0, _cells.Count - 1));
            return _cells.Count > 0 ? _cells[cell].AnchoredPosition : Vector2.zero;
        }
```

- [ ] **Step 5: A bar that says there is more**

With the other grid fields in `ChestScreen.Visuals.cs` (beside `_filterRow`):

```csharp
        private Image _scrollTrack;
        private Image _scrollThumb;

        private const float ScrollBarWidth = 4f;
```

At the end of `BuildGrid`:

```csharp
            // How far down the sack the view is (F5). Drawn only when the sack has more rows than
            // the grid shows — a player who cannot see the rest has to be told it is there.
            _scrollTrack = UiBuild.Box("ScrollTrack", _gridRoot, UiBuild.Well);
            _scrollThumb = UiBuild.Box("ScrollThumb", _scrollTrack.rectTransform, UiBuild.Brass);
            _scrollTrack.gameObject.SetActive(false);
```

In `LayOutGrid`, after `float left = …;`:

```csharp
            float blockHeight = GridRows * size + (GridRows - 1) * gap;
            UiBuild.Pin(_scrollTrack.rectTransform, left + blockWidth + gap, 0f, ScrollBarWidth, blockHeight);
```

And after `GridAnchor`:

```csharp
        /// <summary>The thumb's length is the share of the sack's rows on screen; its offset, how
        /// far the view has scrolled.</summary>
        private void RefreshScrollBar()
        {
            int rows = (_visible.Count + Columns - 1) / Columns;
            bool scrolls = rows > GridRows;
            _scrollTrack.gameObject.SetActive(scrolls);
            if (!scrolls)
            {
                return;
            }

            float track = _scrollTrack.rectTransform.rect.height;
            UiBuild.Pin(_scrollThumb.rectTransform, 0f, track * _nav.TopRow / rows,
                ScrollBarWidth, track * GridRows / rows);
        }
```

(`UiBuild.Pin(rect, x, y, width, height)` pins top-left, `y` measured down — as the filter chips use it.)

- [ ] **Step 6: X and Y act only on what is drawn**

`ChestScreen.cs`, `RunOption` (l.513–519): replace

```csharp
            CollectVisible();
            if (_visible.Count == 0)
            {
                return;
            }
```

with

```csharp
            CollectVisible();

            // Only ever the item drawn under the cursor (F5): X sells instantly with no undo, so a
            // cursor the view has not caught up with sells nothing.
            if (_visible.Count == 0 || !_nav.IsCursorDrawn(Layout()))
            {
                return;
            }
```

`RunLock` (l.543–547): the guard becomes

```csharp
            CollectVisible();
            if (_visible.Count > 0 && _nav.IsCursorDrawn(Layout()))
            {
                ToggleLockAt(_visible[_nav.Cursor]);
            }
```

- [ ] **Step 7: Run them** — `recompile`, `console` `level: error` clean. PlayMode `LootLoopSmokeTests`: all pass, the new case included. Full EditMode **694**, full PlayMode **35**. Delete `Assets/InitTestScene*`.

- [ ] **Step 8: Live check (settled states — drive it yourself)**

Play mode in `Gameplay.unity` opened on its own (two players, so the chest is split). By `eval`: give player one 45 stacks (`bag.Take(driver.RollDebugItem(7, 1.5f + i * 0.05f))`), place them at the chest (`CharacterActor.PlaceAt`, internal — reflection) and `driver.OpenScreen(...)`. Drive the model by reflection (`_nav.Move(0, -1, Layout())`, then `Refresh()`) and sample after each step: `TopRow`, the cells with the focus ring, the `ScrollThumb`'s rect. Expect: rows 0–4 → `TopRow` 0, no bar change; row 5 → `TopRow` 1, ring on `Cell 4x0`, thumb in the lower part of the track. Open the menu (`Confirm()`): the popover under `Cell 4x0`. `capture_game_view source: screen`. Then 200 stacks: `SelectCell(199)` + `Refresh()` → `TopRow` 20, ring on the last row, thumb at the track's bottom; capture. Repeat solo (deactivate player two before opening, as G13b did) and at a shopkeeper in Sell mode if the fixture stage has one. `editor_stop`.

- [ ] **Step 9: Commit** — subject `F5.2: the chest grid scrolls, and nothing acts off-screen`. Body: past the fortieth stack the cursor walked into cells that were never drawn, the item menu opened on cell 39, and X — instant, no undo — could sell a stack nobody could see; the grid now draws from the navigation model's view, the menu anchors to the drawn cell, X and Y refuse an undrawn cursor, and a bar beside the grid shows there is more.

**Michael's checklist gains:** with 45 or more stacks, push down past the fifth row: the grid scrolls one row at a time, the bar beside it shows where you are, the menu opens on the item, and X sells exactly the item under the cursor.

---

## Not done in F5

- **Mouse wheel and touch drag.** The chest has no pointer selection at all yet (pointer support is M13 — ROADMAP §9). A wheel that scrolled the view without moving the cursor would leave the cursor off-screen, where F5's rule makes X and Y refuse — workable, but whether the view detaches from the cursor or drags it along is a design call that belongs with M13's pointer work, not a free extra here.
- **A "rows 6–10 of 25" readout.** The bar says there is more and roughly where you are; the sack meter already says how many stacks there are.

## Room left for Michael's item 12 (not built — his call after the controller pass)

*"When a partner's sale shifts the list, the cursor follows its item, and X briefly ignores presses after that."*

- **Where it slots in:** `ChestScreen.OnBagChanged` (l.332), which every change to the shared sack already reaches — including a partner's sale — before it repaints. Remember the `ItemInstance` drawn under the cursor at each repaint; on a change, after `CollectVisible`, find that value's new index in `_visible` (items have no unique id; value equality still picks the right stack — potions stack by value, and gear, which never stacks, is told apart by its affix array's reference — correction from F5.2's review) and `_nav.SelectCell(...)` onto it. `KeepCursorInView` in the repaint then scrolls to it — which is why it builds on F5.2. If the item under the cursor was the one sold, the cursor stays where it is, and — in both cases — `RunOption` refuses X for ~0.4 s of unscaled time after a change it did not cause (a timestamp field beside `_flash`), with a short flash such as "The sack moved."
- **Size:** one small task — ~30–40 lines in `ChestScreen.cs`, plus one PlayMode case (partner sells an item before the cursor: the cursor keeps its item; X inside the window sells nothing, X after it sells the right one). About a third of F5.

---

## Self-review (done while writing)

- **Brief coverage.** Every stack reachable and seen: `KeepCursorInView` scrolls to any cursor 0–199 (`A_full_sack…`), the grid draws from `TopRow`, and the bar says more exists. The popover sits on the item it names: `GridAnchor` uses the drawn cell (PlayMode assertion). X never acts on an undrawn cell: `IsCursorDrawn` guard in `RunOption` and `RunLock` (EditMode `A_sack_that_shrinks…` shows the state it guards against). Solo, couch and shop: one grid code path, five rows in every layout (`GridRows`), live-checked in all three. Scroll state in `ChestNavigation`, not the drawing; no new framework, no ScrollRect. Wheel/touch listed as not done; item 12 placed and sized.
- **Behaviour kept.** Below 41 stacks nothing changes: `TopRow` stays 0, the bar stays hidden, the cell loop reads the same indices. Layouts built with the six-parameter `ChestLayout` constructor (every existing test) read `Rows` 0 and never scroll.
- **Improves in passing.** A combine's reroll lands at the end of the bag and the cursor follows it (`RecollectOnto`); past 40 stacks that result was invisible before, and now the view follows it.
- **Type consistency.** `ChestLayout.Rows` (ctor param `rows`); `ChestNavigation.{TopRow, KeepCursorInView(in ChestLayout), IsCursorDrawn(in ChestLayout)}`; `Move` → private `Step`; screen `RefreshScrollBar`, `_scrollTrack`, `_scrollThumb`, `ScrollBarWidth`; tests `Scrolling(int)`, `Push`, `OpenChestScreen`, `CursorCells`, `Named`, `Holds`.
