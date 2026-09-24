# M8 options memo — how online co-op gets built

**Netcode lane · 2026-09-24.** Builds on `readiness.md` (what the code gives us). Library facts
checked against current docs this session (Context7, release pages, Valve's partner docs) — sources
at the end. Every recommendation here is a proposal for the design session; nothing is decided
until Michael decides it.

---

## The recommendation in one paragraph

**One player's machine runs the game (the host); the friend's machine sends its button presses
and draws what the host sends back (the guest).** We write that layer ourselves — it is small,
because D10 already built the half that is usually hard — and carry it over **Steam's own
networking**, reached through **Steamworks.NET**, behind a seam so a build without Steam still
runs. The guest's own character is predicted on their machine so it feels immediate; everything
else — hits, damage, loot, enemies — is decided by the host. Two players in total, as D11 says:
a couch pair, or one on each PC. Friends join at character select. Online, no screen pauses the
world. Each player's own save keeps what they earned.

---

## 1. Topology — who runs the game

### The three real options

**A. Host-authoritative ("the host runs it").** The host's machine runs the one real simulation,
exactly as it runs today. The guest's controller becomes commands sent to the host, where a
`RemoteCommandSource` feeds them in like any local pad. The host sends the guest a picture of the
world ~20–30 times a second plus a stream of events (a hit landed, an enemy died, a drop appeared),
and the guest draws it.

**B. Lockstep ("both run it, in step").** Both machines run the full simulation and exchange only
commands. Nobody moves until both machines have both players' commands for that step.

**C. Rollback ("both run it, and rewind").** Like B, but each machine guesses the other player's
input, runs ahead, and when the real input arrives and differs, rewinds and re-runs the last few
steps. Fighting games use it.

### How they compare, for this game

| | A. Host-authoritative | B. Lockstep | C. Rollback |
|---|---|---|---|
| **Host's feel** | Exactly today's. | Input delay on every press (≈ half the round trip). | Immediate. |
| **Guest's feel** | Their own character predicted → immediate; hits confirmed a round trip later. | Same delay as the host. | Immediate; the partner and enemies occasionally snap. |
| **Needs identical maths on two PCs** | **No.** | **Yes, forever.** | **Yes, forever.** |
| **Stage loading (readiness §3.4)** | Harmless: the host decides; the guest follows. | Every load needs a both-machines barrier and must stop mutating state on arrival. | Same as B. |
| **Menus, pause, the save** | Requests to the host. | Every one becomes a stepped command. | Same as B, plus they must survive a rewind. |
| **A bug looks like** | The guest sees something wrong; the host is still right. | A silent desync, found minutes later by a checksum. | Same as B. |
| **What the code already has** | Commands, registry, fixed step, value-type state (readiness §1–2). | Same, plus seeded RNG. | Same as B. |
| **New work unique to it** | Snapshots, replica mode, own-character prediction. | Determinism hardening, desync hashing, stepping every side door. | All of B, plus whole-world save/restore and re-simulation, plus effects that do not fire twice. |
| **Cheating** | Host can cheat. Irrelevant: two friends, co-op, no leaderboards in EA. | Hard to cheat. Irrelevant. | Same. |

### Recommendation: A

The case against B and C rests on two facts from the audit. **Every position, velocity and damage
number in the game is a `float`** — 588 of them across 62 of Core's 101 files, plus `Mathf.Pow` in
the XP curve and the price book, which is not guaranteed to give the same bits on every machine
(readiness §4.3 item 8) — and lockstep needs the two PCs to agree on every bit of every one of them
on every step, for the life of the game, including code nobody has written yet. And **ten separate
things diverge today** (§4.3), from stage loads to menu key-repeat; lockstep has to fix all ten,
host-authority three (and one of those is a bug worth fixing anyway). Lockstep can be done; it is
also the classic way a small team loses months. A keeps the host's game exactly as it plays today
and turns every side door into a message instead of a redesign.

The honest cost of A is that **the guest is a round trip behind the host** on anything the host
decides. For co-op against computer enemies that is the usual trade, and prediction (§6) removes it
from the thing the guest feels most — their own movement.

**Rejected:** B — input delay on both players *and* the determinism burden, for no benefit a
co-op game needs. C — the best feel on paper, the most work in practice: it needs everything A
needs (a full world snapshot, for rewinding) *plus* everything B needs.

---

## 2. The netcode layer — build it, or adopt a library

| Option | What it gives | Why it fits or does not |
|---|---|---|
| **Our own thin layer** (recommended) | Exactly what A needs: send commands up, snapshots and events down. | Our simulation already owns its clock, its state, its order and its ids. The layer is a message codec, a snapshot writer/reader, a replica mode, and a small input buffer. Bandwidth is trivial: two players and ~20 enemies at ~40 bytes each is under 1 KB a snapshot, ~25 KB/s at 25 Hz, with no compression. |
| **Netcode for GameObjects 2.11** (Unity) | Connection management, scene sync, `NetworkObject`/`NetworkVariable` replication, RPCs, its own tick (default 30 Hz), `CustomMessagingManager` for raw messages. | Its model is "each object replicates its own fields on NGO's tick". Ours is "one fixed-step driver owns everything". Using it properly means re-declaring every actor's state as network variables and reconciling two clocks; using it only for `CustomMessagingManager` pays for the whole package to get a message pipe Steam already gives us. **It has no full prediction and reconciliation** — the docs offer "client anticipation", a simplified correct-if-wrong model. Steam only via a community transport. |
| **FishNet** (community, free, no player-count limits) | Server-authoritative, with real client-side prediction: `[Replicate]` / `[Reconcile]` methods on each `NetworkBehaviour`, run on its own `TimeManager` tick. | **The strongest library alternative**, and the one to reach for if our own layer stalls. It still assumes each networked object predicts and reconciles itself on FishNet's tick; ours has one driver stepping everyone in a designed order. Adopting it means handing our clock to its `TimeManager` and splitting the player's step into its replicate shape — a translation, not a drop-in. (Mirror, the other big community library, was not evaluated beyond this.) |
| **Photon Fusion 2 / Quantum** (commercial) | Fusion: host mode with prediction and lag compensation. Quantum: deterministic rollback. | Both route through Photon Cloud with per-player-count pricing past the free 100 CCU (then from $125/month), and both want the game written in their model — Quantum means rewriting the simulation in its deterministic ECS. Not Steam's relay. |

**Recommendation: our own layer.** The engine's architecture *is* a netcode architecture already;
a library would make us translate it into someone else's. The risk of writing our own is the usual
one — connection edge cases, reliability — and Steam's networking handles the hard parts of both
(§3).

---

## 3. Transport, lobbies and the Steam wrapper

### What Steam gives, free

- **Friends-only lobbies** (`ISteamMatchmaking`) — create, invite from the Steam overlay, and a
  callback when a friend clicks "Join game" on the host.
- **Peer-to-peer connections to a Steam ID over Valve's relay** (`ISteamNetworkingSockets`, P2P
  mode) — NAT traversal handled, IPs hidden, reliable and unreliable sends, connect/disconnect
  events. (There is also `ISteamNetworkingMessages`, a connectionless variant; the connection-based
  one is the better fit because "the host left" is an event we must act on.)
- **Steam Cloud** (`ISteamRemoteStorage`) — the second `ISaveStore` D52 planned.
- **Rich presence**, so "Join game" appears on the host in the friends list.

### Which C# wrapper

| | **Steamworks.NET** (recommended) | Facepunch.Steamworks |
|---|---|---|
| Latest release | 2025.164.1, Aug 2025, Steamworks SDK 1.64 | 2.5.2, **Apr 2024** |
| Install | Unity Package Manager git URL, pinned to a tag — one line in `manifest.json` | Zip of DLLs into `Assets/Plugins` |
| API | Thin, 1:1 with Valve's docs; we write our own init/callback MonoBehaviour | Friendlier C# (async lobbies, socket-manager classes) |

Steamworks.NET is the more current of the two, installs as a proper package, and maps 1:1 onto
Valve's documentation. Its rawness costs little: we call perhaps a dozen Steam functions, and all
of them sit behind our own interfaces.

### Where it sits (rule 6: Steam absent must still build and run)

- **Platform** gains the seams: an `INetTransport` (send reliable / unreliable, poll, connected,
  disconnected) and lobby calls on `IPlatformServices`. `NullPlatformServices` stays the default.
- A **separate assembly** (`BattleBomb.Platform.Steam`) is the only code that references
  Steamworks.NET, compiled only when the package is present. It holds `SteamPlatformServices`, the
  Steam P2P transport, and the Steam Cloud store.
- Two non-Steam transports: an **in-memory loopback** (tests), and **plain UDP on localhost/LAN**
  (Multiplayer Play Mode, two editors). Both take optional fake latency, jitter and loss, so feel can
  be tested on one PC.

---

## 4. How couch and online mix

D11 caps the game at two. That leaves three shapes, and the recommendation is simply all three:
**solo**, **a couch pair on one PC**, or **one player on each of two PCs**. A couch pair cannot also
take an online guest — that would be three.

*Open for Michael:* whether the host's couch seat 2 and "invite a friend" are offered side by side
at character select, or online is its own entry on the title.

---

## 5. When a guest can join

| | Character select only (recommended for M8) | Mid-run, drop-in |
|---|---|---|
| What it needs | Lobby → both pick → launch together. | A full world snapshot sent mid-play, the guest's save merged into a live run, a spawn rule, a stage scene loaded mid-fight. |
| Why now / later | Matches the couch today (M7: Player 2 joins at character select). | Host-authority makes it *possible* later — the snapshot is the same one A already needs — so it is deferred, not ruled out. |

---

## 6. Feel — keeping the guest's own character responsive

Two stages, both in M8:

1. **Interpolation only.** The guest sees everything, their own character included, ~100 ms
   behind the host. Built first because it is simpler and because it tells us, on real connections,
   exactly how much the second stage has to fix.
2. **Own-character prediction.** The guest's machine runs *its own character's* motor and combat
   machine locally — both are pure Core (`CharacterMotor`, `CombatMachine`), so they run anywhere —
   and corrects to the host's answer when it arrives, replaying the few presses since. Movement,
   jumping, facing and the swing's start feel immediate. **Hits, damage, knockback from enemies and
   everything about enemies stay the host's** and arrive a round trip later; that is the accepted
   trade.

---

## 7. Screens and pause online

D42 already chose it: online, a chest is full-screen on its own player's display and the world
stays live. Extended to everything:

- **No screen pauses the world online** — chest, shopkeeper, hero panel, settings. The player at a
  screen stands idle, exactly as a couch player at a chest does now.
- **Each display lays out for its own players.** Online, each PC has one local player, so the chest
  is full-screen and the camera behaves as it does solo — today both are keyed to how many
  characters exist, which would give the online player the couch half-screen (readiness §5.3).
- **The host drives the whole-session moments:** leaving the results screen, "return to chapters",
  launching. Today any player's press ends the results screen for everyone; online the guest's
  screen follows the host's.
- Solo and couch keep today's rules (solo chest pauses; settings pauses the couch).

---

## 8. Saves online

D51: each participant brings their own save. Three questions inside that:

**Who holds the guest's gear during play?** *Recommended: the host.* At launch the guest sends
their character and stash to the host; the host's simulation owns them for the run like everything
else (one authority, no split-brain over a full sack at the moment of a grab). The guest's menu
presses are requests to the host; the result comes back as the ordinary "bag changed" redraw.
*Alternative:* the guest holds their own gear and the host keeps a mirror — menus answer instantly,
but a grab has two machines deciding whether the sack has room.

Online there are therefore **two stashes in the simulation** — the host's save's sack and wallet,
and the guest's — where the couch has one (D51 unchanged for the couch). Auto-sell and auto-equip
travel with each stash, because they are saved state, not display settings (readiness §5.2). A
request carries a sack revision so the host can refuse one aimed at a sack that has since changed
(a grab landed, auto-sell fired). And the shopkeeper's rack moves into the simulation, which rolls
it — today the menu rolls and prices it (readiness §5.1), which cannot work across two machines.

**What does the guest's save keep?** *Recommended:* everything they earned — loot, gold, XP,
levels, allocations, worn gear — written on the guest's own machine at the same five autosave
moments (D52), from the state the host sends. Story completions: the guest gets credit for a
chapter and tier they finished with the host, so a friend who helped is not made to replay it
alone. The resume point stays the host's alone.

**What happens when someone leaves?** The host quits or drops: the guest keeps everything up to
the last autosave moment — exactly what a crash costs (D52's rule, unchanged) — and returns to the
title. The guest leaves: the host carries on solo; D25's solo rules apply from that step.

*Progress gating:* the host's save decides what can be launched (the host picks the chapter).

---

## 9. Test tooling

| Tool | What it proves | When |
|---|---|---|
| **EditMode** | Codec round-trips, snapshot write → read equality, the input buffer, prediction replay — two players always. | Every task. |
| **PlayMode, in-process loopback** | The real Gameplay scene hosted, Player 2 a `RemoteCommandSource` fed through the loopback transport by a scripted guest; and a replica run driven by recorded snapshots. | The wiring tripwire (D45), extended. |
| **Multiplayer Play Mode** (Unity, 2.0.x on 6000.4+) | Two editor instances on one PC over the UDP transport, with fake latency. The day-to-day loop. | From the first transport task. Steam cannot run twice on one PC, hence the UDP transport. |
| **App 480 (Spacewar)** on two PCs, two Steam accounts | Lobbies, invites and relay before Michael's own app ID exists. `steam_appid.txt` already holds 480. | From the Steam task. |
| **Steam Playtest** | A free child app of the real one; friend invites, key batches; same Steamworks features. | Once the app exists. |
| **Two real PCs over the internet** | The Done-when: the fixture chapter start to finish from both ends. | The close-out. |

Steam's Remote Play Together stays useful as a comparison and a fallback for playtests, at no cost.

---

## 10. What M8 needs from Michael

- **The design session** (next, in this session) — §1, §4, §5, §7, §8, one at a time.
- **A Steamworks partner account and app ID** ($100, refunded after $1,000 in revenue — ROADMAP
  §5.3, already planned "during M8"). Not needed
  to *start* — app 480 covers development — but needed before Steam Playtest and before the
  close-out.
- **A second PC or the collaborator** for the two-PC checks.

---

## Sources

- Netcode for GameObjects 2.11 — custom messages, transports, ticks, client anticipation:
  <https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.11/manual/advanced-topics/client-anticipation.html>
- Steamworks.NET releases: <https://github.com/rlabrecque/Steamworks.NET/releases>
- Facepunch.Steamworks releases: <https://github.com/Facepunch/Facepunch.Steamworks/releases>
- Valve, `ISteamNetworkingMessages` / `ISteamNetworkingSockets`: <https://partner.steamgames.com/doc/api/ISteamNetworkingMessages>
- Valve, Steam Playtest: <https://partner.steamgames.com/doc/features/playtest>
- Unity, Multiplayer Play Mode: <https://docs.unity3d.com/Manual/com.unity.multiplayer.playmode.html>
- FishNet prediction (`Replicate` / `Reconcile`, `TimeManager`): <https://github.com/firstgeargames/fishnet-documentation>
- Photon Fusion pricing and modes: <https://www.photonengine.com/fusion/pricing>, <https://doc.photonengine.com/fusion/v2/fusion-choose>
