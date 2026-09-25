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
| Builder | `uds:\\.\pipe\LOCAL\cc-msg-43864ba16405a9d03037d6ccae21ba0d` | running — pre-M8 fixes F1–F3 |
| Art | `uds:\\.\pipe\LOCAL\cc-msg-d1e3f618d64192f7034adc4fa072bc9d` | idle — READY TO COMPACT (9902064); send `CONTINUE` when the music tool, a pet direction, or the World bible lands |
| Netcode | `uds:\\.\pipe\LOCAL\cc-msg-8ad7c204f7e01241eec33845b5cce519` | closed — restart from `PROMPTS.md` near M8 |
| World | `uds:\\.\pipe\LOCAL\cc-msg-a778851d8c98083210e64c174195c04c` | closed — at Q1, waits on Michael |

## Current state (2026-09-25)

- **Groundwork:** built — G1–G13b (56e4be5..527df2b); G14's docs 5f3799a. **Michael's pass pending**
  (15 items, `docs/team/groundwork-pass.md`). On his go: QUIET ON to the
  Builder (clean point, hands off Unity), he plays, QUIET OFF. Record his verdict in ROADMAP §4
  (heading → complete), CLAUDE.md's progress row, and memory. Item 12 is his judgement; if he
  wants the fix, it rides F5.
- **Now:** the Builder runs the pre-M8 fixes plan F1–F3, then F5 (chest grid scrolling past 40 stacks), then M8 Plan 1 (Tasks 86–96). Netcode writes
  Plan 2 once Plan 1 is underway, Plan 3 after Michael's lag table (Task 96). Candidate **F4** (lost
  sub-update taps) and the catch-up-loop pause item wait for Netcode to assess.
- **Art:** rounds 1–2 done (knight rig, effects, fixture v2, icons, weapons). Waiting on Michael's
  music tool (GPT steps 26–32), a pet direction, and the World bible.
- **Michael's queue:** see the board — top items are the World session, the same-hero save call,
  the HUD additions yes/no, the music tool.

## Traps learned

- **Unity bridge wedge:** a synchronous `run_tests` that times out holds the bridge's slot forever.
  Only Michael can clear it: Window → Pipeline → Stop Server, then Start Server. (Cause, per the
  Builder: the timeout cancels the run but never releases `BasePipelineServer.m_ExecGate`.)
- **A scene goes dirty after a save** when a property edit is recorded in the same `eval` as the
  `SaveScene` call. Save in its own eval and check `isDirty` before any sync run.
- **`.gitignore`'s macOS `Icon?`** matched any folder named `icons` on Windows; `![Ii]cons` fixes it.
- **Task numbers 84–85 are M7's.** M8 runs 86–114; Groundwork is G1–G14; the pre-M8 fixes F1–F3.

## Log

- 2026-09-25 — Art READY TO COMPACT (9902064); Netcode and World lane files committed as they
  closed (35f5441).
- 2026-09-25 — Lane file created for PROTOCOL rule 11 (Michael: get blocked or idle sessions,
  the orchestrator included, ready to compact).
