# M8 Plan 1 — Michael's online checks

Online play is tested with two copies of the game on this one PC. The second copy is a separate
Unity window the sessions cannot click in, so these checks are yours. It is one sitting, about
20 minutes; the automated tests already prove everything that doesn't need eyes or a feel
judgement. Tell the orchestrator before you start, so it calls quiet on the other sessions.

Tick what works; for anything that doesn't, a sentence on what you saw is enough.

**Keyboard only** throughout — a controller drives both windows at once. Click into the window you
want to drive.

## Setting up (each time)

1. Unity open with the project. **Window → Multiplayer → Multiplayer Play Mode**, tick
   **Player 2**. The first time, a second Unity window takes about half a minute to appear.
2. Open the **Frontend** scene and press **Play**. Both windows start the game.
3. Pick the **Lag** setting in both windows' bottom-right **Net** panel (start with **None**).
4. Main window's Net panel: **Host local** — it reads "Hosting — waiting for a guest".
   Player 2 window's Net panel: **Join local** — the main window reads "A guest joined", Player 2's
   reads "Joined — waiting for the host to launch".
5. Start a game in the main window as usual (title, character select, chapter select) and launch.

To change the lag later: **Leave** in both windows, change **Lag**, then step 4 again.
When you're done: **Stop**, then untick Player 2.

## Stage A — the remote controller

- [ ] **A1.** At launch, the main window waits for Player 2, then both start together.
- [ ] **A2.** In the Player 2 window, move with WASD: Player 2 moves in the main window, and the
      main window's own player stands still. Jump once (Space): Player 2 jumps once, not twice.
- [ ] **A3.** Click into the main window and move: only Player 1 moves.
- [ ] **A4.** In the Player 2 window, open the settings menu (**Esc**), move and press Space inside
      it, then close it with Esc **while still holding Space**: Player 2 in the main window doesn't
      move or jump. Let go of Space and press it again: Player 2 jumps.

## Stage B — the mirror

- [ ] **B1.** The Player 2 window shows the game: both heroes, the stage, the enemies.
- [ ] **B2.** Drive Player 1 in the main window and watch the Player 2 window: Player 1 moves
      smoothly there, about a tenth of a second behind, with no stutter.
- [ ] **B3.** Fight: the hit spark and damage number appear in the Player 2 window at the moment of
      the hit, on the right target. Elite enemies are tinted the same in both windows.
- [ ] **B4.** Get knocked down, or wipe on purpose: the respawn snaps into place rather than
      sliding across the arena.
- [ ] **B5.** Kill enemies until something drops; grab it with either player: it vanishes in both
      windows, and drops show the glow and bounce in both. (The guest can't open chests yet —
      Plan 2.)
- [ ] **B6.** Walk both players to the end of the first stage: the second stage streams in on both,
      its geometry lines up, the exit doesn't open until both windows have it, and the hand-over
      looks clean in the Player 2 window — no pop, no double stage.

## The lag table (Task 96) — the answer Plan 3 is written from

Drive **Player 2 from its own window** — being the guest. There is a small delay between your press
and your hero moving *in that window*; that's what Plan 3's prediction removes. For now, judge how
big it feels. Do it once for each lag setting (Leave, change Lag, host and join again).

| Lag | Moving feels | Attacking feels |
|---|---|---|
| None | fine / noticeable but OK / too late | fine / noticeable but OK / too late |
| Normal (100 ms) | | |
| Bad (200 ms, 2 % loss) | | |

At **Bad**, repeat B2 and B6 once too: smooth, and the stage hand-over still clean.

**Known and expected — don't report these:**
- Player 2 plays the host's own hero until the lobby (Plan 2).
- A drop's bounce in the Player 2 window may start slightly higher (cosmetic).
- To test a guest rejoining, relaunch the run from the title rather than re-hosting mid-run
  (a mid-run rejoin can stall the airlock until Plan 2).

## Verdicts

*(The orchestrator records them here, in HANDOFF-M8's build log, and on the board.)*

**Michael, 2026-09-26 — Stages A and B pass**, A1–A4 and B1–B6, including B2 and B6 at Bad.

| Lag | Moving feels | Attacking feels |
|---|---|---|
| None | fine | fine |
| Normal (100 ms) | noticeable but OK | noticeable but OK |
| Bad (200 ms, 2 % loss) | too late | too late |

So Plan 3's prediction is a must for the guest's own hero, not a nicety. At 200 ms, without it,
the game doesn't feel playable.

**Two bugs found:**
- **96a — hosting at the title.** Host local / Join local clicked *before* Continue on the title
  screen: the game starts with the wrong players or controls. Continue first in both windows, then
  host and join: fine. (The dev panel is meant to be used at the title, and Plan 2's pass D1 does
  exactly that.)
- **96b — the arrow keys cross windows.** In the two-window test, the arrow keys in one window
  moved the other window's player. It can't happen between two real PCs, but it can spoil any
  two-window check, so its cause gets found (the test rig, or the game).
