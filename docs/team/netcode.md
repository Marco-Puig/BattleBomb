# Lane: Netcode

**Mission:** get **M8 — online co-op (D54)** ready to build the moment Groundwork lands: know what
the engine already gives online play and what it lacks, lay out the real options, run M8's design
session with Michael, then write the spec and the implementation plan. You design and plan; the
Builder builds (unless the orchestrator hands you the sim).

**Owns:** `docs/team/netcode/` (research, options, drafts, spikes) · `docs/HANDOFF-M8.md` (new) ·
M8 plan files under `docs/superpowers/plans/` (new) · this file. **No writes under `Assets/`.**

**Read first (in order):** `CLAUDE.md` · `docs/team/PROTOCOL.md` · `docs/team/BOARD.md` · this file
· `docs/ROADMAP.md` §3 and §4 "M8" · `docs/DECISIONS.md` D10, D11, D42, D51, D52, D53, D54 ·
`docs/ARCHITECTURE.md` · `docs/HANDOFF-M7.md` close-out notes · the Groundwork plan
(`docs/superpowers/plans/2026-09-24-groundwork-input.md`) — it replaces the input layer M8 builds on.

---

## The work, in order

1. **Readiness audit** → `docs/team/netcode/readiness.md`. What D10 already bought (commands, the
   fixed step, the player registry, no singletons) and what each topology would still need. Read the
   real code: `SimulationDriver`, `PlayerRegistry`, `IPlayerCommandSource`, `PlayerCommand`,
   `CharacterActor`, `DeterministicRandom` and its streams, `PlayerInventory`'s request surface, the
   solo pause, `SaveMapper`. Name every place state lives, every source of non-determinism
   (floats, Unity physics, `Time`, iteration order), and every UI action that mutates state.
2. **Options memo** → `docs/team/netcode/options.md`. Topology (host-authoritative vs lockstep or
   rollback), transport and lobbies (Steam networking and friend invites; which Steamworks wrapper;
   whether Unity's Netcode packages fit a custom simulation or state sync is written over the
   command model), and test tooling (Multiplayer Play Mode, Steam Playtest, a dev app id). Use
   Context7 for current library docs — do not work from memory. End with a recommendation and why.
3. **The design session** → with Michael, in this session. Agenda: the questions in `ROADMAP.md`
   §4 "M8". One decision at a time, your recommendation first, plain language (he is not
   technical). Record every answer in this file. Send the orchestrator the draft `D` entries.
4. **Spec and plan** → `docs/HANDOFF-M8.md`, then an implementation plan with
   `superpowers:writing-plans`, in the style of the Groundwork plan: exact files, full code, tests
   (two players always — CLAUDE.md), gates, one `DONE` per task.

**Spikes** that need Unity (a latency test, a Steam lobby smoke test) need the sim: `LOCK REQUEST`
with the reason and a time estimate. Draft spike code under `docs/team/netcode/spike/` first.

---

## Waiting on

- **Orchestrator: COMMITTED + D numbers** for the design-session DONE (sent 2026-09-24). Paths:
  `docs/team/netcode/decisions-draft.md` (new), `docs/team/netcode.md`. Asked: record Dα–Dε; note
  Steam app at close-out in ROADMAP; collaborator availability on Michael's queue. Do not edit
  `decisions-draft.md` until COMMITTED.

## Current state

Steps 1–2 done (readiness committed `bdc1f58`; options DONE sent). Step 3: the design session with
Michael, in this session, one decision at a time, recommendation first. Agenda order: topology →
netcode layer + Steam wrapper (Claude recommends, Michael agrees) → how couch and online mix →
join point → screens online → saves online (who holds guest gear / what guest keeps / leaving) →
feel → test tooling + Steamworks account timing. Record each answer below as it lands.
**Session place: all eight decisions answered; draft D entries sent.** Next: present the design to
Michael in sections for approval (brainstorming skill: architecture → the wire → the guest's machine
→ joining/leaving → saves → testing), then write `docs/HANDOFF-M8.md`, self-review, Michael reviews,
then `superpowers:writing-plans`. Then send DONE for readiness.md and go
straight to the options memo (orchestrator: no need to wait for COMMITTED).

Research already done (Context7 + release pages, 2026-09-24): Steamworks.NET 2025.164.1 (Aug 2025,
SDK 1.64, UPM git URL `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1`,
no MonoBehaviour wrapper included); Facepunch.Steamworks 2.5.2 (Apr 2024 — two years stale, zip
install, nicer async API: lobbies, InviteFriend, OnGameLobbyJoinRequested, relay SocketManager);
NGO 2.11 has no full prediction/reconciliation ("client anticipation" only), own tick (default 30),
CustomMessagingManager for raw messages, Steam only via community transports; Multiplayer Play Mode
2.0.x on Unity 6000.4+, up to 4 editor instances (needs a non-Steam transport); ISteamNetworkingMessages
is connectionless P2P to a SteamID over Valve's relay. `steam_appid.txt` = 480 (Spacewar).

## Next steps

1. Finish reading the sim code; write `docs/team/netcode/readiness.md`.
2. Options memo (Context7 for Steamworks.NET / Facepunch / Netcode for GameObjects / NGO transports).
3. Design session with Michael.

## Answers and decisions

- 2026-09-24 (Michael, M8 design session, decision 1): **Topology — host-authoritative.** The host's
  PC runs the one real simulation; the guest sends commands and draws what the host sends; the
  guest's own character is predicted locally. Chosen over lockstep and rollback (options.md §1).
- 2026-09-24 (Michael, decision 2): *"Whatever is the best free option for mobile."* Recorded as:
  **our own thin netcode layer (free), with the transport behind a seam.** Steam (Steamworks.NET)
  is the PC Early Access transport; a cross-platform transport — Epic Online Services is free,
  cross-platform, with authenticated P2P (checked in Epic's docs 2026-09-24) — slots in when mobile
  arrives, and could enable PC↔mobile crossplay later (not an M8 question). **Design constraint from
  this answer: nothing above the transport seam may assume Steam** (ids, lobbies, invites all behind
  Platform interfaces). Told Michael the interpretation; he can object.
- 2026-09-24 (Michael, decision 3): **Two in total** — solo, a couch pair, or one player per PC.
  A couch pair cannot also take an online guest. D11 unchanged.
- 2026-09-24 (Michael, decision 4): **Join at character select AND at checkpoint rooms** (chosen over
  my recommendation of character select only; mid-fight drop-in rejected). M8 scope therefore
  includes: a full world snapshot sent to a newcomer, the guest's save loaded into a live run
  (activating the second player object mid-run — today `SessionBinder.Awake` deactivates an empty
  slot for good), the stage scene(s) streamed to the guest. Rule I told Michael: a join request that
  arrives mid-fight waits until the host's run is at a checkpoint room (`StagePhase.AtCheckpoint`);
  the guest appears at that room's respawn point.
- 2026-09-24 (Michael, decision 4b): **Solo games are open to friends by default** — friends see
  "Join game", the host can invite from the overlay any time, a setting turns it off. Couch games are
  full and never open. (Write it platform-neutral: "platform friends", Steam's today.) Consequence:
  a solo game becomes a two-player online game mid-run, so the solo rules (chest pause, camera,
  layout) must switch live when a guest arrives or leaves.
- 2026-09-24 (Michael, decision 5): **Nothing pauses online** — chest, shop, hero panel, settings
  all full-screen on the player's own display with the world live; the player stands idle. Solo and
  couch keep today's rules; a solo game switches to the online rule the moment a friend drops in.
  (Implied, from options §7: each display lays out for its own local players; the host drives the
  whole-session moments — results, return to chapters, launch.)
- 2026-09-24 (Michael, decision 6a): **The host's PC holds the guest's sack and gear during the
  match.** The guest's character + stash travel to the host at join; two stashes in the simulation
  online (one per save); guest menu actions are requests with a round trip (~0.1 s) before redraw.
- 2026-09-24 (Michael, decision 6b): **The guest keeps loot, gold, XP, levels, gear AND chapter/tier
  credit** for chapters finished with the host, written on the guest's own PC at D52's autosave
  moments. The resume point stays the host's. The host's save gates what can be launched; a guest
  may help above their own unlocks. Leaving: host quits/drops → guest keeps everything up to the
  last autosave (same as a crash); guest leaves → host carries on solo (D25's solo rules from then).
- 2026-09-24 (Michael, decision 7): **Predict the guest's movement and swings** — run, jump, facing,
  and the start of every attack/cast react instantly on the guest's PC; hit confirmation, damage
  numbers, enemy reactions arrive a round trip later. Built in two steps: interpolation-only first
  (measure real connections), then prediction on top.
- 2026-09-24 (Michael, decision 8a): **Steamworks account and app ID at the M8 close-out** (chosen
  over my recommendation of "when the Steam task starts"). All M8 development on app 480
  (Spacewar); Steam Playtest is not available until the close-out. Risk to surface in the spec:
  Valve's onboarding (tax/bank/identity) may take days, so the close-out should start that paperwork
  early enough not to block the two-PC pass. ROADMAP §5.3 says "set up during M8" — orchestrator to
  note the timing.
- 2026-09-24 (Michael, decision 8b): **The collaborator is the remote tester** (from their own home,
  over the real internet). No second PC at home. On app 480 the collaborator needs a build zip with
  `steam_appid.txt`, and Steam running — the plan must include a way to hand builds over.
- 2026-09-24 (orchestrator, COMMITTED bdc1f58): audit findings scheduled — deferred-Destroy bug,
  DEBUG grant, constant seeds → Builder batch after Groundwork, before M8. **Shop rack → MINE: the
  M8 spec and plan must say "the rack lives in the simulation; buy = buy rack slot N".** Seeds: M8
  only needs to say the host owns the seed. Same-hero couch save → Michael's queue, not M8.
- 2026-09-24 (Michael): online co-op ships in Early Access (D54), chosen over Remote Play Together
  and couch-only. D11 still caps the game at two players; how couch and online mix is M8's call.

## Log

- 2026-09-24 — readiness.md complete; DONE sent. Flagged out-of-lane: deferred-Destroy multi-step
  bug (enemies act after a wipe below 60 fps), DEBUG grant in release, shop rack rolled in the UI,
  constant RNG seeds (+ same-hero couch save collision).
- 2026-09-24 — Delivery notice: my resent ONLINE was *held for the orchestrator user's approval and
  expired* (the orchestrator runs in a different permission mode). The orchestrator nonetheless
  answered "ONLINE received". **Messages from this lane may be held; mirror every DONE/QUESTION at
  the top of this file under Waiting on** so it is seen either way.
- 2026-09-24 — ONLINE resent to the orchestrator's from= address. **Session names keep changing:
  always reply to the `from=` address on the orchestrator's latest message** (PROTOCOL §6 updated).
- 2026-09-24 — Lane seeded by the orchestrator.
