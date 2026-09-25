# Lane: World

**Mission:** settle the **Castle Crashers-thin story (D56)** in one World session with Michael and
his collaborator, and write it down as a one-page story bible. The art waits on it — heroes,
regions, enemy families, and bosses cannot be drafted until they are named.

**Owns:** `docs/world/` · this file.

**Read first (in order):** `CLAUDE.md` · `docs/team/PROTOCOL.md` · `docs/team/BOARD.md` · this file
· `docs/DECISIONS.md` D56, D48, D46, D22, D23, D41 · `docs/GAME_DESIGN.md` §3, §4, §6, §9 ·
`docs/ROADMAP.md` §5.1.

---

## The one rule

**You invent no lore.** No names, characters, places, factions, or plot of your own — not even as
placeholders or "for example". Michael and his collaborator decide everything; you ask, organise,
and record. You may frame a question with *structural* choices ("is the goal something you rescue,
collect, or reach?"), never with content.

## The session

One question at a time, in this order. Plain language. Read each answer back before moving on.

1. **The goal** — what pulls the players through the game, moved toward and never defended (D48).
2. **Setting and tone.**
3. **The four heroes** — for each of Fire, Ice, Earth, Air (D46): name, look, a one-line
   personality, and why they wield that element.
4. **Chapter 1** — the region; its climate element (§4); its enemy family (2–3 elemental skins of
   the four archetypes, D22); its boss — the idea, and how you dodge it by jumping and by moving in
   depth (§6); the boss's signature drop (D23).
5. **Chapter 2** — the same, with a different climate so element choice matters.
6. **The shopkeeper.**
7. **Endless** — one line on why it exists in the world.
8. **Three design calls** that sit beside the story (ROADMAP §5.1): do the four heroes play
   identically apart from their element; which elemental reaction pairs exist (D41, D46); are pets
   beyond stat pets in Early Access.

The collaborator may not be in the room — Michael can relay. It can take more than one sitting;
record where you stopped in **Current state** so a later session (or you, after compaction) picks
up at the right question.

## The output

`docs/world/STORY_BIBLE.md` — one page, headed by the goal, then setting and tone, the four heroes,
each chapter, the shopkeeper, Endless. Only what was decided; anything left open is listed as open,
not filled in.

The three design calls go to the orchestrator as draft `D` entries — it records them in
`DECISIONS.md`.

When the bible is done, send `DONE`. The orchestrator tells the Art lane.

---

## Question list

Asked one at a time, in this order. Each answer is read back before moving on. Options offered
are *structural* only; every name, place, creature, and plot point comes from Michael and the
collaborator. Anything they leave undecided is recorded as **open**, never filled in.

**Q1 — The goal.** What pulls the players forward through the whole game? (Michael's own framing
from M7: the "princesses-and-crystals question".) Must be moved toward, never defended (D48).
Structural shapes to offer: something you rescue, something you collect, somewhere you reach,
someone you chase or stop. Follow-up if needed: does it resolve at the end of the story, or keep
going (the game never caps, D24)?

**Q2 — Setting and tone.**
- 2a. Where does it take place, broadly? (Note: `GAME_DESIGN` §9 records that the 2019 build's
  scene names hint at an earlier premise; it is *their* material, not canon. Ask whether any of
  it is kept, changed, or dropped — don't lead with it.)
- 2b. Tone — how serious or silly, and in what way? Castle Crashers-thin means almost no
  dialogue (D56), so tone lives in the look and the situations.

**Q3 — The four heroes** — one hero at a time, Fire → Ice → Earth → Air (D46). For each:
- name · look · one-line personality · why they wield that element.
- Background the answer must fit: the signature cast belongs to the *element*, not the hero
  (D46) — every hero with the element shares it. Heroes differ in look, animation, effects, and
  small tuning. Billboarded 2D sprites, Castle Crashers-style (D47).

**Q4 — Chapter 1.**
- 4a. The region — what and where is it?
- 4b. Its climate — which element(s) does it strengthen, which does it weaken? (Climate is a
  per-element multiplier and helps enemies and players alike, D41.)
- 4c. Its enemy family — 2–3 creature types, each filling one of the four archetypes (D22):
  melee grunt (fights in your lane), ranged (shoots across lanes), caster (slow, elemental,
  applies its status), brute (slow, telegraphed). Which element do they carry?
- 4d. Its boss — the idea; one attack you dodge by **jumping**, one you dodge by **moving in
  depth** (changing lane) (§6).
- 4e. The boss's signature drop (D23) — what is it, and which kind of item: weapon, armour, pet,
  or equipment?

**Q5 — Chapter 2.** Same as Q4, with a *different* climate so element choice matters.

**Q6 — The shopkeeper.** Who they are and their look; one recurring character or different per
region? (Shopkeepers sit mostly in checkpoint rooms, D43.)

**Q7 — Endless.** One line on why it exists in the world.

**Q8 — Three design calls** (ROADMAP §5.1) → sent to the orchestrator as draft `D` entries, not
written into the bible:
- 8a. Do the four heroes play identically apart from their element? (Today: same kit, differing
  in animation, effects, and small tuning — §3. A hero needing its own script is ruled out by
  §3's implementation rule.)
- 8b. Which elemental reaction pairs exist (D41, D46)? Today: Fire marks Burn, Ice marks Chill;
  Earth and Air mark nothing (their hits stun / launch). The table ships empty until they pick.
- 8c. Are pets more than stat pets in Early Access? (Needed by M12.)

**Q9 — Names the game is waiting on** (optional, last; approved by the orchestrator 2026-09-24):
- 9a. The currency's name (D43 — unnamed "until the story names it").
- 9b. The chest's in-world name (D42 — "the chest" is a working name).
- 9c. Does "Chill" stay as the Ice status's name (D46 — working name)?
Anything left undecided is recorded as open. Any that land go to the orchestrator, who records
them as amendments to those D entries.

## Waiting on

- Nothing blocking. The session runs as far as Michael can take it; the collaborator's calls may
  come relayed.

## Current state

Session opened 2026-09-24. **At Q1 (the goal)** — asked, awaiting answer.

## Next steps

1. Record Q1's answer, read it back, move to Q2.
2. Carry on down the list; update this file after every answered question.
3. When the list is done (or a sitting ends), write `docs/world/STORY_BIBLE.md` from what was
   decided, open items marked open.

## Answers and decisions

- 2026-09-24 — **Orchestrator:** question list approved; the three tail names become optional Q9.
- 2026-09-24 — **Orchestrator:** session names are unreliable (Michael renames them). Message it
  at the `from=` address of its last message, not by name — PROTOCOL rule 6 (853ad08). Last known
  address: `uds:\\.\pipe\LOCAL\cc-msg-a5467c87452d77bccc60c704d3ef83a9` (session shown as
  "Battlebomb"). If that fails, ListAgents and look for the row that "says it was" the orchestrator.

## Log

- 2026-09-24 — Orchestrator ANSWER: go ahead; tail names added as Q9.
- 2026-09-24 — ONLINE. "BattleBomb Planning" no longer resolves; orchestrator is "BattleBomb
  Orchestrator" in ListAgents (told it). Question list drafted. Session opened at Q1.
- 2026-09-24 — Lane seeded by the orchestrator.
