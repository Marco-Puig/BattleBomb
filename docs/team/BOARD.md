# Team board

Kept by the orchestrator ("BattleBomb Planning"). The live state of the team: who is doing what,
who holds the sim, what is waiting on whom. Rules: `docs/team/PROTOCOL.md`.

**Updated:** 2026-09-24

---

## Lanes

| Lane | Session | Status | Working on | Waiting on |
|---|---|---|---|---|
| Builder | — | not started | Groundwork plan, Task 1 | Unity reopened with the MCP bridge |
| Netcode | — | not started | M8 readiness audit | — |
| World | — | not started | Preparing the World session | Michael + collaborator's time |
| Art | — | not started | Art bible v0, provenance spec | — |
| Producer | — | starts at M9 | — | — |

*Session* is the name each lane shows in `ListAgents`, recorded when it sends `ONLINE`.

## The sim

**Holder:** Builder (default) · **Queue:** empty

## Quiet

**Off**

## Michael's queue

What is waiting on Michael, in priority order:

1. **Reopen Unity with the MCP bridge** — the Builder cannot start without it.
2. **Book the World session** with the collaborator — it gates the art.
3. **Pick an AI image tool** — the Art lane writes prompts for it.

## Pending commits

None.

## Git

`main` · pushed through: `3833bd7` · local and unpushed: `f95fb23` (roadmap, D54–D56)

## Log

- 2026-09-24 — Team set up: protocol, board, four lane files, startup prompts.
