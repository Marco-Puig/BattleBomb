# Team board

Kept by the orchestrator (currently named "Battlebomb"). The live state of the team: who is doing what,
who holds the sim, what is waiting on whom. Rules: `docs/team/PROTOCOL.md`.

**Updated:** 2026-09-24

---

## Lanes

| Lane | Session | Status | Working on | Waiting on |
|---|---|---|---|---|
| Builder | Builder | idle — ready to compact (close-out drafted) | M8 Plan 1 built (86–95) | Michael's pass (96); Plan 2 from Netcode |
| Netcode | — | not running (session closed) | Standby: Builder's reference; Plan 2 once Plan 1 is underway | Groundwork + F1–F3 |
| World | — | not running (session closed) | The story session, at Q1 (the goal) | Michael |
| Art | Art | idle — ready to compact | GPT steps 1–25 done; 26–32 (music) blocked | Michael: music tool, pet direction, bible review; the World bible |
| Producer | — | starts at M9 | — | — |

*Session* is the name each lane shows in `ListAgents`. Michael renames sessions freely, so replies
go to the address a message came from.

## The sim

**Holder:** Builder (default) · **Queue:** empty

## Quiet

**Off**

## Michael's queue

What is waiting on Michael, in priority order:

1. **Groundwork pass + M8 online checks and the lag table** (~35 min; can be two sittings) — the
   16 controller checks in `docs/team/groundwork-pass.md`, then `docs/team/m8-plan1-pass.md`
   (Stages A and B plus Task 96's lag table, which Plan 3 is written from). Item 12 in the
   Groundwork list is his judgement call.
2. **Reopen the Netcode session** (paste its prompt from `docs/team/PROMPTS.md`) — **now on the
   critical path:** Plan 1 is built, and Plan 2 (97–105) needs writing before the Builder has
   more M8 work.
3. **The World session** — open in the World lane's session, at question 1 (the goal).
4. *(Small decision)* **Two couch players on the same hero** share one character save (audit §7.2)
   — the second overwrites the first. Stop equal picks at character select, or save per seat?
5. **The ChatGPT image prompts** — steps 1–25 done. What's left is the music (steps 26–32), which
   waits on the music tool below.
6. *(Small decisions)* **The HUD additions, yes or no to each** — separate pause and settings menus;
   volume, damage-number and controls-view settings; "sack full" on the loot card; white-hot crit
   numbers; Settings and Quit on the title screen.
7. *(Before M8's end)* **The collaborator's availability** for the two-PC pass — they are the remote
   tester; there is no second PC (D59, ROADMAP §4 M8).
8. *(At M8's close-out)* **Create the Steamworks account and app ID** — start Valve's paperwork a few
   days ahead; it can take that long.
9. *(Before M9's first licensed sound — with the collaborator, who owns the repo)* **Licensed audio
   vs the public repo** (`docs/art/AUDIO.md` §1). Most sound-effect licences forbid redistributing the
   raw files, and a public GitHub repo does exactly that. Options: make the repo private
   *(orchestrator's recommendation: it also covers unreleased source and third-party art, and a
   two-person team cannot live with clones that build silent)*; git-ignore licensed audio and back it
   up privately; or CC0 sounds only.
10. *(Whenever there is a gap)* **Pick the music tool** (placeholders now: AIVA free or Suno Pro — ChatGPT cannot make audio; **blocks GPT steps 26–32**) — Art's research (`AUDIO.md`): AIVA Pro grants
   ownership plus MIDI; Udio no longer allows downloads; ElevenLabs' self-serve plans exclude games.
11. *(Whenever there is a gap)* **Review the art bible v0** — `docs/art/ART_BIBLE.md`, in the Art
   lane's session. Two proposals ride with it: Fire's colour `#FF7326` → `#FF4A1C` (it nearly matches
   Legendary's loot orange), and unlit painted-cel sprites instead of normal maps for M9.
12. *(No date)* **A new pet art direction** — the simple-chunky direction is provisional; the terrier stays
   at its v1 draft until then.

## M8 — Plan 1 underway

Spec approved by Michael: `docs/HANDOFF-M8.md`. Built in three plans, each written while the one
before is underway: **Plan 1** = stages A+B, **Plan 2** = C+D, **Plan 3** = E+F and the close-out.
**Starts only after** Groundwork is merged **and** the Builder backlog below is done.

- **Plan 1 is written** — `docs/superpowers/plans/2026-09-24-m8-plan1-wire-and-mirror.md`, Tasks 86–96.
  Tasks 90, 93, 94, 95, 96 need **quiet** (two-editor Multiplayer Play Mode runs). Task 90 adds the
  `com.unity.multiplayer.playmode` package and sets `runInBackground = true`. Task 96 ends with
  Michael's lag table, which Plan 3 is written from.
- **Plan 2** (97–105) is written once Plan 1 is underway; **Plan 3** (106–114) after Task 96.
- **Done:** 86 (5e586a4), 87 (4f42e0c), 88 (5fd0920), 89 (fd33bea), 90 (d00c552), 91 (5be281c), 92 (07a8c64), 93 (c8744bc), 94 (3d567b1), 95 (f393d98). **96 is Michael's pass + the close-out.**
- **Michael's clone-side checks** collect in `docs/team/m8-plan1-pass.md` (the bridge can't click in
  Player 2's window; he declined screen control). Stage A is ready.
- **Carried into later tasks** (found while building; the orchestrator's calls, 2026-09-25):
  - **96:** re-measure bandwidth (the 3.5 KB figure included ~1 KB of drop ids, now gone).
    Also: a starved remote lets go with a release, so a Heavy charged through a 250 ms stall fires
    — by design; tune `StarvedRepeatSteps`. Bandwidth measured at 91: ~3.5 KB/snapshot, ~105 KB/s
    for the paper case (2 players, 20 enemies, 20 bolts) against the paper's 2.5 KB; worst case
    ~147 KB (under the 256 KB frame).
    From 93: the teleport threshold should scale with the gap between the snapshots around the
    render frame; a host pause stops snapshots and the guest's clock climbs to the newest, losing
    the jitter cushion ~0.4 s after resume — hold at newest − delay while the newest isn't advancing.
  - **Plan 2 (Netcode):** unregistering a source by id can remove a newer source under the same id
    (both `RemoteCommandSource` and `InputSystemCommandSource`) — bites on D61's solo carry-on and
    rejoin; unregister only when `TryGet` returns this source. The refusal reason is lost (the
    host's Lost("left") overwrites it; a lagged guest sees "The host left") — keep Status when never
    welcomed, and let the guest close on Refuse. `UseSeat` assumes Player 2 is authored active. The
    guest's own front door stays live while connected, and the guest's abandoned body still holds
    gates shut (D60/D61). **Task 103:** the drop-in baseline sends a `DropSpawned` for every drop
    still on the ground before the first snapshot.
    From 93: `ApplyReplicaPlayerSide` doesn't raise ScreenChanged (the guest's screens).
    From 95: HANDOFF-M8 §15 makes `HoldMenuPause` a no-op online — keep a "menu open" count
    separate from "the world pauses", or `MenuPauseHeld` never goes true online and the guest's
    menu gate (and SettingsMenu:99) silently stop working; `The_guests_own_menu_never_reaches_the_host`
    fails if missed. While the host sits in the results screen, a remote Player 2's last buttons
    stay held, so only the pointer gets out (the couch version is G8's, on the Later list).
    From 94: a stale `BeginLoad` completion can unload a stage requested again — add a load
    generation. A guest who rejoins mid-match deadlocks the airlock (the host waits for a
    StageReady the new guest was never asked for); until then, Task 96 tests a rejoin by
    relaunching the run, not re-hosting in place.
  - **Plan 3 (Netcode):** `catch (SocketException)` sits in Gameplay and always names port 7777 —
    Listen should report failure through `INetTransport` when the Steam transport arrives. Decide
    whether delta snapshots are needed (the 96 bandwidth above). **Task 107:** a Jump landing in a
    skipped frame is registered by the host at N+1 while the guest's log has it at the skipped
    frame; the next snapshot corrects it — if the feel pass shows it, snapshots carry the host's
    held baseline.

## Builder backlog — after Groundwork, before M8

Found by the Netcode audit (`docs/team/netcode/readiness.md`), checked against the code by the
orchestrator. **Planned:** `docs/superpowers/plans/2026-09-24-pre-m8-fixes.md`, Tasks F1–F3 — the
Builder runs it straight after Groundwork, before M8 Plan 1.

1. ~~**Destroyed enemies keep acting for the rest of the frame** (§4.3 item 1).~~ **Premise false on
   Unity 6000.5.8f1** (Builder, 2026-09-25): `Destroy` runs `OnDisable` at once, and every registry
   leaves there, so nothing acts after removal — the plan's tripwires pass on today's code. The real
   bug underneath: `ResolveDeaths` walks the registry's own list while `Destroy` shrinks it, so the
   enemy after each corpse settles a step late (XP, loot order). **F1 reshaped (orchestrator):**
   collect the dead, then settle them, red-first; the tripwires stay as regression guards. **Done — F1, 15778dd.**
2. ~~**The debug grant row ships in release builds** (§5.2).~~ **Done — F2, 724d44e.** Put it behind the same `#if` as the
   tier-overlay row.
3. ~~**Every run replays the same loot** (§7.1).~~ **Done — F3, 5abd671.** The three seeds are constants 1/2/3. Seed from the
   session at launch — *orchestrator's call, flagged to Michael*.

**Candidate F4 — Netcode to assess when its lane reopens** (found in G4, pre-existing): a tap shorter
than one Input System update, or one that lands in a frame with no simulation step (above 60 fps),
is lost — the sample only sees what is held at that instant. It matters for the revive heartbeat's
mashing (D31) and for M8's remote input path. Fix shape: record press edges between samples.
*Note for Netcode:* F1's finding (removal is immediate here) changes the read of the catch-up-loop
pause item and F4, both filed as F1's "frame-step family" — reassess them on their own.

~~**F5 — the chest grid past 40 stacks**~~ **Done — F5.1 2610ef0, F5.2 cc7a9fe.** (found in G13's review, pre-existing; the orchestrator's
call). The sack holds 200 but the grid draws 40, so the cursor walks into undrawn cells and the
popover opens under cell 39 naming another item — and X sells instantly, so a player can sell a
stack they cannot see. Fix: scroll the grid with the cursor (no clamping — it would strand items
41+). After F1–F3; the Builder sizes it when it gets there.

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
- **M9's HUD plan inherits** the approved mockups in `design/hud-menus/` (direction approved; colours may
  shift) **and these additions, still pending Michael's word on each:** separate
  pause and settings menus; volume, damage-number (on/off, S/M/L) and controls-view settings; "sack
  full" drawn on the loot card (GDD §5.4); white-hot crit numbers (orange clashes with Fire and
  Legendary); Settings and Quit on the title screen.
- **M9's lighting session starts from:** unlit sprites with painted cel shading, an engine climate
  tint, and a rim light — not normal maps, which AI drafts cannot produce consistently.
- **The chest has no approved mockup** — `design/hud-menus/` covers everything else. When the HUD
  plan reaches it, settle where the close ✕ lives: in couch co-op the Hero tab hides it with the
  sack half, so a mouse or touch player has no visible way out (B, Esc and Start still work).
  The strip's right end collides with the hero panel's LEVEL readout (G13b's review).
  Also F5's scroll bar: at 16:10 (Steam Deck's 1280×800) the solo grid is width-bound and the bar
  hangs ~12px into the pad's right margin — cosmetic; the restyle places it properly.

## Later — M13 and Early Access readiness

From Groundwork (G4's review):
- **Rebinding (M13):** runtime binding overrides are not copied into each seat's copy of the
  controls — the rebinding work must apply them to every seat.
- **Steam Input may expose a PlayStation pad twice** (raw and virtual) — check before Early Access.
- **Seat edge cases accepted in G5** (never a lockout): Player 1's own pad sleeping at character
  select un-homes them until they press again; two identical serial-less pads asleep at once can
  wake swapped; a pad that returns as a brand-new device (not a reconnect) loses its seat until the
  title; the Gameplay-scene-alone stand-in follows the lowest-id pad (dev only).
- **Front-door edge cases accepted in G6:** a game started by mouse homes nobody, so the first press
  at character select is Player 1's — a would-be Player 2's A readies Player 1 instead of joining;
  and two players pressing in the same frame can drop a join or eject Player 2 at chapter select
  (pre-existing loop order).
- **From G8's review:** one stuck button on any pad blocks everyone's A on the results screen (the
  pointer still works — per-player arming would fix it); only the settings menu's owner can close it,
  so if their pad dies the game stays paused; and the driver's catch-up loop does not re-check the
  pause, so a menu opened mid-frame lets a few more steps run (the same frame-step family as F1 and
  candidate F4 — Netcode to assess with them).
- **Dead helpers in `ChestScreen`** — `DetailFor`, `AppendStat`, `Abbreviate`, `LabelFor` have no
  callers (G11's review). Remove in a later tidy-up.
- **Before any press, the prompt guess prefers a PlayStation pad** (it streams reports) over an idle
  keyboard — cosmetic; it corrects on the first press.
- **From G12 (for M9's HUD):** with both players at one drop, the loot card names only the first
  player's button; the front door's body text still draws in the built-in font (only its badge
  row uses the project fonts).
- **Watch — `TargetRegistry.Ordered` hands out its own live list** (F1's review). Anything that
  despawns mid-walk — a self-destructing enemy, a reaction that despawns what it kills (reactions
  land by M10) — brings F1's skip back in `StepEnemies` or `StepStatuses`. Collect first, or walk a
  snapshot. Also: an `EnemyDied` handler that throws would consume a step's corpses unsettled (only
  StageRunner's counter listens today). Netcode: both matter to M8's exact-order host.
- **M11 (Endless), from M8 Task 93:** the guest removes an enemy when it's absent from a snapshot;
  past 256 live enemies (the list cap) that would delete living ones. Give enemies an explicit
  removal event, as drops got, if Endless can pass the cap.
- **Chest scrolling by mouse wheel or touch drag** (F5's not-done list) — with M13's pointer work:
  whether the view detaches from the cursor or drags it along is that work's design call.
- **Ultrawide couch co-op:** the doll columns run into the stats panel and now draw over it (G13b's
  review) — outside the supported layouts; look again if ultrawide becomes one.

## Pending commits

None.

## Git

`main` · pushed through: see `git log origin/main` — the orchestrator pushes after every commit

## Log

- 2026-09-25 — Task 96 Step 1: EditMode 791, PlayMode 65 at f393d98. The Builder drafted Plan 1's
  build log and close-out notes (builder.md) and is ready to compact.
- 2026-09-25 — Task 95 committed (f393d98): a recorded fight replays into a real guest frame by
  frame; a remote Player 2 finishes the chapter; the guest's own menu stays off the wire (D57
  amended); drop ids left the snapshot (protocol v2). EditMode 791, PlayMode 65. **M8 Plan 1's
  code is complete**; Task 96 is Michael's pass and the close-out. Checklists merged into one sitting.
- 2026-09-25 — Task 94 committed (3d567b1): the guest streams the host's stages; the launch and the
  airlock wait for it; hand-overs carry the host's step. EditMode 785, PlayMode 61.
- 2026-09-25 — Task 93 committed (c8744bc): replica mode — the guest draws the host's world; drops
  leave on DropRemoved; PlaybackTransport (from 95) drives the first real guest-scene tests.
  EditMode 784, PlayMode 55. Michael's Stage B checks added.
- 2026-09-25 — Task 92 committed (07a8c64): the host sends 30 Hz snapshots and step-stamped reliable
  events; bursts split across messages; lists capped (nearest drops kept). EditMode 772, PlayMode 47.
- 2026-09-25 — Task 91 committed (5be281c): ids and the snapshot model; every travelling struct
  under a field-coverage test (now swap-proof). EditMode 767.
- 2026-09-25 — Task 90 committed (d00c552): Multiplayer Play Mode 2.0.2. Michael's Stage A check
  saved to `m8-plan1-pass.md`.
- 2026-09-25 — Task 90: Multiplayer Play Mode added. Step 4's clone-side Join needs a click the
  bridge cannot make and Michael declined screen control, so it folds into his step 6 checklist.
  Quiet on and off again.
- 2026-09-25 — Task 89 committed (fd33bea): the session and handshake — a remote Player 2, the dev
  panel. EditMode 746, PlayMode 43. Review saved Michael's save twice (a replica never saves; the
  stand-in hero never enters the session). Flagged items placed under M8.
- 2026-09-25 — Task 88 committed (5fd0920): the input buffer and the remote command source.
  EditMode 736, PlayMode 35. Seven gaps found against the plan; calls recorded under M8.
- 2026-09-25 — Task 87 committed (4f42e0c): the transport seam — loopback (with the socket's frame
  limit), lag simulator, local socket. EditMode 723. Busy-port handling lands in 89.
- 2026-09-25 — **M8 begins.** Task 86 committed (5e586a4): the wire in Core/Net — bytes, the stick,
  the command packet. EditMode 708. The loopback transport will enforce the socket's frame limit
  (87); the Events batch gets bounded or split at 92.
- 2026-09-25 — F5.2 committed (cc7a9fe): the chest grid scrolls; the menu anchors to the drawn cell;
  X and Y refuse an undrawn cursor; a bar shows there is more. EditMode 694, PlayMode 35. **The
  pre-M8 batch is done (F1–F3, F5). M8 Plan 1 starts.** Checklist item 16 added.
- 2026-09-25 — F5.1 committed (2610ef0): the chest's navigation model owns the scrolled view. EditMode
  694. F5.2 (the drawing) next.
- 2026-09-25 — F5's plan approved (ded6e1e): the view lives in `ChestNavigation`; the grid draws
  from it; X and Y refuse an undrawn cursor; a thin bar shows there is more. Item 12 sized (~⅓ of F5).
- 2026-09-25 — F3 committed (5abd671): each launch draws its own seeds onto the session; the driver
  takes them through `UseSeeds`, M8's host door. EditMode 689, PlayMode 34. F1–F3 done. The
  Builder writes F5's plan (`2026-09-25-f5-chest-grid-scrolling.md`); Netcode to reopen for M8.
- 2026-09-25 — F1 committed (15778dd): the death pass collects, then settles, so same-step deaths
  settle together in registry order. The plan's F1 carries a revision note.
- 2026-09-25 — F2 committed (724d44e): the DEBUG grant row compiles out of a release (proven by a
  release compile read with Cecil). EditMode 686, PlayMode 32.
- 2026-09-25 — F1's premise didn't hold (Destroy's OnDisable is immediate in this Unity); reshaped to
  the death pass's skip-the-next-enemy bug (option A). F2 runs alongside.
- 2026-09-25 — **Groundwork built** (G1–G13b, 56e4be5..527df2b); G14 applied D57's corrections and
  the shared-doc updates (5f3799a). Michael's controller pass is queued. The Builder starts F1–F3.
- 2026-09-25 — G13b committed (527df2b): the SACK/HERO strip stays up on the couch Hero tab with a
  cursor; item menus draw over the panels they overhang; the couch worn-gear menu now shows
  (A was spending coin on rows nobody could see). EditMode 680, PlayMode 28. Close ✕ → M9.
- 2026-09-25 — G13 committed (7c3014f): the grid's top follows the filter chips (couch co-op);
  solo Up from the filter row stays put. EditMode 680, PlayMode 28. Its review found three
  pre-existing chest bugs: the Hero-tab strip focus and the popover under the compare panel
  become G13b (both on Michael's G14 route); past-40-stacks becomes F5.
- 2026-09-25 — G12 committed (ad189cb): every other prompt in the player's own buttons;
  Start on the title only; results dwell 0.75 s; the front door loads its fonts. EditMode 679,
  PlayMode 28. G14's checklist gains: A can't skip the results for ~¾ s; Start does nothing at
  character or chapter select; with Player 1 on the keyboard the join line says only A.
- 2026-09-25 — Art `READY TO COMPACT`; its lane file committed (9902064). Netcode and World
  lane files as they closed, committed (35f5441).
- 2026-09-25 — PROTOCOL rule 11 (Michael): blocked or out-of-work lanes, the orchestrator included, are
  readied to compact (`PREPARE TO COMPACT` → `READY TO COMPACT`). Orchestrator lane file added.
- 2026-09-25 — Michael restarted the Unity bridge; the Builder continues Task 12.
- 2026-09-25 — Task 12 code in; the Unity bridge jammed after a synchronous test run timed out.
  Waiting on Michael to restart it.
- 2026-09-24 — G10 (8502289), **G11 committed**: the chest and hero footers are badge rows in the player's
  own buttons (keyboard caps and pad badges captured).
- 2026-09-24 — G9 committed (61e5366). Art round 2: storybook fixture kit v2; terrier redo parked
  until Michael sets a pet direction.
- 2026-09-24 — **G8 committed**: settings and results on the new map; Escape/Start never double-fire.
  PlayMode 27. Results gets a short minimum time on screen in Task 12. Art round 1 done; D16 gains
  Michael's look (storybook world, simple pets).
- 2026-09-24 — **G7 committed**: the chest on the new map. EditMode 672, PlayMode 23. A pre-existing solo
  bug (Up from the filter row enters an invisible tab strip) rides Task 13. Art: 49 effect pieces and
  5 fixture pieces cut from Michael's second batch (d5c5ff0).
- 2026-09-24 — **G6 committed** (4b602b7): the front door seats the couch; Player 2 joins on the pad they
  press A on. PlayMode 18. D57 gains "Start starts the game on the title only".
- 2026-09-24 — **G5 committed** (3e6b3bb): PlayerInput gone, the seat is the player's id (the solo "P2"
  fix), sleeping controllers keep their seat, project-wide actions cleared. EditMode 667, PlayMode 15.
  Art: the knight cut into 15 rig parts from Michael's first ChatGPT batch (1e0661c).
- 2026-09-24 — G2 (c73ef97), G3 (66e8492), G4 (aaeaa1f) committed. D57 amended (08f7c40): Player 1's
  home is the device they came into character select on; seats survive a controller reconnect.
- 2026-09-24 — G2 committed (c73ef97). Art back online: HUD canvas approved and exported to
  `design/hud-menus/`; 31 GPT prompts queued in `ArtSource/GPT_PROMPTS.md`.
- 2026-09-24 — Unity open; Builder baseline EditMode 626 / PlayMode 15. **G1 committed** (56e4be5) — also
  fixed Pause reusing Heavy's binding ids. The orchestrator restarted mid-task; the Builder's
  unreachable-orchestrator fallback worked (messages parked in its lane file). Netcode, World, Art
  sessions closed — restart them from `PROMPTS.md` when wanted.
- 2026-09-24 — Netcode `DONE`: pre-M8 fixes plan, F1–F3. The Builder's queue is fully planned:
  Groundwork → F1–F3 → M8 Plan 1.
- 2026-09-24 — Netcode `DONE`: M8 Plan 1, Tasks 86–96 (1a7fdac). Next for Netcode: the pre-M8 fixes plan.
- 2026-09-24 — Art: fixture kit brief (80b5030); HUD and menus canvas v1 up for Michael's review.
- 2026-09-24 — Art `DONE`: audio v0 (b144d79); first pass complete. Approved: a minimal story-free
  fixture environment kit; next, HUD and menu mockups (ROADMAP §5.2 item 6). Licensed-audio question
  → Michael's queue.
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
