# Draft decisions from the M8 design session

**Netcode lane · 2026-09-24.** For the orchestrator to number and record in `docs/DECISIONS.md`.
Numbered here as **Dα–Dε**; D57 is taken by the Groundwork plan. Michael's answers, verbatim where
he gave words, are in `docs/team/netcode.md` → *Answers and decisions*. Evidence:
`docs/team/netcode/readiness.md`, `docs/team/netcode/options.md`.

---

## Dα — Online runs on the host's machine, over our own layer · **Locked** *(amends D10; cashes in D54)*

Settled in the M8 design session with Michael (2026-09-24).

- **Host-authoritative.** One player's PC — the host — runs the one real simulation, exactly as it
  runs today. The guest's device becomes `PlayerCommand`s sent to the host, where a
  `RemoteCommandSource` feeds them in like any local pad (D10's promise, cashed). The host sends the
  guest world snapshots and simulation events; the guest's machine draws them and never rolls,
  resolves or decides anything. The host owns every RNG seed.
- **Our own thin netcode layer**, not a library: a message codec, snapshots, a replica mode for the
  guest, an input buffer, and own-character prediction (Dε). The simulation already owns its clock,
  state, order and ids; a library would make us translate it into someone else's.
- **The transport sits behind a Platform seam.** Steam (via Steamworks.NET, a UPM package pinned to
  a tag, in its own assembly) is the PC Early Access transport: friends-only lobbies, overlay invites,
  P2P over Valve's relay, Steam Cloud as D52's second `ISaveStore`. An in-memory loopback and a plain
  UDP transport exist for tests and two-editor development. **Nothing above the seam may assume
  Steam** — Michael: *"Whatever is the best free option for mobile."* A free cross-platform
  transport (Epic Online Services is the candidate) slots in when mobile arrives (D5). Rule 6
  stands: a build without Steamworks compiles and runs.

**Why host-authoritative:** it needs no bit-identical maths across two PCs (the audit counted 588
`float`s across 62 Core files and `Mathf.Pow` in the XP curve and prices), it is untouched by the ten
live sources of divergence the audit found (stage loads landing at machine-dependent moments above
all), and it keeps the host's game exactly as it plays today. Cheating is irrelevant to two friends
co-operating.

**Rejected:** lockstep (input delay on both players, plus determinism forever); rollback (all of
lockstep's cost plus whole-world save/restore); Netcode for GameObjects (per-object sync on its own
tick, no full prediction/reconciliation); FishNet (the strongest library — kept as the fallback if
our layer stalls); Photon Fusion/Quantum (paid per concurrent player past 100, and a rewrite into
their model); Facepunch.Steamworks (last release April 2024).

---

## Dβ — Two players, any shape; friends drop in at character select and checkpoint rooms · **Locked** *(extends D11, D51)*

Settled with Michael (2026-09-24).

- **Two in total** (D11 unchanged): solo, a couch pair on one PC, or one player on each of two PCs.
  A couch pair cannot also take an online guest.
- **A guest joins at character select, or at a checkpoint room mid-run.** A join request that
  arrives mid-fight waits until the host's run is at a checkpoint room; the guest appears at that
  room's respawn point. Mid-fight drop-in is rejected. (Michael chose the checkpoint rooms over the
  recommended character-select-only.)
- **A solo game is open to the host's platform friends by default** — they see "Join game", and the
  host can invite from the overlay at any time. A setting turns it off. A couch game is full and never
  open.
- **The host's save decides what can be launched**; a guest may help in a chapter above their own
  unlocks.

**Consequence:** a solo run can become a two-player online run mid-chapter, so every solo rule — the
chest pause, the camera, the chest layout — switches live when a guest arrives or leaves. M8 must
send a whole running world to a newcomer and load their save into a live run.

---

## Dγ — Online, nothing pauses · **Locked** *(extends D42)*

Settled with Michael (2026-09-24).

- **No screen stops the world online** — chest, shopkeeper, hero panel, settings. Each opens
  full-screen on its own player's display (D42's online rule, extended); the player at a screen stands
  idle, as a couch player at a chest does now.
- **Each display lays out for its own local players**, not for how many characters exist — online
  that is always one, so full-screen chest and solo camera.
- **The host drives the whole-session moments:** leaving results, returning to chapter select,
  launching. The guest's screens follow.
- Solo and couch keep today's rules. A solo game switches to this rule the moment a guest drops in.

**Rejected:** settings pausing both players ("Paused by Player 2") — either player could stop the
other's game at will.

---

## Dδ — Online saves: the host holds the run, each player keeps what they earned · **Locked** *(amends D51; extends D52)*

Settled with Michael (2026-09-24). D51 left "reconciling two saves' progress" to the networking
milestone; this is that reconciliation.

- **The host's machine holds the guest's gear during a match.** At join the guest's character and
  stash travel to the host; the host's simulation owns them for the run. Online there are therefore
  **two stashes in the simulation**, one per save; the couch keeps one shared stash (D51 unchanged
  for the couch). Auto-sell and auto-equip travel with their stash — they are saved state. The guest's
  menu actions are requests that name the player and carry a sack revision; the host refuses one
  aimed at a sack that has since changed.
- **The shopkeeper's rack lives in the simulation**, rolled by the host; buying names a rack slot.
  (Today the menu rolls and prices it — a rule 2 violation the audit found.)
- **The guest keeps everything they earned** — loot, gold, XP, levels, allocations, worn gear — and
  **credit for chapters and tiers finished with the host**, written on the guest's own machine at
  D52's autosave moments from the state the host sends. The resume point stays the host's.
- **Leaving:** the host quits or drops → the guest keeps everything up to the last autosave moment,
  exactly what a crash costs (D52), and returns to the title. The guest leaves → the host carries on
  solo, with D25's solo rules from that step.

**Rejected:** the guest holding their own gear with the host keeping a mirror (instant menus, but two
machines deciding at every grab whether the sack has room); loot-and-XP-only (a helper made to replay
a chapter alone to unlock it).

---

## Dε — The guest's own character is predicted · **Locked**

Settled with Michael (2026-09-24).

- **The guest's movement and swings react instantly on their own machine:** running, jumping,
  facing, and the start of every attack and cast are predicted locally with Core's own
  `CharacterMotor` and `CombatMachine`, then corrected to the host's answer when it arrives.
- **Everything the host decides arrives a round trip later** — whether a swing hit, damage numbers,
  knockback from enemies, and everything about enemies.
- **Built in two steps:** interpolation only first, to measure real connections; prediction on top.

**Rejected:** movement-only prediction (attacks feel late); no prediction (the guest's own character
visibly behind their presses).

---

## Not decisions, but for the record (ROADMAP §5.3 / §4 M8)

- **The Steamworks account and app ID are created at M8's close-out** (Michael, over the
  recommendation of "when the Steam task starts"). All M8 development runs on Valve's test app 480
  (`steam_appid.txt` already holds it); Steam Playtest waits for the close-out. Risk: Valve's
  onboarding paperwork can take days, so it should start early enough not to block the two-PC pass.
- **The collaborator is the remote tester**, from their own home; there is no second PC.
