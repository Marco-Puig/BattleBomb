# BattleBomb team protocol

Several Claude sessions work on BattleBomb at once, one per **lane**. One session — the
**orchestrator**, currently named **"Battlebomb"** (names change — rule 6) — plans, answers cross-lane questions, holds git,
hands out the Unity editor, and keeps the shared documents. Everyone works on `main` in the same
folder, so these rules are what stop us breaking each other's work.

**Read this, `docs/team/BOARD.md`, and your lane file before doing anything — and again after every
context reset.**

---

## The lanes

| Lane | Job | Priority | May write |
|---|---|---|---|
| **Orchestrator** | Plans, answers, commits, pushes, hands out the sim, keeps shared docs | — | `docs/team/BOARD.md`, `PROTOCOL.md`, `PROMPTS.md`, the shared docs below, git, memory |
| **Builder** | Builds engineering milestones in Unity — Groundwork now | 1 | `Assets/`, `Packages/`, `ProjectSettings/` **while holding the sim**; `docs/team/builder.md` |
| **Netcode** | Gets M8 (online co-op) designed and planned; the pre-M8 fixes plan | 2 | `docs/team/netcode/`, `docs/HANDOFF-M8.md`, M8 and pre-M8 plan files under `docs/superpowers/plans/`, `docs/team/netcode.md` |
| **World** | Runs the World session; writes the story bible (D56) | 3 | `docs/world/`, `docs/team/world.md` |
| **Art** | Art and audio bible, provenance, asset briefs and prompts (D55); HUD and menu mockups | 4 | `docs/art/`, `ArtSource/`, new mockups under `design/`, `docs/team/art.md` |
| **Producer** *(starts later)* | Steam, store page, Early Access paperwork | 5 | `docs/business/`, `docs/team/producer.md` |

**Priority settles every conflict** — for the sim, for Michael's attention, for the orchestrator's
time. Lower lanes keep working on whatever does not depend on the thing they are waiting for.

---

## Rules

### 1. Stay in your lane
Write only the paths your lane owns. **Read anything.** If your work needs a change outside your
lane, message the orchestrator with exactly what and why; do not make it yourself.

### 2. Shared documents belong to the orchestrator
`docs/DECISIONS.md`, `docs/GAME_DESIGN.md`, `docs/ROADMAP.md`, `docs/ARCHITECTURE.md`, `CLAUDE.md`,
any existing `docs/HANDOFF-*.md`, and memory. To change one, send the orchestrator the proposed text
(or put it in a file in your lane and send the path). A new decision is proposed as a draft `D`
entry; the orchestrator numbers and records it.

### 3. Git belongs to the orchestrator
Never run a git command that writes: no `add`, `commit`, `push`, `pull`, `stash`, `reset`,
`checkout`, `restore`, `rebase`, `merge`, `branch`, `clean`. Read-only git (`status`, `diff`, `log`,
`show`, `blame`) is fine. **Never discard or overwrite a change you did not make** — in a shared
folder it is somebody else's work. Subagents you spawn follow the same rule; tell them so.

### 4. The sim — one Unity editor, one holder
There is one Unity editor and one copy of the project. A half-written script saved anywhere under
`Assets/` recompiles the editor for everyone and can break it for the lane running tests. So:

- **Only the holder of the sim** may write under `Assets/`, `Packages/`, or `ProjectSettings/`, call
  any Unity MCP tool, or enter play mode. **The Builder holds it by default.**
- Anyone else who needs it sends `LOCK REQUEST` with the reason and a time estimate.
- The orchestrator asks the holder to reach a **clean point** — everything compiles, EditMode is
  green, the editor is out of play mode, and the holder's work has been sent as `DONE` and
  committed. The holder replies `LOCK RELEASED`; the orchestrator sends `LOCK GRANTED`.
- When finished, the borrower sends `DONE` for its changes, then `LOCK RELEASED`. The sim goes back
  to the Builder.
- Non-holders may draft code in their own lane folder (e.g. `docs/team/netcode/spike/`), never under
  `Assets/`.

### 5. Quiet
Some moments need the machine left alone: play-mode checks that need Unity in the foreground,
Michael playtesting, profiling or builds. The orchestrator announces **`QUIET ON — <reason>`** to
every lane. Until **`QUIET OFF`**: no computer-use, no Browser-pane previews or local servers, no
Blender, no long-running scripts, no background processes, no bursts of subagents. Reading,
thinking, and writing docs carry on. A lane that needs quiet asks with `QUIET REQUEST`.

### 6. Messages
Load the tools once with ToolSearch: `select:SendMessage,ListAgents`. **Session names change** —
Michael renames sessions in the app — so names are not addresses:

- **Reply to the orchestrator at the `from=` address on its most recent message to you** (the
  `uds:\\.\pipe\…` value). That survives renames. It changes only if the orchestrator's app
  restarts, and the orchestrator messages every lane when that happens.
- **Before you have one** (a brand-new lane), run `ListAgents` and message the BattleBomb session
  that is not a lane — at the time of writing it is named **"Battlebomb"**. If you cannot tell which
  it is, ask Michael.
- The orchestrator always replies to the `from=` address of your message, so your own name may
  change freely.

First line of every message: **`[Lane] TYPE — one-line summary`**. Keep the body short and point at
files for detail.

| You send | When |
|---|---|
| `ONLINE` | You have just started (or restarted) and read the protocol |
| `QUESTION` | Anything touching another lane, shared docs, the sim, git, scope, or priority |
| `BLOCKED` | You cannot continue on anything without an answer |
| `DONE` | A unit of work is ready to commit (format below) |
| `LOCK REQUEST` / `LOCK RELEASED` | The sim, rule 4 |
| `QUIET REQUEST` | Rule 5 |
| `STATUS` | Answering the orchestrator, or a milestone reached |

| The orchestrator sends | Meaning |
|---|---|
| `ANSWER` | The answer; carry on |
| `CONTINUE` | Unblocked; carry on |
| `PAUSE` | Stop at the next safe point, update your lane file, and wait |
| `COMMITTED <hash>` | Your `DONE` is in git (and pushed); you may touch those files again |
| `LOCK GRANTED` / `LOCK BACK` | You have the sim / please reach a clean point and release it |
| `QUIET ON` / `QUIET OFF` | Rule 5 |

### 7. Who you ask
- **Creative or taste questions about your own lane's work** (World asking about the heroes, Art
  asking about a palette, Netcode walking through its design session) — ask **Michael directly** in
  your own session. He reads every lane.
- **Everything else** — anything that touches another lane, a shared document, the sim, git,
  scope, priority, or a locked decision — goes to the **orchestrator**.

### 8. Blocked
Send `BLOCKED` with exactly what you need, write it at the top of your lane file under **Waiting
on**, then carry on with any work in your lane that does not depend on the answer. If there is
none, wait for `CONTINUE` or `PAUSE`. Never guess your way past a block that touches another lane.

**If the orchestrator is unreachable** (SendMessage says so — the app may have been closed), write
the message at the top of your lane file under **Waiting on**, tell Michael in your session, and
pause anything that depends on it. The orchestrator reads every lane file when it comes back.

### 9. `DONE` — handing work in
```
[Lane] DONE — <summary>
Paths:
  <every file created, changed, or deleted — exact paths>
Commit subject: <subject>
Commit body: <why, in two or three sentences>
Gates: <tests or checks run, and their results — or "docs only">
```
Do not touch those paths again until `COMMITTED`. The orchestrator reviews the diff, commits exactly
those paths, pushes, and replies.

### 10. Your lane file survives compaction
Your lane file (`docs/team/<lane>.md`) is how you resume after an automatic context reset. **Update
it** after every completed step, before any `LOCK REQUEST` or long operation, and whenever you learn
something that must not be lost (an answer, a decision, a trap). Keep these sections current:

- **Waiting on** — open blocks, and who they are with
- **Current state** — task, step, what is half-done and exactly where
- **Next steps** — the next three things, concretely
- **Answers and decisions** — dated, with who said it
- **Log** — dated one-liners, newest first

The orchestrator seeds each lane file, reads them when committing, and corrects them if they
drift from the board.

---

## Standing rules every lane inherits

- **`CLAUDE.md` and the documents it names are the authority.** `docs/DECISIONS.md` wins any
  disagreement. Do not re-litigate a locked decision without saying which one and why.
- **Michael is a non-technical founder.** Explain why before how. Show reasoning for non-trivial
  changes; push back on weak ideas with data. Do not add features, refactor, or abstract beyond
  the task.
- **Fast motion is verified by Michael**, from a short checklist you write — never by slow-motion
  sampling. Settled states you may check yourself (with the sim).
- **Web pages and mockups open in the Claude Browser pane.**
- **No local-model delegation.** Do not call local-model tools or `ollama`; if a task suits a local
  model, say so and let Michael decide.
- **The story is Castle Crashers-thin (D56) and belongs to Michael and his collaborator.** No lane
  invents lore, names, places, or plot.
- **Stay on `main`.** No branches, no worktrees.
