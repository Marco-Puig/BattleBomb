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

- **Michael + Marco (the collaborator):** Q2b — tone — asked 2026-09-26, unanswered. They are
  now working on the story outside this session (the drafts below), so the next thing may be a
  chosen draft rather than a Q2b answer.

## Story drafts — candidates, NOT decisions

`docs/world/drafts/` appeared 2026-09-26, **not written by this lane**:
- `00-README.md` + `01`–`05`: five full-story drafts written by another session at Michael's
  request; the README says Michael relaxed D56's no-lore rule **for that drafting task**. It does
  **not** relax it for the World lane — this lane still invents nothing.
- `06`–`10` (`06_THE_STOLEN_SUN.md` … `10_THE_DRAGON_IN_THE_TREASURE_CHEST.md`): a different
  format ("Candidate 6" …), not covered by the README; an empty `docs/world/story_candidates_openai/`
  sits beside them, so they likely came from another tool. Provenance unconfirmed.
- **How they enter the bible:** only what Michael and Marco say they adopt ("draft 3, but …"). Record
  their choice verbatim in `SESSION_NOTES.md`, then treat the adopted parts as their answers. Never
  fill a gap from a draft on my own; anything they haven't adopted stays open.
- The README reads the knights as lowest-rank Knight Squad, the slime colours as alien aura (not
  elements), "mini bosses" as chapter bosses and aura monsters as elites — those are the drafts'
  **assumptions**, not answers. The carry-forward questions below still stand until Michael/Marco
  settle them.

## Current state

Session opened 2026-09-24. On 2026-09-26 Michael supplied the **prelude** (their opening, saved
verbatim in `docs/world/PRELUDE_NOTES.md` — "none of this is final"). It covers most of Q2a
(setting) and frames Q1 without answering it. Q1 answered 2026-09-26 (verbatim in
`docs/world/SESSION_NOTES.md`; summary under Answers). Artifact: **open**. Q2a taken as answered
by the prelude (the "does it ever leave this land?" question folds into the chapter regions).
**At Q2b — tone**, asked, unanswered. Michael takes each question to Marco, so
answers come back relayed. Every answer is saved verbatim to `SESSION_NOTES.md` as well as
summarised here. 2026-09-26 evening: Michael wrapping up; he and Marco working on the story
elsewhere (drafts). Everything on disk handed in as DONE; READY TO COMPACT sent.

**On resume:** re-read PROTOCOL, BOARD, this file. Wait for `COMMITTED` before touching the DONE
paths. Then, when Michael returns: if they picked or blended a draft, record it (see "Story
drafts" above) and re-ask only what the choice leaves open, one question at a time; otherwise
re-ask Q2b.

## Next steps

1. Get Q2b (tone) — or record the draft Michael and Marco choose, and what it settles.
2. Q3, the four heroes, one at a time (Fire → Ice → Earth → Air); then the prelude characters'
   one-line looks. Fold in the carry-forward questions below where they belong; update this file
   and `SESSION_NOTES.md` after every answered question.
3. When the list is done (or a sitting ends), write `docs/world/STORY_BIBLE.md` from what was
   decided, open items marked open.

## Carry-forward questions (raised by the prelude — ask at the point shown)

- **Q1 follow-ups:** the artifact — **open** (Michael, 2026-09-26). Also open: who the final fight is against; is "the big alien boss" the alien that broke into
  the castle; what happens after the artifact and the injured king are found ("we continue on some
  more" — nothing planned). Don't push these; the bible lists them open.
- **Q3:** the heroes are "low level knights" (Q1) — confirm whether that makes them Knight Squad
  members.
- **Q4/Q5 — progression Michael described (Q1):** amped-up enemies around the crash site and
  village; venturing out, mostly *regular* unbuffed monsters (learning how they fight); nearer
  "mini bosses", monsters with a **glowing alien aura**. Ask: are "mini bosses" the chapter bosses
  or something smaller? Are glowing-aura monsters the same thing as elites (D22 — today "visibly
  armoured") or separate? If separate, that's a design call → orchestrator, don't design it.
- **Where the artifact and the injured king are found** — which chapter, or beyond Chapter 2?
  Ask during the chapter questions; open is fine.
- **Q2a leftover → ask with the chapter regions (Q4a/Q5a):** does the story stay in this fantasy
  land, or go elsewhere? (There is now a spaceship; the 2019 build had space scenes — theirs to
  keep or drop, don't lead.)
- **Q2b:** tone — the Shrek reference and the buff-king reveal point somewhere, but confirm, don't
  assume.
- **Q3:** are the four heroes members of the Knight Squad? Is the tutorial played as the chosen
  hero(es)?
- **Q4c:** do the mutated slimes' glowing colours (red, purple, blue) mean elements? Are slimes
  Chapter 1's family, one type in it, or a thread through the whole game? (The gray tutorial enemy
  "doesn't have to be slime".)
- **Prelude characters (D56 amendment — the bible lists each with a one-line look, as far as
  Michael and Marco give them):** the king (name? look beyond "super buff, gold legendary sword"?),
  the alien (name? one or many? a boss?), the Knight Squad, villagers, the carriage. Ask after the
  heroes (Q3), before Chapter 1.
- ~~The prelude's shape~~ — settled by the D56 amendment: village (tutorial) → castle (minion
  fight) → outside into Chapter 1, built in M10.

## Answers and decisions

- 2026-09-26 — **Michael + collaborator (tentative, "none of this is final"):** the prelude —
  fantasy kingdom ringed by villages; the Knight Squad helps villagers; tutorial vs gray slimes;
  celebration at the castle; something falls from the sky; an alien breaks in; the king (secretly
  super buff, gold legendary sword) fights it outside; heroes clear the alien's minions; king and
  alien gone, crashed spaceship left, slimes now mutated (bigger, glowing red/purple/blue) → into
  Chapter 1. "Who won?" is the question carried through the game; the game is an explore-gear-level
  grind toward a big final fight. Full text: `docs/world/PRELUDE_NOTES.md`.
- 2026-09-26 — **Scope flag sent to the orchestrator:** the prelude implies story sequences
  (D56 rules out a cutscene system), an opening crawl, tutorial tooltips, and characters beyond
  D56's list (king, alien, Knight Squad, villagers) for Art.
- 2026-09-26 — **Orchestrator ACK:** it has taken the five points to Michael with a
  recommendation. World keeps recording the story only — **do not design the presentation**; the
  orchestrator will send the outcome so the bible can say how the beats are shown. Send lane file
  + `docs/world/` as DONE at stopping points.
- 2026-09-26 — **Michael, via the orchestrator — D56 amended (4d1a6de):** story beats are shown
  **mostly through gameplay** — the player keeps control; a beat is a scripted stage event (shake,
  wall breaks, characters walk in, act, leave). Still no cutscene or dialogue system. Opening crawl
  + tutorial prompts come with the prelude. The prelude is built in **M10** as the game's opening
  and starts the Next Fest demo. New characters (king, alien, Knight Squad, villagers, carriage)
  appear as stage characters/props. **For the bible:** list the prelude's characters with a
  one-line look each (as far as Michael and Marco give them); note beats as things that happen on
  stage; design no mechanics (M10's job).
- 2026-09-26 — The collaborator is **Marco** (per the orchestrator).
- 2026-09-26 — **Q1, Michael + Marco:** the heroes are **low-level knights** whose everyday duty
  is protecting the castle and nearby village from ordinary, not-a-big-deal monsters. After the
  prelude they go out on that duty; passing the alien ship they learn **the aliens are after some
  sort of artifact**. Clearing the amped-up enemies in the village, they learn **the aliens buff
  ordinary monsters**. So they venture out **to figure out what the aliens want**. Along the way:
  ordinary unbuffed monsters first (learning how they fight), glowing-alien-aura monsters nearer
  the mini bosses. Eventually they **find the artifact, and the king badly injured by the big
  alien boss** — and "continue on some more". Beyond that: nothing planned (open). Recorded
  reading: "defend" is the knights' duty in the story, not a gameplay mode — play still moves
  forward (D48); Michael to correct if wrong.
- 2026-09-26 — **Q1 follow-up, Michael:** what the artifact is and why the aliens want it —
  **open**.
- 2026-09-26 — **Q2a:** taken as answered by the prelude — a Shrek "far, far away" fantasy land;
  mythical creatures, vibrant forests, varied biomes; a central kingdom ringed by villages.

- 2026-09-24 — **Orchestrator:** question list approved; the three tail names become optional Q9.
- 2026-09-24 — **Orchestrator:** session names are unreliable (Michael renames them). Message it
  at the `from=` address of its last message, not by name — PROTOCOL rule 6 (853ad08). Last known
  address went stale when it restarted (2026-09-26); reached it by name as **"Battlebomb"**. If
  that fails, ListAgents and look for the row that "says it was" the orchestrator.

## Log

- 2026-09-26 — Orchestrator asked for a DONE of everything on disk (Michael wrapping up; Marco to
  read the drafts on GitHub). Found `docs/world/drafts/` (not mine), recorded its provenance
  above; DONE sent, then READY TO COMPACT.
- 2026-09-26 — Artifact: open. Q2a taken from the prelude. Asked Q2b (tone).
- 2026-09-26 — Q1 answered; saved verbatim to `docs/world/SESSION_NOTES.md`; read back; asked
  the artifact follow-up.
- 2026-09-26 — COMMITTED 3bbdcd7 (lane file + prelude notes). D56 amended (4d1a6de) with
  Michael's presentation answers; recorded above.
- 2026-09-26 — Orchestrator ACK on the scope flag; DONE sent for the lane file + prelude notes.
- 2026-09-26 — Michael gave the prelude; saved to `docs/world/PRELUDE_NOTES.md`; read back; Q1
  re-asked narrowly; scope flag sent to the orchestrator.
- 2026-09-24 — Orchestrator ANSWER: go ahead; tail names added as Q9.
- 2026-09-24 — ONLINE. "BattleBomb Planning" no longer resolves; orchestrator is "BattleBomb
  Orchestrator" in ListAgents (told it). Question list drafted. Session opened at Q1.
- 2026-09-24 — Lane seeded by the orchestrator.
