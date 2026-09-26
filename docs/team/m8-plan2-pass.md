# M8 Plan 2 — Michael's online checks

Plan 2 makes online co-op a whole game: the guest's own menus, the guest's own save, a lobby at
character select, dropping in at a checkpoint room, and leaving. As with Plan 1, the second copy of
the game is a Unity window the sessions cannot click in, so these checks are yours. It is one
sitting, about 30 minutes; the automated tests already prove everything that doesn't need eyes or a
feel judgement. Tell the orchestrator before you start, so it calls quiet on the other sessions.

Tick what works; for anything that doesn't, a sentence on what you saw is enough.

**Keyboard only** throughout — a controller drives both windows at once. Click into the window you
want to drive. The keys: **WASD** move, **Space** jump and confirm, **J** attack / open a chest /
sell in a menu, **K** heavy / lock in a menu, **Esc** settings and back, **Q** and **E** switch tabs.

"The main window" is the host. "The guest window" is Player 2's.

## Setting up (each time)

1. Unity open with the project. **Window → Multiplayer → Multiplayer Play Mode**, tick
   **Player 2**. A second Unity window appears (about half a minute the first time).
2. Open the **Frontend** scene and press **Play**. Both windows start the game.
3. Pick the **Lag** setting in both windows' bottom-right **Net** panel (start with **None**).
4. For the lobby checks (C and D1): main window's Net panel **Host local**; guest window's
   **Join local**. The guest window plays from a save of its own, so its autosaves never land in
   yours.
5. For the drop-in checks (D2 onward): instead of **Host local**, click **Friends: off** in the main
   window so it reads **Friends: local socket** — from then on every solo game you start there is
   open to the guest window, the way a Steam friend will find it.

When you're done: **Stop**, then untick Player 2.

## Stage C — the guest's menus and saves

Start with step 4 of the setup, both players ready at character select (C1 below shows how), and
launch from the main window. Walk both heroes to the first checkpoint room.

- [ ] **C1. The guest's chest.** First give the guest something to sort: in the guest window press
      **Esc**, choose the **[ DEBUG ] grant** row with **Space**, then **Esc** out. Walk the guest to
      the chest and press **J**: the chest opens **in the guest window only**, full screen — and the
      main window's game keeps running (move the host around to see). It shows the guest's own things
      (what the grant gave), not the host's.
- [ ] **C2. Actions take a moment.** In the guest's chest: sell something (**J** on it), lock
      something (**K**), equip something, and combine two of the same thing if you have them (the
      grant gave pairs). Each shows after a very short pause, and nothing happens twice if you press
      quickly.
- [ ] **C3. The shop.** If the room has a shopkeeper: the guest window shows four pieces for sale;
      buy one — the coin goes down by the shown price and the piece is in the guest's sack. Leave
      and come back: a new rack.
- [ ] **C4. Nothing pauses.** In the main window open the settings (**Esc**): the guest window's
      game keeps running and the guest can still move; the host's hero stands still while the menu
      is up. Open the host's chest: the same.
- [ ] **C5. The camera.** Each window, when its own chest is open, moves its camera so its own
      hero sits in the free half of the screen.
- [ ] **C6. The guest's settings.** In the guest window press **Esc**: toggle auto-sell — it
      changes a moment later. The row that says **Leave the game** (the host's says *Return to
      chapter select*) — leave it alone for now; D3 uses it.
- [ ] **C7. Results.** Play to the end of the chapter together: both windows show the results. The
      guest's says *Waiting for the host* and its Space does nothing; the host's Space takes **both**
      windows back — the main window to chapter select with the guest still in Player 2's slot, the
      guest window to its lobby, still ready.
- [ ] **C8. The guest keeps what they earned.** In the guest window's Net panel press **Leave**,
      then start a solo game there (it plays from its own save): the guest's hero has the level,
      gear and coin it earned with you, and chapter select offers the next difficulty tier — which
      only finishing the chapter unlocks. Start a solo game in the main window too: your own sack
      has none of the guest's things.

## Stage D — joining and leaving

- [ ] **D1. The lobby.** Host local / Join local, both at the title. Main window: go to character
      select — Player 2's slot shows the guest as *online*. Guest window: it shows *ONLINE — PLAYER
      2*; pick a hero with the **A** and **D** keys and ready with **Space**. The main window can't move on to the
      chapters until the guest is ready, and shows the guest's hero. Guest: **Esc** un-readies; a
      second **Esc** leaves.
- [ ] **D1b. A full couch.** This one needs a controller: with the keyboard as Player 1 in the main
      window, plug in a pad and press its **A** at character select so a second player joins there
      (the pad presses in both windows — harmless here). Then click **Join local** in the guest
      window: it is turned away, and both windows' Net panels say why (*The game is full*).
- [ ] **D2. Dropping in.** Main window: **Friends: local socket**, then start a solo game and fight
      in the first arena. Guest window: **Join local**, pick, ready. The guest window says *The host is
      playing. You will join at the next checkpoint room.* Walk the host into the checkpoint room:
      the guest window loads, and the guest appears in that room — never in the arena, never
      before its window has finished loading.
- [ ] **D2b. The host walks on.** Repeat D2, but keep walking the host out of the room while the
      guest window loads: the guest window shows the host's world with *Waiting for the host to reach
      a checkpoint room*, and the guest appears at the **next** room.
- [ ] **D3. The guest leaves.** With both playing: guest window **Esc → Leave the game**. The main
      window says *Player 2 left*, the guest's hero is gone, and the host plays on alone — opening
      the chest pauses the game again, as solo always has. The guest window is at its own front door.
      From the guest window, **Join local** and ready again: the guest drops in at the next room.
- [ ] **D4. A connection going quiet.** With both playing, click **Go quiet 4 s** in the guest
      window's Net panel: after about a second the main window shows *Connection problem…*; when the
      four seconds are up the banner goes and play carries on. Now click **Go quiet 12 s**: at ten
      seconds the main window says *Player 2 left* and the host plays on alone; when the guest window
      wakes it finds the host gone and goes back to its title.
- [ ] **D4b. Pulling the plug — the guest.** Drop the guest in again, then press **Stop** in the
      guest window only: the main window says *Player 2 left* at once and the host plays on alone.
- [ ] **D5. Pulling the plug — the host.** With both playing, **Leave** in the main window's Net
      panel (or Stop it): the guest window goes to its title, which says *The host left.*
- [ ] **D6. At Bad lag.** Leave, set **Lag: Bad** in both windows, and repeat C2 and D2 once: the
      menus take a little longer to answer but never do something twice or lose a press; dropping
      in still lands in the room.

**Known and expected — don't report these:**
- The guest's own running and jumping still show a moment late in the guest window — Plan 3's
  prediction removes that (you judged how much in Plan 1's lag table).
- While a guest is waiting to appear, it can't open its settings (its hero isn't in the world yet);
  the Net panel's **Leave** is the way out.
- The lobby, the banner and the *The host left* line are plain placeholder text; M9 restyles them.
- Outside the editor nothing opens to friends yet — that is Plan 3's Steam lobby.

## Verdicts

*(The orchestrator records them here, in HANDOFF-M8's build log, and on the board.)*
