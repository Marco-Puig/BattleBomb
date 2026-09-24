# Team board

Kept by the orchestrator (currently named "Battlebomb"). The live state of the team: who is doing what,
who holds the sim, what is waiting on whom. Rules: `docs/team/PROTOCOL.md`.

**Updated:** 2026-09-24

---

## Lanes

| Lane | Session | Status | Working on | Waiting on |
|---|---|---|---|---|
| Builder | Builder | **BLOCKED** | Reading the Groundwork plan (no writes) | Michael opening Unity with the MCP bridge |
| Netcode | Netcode | online | Readiness audit — writing `docs/team/netcode/readiness.md` | — |
| World | World | online | Drafting the question list, then the session at question 1 | Michael (+ collaborator) for the session |
| Art | BattleBomb Art lane | online | Required reading, then the provenance spec | Michael's AI image tool |
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
2. **M8's design session** — open in the Netcode lane's session (options memo done). M8 is the
   next milestone, so its answers come first when both sessions are waiting.
3. **The World session** — open in the World lane's session, at question 1 (the goal).
4. *(Small decision)* **Two couch players on the same hero** share one character save (audit §7.2)
   — the second overwrites the first. Stop equal picks at character select, or save per seat?

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
- **`Art/AI/maincharactertemp.ai` is Michael's own work**, but a folder named `AI` now means
  AI-generated (D55). Move it out.

## Pending commits

None.

## Git

`main` · pushed through: see `git log origin/main` — the orchestrator pushes after every commit

## Log

- 2026-09-24 — Netcode `DONE`: options memo (e9de806); M8 design session opened. Art `DONE`:
  provenance spec + manifest (dc58c91); ChatGPT images chosen; D55 amended (slot = whole skin).
- 2026-09-24 — Netcode `DONE`: readiness audit (bdc1f58). Three findings → Builder backlog, one
  → M8's plan, one → Michael's queue.
- 2026-09-24 — Art, Builder, World `ONLINE`. Builder `BLOCKED` on Unity. The orchestrator was
  renamed twice; PROTOCOL rule 6 now uses reply addresses, not names. Netcode `ONLINE`.
- 2026-09-24 — Team set up: protocol, board, four lane files, startup prompts.
