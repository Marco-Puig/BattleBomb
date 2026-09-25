# M8 Plan 1 — Michael's online checks

Online play is tested with two copies of the game on this one PC. The second copy is a separate
Unity window the sessions cannot click in, so these checks are yours. Each section is added when
its task lands; the orchestrator batches them so you do as few sittings as possible. Tell the
orchestrator before you start, so it calls quiet on the other sessions.

Tick what works; for anything that doesn't, a sentence on what you saw is enough.

## Stage A — the remote controller (Task 90, 2026-09-25) · about 5 minutes

Unity must be open with the project.

- [ ] **1.** Open **Window → Multiplayer → Multiplayer Play Mode** and tick **Player 2**. The first
      time, a second Unity window takes about half a minute to appear. Leave it open.
- [ ] **2.** Open the **Frontend** scene and press **Play**. Both windows start the game.
- [ ] **3.** In the main window's bottom-right **Net** panel, click **Host local**. It reads
      "Hosting — waiting for a guest".
- [ ] **4.** In the Player 2 window's Net panel, click **Join local**. The main window's panel
      reads "A guest joined"; Player 2's reads "Joined — waiting for the host to launch".
- [ ] **5.** In the main window, start a game as usual (title, character select, chapter select)
      and launch. The Player 2 window loads the game too.
- [ ] **6.** **Keyboard only** for this — a controller drives both windows at once. Click into the
      Player 2 window and move with WASD: Player 2 moves in the main window, and the main
      window's own player stands still. Jump once (Space): Player 2 jumps once, not twice.
- [ ] **7.** Click back into the main window and move: only Player 1 moves.
- [ ] **8.** Press **Stop**, then untick Player 2 in the Multiplayer Play Mode window.

**Known and expected — don't report these:**
- The Player 2 window doesn't draw the host's world yet (Stage B, Tasks 91–94).
- Opening the settings menu in the Player 2 window can make Player 2 move or jump in the main
  window (being fixed in Task 95).
- Player 2 plays the host's own hero until the lobby (Plan 2).

## Verdicts

*(The orchestrator records them here and on the board.)*
