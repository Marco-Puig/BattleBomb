# Lane: Orchestrator

**Mission:** plan, answer cross-lane questions, hold git (every commit and push), hand out the
Unity editor, keep the shared docs and the board, and get lanes — including this one — ready to
compact when they are blocked or out of work (PROTOCOL rule 11).

**Owns:** `docs/team/BOARD.md`, `PROTOCOL.md`, `PROMPTS.md`, this file · the shared docs
(`DECISIONS.md`, `GAME_DESIGN.md`, `ROADMAP.md`, `ARCHITECTURE.md`, `CLAUDE.md`, existing
`HANDOFF-*.md`) · git · project memory.

**After any reset, read:** `CLAUDE.md` · `docs/team/PROTOCOL.md` · `docs/team/BOARD.md` · this file.
Then run `ListAgents`, and message every running lane once so it has this session's new `from=`
address (the address changes when this app restarts).

---

## How each message is handled

- **`DONE`** — `git status --short --untracked-files=all <paths>`; check the paths match the list
  (nothing extra, nothing ignored — `git check-ignore -v` if a new folder is missing); spot-check the
  change against its plan or claim; stage **exactly** the listed paths; commit with the lane's
  subject and body plus `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; `git fetch`, then
  push; reply `COMMITTED <hash>` with any decisions; add a line to the board's Log.
- **Commit messages** go through a python-written file in the scratchpad and `git commit -F` —
  heredocs break on apostrophes here, and PowerShell here-strings leak `@` into the subject.
- **`QUESTION` / `BLOCKED`** — answer if it is in the orchestrator's authority (process, scope,
  small refinements that keep Michael's intent); otherwise put it on Michael's queue with a
  recommendation. Record decisions in `DECISIONS.md` (amend the D entry in place, say who and when).
- **Blocked or out of work** → send `PREPARE TO COMPACT`; on `READY TO COMPACT`, tell Michael.
- **LF→CRLF warnings on commit are harmless** (`text=auto`).

## Lane addresses (reply to these — names change)

| Lane | Last `from=` address | State |
|---|---|---|
| Builder | `uds:\\.\pipe\LOCAL\cc-msg-43864ba16405a9d03037d6ccae21ba0d` | idle — READY TO COMPACT; **104 parked** in stash "104 WIP (Builder)". On resume: `git stash pop` (check the list first), then wake it — recompile, EditMode 847, PlayMode 134, mutations, review, live check, DONE |
| Art | `uds:\\.\pipe\LOCAL\cc-msg-d1e3f618d64192f7034adc4fa072bc9d` | idle — READY TO COMPACT (9902064); send `CONTINUE` when the music tool, a pet direction, or the World bible lands |
| Netcode | `uds:\\.\pipe\LOCAL\cc-msg-ca83eb67f92347f8af2eeeade5ea5050` | idle — READY TO COMPACT (Plan 3 5db14e6); on call for the Builder. At 114: apply Plan 2's and Plan 3's HANDOFF-M8 departures, CLAUDE.md layout line, ARCHITECTURE (Platform/Steam, PlatformRegistry, ILobbyService) |
| World | `uds:\\.\pipe\LOCAL\cc-msg-727b6d4358334e882020a4df8ee0cfb5` | idle — READY TO COMPACT (e016a6f); at Q2b (tone); drafts 1–5 (a Claude session, at Michael's request) and 6–10 (Michael's own) pushed for him and Marco to choose from |

## Current state (2026-09-26, end of day)

- **Groundwork complete.** M8 Plan 1 closed (96, b87a21d; HANDOFF-M8 has its build log). Plans 2
  and 3 written and committed.
- **M8 Plan 2:** 97–103 committed (2209aa3 = 103, EditMode 844, PlayMode 121). **104 parked** in
  `git stash` "104 WIP (Builder)" (24 files). Resume: pop it, wake the Builder. Then 105 = Michael
  and Marco's sitting (`m8-plan2-pass.md`, with 96a/96b and Groundwork items 7 and 12); QUIET for it.
- **At 105's DONE:** apply Plan 2's HANDOFF-M8 departures and build log, and the ROADMAP line.
  **At 114:** Plan 3's departures, CLAUDE.md layout line, ARCHITECTURE (Platform/Steam,
  PlatformRegistry, ILobbyService).
- **Before 109 lands, tell Michael:** Steam in the editor signs in as Spacewar and opens his solo
  games to friends; his save is copied under his Steam ID.
- **World:** D56 amended (beats in gameplay; the prelude is M10's opening). Michael and Marco are
  choosing among drafts 1–10; the session stands at Q2b (tone).
- **Michael's queue:** see the board.

## Traps learned

- **Unity bridge wedge:** a synchronous `run_tests` that times out holds the bridge's slot forever.
  Only Michael can clear it: Window → Pipeline → Stop Server, then Start Server. (Cause, per the
  Builder: the timeout cancels the run but never releases `BasePipelineServer.m_ExecGate`.)
- **A scene goes dirty after a save** when a property edit is recorded in the same `eval` as the
  `SaveScene` call. Save in its own eval and check `isDirty` before any sync run.
- **`.gitignore`'s macOS `Icon?`** matched any folder named `icons` on Windows; `![Ii]cons` fixes it.
- **The MCP bridge reaches only the main editor.** Multiplayer Play Mode's Player 2 is a separate
  process; its IMGUI buttons need a click. Michael declined screen control of Unity (2026-09-25,
  Task 90) — never ask a lane to work around that. Clone-side checks go to a harness
  (HeadlessGuest, Task 95's guest harness) or into Michael's checklists, batched into few sittings.
- **Task numbers 84–85 are M7's.** M8 runs 86–114; Groundwork is G1–G14; the pre-M8 fixes F1–F3.

## Log

- 2026-09-26 — End of day: 104 stashed, GitHub at e016a6f (103's code, compiles clean). All lanes ready
  to compact, the orchestrator too.
- 2026-09-26 — Plan 3 committed (5db14e6). Before 109 lands, tell Michael: Steam in the editor signs in as
  Spacewar and opens his solo games to friends; the save's name becomes his Steam ID (copied).
- 2026-09-26 — 96 committed (b87a21d); HANDOFF-M8 Plan 1 build log applied.
- 2026-09-26 — Michael's passes in: Groundwork complete (item 7 carried), M8 Plan 1 passed, lag table
  (Normal noticeable, Bad too late). Bugs 96a/96b to the Builder. On 96's DONE: apply the close-out
  to HANDOFF-M8 (Build log after "The build — stages and tasks") and ROADMAP §4's M8 line; commit 96.
- 2026-09-25 — Builder BLOCKED on Unity closed → prepped 97 → READY TO COMPACT (acf3c65). Netcode
  READY TO COMPACT (3cc4c48). All lanes idle; Michael to open Unity.
- 2026-09-25 — Plan 2 committed (5bba6ed). Builder woken on 97 (start without 96). Netcode told
  PREPARE TO COMPACT.
- 2026-09-25 — Netcode reopened; writing Plan 2. Sent it the board's Plan 2 carry-forward, the shop
  rack, F4 and G8's catch-up pause (reassess apart), the TargetRegistry watch, and the same-hero
  save question (ask, don't pick).
- 2026-09-25 — M8 Plan 1 code complete (86–95). Builder READY TO COMPACT (924c362), close-out drafted
  in builder.md. Waiting on Michael (one-sitting checks; reopen Netcode for Plan 2).
- 2026-09-25 — Art READY TO COMPACT (9902064); Netcode and World lane files committed as they
  closed (35f5441).
- 2026-09-25 — Lane file created for PROTOCOL rule 11 (Michael: get blocked or idle sessions,
  the orchestrator included, ready to compact).
