# HANDOFF — M5B: the elements get their own twists

**Status — in build, 2026-08-20.** Directed by Michael with his collaborator, in a fork of the
M6 planning session. Decisions D46 (roster + signature casts, resolving O11) and D47 (2D
billboards) locked. This block lands **before** the M6 build; tasks are lettered 60A–60D so
M6's tasks 61–72 (`HANDOFF-M6.md`) keep their numbers.

---

## What M5B is

O11 closed: the roster is **Fire, Ice, Earth, Air**, and instead of four skins on one line
splash, each element's press cast becomes its own move (D46). Fire keeps the line and Burn;
Ice fires a long lane-bound bolt that chills; Earth erupts a short wall that stuns; Air
launches for the juggle. The billboard art direction (D47) is docs-only here — no art exists
yet, so there is nothing to convert.

Almost all of the machinery already exists: the launcher (`LaunchSpeed` → upward velocity,
pinned by the aerial tests), stuns (D41's reaction machinery, `ApplyStun` on both actor
types), player-owned projectiles (the bow's driver path), and the status track. The genuinely
new pieces are **a slow as a status kind**, **projectile delivery for casts**, **stun-on-hit as
an attack property**, and **the element owning the press cast**.

---

## Paper numbers

All dials. 60 steps = 1 s.

| Cast | Shape | Damage | Mana | Twist |
|---|---|---|---|---|
| **Fire — line** | The D39 splash unchanged: reach 3.2, depth-limited | 22 | 20 | Burn: 3 s, tick 0.5 s, 0.5 damage share (existing asset) |
| **Ice — bolt** | Projectile, speed 14 u/s, life 240 steps, lane-bound | 10 | 15 | Chill on contact: move ×0.55 for 3 s, no tick damage |
| **Earth — wall** | Reach 1.6, startup 14 steps | 30 | 25 | Stun 45 steps (0.75 s) on hit |
| **Air — gust** | Reach 2.4 | 12 | 20 | Launch speed 11 — the same pop as the L-L-H launcher |

- Aura and leap: unchanged from D39, still carrying the element's status.
- Earth and Air author **no status** for now (D46) — the definition's empty-status slot is
  already legal. Naming one later is editing an asset.
- Chill and enemy slows are two-way (D40): an Ice caster region slows players.

---

## Planning decisions

1. **The slow is a status kind, not a new system.** `StatusSpec`/`StatusInstance` carry a move
   scale beside the tick damage; `StatusTrack` exposes the strongest active slow; motors
   multiply it in. Reapplying keeps the stronger slow, mirroring the existing renewal rule.
2. **Stun-on-hit is an `AttackTuning` property** (default 0), applied through the same
   `ApplyStun` path reactions use. No new state on any actor.
3. **Projectile delivery is a property of the cast**, not a new cast kind: the combat machine
   runs the same phases; when the active window resolves, the driver spawns a player bolt
   (the bow's existing path) instead of a hitbox test. The bolt carries the element and
   applies its status on hit.
4. **The element owns the press cast**: `ElementSpec` gains a signature `MagicCast`;
   the character's kit composes element signature + character aura + character leap at
   build time. `ElementDefinition` grows the authoring block; characters lose their authored
   splash.
5. **D47 changes no code now.** Placeholders stay; the decision binds the art pass.

---

## Tasks

Each task lands with `run_tests` green and one commit.

### 60A — Status kinds (Core)
The slow: move scale on spec/instance/track, strongest-active query, pricing for damageless
marks (duration still shortened by resistance). Tests: chill slows, expires, renewal keeps the
stronger, resistance shortens, Burn untouched.

### 60B — Signature casts (Core)
`AttackTuning.StunSteps`; `MagicCast` delivery (hitbox | projectile + speed); `ElementSpec`
signature cast; kit composition from element + character. Tests pin the paper table: each
element's cast shape, the stun steps, the launch speed, the bolt's parameters.

### 60C — Wiring and the four elements (Gameplay/Data)
`ElementDefinition` authoring block (signature cast, slow fields); driver: cast-fired player
bolts applying status on hit, slows honored by player and enemy movement, stun-on-hit applied;
author Ice, Earth, Air assets and re-point Fire; characters compose the new kit. Recompile +
EditMode green.

### 60D — Michael's pass and close-out
Fast-motion checklist (his rule — no slow-motion sampling): each cast's feel, the chill
visibly slowing, the wall's stun landing, the gust launching into a juggle, an enemy Ice
caster slowing him back. Fixes, close-out notes here, progress tables, memory. M6 starts in
the original session after this.

---

## Watches inherited into M6

- The reaction pairs for Fire/Ice/Earth/Air are **open design** (D46) — a short session with
  Michael and his collaborator; the smoke suite (D45) is the natural verification home.
- Earth and Air statuses are unauthored dials.
- Heavy stays on watch (D26).
