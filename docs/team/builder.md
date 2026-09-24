# Lane: Builder

**Mission:** build the engineering milestones in Unity, one plan at a time, with both test gates
green before anything is called done. You hold **the sim** by default (PROTOCOL rule 4) — you are
the only lane that writes under `Assets/` unless the orchestrator lends it out.

**Owns:** `Assets/`, `Packages/`, `ProjectSettings/` (while holding the sim) · this file.

**Read first (in order):** `CLAUDE.md` · `docs/team/PROTOCOL.md` · `docs/team/BOARD.md` · this file
· `docs/ROADMAP.md` §4 "Groundwork" · the plan below.

**The plan:** `docs/superpowers/plans/2026-09-24-groundwork-input.md` — 14 tasks, full code and
tests in each.

---

## How to run the plan

- **Use `superpowers:subagent-driven-development`** (the plan's recommended mode): a fresh
  implementer subagent per task, then a spec review and a quality review. Implementers may run on
  Sonnet to save tokens; reviewers should not.
- **Nobody commits.** Every plan step that says "Commit" becomes a `DONE` message to the
  orchestrator (PROTOCOL rule 9) — then wait for `COMMITTED` before starting the next task.
  Tell every subagent: no git writes, and never discard changes it did not make.
- **The Unity MCP bridge is the gate.** Only you call it. After any on-disk scene edit expect the
  "modified externally" modal (see memory: project status → the recovery notes).
- **Ask for `QUIET` before** any play-mode step that needs Unity in the foreground (synthetic
  keyboard or gamepad input — Task 11 Step 8) and before Michael's pass (Task 14 Step 7).
- **Michael's pass** (Task 14 Step 7) is his fast-motion checklist: send it to the orchestrator,
  who schedules it with quiet on.
- **Shared docs in Task 14** (`DECISIONS.md` D57, `GAME_DESIGN.md` §3.1, `CLAUDE.md`,
  `ARCHITECTURE.md`, `ROADMAP.md`) belong to the orchestrator: send the plan's text for those steps
  in your `DONE`, and the orchestrator applies it.
- After Groundwork: the next plan is M8, which the Netcode lane is writing. The orchestrator will
  point you at it.

---

## Waiting on

- **The Unity MCP bridge.** It was disconnected at the last check (2026-09-24). Step 0 is to
  confirm it answers (`editor_status`); if it does not, send `BLOCKED` and ask Michael to reopen
  Unity.

## Current state

Not started.

## Next steps

1. Confirm the bridge; `editor_stop` if the editor is in play mode.
2. Record the baseline: EditMode (expect 626) and PlayMode async (expect 15).
3. Groundwork Task 1 — the menu vocabulary.

## Answers and decisions

- 2026-09-24 (Michael, Groundwork kickoff): X = Sell (Combine-all mid-pick), Y = Lock, LB/RB =
  switch tab or mode — as proposed. X sells **instantly**; locks are the only safety. **Xbox
  letters only** on every controller.

## Log

- 2026-09-24 — Lane seeded by the orchestrator.
