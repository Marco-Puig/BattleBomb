# Team board

Kept by the orchestrator ("BattleBomb Orchestrator"). The live state of the team: who is doing what,
who holds the sim, what is waiting on whom. Rules: `docs/team/PROTOCOL.md`.

**Updated:** 2026-09-24

---

## Lanes

| Lane | Session | Status | Working on | Waiting on |
|---|---|---|---|---|
| Builder | Builder | **BLOCKED** | Reading the Groundwork plan (no writes) | Michael opening Unity with the MCP bridge |
| Netcode | M8 online co-op readiness audit | working (no `ONLINE` received yet) | Readiness audit — reading the sim code | — |
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
2. **The World session** — the World lane is ready to start at question 1 (the goal).
3. **Which AI image tool** — the Art lane will ask in its session.
4. *(Later)* **M8's design session** — Netcode, once its audit and options memo are done.

## Pending commits

None.

## Git

`main` · pushed through: `bd5f110` (team set-up) · nothing unpushed

## Log

- 2026-09-24 — Art, Builder, World `ONLINE`. Builder `BLOCKED` on Unity. The orchestrator was
  renamed "BattleBomb Orchestrator"; PROTOCOL and PROMPTS updated.
- 2026-09-24 — Team set up: protocol, board, four lane files, startup prompts.
