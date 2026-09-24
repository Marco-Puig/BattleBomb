# Team board

Kept by the orchestrator (currently named "Battlebomb"). The live state of the team: who is doing what,
who holds the sim, what is waiting on whom. Rules: `docs/team/PROTOCOL.md`.

**Updated:** 2026-09-24

---

## Lanes

| Lane | Session | Status | Working on | Waiting on |
|---|---|---|---|---|
| Builder | Builder | **BLOCKED** | Reading the Groundwork plan (no writes) | Michael opening Unity with the MCP bridge |
| Netcode | Netcode | online | M8 Plan 1 (stages A+B) — spec approved | — |
| World | World | online | Drafting the question list, then the session at question 1 | Michael (+ collaborator) for the session |
| Art | Art | online | Audio v0 (icons, effects and held weapons briefed) | — |
| Producer | — | starts at M9 | — | — |

*Session* is the name each lane shows in `ListAgents`. Michael renames sessions freely, so replies
go to the address a message came from.

## The sim

**Holder:** Builder (default) · **Queue:** empty

## Quiet

**Off**

## Michael's queue

What is waiting on Michael, in priority order:

1. **Open the BattleBomb project in Unity 6000.5.8f1 with the MCP bridge** — the Builder is
   blocked on it (the bridge stubs are up; no editor is running).
2. **The World session** — open in the World lane's session, at question 1 (the goal).
3. *(Small decision)* **Two couch players on the same hero** share one character save (audit §7.2)
   — the second overwrites the first. Stop equal picks at character select, or save per seat?
4. *(Before M8's end)* **The collaborator's availability** for the two-PC pass — they are the remote
   tester; there is no second PC (D59, ROADMAP §4 M8).
5. *(At M8's close-out)* **Create the Steamworks account and app ID** — start Valve's paperwork a few
   days ahead; it can take that long.
6. *(Whenever there is a gap)* **Review the art bible v0** — `docs/art/ART_BIBLE.md`, in the Art
   lane's session. Two proposals ride with it: Fire's colour `#FF7326` → `#FF4A1C` (it nearly matches
   Legendary's loot orange), and unlit painted-cel sprites instead of normal maps for M9.

## M8 — ready to build after Groundwork

Spec approved by Michael: `docs/HANDOFF-M8.md`. Built in three plans, each written while the one
before is underway: **Plan 1** = stages A+B, **Plan 2** = C+D, **Plan 3** = E+F and the close-out.
**Starts only after** Groundwork is merged **and** the Builder backlog below is done. M8 will ask
for **quiet** during two-editor Multiplayer Play Mode runs.

## Builder backlog — after Groundwork, before M8

Found by the Netcode audit (`docs/team/netcode/readiness.md`), checked against the code by the
orchestrator. Each is a small plan task with a test; the Builder takes them as one batch.

1. **Destroyed enemies keep acting for the rest of the frame** (§4.3 item 1). `Destroy` lands at
   frame end, and the driver can run up to 5 steps a frame, so below 60 fps enemies removed by a
   wipe, a launch, or an airlock keep stepping and hitting; corpses keep pushing bodies
   (`SeparateBodies` has no depleted check). Unregister or deactivate immediately.
2. **The debug grant row ships in release builds** (§5.2). Put it behind the same `#if` as the
   tier-overlay row.
3. **Every run replays the same loot** (§7.1). The three seeds are constants 1/2/3. Seed from the
   session at launch — *orchestrator's call, flagged to Michael*.

Into M8's plan (Netcode designs them; they are online blockers, not bugs today):
- The shop rack is rolled, stored, and priced in the UI, and `RequestBuy` trusts the UI's item and
  price (§5.1) — a rule 2 violation; the rack moves into the simulation.

## M9 backlog — for whoever builds the art pipeline

From the Art lane's provenance spec (`docs/art/PROVENANCE.md` §§8, 10):
- **Four `.cs` files are still in `Art/Low_Swordman/Demo/Scripts/`** — the August trap that took the
  editor down once; the M9 enforcement test fails on them. Remove them (after a GUID reference sweep).
- **Delete `Art/AI/maincharactertemp.ai`** — a byte-identical copy of
  `Sprites/Characters/Templar Knight/AI/Templar Knight.ai`, sitting in a folder whose name now means
  AI-generated (D55).
- **Delete `Art/World/Props/`** (Door, Door_Alt, Asteroid) — a third-party pack with no known name
  or licence, referenced by nothing. Confirm with a GUID reference sweep before deleting.

From the Art lane's bible (`docs/art/ART_BIBLE.md` §§3, 5) — both wait on Michael's sign-off:
- **Fire's element colour** `#FF7326` → `#FF4A1C` in `Data/Elements/Fire.asset`, so it no longer
  reads as Legendary loot.
- **M9's rig session confirms on the Unity side** (`docs/art/HERO_TEMPLATE.md` §7, proposals not
  decisions): Sprite Resolver label swaps keyed in clips on rigid parts (2D Animation 15.1), pivots
  set by an importer preset, one Sorting Group per character, sorted by depth.
- **M9's effects start from** `docs/art/EFFECTS.md` §1–2: particle systems driving still-sprite kits
  (flipbooks only where a kit cannot do the job), and every effect fired by a simulation event, so
  it works online.
- **M9's lighting session starts from:** unlit sprites with painted cel shading, an engine climate
  tint, and a rim light — not normal maps, which AI drafts cannot produce consistently.

## Pending commits

None.

## Git

`main` · pushed through: see `git log origin/main` — the orchestrator pushes after every commit

## Log

- 2026-09-24 — Netcode `DONE`: `HANDOFF-M8.md`, approved by Michael. Task numbers to be moved to
  start at 86 (84 and 85 are M7's). Plan 1 next.
- 2026-09-24 — Art `DONE`: item-icon brief (3ce0274); effects spec, seven kit prompts, held-weapons
  brief (8f16b69). `.gitignore`'s `Icon?` was hiding `icons/` folders on Windows — fixed (05919b0).
- 2026-09-24 — Art `DONE`: hero template — 21 parts, fixed canvases and pivots, layering, guide
  images, and the first (story-free) skin's prompts (f9b6611).
- 2026-09-24 — Netcode `DONE`: M8 design session. Recorded as **D58–D62**; D57 (the menu layer,
  Groundwork) recorded now so the log stays in order. ROADMAP §4/§5.3 and GAME_DESIGN §7 follow.
- 2026-09-24 — Art `DONE`: art bible v0 and six style-frame prompts. Bible review added to
  Michael's queue.
- 2026-09-24 — Art `DONE`: existing art classified with Michael (4435833) — hand-final except the
  unlicensed props; two deletions added to the M9 backlog.
- 2026-09-24 — Netcode `DONE`: options memo (e9de806); M8 design session opened. Art `DONE`:
  provenance spec + manifest (dc58c91); ChatGPT images chosen; D55 amended (slot = whole skin).
- 2026-09-24 — Netcode `DONE`: readiness audit (bdc1f58). Three findings → Builder backlog, one
  → M8's plan, one → Michael's queue.
- 2026-09-24 — Art, Builder, World `ONLINE`. Builder `BLOCKED` on Unity. The orchestrator was
  renamed twice; PROTOCOL rule 6 now uses reply addresses, not names. Netcode `ONLINE`.
- 2026-09-24 — Team set up: protocol, board, four lane files, startup prompts.
