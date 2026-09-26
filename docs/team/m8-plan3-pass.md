# M8 Plan 3 — Michael's online checks

Plan 3 does two things:

1. **The guest's own hero answers at once** instead of a round trip late. That is "prediction": in Plan 1's lag table, Bad lag felt "too late" to play, and this is the fix.
2. **Online co-op goes onto Steam,** so the collaborator can play from their own home.

There are three sittings, each at a different point in the build:
- **Part E**, about 20 minutes, two windows on your PC. When the Builder finishes prediction (Task 108).
- **Part F**, about 30 minutes, you and the collaborator in your two homes. The first real internet game (Task 113).
- **Part G**, about an hour. The close-out: the whole chapter from two homes on BattleBomb's own Steam app, then the couch on your own (Task 114).

Tell the orchestrator before each one, so it calls quiet on the other sessions. Tick what works; for anything that doesn't, one sentence on what you saw is enough.

---

## Part E — Does the guest's hero answer at once? (two windows, one PC)

**Keyboard only**, as before. Click into the window you want to drive. **WASD** move, **Space** jump, **J** attack, **K** heavy (hold to charge), **L** magic, **Esc** settings.

> **96b:** in the two-window setup, the arrow keys in one window can move the other window's hero. Use **WASD**, not the arrows, in both windows. *(The Builder replaces this line with the fix if 96b was fixed.)*

**Setting up** (as in Plan 1 and Plan 2):
1. Unity open. **Window → Multiplayer → Multiplayer Play Mode**, tick **Player 2**.
2. Open the **Frontend** scene and press **Play**.
3. In both windows' bottom-right **Net** panel, pick the **Lag**. Start with **Normal**.
4. Main window: **Host local**. Guest window: **Join local**.
5. Both pick a hero and ready at character select, then launch from the main window.

The guest window's Net panel has a new button: **Prediction: on / off**. It starts **on**.

- [ ] **E1. Moving, Normal lag.** In the guest window, run left and right, stop, turn. The guest's hero starts, stops and turns the moment you press, like the host's does in its own window. Then press **Prediction: on** so it reads **off**, and do the same: it should feel like Plan 1 again (a little late). Switch it back **on**.
- [ ] **E2. Attacking, Normal lag.** In the guest window: **J J J** (a chain), a charged **K**, an **L**. Each swing starts the moment you press. The enemy's reaction and the damage number can come a moment later; that part is the host's and is expected.
- [ ] **E3. Jumping.** Jump, jump and attack in the air, jump off a ledge. No hitch at take-off or landing.
- [ ] **E4. Getting hit.** Let an enemy hit the guest's hero. It staggers and is knocked back a moment after the hit. It may **slide** a little to where the host says it is. It never **snaps** or teleports.
- [ ] **E5. Light by a drop, a chest, a partner.**
  - Stand the guest's hero on a dropped item and press **J**: it picks the item up with no swing first.
  - Do the same at a chest: it opens with no swing first.
  - If the host's hero is down, mash **J** beside it as fast as you can: the revive fills with every tap, with no swings.
- [ ] **E6. Bad lag.** Leave (Net panel **Leave** in both windows), set **Lag: Bad** in both, then host, join and launch again. Repeat E1 and E2 with prediction **on**. Then with it **off**, for the comparison.
- [ ] **E7. A connection hiccup — the guest.** Hold **K** (charging) in the guest window and click **Go quiet 4 s** in the guest window's Net panel. On the main window, the guest's hero lets go of the charge after about a quarter of a second and the heavy swings. *That is by design: a silent guest's hands let go.* When the four seconds are up, the guest's hero carries on normally, with no burst of speed.
- [ ] **E8. A connection hiccup — the host.** Click **Go quiet 4 s** in the **main** window's Net panel. The guest window's world freezes for the four seconds. When it comes back it moves smoothly straight away: at most a moment's extra stillness, and no stutters for the next half-second.

**The lag table, again, with prediction on.** For each, "fine", "noticeable but OK", or "too late":

| Lag | Moving | Attacking |
|---|---|---|
| None | | |
| Normal | | |
| Bad | | |
| Bad, prediction **off** (for comparison) | | |

When you're done: **Stop**, then untick Player 2.

---

## Part F — The first game from two homes (you and the collaborator)

You each need Steam running and logged in, and the zip the Builder made. Both follow the sheet inside it (`HOW-TO-RUN.txt`); you are the **host**, the collaborator is the **guest**.

**Keyboard or controller**, whichever you each like: each PC is its own player now. It can help to be on a voice call together.

- [ ] **F1. Joining from the friends list.** You start a solo game. The collaborator, at their game's title, opens Steam's friends list, finds you, and chooses **Join Game**. Their game shows the lobby (pick a hero, ready). They then appear in your world at the next checkpoint room, never in the middle of a fight.
- [ ] **F2. Joining by Steam ID** (the fallback). The collaborator leaves (Net panel **Leave**), types the number shown in your Net panel's Steam line into their **Join** box, and presses **Join**. Same result as F1.
- [ ] **F3. Play together.** Clear an arena and walk into the next checkpoint room together. Both of you: how does your own hero feel (moving, attacking)? "Fine", "noticeable but OK", or "too late".
- [ ] **F4. Chests on your own screens.** Each opens a chest. It opens full-screen on that person's PC only, and the other's game keeps running.
- [ ] **F5. Hits both ways.** The guest hits an enemy: the enemy reacts on both screens. An enemy hits the guest: the guest's health goes down on both screens.
- [ ] **F6. Leaving and coming back.** The guest presses **Esc → Leave the game**. Your screen says *Player 2 left* and you play on alone. The guest joins again (F1 or F2) and appears at the next checkpoint room.
- [ ] **F7. Pulling the plug.** The guest closes their game window outright (Alt+F4). Your screen says *Player 2 left* within a few seconds, and you play on.
- [ ] **F8. Invite from the overlay** (if the overlay opens for you — it may not in a game started outside Steam, which is fine). In your game press **Shift+Tab**, right-click the collaborator, **Invite to Game**. They accept in Steam's chat and join as in F1.

Anything that didn't work: say which home saw it.

---

## Part G — The close-out

This happens after you have created BattleBomb's own app on Steamworks (your queue item 6). The Builder then swaps it in, builds the zip again, and the collaborator has the app in their Steam library.

### G1. The whole fixture chapter from two homes

Start to finish, together. Tick each from **both** homes, and note if one home saw something the other didn't.

- [ ] **G1a. Fight** — every arena, both of you hitting and being hit.
- [ ] **G1b. Revive** — one of you goes down, the other revives them (mash **J**, or **X** on a controller, beside them).
- [ ] **G1c. Grab** — each picks up a drop; each person's things go into their own sack.
- [ ] **G1d. Chest** — each opens a chest, equips something, sells something.
- [ ] **G1e. Shop** — each buys something from a shopkeeper; the coin goes down by the shown price.
- [ ] **G1f. Wipe** — both go down. You both come back at the checkpoint room.
- [ ] **G1g. Results** — at the chapter's end both see the results. The guest's says *Waiting for the host*; your **A** / **Space** takes you both back.
- [ ] **G1h. Saves** — afterwards each of you starts a solo game. The guest's hero kept the level, gear and coin earned with you, and the next difficulty tier is open for them.
- [ ] **G1i. Steam Cloud** (the collaborator). Delete the game's local `saves` folder (the Builder will say where), start the game again: the hero's progress comes back from the cloud.

### G2. The couch, exactly as before (your PC, a controller and the keyboard)

- [ ] **G2a.** A two-player couch run of the fixture chapter: everything works as it did before online co-op. The chest doesn't pause the couch; a revive, the results, and the save all work.
- [ ] **G2b.** A solo run: opening the chest pauses the world, as it always has.
- [ ] **G2c.** With Steam open during the couch run, the collaborator's Steam friends list shows **no** "Join Game" for you: couch games are full.

---

**Known and expected — don't report these:**
- In Part E, enemies' reactions to the guest's hits still arrive a moment later; only the guest's own hero is predicted.
- While Steam runs on Spacewar (Parts E and F), Steam says you are "playing Spacewar". That is Valve's test app, and expected until Part G.
- Windows may warn about an unknown publisher the first time the zip's game starts: **More info → Run anyway**.
- The banner, the lobby and the Net panel are plain placeholder text. M9 restyles them.

## Verdicts

*(The orchestrator records them here, in HANDOFF-M8's build log, and on the board.)*
