# Groundwork pass — Michael's checklist

Written by the Builder at G14 (2026-09-25); item 16 added with F5. Everything here moves fast,
so it's checked by eye in real time, not by the sessions. Play in Unity (Frontend scene, Play).
The orchestrator calls QUIET ON first so nothing recompiles under you.

Tick what works; for anything that doesn't, a sentence on what you saw is enough.

## Solo

- [ ] **1. Keyboard.** Title → play. Open a chest: the footer shows key caps
      (WASD Move · Enter Actions · J Sell · K Lock · Q/E Hero · Esc Leave). Esc backs out one level
      at a time and closes the chest from the top. Esc in the world opens settings; Esc closes
      them. Closing the chest with Esc does **not** open settings.
- [ ] **2. Switching device mid-menu.** Pick up a controller with a menu open: the footer switches
      to A/B/X/Y the moment you press anything. Put it down, press a key: it switches back.
- [ ] **3. X and Y.** X sells the item under the cursor instantly — the whole stack. Y locks it.
      X on a locked item refuses. Y again releases it.
- [ ] **4. Shoulders.** At a chest, LB/RB jump between your sack and your gear. At the shopkeeper
      they flip Buy/Sell, and X at the rack does nothing (A buys). Start leaves the screen from
      anywhere, even inside an item's menu.
- [ ] **5. Mid-combine.** Pick Combine on an item with duplicates. X combines the whole pile and
      no menu is left open. B cancels.
- [ ] **6. Held stick.** Walk into the chest holding right and tap X to open it: the cursor
      doesn't jump.
- [ ] **9. Start on the title only.** At character select and chapter select, Start does nothing.
- [ ] **10. Label.** Solo, the health bar reads P1 — also after going back to chapters and
      relaunching.
- [ ] **13. Results.** Finish a stage mashing A. The results stay up until you let go and press A
      again, and never less than about ¾ s. Start and Esc do nothing there.
- [ ] **15. Title.** The bottom row shows the button badges, in the game's fonts.
- [ ] **16. A big sack (F5).** With 45 or more stacks (the settings menu's DEBUG grant row adds
      loot), push down past the fifth row: the grid scrolls one row at a time, the bar beside it
      shows where you are, the menu opens on the item, and X sells exactly the item under the
      cursor.

## Couch (two players)

- [ ] **7. Two controllers.** Start the title with pad 1. At character select press A on pad 2:
      Player 2 joins. In game each pad moves only its own character. B on pad 2 at character
      select leaves.
- [ ] **8. Keyboard + controller.** Same as 7 with the keyboard as Player 1. The join line says
      "press A to join" (Enter would ready Player 1).
- [ ] **11. Couch chest.**
  - the filter chips sit clear of the first row of items
  - the footer stays visible on the Hero tab
  - the SACK/HERO strip stays up on the Hero tab and lights the tab the cursor is on
  - a bottom-row item's menu shows Sell and Lock in full
  - A on a worn piece on the Hero tab opens its menu, and its upgrade list is fully visible
- [ ] **14. Controller sleep.** Mid-run, switch Player 2's controller off and on: it still
      controls Player 2.

## Your call, not pass/fail

- **12. Shared sack.** If your partner sells while you browse, the items shift, and a different
  item can slide under your cursor just before you press X. Instant sell has no undo.
  *Recommendation:* if this bothers you, the cursor follows its item when the list changes, and X
  ignores presses for a moment after your partner changes what's under it. It would ride F5 (the
  chest grid scrolling fix), which already reworks that cursor.

## Verdict

*(The orchestrator records it here, in ROADMAP §4, CLAUDE.md's progress table, and memory.)*

**Michael, 2026-09-26 — pass.** Everything checked works.
- **Item 7 (two controllers) is untested.** There is one controller here. It carries to the
  collaborator's pass, or whenever a second pad is to hand; PlayMode already covers the
  two-seat logic.
- **Item 12 wasn't judged** (it needs a partner selling). It stays as it is, and Michael can
  raise it after couch play with the collaborator.
- **The keyboard has no visual indication, where the controllers get their badges.** This goes
  to M9's HUD work: keyboard key art on a par with the pad badges, plus a way to see the controls.
