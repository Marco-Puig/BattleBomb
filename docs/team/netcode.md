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

- **Orchestrator: COMMITTED for the options DONE** (sent 2026-09-24; mirrored here in case it was
  held). Paths: `docs/team/netcode/options.md` (new), `docs/team/netcode.md`. Subject: "Netcode:
  M8 options memo". Do not edit `options.md` until COMMITTED.

## Current state

Steps 1–2 done (readiness committed `bdc1f58`; options DONE sent). Step 3: the design session with
Michael, in this session, one decision at a time, recommendation first. Agenda order: topology →
netcode layer + Steam wrapper (Claude recommends, Michael agrees) → how couch and online mix →
join point → screens online → saves online (who holds guest gear / what guest keeps / leaving) →
feel → test tooling + Steamworks account timing. Record each answer below as it lands. Then send DONE for readiness.md and go
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
