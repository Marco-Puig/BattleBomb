# M8 Plan 2 — Menus, Saves, Joining and Leaving (Tasks 97–105) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Online co-op becomes a whole game rather than a mirror: the guest opens chests, shops and the hero panel on their own screen while the world runs (D60), keeps their own gear, gold, XP and chapter credit in their own save (D61), picks their own hero in a lobby at character select, drops in at a checkpoint room mid-run (D59), and either player can leave — or be dropped — without breaking the other's game.

**Architecture:** Host-authoritative, unchanged (D58). Every menu action becomes a **request** — a small value naming the player, the verb and its arguments — that the screen hands to an `IPlayerRequests`: on the couch and on the host it runs at once through the same `PlayerInventory` calls as today, so **the couch plays exactly as before**; on the guest it crosses the wire, the host runs it in a new first phase of its step, and the answer comes back a round trip later with the guest's whole inventory. The shopkeeper's rack moves into the simulation. Screens open on the guest from reliable events, never from a snapshot guess. Each machine saves only its own player; the guest writes when the host says a D52 moment happened. The lobby, drop-in and leaving are session rules on top of the same pieces.

**Tech Stack:** Unity 6.5 (6000.5.8f1), C# (.NET Standard 2.1), `System.IO.Compression` (deflate, for the inventory payload), NUnit EditMode + PlayMode suites, the Unity MCP bridge, Multiplayer Play Mode (Michael's pass only).

**Source of scope:** `docs/HANDOFF-M8.md` — Stages C and D, planning decisions 11–17. Decisions D59, D60, D61. The board's "Carried into later tasks → Plan 2" list (every item is placed below — see "Where the board's carried items land").

**Written against:** `main` at `f393d98` (Task 95; Plan 1's code complete). Task 96 — Michael's Stage A/B pass and the lag table — has **not** run yet. Every file this plan edits was read at `f393d98`. **Before each task, re-read every file it modifies.** Where a line this plan quotes has changed, keep the change and apply this plan's intent on top — and say so in the `DONE`.

**How this plan changes code (the Builder's lesson from Plan 1).** Plan 1's whole-file replacement blocks silently reverted later work twice. This plan therefore **never replaces an existing file wholesale**: every edit to an existing file is an anchored change — "replace this exact block with this one", or "after this exact line, add". New files are given whole. If an anchor is not found verbatim, stop and re-read; do not guess.

Consequences worth knowing before building (surface them in Task 105's close-out):

1. **The couch is unchanged, with one deliberate exception.** A combine in progress used to be cancelled by *any* change to the bag — including the XP a partner's kill gives. From Task 99 it is cancelled only when the sack itself moved (`Sack.Revision`). Online the old rule would cancel the guest's combine every time the host killed something; on the couch it was a small annoyance nobody had reported.
2. **Guest menu actions take a round trip to show** (D61). While one is in flight the guest's screen takes the stick and the way out, and ignores A, X and Y. At Michael's "bad" lag (200 ms) that is a fifth of a second per action.
3. **The guest's front door is a lobby while connected.** A guest chooses a hero from their own save and readies; the host chooses the chapter. The guest cannot launch anything of their own while connected — the "front door stays live" item from Plan 1 closes here.
4. **Development transport only.** Hosting and joining still go through the development panel's local socket; "open to friends by default" is built as a setting plus a transport factory that Plan 3's Steam lobby fills. In a release build without Steam there is no transport, so nothing opens (rule 6).
5. **Two stashes online, one on the couch.** Online, each player's sack and wallet are their own save's; on the host the guest's live in a second `SharedStash`. The couch keeps its one shared stash (D51).
6. **Each guest sale sends the guest's whole sack once** (inserted Task 101a, 2026-09-26). The guest's copy travels whole only when the sack, the coin or the auto flags moved; the fight's XP sends the character alone.
   - Every sale moves the sack, so rapid selling costs one whole copy per sale on the ordered channel: up to ~17 KB deflated for a full, varied 200-stack sack.
   - Accepted for M8: the guest isn't fighting while in the chest.
   - If D6 in Michael's pass (C2 and D2 at Bad lag) finds the guest's menus sluggish, this is the first suspect. The next lever is a stack-level sack delta, parked with the snapshot deltas for M11.

**Where this plan departs from HANDOFF-M8 (send the orchestrator this text for the spec's planning decisions — Task 105 Step 4):**

- **The guest's save payload travels in `LobbyPick`, not in `Hello`.** The guest chooses a hero *after* connecting (the lobby is at character select), so the payload — that hero's `CharacterSave`, the sack, the wallet, the auto flags — goes with the pick. `Hello` stays the version check.
- **The inventory payload is the save's own format**, a one-character `SaveGame` through `SaveCodec`, deflated — not a new binary format. A field added to the save travels without anyone remembering to add it to a second codec (the same argument as Plan 1's `ItemWire`). A full 200-stack sack is roughly 150 KB of JSON and ~10–15 KB deflated.
- **Screens open on the guest from reliable events** (`ScreenOpened`, `ScreenClosed`, `RackChanged`), stamped with their step and raised when the picture reaches it. The snapshot's per-player `OpenScreen` stays on the wire, unread by the guest (Plan 3's prediction may want it), so no snapshot codec changes.
- **`AutosaveNow` and `SessionMoment` are one message, `Moment`.** The host's run passes through moments the guest acts on — the chapter's end (its results open and its own save records the credit) and D52's other autosave moments (its own save writes). One message kind with a moment byte carries both; the resume point never travels, because it is the host's alone (D61).
- **No second stash on the guest.** Only the host needs two (the guest's inventory lives there during a match). The guest never simulates its partner's sack, so its copy of the partner keeps only worn gear and a ledger — no throwaway stash.
- **Protocol version:** Plan 2 bumps `NetProtocol.Version` once per task that changes the bytes — 97 → 3, 99 → 4, 100 → 5, 101 → 6, 102 → 7, 103 → 8.
- **Task contents moved between 101 and 102:** the `LobbyPick` message (and the end of Plan 1's "guest plays the host's hero" stand-in) arrives in 101, because two stashes need the guest's own inventory to fill the second one; 102 then builds the lobby screens on both machines around it.

---

## Before you start

- [ ] **The Unity MCP bridge must be connected.** It is the test gate. If `editor_status` does not answer, stop and ask the orchestrator (Michael opens Unity).
- [ ] `editor_status` — if the editor is in play mode, `editor_stop` (standing permission).
- [ ] Confirm `git log --oneline -1` is at or after `f393d98`, and that Task 96 has either been committed or the orchestrator has said to start without it.
- [ ] Record the baseline: `run_tests` mode `EditMode` (791 at `f393d98`), and `run_tests` mode `PlayMode` with **`async_tests: true`** (65 at `f393d98`). **PlayMode is always async**: a synchronous PlayMode run that times out wedges the bridge until Michael restarts it. An async run whose request "times out" keeps running — wait (~300 s for the full suite), then read `test_status`. Full EditMode output spills to a file: read the summary with `head -c 400 "<path>"` and failures with `grep -B3 -A8 '"Status": "Failed"' "<path>"`.
- [ ] After every PlayMode run, delete Unity's generated `Assets/InitTestScene*.unity` files and their `.meta`s.
- [ ] **Line endings are mixed; keep each file's own.** CRLF: `StageRunner.cs`, `SimulationDriver.cs`, `TrainingDummy.cs`, `LoadedStage.cs`, `GameSession.cs`, `SessionBinder.cs`, `SaveService.cs`, `FrontendFlow.cs`. LF: every `Net` file and every test. Check with Python bytes, not Git Bash `grep` (it miscounts CR). New files: LF.
- [ ] Bash heredocs break on apostrophes in this harness. Write C# with the Write tool, and anchored edits with the Edit tool or a Python script in the scratchpad.
- [ ] **PlayMode per-step facts come from `Stepped`, never from `Frame` read between yields** — the editor can run several steps in one render frame (Plan 1's close-out).
- [ ] **Only Task 105 needs `QUIET`** (Michael's two-editor pass). Every other task is proven by the harnesses — `HeadlessGuest` on the host side, `PlaybackTransport` on the guest side — because the bridge cannot click in the Multiplayer Play Mode clone and Michael has declined screen control. Never reach the clone another way.

**How to run a single fixture:** `run_tests` with `mode: EditMode` (or `PlayMode` with `async_tests: true`), `filter_type: testName`, `filter: <FixtureName>`. The filter matches substrings, not regexes (`"A|B"` matches nothing). Valid `filter_type` values are `testName`, `assembly`, `category` — anything else wedges the bridge.

**Commit convention:** one commit per task, subject `<task number>: <what>`, body explaining why, ending with:

```
Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

In the team setup the sim holder sends `DONE` and the orchestrator commits (`docs/team/PROTOCOL.md` rule 9) — every "Commit" step below is a `DONE`.

### If Michael's Plan 1 pass finds something

Task 96's pass has not run. A finding lands as a **small inserted task** — `96a`, `96b`, … — done before whichever Plan 2 task first touches the same file, never as a rewrite of a Plan 2 task. So that the Builder can see where a fix collides, here is every Plan 1 file this plan edits and the Plan 2 tasks that edit it:

| Plan 1 file | Plan 2 tasks |
|---|---|
| `Gameplay/Net/NetHost.cs` | 97, 99, 100, 101, 102, 103, 104 |
| `Gameplay/Net/NetGuest.cs` | 97, 99, 100, 101, 103 |
| `Gameplay/Net/NetSession.cs` | 101, 102, 103, 104 |
| `Gameplay/Net/ReplicaWorld.cs` | 99, 103 |
| `Gameplay/Net/RemoteCommandSource.cs` | 99, 104 |
| `Gameplay/Simulation/SimulationDriver.cs` | 97, 98, 99, 100 |
| `Gameplay/Session/SessionBinder.cs` | 101, 103, 104 |
| `Gameplay/Session/SaveService.cs` | 101, 104 |
| `Gameplay/Session/GameSession.cs` | 104 |
| `Gameplay/World/StageRunner.cs` | 100, 103 |
| `Gameplay/Players/InputSystemCommandSource.cs` | 104 |
| `Core/Net/NetProtocol.cs`, `NetMessageKind.cs` | 97, 99, 100, 101, 102, 103 |
| `Core/Net/ReplicatedEvents.cs`, `EventCodec.cs` | 99 |
| `Core/Net/HandshakeCodec.cs`, `StageCodec.cs` | 103 |
| `UI/Frontend/FrontendFlow.cs` | 102, 104 |
| `UI/Debug/NetDevOverlay.cs` | 104 |
| `Tests/PlayMode/HeadlessGuest.cs` | 97, 101, 102, 104 |
| `Tests/PlayMode/OnlineHostSmokeTests.cs`, `ReplicaReplaySmokeTests.cs` | 102 (the lobby wait) |

The lag-related findings (the teleport threshold, the render clock across a host pause, the let-go and drain numbers, bandwidth) are Plan 3's and do not touch this plan.

### Where the board's carried items land

| Carried item (BOARD.md, M8 → Plan 2) | Lands in |
|---|---|
| Unregistering a source by id can remove a newer source (both source types) | 104 |
| The refusal reason is lost; the guest should close on `Refuse` | 102 |
| `UseSeat` assumes Player 2 is authored active | 104 |
| The guest's own front door stays live while connected | 102 |
| The guest's abandoned body holds gates shut (D60/D61) | 104 |
| Task 103: the drop-in baseline sends a `DropSpawned` for every drop on the ground | 103 |
| `ApplyReplicaPlayerSide` doesn't raise `ScreenChanged` | 99 |
| `HoldMenuPause` a no-op online — keep a menu-open count apart from the world pause | 100 |
| The host's results screen waits on a remote Player 2's frozen held buttons | 100 |
| A stale `BeginLoad` completion can unload a stage requested again (load generation) | 103 |
| A guest who rejoins mid-match deadlocks the airlock | 103 |
| The shop rack is rolled and priced in the UI; `RequestBuy` trusts the UI | 98 |
| G8: the driver's catch-up loop does not re-check the pause | 100 |
| Candidate F4: a tap shorter than one sample is lost | **Plan 3**, first task — see below |
| Watch: `TargetRegistry.Ordered` hands out its live list; a throwing `EnemyDied` handler | Not touched — see below |

**F4 goes to Plan 3.** It is input fidelity, not networking: `SeatInput.Sample` reads what is held at the instant of a step, so a tap that starts and ends between two samples is never seen — on the couch exactly as on a guest. The wire neither adds nor removes that loss (a one-step press survives packet loss through the command redundancy). Nothing in Plan 2 depends on it, and Plan 3 holds Michael's feel pass (Task 108), where a lost mash would otherwise be blamed on the network. Its fix — latch press edges from the action callbacks between samples — should be Plan 3's first task. If Michael's Plan 1 pass reports presses that do not register, it jumps the queue as a `96x` fix.

**The G8 catch-up-loop item goes to Task 100.** With F1 showing that `Destroy` removes at once, the item stands on its own: a menu opened inside a step raises the pause, but the `while` loop in `SimulationDriver.Update` keeps running the frame's remaining steps. Task 100 rewrites exactly that pause logic for online play, so the re-check (and dropping the banked remainder) rides with it.

**The `TargetRegistry.Ordered` watch and a throwing `EnemyDied` handler are not touched.** Plan 2 adds no despawn inside a registry walk and no `EnemyDied` subscriber. The two things it adds that change the world's population — binding a late guest's body (103) and removing a departed one (104) — run from `NetHost`'s `Stepped` handler (after every phase of the step has finished walking), from `MenuStepped` on a paused frame, or from the session's pump before the step — never while a phase walks a registry.

**Michael's open call on same-hero couch saves does not touch this plan.** Online, each machine saves exactly one player (a couch pair cannot also take an online guest — D59), so two local players on one hero cannot occur in an online game. The couch save path is unchanged by Task 101 apart from filtering to local players, which on the couch is both of them.

---

## File map

**Core** (`Assets/_BattleBomb/Core/`)
- Modify `Items/Sack.cs` — `Revision` (97). Modify `Items/Inventory.cs` — every change to the sack moves it (97).
- Create `Items/PlayerRequest.cs` — `PlayerRequestKind`, `PlayerRequest`, `RequestRefusal`, `RequestOutcome` (97).
- Modify `Saves/SaveMapper.cs` — restores move the revision (97); a restore with the host's revision, `ClearLoadout`, `Participant` (99); `ParticipantFrom` (101).
- Modify `Saves/SaveModel.cs` — `ClosedToFriends` (104).
- Create `Net/RequestCodec.cs` (97), `Net/NetDeflate.cs` and `Net/ParticipantCodec.cs` (99), `Net/SessionCodec.cs` (100), `Net/LobbyCodec.cs` (101, 102).
- Modify `Net/NetProtocol.cs`, `Net/NetMessageKind.cs` (97–103), `Net/NetWriter.cs`, `Net/NetReader.cs`, `Net/ReplicatedEvents.cs`, `Net/EventCodec.cs` (99), `Net/HandshakeCodec.cs`, `Net/StageCodec.cs` (103).
- Modify `Chapters/FrontendState.cs` — a remote slot (102). Create `Chapters/GuestLobby.cs` (102).

**Gameplay** (`Assets/_BattleBomb/Gameplay/`)
- Create `Items/IPlayerRequests.cs`, `Items/LocalPlayerRequests.cs`, `Items/PlayerRequestRunner.cs` (97; the debug grant 100).
- Modify `Items/PlayerInventory.cs` — `RequestBuy` internal (98), `ApplyMirror` (99), `UseStash`, `MirroredFromHost` (101).
- Modify `Simulation/SimulationDriver.cs` — requests (97), the rack (98), replica screens (99), online pause rules (100).
- Create `Players/IRemotePlayerSource.cs`; modify `Players/PlayerRegistry.cs` — `IsLocal`, `LocalCount` (99), `Unregister(source)` (104).
- Modify `Players/InputSystemCommandSource.cs` — `UseSeat` rebuilds, safe unregister (104).
- Create `Net/RemotePlayerRequests.cs` (97). Modify `Net/NetHost.cs`, `Net/NetGuest.cs`, `Net/NetSession.cs`, `Net/ReplicaWorld.cs`, `Net/RemoteCommandSource.cs` (as the table above).
- Modify `Session/SessionBinder.cs` — the guest's hero and stash (101), `BindLate` and the open host (103), `Unbind` (104). Modify `Session/SaveService.cs` — local players only, the guest's copy (101), the setting (104). Modify `Session/GameSession.cs` — `ClosedToFriends` (104).
- Modify `World/StageRunner.cs` — `ReplicaChapterCompleted` (100); `TryDescribeForDropIn`, the stale-load fix, placed launches (103).

**Presentation** — Modify `Cameras/CameraRig.cs` — the sole local player, `FramingOneScreen` (100).

**UI** (`Assets/_BattleBomb/UI/`)
- Modify `Chest/ChestScreen.cs`, `Chest/ChestScreenHost.cs` (97, 98, 99); `Chest/SettingsMenu.cs` (100, 101, 104); `Chest/SettingsRows.cs` (104); `Frontend/ResultsScreen.cs` (100); `Frontend/FrontendFlow.cs` (102, 104); `Debug/NetDevOverlay.cs` (104).
- Create `Combat/NetBanner.cs` (103; the connection lines 104).

**Tests** (`Assets/_BattleBomb/Tests/`)
- Create EditMode: `SackRevisionTests.cs`, `PlayerRequestRunnerTests.cs` (97), `PlayerRegistryLocalTests.cs` (99), `FrontendRemoteSlotTests.cs`, `GuestLobbyTests.cs` (102), `SeatSourceTests.cs` (104); `Net/RequestCodecTests.cs` (97), `Net/ParticipantCodecTests.cs` (99), `Net/SessionCodecTests.cs` (100), `Net/LobbyCodecTests.cs` (101).
- Modify EditMode: `Net/EventCodecTests.cs` (99), `Acceptance/SettingsMenuReleaseAcceptanceTests.cs` (100), `Net/StageCodecTests.cs`, `Net/HandshakeCodecTests.cs` (103), `SaveCodecTests.cs`, `SettingsRowsTests.cs` (104).
- Create PlayMode: `OnlineMenuSmokeTests.cs` (host side, 97–102), `GuestMenuSmokeTests.cs` (guest side, 99–101), `OnlineJoinSmokeTests.cs` (lobby, drop-in, leaving — 102–104).
- Modify PlayMode: `HeadlessGuest.cs` (97, 101, 102, 104), `LootLoopSmokeTests.cs` (98, 100), `OnlineHostSmokeTests.cs` and `ReplicaReplaySmokeTests.cs` (102), `BattleBomb.Tests.PlayMode.asmdef` (100 — references Presentation).

**Docs** — `docs/team/m8-plan2-pass.md` (Michael's one sitting — written with this plan). HANDOFF-M8 and ROADMAP text goes to the orchestrator in Task 105's `DONE`.

---

## Stage C — Menus and saves

### Task 97: The requests seam

Every menu action becomes a value — which player, which verb, which arguments, and the sack revision the player was looking at — handed to an `IPlayerRequests`. Locally the request runs at once through the same `PlayerInventory` call as today, inside the same call, so the couch cannot tell the difference. On the host, a request from the wire runs in a new first phase of the step and is answered. On the guest, the player's requests cross the wire and the answer comes back a round trip later. The guest's screens do not open until Task 99; this task proves the wire from the host's side with the headless guest.

**Files:**
- Modify: `Assets/_BattleBomb/Core/Items/Sack.cs`, `Core/Items/Inventory.cs`, `Core/Saves/SaveMapper.cs`, `Core/Net/NetMessageKind.cs`, `Core/Net/NetProtocol.cs`
- Create: `Assets/_BattleBomb/Core/Items/PlayerRequest.cs`, `Core/Net/RequestCodec.cs`
- Create: `Assets/_BattleBomb/Gameplay/Items/IPlayerRequests.cs`, `Gameplay/Items/PlayerRequestRunner.cs`, `Gameplay/Items/LocalPlayerRequests.cs`, `Gameplay/Net/RemotePlayerRequests.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF), `Gameplay/Net/NetHost.cs`, `Gameplay/Net/NetGuest.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.cs`, `UI/Chest/ChestScreenHost.cs`
- Test: create `Assets/_BattleBomb/Tests/EditMode/SackRevisionTests.cs`, `Tests/EditMode/PlayerRequestRunnerTests.cs`, `Tests/EditMode/Net/RequestCodecTests.cs`, `Tests/PlayMode/OnlineMenuSmokeTests.cs`; modify `Tests/PlayMode/HeadlessGuest.cs`

- [ ] **Step 1: Write the failing Core tests**

`Assets/_BattleBomb/Tests/EditMode/SackRevisionTests.cs`:

```csharp
using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// HANDOFF-M8 planning decision 11: a request that names a place in the sack carries the revision
    /// the player was looking at, and the host refuses one aimed at a sack that has changed since. That
    /// only works if every change to what the sack holds moves the number — and nothing else does.
    /// </summary>
    public sealed class SackRevisionTests
    {
        private const int KnifeId = 4;
        private const int DraughtId = 50;

        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(
                new ItemIdentity(KnifeId, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
                new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f)),
        };

        private static GenerationContext Context() =>
            new GenerationContext(3f, 10, Catalog, QualityTable.Default, DropWeights.Default);

        private static ItemInstance Knife() => new ItemInstance(
            new ItemIdentity(KnifeId, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
            QualityRank.Shiny,
            new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f),
            new AffixRoll[0],
            requiredLevel: 10,
            new ItemInvestment(3));

        private static ItemInstance Draught() => new ItemInstance(
            new ItemIdentity(DraughtId, "Draught", ItemSlot.Consumable),
            QualityRank.Shiny,
            GearContribution.Zero,
            new AffixRoll[0],
            requiredLevel: 1,
            consumable: new RestorePayload(RestoreKind.Health, 0.35f));

        [Test]
        public void Every_change_to_what_the_sack_holds_moves_its_revision()
        {
            var inventory = new Inventory();
            int last = inventory.Sack.Revision;

            void Moved(string what)
            {
                Assert.That(inventory.Sack.Revision, Is.GreaterThan(last), $"{what} changed the sack and its revision stayed put.");
                last = inventory.Sack.Revision;
            }

            inventory.Add(Knife(), 99);
            Moved("Adding a knife");
            inventory.Add(Knife(), 99);
            Moved("Adding a second knife");
            inventory.Add(Draught(), 1);
            Moved("Adding a draught");
            inventory.Add(Draught(), 1);
            Moved("Stacking a second draught");
            inventory.Add(Knife(), 99);
            Moved("Adding a third knife");

            // The first two knives combine; the third — with its upgrade capacity known — takes the rest.
            inventory.TryCombine(new DeterministicRandom(7u), 0, 1, Context(), out CombineResult combined);
            Assert.That(combined.Combined, Is.True, "The two knives would not combine, so the combine line proves nothing.");
            Moved("Combining two knives");

            // [draughts, third knife, reroll]: the reroll lands last, so the first piece of gear is the third knife.
            int knife = -1;
            for (int i = 0; i < inventory.Items.Count && knife < 0; i++)
            {
                if (!inventory.Items[i].Item.IsConsumable)
                {
                    knife = i;
                }
            }

            inventory.SetLock(knife, true);
            Moved("Locking");
            inventory.SetLock(knife, false);
            Moved("Releasing");
            Assert.That(inventory.TryUpgradeBagged(knife, UpgradeTarget.Core(CoreStatId.WeaponDamage)), Is.True);
            Moved("Deepening");

            Assert.That(inventory.TryEquip(knife, 99), Is.True);
            Moved("Equipping");
            Assert.That(inventory.Unequip(ItemSlot.Weapon), Is.True);
            Moved("Taking it off");

            Assert.That(inventory.AssignQuickConsumable(DraughtId), Is.True);
            inventory.UseQuickSlot(0);
            Moved("Drinking a draught");

            Assert.That(inventory.Sell(inventory.Items.Count - 1), Is.GreaterThan(0));
            Moved("Selling");
        }

        [Test]
        public void Reading_the_sack_never_moves_it()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.Add(Knife(), 99);
            int before = inventory.Sack.Revision;

            _ = inventory.Items.Count;
            _ = inventory.SlotsUsed;
            _ = inventory.IsFull;
            _ = inventory.PreviewJunk(QualityRank.Legendary);
            _ = inventory.HasCombinePartner(0);
            inventory.CombineChoices(0, new System.Collections.Generic.List<int>());
            _ = inventory.FindAutoSellTarget();

            Assert.That(inventory.Sack.Revision, Is.EqualTo(before), "Looking at the sack moved its revision.");
        }

        [Test]
        public void A_refused_change_leaves_it_alone()
        {
            var inventory = new Inventory();
            inventory.Add(Draught(), 1);
            int before = inventory.Sack.Revision;

            Assert.That(inventory.Sell(5), Is.Zero);
            Assert.That(inventory.SetLock(-1, true), Is.False);
            Assert.That(inventory.TryEquip(0, 99), Is.False, "A draught cannot be worn.");

            Assert.That(inventory.Sack.Revision, Is.EqualTo(before), "A change that did not happen moved the revision.");
        }

        [Test]
        public void A_restore_from_a_save_moves_it()
        {
            var sack = new Sack();
            int before = sack.Revision;

            SaveMapper.RestoreSack(SaveGame.Fresh(), sack, Catalog);

            Assert.That(sack.Revision, Is.GreaterThan(before), "A load replaced the sack's contents and the revision stayed put.");
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/RequestCodecTests.cs`:

```csharp
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>A menu action and its answer cross the wire whole (HANDOFF-M8 planning decision 11).</summary>
    public sealed class RequestCodecTests
    {
        [Test]
        public void A_request_travels_whole()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                PlayerRequest request = FieldCoverage.Filled<PlayerRequest>(seed);
                var writer = new NetWriter();
                RequestCodec.WriteRequest(writer, request);

                var reader = new NetReader(writer.ToArray());
                Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Request));
                PlayerRequest back = RequestCodec.ReadRequest(reader);

                FieldCoverage.AssertSame(request, back, nameof(PlayerRequest));
                Assert.That(reader.Remaining, Is.Zero, "The request read back fewer bytes than it wrote.");
            }
        }

        [Test]
        public void An_answer_travels_whole_with_its_sequence()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                RequestOutcome outcome = FieldCoverage.Filled<RequestOutcome>(seed);
                var writer = new NetWriter();
                RequestCodec.WriteResult(writer, 4000 + seed, outcome);

                var reader = new NetReader(writer.ToArray());
                Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.RequestResult));
                RequestOutcome back = RequestCodec.ReadResult(reader, out int sequence);

                Assert.That(sequence, Is.EqualTo(4000 + seed));
                FieldCoverage.AssertSame(outcome, back, nameof(RequestOutcome));
                Assert.That(reader.Remaining, Is.Zero, "The answer read back fewer bytes than it wrote.");
            }
        }

        [Test]
        public void An_unknown_verb_is_refused_at_the_door()
        {
            var writer = new NetWriter();
            writer.WriteByte((byte)NetMessageKind.Request);
            writer.WriteByte(200);
            for (int i = 0; i < 7; i++)
            {
                writer.WriteInt(0);
            }

            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            Assert.Throws<NetFormatException>(() => RequestCodec.ReadRequest(reader));
        }

        [Test]
        public void An_upgrade_names_its_target_either_way()
        {
            UpgradeTarget affix = PlayerRequest.Upgrade(3, UpgradeTarget.Affix(2)).Target;
            Assert.That(affix.IsAffix, Is.True);
            Assert.That(affix.AffixIndex, Is.EqualTo(2));

            UpgradeTarget core = PlayerRequest.UpgradeWorn(ItemSlot.Weapon, 0, UpgradeTarget.Core(CoreStatId.WeaponDamage)).Target;
            Assert.That(core.IsAffix, Is.False);
            Assert.That(core.CoreStat, Is.EqualTo(CoreStatId.WeaponDamage));
        }

        [Test]
        public void Only_the_verbs_that_name_a_place_in_the_sack_carry_a_revision_check()
        {
            Assert.That(PlayerRequest.Sell(0).NamesASackPlace, Is.True);
            Assert.That(PlayerRequest.Equip(0).NamesASackPlace, Is.True);
            Assert.That(PlayerRequest.Combine(0, 1).NamesASackPlace, Is.True);
            Assert.That(PlayerRequest.CloseScreen().NamesASackPlace, Is.False, "Closing must always work.");
            Assert.That(PlayerRequest.Unequip(ItemSlot.Helmet, 0).NamesASackPlace, Is.False, "A worn slot never moves.");
            Assert.That(PlayerRequest.SetAutoSell(true).NamesASackPlace, Is.False);
        }
    }
}
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors — `Sack.Revision`, `PlayerRequest`, `RequestOutcome`, `RequestCodec`, `NetMessageKind.Request` do not exist.

- [ ] **Step 3: The sack's revision**

In `Assets/_BattleBomb/Core/Items/Sack.cs`, after the line `public bool IsFull => Entries.Count >= Rules.Capacity;` add:

```csharp

        /// <summary>
        /// Moves every time what the sack holds changes — a stack added, removed, split, locked or
        /// deepened. A menu action that names a place in the sack carries the number its player was
        /// looking at, so the host can refuse one aimed at a sack that has changed since (HANDOFF-M8
        /// planning decision 11). The auto flags do not move it: they change no place.
        /// </summary>
        public int Revision { get; private set; }

        internal void Touch() => Revision++;

        /// <summary>The guest's copy of its sack takes the host's number (Task 99), so what it asks for
        /// names the sack the host holds.</summary>
        internal void AdoptRevision(int revision) => Revision = revision;
```

In `Assets/_BattleBomb/Core/Items/Inventory.cs`, nine anchored insertions — each adds `_sack.Touch();` after a change has happened, never before a check that can still refuse:

(a) In `Add`, replace

```csharp
                    _items[stack] = new ItemStack(_items[stack].Item, _items[stack].Count + 1);
                    return new AddResult(true, false, 0);
```

with

```csharp
                    _items[stack] = new ItemStack(_items[stack].Item, _items[stack].Count + 1);
                    _sack.Touch();
                    return new AddResult(true, false, 0);
```

(b) In `Add`, replace

```csharp
            _items.Add(new ItemStack(item, 1));

            if (!item.IsConsumable && AutoEquip && item.RequiredLevel <= currentLevel)
```

with

```csharp
            _items.Add(new ItemStack(item, 1));
            _sack.Touch();

            if (!item.IsConsumable && AutoEquip && item.RequiredLevel <= currentLevel)
```

(c) In `Sell`, replace

```csharp
            ItemStack stack = _items[bagIndex];
            _items.RemoveAt(bagIndex);
            if (stack.Item.IsConsumable && _quickKind == QuickSlotKind.Consumable
```

with

```csharp
            ItemStack stack = _items[bagIndex];
            _items.RemoveAt(bagIndex);
            _sack.Touch();
            if (stack.Item.IsConsumable && _quickKind == QuickSlotKind.Consumable
```

(d) In `SetLock`, replace

```csharp
            _items[bagIndex] = new ItemStack(stack.Item.WithLock(locked), stack.Count);
            return true;
```

with

```csharp
            _items[bagIndex] = new ItemStack(stack.Item.WithLock(locked), stack.Count);
            _sack.Touch();
            return true;
```

(e) In `TryUpgradeBagged`, replace

```csharp
            _items[bagIndex] = new ItemStack(upgraded, stack.Count);
            return true;
```

with

```csharp
            _items[bagIndex] = new ItemStack(upgraded, stack.Count);
            _sack.Touch();
            return true;
```

(f) In `TryCombine`, replace

```csharp
            RemoveOne(high);
            RemoveOne(low);
            _items.Add(new ItemStack(result.Item, 1));
            return next;
```

with

```csharp
            RemoveOne(high);
            RemoveOne(low);
            _items.Add(new ItemStack(result.Item, 1));
            _sack.Touch();
            return next;
```

(g) In `TryEquip`, replace

```csharp
            if (!previous.IsEmpty)
            {
                _items.Add(new ItemStack(previous, 1));
            }

            if (item.Slot == ItemSlot.Equipment && _quickKind == QuickSlotKind.EquipmentActive
```

with

```csharp
            if (!previous.IsEmpty)
            {
                _items.Add(new ItemStack(previous, 1));
            }

            _sack.Touch();
            if (item.Slot == ItemSlot.Equipment && _quickKind == QuickSlotKind.EquipmentActive
```

(h) In the private `Unequip(ItemSlot slot, int equipmentIndex, bool forced)`, replace

```csharp
            Loadout.Swap(slot, equipmentIndex, default);
            _items.Add(new ItemStack(worn, 1));
```

with

```csharp
            Loadout.Swap(slot, equipmentIndex, default);
            _items.Add(new ItemStack(worn, 1));
            _sack.Touch();
```

(i) In `UseQuickSlot`, replace

```csharp
            _quickCooldown = Mathf.Max(0, cooldownSteps);
            return new QuickUseResult(true, heal, restores);
```

with

```csharp
            _sack.Touch();
            _quickCooldown = Mathf.Max(0, cooldownSteps);
            return new QuickUseResult(true, heal, restores);
```

In `Assets/_BattleBomb/Core/Saves/SaveMapper.cs`, in `RestoreSack`, replace

```csharp
                sack.Entries.Add(new ItemStack(ToInstance(stacks[i].Item, catalog), stacks[i].Count));
            }
        }
```

with

```csharp
                sack.Entries.Add(new ItemStack(ToInstance(stacks[i].Item, catalog), stacks[i].Count));
            }

            sack.Touch();
        }
```

- [ ] **Step 4: The request, its answer, and the wire**

`Assets/_BattleBomb/Core/Items/PlayerRequest.cs`:

```csharp
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>What a menu can ask of a player's inventory. Values are wire format: never renumber, only add.</summary>
    public enum PlayerRequestKind : byte
    {
        Equip = 1,
        Unequip = 2,
        Sell = 3,
        SellJunk = 4,
        Lock = 5,
        LockWorn = 6,
        Upgrade = 7,
        UpgradeWorn = 8,
        Combine = 9,
        CombineAll = 10,
        QuickConsumable = 11,
        Allocate = 12,
        Buy = 13,
        CloseScreen = 14,
        SetAutoEquip = 15,
        SetAutoSell = 16,
        DebugGrant = 17,
    }

    /// <summary>Why a request did nothing. Values are wire format: never renumber, only add.</summary>
    public enum RequestRefusal : byte
    {
        None = 0,

        /// <summary>The rules said no — locked, full, too poor, nothing there to do it to.</summary>
        Refused = 1,

        /// <summary>It named a place in a sack that has changed since its player looked.</summary>
        StaleSack = 2,

        /// <summary>No chest or shop is open for this player, or not the one the verb needs.</summary>
        NoScreen = 3,

        /// <summary>An earlier request from the same screen is still waiting for its answer.</summary>
        Busy = 4,
    }

    /// <summary>
    /// One menu action, as a value (HANDOFF-M8 planning decision 11): who, which verb, up to four
    /// integer arguments, the sequence its sender numbered it with, and the sack revision its player
    /// was looking at. The same value runs on the couch at once and crosses the wire from a guest, so a
    /// menu has exactly one way to change anything.
    /// </summary>
    public readonly struct PlayerRequest
    {
        public readonly PlayerRequestKind Kind;
        public readonly int PlayerId;
        public readonly int Sequence;
        public readonly int Revision;
        public readonly int A;
        public readonly int B;
        public readonly int C;
        public readonly int D;

        public PlayerRequest(PlayerRequestKind kind, int playerId, int sequence, int revision, int a, int b, int c, int d)
        {
            Kind = kind;
            PlayerId = playerId;
            Sequence = sequence;
            Revision = revision;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        private static PlayerRequest Of(PlayerRequestKind kind, int a = 0, int b = 0, int c = 0, int d = 0) =>
            new PlayerRequest(kind, -1, 0, 0, a, b, c, d);

        public static PlayerRequest Equip(int bagIndex) => Of(PlayerRequestKind.Equip, bagIndex);

        public static PlayerRequest Unequip(ItemSlot slot, int equipmentIndex) =>
            Of(PlayerRequestKind.Unequip, (int)slot, equipmentIndex);

        public static PlayerRequest Sell(int bagIndex) => Of(PlayerRequestKind.Sell, bagIndex);

        public static PlayerRequest SellJunk(QualityRank below) => Of(PlayerRequestKind.SellJunk, (int)below);

        public static PlayerRequest Lock(int bagIndex, bool locked) => Of(PlayerRequestKind.Lock, bagIndex, locked ? 1 : 0);

        public static PlayerRequest LockWorn(ItemSlot slot, int equipmentIndex, bool locked) =>
            Of(PlayerRequestKind.LockWorn, (int)slot, equipmentIndex, locked ? 1 : 0);

        public static PlayerRequest Upgrade(int bagIndex, in UpgradeTarget target) =>
            Of(PlayerRequestKind.Upgrade, bagIndex, (int)target.CoreStat, target.AffixIndex);

        public static PlayerRequest UpgradeWorn(ItemSlot slot, int equipmentIndex, in UpgradeTarget target) =>
            Of(PlayerRequestKind.UpgradeWorn, (int)slot, equipmentIndex, (int)target.CoreStat, target.AffixIndex);

        public static PlayerRequest Combine(int firstIndex, int secondIndex) =>
            Of(PlayerRequestKind.Combine, firstIndex, secondIndex);

        public static PlayerRequest CombineAll(int anchorIndex) => Of(PlayerRequestKind.CombineAll, anchorIndex);

        public static PlayerRequest QuickConsumable(int definitionId) => Of(PlayerRequestKind.QuickConsumable, definitionId);

        public static PlayerRequest Allocate(StatId stat) => Of(PlayerRequestKind.Allocate, (int)stat);

        public static PlayerRequest Buy(int rackSlot) => Of(PlayerRequestKind.Buy, rackSlot);

        public static PlayerRequest CloseScreen() => Of(PlayerRequestKind.CloseScreen);

        public static PlayerRequest SetAutoEquip(bool on) => Of(PlayerRequestKind.SetAutoEquip, on ? 1 : 0);

        public static PlayerRequest SetAutoSell(bool on) => Of(PlayerRequestKind.SetAutoSell, on ? 1 : 0);

        public static PlayerRequest DebugGrant() => Of(PlayerRequestKind.DebugGrant);

        /// <summary>The same request, as this player's. The host always stamps a remote player's own id
        /// over whatever arrived: a guest can only ever act as itself.</summary>
        public PlayerRequest For(int playerId) => new PlayerRequest(Kind, playerId, Sequence, Revision, A, B, C, D);

        public PlayerRequest WithSequence(int sequence) => new PlayerRequest(Kind, PlayerId, sequence, Revision, A, B, C, D);

        public PlayerRequest WithRevision(int revision) => new PlayerRequest(Kind, PlayerId, Sequence, revision, A, B, C, D);

        /// <summary>The verbs that name a place in the sack by its index — the ones a stale view could aim at
        /// the wrong item, and so the ones the revision guards. A worn slot, a definition id, a stat, a rack
        /// slot or a setting cannot move under the player's cursor.</summary>
        public bool NamesASackPlace =>
            Kind == PlayerRequestKind.Equip || Kind == PlayerRequestKind.Sell || Kind == PlayerRequestKind.Lock
            || Kind == PlayerRequestKind.Upgrade || Kind == PlayerRequestKind.Combine || Kind == PlayerRequestKind.CombineAll;

        /// <summary>What an Upgrade (B, C) or an UpgradeWorn (C, D) deepens.</summary>
        public UpgradeTarget Target => Kind == PlayerRequestKind.UpgradeWorn ? TargetOf(C, D) : TargetOf(B, C);

        private static UpgradeTarget TargetOf(int coreStat, int affixIndex) =>
            affixIndex >= 0 ? UpgradeTarget.Affix(affixIndex) : UpgradeTarget.Core((CoreStatId)coreStat);
    }

    /// <summary>
    /// What a request did. The numbers mean what the verb says: Sell — A coins; SellJunk — A stacks, B
    /// pieces, C coins; Combine — A is 1 when it promoted; CombineAll — A combines, B promotions; Buy and
    /// the upgrades — A the price paid. A screen redraws from its bag's own change, never from these.
    /// </summary>
    public readonly struct RequestOutcome
    {
        public readonly bool Ok;
        public readonly RequestRefusal Refusal;
        public readonly int A;
        public readonly int B;
        public readonly int C;

        public RequestOutcome(bool ok, RequestRefusal refusal, int a, int b, int c)
        {
            Ok = ok;
            Refusal = refusal;
            A = a;
            B = b;
            C = c;
        }

        public static RequestOutcome Done(int a = 0, int b = 0, int c = 0) =>
            new RequestOutcome(true, RequestRefusal.None, a, b, c);

        public static RequestOutcome No(RequestRefusal why) =>
            new RequestOutcome(false, why == RequestRefusal.None ? RequestRefusal.Refused : why, 0, 0, 0);

        public static RequestOutcome From(bool ok) => ok ? Done() : No(RequestRefusal.Refused);
    }
}
```

`Assets/_BattleBomb/Core/Net/RequestCodec.cs`:

```csharp
using BattleBomb.Core.Items;

namespace BattleBomb.Core.Net
{
    /// <summary>A menu action and its answer on the reliable channel. Readers take a reader positioned after
    /// the kind byte.</summary>
    public static class RequestCodec
    {
        public static void WriteRequest(NetWriter w, in PlayerRequest r)
        {
            w.WriteByte((byte)NetMessageKind.Request);
            w.WriteByte((byte)r.Kind);
            w.WriteInt(r.PlayerId);
            w.WriteInt(r.Sequence);
            w.WriteInt(r.Revision);
            w.WriteInt(r.A);
            w.WriteInt(r.B);
            w.WriteInt(r.C);
            w.WriteInt(r.D);
        }

        public static PlayerRequest ReadRequest(NetReader r)
        {
            var kind = (PlayerRequestKind)r.ReadByte();
            if (kind < PlayerRequestKind.Equip || kind > PlayerRequestKind.DebugGrant)
            {
                throw new NetFormatException($"A request of kind {(byte)kind}.");
            }

            return new PlayerRequest(kind, r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt());
        }

        public static void WriteResult(NetWriter w, int sequence, in RequestOutcome o)
        {
            w.WriteByte((byte)NetMessageKind.RequestResult);
            w.WriteInt(sequence);
            w.WriteBool(o.Ok);
            w.WriteByte((byte)o.Refusal);
            w.WriteInt(o.A);
            w.WriteInt(o.B);
            w.WriteInt(o.C);
        }

        public static RequestOutcome ReadResult(NetReader r, out int sequence)
        {
            sequence = r.ReadInt();
            bool ok = r.ReadBool();
            var refusal = (RequestRefusal)r.ReadByte();
            if (refusal > RequestRefusal.Busy)
            {
                throw new NetFormatException($"A refusal of kind {(byte)refusal}.");
            }

            return new RequestOutcome(ok, refusal, r.ReadInt(), r.ReadInt(), r.ReadInt());
        }
    }
}
```

In `Assets/_BattleBomb/Core/Net/NetMessageKind.cs`, replace

```csharp
        Bye = 13,
    }
```

with

```csharp
        Bye = 13,
        Request = 14,
        RequestResult = 15,
    }
```

In `Assets/_BattleBomb/Core/Net/NetProtocol.cs`, replace `public const int Version = 2;` with `public const int Version = 3;` and extend its summary's last sentence: `… bumps <see cref="Version"/>. 3: menu requests (Plan 2, Task 97).`

- [ ] **Step 5: Run the Core tests** — `recompile`; `run_tests` EditMode `SackRevisionTests`, then `RequestCodecTests`. Expected: all pass. A coverage failure names the field — fix the codec, never the test.

- [ ] **Step 6: Write the failing runner tests**

`Assets/_BattleBomb/Tests/EditMode/PlayerRequestRunnerTests.cs`:

```csharp
using System.Reflection;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The one place a request becomes a change (HANDOFF-M8 planning decision 11). The same runner
    /// serves the couch, now, and a guest, a round trip later, so its refusals are the host's rules:
    /// no open chest, no inventory verbs; a stale sack, no index verbs; closing and the settings always.
    /// <c>PlayerInventory.OnEnable</c> is fired by reflection, as <see cref="SharedStashWiringTests"/> does.
    /// </summary>
    public sealed class PlayerRequestRunnerTests
    {
        private const int PlayerId = 1;

        private GameObject _stashGo;
        private GameObject _playerGo;
        private GameObject _driverGo;
        private PlayerInventory _bag;
        private SimulationDriver _driver;

        private static ItemInstance Helmet(int id) => new ItemInstance(
            new ItemIdentity(id, $"Helm {id}", ItemSlot.Helmet), QualityRank.Shiny,
            new GearContribution(defence: 3f), new AffixRoll[0], requiredLevel: 1);

        [SetUp]
        public void Build()
        {
            _stashGo = new GameObject("Stash");
            _stashGo.AddComponent<SharedStash>();
            _playerGo = new GameObject("Player");
            _bag = _playerGo.AddComponent<PlayerInventory>();
            typeof(PlayerInventory).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_bag, null);
            _driverGo = new GameObject("Driver");
            _driver = _driverGo.AddComponent<SimulationDriver>();
            _bag.Take(Helmet(1));
        }

        [TearDown]
        public void Clear()
        {
            Object.DestroyImmediate(_playerGo);
            Object.DestroyImmediate(_stashGo);
            Object.DestroyImmediate(_driverGo);
        }

        private RequestOutcome Run(PlayerRequest request) =>
            PlayerRequestRunner.Run(request.For(PlayerId).WithRevision(_bag.Inventory.Sack.Revision), _bag, _driver);

        [Test]
        public void A_sale_at_an_open_chest_pays_and_says_how_much()
        {
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);
            int before = _bag.Wallet.Balance;

            RequestOutcome outcome = Run(PlayerRequest.Sell(0));

            Assert.That(outcome.Ok, Is.True);
            Assert.That(outcome.A, Is.GreaterThan(0), "The answer does not say what the sale paid.");
            Assert.That(_bag.Wallet.Balance, Is.EqualTo(before + outcome.A));
            Assert.That(_bag.Inventory.Items.Count, Is.Zero);
        }

        [Test]
        public void With_no_chest_open_an_inventory_verb_does_nothing()
        {
            RequestOutcome outcome = Run(PlayerRequest.Sell(0));

            Assert.That(outcome.Ok, Is.False);
            Assert.That(outcome.Refusal, Is.EqualTo(RequestRefusal.NoScreen));
            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(1), "A sale ran with no chest open.");
        }

        [Test]
        public void A_request_aimed_at_a_sack_that_moved_is_refused()
        {
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);
            int stale = _bag.Inventory.Sack.Revision;
            _bag.Take(Helmet(2));

            RequestOutcome outcome = PlayerRequestRunner.Run(
                PlayerRequest.Sell(0).For(PlayerId).WithRevision(stale), _bag, _driver);

            Assert.That(outcome.Refusal, Is.EqualTo(RequestRefusal.StaleSack));
            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(2), "A sale aimed at an old view of the sack still sold.");
        }

        [Test]
        public void Closing_needs_no_revision_and_always_closes()
        {
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);

            RequestOutcome outcome = PlayerRequestRunner.Run(
                PlayerRequest.CloseScreen().For(PlayerId).WithRevision(-99), _bag, _driver);

            Assert.That(outcome.Ok, Is.True);
            Assert.That(_driver.TryGetOpenScreen(PlayerId, out _), Is.False);
        }

        [Test]
        public void The_auto_flags_need_no_chest()
        {
            RequestOutcome outcome = Run(PlayerRequest.SetAutoEquip(true));

            Assert.That(outcome.Ok, Is.True);
            Assert.That(_bag.Inventory.AutoEquip, Is.True);
        }

        [Test]
        public void A_point_is_spent_only_at_a_chest()
        {
            _bag.Earn(5000f);
            Assert.That(Run(PlayerRequest.Allocate(StatId.Strength)).Refusal, Is.EqualTo(RequestRefusal.NoScreen));

            _driver.OpenScreen(PlayerId, InteractionKind.Chest);
            Assert.That(Run(PlayerRequest.Allocate(StatId.Strength)).Ok, Is.True);
            Assert.That(_bag.Ledger.Allocations.Strength, Is.EqualTo(1));
        }
    }
}
```

- [ ] **Step 7: Run it to see it fail** — `recompile`. Expected: compile errors, `PlayerRequestRunner` does not exist.

- [ ] **Step 8: The seam in Gameplay**

`Assets/_BattleBomb/Gameplay/Items/IPlayerRequests.cs`:

```csharp
using System;
using BattleBomb.Core.Items;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// Where one player's menu actions go (HANDOFF-M8 planning decision 11). On the couch and on the host
    /// the answer comes inside <see cref="Send"/>; on a guest it comes a round trip later, after the host's
    /// copy of the bag has arrived. A screen writes the code after an action once, in the callback, and it
    /// reads the same both ways.
    /// </summary>
    public interface IPlayerRequests
    {
        /// <summary>An earlier request is still waiting for its answer. Never true on the couch.</summary>
        bool Pending { get; }

        /// <summary>Runs or sends <paramref name="request"/> for this player; <paramref name="answered"/>
        /// (may be null) hears what happened. The sender stamps the player and the sack revision.</summary>
        void Send(PlayerRequest request, Action<RequestOutcome> answered);
    }
}
```

`Assets/_BattleBomb/Gameplay/Items/PlayerRequestRunner.cs`:

```csharp
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// The one place a menu action becomes a change (HANDOFF-M8 planning decision 11). Every verb is the
    /// same <see cref="PlayerInventory"/> call the chest screen made before M8, so the couch is untouched;
    /// what is added are the host's checks for a request that may have been sent from a stale screen: an
    /// inventory verb needs this player's chest or shop open, and a verb that names a place in the sack
    /// needs the revision its player was looking at.
    /// </summary>
    public static class PlayerRequestRunner
    {
        public static RequestOutcome Run(in PlayerRequest request, PlayerInventory bag, SimulationDriver driver)
        {
            if (bag == null || driver == null)
            {
                return RequestOutcome.No(RequestRefusal.Refused);
            }

            switch (request.Kind)
            {
                case PlayerRequestKind.CloseScreen:
                    driver.CloseScreen(request.PlayerId);
                    return RequestOutcome.Done();

                case PlayerRequestKind.SetAutoEquip:
                    bag.SetAutoEquip(request.A != 0);
                    return RequestOutcome.Done();

                case PlayerRequestKind.SetAutoSell:
                    bag.SetAutoSell(request.A != 0);
                    return RequestOutcome.Done();
            }

            if (!driver.TryGetOpenScreen(request.PlayerId, out InteractionKind screen))
            {
                return RequestOutcome.No(RequestRefusal.NoScreen);
            }

            if (request.NamesASackPlace && request.Revision != bag.Inventory.Sack.Revision)
            {
                return RequestOutcome.No(RequestRefusal.StaleSack);
            }

            switch (request.Kind)
            {
                case PlayerRequestKind.Equip:
                    return RequestOutcome.From(bag.RequestEquip(request.A));

                case PlayerRequestKind.Unequip:
                    return RequestOutcome.From(bag.RequestUnequip((ItemSlot)request.A, request.B));

                case PlayerRequestKind.Sell:
                    int coins = bag.RequestSell(request.A);
                    return coins > 0 ? RequestOutcome.Done(coins) : RequestOutcome.No(RequestRefusal.Refused);

                case PlayerRequestKind.SellJunk:
                    JunkSale sale = bag.RequestSellJunk((QualityRank)request.A);
                    return sale.IsEmpty
                        ? RequestOutcome.No(RequestRefusal.Refused)
                        : RequestOutcome.Done(sale.Stacks, sale.Pieces, sale.Coins);

                case PlayerRequestKind.Lock:
                    return RequestOutcome.From(bag.RequestLock(request.A, request.B != 0));

                case PlayerRequestKind.LockWorn:
                    return RequestOutcome.From(bag.RequestLockWorn((ItemSlot)request.A, request.B, request.C != 0));

                case PlayerRequestKind.Upgrade:
                    return Upgrade(bag, request);

                case PlayerRequestKind.UpgradeWorn:
                    return UpgradeWorn(bag, request);

                case PlayerRequestKind.Combine:
                    return bag.RequestCombine(request.A, request.B, out CombineResult combined)
                        ? RequestOutcome.Done(combined.Promoted ? 1 : 0)
                        : RequestOutcome.No(RequestRefusal.Refused);

                case PlayerRequestKind.CombineAll:
                    return bag.RequestCombineAll(request.A, out CombineRun run)
                        ? RequestOutcome.Done(run.Combines, run.Promotions)
                        : RequestOutcome.No(RequestRefusal.Refused);

                case PlayerRequestKind.QuickConsumable:
                    return RequestOutcome.From(bag.RequestQuickConsumable(request.A));

                case PlayerRequestKind.Allocate:
                    return screen == InteractionKind.Chest
                        ? RequestOutcome.From(bag.RequestAllocate((StatId)request.A))
                        : RequestOutcome.No(RequestRefusal.NoScreen);

                default:
                    // Buy arrives with the rack in the simulation (Task 98), DebugGrant with the settings
                    // online (Task 100). Until then, asked for either, the host does nothing.
                    return RequestOutcome.No(RequestRefusal.Refused);
            }
        }

        private static RequestOutcome Upgrade(PlayerInventory bag, in PlayerRequest request)
        {
            int index = request.A;
            if (index < 0 || index >= bag.Inventory.Items.Count)
            {
                return RequestOutcome.No(RequestRefusal.Refused);
            }

            int price = bag.Inventory.Prices.UpgradeCost(bag.Inventory.Items[index].Item);
            return bag.RequestUpgrade(index, request.Target)
                ? RequestOutcome.Done(price)
                : RequestOutcome.No(RequestRefusal.Refused);
        }

        private static RequestOutcome UpgradeWorn(PlayerInventory bag, in PlayerRequest request)
        {
            var slot = (ItemSlot)request.A;
            ItemInstance worn = bag.Inventory.Loadout.Worn(slot, request.B);
            int price = worn.IsEmpty ? 0 : bag.Inventory.Prices.UpgradeCost(worn);
            return bag.RequestUpgradeWorn(slot, request.B, request.Target)
                ? RequestOutcome.Done(price)
                : RequestOutcome.No(RequestRefusal.Refused);
        }
    }
}
```

`Assets/_BattleBomb/Gameplay/Items/LocalPlayerRequests.cs`:

```csharp
using System;
using BattleBomb.Core.Items;
using BattleBomb.Gameplay.Simulation;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// A player whose menus are on this machine and whose bag is simulated here: the couch, a solo game,
    /// and the host's own player. The request runs inside <see cref="Send"/> — which is what keeps the
    /// couch exactly as it was: the answer arrives before the screen's next line runs.
    /// </summary>
    internal sealed class LocalPlayerRequests : IPlayerRequests
    {
        private readonly SimulationDriver _driver;
        private readonly int _playerId;

        internal LocalPlayerRequests(SimulationDriver driver, int playerId)
        {
            _driver = driver;
            _playerId = playerId;
        }

        public bool Pending => false;

        public void Send(PlayerRequest request, Action<RequestOutcome> answered)
        {
            PlayerInventory bag = _driver.InventoryOf(_playerId);
            PlayerRequest stamped = request.For(_playerId).WithRevision(bag != null ? bag.Inventory.Sack.Revision : 0);
            RequestOutcome outcome = PlayerRequestRunner.Run(stamped, bag, _driver);
            answered?.Invoke(outcome);
        }
    }
}
```

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF — keep it):

(a) After the field `private readonly List<EnemyActor> _dying = new List<EnemyActor>();` add:

```csharp
        private readonly Dictionary<int, LocalPlayerRequests> _localRequests = new Dictionary<int, LocalPlayerRequests>();
        private readonly List<PlayerRequest> _remoteRequests = new List<PlayerRequest>();
        private readonly List<PlayerRequest> _requestScratch = new List<PlayerRequest>();
```

(b) After the `PickupRemoved` event declaration (the one ending `internal event Action<DropPickup> PickupRemoved;`) add:

```csharp

        /// <summary>
        /// Where a player's menu actions go on this machine (HANDOFF-M8 planning decision 11). The guest's
        /// half sets it so its own player's requests cross the wire; null — everywhere else — runs them
        /// here, now.
        /// </summary>
        internal Func<int, IPlayerRequests> RequestRoute { get; set; }

        /// <summary>Raised inside the host's step for every remote request it ran, with what happened.</summary>
        internal event Action<PlayerRequest, RequestOutcome> RemoteRequestAnswered;

        /// <summary>This player's menu actions: run here and now, or — a guest's own player — sent to the host.</summary>
        public IPlayerRequests RequestsFor(int playerIdValue)
        {
            IPlayerRequests routed = RequestRoute?.Invoke(playerIdValue);
            if (routed != null)
            {
                return routed;
            }

            if (!_localRequests.TryGetValue(playerIdValue, out LocalPlayerRequests local))
            {
                local = new LocalPlayerRequests(this, playerIdValue);
                _localRequests[playerIdValue] = local;
            }

            return local;
        }

        /// <summary>
        /// A screen's way out — B, Start, the pointer's close. It closes here at once; on a guest the host is
        /// told as well, because the host's copy of the screen is what keeps the body standing idle (D42).
        /// </summary>
        public void RequestClose(int playerIdValue)
        {
            CloseScreen(playerIdValue);
            if (_replica)
            {
                RequestsFor(playerIdValue).Send(PlayerRequest.CloseScreen(), null);
            }
        }

        /// <summary>This player's bag, or null when nobody by that id is in the world.</summary>
        public PlayerInventory InventoryOf(int playerIdValue)
        {
            IReadOnlyList<CharacterActor> actors = Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].PlayerId.Value == playerIdValue)
                {
                    return actors[i].GetComponent<PlayerInventory>();
                }
            }

            return null;
        }

        /// <summary>A remote player's request, for the host's next step to run.</summary>
        internal void QueueRemoteRequest(in PlayerRequest request) => _remoteRequests.Add(request);
```

(c) In `RunStep`, replace

```csharp
            SampleCommands(frame);
            StepPlayers(frame, actors, bounds);
```

with

```csharp
            SampleCommands(frame);
            ApplyRemoteRequests();
            StepPlayers(frame, actors, bounds);
```

(d) After the method `private void SampleCommands(int frame) => _players.SampleAll(frame, _commands);` add:

```csharp

        /// <summary>
        /// What a remote player's menus asked for, run first thing in the host's step — after intent is
        /// sampled and before anything moves (planning decision 11) — so a sale lands before a grab in the
        /// same step, exactly as the couch's press in the step before would have.
        /// </summary>
        private void ApplyRemoteRequests()
        {
            if (_remoteRequests.Count == 0)
            {
                return;
            }

            _requestScratch.Clear();
            _requestScratch.AddRange(_remoteRequests);
            _remoteRequests.Clear();
            for (int i = 0; i < _requestScratch.Count; i++)
            {
                PlayerRequest request = _requestScratch[i];
                RequestOutcome outcome = PlayerRequestRunner.Run(request, InventoryOf(request.PlayerId), this);
                RemoteRequestAnswered?.Invoke(request, outcome);
            }
        }
```

`SimulationDriver.cs` already has `using BattleBomb.Core.Items;` and `using BattleBomb.Gameplay.Items;`.

- [ ] **Step 9: Run the runner tests** — `recompile`; `run_tests` EditMode `PlayerRequestRunnerTests`. Expected: all six pass.

- [ ] **Step 10: The chest screen asks instead of calling**

In `Assets/_BattleBomb/UI/Chest/ChestScreenHost.cs`, replace

```csharp
        internal void RequestClose(int playerId) => _driver?.CloseScreen(playerId);
```

with

```csharp
        internal void RequestClose(int playerId) => _driver?.RequestClose(playerId);

        /// <summary>Where this player's menu actions go: here and now on the couch, across the wire on a
        /// guest (HANDOFF-M8 planning decision 11).</summary>
        internal IPlayerRequests RequestsFor(int playerId) => _driver != null ? _driver.RequestsFor(playerId) : null;
```

In `Assets/_BattleBomb/UI/Chest/ChestScreen.cs`:

(a) After the field `private readonly ChestNavigation _nav = new ChestNavigation();` add:

```csharp

        /// <summary>This player's menu actions (HANDOFF-M8 planning decision 11) — the screen asks, it never
        /// calls the bag itself.</summary>
        private IPlayerRequests _requests;
```

(b) In `Bind`, replace

```csharp
            _split = split;
            if (kind == InteractionKind.Shopkeeper)
```

with

```csharp
            _split = split;
            _requests = Host != null ? Host.RequestsFor(playerId) : null;
            if (kind == InteractionKind.Shopkeeper)
```

(c) In `Tick`, replace

```csharp
            if (press.Option)
            {
                RunOption();
```

with

```csharp
            // A guest's last action is still on its way to the host and back (D61): A, X and Y wait for
            // its answer, so no second action is aimed at a bag that is about to change under it.
            bool actionsWait = Waiting;

            if (press.Option && !actionsWait)
            {
                RunOption();
```

then replace `if (press.Lock)` with `if (press.Lock && !actionsWait)`, and `if (press.Confirm)` with `if (press.Confirm && !actionsWait)`.

(d) After the method `internal void CloseFromPointer() => Host?.RequestClose(_playerId);` add:

```csharp

        /// <summary>A guest's action still waiting for the host's answer.</summary>
        private bool Waiting => _requests != null && _requests.Pending;

        /// <summary>
        /// Hands an action to this player's requests (HANDOFF-M8 planning decision 11) and runs
        /// <paramref name="answered"/> with what happened. On the couch that is now, inside this call, so
        /// the code after it reads exactly as it did when the screen called the bag itself; on a guest it
        /// is a round trip later, after the host's copy of the bag has arrived — so an answer reads the bag
        /// as it now is, never as it was when the button went down.
        /// </summary>
        private void Send(PlayerRequest request, System.Action<RequestOutcome> answered)
        {
            if (_requests == null)
            {
                return;
            }

            _requests.Send(request, outcome =>
            {
                // A guest's screen can close while its request is on the wire.
                if (this == null || _bag == null)
                {
                    return;
                }

                answered?.Invoke(outcome);
                if (outcome.Refusal == RequestRefusal.StaleSack)
                {
                    Flash("The sack moved — try again.");
                }
            });
        }
```

(e) In `Confirm`, replace

```csharp
                    Flash(_bag.RequestAllocate(stats[_nav.StatCursor])
                        ? "Point spent." : "No points to spend.");
                    break;
```

with

```csharp
                    Send(PlayerRequest.Allocate(stats[_nav.StatCursor]),
                        outcome => Flash(outcome.Ok ? "Point spent." : "No points to spend."));
                    break;
```

(f) Replace the whole `RunSellJunk` method body (keep its `///` summary) with:

```csharp
        private void RunSellJunk()
        {
            QualityRank below = JunkThreshold;
            Send(PlayerRequest.SellJunk(below), outcome =>
            {
                if (!outcome.Ok)
                {
                    Flash($"Nothing below {below}.");
                    return;
                }

                CollectVisible();
                _nav.ClampCursor(_visible.Count);
                Flash($"Cleared {outcome.B} for {outcome.C}.");
            });
        }
```

(g) In `RunMenuAction`, replace

```csharp
                case ItemAction.Equip:
                    Flash(_bag.RequestEquip(bagIndex) ? "Equipped." : "Cannot equip — check the level.");
                    break;

                case ItemAction.QuickUse:
                    Flash(_bag.RequestQuickConsumable(item.DefinitionId)
                        ? "Quick-use set." : "Cannot set.");
                    break;
```

with

```csharp
                case ItemAction.Equip:
                    Send(PlayerRequest.Equip(bagIndex),
                        outcome => Flash(outcome.Ok ? "Equipped." : "Cannot equip — check the level."));
                    break;

                case ItemAction.QuickUse:
                    Send(PlayerRequest.QuickConsumable(item.DefinitionId),
                        outcome => Flash(outcome.Ok ? "Quick-use set." : "Cannot set."));
                    break;
```

(h) Replace the methods `SellAt`, `ToggleLockAt` and `ToggleWornLock` (signatures unchanged) with:

```csharp
        private void SellAt(int bagIndex)
        {
            Send(PlayerRequest.Sell(bagIndex),
                outcome => Flash(outcome.Ok ? $"Sold for {outcome.A}." : "Locked — release it first."));
        }

        private void ToggleLockAt(int bagIndex)
        {
            bool wasLocked = _bag.Inventory.Items[bagIndex].Item.Locked;
            Send(PlayerRequest.Lock(bagIndex, !wasLocked), _ => Flash(wasLocked ? "Released." : "Locked."));
        }

        private void ToggleWornLock()
        {
            ItemInstance worn = WornItem;
            if (worn.IsEmpty)
            {
                Flash("Nothing in that slot.");
                return;
            }

            HeroPanel.Slot slot = WornSlot;
            bool wasLocked = worn.Locked;
            Send(PlayerRequest.LockWorn(slot.Which, slot.EquipmentIndex, !wasLocked),
                _ => Flash(wasLocked ? "Released." : "Locked."));
        }
```

(i) In `RunWornAction`, replace

```csharp
                case ItemAction.Unequip:
                    Flash(_bag.RequestUnequip(slot.Which, slot.EquipmentIndex)
                        ? "Taken off." : "The sack is full.");
                    break;
```

with

```csharp
                case ItemAction.Unequip:
                    Send(PlayerRequest.Unequip(slot.Which, slot.EquipmentIndex),
                        outcome => Flash(outcome.Ok ? "Taken off." : "The sack is full."));
                    break;
```

(j) In `RunCombineAll`, replace everything from `_nav.CancelCombine();` to the end of the method with:

```csharp
            _nav.CancelCombine();

            Send(PlayerRequest.CombineAll(anchor), outcome =>
            {
                if (!outcome.Ok)
                {
                    RecollectOnto(anchor);
                    Flash("Nothing left to combine.");
                    return;
                }

                RecollectOnto(_bag.Inventory.Items.Count - 1);
                Flash(outcome.B > 0
                    ? $"Combined {outcome.A} — {outcome.B} UPGRADED!"
                    : $"Combined {outcome.A}.");
            });
        }
```

(k) In `RunCombine`, replace everything from `int first = _nav.PendingCombine;` to the end of the method with:

```csharp
            int first = _nav.PendingCombine;
            _nav.CancelCombine();

            Send(PlayerRequest.Combine(first, bagIndex), outcome =>
            {
                if (outcome.Ok)
                {
                    // The reroll lands at the end of the bag; the cursor follows it, since seeing
                    // what the gamble returned is the whole reason the gamble was taken.
                    int landed = _bag.Inventory.Items.Count - 1;
                    RecollectOnto(landed);
                    string name = landed >= 0 ? _bag.Inventory.Items[landed].Item.DisplayName : string.Empty;
                    Flash(outcome.A != 0 ? $"UPGRADED! {name}" : $"Rerolled: {name}");
                    return;
                }

                RecollectOnto(bagIndex);
                Flash("These two cannot combine.");
            });
        }
```

(l) In `RunUpgrade`, replace everything from `int bagIndex = _visible[_nav.Cursor];` to the end of the method with:

```csharp
            int bagIndex = _visible[_nav.Cursor];
            int price = _bag.Inventory.Prices.UpgradeCost(_bag.Inventory.Items[bagIndex].Item);

            Send(PlayerRequest.Upgrade(bagIndex, _upgradeTargets[_nav.UpgradeCursor]), outcome =>
            {
                if (outcome.Ok && bagIndex < _bag.Inventory.Items.Count)
                {
                    Flash($"Deepened for {price}.");
                    ItemUpgrade.Targets(_bag.Inventory.Items[bagIndex].Item, _upgradeTargets);
                    _nav.FinishUpgrade(ItemUpgrade.CanUpgrade(_bag.Inventory.Items[bagIndex].Item));
                    return;
                }

                Flash(_bag.Wallet.CanAfford(price)
                    ? "The capacity is spent."
                    : $"Not enough coin ({price}).");
            });
        }
```

(m) In `RunUpgradeWorn`, replace everything from `if (_bag.RequestUpgradeWorn(` to the end of the method with:

```csharp
            Send(PlayerRequest.UpgradeWorn(slot.Which, slot.EquipmentIndex, _upgradeTargets[_nav.UpgradeCursor]), outcome =>
            {
                if (outcome.Ok)
                {
                    Flash($"Deepened for {price}.");
                    ItemInstance next = WornItem;
                    ItemUpgrade.Targets(next, _upgradeTargets);
                    _nav.FinishUpgrade(ItemUpgrade.CanUpgrade(next));
                    return;
                }

                Flash(_bag.Wallet.CanAfford(price)
                    ? "The capacity is spent."
                    : $"Not enough coin ({price}).");
            });
        }
```

After (a)–(m), `grep -n "_bag\.Request" Assets/_BattleBomb/UI/Chest/ChestScreen.cs` finds nothing, and `RunBuy` still calls `Host.Buy` — Task 98 moves it. `ChestScreen.cs` needs no new `using`: `PlayerRequest`, `RequestOutcome` and `RequestRefusal` are in `BattleBomb.Core.Items`, `IPlayerRequests` in `BattleBomb.Gameplay.Items`, both already imported.

- [ ] **Step 11: Recompile and run both suites.** `recompile`, `console` `level: error` — clean. Full EditMode green. Full PlayMode (async) green — `LootLoopSmokeTests` drives the real chest screen with commands (the 45-stack grid, X selling a stack, the combine flows), and it must pass **unchanged**: it is the proof the couch did not move.

- [ ] **Step 12: The wire — host and guest halves**

`Assets/_BattleBomb/Gameplay/Net/RemotePlayerRequests.cs`:

```csharp
using System;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Items;
using BattleBomb.Platform.Net;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The guest's own player's menu actions (D61): each crosses to the host, runs there, and is answered a
    /// round trip later — after the host's copy of the bag, which travels first on the same channel. One at
    /// a time: while one is out, <see cref="Pending"/> holds the screen's hands. Closing is never held up.
    /// </summary>
    internal sealed class RemotePlayerRequests : IPlayerRequests
    {
        private readonly NetWriter _writer = new NetWriter(64);
        private readonly NetSession _net;
        private readonly Func<PlayerInventory> _bag;
        private int _nextSequence = 1;
        private int _waitingFor = -1;
        private Action<RequestOutcome> _answered;

        internal RemotePlayerRequests(NetSession net, Func<PlayerInventory> bag)
        {
            _net = net;
            _bag = bag;
        }

        public bool Pending => _waitingFor >= 0;

        public void Send(PlayerRequest request, Action<RequestOutcome> answered)
        {
            bool close = request.Kind == PlayerRequestKind.CloseScreen;
            if (Pending && !close)
            {
                answered?.Invoke(RequestOutcome.No(RequestRefusal.Busy));
                return;
            }

            PlayerInventory bag = _bag();
            PlayerRequest stamped = request
                .WithSequence(_nextSequence++)
                .WithRevision(bag != null ? bag.Inventory.Sack.Revision : 0);
            _writer.Reset();
            RequestCodec.WriteRequest(_writer, stamped);
            _net.Send(NetChannel.Reliable, _writer);

            if (!close)
            {
                _waitingFor = stamped.Sequence;
                _answered = answered;
            }
        }

        /// <summary>The host's answer. An answer to anything but the one being waited for — a close, or one
        /// abandoned — is dropped.</summary>
        internal void Answer(int sequence, in RequestOutcome outcome)
        {
            if (sequence != _waitingFor)
            {
                return;
            }

            Action<RequestOutcome> answered = _answered;
            _waitingFor = -1;
            _answered = null;
            answered?.Invoke(outcome);
        }

        /// <summary>The match is over: whatever was out will never be answered.</summary>
        internal void Abandon()
        {
            _waitingFor = -1;
            _answered = null;
        }
    }
}
```

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`:

(a) After the field `private readonly NetWriter _scratch = new NetWriter(4096);` add:

```csharp
        private readonly NetWriter _answer = new NetWriter(64);
```

(b) In `Begin`, after `_driver.PickupRemoved += OnPickupRemoved;` add:

```csharp
            _driver.RemoteRequestAnswered += OnRequestAnswered;
```

(c) In `OnMessage`, replace

```csharp
            if (kind == NetMessageKind.StageReady)
            {
                OnGuestStageReady(StageCodec.ReadStage(reader));
                return;
            }
```

with

```csharp
            if (kind == NetMessageKind.StageReady)
            {
                OnGuestStageReady(StageCodec.ReadStage(reader));
                return;
            }

            if (kind == NetMessageKind.Request)
            {
                // A guest can only ever act as itself, whatever id it wrote (planning decision 11). Run in
                // the next step's first phase, never here: this is the session's pump, outside the step.
                _driver.QueueRemoteRequest(RequestCodec.ReadRequest(reader).For(_net.GuestPlayerId.Value));
                return;
            }
```

(d) After the method `OnPickupRemoved` add:

```csharp

        private void OnRequestAnswered(PlayerRequest request, RequestOutcome outcome)
        {
            if (!_net.IsConnected || request.PlayerId != _net.GuestPlayerId.Value)
            {
                return;
            }

            _answer.Reset();
            RequestCodec.WriteResult(_answer, request.Sequence, outcome);
            _net.Send(NetChannel.Reliable, _answer);
        }
```

(e) In `OnDestroy`, after `_driver.PickupRemoved -= OnPickupRemoved;` add `_driver.RemoteRequestAnswered -= OnRequestAnswered;`.

Add `using BattleBomb.Core.Items;` to `NetHost.cs`.

In `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

(a) After the field `private readonly MenuGate _menu = new MenuGate();` add:

```csharp
        private RemotePlayerRequests _requests;
```

(b) In `Begin`, after `_world = new ReplicaWorld(_driver, _runner, FindAnyObjectByType<EnemySpawner>(), GameSession.Find());` add:

```csharp
            _requests = new RemotePlayerRequests(_net, () => _driver.InventoryOf(_local.Value));
            _driver.RequestRoute = id => id == _local.Value ? _requests : null;
```

(c) In `OnMessage`, before `case NetMessageKind.LoadStage:` add:

```csharp
                case NetMessageKind.RequestResult:
                    RequestOutcome outcome = RequestCodec.ReadResult(reader, out int sequence);
                    _requests.Answer(sequence, outcome);
                    break;

```

(d) In `OnDestroy`, replace

```csharp
            if (_driver != null)
            {
                _driver.ReplicaStepping -= OnLocalStep;
            }
```

with

```csharp
            if (_driver != null)
            {
                _driver.ReplicaStepping -= OnLocalStep;
                _driver.RequestRoute = null;
            }

            _requests?.Abandon();
```

Add `using BattleBomb.Core.Items;` to `NetGuest.cs`.

- [ ] **Step 13: The headless guest can ask**

In `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`:

(a) After `internal List<LoadStageMessage> LoadMessages { get; } = new List<LoadStageMessage>();` add:

```csharp

        /// <summary>Every answer the host sent to a request, in order.</summary>
        internal List<(int Sequence, RequestOutcome Outcome)> Results { get; } = new List<(int Sequence, RequestOutcome Outcome)>();

        internal void SendRequest(in PlayerRequest request)
        {
            _writer.Reset();
            RequestCodec.WriteRequest(_writer, request);
            _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
        }
```

(b) In `Handle`, replace

```csharp
                    else if (kind == NetMessageKind.SessionEnd)
```

with

```csharp
                    else if (kind == NetMessageKind.RequestResult)
                    {
                        RequestOutcome outcome = RequestCodec.ReadResult(reader, out int sequence);
                        Results.Add((sequence, outcome));
                        Received.Add(netEvent.Payload);
                    }
                    else if (kind == NetMessageKind.SessionEnd)
```

Add `using BattleBomb.Core.Items;` to `HeadlessGuest.cs`.

- [ ] **Step 14: Write the hosted request test**

`Assets/_BattleBomb/Tests/PlayMode/OnlineMenuSmokeTests.cs` — a new fixture with the same set-up as `OnlineHostSmokeTests` (the real Gameplay scene hosted, Player 2 a headless guest over the loopback). Its own helpers, as every smoke fixture in this project keeps its own:

```csharp
using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D45's tripwire for the menus online (HANDOFF-M8 Stage C): the real Gameplay scene hosted, Player 2 a
    /// headless guest over the loopback. The guest's menu actions reach the host as requests, run there,
    /// and are answered. Each test starts with Player 2 standing at the first chest.
    /// </summary>
    public sealed class OnlineMenuSmokeTests
    {
        private const int PatienceSteps = 1200;
        private const int FrameCeiling = 30000;
        private const int LoadFrameCeiling = 1500;
        private const int KnifeDefinitionId = 7;

        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _host;
        private CharacterActor _guestBody;
        private ScriptedCommandSource _hostInput;
        private HeadlessGuest _guest;
        private WorldInteractable _chest;

        [UnitySetUp]
        public IEnumerator HostWithAGuestAtTheChest()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-menu-smoke";

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            DisableDevices();

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.IsConnected && _guest.IsWelcomed, "the handshake never finished");

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return UntilFrames(() => _runner.IsStageLoaded, "the stage never streamed in");
            yield return UntilFrames(() => _driver.Frame > 5, "the launch hold never released");

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            Assert.That(actors.Count, Is.EqualTo(2));
            _host = actors[0];
            _guestBody = actors[1];

            DisableDevices();
            _driver.Players.Unregister(_host.PlayerId);
            _hostInput = _host.gameObject.AddComponent<ScriptedCommandSource>();
            _hostInput.Bind(_host.PlayerId.Value);
            _driver.Players.Register(_hostInput);

            _chest = FirstChest();
            Assert.That(_chest, Is.Not.Null, "The launch stage has no chest.");
            yield return WalkGuestTo(_chest.Position, "the first chest");
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            if (_guest != null)
            {
                Object.Destroy(_guest.gameObject);
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator A_guests_sale_runs_on_the_host_and_the_answer_comes_back()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            int items = bag.Inventory.Items.Count;
            int coins = bag.Wallet.Balance;
            yield return Steps(2);

            _guest.SendRequest(PlayerRequest.Sell(items - 1).WithSequence(7).WithRevision(bag.Inventory.Sack.Revision));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered the guest's sale");

            Assert.That(_guest.Results[0].Sequence, Is.EqualTo(7), "The answer did not name the request it answers.");
            Assert.That(_guest.Results[0].Outcome.Ok, Is.True, $"The sale was refused: {_guest.Results[0].Outcome.Refusal}.");
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(items - 1), "The host's copy of the guest's bag still has the item.");
            Assert.That(bag.Wallet.Balance, Is.EqualTo(coins + _guest.Results[0].Outcome.A), "The coins the answer names never reached the wallet.");
        }

        [UnityTest]
        public IEnumerator A_guest_can_only_ever_act_as_itself()
        {
            PlayerInventory guestBag = _guestBody.GetComponent<PlayerInventory>();
            PlayerInventory hostBag = _host.GetComponent<PlayerInventory>();
            guestBag.Earn(5000f);
            hostBag.Earn(5000f);
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            yield return Steps(2);

            // Written as Player 1's: the host must stamp the guest's own id over it.
            _guest.SendRequest(PlayerRequest.Allocate(StatId.Strength).For(0).WithSequence(1));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            Assert.That(guestBag.Ledger.Allocations.Strength, Is.EqualTo(1), "The guest's point went nowhere.");
            Assert.That(hostBag.Ledger.Allocations.Strength, Is.Zero, "The guest spent the host's point.");
        }

        [UnityTest]
        public IEnumerator A_guests_request_aimed_at_an_old_view_of_the_sack_is_refused()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            int stale = bag.Inventory.Sack.Revision;
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            int items = bag.Inventory.Items.Count;
            yield return Steps(2);

            _guest.SendRequest(PlayerRequest.Sell(0).WithSequence(3).WithRevision(stale));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            Assert.That(_guest.Results[0].Outcome.Refusal, Is.EqualTo(RequestRefusal.StaleSack));
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(items), "A sale aimed at an old view of the sack still sold.");
        }

        [UnityTest]
        public IEnumerator With_no_chest_open_a_guests_sale_does_nothing()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            int items = bag.Inventory.Items.Count;
            yield return Steps(2);

            _guest.SendRequest(PlayerRequest.Sell(0).WithSequence(5).WithRevision(bag.Inventory.Sack.Revision));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            Assert.That(_guest.Results[0].Outcome.Refusal, Is.EqualTo(RequestRefusal.NoScreen));
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(items));
        }

        private WorldInteractable FirstChest()
        {
            WorldInteractable first = null;
            foreach (WorldInteractable candidate in Object.FindObjectsByType<WorldInteractable>(FindObjectsInactive.Exclude))
            {
                if (candidate.Kind == InteractionKind.Chest && candidate.gameObject.scene == _runner.StageScene
                    && (first == null || candidate.Position.x < first.Position.x))
                {
                    first = candidate;
                }
            }

            return first;
        }

        /// <summary>Walks Player 2 over the wire until they stand within the chest's reach.</summary>
        private IEnumerator WalkGuestTo(Vector3 target, string what)
        {
            int deadline = _driver.Frame + 5000;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                Vector3 to = target - _guestBody.Position;
                to.y = 0f;
                if (to.magnitude <= 0.8f)
                {
                    _guest.Move = Vector2.zero;
                    yield return Steps(4);
                    yield break;
                }

                _guest.Move = new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f));
                yield return null;
            }

            _guest.Move = Vector2.zero;
            Assert.Fail($"Player 2 never reached {what} — stopped {Vector3.Distance(target, _guestBody.Position):F2} away.");
        }

        private static void DisableDevices()
        {
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }
        }

        private IEnumerator Steps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {PatienceSteps} steps).");
        }

        private static IEnumerator UntilFrames(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < LoadFrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {LoadFrameCeiling} frames).");
        }
    }
}
```

The screen is opened with `SimulationDriver.OpenScreen` because the guest cannot open one itself until Task 99 retires `MayOpenScreen`; Player 2 stands within the chest's reach, so the driver's "no chest under it" rule keeps it open. Until Task 101 the guest's body shares the host's stash (Plan 1), so these tests read Player 2's bag and never compare stashes.

- [ ] **Step 15: Run the new fixture, then both suites** — `recompile`; `run_tests` PlayMode (async) `OnlineMenuSmokeTests`. Expected: four pass. If `WalkGuestTo` times out, read where Player 2 stopped: the first arena's gate opens only once its waves are counted cleared (spawns are off), so a clamp at the arena edge for the first second is normal. Then full EditMode (expected 791 + 15 = 806) and full PlayMode (65 + 4 = 69). Delete `Assets/InitTestScene*`.

- [ ] **Step 16: Commit** — subject `97: the requests seam — every menu action is a request`. Body: a menu action is a value (player, verb, arguments, sack revision) run by one runner; on the couch it runs inside the call, so the chest screen reads exactly as before and `LootLoopSmokeTests` passes unchanged; the host runs a guest's request in the step's first phase, stamps the guest's own id over it, refuses a stale sack or a closed chest, and answers; protocol 3.

---

### Task 98: The rack in the simulation

The audit's rule 2 finding (readiness §5.1): the shopkeeper's rack is rolled by the chest screen, held in a UI list, and bought from by handing the bag an item and a price the UI chose. Online that would let a guest's screen name any item at any price. The rack moves into the driver: opening a shop rolls that player's rack from the loot stream — the same draws, at the same moment, as the screen made before — the screen draws a copy of it, and buying names a rack slot while the price comes from the buyer's own price book. On the couch the only visible change is that a screen rebuilt mid-visit shows the same rack.

**Files:**
- Modify: `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF), `Gameplay/Items/PlayerInventory.cs`, `Gameplay/Items/PlayerRequestRunner.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.cs`, `UI/Chest/ChestScreenHost.cs`
- Test: modify `Assets/_BattleBomb/Tests/EditMode/PlayerRequestRunnerTests.cs`, `Tests/PlayMode/LootLoopSmokeTests.cs`

- [ ] **Step 1: Write the failing tests**

In `Assets/_BattleBomb/Tests/EditMode/PlayerRequestRunnerTests.cs`, before the closing brace of the class add:

```csharp

        [Test]
        public void Buying_and_the_junk_sweep_need_the_shopkeeper()
        {
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);

            Assert.That(Run(PlayerRequest.Buy(0)).Refusal, Is.EqualTo(RequestRefusal.NoScreen), "A chest bought from a rack.");
            Assert.That(Run(PlayerRequest.SellJunk(QualityRank.Legendary)).Refusal, Is.EqualTo(RequestRefusal.NoScreen),
                "The junk sweep ran at a chest; it lives on the shopkeeper's counter.");
            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(1));
        }
```

In `Assets/_BattleBomb/Tests/PlayMode/LootLoopSmokeTests.cs`, after the test `A_player_at_a_chest_takes_no_orders` add:

```csharp

        [UnityTest]
        public IEnumerator The_shopkeepers_rack_is_the_simulations_and_a_purchase_names_its_slot()
        {
            // Standing at the chest keeps any screen open (the driver closes one with nothing in reach),
            // so the shop is opened there by hand: this is about the rack, not the counter's placement.
            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            int id = _player.PlayerId.Value;
            _driver.OpenScreen(id, InteractionKind.Shopkeeper);
            yield return SimulationFrames(2);

            IReadOnlyList<ItemInstance> rack = _driver.RackFor(id);
            Assert.That(rack.Count, Is.GreaterThan(0), "Opening the shop rolled no rack.");
            int racked = rack.Count;
            int price = _bag.Inventory.Prices.BuyPrice(rack[0]);
            _bag.GrantCoins(price + 10);
            int coins = _bag.Wallet.Balance;
            int items = _bag.Inventory.Items.Count;

            RequestOutcome outcome = default;
            _driver.RequestsFor(id).Send(PlayerRequest.Buy(0), answer => outcome = answer);

            Assert.That(outcome.Ok, Is.True, $"The purchase was refused: {outcome.Refusal}.");
            Assert.That(outcome.A, Is.EqualTo(price), "The simulation charged something other than its own price book's price.");
            Assert.That(_bag.Wallet.Balance, Is.EqualTo(coins - price));
            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(items + 1), "The bought piece never reached the sack.");
            Assert.That(_driver.RackFor(id).Count, Is.EqualTo(racked - 1), "The bought piece is still on the rack.");
        }

        [UnityTest]
        public IEnumerator The_rack_lasts_the_visit_and_goes_with_it()
        {
            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            int id = _player.PlayerId.Value;
            _driver.OpenScreen(id, InteractionKind.Shopkeeper);
            yield return SimulationFrames(2);

            var names = new List<string>();
            foreach (ItemInstance piece in _driver.RackFor(id))
            {
                names.Add(piece.DisplayName);
            }

            yield return SimulationFrames(30);
            var later = new List<string>();
            foreach (ItemInstance piece in _driver.RackFor(id))
            {
                later.Add(piece.DisplayName);
            }

            Assert.That(later, Is.EqualTo(names), "The rack changed in the middle of one visit.");

            _driver.CloseScreen(id);
            Assert.That(_driver.RackFor(id), Is.Empty, "The rack outlived the visit.");
        }
```

Add `using BattleBomb.Core.Items;` and `using BattleBomb.Gameplay.Items;` to `LootLoopSmokeTests.cs` if they are not already there.

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors — `SimulationDriver.RackFor` does not exist.

- [ ] **Step 3: The rack in the driver**

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF):

(a) Replace the method `RollShopStock` in full (its `///` summary included) with:

```csharp
        /// <summary>Pieces the shopkeeper offers per visit (D43's "3–4 rolled gear pieces").</summary>
        public const int RackSize = 4;

        /// <summary>
        /// Each player's shopkeeper rack while their shop is open (D43, HANDOFF-M8 planning decision 12):
        /// rolled from the loot stream when the visit opens, emptied slot by slot as it is bought from, gone
        /// when the visit ends. The simulation's, not the screen's — a screen may be rebuilt, and online the
        /// screen is on another machine.
        /// </summary>
        private readonly Dictionary<int, List<ItemInstance>> _racks = new Dictionary<int, List<ItemInstance>>();

        private static readonly List<ItemInstance> NoRack = new List<ItemInstance>();

        /// <summary>A player's rack was rolled, bought from, or cleared.</summary>
        public event Action<int> RackChanged;

        /// <summary>This player's rack for the visit, or empty when no shop is open for them.</summary>
        public IReadOnlyList<ItemInstance> RackFor(int playerIdValue) =>
            _racks.TryGetValue(playerIdValue, out List<ItemInstance> rack) ? rack : NoRack;

        /// <summary>
        /// Buying from the rack (D43): the piece in that slot, at the buyer's own price for it. The sack's
        /// refusal refunds nothing because nothing was taken; the slot empties only once the piece is in.
        /// </summary>
        internal RequestOutcome BuyFromRack(int playerIdValue, int slot, PlayerInventory bag)
        {
            if (bag == null || !_racks.TryGetValue(playerIdValue, out List<ItemInstance> rack)
                || slot < 0 || slot >= rack.Count)
            {
                return RequestOutcome.No(RequestRefusal.Refused);
            }

            ItemInstance piece = rack[slot];
            int price = bag.Inventory.Prices.BuyPrice(piece);
            if (!bag.RequestBuy(piece, price))
            {
                return RequestOutcome.No(RequestRefusal.Refused);
            }

            rack.RemoveAt(slot);
            RackChanged?.Invoke(playerIdValue);
            return RequestOutcome.Done(price);
        }

        /// <summary>
        /// A visit's rack: a handful of generator-rolled pieces at current progress quality, rolled fresh
        /// per visit from the loot stream — exactly the draws the chest screen made before the rack moved
        /// here, at the same moment, which is what keeps the couch's loot sequence unchanged.
        /// </summary>
        private void RollRack(int playerIdValue)
        {
            if (!_racks.TryGetValue(playerIdValue, out List<ItemInstance> rack))
            {
                rack = new List<ItemInstance>(RackSize);
                _racks[playerIdValue] = rack;
            }

            rack.Clear();
            for (int i = 0; i < RackSize; i++)
            {
                _lootRng = _lootRng.NextFloat(out float spread);
                var context = new GenerationContext(
                    (DropRoll.SpreadMin + spread) * Encounter.LootProgress * Encounter.LootDifficulty,
                    Encounter.LevelStamp, _itemSpecs, _qualityTable, _dropWeights, _elements.Ids);
                _lootRng = ItemGenerator.Roll(_lootRng, context, out ItemInstance rolled);
                if (!rolled.IsEmpty)
                {
                    rack.Add(rolled);
                }
            }

            RackChanged?.Invoke(playerIdValue);
        }

        private void ClearRack(int playerIdValue)
        {
            if (_racks.Remove(playerIdValue))
            {
                RackChanged?.Invoke(playerIdValue);
            }
        }
```

(b) In `OpenScreen(int playerIdValue, InteractionKind kind, WorldInteractable source)`, replace

```csharp
            else
            {
                _openSources.Remove(playerIdValue);
            }

            ScreenChanged?.Invoke(playerIdValue, kind, true);
```

with

```csharp
            else
            {
                _openSources.Remove(playerIdValue);
            }

            // Rolled before anyone hears of the screen, so the screen that answers finds its rack waiting.
            // Never on a guest: a replica rolls nothing (D58).
            if (kind == InteractionKind.Shopkeeper && !_replica)
            {
                RollRack(playerIdValue);
            }
            else
            {
                ClearRack(playerIdValue);
            }

            ScreenChanged?.Invoke(playerIdValue, kind, true);
```

(c) In `CloseScreen`, replace

```csharp
            _openScreens.Remove(playerIdValue);
            _openSources.Remove(playerIdValue);
            ScreenChanged?.Invoke(playerIdValue, kind, false);
```

with

```csharp
            _openScreens.Remove(playerIdValue);
            _openSources.Remove(playerIdValue);
            ClearRack(playerIdValue);
            ScreenChanged?.Invoke(playerIdValue, kind, false);
```

In `Assets/_BattleBomb/Gameplay/Items/PlayerInventory.cs`, replace

```csharp
        /// <summary>Buying from the shopkeeper (D43): the price leaves the wallet, the item
        /// lands in the sack — and a sack that refuses it refunds nothing, so the check comes
        /// first.</summary>
        public bool RequestBuy(in ItemInstance item, int price)
```

with

```csharp
        /// <summary>Buying from the shopkeeper (D43): the price leaves the wallet, the item
        /// lands in the sack — and a sack that refuses it refunds nothing, so the check comes
        /// first. Internal since the rack moved into the simulation (HANDOFF-M8 planning decision
        /// 12): only the driver, which holds the rack and prices it, may name an item and a price.</summary>
        internal bool RequestBuy(in ItemInstance item, int price)
```

In `Assets/_BattleBomb/Gameplay/Items/PlayerRequestRunner.cs`:

(a) Replace

```csharp
                case PlayerRequestKind.SellJunk:
                    JunkSale sale = bag.RequestSellJunk((QualityRank)request.A);
```

with

```csharp
                case PlayerRequestKind.SellJunk when screen != InteractionKind.Shopkeeper:
                case PlayerRequestKind.Buy when screen != InteractionKind.Shopkeeper:
                    // The sweep and the rack live on the shopkeeper's counter, never at a chest (D43).
                    return RequestOutcome.No(RequestRefusal.NoScreen);

                case PlayerRequestKind.Buy:
                    return driver.BuyFromRack(request.PlayerId, request.A, bag);

                case PlayerRequestKind.SellJunk:
                    JunkSale sale = bag.RequestSellJunk((QualityRank)request.A);
```

(b) Replace the `default:` comment

```csharp
                    // Buy arrives with the rack in the simulation (Task 98), DebugGrant with the settings
                    // online (Task 100). Until then, asked for either, the host does nothing.
```

with

```csharp
                    // DebugGrant arrives with the settings online (Task 100). Until then the host does nothing.
```

- [ ] **Step 4: The screen draws the simulation's rack**

In `Assets/_BattleBomb/UI/Chest/ChestScreenHost.cs`:

(a) Replace

```csharp
        /// <summary>The shopkeeper's rack for one visit (D43) — the driver owns the generator.</summary>
        internal void RollStock(List<Core.Items.ItemInstance> stock, int count) =>
            _driver?.RollShopStock(stock, count);

        /// <summary>One purchase. The bag checks the money and the room; the UI only asks.</summary>
        internal bool Buy(PlayerInventory bag, in Core.Items.ItemInstance item, int price) =>
            bag != null && bag.RequestBuy(item, price);
```

with

```csharp
        /// <summary>The shopkeeper's rack for this player's visit (D43) — the simulation's; the screen draws a copy.</summary>
        internal IReadOnlyList<Core.Items.ItemInstance> RackFor(int playerId) =>
            _driver != null ? _driver.RackFor(playerId) : (IReadOnlyList<Core.Items.ItemInstance>)System.Array.Empty<Core.Items.ItemInstance>();
```

(b) In `OnEnable`, after `_driver.MenuStepped += OnMenuStepped;` add `_driver.RackChanged += OnRackChanged;`; in `OnDisable`, after `_driver.MenuStepped -= OnMenuStepped;` add `_driver.RackChanged -= OnRackChanged;`.

(c) After the method `OnScreenChanged` add:

```csharp

        /// <summary>A purchase emptied a slot, or — on a guest — the host's rack arrived.</summary>
        private void OnRackChanged(int playerId)
        {
            if (_screens.TryGetValue(playerId, out ChestScreen screen) && screen != null)
            {
                screen.OnRackChanged();
            }
        }
```

In `Assets/_BattleBomb/UI/Chest/ChestScreen.cs`:

(a) Replace

```csharp
        /// <summary>Pieces the shopkeeper offers per visit (D43's "3–4 rolled gear pieces").</summary>
        private const int ShopStockCount = 4;
```

with

```csharp
        /// <summary>The rack's rows — the simulation's rack size (D43), which the layout is drawn for.</summary>
        private const int ShopStockCount = Gameplay.Simulation.SimulationDriver.RackSize;
```

(b) Replace

```csharp
        /// <summary>The shopkeeper's rack for this visit (D43) — empty at a chest.</summary>
        private readonly List<ItemInstance> _stock = new List<ItemInstance>();
```

with

```csharp
        /// <summary>A copy of the simulation's rack for this visit (D43, HANDOFF-M8 planning decision 12),
        /// taken by <see cref="CollectVisible"/> — empty at a chest.</summary>
        private readonly List<ItemInstance> _stock = new List<ItemInstance>();
```

(c) In `Bind`, replace

```csharp
                // Rolled once per visit, so coming back later is worth doing (D43).
                Host?.RollStock(_stock, ShopStockCount);
                CollectVisible();
```

with

```csharp
                // The simulation rolled this visit's rack as the shop opened (D43); CollectVisible copies it.
                CollectVisible();
```

(d) After the method `OnBagChanged` add:

```csharp

        /// <summary>The rack moved — a purchase, or on a guest the host's rack arriving.</summary>
        internal void OnRackChanged() => Refresh();
```

(e) Replace the method `RunBuy` (its `///` summary included) with:

```csharp
        /// <summary>Buying from the rack (D43): the screen names the slot and the simulation does the rest —
        /// the piece, its price, the money and the room (HANDOFF-M8 planning decision 12).</summary>
        private void RunBuy()
        {
            if (_nav.StockCursor < 0 || _nav.StockCursor >= _stock.Count)
            {
                return;
            }

            int price = _bag.Inventory.Prices.BuyPrice(_stock[_nav.StockCursor]);
            Send(PlayerRequest.Buy(_nav.StockCursor), outcome =>
            {
                if (!outcome.Ok)
                {
                    Flash(_bag.Inventory.IsFull ? "The sack is full." : $"Not enough coin ({price}).");
                    return;
                }

                CollectVisible();
                _nav.FinishBuy(_stock.Count);
                Flash($"Bought for {outcome.A}.");
            });
        }
```

(f) In `CollectVisible`, make its first line

```csharp
            CollectRack();
```

and after the method `CollectVisible` add:

```csharp

        /// <summary>The rack as the simulation holds it — copied every time, never kept, so a screen rebuilt
        /// mid-visit, or a guest's screen after the host's rack arrives, draws the one rack there is.</summary>
        private void CollectRack()
        {
            _stock.Clear();
            if (!IsShop || Host == null)
            {
                return;
            }

            IReadOnlyList<ItemInstance> rack = Host.RackFor(_playerId);
            for (int i = 0; i < rack.Count; i++)
            {
                _stock.Add(rack[i]);
            }
        }
```

After these edits, `grep -n "RollShopStock\|RollStock\|Host.Buy" Assets/_BattleBomb` finds nothing.

- [ ] **Step 5: Run the tests** — `recompile`, `console` `level: error` clean; `run_tests` EditMode `PlayerRequestRunnerTests` (seven pass); PlayMode (async) `LootLoopSmokeTests` (every case passes, the two new ones included). Then both full suites: EditMode 807, PlayMode 71. Delete `Assets/InitTestScene*`.

- [ ] **Step 6: Live check (settled state — drive it yourself; no QUIET needed, one editor).** Open `Gameplay`, enter play mode, walk Player 1 to the first chest by `eval` (or the keyboard), and open a shop with `eval`: `Object.FindAnyObjectByType<SimulationDriver>().OpenScreen(0, InteractionKind.Shopkeeper)`. `capture_game_view`: the rack shows up to four pieces with prices. Grant coin with the settings menu's debug row, buy one with A, and check the rack has one fewer, the coin went down by the shown price, and the piece is in the sack. Close with B. `editor_stop`.

- [ ] **Step 7: Commit** — subject `98: the rack in the simulation — buy names a slot`. Body: the audit's rule 2 finding closed: the driver rolls a visit's rack from the loot stream when the shop opens (the same draws the screen made, at the same moment), the screen draws a copy, and a purchase names a slot while the price comes from the buyer's own price book; `RequestBuy` is internal; the junk sweep and the rack are shop-only on the host.

---

### Task 99: The guest's screens and the guest's bag

The guest's chest, shop and hero panel open on the guest's own display, over a copy of the guest's own bag that the host keeps current. Four pieces: the registry learns which players are **local** (their hands, and so their screens, are on this machine); the host tells the guest when a screen opens or closes and what is on a rack — reliable events, taken on arrival because they are menu state, not picture; the host sends each player's inventory (**participant state**) — the guest's whole bag and wallet, and the partner's worn gear so the guest's copy of the partner swings the right weapon; and the guest's screens ask through Task 97's requests. `MayOpenScreen`, Plan 1's stand-in, retires.

**Files:**
- Modify: `Assets/_BattleBomb/Core/Net/NetWriter.cs`, `Core/Net/NetReader.cs`, `Core/Net/NetProtocol.cs`, `Core/Net/NetMessageKind.cs`, `Core/Net/ReplicatedEvents.cs`, `Core/Net/EventCodec.cs`, `Core/Saves/SaveMapper.cs`
- Create: `Assets/_BattleBomb/Core/Net/NetDeflate.cs`, `Core/Net/ParticipantCodec.cs`
- Create: `Assets/_BattleBomb/Gameplay/Players/IRemotePlayerSource.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Players/PlayerRegistry.cs`, `Gameplay/Net/RemoteCommandSource.cs`, `Gameplay/Items/PlayerInventory.cs`, `Gameplay/Simulation/SimulationDriver.cs` (CRLF), `Gameplay/Net/NetHost.cs`, `Gameplay/Net/NetGuest.cs`, `Gameplay/Net/ReplicaWorld.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreenHost.cs`, `UI/Chest/ChestScreen.cs`
- Test: create `Tests/EditMode/Net/ParticipantCodecTests.cs`, `Tests/EditMode/PlayerRegistryLocalTests.cs`, `Tests/PlayMode/GuestMenuSmokeTests.cs`; modify `Tests/EditMode/Net/EventCodecTests.cs`, `Tests/PlayMode/OnlineMenuSmokeTests.cs`

- [ ] **Step 1: Write the failing Core tests**

`Assets/_BattleBomb/Tests/EditMode/Net/ParticipantCodecTests.cs`:

```csharp
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>
    /// A player's inventory crosses the wire in the save's own format, deflated (HANDOFF-M8 Task 99): a
    /// field added to the save travels without a second codec to keep in step. The guest's copy of its
    /// bag, and of its partner's worn gear, is rebuilt from it every time it changes.
    /// </summary>
    public sealed class ParticipantCodecTests
    {
        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), new GearContribution(weaponDamage: 9f)),
        };

        private static ItemInstance Knife(int affixes = 0)
        {
            var rolls = new AffixRoll[affixes];
            for (int i = 0; i < affixes; i++)
            {
                rolls[i] = new AffixRoll(AffixId.CritChance, 0.01f * (i + 1), ElementId.None);
            }

            return new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), rolls, requiredLevel: 1, new ItemInvestment(3));
        }

        private static CharacterState Wearing(Inventory inventory) =>
            new CharacterState(new ElementId(2), new XpLedger(4, 12.5f, 1, BaseStats.Zero, 0), inventory);

        private static ParticipantMessage RoundTrip(int playerId, int revision, bool full, SaveGame state)
        {
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, playerId, revision, full, state);
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Participant));
            ParticipantMessage back = ParticipantCodec.Read(reader);
            Assert.That(reader.Remaining, Is.Zero);
            return back;
        }

        [Test]
        public void A_players_whole_inventory_travels_in_the_saves_own_format()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.TryEquip(0, 99);
            inventory.Add(Knife(2), 99);
            inventory.Add(Knife(), 99);
            inventory.AutoSell = true;

            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(250), Wearing(inventory), withSack: true);
            ParticipantMessage back = RoundTrip(1, 41, true, state);

            Assert.That(back.PlayerId, Is.EqualTo(1));
            Assert.That(back.Revision, Is.EqualTo(41));
            Assert.That(back.Full, Is.True);
            Assert.That(back.State.Coins, Is.EqualTo(250));
            Assert.That(back.State.AutoSell, Is.True);
            Assert.That(back.State.Sack.Length, Is.EqualTo(2));
            Assert.That(back.State.Characters.Length, Is.EqualTo(1));
            Assert.That(back.State.Characters[0].Level, Is.EqualTo(4));
            Assert.That(back.State.Characters[0].Worn.Length, Is.EqualTo(1), "The worn knife did not travel.");
            Assert.That(back.State.Sack[0].Item.Affixes.Length, Is.EqualTo(2));
        }

        [Test]
        public void A_partners_gear_travels_without_their_sack()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.TryEquip(0, 99);
            inventory.Add(Knife(), 99);

            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(250), Wearing(inventory), withSack: false);
            ParticipantMessage back = RoundTrip(0, 3, false, state);

            Assert.That(back.Full, Is.False);
            Assert.That(back.State.Sack, Is.Empty, "The partner's sack crossed the wire; only what they wear should.");
            Assert.That(back.State.Coins, Is.Zero);
            Assert.That(back.State.Characters[0].Worn.Length, Is.EqualTo(1));
        }

        [Test]
        public void A_full_sack_fits_one_message_with_room_to_spare()
        {
            var inventory = new Inventory();
            for (int i = 0; i < inventory.Rules.Capacity; i++)
            {
                inventory.Add(Knife(4), 99);
            }

            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(99999), Wearing(inventory), withSack: true);
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, 1, 1, true, state);

            Assert.That(writer.Length, Is.LessThan(NetProtocol.MaxParticipantBytes),
                $"A full sack is {writer.Length} bytes on the wire.");
            Assert.That(RoundTrip(1, 1, true, state).State.Sack.Length, Is.EqualTo(inventory.Rules.Capacity));
        }

        [Test]
        public void Bytes_that_are_not_a_save_are_refused_at_the_door()
        {
            var writer = new NetWriter();
            writer.WriteByte((byte)NetMessageKind.Participant);
            writer.WriteInt(1);
            writer.WriteInt(1);
            writer.WriteBool(true);
            writer.WriteBlob(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            Assert.Throws<NetFormatException>(() => ParticipantCodec.Read(reader));
        }

        [Test]
        public void A_blob_longer_than_it_may_be_is_refused_before_it_is_read()
        {
            var writer = new NetWriter();
            writer.WriteInt(NetProtocol.MaxParticipantBytes + 1);

            Assert.Throws<NetFormatException>(() => new NetReader(writer.ToArray()).ReadBlob(NetProtocol.MaxParticipantBytes));
        }

        [Test]
        public void A_second_restore_replaces_a_loadout_rather_than_adding_to_it()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.TryEquip(0, 99);
            Assert.That(inventory.Loadout.Weapon.IsEmpty, Is.False);

            var nothingWorn = new CharacterSave(2, 1, 0f, 0, 0, 0, 0, 0, 0, new WornSave[0], 0, 0, 0);
            SaveMapper.RestoreCharacter(nothingWorn, inventory, Catalog);

            Assert.That(inventory.Loadout.Weapon.IsEmpty, Is.True, "A restore wearing nothing left the old knife on.");
        }

        [Test]
        public void A_guests_copy_of_its_sack_takes_the_hosts_revision()
        {
            var sack = new Sack();
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(5), Wearing(inventory), withSack: true);

            SaveMapper.RestoreSack(state, sack, Catalog, 777);

            Assert.That(sack.Revision, Is.EqualTo(777));
            Assert.That(sack.Items.Count, Is.EqualTo(1));
        }
    }
}
```

In `Assets/_BattleBomb/Tests/EditMode/Net/EventCodecTests.cs`, before the closing brace of the class add:

```csharp

        [Test]
        public void Screens_and_racks_round_trip_with_their_steps()
        {
            var knife = new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, default, default), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], 2);
            var sent = new List<ReplicatedEvent>
            {
                ReplicatedEvent.OfRack(1, new[] { knife, knife }).At(40),
                ReplicatedEvent.OfScreen(1, 1, true).At(40),
                ReplicatedEvent.OfScreen(0, 0, false).At(41),
                ReplicatedEvent.OfRack(1, new ItemInstance[0]).At(42),
            };

            var writer = new NetWriter();
            EventCodec.Write(writer, sent);
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            var received = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, received);

            Assert.That(received.Count, Is.EqualTo(4));
            Assert.That(received[0].Kind, Is.EqualTo(ReplicatedEventKind.RackChanged));
            Assert.That(received[0].Rack.PlayerId, Is.EqualTo(1));
            Assert.That(received[0].Rack.Pieces.Length, Is.EqualTo(2));
            Assert.That(received[0].Rack.Pieces[1].DefinitionId, Is.EqualTo(7));
            Assert.That(received[1].Kind, Is.EqualTo(ReplicatedEventKind.ScreenOpened));
            Assert.That(received[1].Screen.PlayerId, Is.EqualTo(1));
            Assert.That(received[1].Screen.Kind, Is.EqualTo(1));
            Assert.That(received[2].Kind, Is.EqualTo(ReplicatedEventKind.ScreenClosed));
            Assert.That(received[2].HostFrame, Is.EqualTo(41));
            Assert.That(received[3].Rack.Pieces, Is.Empty);
        }
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors (`ParticipantCodec`, `SaveMapper.Participant`, `WriteBlob`, `ReplicatedEvent.OfRack` do not exist).

- [ ] **Step 3: Bytes, deflate, and the participant codec**

In `Assets/_BattleBomb/Core/Net/NetWriter.cs`, after the method `WriteCount` add:

```csharp

        /// <summary>Raw bytes behind a 32-bit length — a deflated payload (HANDOFF-M8 Task 99).</summary>
        public void WriteBlob(byte[] bytes)
        {
            byte[] value = bytes ?? Array.Empty<byte>();
            if (value.Length > NetProtocol.MaxMessageBytes)
            {
                throw new NetFormatException($"A {value.Length}-byte blob does not fit a message.");
            }

            WriteInt(value.Length);
            Ensure(value.Length);
            Array.Copy(value, 0, _buffer, Length, value.Length);
            Length += value.Length;
        }
```

In `Assets/_BattleBomb/Core/Net/NetReader.cs`, after the method `ReadCount` add:

```csharp

        /// <summary>Raw bytes behind a 32-bit length, refused before a byte is copied if longer than
        /// <paramref name="maxBytes"/>.</summary>
        public byte[] ReadBlob(int maxBytes)
        {
            int length = ReadInt();
            if (length < 0 || length > maxBytes)
            {
                throw new NetFormatException($"A {length}-byte blob where at most {maxBytes} is allowed.");
            }

            Need(length);
            var value = new byte[length];
            Array.Copy(_data, Position, value, 0, length);
            Position += length;
            return value;
        }
```

In `Assets/_BattleBomb/Core/Net/NetProtocol.cs`:

(a) Replace `public const int Version = 3;` with `public const int Version = 4;` and add `4: the guest's screens, racks and inventory (Task 99).` to its summary.

(b) After `public const int MaxItemJsonBytes = 8192;` add:

```csharp

        /// <summary>A player's inventory, deflated. A full 200-stack sack of four-affix gear is ~15 KB.</summary>
        public const int MaxParticipantBytes = 192 * 1024;

        /// <summary>What a participant may inflate to — a decompression bomb stops here, not at memory's end.</summary>
        public const int MaxParticipantJsonBytes = 4 * 1024 * 1024;

        /// <summary>Pieces a rack may carry on the wire; the simulation rolls four (D43).</summary>
        public const int MaxRack = 16;

        /// <summary>
        /// Steps between two sends of one player's inventory when nothing forces it: a kill's XP changes the
        /// bag's owner every few seconds in a fight, and a quarter of a second is quick enough for an XP bar.
        /// A request's answer, a screen opening and an autosave send it at once.
        /// </summary>
        public const int ParticipantMinSteps = 15;
```

In `Assets/_BattleBomb/Core/Net/NetMessageKind.cs`, replace

```csharp
        RequestResult = 15,
    }
```

with

```csharp
        RequestResult = 15,
        Participant = 16,
    }
```

`Assets/_BattleBomb/Core/Net/NetDeflate.cs`:

```csharp
using System.IO;
using System.IO.Compression;

namespace BattleBomb.Core.Net
{
    /// <summary>Deflate for payloads that are mostly repeated field names — a save's JSON shrinks ten-fold.
    /// Inflating is bounded, so a hostile payload is refused rather than filling memory.</summary>
    public static class NetDeflate
    {
        public static byte[] Pack(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new DeflateStream(output, CompressionLevel.Fastest, true))
                {
                    deflate.Write(raw, 0, raw.Length);
                }

                return output.ToArray();
            }
        }

        public static byte[] Unpack(byte[] packed, int maxBytes)
        {
            try
            {
                using (var input = new MemoryStream(packed))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[8192];
                    int read;
                    while ((read = deflate.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (output.Length + read > maxBytes)
                        {
                            throw new NetFormatException($"A payload that inflates past {maxBytes} bytes.");
                        }

                        output.Write(buffer, 0, read);
                    }

                    return output.ToArray();
                }
            }
            catch (InvalidDataException e)
            {
                throw new NetFormatException($"A payload that is not deflate: {e.Message}");
            }
        }
    }
}
```

If `System.IO.Compression` does not compile in the Core assembly, stop and tell the orchestrator before working around it: the fallback is to send the JSON uncompressed with the same bounds (a full sack is ~150 KB, still under the 256 KB frame), but that is a decision, not a quiet change.

`Assets/_BattleBomb/Core/Net/ParticipantCodec.cs`:

```csharp
using System.Text;
using BattleBomb.Core.Saves;

namespace BattleBomb.Core.Net
{
    /// <summary>One player's inventory as the host holds it, at a sack revision.</summary>
    public readonly struct ParticipantMessage
    {
        public readonly int PlayerId;
        public readonly int Revision;

        /// <summary>The sack, wallet and auto flags came too — the guest's own player. Otherwise only what the
        /// player wears, their quick slot and their ledger — the partner, for the guest's copy of them.</summary>
        public readonly bool Full;

        public readonly SaveGame State;

        public ParticipantMessage(int playerId, int revision, bool full, SaveGame state)
        {
            PlayerId = playerId;
            Revision = revision;
            Full = full;
            State = state;
        }
    }

    /// <summary>
    /// A player's inventory on the wire, in the save's own format (a one-character <see cref="SaveGame"/>
    /// through <see cref="SaveCodec"/>), deflated — so a field added to the save travels with no second
    /// codec to keep in step (the argument <c>ItemWire</c> made for single items). Readers take a reader
    /// positioned after the kind byte.
    /// </summary>
    public static class ParticipantCodec
    {
        public static void Write(NetWriter w, int playerId, int revision, bool full, SaveGame state)
        {
            w.WriteByte((byte)NetMessageKind.Participant);
            w.WriteInt(playerId);
            w.WriteInt(revision);
            w.WriteBool(full);
            WriteState(w, state);
        }

        public static ParticipantMessage Read(NetReader r)
        {
            int playerId = r.ReadInt();
            int revision = r.ReadInt();
            bool full = r.ReadBool();
            return new ParticipantMessage(playerId, revision, full, ReadState(r));
        }

        /// <summary>A save, deflated, as a blob — for any message that carries one (the lobby's pick too).</summary>
        public static void WriteState(NetWriter w, SaveGame state)
        {
            byte[] packed = NetDeflate.Pack(Encoding.UTF8.GetBytes(SaveCodec.Encode(state)));
            if (packed.Length > NetProtocol.MaxParticipantBytes)
            {
                throw new NetFormatException($"An inventory of {packed.Length} deflated bytes is too large to send.");
            }

            w.WriteBlob(packed);
        }

        public static SaveGame ReadState(NetReader r)
        {
            byte[] packed = r.ReadBlob(NetProtocol.MaxParticipantBytes);
            string json = Encoding.UTF8.GetString(NetDeflate.Unpack(packed, NetProtocol.MaxParticipantJsonBytes));
            SaveLoad load = SaveCodec.Decode(json);
            if (!load.Ok)
            {
                throw new NetFormatException($"An inventory that is not a save ({load.Reason}).");
            }

            return load.Save;
        }
    }
}
```

In `Assets/_BattleBomb/Core/Saves/SaveMapper.cs`:

(a) Replace

```csharp
        public static void RestoreSack(SaveGame save, Sack sack, IReadOnlyList<ItemSpec> catalog)
        {
```

with

```csharp
        public static void RestoreSack(SaveGame save, Sack sack, IReadOnlyList<ItemSpec> catalog) =>
            RestoreSack(save, sack, catalog, -1);

        /// <summary>As above; <paramref name="revision"/> is the host's number for a guest's copy of its sack
        /// (HANDOFF-M8 Task 99), and -1 moves the revision on as any other change to the sack does.</summary>
        public static void RestoreSack(SaveGame save, Sack sack, IReadOnlyList<ItemSpec> catalog, int revision)
        {
```

and at the end of that method replace

```csharp
            sack.Touch();
        }
```

with

```csharp
            if (revision >= 0)
            {
                sack.AdoptRevision(revision);
            }
            else
            {
                sack.Touch();
            }
        }
```

(b) In `RestoreCharacter`, replace

```csharp
            WornSave[] worn = character.Worn;
            for (int i = 0; i < worn.Length; i++)
```

with

```csharp
            ClearLoadout(inventory.Loadout);
            WornSave[] worn = character.Worn;
            for (int i = 0; i < worn.Length; i++)
```

(c) After the method `RestoreCharacter` add:

```csharp

        /// <summary>Everything worn comes off first, so a restore replaces a loadout rather than adding to it:
        /// a guest's copy of a player is restored again every time it changes (HANDOFF-M8 Task 99). At a boot
        /// the loadout is empty, so nothing changes there.</summary>
        private static void ClearLoadout(Loadout loadout)
        {
            for (int i = 0; i < WornSlots.Length; i++)
            {
                loadout.Swap(WornSlots[i], 0, default);
            }

            for (int i = 0; i < Loadout.EquipmentSlots; i++)
            {
                loadout.Swap(ItemSlot.Equipment, i, default);
            }
        }

        /// <summary>
        /// One player as a save of their own (HANDOFF-M8 Task 99): that character and — when
        /// <paramref name="withSack"/> — the sack, wallet and auto flags they play from. What crosses the wire
        /// for a player online, in the save's own format.
        /// </summary>
        public static SaveGame Participant(Sack sack, in Wallet wallet, in CharacterState character, bool withSack)
        {
            var roster = new[] { character };
            return withSack
                ? Capture(sack, wallet, roster, new StoryProgress())
                : Capture(new Sack(), Wallet.Empty, roster, new StoryProgress());
        }
```

In `Assets/_BattleBomb/Core/Net/ReplicatedEvents.cs`:

(a) Replace

```csharp
        DropRemoved = 3,
    }
```

with

```csharp
        DropRemoved = 3,
        ScreenOpened = 4,
        ScreenClosed = 5,
        RackChanged = 6,
    }
```

(b) Before the line `/// <summary>Something that happened once, at a host step. The guest raises it when its picture` add:

```csharp
    /// <summary>A player's chest or shop opening or closing (D42). Menu state, which the guest takes on arrival.</summary>
    public readonly struct ScreenRecord
    {
        public readonly int PlayerId;

        /// <summary>The driver's interaction kind, as its number (the enum is Gameplay's).</summary>
        public readonly int Kind;

        public ScreenRecord(int playerId, int kind)
        {
            PlayerId = playerId;
            Kind = kind;
        }
    }

    /// <summary>A player's shopkeeper rack as it now stands (D43): every piece, in slot order.</summary>
    public readonly struct RackRecord
    {
        public readonly int PlayerId;
        public readonly ItemInstance[] Pieces;

        public RackRecord(int playerId, ItemInstance[] pieces)
        {
            PlayerId = playerId;
            Pieces = pieces ?? System.Array.Empty<ItemInstance>();
        }
    }

```

(c) Replace everything in `ReplicatedEvent` from `public readonly DropRecord Drop;` to the end of the struct with:

```csharp
        public readonly DropRecord Drop;
        public readonly ScreenRecord Screen;
        public readonly RackRecord Rack;

        private ReplicatedEvent(
            ReplicatedEventKind kind, int hostFrame, in HitRecord hit, in DropRecord drop, in ScreenRecord screen, in RackRecord rack)
        {
            Kind = kind;
            HostFrame = hostFrame;
            Hit = hit;
            Drop = drop;
            Screen = screen;
            Rack = rack;
        }

        public static ReplicatedEvent OfHit(in HitRecord hit) =>
            new ReplicatedEvent(ReplicatedEventKind.Hit, 0, hit, default, default, default);

        public static ReplicatedEvent OfDrop(in DropRecord drop) =>
            new ReplicatedEvent(ReplicatedEventKind.DropSpawned, 0, default, drop, default, default);

        /// <summary>A drop that left the host's world — grabbed, swept or cleared by a wipe.</summary>
        public static ReplicatedEvent OfDropRemoved(int netId) =>
            new ReplicatedEvent(
                ReplicatedEventKind.DropRemoved, 0, default, new DropRecord(netId, Vector3.zero, default), default, default);

        /// <summary>A player's screen opening or closing — <paramref name="kind"/> is the driver's interaction kind.</summary>
        public static ReplicatedEvent OfScreen(int playerId, int kind, bool opened) =>
            new ReplicatedEvent(
                opened ? ReplicatedEventKind.ScreenOpened : ReplicatedEventKind.ScreenClosed, 0, default, default,
                new ScreenRecord(playerId, kind), default);

        /// <summary>A player's rack as it now stands.</summary>
        public static ReplicatedEvent OfRack(int playerId, ItemInstance[] pieces) =>
            new ReplicatedEvent(ReplicatedEventKind.RackChanged, 0, default, default, default, new RackRecord(playerId, pieces));

        /// <summary>The same event stamped with the step it happened in.</summary>
        public ReplicatedEvent At(int hostFrame) => new ReplicatedEvent(Kind, hostFrame, Hit, Drop, Screen, Rack);

        /// <summary>Menu state — a screen or a rack — which the guest takes as it arrives rather than when the
        /// picture reaches its step: it is not drawn in the world, and a menu a tenth of a second late is a menu
        /// that feels broken.</summary>
        public bool IsMenuState =>
            Kind == ReplicatedEventKind.ScreenOpened || Kind == ReplicatedEventKind.ScreenClosed
            || Kind == ReplicatedEventKind.RackChanged;
    }
}
```

In `Assets/_BattleBomb/Core/Net/EventCodec.cs`:

(a) In `Read`, replace

```csharp
                    case ReplicatedEventKind.DropRemoved:
                        into.Add(ReplicatedEvent.OfDropRemoved(r.ReadInt()).At(frame));
                        break;
```

with

```csharp
                    case ReplicatedEventKind.DropRemoved:
                        into.Add(ReplicatedEvent.OfDropRemoved(r.ReadInt()).At(frame));
                        break;

                    case ReplicatedEventKind.ScreenOpened:
                    case ReplicatedEventKind.ScreenClosed:
                        into.Add(ReplicatedEvent.OfScreen(r.ReadInt(), r.ReadByte(), kind == ReplicatedEventKind.ScreenOpened).At(frame));
                        break;

                    case ReplicatedEventKind.RackChanged:
                        int player = r.ReadInt();
                        var pieces = new ItemInstance[r.ReadCount(NetProtocol.MaxRack)];
                        for (int p = 0; p < pieces.Length; p++)
                        {
                            pieces[p] = ItemWire.Read(r, catalog);
                        }

                        into.Add(ReplicatedEvent.OfRack(player, pieces).At(frame));
                        break;
```

(b) In `WriteEvent`, replace

```csharp
                case ReplicatedEventKind.DropRemoved:
                    w.WriteInt(e.Drop.NetId);
                    break;
```

with

```csharp
                case ReplicatedEventKind.DropRemoved:
                    w.WriteInt(e.Drop.NetId);
                    break;

                case ReplicatedEventKind.ScreenOpened:
                case ReplicatedEventKind.ScreenClosed:
                    w.WriteInt(e.Screen.PlayerId);
                    w.WriteByte((byte)e.Screen.Kind);
                    break;

                case ReplicatedEventKind.RackChanged:
                    w.WriteInt(e.Rack.PlayerId);
                    w.WriteCount(e.Rack.Pieces.Length, NetProtocol.MaxRack);
                    for (int p = 0; p < e.Rack.Pieces.Length; p++)
                    {
                        ItemWire.Write(w, e.Rack.Pieces[p]);
                    }

                    break;
```

- [ ] **Step 4: Run the Core tests** — `recompile`; `run_tests` EditMode `ParticipantCodecTests` (seven pass), `EventCodecTests` (all pass). `A_full_sack_fits_one_message_with_room_to_spare` prints nothing on success — put the measured `writer.Length` in the `DONE` for the bandwidth record.

- [ ] **Step 5: Which players are local**

`Assets/_BattleBomb/Tests/EditMode/PlayerRegistryLocalTests.cs`:

```csharp
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>"Local players on this display", never "characters in the world" (HANDOFF-M8 planning decision
    /// 15): a device, a script or a replay is local; a player whose hands are on another machine is not.</summary>
    public sealed class PlayerRegistryLocalTests
    {
        private sealed class Pad : IPlayerCommandSource
        {
            public Pad(int id) => PlayerId = new PlayerId(id);

            public PlayerId PlayerId { get; }

            public PlayerCommand Sample(int frame) => PlayerCommand.Idle(frame);
        }

        private sealed class Wire : IPlayerCommandSource, IRemotePlayerSource
        {
            public Wire(int id) => PlayerId = new PlayerId(id);

            public PlayerId PlayerId { get; }

            public PlayerCommand Sample(int frame) => PlayerCommand.Idle(frame);
        }

        [Test]
        public void A_remote_player_is_in_the_world_but_not_on_this_display()
        {
            var registry = new PlayerRegistry();
            registry.Register(new Pad(0));
            registry.Register(new Wire(1));

            Assert.That(registry.Count, Is.EqualTo(2));
            Assert.That(registry.LocalCount, Is.EqualTo(1));
            Assert.That(registry.IsLocal(new PlayerId(0)), Is.True);
            Assert.That(registry.IsLocal(new PlayerId(1)), Is.False);
            Assert.That(registry.IsLocal(new PlayerId(5)), Is.False, "Nobody is not a local player.");
        }

        [Test]
        public void The_couch_is_two_local_players()
        {
            var registry = new PlayerRegistry();
            registry.Register(new Pad(0));
            registry.Register(new Pad(1));

            Assert.That(registry.LocalCount, Is.EqualTo(2));
        }
    }
}
```

`Assets/_BattleBomb/Gameplay/Players/IRemotePlayerSource.cs`:

```csharp
namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// Marks a command source whose player's hands are on another machine (HANDOFF-M8 planning decision 15).
    /// The registry counts every other source as local — a device, a test script, a replay — because each
    /// of those has its screens on this display.
    /// </summary>
    public interface IRemotePlayerSource
    {
    }
}
```

In `Assets/_BattleBomb/Gameplay/Players/PlayerRegistry.cs`, after the method `IsRegistered` add:

```csharp

        /// <summary>This player's hands are on this machine — a device, a script, a replay — so their menus open
        /// on this display (HANDOFF-M8 planning decision 15). False for a remote player, and for nobody.</summary>
        public bool IsLocal(PlayerId playerId) =>
            _sources.TryGetValue(playerId.Value, out IPlayerCommandSource source) && !(source is IRemotePlayerSource);

        /// <summary>How many players this display is for: two on the couch; one alone, or online.</summary>
        public int LocalCount
        {
            get
            {
                int count = 0;
                foreach (IPlayerCommandSource source in _sources.Values)
                {
                    if (!(source is IRemotePlayerSource))
                    {
                        count++;
                    }
                }

                return count;
            }
        }
```

In `Assets/_BattleBomb/Gameplay/Net/RemoteCommandSource.cs`, replace

```csharp
    public sealed class RemoteCommandSource : MonoBehaviour, IPlayerCommandSource
```

with

```csharp
    public sealed class RemoteCommandSource : MonoBehaviour, IPlayerCommandSource, IRemotePlayerSource
```

- [ ] **Step 6: The guest's copy of a player, and replica screens**

In `Assets/_BattleBomb/Gameplay/Items/PlayerInventory.cs`:

(a) Add `using System.Collections.Generic;` and `using BattleBomb.Core.Saves;` to the usings.

(b) After the method `SetLedger` add:

```csharp

        /// <summary>
        /// The guest's copy of this player (D61, HANDOFF-M8 Task 99): what the host holds for them, laid over
        /// this machine's copy in one go — worn gear, quick slot and ledger always; the sack, wallet and auto
        /// flags too when <paramref name="full"/> (the guest's own player). The sack takes the host's revision,
        /// so a request sent from it names the sack the host holds. One arrival, one <see cref="Changed"/>.
        /// </summary>
        internal void ApplyMirror(SaveGame state, int revision, bool full, IReadOnlyList<ItemSpec> catalog)
        {
            CharacterSave character = state != null && state.Characters.Length > 0 ? state.Characters[0] : null;
            if (character == null)
            {
                return;
            }

            _ledger = SaveMapper.RestoreCharacter(character, Inventory, catalog);
            if (full)
            {
                SaveMapper.RestoreSack(state, Stash.Sack, catalog, revision);

                // Raises the stash's Changed, which this bag passes on as its own.
                Stash.Restore(Stash.Sack, SaveMapper.RestoreWallet(state));
                return;
            }

            Changed?.Invoke();
        }
```

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF):

(a) Delete `MayOpenScreen` — the property and its whole `///` summary (the block beginning `/// Plan 1 only (HANDOFF-M8 Task 99 retires it)`).

(b) In `StepPlayers`, replace

```csharp
                if (result.OpenedInteractable && interactable >= 0 && interactable < _interactables.Count
                    && (MayOpenScreen == null || MayOpenScreen(playerId)))
```

with

```csharp
                if (result.OpenedInteractable && interactable >= 0 && interactable < _interactables.Count)
```

(c) Replace the method `ApplyReplicaPlayerSide` in full with:

```csharp
        /// <summary>The snapshot's per-player numbers. Screens are not among them: they follow the host's
        /// events (<see cref="ApplyReplicaScreen"/>), which are told, never guessed from a snapshot.</summary>
        internal void ApplyReplicaPlayerSide(int playerIdValue, int grabCount, int refusedSteps)
        {
            _grabCounts[playerIdValue] = grabCount;
            _refusedGrabs[playerIdValue] = refusedSteps;
        }
```

(d) After the method `ApplyReplicaWorld` add:

```csharp

        /// <summary>
        /// A screen the host opened or closed (D42, HANDOFF-M8 Task 99), taken as it arrives. The guest's own
        /// chest then opens on the guest's display — the screens listen to <see cref="ScreenChanged"/> — and
        /// what it was opened on is the nearest thing of that kind to the player, which is what the host opened.
        /// </summary>
        internal void ApplyReplicaScreen(int playerIdValue, InteractionKind kind, bool opened)
        {
            if (!opened)
            {
                CloseScreen(playerIdValue);
                return;
            }

            if (_openScreens.TryGetValue(playerIdValue, out InteractionKind current) && current == kind)
            {
                return;
            }

            _openScreens[playerIdValue] = kind;
            WorldInteractable source = NearestInteractable(playerIdValue, kind);
            if (source != null)
            {
                _openSources[playerIdValue] = source;
            }
            else
            {
                _openSources.Remove(playerIdValue);
            }

            ScreenChanged?.Invoke(playerIdValue, kind, true);
        }

        /// <summary>The host's rack for a player, as it now stands (D43). A guest rolls nothing.</summary>
        internal void ApplyReplicaRack(int playerIdValue, ItemInstance[] pieces)
        {
            if (!_racks.TryGetValue(playerIdValue, out List<ItemInstance> rack))
            {
                rack = new List<ItemInstance>(RackSize);
                _racks[playerIdValue] = rack;
            }

            rack.Clear();
            if (pieces != null)
            {
                rack.AddRange(pieces);
            }

            RackChanged?.Invoke(playerIdValue);
        }

        private WorldInteractable NearestInteractable(int playerIdValue, InteractionKind kind)
        {
            CharacterActor player = null;
            IReadOnlyList<CharacterActor> actors = Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].PlayerId.Value == playerIdValue)
                {
                    player = actors[i];
                }
            }

            if (player == null)
            {
                return null;
            }

            WorldInteractable best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < _interactables.Count; i++)
            {
                WorldInteractable candidate = _interactables[i];
                if (candidate == null || candidate.Kind != kind)
                {
                    continue;
                }

                Vector3 to = candidate.Position - player.Position;
                to.y = 0f;
                if (to.sqrMagnitude < bestSq)
                {
                    best = candidate;
                    bestSq = to.sqrMagnitude;
                }
            }

            return best;
        }
```

In `Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`, replace

```csharp
                _driver.ApplyReplicaPlayerSide(player.PlayerId, player.OpenScreen, player.GrabCount, player.RefusedSteps);
```

with

```csharp
                // The snapshot still carries each player's open screen; the guest follows the host's screen events.
                _driver.ApplyReplicaPlayerSide(player.PlayerId, player.GrabCount, player.RefusedSteps);
```

- [ ] **Step 7: The host tells; the guest takes**

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`:

(a) After the field `private readonly NetWriter _answer = new NetWriter(64);` add:

```csharp
        private readonly NetWriter _participant = new NetWriter(16 * 1024);
        private readonly List<(int Sequence, RequestOutcome Outcome)> _answers = new List<(int Sequence, RequestOutcome Outcome)>();
        private readonly Dictionary<int, Watched> _watched = new Dictionary<int, Watched>();
        private readonly List<int> _unwatch = new List<int>();

        /// <summary>A player whose inventory the guest keeps a copy of (Task 99).</summary>
        private sealed class Watched
        {
            internal CharacterActor Actor;
            internal PlayerInventory Bag;
            internal Action Handler;
            internal bool Dirty;
            internal bool Forced;
            internal int SentAt;
        }
```

(b) In `Begin`, delete the line `_driver.MayOpenScreen = id => id != _net.GuestPlayerId.Value;`, and after `_driver.RemoteRequestAnswered += OnRequestAnswered;` add:

```csharp
            _driver.ScreenChanged += OnScreenChanged;
            _driver.RackChanged += OnRackChanged;
```

(c) Replace the method `OnRequestAnswered` (Task 97's) with:

```csharp
        /// <summary>The answer waits for the end of the step: the guest must have the bag the request changed —
        /// and the rack it bought from — before it hears the answer (all three on the one ordered channel).</summary>
        private void OnRequestAnswered(PlayerRequest request, RequestOutcome outcome)
        {
            if (!_net.IsConnected || request.PlayerId != _net.GuestPlayerId.Value)
            {
                return;
            }

            _answers.Add((request.Sequence, outcome));
            Force(request.PlayerId);
        }

        private void OnScreenChanged(int playerId, InteractionKind kind, bool opened)
        {
            _pending.Add(ReplicatedEvent.OfScreen(playerId, (int)kind, opened));
            if (opened && playerId == _net.GuestPlayerId.Value)
            {
                // The guest's chest opens over the bag as it is now, not as it was a quarter-second ago.
                Force(playerId);
            }
        }

        private void OnRackChanged(int playerId)
        {
            IReadOnlyList<Core.Items.ItemInstance> rack = _driver.RackFor(playerId);
            var pieces = new Core.Items.ItemInstance[rack.Count];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = rack[i];
            }

            _pending.Add(ReplicatedEvent.OfRack(playerId, pieces));
        }

        private void Force(int playerId)
        {
            if (_watched.TryGetValue(playerId, out Watched watched))
            {
                watched.Forced = true;
            }
        }

        /// <summary>Every player in the world gets a watcher the first step they are there — the binder brings the
        /// host up before any player object has enabled — and loses it when they leave.</summary>
        private void WatchParticipants()
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                int id = actors[i].PlayerId.Value;
                if (_watched.ContainsKey(id))
                {
                    continue;
                }

                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag == null)
                {
                    continue;
                }

                var watched = new Watched { Actor = actors[i], Bag = bag, Dirty = true, Forced = true };
                watched.Handler = () => watched.Dirty = true;
                bag.Changed += watched.Handler;
                _watched[id] = watched;
            }

            _unwatch.Clear();
            foreach (KeyValuePair<int, Watched> entry in _watched)
            {
                if (entry.Value.Actor == null || !entry.Value.Actor.isActiveAndEnabled)
                {
                    _unwatch.Add(entry.Key);
                }
            }

            for (int i = 0; i < _unwatch.Count; i++)
            {
                Unwatch(_unwatch[i]);
            }
        }

        private void Unwatch(int playerId)
        {
            if (!_watched.TryGetValue(playerId, out Watched watched))
            {
                return;
            }

            if (watched.Bag != null)
            {
                watched.Bag.Changed -= watched.Handler;
            }

            _watched.Remove(playerId);
        }

        /// <summary>The guest's whole bag; the partner's worn gear only. Sent when forced, or when changed and not
        /// sent for <see cref="NetProtocol.ParticipantMinSteps"/>.</summary>
        private void FlushParticipants(int frame)
        {
            foreach (KeyValuePair<int, Watched> entry in _watched)
            {
                Watched watched = entry.Value;
                bool due = watched.Forced || (watched.Dirty && frame - watched.SentAt >= NetProtocol.ParticipantMinSteps);
                if (!due)
                {
                    continue;
                }

                SendParticipant(entry.Key, watched);
                watched.Dirty = false;
                watched.Forced = false;
                watched.SentAt = frame;
            }
        }

        private void SendParticipant(int playerId, Watched watched)
        {
            bool full = playerId == _net.GuestPlayerId.Value;
            PlayerInventory bag = watched.Bag;
            var character = new CharacterState(watched.Actor.Element, bag.Ledger, bag.Inventory);
            SaveGame state = SaveMapper.Participant(bag.Stash.Sack, bag.Wallet, character, full);
            _participant.Reset();
            ParticipantCodec.Write(_participant, playerId, bag.Inventory.Sack.Revision, full, state);
            _net.Send(NetChannel.Reliable, _participant);
        }

        private void SendAnswers()
        {
            for (int i = 0; i < _answers.Count; i++)
            {
                _answer.Reset();
                RequestCodec.WriteResult(_answer, _answers[i].Sequence, _answers[i].Outcome);
                _net.Send(NetChannel.Reliable, _answer);
            }

            _answers.Clear();
        }
```

(d) In `OnStepped`, replace

```csharp
            if (!_net.IsConnected)
            {
                _pending.Clear();
                return;
            }

            if (_pending.Count > 0)
```

with

```csharp
            if (!_net.IsConnected)
            {
                _pending.Clear();
                _answers.Clear();
                return;
            }

            // Bag first, then what happened, then the answers that read them: one ordered channel (Task 99).
            WatchParticipants();
            FlushParticipants(frame);

            if (_pending.Count > 0)
```

and replace

```csharp
                EventCodec.WriteBatches(_stamped, _writer, _scratch, _sendReliable);
            }

            if (frame % NetProtocol.SnapshotEverySteps != 0)
```

with

```csharp
                EventCodec.WriteBatches(_stamped, _writer, _scratch, _sendReliable);
            }

            SendAnswers();

            if (frame % NetProtocol.SnapshotEverySteps != 0)
```

(e) In `OnDestroy`, delete the line `_driver.MayOpenScreen = null;`, and after `_driver.RemoteRequestAnswered -= OnRequestAnswered;` add:

```csharp
                _driver.ScreenChanged -= OnScreenChanged;
                _driver.RackChanged -= OnRackChanged;
```

and before `if (_net == null)` add:

```csharp
            foreach (int id in new List<int>(_watched.Keys))
            {
                Unwatch(id);
            }

```

Add `using BattleBomb.Core.Saves;` and `using BattleBomb.Gameplay.Items;` to `NetHost.cs` (`BattleBomb.Gameplay.World` is already there for `InteractionKind`).

In `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

(a) In `OnMessage`, replace

```csharp
                case NetMessageKind.Events:
                    EventCodec.Read(reader, _driver.ItemSpecs, _incoming);
                    _pending.AddRange(_incoming);
                    break;
```

with

```csharp
                case NetMessageKind.Events:
                    EventCodec.Read(reader, _driver.ItemSpecs, _incoming);
                    for (int i = 0; i < _incoming.Count; i++)
                    {
                        if (_incoming[i].IsMenuState)
                        {
                            ApplyMenuState(_incoming[i]);
                        }
                        else
                        {
                            _pending.Add(_incoming[i]);
                        }
                    }

                    break;

                case NetMessageKind.Participant:
                    ParticipantMessage participant = ParticipantCodec.Read(reader);
                    PlayerInventory bag = _driver.InventoryOf(participant.PlayerId);
                    if (bag != null)
                    {
                        bag.ApplyMirror(participant.State, participant.Revision, participant.Full, _driver.ItemSpecs);
                    }

                    break;
```

(b) After the method `OnStageReady` add:

```csharp

        /// <summary>A screen or a rack, taken as it arrives (Task 99): menu state, not picture.</summary>
        private void ApplyMenuState(in ReplicatedEvent e)
        {
            switch (e.Kind)
            {
                case ReplicatedEventKind.ScreenOpened:
                    _driver.ApplyReplicaScreen(e.Screen.PlayerId, (InteractionKind)e.Screen.Kind, true);
                    break;

                case ReplicatedEventKind.ScreenClosed:
                    _driver.ApplyReplicaScreen(e.Screen.PlayerId, (InteractionKind)e.Screen.Kind, false);
                    break;

                case ReplicatedEventKind.RackChanged:
                    _driver.ApplyReplicaRack(e.Rack.PlayerId, e.Rack.Pieces);
                    break;
            }
        }
```

Add `using BattleBomb.Gameplay.Items;` to `NetGuest.cs` (`BattleBomb.Gameplay.World` is already there).

- [ ] **Step 8: Screens open only for this display's players**

In `Assets/_BattleBomb/UI/Chest/ChestScreenHost.cs`, in `Open`, replace

```csharp
            if (bag == null)
            {
                return;
            }

            // One player on this display — solo, or online co-op where the partner has their own
            // screen — means the sack takes the left half and the camera pushes the hero into the
            // right. Two players sharing a display means the camera belongs to both, so the screen
            // takes that player's half and tabs instead. CameraRig reads the same condition off
            // the driver rather than being told: UI does not reach into Presentation.
            bool split = actors.Count > 1;
```

with

```csharp
            // A remote player's screen is on their own machine (HANDOFF-M8 planning decision 15): the host holds
            // it open — that is what keeps their body standing idle — and draws nothing here.
            if (bag == null || !_driver.Players.IsLocal(new PlayerId(playerId)))
            {
                return;
            }

            // One player on this display — solo, or online co-op where the partner has their own
            // screen — means the sack takes the left half and the camera pushes the hero into the
            // right. Two players sharing a display means the camera belongs to both, so the screen
            // takes that player's half and tabs instead. CameraRig reads the same condition off
            // the driver rather than being told: UI does not reach into Presentation.
            bool split = _driver.Players.LocalCount > 1;
```

and in the same method replace `LayOut((RectTransform)go.transform, playerId, split, actors.Count);` with `LayOut((RectTransform)go.transform, playerId, split, _driver.Players.LocalCount);`.

In `Assets/_BattleBomb/UI/Chest/ChestScreen.cs`:

(a) After the field `private IPlayerRequests _requests;` add:

```csharp

        /// <summary>The sack's revision when the pending combine's pick was made — the pick is a bag index,
        /// and only a change to the sack can move it (Task 99).</summary>
        private int _combineRevision;
```

(b) Replace the method `OnBagChanged` (its `///` summary included) with:

```csharp
        /// <summary>
        /// The bag moved — possibly under the partner's hand (D51), or with the host's copy of it arriving on a
        /// guest. Repaint from the new truth, and drop a combine in progress only if the sack itself moved: the
        /// pick is a bag index, and selling or equipping anything above it slides every index below down one.
        /// XP from a kill changes the bag's owner, not the sack, and used to cancel the pick for nothing — online
        /// the host's every kill would have done it to the guest.
        /// </summary>
        internal void OnBagChanged()
        {
            if (_nav.PendingCombine >= 0 && _bag.Inventory.Sack.Revision != _combineRevision)
            {
                _nav.CancelCombine();
                Flash("Combine cancelled — the sack moved.");
                return;
            }

            Refresh();
        }
```

(c) In `RunCombine`, replace

```csharp
                _nav.BeginCombine(bagIndex);
```

with

```csharp
                _nav.BeginCombine(bagIndex);
                _combineRevision = _bag.Inventory.Sack.Revision;
```

(d) Replace Task 98's `internal void OnRackChanged() => Refresh();` (its summary included) with:

```csharp
        /// <summary>The rack moved — a purchase, or on a guest the host's rack arriving after the answer. The
        /// cursor may have been on the slot that emptied.</summary>
        internal void OnRackChanged()
        {
            CollectVisible();
            _nav.FinishBuy(_stock.Count);
            Refresh();
        }
```

- [ ] **Step 9: Run EditMode** — `recompile`, `console` `level: error` clean; `run_tests` EditMode `PlayerRegistryLocalTests` (two pass), then the full suite (807 + 7 + 1 + 2 = 817). Nothing online changes the couch here except the combine rule, and `LootLoopSmokeTests`' combine cases still pass in Step 12.

- [ ] **Step 10: The host side, tested**

In `Assets/_BattleBomb/Tests/PlayMode/OnlineMenuSmokeTests.cs`, before `private WorldInteractable FirstChest()` add:

```csharp
        [UnityTest]
        public IEnumerator The_guests_press_at_the_chest_opens_it_and_the_guest_is_told()
        {
            int before = _guest.Received.Count;
            _guest.Held = CommandButtons.Light;
            yield return Steps(3);
            _guest.Held = CommandButtons.None;
            yield return Until(() => _driver.TryGetOpenScreen(PlayerId.Two.Value, out InteractionKind kind) && kind == InteractionKind.Chest,
                "Player 2's Light beside the chest never opened it on the host");
            yield return Steps(4);

            int opened = IndexOf(before, m => IsEvent(m, ReplicatedEventKind.ScreenOpened, PlayerId.Two.Value));
            int bag = IndexOf(before, m => IsParticipant(m, PlayerId.Two.Value, full: true));
            Assert.That(opened, Is.GreaterThanOrEqualTo(0), "The guest was never told its chest opened.");
            Assert.That(bag, Is.GreaterThanOrEqualTo(0), "The guest's bag never reached it.");
            Assert.That(bag, Is.LessThan(opened), "The chest opened on the guest before its bag arrived.");
        }

        [UnityTest]
        public IEnumerator No_screen_is_drawn_on_the_host_for_a_remote_players_chest()
        {
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            yield return Steps(3);

            Assert.That(_driver.TryGetOpenScreen(PlayerId.Two.Value, out _), Is.True);
            Assert.That(GameObject.Find("Chest Screen P2"), Is.Null,
                "The host drew the guest's chest on the host's display.");
        }

        [UnityTest]
        public IEnumerator A_guests_bag_reaches_it_before_the_answer_that_reads_it()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            yield return Steps(4);
            int items = bag.Inventory.Items.Count;
            int before = _guest.Received.Count;

            _guest.SendRequest(PlayerRequest.Sell(items - 1).WithSequence(9).WithRevision(bag.Inventory.Sack.Revision));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            int answer = IndexOf(before, m => (NetMessageKind)m[0] == NetMessageKind.RequestResult);
            Assert.That(answer, Is.GreaterThanOrEqualTo(0));
            int copy = LastIndexOf(before, answer, m => IsParticipant(m, PlayerId.Two.Value, full: true));
            Assert.That(copy, Is.GreaterThanOrEqualTo(0), "The answer arrived before the bag it describes.");

            var reader = new NetReader(_guest.Received[copy]);
            reader.ReadByte();
            ParticipantMessage sent = ParticipantCodec.Read(reader);
            Assert.That(sent.State.Sack.Length, Is.EqualTo(items - 1), "The copy the guest got still has the sold item.");
            Assert.That(sent.Revision, Is.EqualTo(bag.Inventory.Sack.Revision));
        }

        [UnityTest]
        public IEnumerator The_guest_is_told_what_its_partner_wears()
        {
            PlayerInventory hostBag = _host.GetComponent<PlayerInventory>();
            hostBag.Earn(5000f);
            hostBag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            Assert.That(hostBag.RequestEquip(hostBag.Inventory.Items.Count - 1), Is.True, "The host could not wear the knife.");
            int before = _guest.Received.Count;
            yield return Steps(NetProtocol.ParticipantMinSteps + 4);

            int copy = LastIndexOf(before, _guest.Received.Count, m => IsParticipant(m, 0, full: false));
            Assert.That(copy, Is.GreaterThanOrEqualTo(0), "The partner's new gear never reached the guest.");
            var reader = new NetReader(_guest.Received[copy]);
            reader.ReadByte();
            ParticipantMessage sent = ParticipantCodec.Read(reader);
            Assert.That(sent.State.Sack, Is.Empty, "The host's whole sack crossed the wire; only its gear should.");
            Assert.That(sent.State.Characters[0].Worn.Length, Is.GreaterThan(0));
        }

        private int IndexOf(int from, System.Func<byte[], bool> match)
        {
            for (int i = from; i < _guest.Received.Count; i++)
            {
                if (match(_guest.Received[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The last match in [<paramref name="from"/>, <paramref name="end"/>), or -1.</summary>
        private int LastIndexOf(int from, int end, System.Func<byte[], bool> match)
        {
            for (int i = end - 1; i >= from; i--)
            {
                if (match(_guest.Received[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsParticipant(byte[] message, int playerId, bool full)
        {
            var reader = new NetReader(message);
            if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Participant)
            {
                return false;
            }

            return reader.ReadInt() == playerId && ReadFull(reader) == full;
        }

        private static bool ReadFull(NetReader reader)
        {
            reader.ReadInt();
            return reader.ReadBool();
        }

        private static bool IsEvent(byte[] message, ReplicatedEventKind kind, int playerId)
        {
            var reader = new NetReader(message);
            if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Events)
            {
                return false;
            }

            var batch = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, batch);
            foreach (ReplicatedEvent e in batch)
            {
                if (e.Kind == kind && e.Screen.PlayerId == playerId)
                {
                    return true;
                }
            }

            return false;
        }
```

- [ ] **Step 11: The guest side, tested**

`Assets/_BattleBomb/Tests/PlayMode/GuestMenuSmokeTests.cs` — a real guest machine fed a hand-built host, one recording per test:

```csharp
using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// The guest's menus (HANDOFF-M8 Task 99), on a real guest machine fed a hand-built host: the guest's
    /// chest and shop open on its own display over the copy of its bag the host sent, an action becomes a
    /// request that waits for the host, and closing is at once. The host itself is Task 97's, tested from
    /// the other side in <see cref="OnlineMenuSmokeTests"/>.
    /// </summary>
    public sealed class GuestMenuSmokeTests
    {
        private const int Start = 1000;
        private const int Length = 400;
        private const int LoadMargin = 120;
        private const int OpenAt = Start + 20;
        private const int GuestRevision = 41;

        private PlaybackTransport _playback;
        private NetGuest _guest;
        private SimulationDriver _driver;

        [UnitySetUp]
        public IEnumerator OpenTheFrontDoor()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "guest-menu";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator The_guests_chest_opens_on_its_own_display_over_its_own_bag()
        {
            yield return Join(Recording(ChestOpens()));
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The host said the guest's chest opened; it never did.");
            yield return AdvanceSteps(2);

            Assert.That(GameObject.Find("Chest Screen P2"), Is.Not.Null, "The guest's own chest is not on its display.");
            Assert.That(GameObject.Find("Chest Screen P1"), Is.Null);
            PlayerInventory bag = _driver.InventoryOf(1);
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(2), "The guest's copy of its bag is not the host's.");
            Assert.That(bag.Wallet.Balance, Is.EqualTo(100));
            Assert.That(bag.Inventory.Sack.Revision, Is.EqualTo(GuestRevision), "The copy did not take the host's revision.");
            Assert.That(_driver.InventoryOf(0).Inventory.Loadout.Weapon.IsEmpty, Is.False,
                "The guest's copy of its partner is not wearing the partner's knife.");
        }

        [UnityTest]
        public IEnumerator A_sale_on_the_guest_is_sent_to_the_host_and_waits_for_its_answer()
        {
            yield return Join(Recording(ChestOpens()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceSteps(3);

            hands.Set(Vector2.zero, CommandButtons.Light | CommandButtons.Option);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceSteps(2);

            PlayerRequest sent = default;
            bool found = false;
            foreach (byte[] message in _playback.Sent)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Request)
                {
                    sent = RequestCodec.ReadRequest(reader);
                    found = true;
                }
            }

            Assert.That(found, Is.True, "X on the guest's chest sent the host nothing.");
            Assert.That(sent.Kind, Is.EqualTo(PlayerRequestKind.Sell));
            Assert.That(sent.Revision, Is.EqualTo(GuestRevision), "The request does not name the sack the host holds.");
            Assert.That(_driver.RequestsFor(1).Pending, Is.True, "The screen is not waiting for the host's answer.");
            Assert.That(_driver.InventoryOf(1).Inventory.Items.Count, Is.EqualTo(2),
                "The guest changed its own bag; only the host's answer may.");
        }

        [UnityTest]
        public IEnumerator Closing_on_the_guest_is_at_once_and_the_host_is_told()
        {
            yield return Join(Recording(ChestOpens()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceSteps(3);

            hands.Set(Vector2.zero, CommandButtons.Back);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceSteps(1);

            Assert.That(_driver.TryGetOpenScreen(1, out _), Is.False, "Back did not close the guest's chest at once.");
            bool told = false;
            foreach (byte[] message in _playback.Sent)
            {
                var reader = new NetReader(message);
                told |= (NetMessageKind)reader.ReadByte() == NetMessageKind.Request
                    && RequestCodec.ReadRequest(reader).Kind == PlayerRequestKind.CloseScreen;
            }

            Assert.That(told, Is.True, "The host was never told the guest closed its chest; the body would stand idle.");
        }

        [UnityTest]
        public IEnumerator The_guests_shop_draws_the_hosts_rack()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            extra.Add((OpenAt, Events(writer,
                ReplicatedEvent.OfRack(1, new[] { Knife(), Knife() }).At(OpenAt),
                ReplicatedEvent.OfScreen(1, (int)InteractionKind.Shopkeeper, true).At(OpenAt))));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out InteractionKind kind) && kind == InteractionKind.Shopkeeper,
                "The guest's shop never opened.");

            Assert.That(_driver.RackFor(1).Count, Is.EqualTo(2), "The guest's shop does not show the host's rack.");
            Assert.That(GameObject.Find("Chest Screen P2"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator A_screen_the_host_closes_closes_on_the_guest()
        {
            var extra = ChestOpens();
            var writer = new NetWriter();
            extra.Add((OpenAt + 60, Events(writer, ReplicatedEvent.OfScreen(1, (int)InteractionKind.Chest, false).At(OpenAt + 60))));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceUntil(() => !_driver.TryGetOpenScreen(1, out _), "The host closed the guest's chest; the guest's stayed open.");

            Assert.That(GameObject.Find("Chest Screen P2"), Is.Null);
        }

        private IEnumerator Join(List<(int Frame, byte[] Payload)> recording)
        {
            _playback = new PlaybackTransport(recording);
            NetSession.FindOrCreate().Join(_playback, "playback");
            for (int i = 0; i < 1500 && _guest == null; i++)
            {
                yield return null;
                _guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(_guest, Is.Not.Null, "The guest never loaded the host's run.");
            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            yield return AdvanceUntil(() => _guest.RenderFrame >= Start, "The guest never started drawing.");
        }

        /// <summary>The guest's own pad out of the loop, a script in its place, speaking as Player 2.</summary>
        private ScriptedCommandSource TakeTheGuestsHands()
        {
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            CharacterActor own = null;
            foreach (CharacterActor actor in _driver.Characters.Ordered)
            {
                if (actor.PlayerId.Value == 1)
                {
                    own = actor;
                }
            }

            Assert.That(own, Is.Not.Null);
            _driver.Players.Unregister(own.PlayerId);
            var hands = own.gameObject.AddComponent<ScriptedCommandSource>();
            hands.Bind(1);
            _driver.Players.Register(hands);
            return hands;
        }

        private IEnumerator AdvanceSteps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < 6000 && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private static IEnumerator AdvanceUntil(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < 6000 && !condition(); guard++)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, failure);
        }

        private static ItemInstance Knife() => new ItemInstance(
            new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
            new GearContribution(weaponDamage: 9f), new AffixRoll[0], requiredLevel: 1, new ItemInvestment(3));

        /// <summary>Both players' inventories, then the guest's chest opening.</summary>
        private static List<(int, byte[])> ChestOpens()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();

            var guestBag = new Inventory();
            guestBag.Add(Knife(), 99);
            guestBag.Add(Knife(), 99);
            SaveGame guest = SaveMapper.Participant(
                guestBag.Sack, new Wallet(100), new CharacterState(ElementId.None, XpLedger.Fresh, guestBag), withSack: true);
            ParticipantCodec.Write(writer, 1, GuestRevision, true, guest);
            extra.Add((Start + 10, writer.ToArray()));

            var hostBag = new Inventory();
            hostBag.Add(Knife(), 99);
            hostBag.TryEquip(0, 99);
            SaveGame host = SaveMapper.Participant(
                hostBag.Sack, Wallet.Empty, new CharacterState(ElementId.None, XpLedger.Fresh, hostBag), withSack: false);
            writer.Reset();
            ParticipantCodec.Write(writer, 0, 3, false, host);
            extra.Add((Start + 10, writer.ToArray()));

            extra.Add((OpenAt, Events(writer, ReplicatedEvent.OfScreen(1, (int)InteractionKind.Chest, true).At(OpenAt))));
            return extra;
        }

        /// <summary>A launch, both players standing still in a snapshot every second step, and the extras — merged
        /// in step order, as a host would have sent them.</summary>
        private static List<(int Frame, byte[] Payload)> Recording(List<(int, byte[])> extra)
        {
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1));
            recording.Add((Start - LoadMargin, writer.ToArray()));

            for (int frame = Start; frame <= Start + Length; frame += NetProtocol.SnapshotEverySteps)
            {
                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                world.Players.Add(Standing(1, 2f));
                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            foreach ((int frame, byte[] payload) in extra)
            {
                recording.Add((frame, payload));
            }

            // Stable: a snapshot and an extra on one step keep the order they were added in.
            var ordered = new List<(int Frame, byte[] Payload)>();
            for (int i = 0; i < recording.Count; i++)
            {
                int at = ordered.Count;
                while (at > 0 && ordered[at - 1].Frame > recording[i].Frame)
                {
                    at--;
                }

                ordered.Insert(at, recording[i]);
            }

            return ordered;
        }

        private static PlayerSnapshot Standing(int playerId, float x) => new PlayerSnapshot(
            playerId, MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready,
            new PlayerCondition(Health.FromValues(100f, 100f), 0, 0), ReviveChannel.Inactive,
            ManaPool.FromValues(50f, 50f), true, null, -1, 0, 0);

        private static byte[] Events(NetWriter writer, params ReplicatedEvent[] events)
        {
            writer.Reset();
            EventCodec.Write(writer, events);
            return writer.ToArray();
        }
    }
}
```

The recordings carry no `LoadStage`, so the guest has no stage and no chest prop: the screen opens without a source, which is exactly the case `ApplyReplicaScreen` allows. The chest screen needs nothing in reach on a guest — only the host's copy of it does.

- [ ] **Step 12: Run both suites** — `recompile`; PlayMode (async) `OnlineMenuSmokeTests` (eight pass) and `GuestMenuSmokeTests` (five pass); then full EditMode (817) and full PlayMode (71 + 4 + 5 = 80). `ReplicaReplaySmokeTests`' vetted `Stepped` list must pass unchanged — Task 99 adds no `Stepped` subscriber on a guest. `LootLoopSmokeTests`' combine cases prove the new combine rule on the couch. Delete `Assets/InitTestScene*`.

- [ ] **Step 13: Commit** — subject `99: the guest's screens and the guest's bag`. Body: screens open on the display of the player whose hands they are (the registry now knows local from remote); the host tells the guest when a screen opens or closes and what is on its rack — reliable events, taken on arrival; the host keeps the guest's copy of its bag current in the save's own format, deflated, sending the bag before the answer that reads it; the partner's gear travels so the guest's copy of them swings the right weapon; `MayOpenScreen` retired; a combine pick is cancelled only when the sack moved; protocol 4. Put the measured size of a full sack on the wire in the body.

---

### Task 100: Online screen rules

D60: nothing pauses online. A menu that would have stopped the world — the settings, the results — instead stands **this display's** player idle, as a chest does, while the partner on the other machine plays on. The count of open global menus stays exactly as it is (`MenuPauseHeld`), because the guest's `MenuGate` and the settings menu's "another menu has the screen" check both read it — only the world's pause stops listening to it while a guest is in the world. The camera frames a chest for the one player on this display. On a guest, the settings menu's session row leaves the game instead of the run; the auto flags travel as requests; the results screen opens when the host's chapter ends and only the host can end it. The host's results listen only to the host's hands (a remote Player 2's last buttons used to hold it shut). And the G8 catch-up item: a menu opened inside a step stops the world before the frame's next step.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/SessionCodec.cs`
- Modify: `Assets/_BattleBomb/Core/Net/NetMessageKind.cs`, `Core/Net/NetProtocol.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF), `Gameplay/Items/PlayerRequestRunner.cs`, `Gameplay/World/StageRunner.cs` (CRLF), `Gameplay/Net/NetHost.cs`, `Gameplay/Net/NetGuest.cs`
- Modify: `Assets/_BattleBomb/Presentation/Cameras/CameraRig.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/SettingsMenu.cs`, `UI/Frontend/ResultsScreen.cs`
- Modify: `Assets/_BattleBomb/Tests/PlayMode/BattleBomb.Tests.PlayMode.asmdef` (reference `BattleBomb.Presentation`)
- Test: create `Tests/EditMode/Net/SessionCodecTests.cs`; modify `Tests/EditMode/Acceptance/SettingsMenuReleaseAcceptanceTests.cs`, `Tests/PlayMode/LootLoopSmokeTests.cs`, `Tests/PlayMode/OnlineMenuSmokeTests.cs`, `Tests/PlayMode/GuestMenuSmokeTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_BattleBomb/Tests/EditMode/Net/SessionCodecTests.cs`:

```csharp
using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>A moment in the host's run that the guest acts on too (D60/D61).</summary>
    public sealed class SessionCodecTests
    {
        [Test]
        public void Every_moment_round_trips()
        {
            foreach (MomentKind moment in new[] { MomentKind.ChapterCompleted, MomentKind.CheckpointReached, MomentKind.StageCompleted })
            {
                var writer = new NetWriter();
                SessionCodec.WriteMoment(writer, moment);
                var reader = new NetReader(writer.ToArray());
                Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Moment));
                Assert.That(SessionCodec.ReadMoment(reader), Is.EqualTo(moment));
                Assert.That(reader.Remaining, Is.Zero);
            }
        }

        [Test]
        public void An_unknown_moment_is_refused_at_the_door()
        {
            var reader = new NetReader(new byte[] { 99 });
            Assert.Throws<NetFormatException>(() => SessionCodec.ReadMoment(reader));
        }
    }
}
```

In `Assets/_BattleBomb/Tests/EditMode/Acceptance/SettingsMenuReleaseAcceptanceTests.cs`, the debug grant moves into the request runner (Step 5), so the release rule must follow it. Replace everything from `[Test]` through the end of the method `Every_debug_member_is_inside_a_development_only_region` with:

```csharp
        [Test]
        public void Every_debug_member_is_inside_a_development_only_region() =>
            AssertDevelopmentOnly(MenuPath, DebugOnly);

        /// <summary>The grant itself moved into the request runner with the settings online (HANDOFF-M8 Task 100),
        /// so a guest's grant runs on the host — and it must stay out of a release there too.</summary>
        [Test]
        public void The_runners_debug_grant_is_inside_a_development_only_region() =>
            AssertDevelopmentOnly(
                "Assets/_BattleBomb/Gameplay/Items/PlayerRequestRunner.cs",
                new[] { "RollDebugItem(", "GrantCoins(", "DebugGrantQuality" });

        private static void AssertDevelopmentOnly(string path, string[] members)
        {
            string[] lines = File.ReadAllLines(path);
            int developmentDepth = 0;
            int depth = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#if"))
                {
                    depth++;
                    if (line.Contains("DEVELOPMENT_BUILD") && line.Contains("UNITY_EDITOR"))
                    {
                        developmentDepth = developmentDepth == 0 ? depth : developmentDepth;
                    }

                    continue;
                }

                if (line.StartsWith("#else") && developmentDepth == depth)
                {
                    developmentDepth = 0;
                    continue;
                }

                if (line.StartsWith("#endif"))
                {
                    if (developmentDepth == depth)
                    {
                        developmentDepth = 0;
                    }

                    depth--;
                    continue;
                }

                if (line.StartsWith("///") || line.StartsWith("//"))
                {
                    continue;
                }

                foreach (string member in members)
                {
                    Assert.That(developmentDepth > 0 || !lines[i].Contains(member), Is.True,
                        $"{path}:{i + 1} mentions {member} outside a DEVELOPMENT_BUILD || UNITY_EDITOR region, " +
                        "so it compiles into a release.");
                }
            }
        }
```

(The body of `AssertDevelopmentOnly` is the old test's body, word for word, with `MenuPath` and `DebugOnly` made parameters.)

In `Assets/_BattleBomb/Tests/PlayMode/LootLoopSmokeTests.cs`, after Task 98's `The_rack_lasts_the_visit_and_goes_with_it` add:

```csharp

        [UnityTest]
        public IEnumerator A_menu_opened_inside_a_step_stops_the_world_before_the_frames_next_step()
        {
            // G8's review: the catch-up loop ran the frame's remaining steps under a menu opened in its first.
            var stepped = new List<int>();
            int heldAt = -1;
            void OnStep(int frame)
            {
                stepped.Add(frame);
                if (heldAt < 0)
                {
                    heldAt = frame;
                    _driver.HoldMenuPause(true);
                }
            }

            Time.captureDeltaTime = 5f / 60f;
            _driver.Stepped += OnStep;
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    yield return null;
                }
            }
            finally
            {
                _driver.Stepped -= OnStep;
                Time.captureDeltaTime = 0f;
                if (heldAt >= 0)
                {
                    _driver.HoldMenuPause(false);
                }
            }

            Assert.That(heldAt, Is.GreaterThanOrEqualTo(0), "No step ran — the case proves nothing.");
            Assert.That(stepped, Is.EqualTo(new List<int> { heldAt }),
                "Steps ran after a menu paused the world inside the frame's first step.");
        }
```

In `Assets/_BattleBomb/Tests/PlayMode/BattleBomb.Tests.PlayMode.asmdef`, add `"BattleBomb.Presentation"` to `references`, after `"BattleBomb.UI"` (the camera test below reads `CameraRig`).

In `Assets/_BattleBomb/Tests/PlayMode/OnlineMenuSmokeTests.cs`, before `private int IndexOf(` add:

```csharp
        [UnityTest]
        public IEnumerator Online_the_hosts_menu_stands_the_host_still_and_the_world_runs_on()
        {
            _driver.HoldMenuPause(true);
            try
            {
                int from = _driver.Frame;
                Vector3 host = _host.Position;
                Vector3 guest = _guestBody.Position;
                _hostInput.Set(Vector2.left, CommandButtons.None);
                _guest.Move = Vector2.left;
                for (int i = 0; i < 120 && _driver.Frame < from + 40; i++)
                {
                    yield return null;
                }

                _hostInput.Release();
                _guest.Move = Vector2.zero;
                Assert.That(_driver.Frame, Is.GreaterThan(from + 30), "The host's menu paused an online game (D60).");
                Assert.That(_driver.PausedForScreen, Is.False);
                Assert.That(_driver.MenuPauseHeld, Is.True, "The menu count must survive: the guest's gate and the settings menu read it.");
                Assert.That(Vector3.Distance(_host.Position, host), Is.LessThan(0.05f), "The host walked while its own menu was open.");
                Assert.That(Vector3.Distance(_guestBody.Position, guest), Is.GreaterThan(0.5f), "The guest was frozen by the host's menu.");
            }
            finally
            {
                _driver.HoldMenuPause(false);
            }
        }

        [UnityTest]
        public IEnumerator Online_the_camera_frames_the_hosts_own_chest()
        {
            yield return WalkHostTo(_chest.Position + new Vector3(-0.5f, 0f, 0f), "the chest");
            _driver.OpenScreen(0, InteractionKind.Chest);
            yield return Steps(3);

            Presentation.Cameras.CameraRig rig = Object.FindAnyObjectByType<Presentation.Cameras.CameraRig>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.FramingOneScreen, Is.True,
                "Online the host is the only player on its display, and its camera must frame its own chest.");
        }

        [UnityTest]
        public IEnumerator At_the_chapters_end_the_guest_is_told_and_the_hosts_results_hear_only_the_host()
        {
            // The guest leans on Jump the whole way: a remote Player 2's held buttons used to hold the host's results shut.
            _guest.Held = CommandButtons.Jump;
            StageRunner runner = _runner;
            yield return PushBothRight(() => runner.StageIndex == 1, 5000, "stage two, through the airlock");
            yield return PushBothRight(() => runner.Phase == StagePhase.Complete, 5000, "the end of the chapter");
            _guest.Held = CommandButtons.Jump;

            ResultsScreen results = Object.FindAnyObjectByType<ResultsScreen>();
            yield return Until(() => results.IsOpen, "the results never opened");
            yield return Steps(4);
            Assert.That(ReceivedMoment(MomentKind.ChapterCompleted), Is.True, "The guest was never told the chapter ended.");

            yield return Steps(90);
            _hostInput.Set(Vector2.zero, CommandButtons.Confirm | CommandButtons.Jump);
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().name == "Gameplay"; i++)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Frontend"),
                "The host's A never left its results while the guest held its buttons.");
        }

        private bool ReceivedMoment(MomentKind moment)
        {
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Moment && SessionCodec.ReadMoment(reader) == moment)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator WalkHostTo(Vector3 target, string what)
        {
            int deadline = _driver.Frame + 5000;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                Vector3 to = target - _host.Position;
                to.y = 0f;
                if (to.magnitude <= 0.8f)
                {
                    _hostInput.Release();
                    yield return Steps(4);
                    yield break;
                }

                _hostInput.Set(new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f)), CommandButtons.None);
                yield return null;
            }

            _hostInput.Release();
            Assert.Fail($"The host never reached {what} — stopped {Vector3.Distance(target, _host.Position):F2} away.");
        }

        /// <summary>Both players push right — the host by script, the guest over the wire — until done.</summary>
        private IEnumerator PushBothRight(System.Func<bool> done, int steps, string what)
        {
            _guest.Move = Vector2.right;
            _hostInput.Set(Vector2.right, CommandButtons.None);
            int deadline = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline && !done(); guard++)
            {
                yield return null;
            }

            _guest.Move = Vector2.zero;
            _hostInput.Release();
            Assert.That(done(), Is.True, $"Pushing right never reached {what}.");
        }
```

Add `using BattleBomb.Core.Chapters;` and `using BattleBomb.UI.Frontend;` to `OnlineMenuSmokeTests.cs` if not present.

In `Assets/_BattleBomb/Tests/PlayMode/GuestMenuSmokeTests.cs`, before `private IEnumerator Join(` add:

```csharp
        [UnityTest]
        public IEnumerator The_hosts_chapter_end_opens_the_guests_results_which_only_the_host_can_end()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.ChapterCompleted);
            extra.Add((Start + 20, writer.ToArray()));
            yield return Join(Recording(extra));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            ResultsScreen results = Object.FindAnyObjectByType<ResultsScreen>();
            yield return AdvanceUntil(() => results.IsOpen, "The host's chapter ended and the guest's results never opened.");

            // Past the dwell, then the guest's A.
            yield return AdvanceSteps(90);
            hands.Set(Vector2.zero, CommandButtons.Confirm | CommandButtons.Jump);
            yield return AdvanceSteps(3);
            hands.Release();
            yield return AdvanceSteps(10);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(NetSession.GameplayScene),
                "The guest's A ended the host's results; only the host's may (D60).");
            Assert.That(results.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator On_a_guest_the_auto_flags_are_the_hosts_to_change()
        {
            yield return Join(Recording(new List<(int, byte[])>()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return OpenSettings(hands);

            hands.Set(Vector2.zero, CommandButtons.Confirm);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceSteps(2);

            bool asked = false;
            foreach (byte[] message in _playback.Sent)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Request)
                {
                    PlayerRequest request = RequestCodec.ReadRequest(reader);
                    asked |= request.Kind == PlayerRequestKind.SetAutoEquip && request.A == 1;
                }
            }

            Assert.That(asked, Is.True, "Toggling auto-equip on a guest never asked the host.");
            Assert.That(_driver.InventoryOf(1).Inventory.AutoEquip, Is.False,
                "The guest changed its own copy; only the host's answer may.");
        }

        [UnityTest]
        public IEnumerator On_a_guest_the_session_row_leaves_the_game()
        {
            yield return Join(Recording(new List<(int, byte[])>()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return OpenSettings(hands);

            // AutoEquip, AutoSell, then the session row.
            for (int i = 0; i < 2; i++)
            {
                hands.Set(new Vector2(0f, -1f), CommandButtons.None);
                yield return AdvanceSteps(2);
                hands.Release();
                yield return AdvanceSteps(2);
            }

            hands.Set(Vector2.zero, CommandButtons.Confirm);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceUntil(() => SceneManager.GetActiveScene().name == NetSession.FrontendScene,
                "The guest's session row never took it out of the game.");

            Assert.That(GameSession.Find().Net.Role, Is.EqualTo(NetRole.Offline), "The guest left the picture but not the game.");
            bool bye = false;
            foreach (byte[] message in _playback.Sent)
            {
                bye |= message.Length > 0 && (NetMessageKind)message[0] == NetMessageKind.Bye;
            }

            Assert.That(bye, Is.True, "The host was never told the guest left.");
        }

        private IEnumerator OpenSettings(ScriptedCommandSource hands)
        {
            hands.Set(Vector2.zero, CommandButtons.Pause);
            yield return AdvanceSteps(1);
            hands.Release();
            yield return AdvanceUntil(() => _driver.MenuPauseHeld, "Pause never opened the guest's own settings.");
            yield return AdvanceSteps(2);
        }
```

Add `using BattleBomb.UI.Frontend;` to `GuestMenuSmokeTests.cs`.

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors (`SessionCodec`, `MomentKind`, `NetMessageKind.Moment`, `CameraRig.FramingOneScreen` do not exist).

- [ ] **Step 3: The moment on the wire**

`Assets/_BattleBomb/Core/Net/SessionCodec.cs`:

```csharp
namespace BattleBomb.Core.Net
{
    /// <summary>A moment in the host's run that the guest acts on too. Values are wire format: never renumber, only add.</summary>
    public enum MomentKind : byte
    {
        /// <summary>The chapter is done: the guest's results open and its own save records the credit (D61).</summary>
        ChapterCompleted = 1,

        /// <summary>D52's run moments — the guest writes its own save at each (Task 101). A chest closing needs no
        /// moment: the guest's own chest closes on the guest, which saves there and then.</summary>
        CheckpointReached = 2,
        StageCompleted = 3,
    }

    /// <summary>The host's session moments (D60): the guest follows them and never starts one. Readers take a reader
    /// positioned after the kind byte.</summary>
    public static class SessionCodec
    {
        public static void WriteMoment(NetWriter w, MomentKind moment)
        {
            w.WriteByte((byte)NetMessageKind.Moment);
            w.WriteByte((byte)moment);
        }

        public static MomentKind ReadMoment(NetReader r)
        {
            var moment = (MomentKind)r.ReadByte();
            if (moment < MomentKind.ChapterCompleted || moment > MomentKind.StageCompleted)
            {
                throw new NetFormatException($"A moment of kind {(byte)moment}.");
            }

            return moment;
        }
    }
}
```

In `Assets/_BattleBomb/Core/Net/NetMessageKind.cs`, replace

```csharp
        Participant = 16,
    }
```

with

```csharp
        Participant = 16,
        Moment = 17,
    }
```

In `Assets/_BattleBomb/Core/Net/NetProtocol.cs`, replace `public const int Version = 4;` with `public const int Version = 5;` and add `5: the host's session moments (Task 100).` to its summary.

- [ ] **Step 4: Nothing pauses online; a menu stands its own player still**

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF):

(a) Replace

```csharp
        internal bool IsReplica => _replica;
```

with

```csharp
        /// <summary>This machine draws the host's world and simulates none of it (D58). Read from what the scene was
        /// built as, not from the connection: a guest who leaves mid-match still stands in a picture.</summary>
        public bool IsReplica => _replica;

        /// <summary>
        /// Online (D60): a guest is in the world with the host — the host's half sets it while one is bound, the
        /// guest's half for the guest's own machine. Nothing pauses; a menu stands its own player idle instead. A solo
        /// host whose game is open to friends, with nobody joined yet, is not online.
        /// </summary>
        public bool IsOnline { get; internal set; }
```

(b) Replace

```csharp
        public bool PausedForScreen =>
            _menuPauseHolders > 0 || (Characters.Ordered.Count <= 1 && _openScreens.Count > 0);
```

with

```csharp
        public bool PausedForScreen =>
            !IsOnline && (_menuPauseHolders > 0 || (Characters.Ordered.Count <= 1 && _openScreens.Count > 0));
```

and change the summary line above it that begins `/// D42's per-mode rule:` to end with ` Online nothing pauses (D60).` (keep the rest).

(c) In `Update`, replace

```csharp
            while (_clock.TryConsumeStep(out int frame))
            {
                RunStep(frame);
            }
```

with

```csharp
            while (_clock.TryConsumeStep(out int frame))
            {
                RunStep(frame);

                // A menu opened inside that step stops the world now, not after the frame's other steps have
                // run under it (G8's review). What the frame had banked is dropped, as the paused branch drops
                // time: nothing is owed to a world that was stopped.
                if (PausedForScreen || HoldForPeer)
                {
                    _clock.Reset();
                    break;
                }
            }
```

(d) In `StepPlayers`, replace

```csharp
                int grabTarget = FindGrabTarget(actor);
```

with

```csharp
                if (!screenOpen && IsOnline && _menuPauseHolders > 0 && _players.IsLocal(actor.PlayerId))
                {
                    // Online nothing pauses (D60): a menu on this display stands this display's player idle
                    // instead, exactly as a chest does — the partner on the other machine plays on.
                    command = PlayerCommand.Idle(frame);
                }

                int grabTarget = FindGrabTarget(actor);
```

- [ ] **Step 5: The debug grant becomes a request**

In `Assets/_BattleBomb/Gameplay/Items/PlayerRequestRunner.cs`:

(a) In the first `switch`, after the `SetAutoSell` case add:

```csharp

                case PlayerRequestKind.DebugGrant:
                    return DebugGrant(bag, driver);
```

(b) Replace the `default:` branch's comment and return with:

```csharp
                default:
                    // Nothing else is a verb a screen can ask for.
                    return RequestOutcome.No(RequestRefusal.Refused);
```

(c) After the method `UpgradeWorn` add:

```csharp

        /// <summary>
        /// Debug only, and it dies when real content arrives: one of each starter definition twice, spending money
        /// and XP — combining (D44) needs two identical items, which random drops almost never hand you. Here rather
        /// than in the settings menu since the settings went online (HANDOFF-M8 Task 100): a guest's grant runs on
        /// the host, into the guest's own bag. Compiles out of a release (F2), and a release host refuses it.
        /// </summary>
        private static RequestOutcome DebugGrant(PlayerInventory bag, SimulationDriver driver)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int[] definitions = { 7, 8, 9, 12, 13, 1, 2, 3 };
            for (int i = 0; i < definitions.Length; i++)
            {
                for (int copy = 0; copy < 2; copy++)
                {
                    ItemInstance item = driver.RollDebugItem(definitions[i], DebugGrantQuality);
                    if (!item.IsEmpty)
                    {
                        bag.Take(item);
                    }
                }
            }

            bag.GrantCoins(5000);
            bag.Earn(2000f);
            return RequestOutcome.Done();
#else
            return RequestOutcome.No(RequestRefusal.Refused);
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Mid-ladder, so a feel judgement is never about an absurd item.</summary>
        private const float DebugGrantQuality = 2.2f;
#endif
```

- [ ] **Step 6: The host tells the guest the chapter ended; the guest follows**

In `Assets/_BattleBomb/Gameplay/World/StageRunner.cs` (CRLF), after the method `ReplicaHandOver` add:

```csharp

        /// <summary>The host's chapter ended (HANDOFF-M8 Task 100): the guest's results — and its own save's credit for
        /// the chapter — follow it, through the same event the host's own results and save hear.</summary>
        internal void ReplicaChapterCompleted()
        {
            if (_replica)
            {
                ChapterCompleted?.Invoke();
            }
        }
```

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`:

(a) After the field `private readonly NetWriter _participant = new NetWriter(16 * 1024);` add:

```csharp
        private readonly NetWriter _moment = new NetWriter(16);
```

(b) In `Begin`, after `_driver.RackChanged += OnRackChanged;` add:

```csharp
            _driver.IsOnline = _net.IsConnected;
```

and inside the existing `if (_runner != null)` block, after `_runner.RemoteStageReady = stage => !_net.IsConnected || _guestReady.Contains(stage);` add:

```csharp
                _runner.ChapterCompleted += OnChapterCompleted;
```

(c) After the method `SendAnswers` add:

```csharp

        /// <summary>The run's end reaches the guest at once, after its bag: its results open, and its own save records
        /// the chapter (D61). The host's own results are already opening from the same event.</summary>
        private void OnChapterCompleted()
        {
            if (!_net.IsConnected)
            {
                return;
            }

            if (_watched.TryGetValue(_net.GuestPlayerId.Value, out Watched guest))
            {
                SendParticipant(_net.GuestPlayerId.Value, guest);
                guest.Dirty = false;
                guest.Forced = false;
                guest.SentAt = _driver.Frame;
            }

            _moment.Reset();
            SessionCodec.WriteMoment(_moment, MomentKind.ChapterCompleted);
            _net.Send(NetChannel.Reliable, _moment);
        }
```

(d) In `OnPeerLeft`, after `_driver.HoldForPeer = false;` add `_driver.IsOnline = false;`.

(e) In `OnDestroy`, inside `if (_driver != null)`, after `_driver.HoldForPeer = false;` add `_driver.IsOnline = false;`; inside `if (_runner != null)`, after `_runner.RemoteStageReady = null;` add `_runner.ChapterCompleted -= OnChapterCompleted;`.

In `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

(a) In `Begin`, after `_driver.RequestRoute = id => id == _local.Value ? _requests : null;` add `_driver.IsOnline = true;`.

(b) In `OnMessage`, before `case NetMessageKind.LoadStage:` add:

```csharp
                case NetMessageKind.Moment:
                    OnMoment(SessionCodec.ReadMoment(reader));
                    break;

```

(c) After the method `ApplyMenuState` add:

```csharp

        /// <summary>A moment in the host's run. The chapter's end opens the guest's results; the others are Task 101's.</summary>
        private void OnMoment(MomentKind moment)
        {
            if (moment == MomentKind.ChapterCompleted)
            {
                _runner?.ReplicaChapterCompleted();
            }
        }
```

(d) In `OnDestroy`, inside `if (_driver != null)`, add `_driver.IsOnline = false;`.

- [ ] **Step 7: The camera frames the one player on this display**

In `Assets/_BattleBomb/Presentation/Cameras/CameraRig.cs`:

(a) After the field `[SerializeField] private bool _frameOnScreens = true;` add:

```csharp

        /// <summary>The camera is holding one player's screen subject in the half the panel leaves free — read by the
        /// smoke suite; nothing in the game depends on it.</summary>
        public bool FramingOneScreen { get; private set; }
```

(b) In `LateUpdate`, replace

```csharp
            CameraFrame frame = TryFrameOnScreen(actors, tuning, out CameraFrame framed)
                ? framed
                : CameraFraming.Compute(_targets, tuning, _driver.Bounds);
```

with

```csharp
            FramingOneScreen = TryFrameOnScreen(actors, tuning, out CameraFrame framed);
            CameraFrame frame = FramingOneScreen
                ? framed
                : CameraFraming.Compute(_targets, tuning, _driver.Bounds);
```

(c) In `TryFrameOnScreen`, replace

```csharp
            if (!_frameOnScreens || actors.Count != 1 || actors[0] == null)
            {
                return false;
            }

            int playerId = actors[0].PlayerId.Value;
            if (!_driver.TryGetOpenScreen(playerId, out InteractionKind kind))
            {
                return false;
            }

            frame = FrameOnSubject(SubjectOf(playerId, kind, actors[0]), tuning);
            return true;
        }
```

with

```csharp
            CharacterActor local = SoleLocalPlayer(actors);
            if (!_frameOnScreens || local == null)
            {
                return false;
            }

            int playerId = local.PlayerId.Value;
            if (!_driver.TryGetOpenScreen(playerId, out InteractionKind kind))
            {
                return false;
            }

            frame = FrameOnSubject(SubjectOf(playerId, kind, local), tuning);
            return true;
        }

        /// <summary>The one player this display is for — alone, or online where the partner has a display of their own
        /// (HANDOFF-M8 planning decision 15) — or null on the couch, where the camera belongs to both.</summary>
        private CharacterActor SoleLocalPlayer(IReadOnlyList<CharacterActor> actors)
        {
            if (_driver.Players.LocalCount != 1)
            {
                return null;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] != null && _driver.Players.IsLocal(actors[i].PlayerId))
                {
                    return actors[i];
                }
            }

            return null;
        }
```

and in the `TryFrameOnScreen` summary replace `a single character means a single player holds the display, so the camera is theirs to take` with `a single local player means a single player holds the display, so the camera is theirs to take`.

- [ ] **Step 8: The settings and the results, online**

In `Assets/_BattleBomb/UI/Chest/SettingsMenu.cs`:

(a) Add `using BattleBomb.Core.Items;` to the usings.

(b) After the field `private readonly List<PlayerInventory> _bags = new List<PlayerInventory>();` add:

```csharp
        private readonly List<int> _bagOwners = new List<int>();
```

(c) Replace the method `ToggleSetting` body's loop

```csharp
            bool current = autoSell ? _bags[0].Inventory.AutoSell : _bags[0].Inventory.AutoEquip;
            for (int i = 0; i < _bags.Count; i++)
            {
                PlayerInventory bag = _bags[i];
                if (autoSell)
                {
                    bag.SetAutoSell(!current);
                }
                else
                {
                    bag.SetAutoEquip(!current);
                }
            }
```

with

```csharp
            bool current = autoSell ? _bags[0].Inventory.AutoSell : _bags[0].Inventory.AutoEquip;
            for (int i = 0; i < _bags.Count; i++)
            {
                // Through each player's requests (HANDOFF-M8 planning decision 11): on the couch it lands now; on a
                // guest the host changes the setting in the guest's own save, and the copy comes back.
                _driver.RequestsFor(_bagOwners[i]).Send(
                    autoSell ? PlayerRequest.SetAutoSell(!current) : PlayerRequest.SetAutoEquip(!current), null);
            }
```

(d) Replace the whole `GrantTestLoot` method and the `DebugGrantQuality` constant after it (both inside the `#if DEVELOPMENT_BUILD || UNITY_EDITOR` region — the region's `#if` line and its `Overlay` members stay) with:

```csharp
        /// <summary>
        /// Debug only: every player on this display is handed test loot — the grant itself is the request runner's
        /// since the settings went online (HANDOFF-M8 Task 100), so a guest's runs on the host, into its own bag.
        /// </summary>
        private void GrantTestLoot()
        {
            for (int b = 0; b < _bagOwners.Count; b++)
            {
                _driver.RequestsFor(_bagOwners[b]).Send(PlayerRequest.DebugGrant(), null);
            }
        }
```

(e) In `Toggle`, in the `ReturnToChapters` case, replace `ReturnToChapters();` with:

```csharp
                    if (_driver.IsReplica)
                    {
                        LeaveTheGame();
                    }
                    else
                    {
                        ReturnToChapters();
                    }

```

(f) After the method `ReturnToChapters` add:

```csharp

        /// <summary>
        /// On a guest the session's moments are the host's (D60): the row that returns a run to chapter select leaves
        /// the game instead. The host hears it at once and carries on solo (D61); this machine goes back to its own
        /// front door. Task 101 saves the guest's latest copy of itself on the way out.
        /// </summary>
        private void LeaveTheGame()
        {
            Close();
            Gameplay.Session.GameSession session = Gameplay.Session.GameSession.Find();
            Gameplay.Net.NetSession net = session != null ? session.Net : null;
            if (net != null)
            {
                net.Leave();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(
                "Frontend", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
```

(g) Replace the method `CollectBags` with:

```csharp
        private void CollectBags()
        {
            _bags.Clear();
            _bagOwners.Clear();
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                // This display's players only (HANDOFF-M8 planning decision 15): online, the partner's settings are
                // on their own machine and in their own save.
                if (!_driver.Players.IsLocal(actors[i].PlayerId))
                {
                    continue;
                }

                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag != null)
                {
                    _bags.Add(bag);
                    _bagOwners.Add(actors[i].PlayerId.Value);
                }
            }
        }
```

(h) In `Repaint`, replace

```csharp
                    case SettingsRow.ReturnToChapters:
                        AppendLine(row, "Return to chapter select", UiBuild.Focus);
                        break;
```

with

```csharp
                    case SettingsRow.ReturnToChapters:
                        AppendLine(row, _driver.IsReplica ? "Leave the game" : "Return to chapter select", UiBuild.Focus);
                        break;
```

After these edits `grep -n "RollDebugItem\|GrantCoins\|\.Take(\|\.Earn(\|SetAutoSell(\|SetAutoEquip(" Assets/_BattleBomb/UI/Chest/SettingsMenu.cs` finds only the two `PlayerRequest.SetAuto…` calls.

In `Assets/_BattleBomb/UI/Frontend/ResultsScreen.cs`:

(a) In `Tick`, replace

```csharp
            Repaint();

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
```

with

```csharp
            Repaint();

            // A guest's results follow the host's (D60): its hands never end the host's run's last screen. When
            // the host leaves the results the guest's machine follows it to the front door.
            if (_driver.IsReplica)
            {
                return;
            }

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
```

(b) In `Tick`, in the swallow loop replace

```csharp
                for (int i = 0; i < actors.Count; i++)
                {
                    if (MenuPress.AnyHeld(_driver.CommandFor(actors[i].PlayerId.Value)))
```

with

```csharp
                for (int i = 0; i < actors.Count; i++)
                {
                    // This display's hands only: a remote player's last buttons stay held on the host while their
                    // own machine shows its own results, and used to hold these shut.
                    if (!_driver.Players.IsLocal(actors[i].PlayerId))
                    {
                        continue;
                    }

                    if (MenuPress.AnyHeld(_driver.CommandFor(actors[i].PlayerId.Value)))
```

and in the confirm loop replace

```csharp
            for (int i = 0; i < actors.Count; i++)
            {
                if (MenuPress.From(_driver.CommandFor(actors[i].PlayerId.Value)).Confirm)
```

with

```csharp
            for (int i = 0; i < actors.Count; i++)
            {
                if (_driver.Players.IsLocal(actors[i].PlayerId)
                    && MenuPress.From(_driver.CommandFor(actors[i].PlayerId.Value)).Confirm)
```

(c) At the top of `Leave`, before `if (_leaving)`, add:

```csharp
            if (_driver != null && _driver.IsReplica)
            {
                return;
            }

```

(d) In `Repaint`, replace

```csharp
            _prompts.Clear();
            _prompts.Add(new Prompt(PromptKey.Confirm, "Back to chapters"));
            _promptRow.Show(_prompts, family);
```

with

```csharp
            _prompts.Clear();
            if (_driver.IsReplica)
            {
                _text.Append("\n\n").Append(UiBuild.Tint("Waiting for the host.", UiBuild.InkDim));
                _body.text = _text.ToString();
            }
            else
            {
                _prompts.Add(new Prompt(PromptKey.Confirm, "Back to chapters"));
            }

            _promptRow.Show(_prompts, family);
```

- [ ] **Step 9: Run the tests** — `recompile`, `console` `level: error` clean. EditMode `SessionCodecTests` (two), `SettingsMenuReleaseAcceptanceTests` (two), then the full EditMode suite (817 + 2 + 1 = 820). PlayMode (async): `LootLoopSmokeTests` (the catch-up case red before Step 4 (c), green after), `OnlineMenuSmokeTests` (eleven), `GuestMenuSmokeTests` (eight), `ChapterLoopSmokeTests` (its solo-pause cases must pass unchanged — offline nothing about the pause changed), `GuestReplicaSmokeTests.The_guests_own_menu_never_reaches_the_host` (the menu count still drives the gate). Then the full PlayMode suite (80 + 1 + 3 + 3 = 87). Delete `Assets/InitTestScene*`.

- [ ] **Step 10: Commit** — subject `100: online screen rules — nothing pauses, the host's moments`. Body: online a menu stands its own display's player idle instead of pausing (D60) while the open-menu count keeps driving the guest's gate; the camera and the chest layout follow the one player on this display; the guest's settings toggle through requests and its session row leaves the game; the host's chapter end opens the guest's results, which only the host can end; the host's results hear only the host's hands; a menu opened inside a step stops the frame's catch-up (G8); the debug grant is a request, still out of a release; protocol 5.

---

### Task 101: Two stashes and two saves

D61: each player keeps what they earned, in their own save. The guest brings their own hero and their own inventory — the pick, and their save cut down to that hero, travel in a new `LobbyPick` message the moment the guest is welcomed (until Task 102's lobby lets them choose, the guest's machine picks its own couch hero for them). On the host the guest's inventory goes into a **second stash** — the couch keeps its one — and Plan 1's stand-in (the guest playing the host's hero) retires. Each machine saves **only its own player**: the host at its D52 moments as today; the guest when the host says a moment passed (`Moment` — the host sends the guest's latest copy first), when its own chest closes, and at its own clean exits — and never before the host's first copy of the guest has arrived, because until then the guest's machine holds nothing of the player's and writing it would destroy their save. The chapter's credit lands in the guest's own save; the resume point stays the host's.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/LobbyCodec.cs`
- Modify: `Assets/_BattleBomb/Core/Net/NetMessageKind.cs`, `Core/Net/NetProtocol.cs`, `Core/Saves/SaveMapper.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Items/PlayerInventory.cs`, `Gameplay/Net/NetSession.cs`, `Gameplay/Net/NetHost.cs`, `Gameplay/Net/NetGuest.cs`, `Gameplay/Session/SessionBinder.cs` (CRLF), `Gameplay/Session/SaveService.cs` (CRLF)
- Modify: `Assets/_BattleBomb/UI/Chest/SettingsMenu.cs`
- Test: create `Tests/EditMode/Net/LobbyCodecTests.cs`; modify `Tests/PlayMode/HeadlessGuest.cs`, `Tests/PlayMode/OnlineMenuSmokeTests.cs`, `Tests/PlayMode/GuestMenuSmokeTests.cs`

- [ ] **Step 1: Write the failing Core tests**

`Assets/_BattleBomb/Tests/EditMode/Net/LobbyCodecTests.cs`:

```csharp
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>What a guest brings to the host's game (D59/D61): the hero they picked, whether they are ready, and
    /// their own save cut down to that hero.</summary>
    public sealed class LobbyCodecTests
    {
        private static SaveGame SaveWithTwoHeroes()
        {
            var inventory = new Inventory();
            inventory.Add(new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], requiredLevel: 1), 99);
            var fire = new CharacterState(new ElementId(1), new XpLedger(7, 0f, 0, BaseStats.Zero, 0), new Inventory());
            var ice = new CharacterState(new ElementId(2), new XpLedger(3, 0f, 0, BaseStats.Zero, 0), new Inventory());
            var story = new Core.Chapters.StoryProgress();
            story.SetResume("fixture", 1, 0);
            return SaveMapper.Capture(inventory.Sack, new Wallet(640), new[] { fire, ice }, story);
        }

        [Test]
        public void A_guest_brings_their_sack_their_wallet_and_only_the_hero_they_picked()
        {
            SaveGame brought = SaveMapper.ParticipantFrom(SaveWithTwoHeroes(), 2);

            Assert.That(brought.Coins, Is.EqualTo(640));
            Assert.That(brought.Sack.Length, Is.EqualTo(1));
            Assert.That(brought.Characters.Length, Is.EqualTo(1));
            Assert.That(brought.Characters[0].ElementId, Is.EqualTo(2));
            Assert.That(brought.Characters[0].Level, Is.EqualTo(3));
            Assert.That(brought.Story.ResumeChapterId, Is.Empty, "The guest's resume point went to the host; it is the guest's alone.");
        }

        [Test]
        public void A_hero_never_played_is_brought_as_nobody_yet()
        {
            SaveGame brought = SaveMapper.ParticipantFrom(SaveWithTwoHeroes(), 9);

            Assert.That(brought.Characters, Is.Empty);
            Assert.That(brought.Coins, Is.EqualTo(640), "A new hero still plays from the player's own sack and wallet.");
        }

        [Test]
        public void No_save_at_all_brings_an_empty_one()
        {
            SaveGame brought = SaveMapper.ParticipantFrom(null, 1);

            Assert.That(brought.Coins, Is.Zero);
            Assert.That(brought.Sack, Is.Empty);
        }

        [Test]
        public void A_pick_travels_with_what_it_brings()
        {
            var writer = new NetWriter();
            LobbyCodec.WritePick(writer, new LobbyPick(3, true, SaveMapper.ParticipantFrom(SaveWithTwoHeroes(), 1)));
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.LobbyPick));
            LobbyPick back = LobbyCodec.ReadPick(reader);

            Assert.That(back.RosterIndex, Is.EqualTo(3));
            Assert.That(back.Ready, Is.True);
            Assert.That(back.Brought, Is.Not.Null);
            Assert.That(back.Brought.Characters[0].Level, Is.EqualTo(7));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void A_pick_that_is_not_ready_brings_nothing()
        {
            var writer = new NetWriter();
            LobbyCodec.WritePick(writer, new LobbyPick(1, false, null));
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            LobbyPick back = LobbyCodec.ReadPick(reader);

            Assert.That(back.Ready, Is.False);
            Assert.That(back.Brought, Is.Null);
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail** — `recompile`. Expected: compile errors (`LobbyCodec`, `LobbyPick`, `SaveMapper.ParticipantFrom`).

- [ ] **Step 3: The pick and what it brings**

`Assets/_BattleBomb/Core/Net/LobbyCodec.cs`:

```csharp
using BattleBomb.Core.Saves;

namespace BattleBomb.Core.Net
{
    /// <summary>A guest's choice at character select (D59): a hero by roster index, ready or not, and — once ready —
    /// their own save cut down to that hero (<see cref="SaveMapper.ParticipantFrom"/>).</summary>
    public readonly struct LobbyPick
    {
        public readonly int RosterIndex;
        public readonly bool Ready;
        public readonly SaveGame Brought;

        public LobbyPick(int rosterIndex, bool ready, SaveGame brought)
        {
            RosterIndex = rosterIndex;
            Ready = ready;
            Brought = ready ? brought : null;
        }
    }

    /// <summary>The lobby's messages (HANDOFF-M8 Tasks 101–102). Readers take a reader positioned after the kind byte.</summary>
    public static class LobbyCodec
    {
        public static void WritePick(NetWriter w, in LobbyPick pick)
        {
            w.WriteByte((byte)NetMessageKind.LobbyPick);
            w.WriteInt(pick.RosterIndex);
            w.WriteBool(pick.Ready);
            w.WriteBool(pick.Brought != null);
            if (pick.Brought != null)
            {
                ParticipantCodec.WriteState(w, pick.Brought);
            }
        }

        public static LobbyPick ReadPick(NetReader r)
        {
            int roster = r.ReadInt();
            bool ready = r.ReadBool();
            SaveGame brought = r.ReadBool() ? ParticipantCodec.ReadState(r) : null;
            return new LobbyPick(roster, ready, brought);
        }
    }
}
```

In `Assets/_BattleBomb/Core/Net/NetMessageKind.cs`, replace

```csharp
        Moment = 17,
    }
```

with

```csharp
        Moment = 17,
        LobbyPick = 18,
    }
```

In `Assets/_BattleBomb/Core/Net/NetProtocol.cs`, replace `public const int Version = 5;` with `public const int Version = 6;` and add `6: the guest's pick and what it brings (Task 101).` to its summary.

In `Assets/_BattleBomb/Core/Saves/SaveMapper.cs`, after Task 99's method `Participant` add:

```csharp

        /// <summary>
        /// A player's own save cut down to the hero they chose (HANDOFF-M8 Task 101): the sack, wallet and auto
        /// flags, and that hero's character if they have ever played it. What a guest brings to the host's game. The
        /// story does not come: the resume point and the unlocks are the guest's own, and stay home.
        /// </summary>
        public static SaveGame ParticipantFrom(SaveGame save, int elementId)
        {
            SaveGame source = save ?? SaveGame.Fresh();
            CharacterSave hero = null;
            CharacterSave[] characters = source.Characters;
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] != null && characters[i].ElementId == elementId)
                {
                    hero = characters[i];
                }
            }

            return new SaveGame(
                SaveCodec.CurrentVersion, source.Coins, source.AutoEquip, source.AutoSell, source.Sack,
                hero != null ? new[] { hero } : System.Array.Empty<CharacterSave>(), new StoryProgressSave());
        }
```

- [ ] **Step 4: Run it** — `recompile`; `run_tests` EditMode `LobbyCodecTests`. Expected: five pass.

- [ ] **Step 5: The session carries the guest's pick**

In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

(a) After the property `public string LastRefusal { get; private set; }` add:

```csharp

        /// <summary>Host: the hero the guest picked, by roster index, or -1 before a pick has arrived (D59).</summary>
        public int GuestPick { get; private set; } = -1;

        /// <summary>Host: the guest has readied at character select.</summary>
        public bool GuestReady { get; private set; }

        /// <summary>Host: the guest's own save cut down to their hero — what their stash and body are restored from
        /// (D61). Null until they ready. Never written into this machine's save or session.</summary>
        public SaveGame GuestBrought { get; private set; }

        /// <summary>Host: the guest's pick or readiness changed.</summary>
        public event Action GuestLobbyChanged;
```

(b) In `Dispatch`, before the `case NetMessageKind.KeepAlive:` line add:

```csharp
                    case NetMessageKind.LobbyPick when Role == NetRole.Host && _welcomed:
                        LobbyPick pick = LobbyCodec.ReadPick(reader);
                        GuestPick = pick.RosterIndex;
                        GuestReady = pick.Ready;
                        GuestBrought = pick.Brought;
                        GuestLobbyChanged?.Invoke();
                        return;

```

(c) In the `Welcome` case, replace

```csharp
                        _welcomed = true;
                        Status = "Joined — waiting for the host to launch";
                        PeerJoined?.Invoke();
                        return;
```

with

```csharp
                        _welcomed = true;
                        Status = "Joined — waiting for the host to launch";
                        PeerJoined?.Invoke();

                        // Until the lobby's screens (Task 102), the guest's machine picks for its player: the hero
                        // they last sat as on their own couch, ready at once.
                        SendLobbyPick(StandInPick(), true);
                        return;
```

(d) After the method `JoinLocal` add:

```csharp

        /// <summary>
        /// Guest: tells the host which hero this machine's player picked and whether they are ready. Ready, it brings
        /// their own save cut down to that hero (D61) — the host restores the guest's stash and body from it.
        /// </summary>
        public void SendLobbyPick(int rosterIndex, bool ready)
        {
            if (Role != NetRole.Guest)
            {
                return;
            }

            GameSession session = GetComponent<GameSession>();
            CharacterDefinition hero = session != null && rosterIndex >= 0 && rosterIndex < session.Roster.Length
                ? session.Roster[rosterIndex]
                : null;
            SaveGame brought = ready && hero != null
                ? SaveMapper.ParticipantFrom(session.LoadedSave, hero.Element.Value)
                : null;
            _writer.Reset();
            LobbyCodec.WritePick(_writer, new LobbyPick(rosterIndex, ready && hero != null, brought));
            Send(NetChannel.Reliable, _writer);
        }

        /// <summary>The hero this machine's own couch last sat Player 1 as, or the roster's first.</summary>
        private int StandInPick()
        {
            GameSession session = GetComponent<GameSession>();
            if (session == null || session.Characters.Length == 0 || session.Characters[0] == null)
            {
                return 0;
            }

            for (int i = 0; i < session.Roster.Length; i++)
            {
                if (session.Roster[i] == session.Characters[0])
                {
                    return i;
                }
            }

            return 0;
        }
```

(e) In `Lost`, replace

```csharp
            Status = $"Hosting — the guest {why}; waiting for another";
```

with

```csharp
            ForgetGuest();
            Status = $"Hosting — the guest {why}; waiting for another";
```

(f) In `Close`, after `_welcomed = false;` add `ForgetGuest();`, and after the method `Close` add:

```csharp

        private void ForgetGuest()
        {
            GuestPick = -1;
            GuestReady = false;
            GuestBrought = null;
        }
```

Add `using BattleBomb.Core.Saves;` to `NetSession.cs`. The `NetWriter _writer` is 512 bytes and grows; a brought save is a few kilobytes deflated.

- [ ] **Step 6: Two stashes, and the guest as themselves**

In `Assets/_BattleBomb/Gameplay/Items/PlayerInventory.cs`:

(a) After the property `Stash` add:

```csharp

        /// <summary>
        /// Online (HANDOFF-M8 planning decision 13): the binder gives a player whose inventory comes from another save
        /// a stash of their own. Before <c>OnEnable</c> it is simply kept; after, the bag is rebuilt over it — the
        /// binder calls this before anything is restored, so nothing is lost.
        /// </summary>
        internal void UseStash(SharedStash stash)
        {
            if (stash == null || stash == _stash)
            {
                return;
            }

            if (isActiveAndEnabled && _stash != null)
            {
                _stash.Changed -= OnStashChanged;
            }

            _stash = stash;
            _inventory = new Inventory(stash.Sack);
            if (isActiveAndEnabled)
            {
                _stash.Changed += OnStashChanged;
                Changed?.Invoke();
            }
        }

        /// <summary>The guest's copy of its own player has arrived from the host at least once (Task 101) — the
        /// guest's save may be written from it only after that.</summary>
        internal bool MirroredFromHost { get; private set; }
```

(b) In Task 99's `ApplyMirror`, replace

```csharp
            if (full)
            {
                SaveMapper.RestoreSack(state, Stash.Sack, catalog, revision);
```

with

```csharp
            if (full)
            {
                MirroredFromHost = true;
                SaveMapper.RestoreSack(state, Stash.Sack, catalog, revision);
```

In `Assets/_BattleBomb/Gameplay/Session/SessionBinder.cs` (CRLF):

(a) Replace

```csharp
                // Plan 1's stand-in until the lobby (HANDOFF-M8 Task 102): a connected guest takes the
                // second seat as the host's own hero — over a local Player 2, because D59 lets a couch
                // pair and an online guest never both be here. Chosen here and never written into the
                // session, which outlives the match and would seat the stand-in back at the front door.
                int seat = hosting && i == net.GuestPlayerId.Value ? 0 : i;
                CharacterDefinition definition = seat < _session.Characters.Length ? _session.Characters[seat] : null;
```

with

```csharp
                // A connected guest's seat is the hero they picked from their own save (D59, Task 101) — over a
                // local Player 2, because a couch pair and an online guest are never both here. Chosen here and never
                // written into the session, which outlives the match and would seat it back at the front door.
                CharacterDefinition definition = hosting && i == net.GuestPlayerId.Value
                    ? GuestDefinition(net)
                    : i < _session.Characters.Length ? _session.Characters[i] : null;
```

(b) Replace

```csharp
            if (_session == null || _session.LoadedSave == null)
            {
                return;
            }

            // The guest's gear is the host's to hold during a match (D61); nothing of this machine's
            // save is restored into a world it only draws.
            if (NetSession.RoleOf(_session) == NetRole.Guest)
            {
                return;
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_stash == null)
            {
                _stash = FindAnyObjectByType<SharedStash>();
            }

            if (_driver == null || _stash == null)
```

with

```csharp
            if (_session == null)
            {
                return;
            }

            // The guest's gear is the host's to hold during a match (D61); nothing of this machine's
            // save is restored into a world it only draws.
            if (NetSession.RoleOf(_session) == NetRole.Guest)
            {
                return;
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_stash == null)
            {
                _stash = FindAnyObjectByType<SharedStash>();
            }

            // The guest's own save — what they brought in the lobby — into their stash and body (D61). Before the
            // host's: a host with no save yet still plays with a guest who has one.
            if (_remoteSeat >= 0 && _driver != null)
            {
                RestoreGuest(_players[_remoteSeat], _session.Net);
            }

            if (_session.LoadedSave == null)
            {
                return;
            }

            if (_driver == null || _stash == null)
```

(c) In `BindHost`, replace

```csharp
            _remoteSeat = slot;
            RemoteCommandSource remote = NetSeats.MakeRemote(_players[slot], net.GuestPlayerId);
```

with

```csharp
            _remoteSeat = slot;

            // The guest plays from their own save (D61, planning decision 13): a stash of their own, beside the
            // couch's one — which is the host's alone now.
            PlayerInventory guestBag = _players[slot].GetComponent<PlayerInventory>();
            if (guestBag != null)
            {
                guestBag.UseStash(new GameObject("Guest Stash").AddComponent<SharedStash>());
            }

            RemoteCommandSource remote = NetSeats.MakeRemote(_players[slot], net.GuestPlayerId);
```

(d) After the method `BindGuest` add:

```csharp

        /// <summary>The hero the guest picked, or — before the pick has arrived, which only a hand-driven development
        /// launch can race — the host's own (Plan 1's stand-in).</summary>
        private CharacterDefinition GuestDefinition(NetSession net)
        {
            int pick = net.GuestPick;
            if (pick >= 0 && pick < _session.Roster.Length && _session.Roster[pick] != null)
            {
                return _session.Roster[pick];
            }

            return _session.Characters.Length > 0 ? _session.Characters[0] : null;
        }

        /// <summary>The guest's own save, brought in the lobby, laid into the stash and body the binder gave them
        /// (D61). A hero never played starts fresh; a guest who brought nothing starts with nothing.</summary>
        private void RestoreGuest(CharacterActor actor, NetSession net)
        {
            PlayerInventory bag = actor != null ? actor.GetComponent<PlayerInventory>() : null;
            SaveGame brought = net != null ? net.GuestBrought : null;
            if (bag == null || brought == null)
            {
                return;
            }

            IReadOnlyList<ItemSpec> catalog = _driver.ItemSpecs;
            SaveMapper.RestoreSack(brought, bag.Stash.Sack, catalog);
            bag.Stash.Restore(bag.Stash.Sack, SaveMapper.RestoreWallet(brought));
            CharacterSave saved = FindCharacter(brought, actor.Element.Value);
            if (saved != null)
            {
                bag.SetLedger(SaveMapper.RestoreCharacter(saved, bag.Inventory, catalog));
            }
        }
```

(e) In `Start`, the per-player restore loop's comment — `// The guest's stand-in wears none of the host's saved gear: a copy of it could reach` / `// the shared sack and be saved back as a second of each item.` — becomes `// The guest's seat wears the guest's own gear (RestoreGuest), never the host's.` The condition `i == _remoteSeat` stays.

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`:

(a) Replace the loop in `SendLaunch`

```csharp
            for (int i = 0; i < picks.Length; i++)
            {
                // The guest's seat is the host's own hero until the lobby (SessionBinder's stand-in).
                picks[i] = IndexInRoster(_session.Characters[i == _net.GuestPlayerId.Value ? 0 : i]);
            }
```

with

```csharp
            for (int i = 0; i < picks.Length; i++)
            {
                picks[i] = i == _net.GuestPlayerId.Value ? GuestPick() : IndexInRoster(_session.Characters[i]);
            }
```

and after the method `IndexInRoster` add:

```csharp

        /// <summary>The guest's own pick (D59), or the host's hero if none has arrived (SessionBinder's fallback).</summary>
        private int GuestPick() =>
            _net.GuestPick >= 0 && _net.GuestPick < _session.Roster.Length
                ? _net.GuestPick
                : IndexInRoster(_session.Characters[0]);
```

(b) Replace the method `OnChapterCompleted` (Task 100) with:

```csharp
        private void OnChapterCompleted() => SendMoment(MomentKind.ChapterCompleted);

        private void OnCheckpointReached(int stage, int arena) => SendMoment(MomentKind.CheckpointReached);

        private void OnStageCompleted(int stage) => SendMoment(MomentKind.StageCompleted);

        /// <summary>
        /// A moment in the host's run reaches the guest at once, after the guest's latest copy of itself (D61): at
        /// the chapter's end its results open and its own save records the credit; at the others its own save writes.
        /// The resume point never travels — it is the host's alone.
        /// </summary>
        private void SendMoment(MomentKind moment)
        {
            if (!_net.IsConnected)
            {
                return;
            }

            if (_watched.TryGetValue(_net.GuestPlayerId.Value, out Watched guest))
            {
                SendParticipant(_net.GuestPlayerId.Value, guest);
                guest.Dirty = false;
                guest.Forced = false;
                guest.SentAt = _driver.Frame;
            }

            _moment.Reset();
            SessionCodec.WriteMoment(_moment, moment);
            _net.Send(NetChannel.Reliable, _moment);
        }
```

(c) In `Begin`, after `_runner.ChapterCompleted += OnChapterCompleted;` add:

```csharp
                _runner.CheckpointReached += OnCheckpointReached;
                _runner.StageCompleted += OnStageCompleted;
```

and in `OnDestroy`, after `_runner.ChapterCompleted -= OnChapterCompleted;` add:

```csharp
                _runner.CheckpointReached -= OnCheckpointReached;
                _runner.StageCompleted -= OnStageCompleted;
```

In `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

(a) After the field `private RemotePlayerRequests _requests;` add `private SaveService _saves;`.

(b) In `Begin`, after `_runner = FindAnyObjectByType<StageRunner>();` add `_saves = FindAnyObjectByType<SaveService>();`.

(c) Replace Task 100's `OnMoment` with:

```csharp
        /// <summary>A moment in the host's run (D61). The chapter's end opens the guest's results and records the
        /// credit through the runner's own event; the others write the guest's own save from its latest copy, which
        /// the host sent just before.</summary>
        private void OnMoment(MomentKind moment)
        {
            if (moment == MomentKind.ChapterCompleted)
            {
                _runner?.ReplicaChapterCompleted();
                return;
            }

            if (_saves != null)
            {
                _saves.SaveNow();
            }
        }
```

`NetGuest.cs` already has `using BattleBomb.Gameplay.Session;`.

- [ ] **Step 7: Each machine saves its own player**

In `Assets/_BattleBomb/Gameplay/Session/SaveService.cs` (CRLF):

(a) Replace

```csharp
            // The guest writes its own save only when the host says a D52 moment happened (Plan 2,
            // D61). Its own copy of the session is a picture of the host's run, and saving a picture
            // over a real file is how a guest loses their gear. Read from what this scene was built
            // as, not from the connection: a guest who leaves mid-match still stands in a picture.
            if (_driver != null && _driver.IsReplica)
            {
                return;
            }
```

with

```csharp
            // A guest writes its own save from its copy of itself, which the host keeps current (D61): at the host's
            // D52 moments, when its own chest closes, and at its own clean exits. Never before the host's first copy
            // has arrived — until then this machine holds nothing of the player's, and writing it would put an empty
            // stash over their real file. Read from what this scene was built as, not from the connection: a guest
            // who leaves mid-match still stands in a picture.
            if (_driver != null && _driver.IsReplica && !GuestCopyArrived())
            {
                return;
            }
```

(b) In `SaveNow`, replace

```csharp
            for (int i = 0; i < actors.Count; i++)
            {
                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag != null)
                {
                    _states.Add(new CharacterState(actors[i].Element, bag.Ledger, bag.Inventory));
                }
            }
```

with

```csharp
            for (int i = 0; i < actors.Count; i++)
            {
                // Each machine saves only its own players (planning decision 14): online, the partner's are saved on
                // their own machine, into their own file. On the couch both are this machine's.
                if (!_driver.Players.IsLocal(actors[i].PlayerId))
                {
                    continue;
                }

                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag != null)
                {
                    _states.Add(new CharacterState(actors[i].Element, bag.Ledger, bag.Inventory));
                }
            }
```

(c) After the method `SaveNow` add:

```csharp

        /// <summary>A guest's own player has had its copy from the host at least once.</summary>
        private bool GuestCopyArrived()
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag != null && bag.MirroredFromHost && _driver.Players.IsLocal(actors[i].PlayerId))
                {
                    return true;
                }
            }

            return false;
        }
```

The guest's `_stash` is the scene's `SharedStash`, which its own player's bag sits over — the guest's copy of its own sack and wallet — so `SaveMapper.Capture` writes the guest's inventory, its own `Progress` (with the chapter's credit once `OnChapterCompleted` ran; its resume point untouched, since the guest's runner never raises `CheckpointReached` or `StageCompleted`), and the heroes it did not play carried through from its `LoadedSave` exactly as on the couch.

In `Assets/_BattleBomb/UI/Chest/SettingsMenu.cs`, in Task 100's `LeaveTheGame`, replace

```csharp
            Close();
            Gameplay.Session.GameSession session = Gameplay.Session.GameSession.Find();
```

with

```csharp
            Close();

            // Leaving deliberately is a clean exit (D52): the guest's latest copy of itself is saved first.
            Gameplay.Session.SaveService saves = FindAnyObjectByType<Gameplay.Session.SaveService>();
            if (saves != null)
            {
                saves.SaveNow();
            }

            Gameplay.Session.GameSession session = Gameplay.Session.GameSession.Find();
```

- [ ] **Step 8: Run EditMode** — `recompile`, `console` `level: error` clean; full EditMode (820 + 5 = 825).

- [ ] **Step 9: The headless guest brings a hero**

In `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`:

(a) After `internal CommandButtons Held { get; set; }` add:

```csharp

        /// <summary>The hero this guest picks, by roster index, sent ready the moment it is welcomed.</summary>
        internal int Pick { get; set; }

        /// <summary>What it brings — its own save cut down to that hero. Null brings an empty one.</summary>
        internal SaveGame Bring { get; set; }

        /// <summary>False: welcomed, it sends no pick at all (a test that picks by hand).</summary>
        internal bool AutoPick { get; set; } = true;

        internal void SendPick(int rosterIndex, bool ready, SaveGame brought)
        {
            _writer.Reset();
            LobbyCodec.WritePick(_writer, new LobbyPick(rosterIndex, ready, brought ?? SaveGame.Fresh()));
            _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
        }
```

(b) In `Handle`, replace

```csharp
                    if (kind == NetMessageKind.Welcome)
                    {
                        IsWelcomed = true;
                    }
```

with

```csharp
                    if (kind == NetMessageKind.Welcome)
                    {
                        IsWelcomed = true;
                        if (AutoPick)
                        {
                            SendPick(Pick, true, Bring);
                        }
                    }
```

Add `using BattleBomb.Core.Saves;` to `HeadlessGuest.cs`. `_writer` is 256 bytes and grows.

- [ ] **Step 10: The host side, tested**

In `Assets/_BattleBomb/Tests/PlayMode/OnlineMenuSmokeTests.cs`:

(a) In the set-up, replace

```csharp
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.IsConnected && _guest.IsWelcomed, "the handshake never finished");
```

with

```csharp
            _guest = HeadlessGuest.Join(guestSide);
            _guest.Bring = GuestSave();
            yield return UntilFrames(() => net.IsConnected && _guest.IsWelcomed, "the handshake never finished");
            yield return UntilFrames(() => net.GuestBrought != null, "the guest's pick never reached the host");
```

(b) Before `private WorldInteractable FirstChest()` add:

```csharp
        [UnityTest]
        public IEnumerator The_guest_plays_from_the_bag_they_brought_and_the_hosts_is_their_own()
        {
            PlayerInventory guestBag = _guestBody.GetComponent<PlayerInventory>();
            PlayerInventory hostBag = _host.GetComponent<PlayerInventory>();

            Assert.That(guestBag.Stash, Is.Not.SameAs(hostBag.Stash), "Online the guest's sack is the host's (D61).");
            Assert.That(guestBag.Inventory.Items.Count, Is.EqualTo(2), "The guest's own knives never arrived.");
            Assert.That(guestBag.Wallet.Balance, Is.EqualTo(300));
            Assert.That(hostBag.Inventory.Items.Count, Is.Zero, "The guest's knives landed in the host's sack.");
            yield break;
        }

        [UnityTest]
        public IEnumerator The_host_saves_only_its_own_player()
        {
            SaveService saves = Object.FindAnyObjectByType<SaveService>();
            saves.SaveNow();
            GameSession session = GameSession.Find();
            Assert.That(session.Store.TryRead(session.SaveName, out string text), Is.True, "The host wrote nothing.");
            SaveGame written = SaveCodec.Decode(text).Save;

            Assert.That(written.Sack, Is.Empty, "The guest's knives went into the host's save.");
            Assert.That(written.Coins, Is.Zero, "The guest's coins went into the host's save.");

            // Both seats picked roster 0 here, so the heroes cannot be told apart by element: the count is the proof.
            Assert.That(written.Characters.Length, Is.EqualTo(1), "The guest's player was captured into the host's save.");
            yield break;
        }

        [UnityTest]
        public IEnumerator A_checkpoint_reaches_the_guest_as_a_moment_after_its_copy()
        {
            // Walking Player 2 into the first room in the set-up reached a checkpoint.
            int moment = IndexOf(0, m => (NetMessageKind)m[0] == NetMessageKind.Moment
                && SessionCodec.ReadMoment(Skip(m)) == MomentKind.CheckpointReached);
            Assert.That(moment, Is.GreaterThanOrEqualTo(0), "The guest was never told the run reached a checkpoint.");
            int copy = LastIndexOf(0, moment, m => IsParticipant(m, PlayerId.Two.Value, full: true));
            Assert.That(copy, Is.GreaterThanOrEqualTo(0), "The moment reached the guest before its latest copy of itself.");
            yield break;
        }

        private static NetReader Skip(byte[] message)
        {
            var reader = new NetReader(message);
            reader.ReadByte();
            return reader;
        }

        /// <summary>Two knives and three hundred coins, as the guest's own save would carry them.</summary>
        private static SaveGame GuestSave()
        {
            var inventory = new Inventory();
            for (int i = 0; i < 2; i++)
            {
                inventory.Add(new ItemInstance(
                    new ItemIdentity(KnifeDefinitionId, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                    new GearContribution(weaponDamage: 9f), new AffixRoll[0], requiredLevel: 1, new ItemInvestment(3)), 99);
            }

            return SaveMapper.Participant(inventory.Sack, new Wallet(300),
                new CharacterState(ElementId.None, XpLedger.Fresh, new Inventory()), withSack: true);
        }
```

Add `using BattleBomb.Core.Combat;`, `using BattleBomb.Core.Progression;` and `using BattleBomb.Core.Saves;` to `OnlineMenuSmokeTests.cs`. (Task 97's tests that `Take` a knife into the guest's bag still hold: the guest now has two knives from the start, and those tests count relative to what is there.)

The `SaveGame` brought with `ElementId.None` carries a character the guest's actual hero never matches, so the guest plays a fresh hero from its own sack and wallet — which is what `The_guest_plays_from_the_bag_they_brought…` checks.

- [ ] **Step 11: The guest side, tested**

In `Assets/_BattleBomb/Tests/PlayMode/GuestMenuSmokeTests.cs`:

(a) After the field `private SimulationDriver _driver;` add `private MemorySaveStore _store;`, and in the set-up replace `session.Store = new MemorySaveStore();` with:

```csharp
            _store = new MemorySaveStore();
            session.Store = _store;
```

(b) Before `private IEnumerator Join(` add:

```csharp
        [UnityTest]
        public IEnumerator When_the_host_says_so_the_guest_saves_its_own_copy_and_not_the_resume_point()
        {
            List<(int, byte[])> extra = ChestOpens();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.CheckpointReached);
            extra.Add((Start + 40, writer.ToArray()));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _store.Names().Count > 0, "The host's checkpoint never wrote the guest's save.");

            Assert.That(_store.TryRead("guest-menu", out string text), Is.True);
            SaveGame written = SaveCodec.Decode(text).Save;
            Assert.That(written.Coins, Is.EqualTo(100), "The guest's save is not its copy from the host.");
            Assert.That(written.Sack.Length, Is.EqualTo(2));
            Assert.That(written.Story.ResumeChapterId, Is.Empty, "The host's resume point was written into the guest's save.");
        }

        [UnityTest]
        public IEnumerator Before_its_copy_arrives_the_guest_writes_nothing()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.CheckpointReached);
            extra.Add((Start + 20, writer.ToArray()));
            yield return Join(Recording(extra));
            yield return AdvanceSteps(60);

            Assert.That(_store.Names(), Is.Empty,
                "The guest wrote a save before the host had sent it anything of its own — an empty stash over its file.");
        }

        [UnityTest]
        public IEnumerator The_chapters_credit_goes_into_the_guests_own_save()
        {
            List<(int, byte[])> extra = ChestOpens();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.ChapterCompleted);
            extra.Add((Start + 40, writer.ToArray()));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _store.Names().Count > 0, "The chapter's end never wrote the guest's save.");

            _store.TryRead("guest-menu", out string text);
            SaveGame written = SaveCodec.Decode(text).Save;
            Assert.That(SaveMapper.RestoreProgress(written).HighestTierBeaten("fixture"), Is.GreaterThan(0),
                "The chapter the guest finished with the host is not in the guest's own save (D61).");
        }
```

- [ ] **Step 12: Run both suites** — `recompile`; PlayMode (async) `OnlineMenuSmokeTests` (fourteen), `GuestMenuSmokeTests` (eleven), `OnlineHostSmokeTests` (every case — the headless guest now picks roster 0 and brings an empty save, and `The_stand_in_hero_never_enters_the_session_that_outlives_the_match` still holds: the guest's hero is chosen in the binder and never written into the session), `GuestReplicaSmokeTests.A_guest_who_leaves_mid_match_writes_no_save` (still nothing written: its recording never sends the guest a copy of itself). Then full EditMode (825) and full PlayMode (87 + 3 + 3 = 93). Delete `Assets/InitTestScene*`.

- [ ] **Step 13: Commit** — subject `101: two stashes and two saves — each player keeps what they earned`. Body: the guest brings their hero and their own save cut down to it (`LobbyPick`); on the host their sack and wallet live in a stash of their own and the couch keeps its one; Plan 1's stand-in hero retires; each machine saves only its own player — the guest from its copy, at the host's moments, its own chest closing and its own clean exits, never before its first copy arrives; the chapter's credit lands in the guest's save and the resume point stays the host's (D61); protocol 6.

---

## Stage D — Joining and leaving

### Task 102: The lobby at character select

D59's first join point. On the host, a connected guest sits in Player 2's slot at character select — their pick and readiness come from their own machine, the host's second pad cannot touch the slot, and the host cannot move on to launch until the guest is ready. On the guest, the front door becomes a lobby while connected: pick a hero from this machine's own save, ready, and wait — the host chooses the chapter and launches both, and nothing of the guest's own can launch while connected (Plan 1's "front door stays live" item). Each machine shows the other's state. A couch pair is full: a friend who asks is turned away with a reason — and the reason now survives (Plan 1's "refusal reason is lost" item). Task 101's automatic pick retires.

**Files:**
- Create: `Assets/_BattleBomb/Core/Chapters/GuestLobby.cs`
- Modify: `Assets/_BattleBomb/Core/Chapters/FrontendState.cs`, `Core/Net/LobbyCodec.cs`, `Core/Net/NetMessageKind.cs`, `Core/Net/NetProtocol.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`, `Gameplay/Net/NetHost.cs`
- Modify: `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs` (CRLF)
- Test: create `Tests/EditMode/FrontendRemoteSlotTests.cs`, `Tests/EditMode/GuestLobbyTests.cs`, `Tests/PlayMode/OnlineJoinSmokeTests.cs`; modify `Tests/EditMode/Net/LobbyCodecTests.cs`, `Tests/PlayMode/HeadlessGuest.cs`, `Tests/PlayMode/OnlineHostSmokeTests.cs`, `Tests/PlayMode/ReplicaReplaySmokeTests.cs`, `Tests/PlayMode/OnlineMenuSmokeTests.cs`

- [ ] **Step 1: Write the failing Core tests**

`Assets/_BattleBomb/Tests/EditMode/FrontendRemoteSlotTests.cs`:

```csharp
using BattleBomb.Core.Chapters;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>An online guest in Player 2's slot at character select (D59): their pick and readiness come from their
    /// own machine, this machine's pads never drive the slot, and nobody launches until they are ready.</summary>
    public sealed class FrontendRemoteSlotTests
    {
        private static FrontendState AtCharacters()
        {
            var state = new FrontendState(3, canContinue: false);
            state.Confirm(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Characters));
            return state;
        }

        [Test]
        public void An_online_guest_takes_slot_two_and_this_machines_pads_cannot_drive_it()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 2, false);

            Assert.That(state.IsJoined(1), Is.True);
            Assert.That(state.IsRemote(1), Is.True);
            Assert.That(state.PickOf(1), Is.EqualTo(2));

            state.MovePick(1, 1);
            state.Confirm(1);
            state.Back(1);
            Assert.That(state.PickOf(1), Is.EqualTo(2), "A local pad moved the online guest's pick.");
            Assert.That(state.IsReady(1), Is.False, "A local pad readied the online guest.");
            Assert.That(state.IsJoined(1), Is.True, "A local pad unseated the online guest.");
        }

        [Test]
        public void The_host_moves_on_only_when_the_guest_is_ready()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 0, false);
            state.Confirm(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Characters), "The host moved on without its guest.");

            state.SetRemote(1, true, 0, true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters));
        }

        [Test]
        public void A_launch_waits_for_a_guest_who_unreadied()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 0, true);
            state.Confirm(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters));

            state.SetRemote(1, true, 0, false);
            state.Launch(true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters), "The host launched without its guest.");

            state.SetRemote(1, true, 0, true);
            state.Launch(true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Launching));
        }

        [Test]
        public void A_guest_leaving_frees_the_slot_but_never_unseats_a_couch_player()
        {
            FrontendState online = AtCharacters();
            online.SetRemote(1, true, 1, true);
            online.SetRemote(1, false, 0, false);
            Assert.That(online.IsJoined(1), Is.False);
            Assert.That(online.IsRemote(1), Is.False);

            FrontendState couch = AtCharacters();
            couch.Confirm(1);
            couch.SetRemote(1, false, 0, false);
            Assert.That(couch.IsJoined(1), Is.True, "Nobody online left, and the couch's Player 2 was unseated.");
        }

        [Test]
        public void Going_back_to_the_title_keeps_an_online_guest_seated()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 0, false);
            state.Back(0);

            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Title));
            Assert.That(state.IsJoined(1), Is.True, "The title forgot a guest who is still connected.");
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/GuestLobbyTests.cs`:

```csharp
using BattleBomb.Core.Chapters;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>The guest's front door while connected (D59): pick, ready, un-ready, leave.</summary>
    public sealed class GuestLobbyTests
    {
        [Test]
        public void The_pick_wraps_and_holds_still_once_ready()
        {
            var lobby = new GuestLobby(3, 0, false);
            lobby.MovePick(-1);
            Assert.That(lobby.Pick, Is.EqualTo(2));

            lobby.Confirm();
            lobby.MovePick(1);
            Assert.That(lobby.Ready, Is.True);
            Assert.That(lobby.Pick, Is.EqualTo(2), "A ready guest's pick moved.");
        }

        [Test]
        public void Back_unreadies_and_then_leaves()
        {
            var lobby = new GuestLobby(3, 1, true);
            lobby.Back();
            Assert.That(lobby.Ready, Is.False);
            Assert.That(lobby.LeaveRequested, Is.False);

            lobby.Back();
            Assert.That(lobby.LeaveRequested, Is.True);
        }

        [Test]
        public void It_comes_back_as_it_was_left()
        {
            var lobby = new GuestLobby(3, 7, true);
            Assert.That(lobby.Pick, Is.EqualTo(1), "A remembered pick past the roster wraps into it.");
            Assert.That(lobby.Ready, Is.True);
        }
    }
}
```

In `Assets/_BattleBomb/Tests/EditMode/Net/LobbyCodecTests.cs`, before the closing brace of the class add:

```csharp

        [Test]
        public void The_hosts_lobby_travels_whole()
        {
            var writer = new NetWriter();
            LobbyCodec.WriteLobby(writer, new LobbyState(Core.Chapters.FrontendScreen.Chapters, 2, true, false));
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.LobbyState));
            LobbyState back = LobbyCodec.ReadLobby(reader);

            Assert.That(back.HostScreen, Is.EqualTo(Core.Chapters.FrontendScreen.Chapters));
            Assert.That(back.HostPick, Is.EqualTo(2));
            Assert.That(back.HostReady, Is.True);
            Assert.That(back.InMatch, Is.False);
            Assert.That(reader.Remaining, Is.Zero);
        }
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors (`SetRemote`, `IsRemote`, `GuestLobby`, `LobbyState`, `WriteLobby`).

- [ ] **Step 3: The remote slot, the guest's lobby, and the host's lobby on the wire**

In `Assets/_BattleBomb/Core/Chapters/FrontendState.cs`:

(a) After the field `private readonly int[] _pick = new int[Slots];` add:

```csharp
        private readonly bool[] _remote = new bool[Slots];
```

(b) After the method `PickOf` add:

```csharp

        /// <summary>This slot is an online guest's (D59): driven from their machine, never by a pad here.</summary>
        public bool IsRemote(int slot) => slot >= 0 && slot < Slots && _remote[slot];

        /// <summary>
        /// Seats an online guest in <paramref name="slot"/> with the pick and readiness their own machine sent, or
        /// unseats one (D59). Unseating clears only a remote slot: a couch Player 2 is never unseated by it. A guest
        /// readying while the host already is moves both on to the chapters, as a couch Player 2 readying would.
        /// Slot 0 is always this machine's own.
        /// </summary>
        public void SetRemote(int slot, bool present, int pick, bool ready)
        {
            if (slot <= 0 || slot >= Slots)
            {
                return;
            }

            if (!present)
            {
                if (_remote[slot])
                {
                    _remote[slot] = false;
                    _joined[slot] = false;
                    _ready[slot] = false;
                }

                return;
            }

            _remote[slot] = true;
            _joined[slot] = true;
            _pick[slot] = ((pick % _rosterCount) + _rosterCount) % _rosterCount;
            _ready[slot] = ready;
            if (Screen == FrontendScreen.Characters && EveryonePresentIsReady())
            {
                Screen = FrontendScreen.Chapters;
            }
        }
```

(c) In `MovePick`, replace

```csharp
            if (Screen != FrontendScreen.Characters || !IsJoined(slot) || IsReady(slot))
```

with

```csharp
            if (Screen != FrontendScreen.Characters || !IsJoined(slot) || IsReady(slot) || IsRemote(slot))
```

(d) At the top of `Confirm`, before `switch (Screen)`, add:

```csharp
            if (IsRemote(slot))
            {
                return;
            }

```

(e) In `Back`, in the `Characters` case, replace

```csharp
                    if (slot < 0 || slot >= Slots || !_joined[slot])
                    {
                        return;
                    }
```

with

```csharp
                    if (slot < 0 || slot >= Slots || !_joined[slot] || _remote[slot])
                    {
                        return;
                    }
```

and replace

```csharp
                        // The title forgets both seats (D57), so the couch re-forms from scratch.
                        _joined[1] = false;
                        _ready[1] = false;
                        Screen = FrontendScreen.Title;
```

with

```csharp
                        // The title forgets both seats (D57), so the couch re-forms from scratch — but a guest who is
                        // still connected keeps theirs: they are not on this couch to forget.
                        if (!_remote[1])
                        {
                            _joined[1] = false;
                            _ready[1] = false;
                        }

                        Screen = FrontendScreen.Title;
```

(f) Replace the method `Launch` with:

```csharp
        public void Launch(bool canLaunch)
        {
            // Everyone present, not only the couch: an online guest who un-readied at chapter select is still here.
            if (Screen == FrontendScreen.Chapters && canLaunch && EveryonePresentIsReady())
            {
                Screen = FrontendScreen.Launching;
            }
        }
```

`Assets/_BattleBomb/Core/Chapters/GuestLobby.cs`:

```csharp
namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// The guest's front door while connected (D59): this machine's one player picks a hero from their own save and
    /// readies; the host chooses the chapter and launches both. Back un-readies, and from not-ready leaves the game.
    /// Built again from what it was when the front door comes back after a match.
    /// </summary>
    public sealed class GuestLobby
    {
        private readonly int _rosterCount;

        public GuestLobby(int rosterCount, int pick, bool ready)
        {
            _rosterCount = rosterCount < 1 ? 1 : rosterCount;
            Pick = Wrap(pick);
            Ready = ready;
        }

        public int Pick { get; private set; }

        public bool Ready { get; private set; }

        /// <summary>Back from not-ready: the player wants out of the host's game.</summary>
        public bool LeaveRequested { get; private set; }

        public void MovePick(int delta)
        {
            if (Ready || delta == 0)
            {
                return;
            }

            Pick = Wrap(Pick + delta);
        }

        public void Confirm() => Ready = true;

        public void Back()
        {
            if (Ready)
            {
                Ready = false;
            }
            else
            {
                LeaveRequested = true;
            }
        }

        private int Wrap(int pick) => ((pick % _rosterCount) + _rosterCount) % _rosterCount;
    }
}
```

In `Assets/_BattleBomb/Core/Net/LobbyCodec.cs`:

(a) After the struct `LobbyPick` add:

```csharp

    /// <summary>What the guest's lobby shows of the host (D59): where the host's front door is, the host's hero and
    /// readiness, and whether the host is already playing — a guest who joins then waits for a checkpoint room.</summary>
    public readonly struct LobbyState
    {
        public readonly Chapters.FrontendScreen HostScreen;
        public readonly int HostPick;
        public readonly bool HostReady;
        public readonly bool InMatch;

        public LobbyState(Chapters.FrontendScreen hostScreen, int hostPick, bool hostReady, bool inMatch)
        {
            HostScreen = hostScreen;
            HostPick = hostPick;
            HostReady = hostReady;
            InMatch = inMatch;
        }

        public bool Same(in LobbyState other) =>
            HostScreen == other.HostScreen && HostPick == other.HostPick
            && HostReady == other.HostReady && InMatch == other.InMatch;
    }
```

(b) Before the closing brace of `LobbyCodec` add:

```csharp

        public static void WriteLobby(NetWriter w, in LobbyState state)
        {
            w.WriteByte((byte)NetMessageKind.LobbyState);
            w.WriteByte((byte)state.HostScreen);
            w.WriteInt(state.HostPick);
            w.WriteBool(state.HostReady);
            w.WriteBool(state.InMatch);
        }

        public static LobbyState ReadLobby(NetReader r)
        {
            var screen = (Chapters.FrontendScreen)r.ReadByte();
            if (screen > Chapters.FrontendScreen.Launching)
            {
                throw new NetFormatException($"A front-door screen of {(byte)screen}.");
            }

            return new LobbyState(screen, r.ReadInt(), r.ReadBool(), r.ReadBool());
        }
```

In `Assets/_BattleBomb/Core/Net/NetMessageKind.cs`, replace

```csharp
        LobbyPick = 18,
    }
```

with

```csharp
        LobbyPick = 18,
        LobbyState = 19,
    }
```

In `Assets/_BattleBomb/Core/Net/NetProtocol.cs`:

(a) Replace `public const int Version = 6;` with `public const int Version = 7;` and add `7: the host's lobby (Task 102).` to its summary.

(b) After `public const float DropAfterSeconds = 10f;` add:

```csharp

        /// <summary>How long a refused guest has to read the reason and close before the host drops them. The guest
        /// closing is what keeps the reason on both screens; dropping at once raced it (Plan 1's lost refusal).</summary>
        public const float RefusalGraceSeconds = 2f;
```

- [ ] **Step 4: Run the Core tests** — `recompile`; `run_tests` EditMode `FrontendRemoteSlotTests` (five), `GuestLobbyTests` (three), `LobbyCodecTests` (six), `FrontendStateTests` (unchanged, all pass — the couch's rules did not move).

- [ ] **Step 5: The session's lobby**

In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

(a) After the field `private CharacterDefinition[] _couch;` add:

```csharp

        /// <summary>Where this machine's own front door stood before the host's launch filled the session — put back
        /// with the couch, so a guest who leaves lands where they came from, not at the host's chapter.</summary>
        private ChapterDefinition _couchChapter;
        private int _couchStage;
        private int _couchTier;
        private int _couchResume = -1;

        private double _refusedAt = -1.0;
        private bool _lobbyPublished;
        private LobbyState _lastLobby;
```

(b) After Task 101's `public event Action GuestLobbyChanged;` add:

```csharp

        /// <summary>Host: this game takes no guest — a couch pair is playing, or about to (D59). A guest who asks is
        /// turned away with the reason on both screens.</summary>
        public bool IsFull { get; private set; }

        public void SetFull(bool full) => IsFull = full;

        /// <summary>Guest: the host's front door as it last said (D59), for the lobby to show.</summary>
        public LobbyState HostLobby { get; private set; }

        /// <summary>Guest: this machine's lobby pick and readiness, kept across the host's matches so the lobby comes
        /// back as it was left. -1 before a pick.</summary>
        public int LobbyPick { get; set; } = -1;

        public bool LobbyReady { get; set; }

        /// <summary>Host: tells the guest where this machine's front door is, when it changed.</summary>
        public void PublishLobby(in LobbyState state)
        {
            if (Role != NetRole.Host || !IsConnected || (_lobbyPublished && _lastLobby.Same(state)))
            {
                return;
            }

            _lastLobby = state;
            _lobbyPublished = true;
            _writer.Reset();
            LobbyCodec.WriteLobby(_writer, state);
            Send(NetChannel.Reliable, _writer);
        }
```

(c) In `Update`, replace

```csharp
            if (Now - _lastReceived > NetProtocol.DropAfterSeconds)
```

with

```csharp
            if (_refusedAt >= 0.0 && Now - _refusedAt > NetProtocol.RefusalGraceSeconds)
            {
                // The refused guest had its reason and did not close: drop it now.
                _refusedAt = -1.0;
                _transport.Disconnect(Peer);
                return;
            }

            if (Now - _lastReceived > NetProtocol.DropAfterSeconds)
```

(d) In `Dispatch`, before `case NetMessageKind.KeepAlive:` add:

```csharp
                    case NetMessageKind.LobbyState when Role == NetRole.Guest && _welcomed:
                        HostLobby = LobbyCodec.ReadLobby(reader);
                        return;

```

(e) In the `Welcome` case, remove Task 101's stand-in pick — replace

```csharp
                        PeerJoined?.Invoke();

                        // Until the lobby's screens (Task 102), the guest's machine picks for its player: the hero
                        // they last sat as on their own couch, ready at once.
                        SendLobbyPick(StandInPick(), true);
                        return;
```

with

```csharp
                        PeerJoined?.Invoke();
                        return;
```

and delete the method `StandInPick` (with its summary).

(f) Replace the method `Greet` with:

```csharp
        private void Greet(in HelloMessage hello)
        {
            string refusal = HandshakeCodec.CheckHello(hello, Application.version)
                ?? (IsFull ? "The game is full." : null);
            if (refusal != null)
            {
                _writer.Reset();
                HandshakeCodec.WriteRefuse(_writer, refusal);
                Send(NetChannel.Reliable, _writer);
                Status = $"Refused a guest: {refusal}";

                // Not dropped here: the guest closes once it has read the reason, and a drop racing the message was how
                // a lagged guest came to read "The host left" instead. The grace ends it if the guest never does.
                _refusedAt = Now;
                return;
            }

            _welcomed = true;
            _lobbyPublished = false;
            _writer.Reset();
            HandshakeCodec.WriteWelcome(_writer, new WelcomeMessage(GuestPlayerId.Value));
            Send(NetChannel.Reliable, _writer);
            Status = "A guest joined";
            PeerJoined?.Invoke();
        }
```

(g) In `FollowLaunch`, replace

```csharp
            if (_couch == null)
            {
                _couch = (CharacterDefinition[])session.Characters.Clone();
            }
```

with

```csharp
            if (_couch == null)
            {
                _couch = (CharacterDefinition[])session.Characters.Clone();
                _couchChapter = session.Chapter;
                _couchStage = session.StageIndex;
                _couchTier = session.TierIndex;
                _couchResume = session.ResumeCheckpointArena;
            }
```

(h) In `RestoreCouch`, replace

```csharp
            Array.Copy(_couch, session.Characters, Math.Min(_couch.Length, session.Characters.Length));
            _couch = null;
```

with

```csharp
            Array.Copy(_couch, session.Characters, Math.Min(_couch.Length, session.Characters.Length));
            session.Chapter = _couchChapter;
            session.StageIndex = _couchStage;
            session.TierIndex = _couchTier;
            session.ResumeCheckpointArena = _couchResume;
            _couch = null;
            _couchChapter = null;
```

(i) In `Lost`, replace Task 101's

```csharp
            ForgetGuest();
            Status = $"Hosting — the guest {why}; waiting for another";
```

with

```csharp
            ForgetGuest();
            _refusedAt = -1.0;
            _lobbyPublished = false;

            // A guest who was never welcomed — refused, or gone before the handshake — changes nothing the panel said.
            if (wasJoined)
            {
                Status = $"Hosting — the guest {why}; waiting for another";
            }
```

(j) In `Close`, after Task 101's `ForgetGuest();` add:

```csharp
            _refusedAt = -1.0;
            _lobbyPublished = false;
            HostLobby = default;
            LobbyPick = -1;
            LobbyReady = false;
```

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`, at the end of `Begin` (after `SendLaunch();`) add:

```csharp

            // The guest's lobby shows a host at play — which is what a guest who joins mid-run sees too (Task 103).
            _net.PublishLobby(new LobbyState(FrontendScreen.Launching, IndexInRoster(_session.Characters[0]), true, true));
```

Add `using BattleBomb.Core.Chapters;` to `NetHost.cs`.

- [ ] **Step 6: The front door, on both machines**

In `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs` (CRLF):

(a) Add `using BattleBomb.Core.Net;` and `using BattleBomb.Gameplay.Net;` to the usings.

(b) After the field `private bool _launched;` add:

```csharp

        /// <summary>This machine is a guest: its front door is a lobby (D59), null otherwise.</summary>
        private GuestLobby _lobby;
        private bool _sentAny;
        private int _sentPick;
        private bool _sentReady;
```

(c) After `public StageSelection Selection => _selection;` add:

```csharp

        /// <summary>The lobby while this machine is a guest, or null — read by the smoke suite.</summary>
        public GuestLobby Lobby => _lobby;
```

(d) In `Update`, replace

```csharp
            if (_input == null || _state == null || _launched)
            {
                return;
            }

            for (int slot = 0; slot < FrontendState.Slots; slot++)
            {
                PlayerCommand command = _input.CommandFor(slot);
```

with

```csharp
            if (_input == null || _state == null || _launched)
            {
                return;
            }

            NetSession net = _session.Net;
            if (net != null && net.Role == NetRole.Guest)
            {
                UpdateAsGuest(net);
                return;
            }

            _lobby = null;
            SyncRemote(net);

            for (int slot = 0; slot < FrontendState.Slots; slot++)
            {
                if (_state.IsRemote(slot))
                {
                    // The online guest's seat is driven from their own machine (D59).
                    continue;
                }

                PlayerCommand command = _input.CommandFor(slot);
```

and replace

```csharp
            _session.Seats.Follow(
                _state.Screen,
                _state.IsJoined(1),
```

with

```csharp
            _session.Seats.Follow(
                _state.Screen,
                _state.IsJoined(1) && !_state.IsRemote(1),
```

(e) After the method `Update` add:

```csharp

        /// <summary>
        /// The host's side of the lobby (D59): a connected guest sits in Player 2's slot with their own pick; a couch
        /// Player 2 makes the game full; and the guest is told where this front door is.
        /// </summary>
        private void SyncRemote(NetSession net)
        {
            bool guest = net != null && net.Role == NetRole.Host && net.IsConnected;
            _state.SetRemote(1, guest, guest ? Mathf.Max(0, net.GuestPick) : 0, guest && net.GuestReady);
            if (net == null || net.Role != NetRole.Host)
            {
                return;
            }

            net.SetFull(!guest && _state.IsJoined(1));
            net.PublishLobby(new LobbyState(_state.Screen, _state.PickOf(0), _state.IsReady(0), false));
        }

        /// <summary>
        /// The front door while this machine is a guest (D59): pick a hero from this machine's own save, ready, and
        /// wait for the host, who chooses the chapter and launches both. Nothing of this machine's own can launch while
        /// connected. Back un-readies, then leaves the game.
        /// </summary>
        private void UpdateAsGuest(NetSession net)
        {
            if (_lobby == null)
            {
                int start = net.LobbyPick >= 0 ? net.LobbyPick : IndexInRoster(_session.Characters[0]);
                _lobby = new GuestLobby(_roster.Length, start, net.LobbyReady);
                _sentAny = false;
            }

            PlayerCommand command = _input.CommandFor(0);
            MenuPress press = MenuPress.From(command);
            Vector2 move = command.Move;
            bool freshX = Mathf.Abs(move.x) > 0.5f && Mathf.Abs(_lastMove[0].x) <= 0.5f;
            _lastMove[0] = move;
            if (freshX)
            {
                _lobby.MovePick(move.x > 0f ? 1 : -1);
            }

            if (press.Confirm)
            {
                _lobby.Confirm();
            }
            else if (press.Back)
            {
                _lobby.Back();
            }

            if (_lobby.LeaveRequested)
            {
                net.Leave();
                _lobby = null;
                Repaint();
                return;
            }

            net.LobbyPick = _lobby.Pick;
            net.LobbyReady = _lobby.Ready;
            if (net.IsConnected && (!_sentAny || _lobby.Pick != _sentPick || _lobby.Ready != _sentReady))
            {
                // Ready, it brings this machine's own save cut down to the hero (D61), re-read whenever this front
                // door comes up — so after a match it brings what the last autosave wrote.
                net.SendLobbyPick(_lobby.Pick, _lobby.Ready);
                _sentAny = true;
                _sentPick = _lobby.Pick;
                _sentReady = _lobby.Ready;
            }

            Repaint();
        }
```

(f) In `LaunchNow`, replace

```csharp
                _session.Characters[i] = _state.IsJoined(i) && _roster.Length > 0
```

with

```csharp
                // An online guest's hero is theirs, and never enters this machine's session (the binder seats it).
                _session.Characters[i] = _state.IsJoined(i) && !_state.IsRemote(i) && _roster.Length > 0
```

(g) In `Build`, replace

```csharp
            _primary = MakeButton(panel, "Primary", 0.55f, 0.02f, 0.95f, 0.1f, () => Confirm(0));
            _back = MakeButton(panel, "Back", 0.05f, 0.02f, 0.45f, 0.1f, () => _state.Back(0));
```

with

```csharp
            _primary = MakeButton(panel, "Primary", 0.55f, 0.02f, 0.95f, 0.1f, () =>
            {
                if (_lobby != null)
                {
                    _lobby.Confirm();
                }
                else
                {
                    Confirm(0);
                }
            });
            _back = MakeButton(panel, "Back", 0.05f, 0.02f, 0.45f, 0.1f, () =>
            {
                if (_lobby != null)
                {
                    _lobby.Back();
                }
                else
                {
                    _state.Back(0);
                }
            });
```

(h) In `Repaint`, replace

```csharp
            _text.Clear();
            switch (_state.Screen)
            {
                case FrontendScreen.Title:
```

with

```csharp
            _text.Clear();
            if (_lobby != null)
            {
                RepaintLobby();
                return;
            }

            switch (_state.Screen)
            {
                case FrontendScreen.Title:
```

(i) In `Repaint`, in the `Characters` case, replace

```csharp
                        _text.Append("Player ").Append(slot + 1).Append(":  ");
                        if (!_state.IsJoined(slot))
```

with

```csharp
                        _text.Append("Player ").Append(slot + 1).Append(_state.IsRemote(slot) ? " (online):  " : ":  ");
                        if (_state.IsRemote(slot))
                        {
                            string guestHero = _roster.Length > 0 && _roster[_state.PickOf(slot)] != null
                                ? _roster[_state.PickOf(slot)].DisplayName
                                : "?";
                            _text.Append(UiBuild.Tint(guestHero, UiBuild.Focus))
                                .Append(_state.IsReady(slot) ? "   READY" : "   choosing")
                                .Append('\n');
                            continue;
                        }

                        if (!_state.IsJoined(slot))
```

(j) In `Repaint`, in the `Chapters` case, replace

```csharp
                    if (_selection.IsResuming)
                    {
                        _text.Append("\n\nContinue from stage ").Append(_selection.LaunchStageIndex + 1);
                    }
```

with

```csharp
                    if (_selection.IsResuming)
                    {
                        _text.Append("\n\nContinue from stage ").Append(_selection.LaunchStageIndex + 1);
                    }

                    if (_state.IsRemote(1) && !_state.IsReady(1))
                    {
                        _text.Append("\n\n").Append(UiBuild.Tint("Waiting for Player 2 to be ready.", UiBuild.InkDim));
                    }
```

(k) After the method `Repaint` add:

```csharp

        private void RepaintLobby()
        {
            NetSession net = _session.Net;
            _text.Append("ONLINE — PLAYER 2\n\n");
            if (net == null || !net.IsConnected)
            {
                _text.Append(net != null ? net.Status : "Offline");
            }
            else
            {
                string hero = _roster.Length > 0 && _roster[_lobby.Pick] != null ? _roster[_lobby.Pick].DisplayName : "?";
                _text.Append("Your hero:  <  ").Append(UiBuild.Tint(hero, UiBuild.Focus)).Append("  >")
                    .Append(_lobby.Ready ? "   READY" : $"   ({PromptRow.Inline(FamilyOf(0), PromptKey.Confirm)}: ready)")
                    .Append("\n\n").Append(HostLine(net.HostLobby));
            }

            _body.text = _text.ToString();
            SetButtonLabel(_primary, _lobby.Ready ? string.Empty : "Ready");
            SetButtonLabel(_back, _lobby.Ready ? "Not ready" : "Leave");

            _prompts.Clear();
            if (!_lobby.Ready)
            {
                _prompts.Add(new Prompt(PromptKey.Move, "Pick"));
                _prompts.Add(new Prompt(PromptKey.Confirm, "Ready"));
            }

            _prompts.Add(new Prompt(PromptKey.Back, _lobby.Ready ? "Not ready" : "Leave"));
            _promptRow.Show(_prompts, FamilyOf(0));
        }

        private string HostLine(in LobbyState host)
        {
            if (host.InMatch)
            {
                return "The host is playing. You will join at the next checkpoint room.";
            }

            switch (host.HostScreen)
            {
                case FrontendScreen.Chapters:
                    return "The host is choosing a chapter.";
                case FrontendScreen.Launching:
                    return "Starting…";
                default:
                    return "The host is choosing a hero.";
            }
        }
```

- [ ] **Step 7: The headless guest hears the lobby and a refusal**

In `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`:

(a) After Task 101's `internal bool AutoPick { get; set; } = true;` add:

```csharp

        /// <summary>The host's front door as it last said, or null before it has.</summary>
        internal LobbyState? HostLobby { get; private set; }

        /// <summary>Why the host turned this guest away, or null.</summary>
        internal string Refusal { get; private set; }
```

(b) In `Handle`, replace

```csharp
                    else if (kind == NetMessageKind.Launch)
```

with

```csharp
                    else if (kind == NetMessageKind.LobbyState)
                    {
                        HostLobby = LobbyCodec.ReadLobby(reader);
                    }
                    else if (kind == NetMessageKind.Refuse)
                    {
                        // A real guest closes once it has read the reason (Task 102).
                        Refusal = HandshakeCodec.ReadRefuse(reader);
                        _transport.Disconnect(_host);
                    }
                    else if (kind == NetMessageKind.Launch)
```

- [ ] **Step 8: Host fixtures wait for the guest to be ready**

With `FrontendState.Launch` requiring everyone ready, each hosted fixture waits for its headless guest's pick before readying the host. In each of the four places below, **after** the handshake wait and **before** the first `flow.State.Confirm(0)`, the lines become:

`Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`, in `HostWithAGuest`, replace

```csharp
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
```

with

```csharp
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.State.IsReady(1), "the guest never readied in the lobby");
            flow.State.Confirm(0);
```

`Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`, in `OnlineLaunchHoldSmokeTests.The_host_holds_its_first_step_until_the_guest_has_loaded`, replace

```csharp
                FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
                flow.State.Confirm(0);
```

with

```csharp
                FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
                for (int i = 0; i < 300 && !flow.State.IsReady(1); i++)
                {
                    yield return null;
                }

                flow.State.Confirm(0);
```

`Assets/_BattleBomb/Tests/PlayMode/ReplicaReplaySmokeTests.cs`, in the recording set-up, replace

```csharp
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
```

with

```csharp
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            for (int i = 0; i < 300 && !flow.State.IsReady(1); i++)
            {
                yield return null;
            }

            flow.State.Confirm(0);
```

`Assets/_BattleBomb/Tests/PlayMode/OnlineMenuSmokeTests.cs`, in `HostWithAGuestAtTheChest`, replace

```csharp
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
```

with

```csharp
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.State.IsReady(1), "the guest never readied in the lobby");
            flow.State.Confirm(0);
```

- [ ] **Step 9: The lobby, tested**

`Assets/_BattleBomb/Tests/PlayMode/OnlineJoinSmokeTests.cs` — the lobby, drop-in (Task 103) and leaving (Task 104), each test building its own host and guest:

```csharp
using System.Collections;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D59's joins and D61's leaving (HANDOFF-M8 Stage D): the lobby at character select from both sides, a couch
    /// pair turning a guest away with a reason, and — from Tasks 103 and 104 — dropping in at a checkpoint room and
    /// leaving. Host sides use the headless guest over the loopback; guest sides a recording.
    /// </summary>
    public sealed class OnlineJoinSmokeTests
    {
        private const int LoadFrameCeiling = 1500;

        private HeadlessGuest _guest;

        [UnitySetUp]
        public IEnumerator OpenTheFrontDoor()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-join";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            DisableDevices();
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            if (_guest != null)
            {
                Object.Destroy(_guest.gameObject);
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator The_host_launches_only_once_its_guest_is_ready_and_the_guest_plays_their_own_pick()
        {
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            _guest = HeadlessGuest.Join(guestSide);
            _guest.AutoPick = false;
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.State.IsRemote(1), "the guest never took Player 2's slot");

            flow.State.Confirm(0);
            flow.State.Confirm(0);
            yield return null;
            yield return null;
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Characters), "The host moved on without its guest.");
            yield return UntilFrames(() => _guest.HostLobby.HasValue && _guest.HostLobby.Value.HostReady,
                "the guest never heard the host was ready");

            _guest.SendPick(0, true, null);
            yield return UntilFrames(() => flow.State.Screen == FrontendScreen.Chapters, "the guest's ready never moved the host on");
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");

            Assert.That(_guest.Launch.HasValue, Is.True);
            Assert.That(_guest.Launch.Value.RosterPicks[1], Is.EqualTo(0), "The launch did not seat the guest's own pick.");
            Assert.That(GameSession.Find().Characters[1], Is.Null, "The guest's hero entered the host's session.");
            Assert.That(net.GuestReady, Is.True);
        }

        [UnityTest]
        public IEnumerator A_couch_pair_turns_a_guest_away_and_both_sides_keep_the_reason()
        {
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(1);
            Assert.That(flow.State.IsJoined(1), Is.True);

            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return null;
            yield return null;
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => _guest.Refusal != null, "the full game never turned the guest away");

            Assert.That(_guest.Refusal, Does.Contain("full"));
            yield return UntilFrames(() => net.Peer.IsNone, "the refused guest's connection never closed");
            Assert.That(net.Status, Does.Contain("Refused"), "The host's reason was overwritten when the guest closed.");
            Assert.That(net.IsConnected, Is.False);
        }

        [UnityTest]
        public IEnumerator The_guests_front_door_is_a_lobby_that_sends_its_pick_and_can_leave()
        {
            var playback = new PlaybackTransport(new System.Collections.Generic.List<(int Frame, byte[] Payload)>());
            NetSession net = NetSession.FindOrCreate();
            net.Join(playback, "playback");
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.Lobby != null && net.IsConnected, "the guest's front door never became a lobby");

            flow.Lobby.Confirm();
            yield return null;
            yield return null;
            LobbyPick sent = default;
            bool found = false;
            foreach (byte[] message in playback.Sent)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.LobbyPick)
                {
                    sent = LobbyCodec.ReadPick(reader);
                    found = true;
                }
            }

            Assert.That(found, Is.True, "The guest's lobby sent the host nothing.");
            Assert.That(sent.Ready, Is.True);
            Assert.That(sent.Brought, Is.Not.Null, "A ready guest brought nothing of its own save.");
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Title), "The guest's own front door moved while it was a guest.");

            flow.Lobby.Back();
            flow.Lobby.Back();
            yield return UntilFrames(() => net.Role == NetRole.Offline, "Back from not-ready never left the game");
            yield return null;
            Assert.That(flow.Lobby, Is.Null, "The lobby outlived the connection.");
        }

        private static NetSession HostOverLoopback(out LoopbackTransport guestSide)
        {
            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            return net;
        }

        private static void DisableDevices()
        {
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }
        }

        private static IEnumerator UntilFrames(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < LoadFrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {LoadFrameCeiling} frames).");
        }
    }
}
```

The front door's devices are disabled in the set-up, so `FrontendFlow`'s own reading of pads does nothing and every step is the test's.

- [ ] **Step 10: Run the suites** — `recompile`, `console` `level: error` clean. Full EditMode (825 + 5 + 3 + 1 = 834). PlayMode (async): `OnlineJoinSmokeTests` (three), `OnlineHostSmokeTests` (all), `OnlineMenuSmokeTests` (all), `ReplicaReplaySmokeTests` (the replay still compares clean — the recording now also carries lobby messages the guest stores and ignores), `SeatJoinSmokeTests` and `ChapterLoopSmokeTests` (the couch front door unchanged). Then the full PlayMode suite (93 + 3 = 96). Delete `Assets/InitTestScene*`.

- [ ] **Step 11: Commit** — subject `102: the lobby at character select`. Body: a connected guest sits in Player 2's slot with the pick and readiness their own machine sends, the host's pads never drive it, and the host launches only once they are ready (D59); the guest's front door is a lobby while connected — pick from your own save, ready, wait for the host — so nothing of the guest's own can launch; each side shows the other; a couch pair is full, and a refused guest keeps the reason on both screens; a guest who leaves lands where they came from; protocol 7.

---

### Task 103: Drop-in at checkpoint rooms

D59's second join point, as Michael approved it (design §1): a friend who readies while the host is mid-run waits — "The host is playing. You will join at the next checkpoint room." — until the host's run is standing in a checkpoint room; then the friend's machine is sent the run and loads it, and **only once it has finished loading, and the run is still in a checkpoint room**, the friend stands up at that room's respawn point. If the host walked on while the friend loaded, the friend's machine follows the host's stages, showing the host's world with the friend's own body not yet in it, and the friend appears at the next room.

Four pieces. The host keeps its half (`NetHost`) whenever it is hosting at all — open to a friend, not only with one already connected — and binds the second player object late (`SessionBinder.BindLate`, planning decision 17), after the step's phases or on a paused frame. The late guest is told everything a snapshot never carries — every drop on the ground, every open screen and rack, both inventories — before the first snapshot with them in it (the board's Task 103 item). Only a guest who is actually in the world holds the airlock shut, which ends the "rejoin deadlocks the airlock" item. And a stage asked for twice on the guest loads once (the board's stale-load item).

**Files:**
- Modify: `Assets/_BattleBomb/Core/Net/StageCodec.cs`, `Core/Net/HandshakeCodec.cs`, `Core/Net/NetProtocol.cs`
- Modify: `Assets/_BattleBomb/Gameplay/World/StageRunner.cs` (CRLF), `Gameplay/Session/SessionBinder.cs` (CRLF), `Gameplay/Net/NetSession.cs`, `Gameplay/Net/NetHost.cs`, `Gameplay/Net/NetGuest.cs`, `Gameplay/Net/ReplicaWorld.cs`
- Create: `Assets/_BattleBomb/UI/Combat/NetBanner.cs`
- Test: modify `Tests/EditMode/Net/StageCodecTests.cs`, `Tests/EditMode/Net/HandshakeCodecTests.cs`, `Tests/PlayMode/OnlineJoinSmokeTests.cs`

- [ ] **Step 1: Write the failing Core tests**

In `Assets/_BattleBomb/Tests/EditMode/Net/StageCodecTests.cs`, before the closing brace of the class add:

```csharp

        [Test]
        public void A_stage_says_whether_it_was_placed()
        {
            Assert.That(new LoadStageMessage(0, true, 0f, -1).Placed, Is.False, "A launch stage stands as authored.");
            Assert.That(new LoadStageMessage(1, false, 118.5f, -1).Placed, Is.True, "A stage behind the airlock is always placed.");

            // A late guest's launch stage is wherever the host's airlocks slid it (Task 103).
            var writer = new NetWriter();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, true, 118.5f, 2, placed: true));
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            LoadStageMessage back = StageCodec.ReadLoad(reader);

            Assert.That(back.IsLaunch, Is.True);
            Assert.That(back.Placed, Is.True);
            Assert.That(back.FirstArenaMinX, Is.EqualTo(118.5f));
            Assert.That(back.ResumeCheckpointArena, Is.EqualTo(2));
            Assert.That(reader.Remaining, Is.Zero);
        }
```

In `Assets/_BattleBomb/Tests/EditMode/Net/HandshakeCodecTests.cs`, before the closing brace of the class add:

```csharp

        [Test]
        public void A_launch_says_whether_the_guest_drops_into_a_run_already_going()
        {
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 1, 0, 2, new[] { 0, 0 }, 1, dropIn: true));
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            LaunchMessage back = HandshakeCodec.ReadLaunch(reader);

            Assert.That(back.DropIn, Is.True);
            Assert.That(back.StageIndex, Is.EqualTo(1));
            Assert.That(back.ResumeCheckpointArena, Is.EqualTo(2));
            Assert.That(new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1).DropIn, Is.False);
        }
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors (`Placed`, `dropIn`, `DropIn`).

- [ ] **Step 3: The wire**

In `Assets/_BattleBomb/Core/Net/StageCodec.cs`, replace the struct `LoadStageMessage` (its summary included) with:

```csharp
    /// <summary>A stage the guest must load: the launch stage (unload everything first, then load
    /// with its resume point) or the next stage behind the airlock (load beside the current one,
    /// shifted so its first arena begins at the host's exit line). A late guest's launch stage is
    /// <see cref="Placed"/> too — wherever the host's airlocks slid it (HANDOFF-M8 Task 103).</summary>
    public readonly struct LoadStageMessage
    {
        public readonly int StageIndex;
        public readonly bool IsLaunch;
        public readonly float FirstArenaMinX;
        public readonly int ResumeCheckpointArena;

        /// <summary>Slide the scene so its first arena begins at <see cref="FirstArenaMinX"/>; otherwise it stands as
        /// authored. A preload always is; a launch is only when the guest drops into a run already past its first stage.</summary>
        public readonly bool Placed;

        public LoadStageMessage(int stageIndex, bool isLaunch, float firstArenaMinX, int resumeCheckpointArena)
            : this(stageIndex, isLaunch, firstArenaMinX, resumeCheckpointArena, !isLaunch)
        {
        }

        public LoadStageMessage(int stageIndex, bool isLaunch, float firstArenaMinX, int resumeCheckpointArena, bool placed)
        {
            StageIndex = stageIndex;
            IsLaunch = isLaunch;
            FirstArenaMinX = firstArenaMinX;
            ResumeCheckpointArena = resumeCheckpointArena;
            Placed = placed;
        }
    }
```

and replace `WriteLoad` and `ReadLoad` with:

```csharp
        public static void WriteLoad(NetWriter w, in LoadStageMessage load)
        {
            w.WriteByte((byte)NetMessageKind.LoadStage);
            w.WriteInt(load.StageIndex);
            w.WriteBool(load.IsLaunch);
            w.WriteFloat(load.FirstArenaMinX);
            w.WriteInt(load.ResumeCheckpointArena);
            w.WriteBool(load.Placed);
        }

        public static LoadStageMessage ReadLoad(NetReader r) =>
            new LoadStageMessage(r.ReadInt(), r.ReadBool(), r.ReadFloat(), r.ReadInt(), r.ReadBool());
```

In `Assets/_BattleBomb/Core/Net/HandshakeCodec.cs`:

(a) In `LaunchMessage`, after `public readonly int GuestPlayerId;` add:

```csharp

        /// <summary>The guest joins a run already going (D59): it arrives at a checkpoint room, hidden until the host's
        /// world has it, rather than standing at the stage's spawn with the host.</summary>
        public readonly bool DropIn;
```

and replace its constructor

```csharp
        public LaunchMessage(
            string chapterId, int stageIndex, int tierIndex, int resumeCheckpointArena,
            int[] rosterPicks, int guestPlayerId)
        {
```

with

```csharp
        public LaunchMessage(
            string chapterId, int stageIndex, int tierIndex, int resumeCheckpointArena,
            int[] rosterPicks, int guestPlayerId, bool dropIn = false)
        {
            DropIn = dropIn;
```

(b) In `WriteLaunch`, after `writer.WriteInt(launch.GuestPlayerId);` add `writer.WriteBool(launch.DropIn);`.

(c) In `ReadLaunch`, replace `return new LaunchMessage(chapter, stage, tier, resume, picks, reader.ReadInt());` with:

```csharp
            int guest = reader.ReadInt();
            return new LaunchMessage(chapter, stage, tier, resume, picks, guest, reader.ReadBool());
```

In `Assets/_BattleBomb/Core/Net/NetProtocol.cs`, replace `public const int Version = 7;` with `public const int Version = 8;` and add `8: dropping in at a checkpoint room (Task 103).` to its summary.

- [ ] **Step 4: Run the Core tests** — `recompile`; `run_tests` EditMode `StageCodecTests`, `HandshakeCodecTests`. Expected: all pass.

- [ ] **Step 5: The runner describes where a late guest starts, and a stage asked for twice loads once**

In `Assets/_BattleBomb/Gameplay/World/StageRunner.cs` (CRLF):

(a) In `BeginLoad`'s completion, replace

```csharp
                if (stage.Generation != _generation)
                {
                    // The launch this belonged to was abandoned while the scene was in flight.
                    // Nothing else knows this scene exists, so it has to clean up after itself.
```

with

```csharp
                if (stage.Generation != _generation || (stage != _current && stage != _next))
                {
                    // The launch this belonged to was abandoned while the scene was in flight — or, on a
                    // guest, the same stage was asked for again and a newer plan took its place (the board's
                    // stale-load item). Nothing else knows this scene exists, so it has to clean up after itself.
```

(b) In `ReplicaLoad`, replace

```csharp
                _current = new LoadedStage(
                    definition, new StageRun(definition.ToRuntime(), load.ResumeCheckpointArena), load.StageIndex, _generation);
                BeginLoad(_current, null);
                return;
```

with

```csharp
                _current = new LoadedStage(
                    definition, new StageRun(definition.ToRuntime(), load.ResumeCheckpointArena), load.StageIndex, _generation);

                // A late guest's launch stage stands where the host's airlocks slid it (Task 103).
                BeginLoad(_current, load.Placed ? load.FirstArenaMinX : (float?)null);
                return;
```

(c) After Task 100's `ReplicaChapterCompleted` add:

```csharp

        /// <summary>
        /// Where a late guest starts (D59, HANDOFF-M8 Task 103): the stage under the players as a launch — placed where
        /// this machine put it, resuming at the room the run is standing in — the stage behind the airlock if one is on
        /// its way, and the room's respawn point for <paramref name="slot"/>. False unless the run is standing in a
        /// checkpoint room: a guest only ever arrives in one.
        /// </summary>
        internal bool TryDescribeForDropIn(
            int slot, out LoadStageMessage current, out LoadStageMessage? next, out Vector3 respawn)
        {
            current = default;
            next = null;
            respawn = default;
            if (_replica || _current == null || !_current.IsReady || _current.Run.Phase != StagePhase.AtCheckpoint)
            {
                return false;
            }

            StageRun run = _current.Run;
            CheckpointRoomMarker room = _current.RoomAfter(run.ArenaIndex);
            ArenaMarker first = _current.Arena(0);
            if (room == null || first == null)
            {
                return false;
            }

            current = new LoadStageMessage(_current.StageIndex, true, first.MinX, run.ArenaIndex, placed: true);
            if (_next != null && _current.Exit != null)
            {
                next = new LoadStageMessage(_next.StageIndex, false, _current.Exit.X, -1);
            }

            respawn = room.RespawnPosition + PartnerOffset(slot);
            return true;
        }
```

`StageRunner.cs` already has `using BattleBomb.Core.Net;` (for `LoadStageMessage`) and `using BattleBomb.Gameplay.World.Markers;`.

- [ ] **Step 6: The binder binds late; the guest hides until it is in the world**

In `Assets/_BattleBomb/Gameplay/Session/SessionBinder.cs` (CRLF):

(a) In `Awake`, replace

```csharp
            if (hosting)
            {
                BindHost(net);
            }
            else if (guesting)
            {
                BindGuest(net);
            }
```

with

```csharp
            if (hosting)
            {
                BindHost(net);
            }
            else if (net != null && net.Role == NetRole.Host)
            {
                // Hosting with nobody in yet — open to a friend, or one waiting in their lobby: the host's half comes up
                // now and binds them late, at a checkpoint room (D59, planning decision 17).
                gameObject.AddComponent<NetHost>().Begin(
                    net, _session, _driver, FindAnyObjectByType<World.StageRunner>(), null, this);
            }
            else if (guesting)
            {
                BindGuest(net);
            }
```

(b) In `BindHost`, replace

```csharp
            gameObject.AddComponent<NetHost>().Begin(
                net, _session, _driver, FindAnyObjectByType<World.StageRunner>(), remote);
```

with

```csharp
            gameObject.AddComponent<NetHost>().Begin(
                net, _session, _driver, FindAnyObjectByType<World.StageRunner>(), remote, this);
```

(c) In `BindGuest`, replace

```csharp
            _driver.EnterReplicaMode();
            gameObject.AddComponent<NetGuest>().Begin(net, _driver, net.GuestPlayerId);
```

with

```csharp
            // Dropping into a run already going (D59): this machine's own player waits unseen — never standing in the
            // world defenceless — until the host's world has them. Deactivated before its components wake, as an empty
            // slot is, so nothing registers.
            CharacterActor hidden = null;
            int own = net.GuestPlayerId.Value;
            if (net.JoinedMidRun && own < _players.Length && _players[own] != null)
            {
                hidden = _players[own];
                hidden.gameObject.SetActive(false);
            }

            _driver.EnterReplicaMode();
            gameObject.AddComponent<NetGuest>().Begin(net, _driver, net.GuestPlayerId, hidden);
```

(d) After Task 101's `RestoreGuest` add:

```csharp

        /// <summary>
        /// A guest dropping in at a checkpoint room (D59, planning decision 17): the second player object wakes as the
        /// guest's hero, takes orders from the wire, gets a stash of its own and the guest's own save, and stands up at
        /// <paramref name="respawn"/> — at full health, with the room as home. Runs from the host's half in <c>Stepped</c>,
        /// after every phase of the step, or on a paused frame — never while a phase walks a registry (the TargetRegistry
        /// watch).
        /// </summary>
        internal RemoteCommandSource BindLate(NetSession net, Vector3 respawn)
        {
            int slot = net.GuestPlayerId.Value;
            if (slot >= _players.Length || _players[slot] == null)
            {
                Debug.LogError($"{name}: a guest is dropping in but there is no player object for seat {slot}.", this);
                return null;
            }

            CharacterActor actor = _players[slot];
            actor.SetDefinition(GuestDefinition(net));
            PlayerInventory bag = actor.GetComponent<PlayerInventory>();
            if (bag != null)
            {
                bag.UseStash(new GameObject("Guest Stash").AddComponent<SharedStash>());
            }

            // The wire's source is named before the object wakes, so the actor binds it and no device ever registers.
            RemoteCommandSource remote = NetSeats.MakeRemote(actor, net.GuestPlayerId);
            _remoteSeat = slot;
            actor.gameObject.SetActive(true);

            RestoreGuest(actor, net);
            actor.SetSpawnPoint(respawn);
            actor.ResetForAttempt();
            return remote;
        }
```

In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

(a) After Task 102's `public bool LobbyReady { get; set; }` add:

```csharp

        /// <summary>Guest: the run this machine loaded was already going (D59) — it arrives at a checkpoint room.</summary>
        public bool JoinedMidRun { get; private set; }
```

(b) In `FollowLaunch`, after `session.ResumeCheckpointArena = launch.ResumeCheckpointArena;` add `JoinedMidRun = launch.DropIn;`.

In `Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`:

(a) After the field `private readonly HashSet<int> _unspawnable = new HashSet<int>();` add:

```csharp

        /// <summary>This machine's own player while it drops in (D59), unseen until the host's world has them.</summary>
        private CharacterActor _hidden;
        private int _hiddenId = -1;
```

(b) After the constructor add:

```csharp

        internal void HideUntilSeen(CharacterActor actor, int playerId)
        {
            _hidden = actor;
            _hiddenId = actor != null ? playerId : -1;
        }

        /// <summary>This machine's own player is still waiting to appear.</summary>
        internal bool IsHidingLocal => _hidden != null;
```

(c) In `ApplyPlayers`, replace

```csharp
                CharacterActor actor = PlayerById(player.PlayerId);
                if (actor == null)
                {
                    continue;
                }
```

with

```csharp
                CharacterActor actor = PlayerById(player.PlayerId);
                if (actor == null && _hidden != null && player.PlayerId == _hiddenId)
                {
                    // Dropping in (D59): the first snapshot with this machine's own player in it is when they appear —
                    // standing where the host put them, never anywhere first.
                    _hidden.gameObject.SetActive(true);
                    actor = _hidden;
                    _hidden = null;
                }

                if (actor == null)
                {
                    continue;
                }
```

In `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

(a) Replace the signature line `internal void Begin(NetSession net, SimulationDriver driver, PlayerId local)` with `internal void Begin(NetSession net, SimulationDriver driver, PlayerId local, CharacterActor hidden = null)`, and after the line `_world = new ReplicaWorld(_driver, _runner, FindAnyObjectByType<EnemySpawner>(), GameSession.Find());` add:

```csharp
            _world.HideUntilSeen(hidden, local.Value);
```

(b) After the property `public int NewestHostFrame => _buffer.NewestFrame;` add:

```csharp

        /// <summary>This machine's player has not appeared in the host's world yet — dropping in, waiting for a
        /// checkpoint room (D59). The banner says so.</summary>
        public bool WaitingToAppear => _world != null && _world.IsHidingLocal;
```

Add `using BattleBomb.Gameplay.Characters;` to `NetGuest.cs`.

- [ ] **Step 7: The host's half, open and binding late**

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`:

(a) After the field `private RemoteCommandSource _remote;` add:

```csharp
        private SessionBinder _binder;

        /// <summary>A guest is in the world — bound at the launch, or late at a checkpoint room (Task 103).</summary>
        private bool _bound;

        /// <summary>The guest has been sent a run to load. Until then it hears only the lobby.</summary>
        private bool _launchSent;
```

(b) Replace the signature of `Begin` and its first lines

```csharp
        internal void Begin(
            NetSession net, GameSession session, SimulationDriver driver, StageRunner runner, RemoteCommandSource remote)
        {
            _net = net;
            _session = session;
            _driver = driver;
            _runner = runner;
            _remote = remote;
```

with

```csharp
        internal void Begin(
            NetSession net, GameSession session, SimulationDriver driver, StageRunner runner, RemoteCommandSource remote,
            SessionBinder binder)
        {
            _net = net;
            _session = session;
            _driver = driver;
            _runner = runner;
            _remote = remote;
            _binder = binder;
            _bound = remote != null;
```

(c) In `Begin`, replace

```csharp
                _runner.RemoteStageReady = stage => !_net.IsConnected || _guestReady.Contains(stage);
```

with

```csharp
                // Only a guest who is in the world holds the airlock shut: one still in their lobby, or loading to drop
                // in, has no body at the exit line (the board's "rejoin deadlocks the airlock" item).
                _runner.RemoteStageReady = stage => !_bound || _guestReady.Contains(stage);
```

(d) In `Begin`, replace

```csharp
            _driver.IsOnline = _net.IsConnected;
```

with

```csharp
            _driver.IsOnline = _bound;
            _driver.MenuStepped += OnMenuStepped;
            _net.PeerJoined += OnPeerJoined;
```

(e) In `Begin`, replace

```csharp
            SendLaunch();

            // The guest's lobby shows a host at play — which is what a guest who joins mid-run sees too (Task 103).
            _net.PublishLobby(new LobbyState(FrontendScreen.Launching, IndexInRoster(_session.Characters[0]), true, true));
```

with

```csharp
            if (_bound)
            {
                SendLaunch(_session.Chapter != null ? _session.Chapter.Id : string.Empty,
                    _session.StageIndex, _session.TierIndex, _session.ResumeCheckpointArena, false);
            }

            // The guest's lobby shows a host at play — which is what a guest who joins mid-run sees too.
            OnPeerJoined();
```

(f) Replace the method `SendLaunch` with:

```csharp
        private void SendLaunch(string chapterId, int stage, int tier, int resume, bool dropIn)
        {
            var picks = new int[_session.Characters.Length];
            for (int i = 0; i < picks.Length; i++)
            {
                picks[i] = i == _net.GuestPlayerId.Value ? GuestPick() : IndexInRoster(_session.Characters[i]);
            }

            _writer.Reset();
            HandshakeCodec.WriteLaunch(_writer, new LaunchMessage(
                chapterId, stage, tier, resume, picks, _net.GuestPlayerId.Value, dropIn));
            _net.Send(NetChannel.Reliable, _writer);
            _launchSent = true;
        }

        private void OnPeerJoined() =>
            _net.PublishLobby(new LobbyState(FrontendScreen.Launching, IndexInRoster(_session.Characters[0]), true, true));

        private void OnMenuStepped()
        {
            if (_net.IsConnected)
            {
                Admit();
            }
        }

        /// <summary>
        /// A guest dropping in (D59, planning decision 17). Once they are ready, the first checkpoint room the run
        /// stands in sends them the run to load — the stage under the players and the one behind the airlock. Once
        /// they have loaded both, and the run is still in a checkpoint room, they stand up there; if the run walked
        /// on meanwhile, the stages it streams reach them as they would a bound guest, and the next room binds them.
        /// Runs after a step or on a paused frame, never inside one.
        /// </summary>
        private void Admit()
        {
            if (_bound || !_net.GuestReady || _runner == null
                || !_runner.TryDescribeForDropIn(
                    _net.GuestPlayerId.Value, out LoadStageMessage current, out LoadStageMessage? next, out Vector3 respawn))
            {
                return;
            }

            if (!_launchSent)
            {
                SendLaunch(_runner.Chapter != null ? _runner.Chapter.Id : string.Empty,
                    current.StageIndex, _runner.TierIndex, current.ResumeCheckpointArena, true);
                _guestReady.Clear();
                SendLoad(current);
                if (next.HasValue)
                {
                    SendLoad(next.Value);
                }

                return;
            }

            if (!_guestReady.Contains(current.StageIndex) || (next.HasValue && !_guestReady.Contains(next.Value.StageIndex)))
            {
                return;
            }

            Bind(respawn);
        }

        private void SendLoad(in LoadStageMessage load)
        {
            _writer.Reset();
            StageCodec.WriteLoad(_writer, load);
            _net.Send(NetChannel.Reliable, _writer);
        }

        /// <summary>
        /// The guest stands up in the room (Task 103), and is told everything a snapshot never carries before the first
        /// snapshot with them in it: every drop on the ground, every open screen and its rack, and — forced — both
        /// players' inventories (the board's baseline item).
        /// </summary>
        private void Bind(Vector3 respawn)
        {
            _remote = _binder != null ? _binder.BindLate(_net, respawn) : null;
            if (_remote == null)
            {
                return;
            }

            _bound = true;
            _driver.IsOnline = true;
            _remote.Stream.Release();

            IReadOnlyList<DropPickup> pickups = _driver.Pickups;
            for (int i = 0; i < pickups.Count; i++)
            {
                if (pickups[i] != null)
                {
                    _pending.Add(ReplicatedEvent.OfDrop(new DropRecord(pickups[i].NetId, pickups[i].Position, pickups[i].Item)));
                }
            }

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                int id = actors[i].PlayerId.Value;
                if (_driver.TryGetOpenScreen(id, out InteractionKind kind))
                {
                    if (kind == InteractionKind.Shopkeeper)
                    {
                        OnRackChanged(id);
                    }

                    _pending.Add(ReplicatedEvent.OfScreen(id, (int)kind, true));
                }
            }

            foreach (KeyValuePair<int, Watched> entry in _watched)
            {
                entry.Value.Forced = true;
            }

            _runner?.RefreshBounds();
        }
```

(g) In `OnStepped`, replace

```csharp
            if (!_net.IsConnected)
            {
                _pending.Clear();
                _answers.Clear();
                return;
            }
```

with

```csharp
            if (!_net.IsConnected)
            {
                _pending.Clear();
                _answers.Clear();
                return;
            }

            Admit();
            if (!_launchSent)
            {
                // A guest still in their lobby hears only the lobby — nothing of a world they are not yet loading.
                _pending.Clear();
                return;
            }
```

(h) In `OnStageLoadRequested`, add at the very top:

```csharp
            if (!_launchSent)
            {
                return;
            }

```

and replace `_driver.HoldForPeer = _net.IsConnected;` with `_driver.HoldForPeer = _bound;`.

(i) In `OnStageHandedOver`, add the same `if (!_launchSent) { return; }` at the top.

(j) In `OnDestroy`, inside `if (_driver != null)`, add `_driver.MenuStepped -= OnMenuStepped;`; after `_net.PeerLeft -= OnPeerLeft;` add `_net.PeerJoined -= OnPeerJoined;`.

`NetHost.cs` already has `using BattleBomb.Gameplay.Loot;` (for `DropPickup`), `BattleBomb.Gameplay.Characters` and `BattleBomb.Gameplay.World`.

- [ ] **Step 8: The guest's banner**

`Assets/_BattleBomb/UI/Combat/NetBanner.cs`:

```csharp
using BattleBomb.Gameplay.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// One line at the top of the screen when the connection has something to say (HANDOFF-M8 Stage D): a guest
    /// dropping in waits for a checkpoint room here (Task 103). Placeholder IMGUI like the rest of the HUD — M9's HUD
    /// replaces its look, not what it says. Installs itself, so no scene carries it; with nothing to say it draws nothing.
    /// </summary>
    public sealed class NetBanner : MonoBehaviour
    {
        private GUIStyle _style;
        private NetGuest _guest;
        private float _nextLook;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("Net Banner");
            DontDestroyOnLoad(go);
            go.AddComponent<NetBanner>();
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;

        private void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

        private void OnSceneChanged(Scene from, Scene to)
        {
            _guest = null;
            _nextLook = 0f;
        }

        /// <summary>What the banner says now, or null. Public for the smoke suite.</summary>
        public string Line
        {
            get
            {
                // Looked for at most twice a second, never every frame (Plan 1's note about Find in OnGUI).
                if (_guest == null && Time.unscaledTime >= _nextLook)
                {
                    _guest = FindAnyObjectByType<NetGuest>();
                    _nextLook = Time.unscaledTime + 0.5f;
                }

                if (_guest != null && _guest.WaitingToAppear)
                {
                    return "Waiting for the host to reach a checkpoint room.";
                }

                return null;
            }
        }

        private void OnGUI()
        {
            string line = Line;
            if (line == null)
            {
                return;
            }

            int fontSize = Mathf.Max(14, Screen.height / 40);
            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.box) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter };
            }

            float width = Mathf.Min(Screen.width - 32f, fontSize * 30f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, 16f, width, fontSize * 2.2f), line, _style);
        }
    }
}
```

The `NetGuest` lookup happens at most twice a second and only while there is none, never per frame (Plan 1's M6 note about `Find` in `OnGUI`).

- [ ] **Step 9: Drop-in, tested**

In `Assets/_BattleBomb/Tests/PlayMode/OnlineJoinSmokeTests.cs`:

(a) Add `using System.Collections.Generic;`, `using BattleBomb.Core.Combat;`, `using BattleBomb.Core.Items;`, `using BattleBomb.Core.Movement;`, `using BattleBomb.Core.Players;`, `using BattleBomb.Core.Stats;`, `using BattleBomb.Gameplay.Characters;`, `using BattleBomb.Gameplay.Loot;` and `using BattleBomb.Gameplay.World;` to the usings.

(b) After the field `private HeadlessGuest _guest;` add:

```csharp
        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _host;
        private ScriptedCommandSource _hostInput;
```

(c) Before `private static NetSession HostOverLoopback(` add:

```csharp
        [UnityTest]
        public IEnumerator A_guest_who_readies_mid_run_waits_for_a_checkpoint_room_and_stands_up_there()
        {
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return LaunchSolo();
            ItemInstance knife = _driver.RollDebugItem(7, 2.2f);
            _driver.SpawnDebugDrop(_host.Position + new Vector3(2f, 0f, 0f), knife);
            int dropId = _driver.Pickups[_driver.Pickups.Count - 1].NetId;

            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.GuestReady, "the guest never readied");
            yield return Steps(60);
            Assert.That(_guest.Launch.HasValue, Is.False, "The guest was sent the run mid-fight; it must wait for a checkpoint room.");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1));

            yield return PushHostRight(() => _runner.Phase == StagePhase.AtCheckpoint, "the first checkpoint room");
            yield return Until(() => _driver.Characters.Ordered.Count == 2, "the guest never stood up in the room");

            Assert.That(_guest.Launch.HasValue, Is.True);
            Assert.That(_guest.Launch.Value.DropIn, Is.True, "The guest was not told it is dropping into a run already going.");
            LoadStageMessage launchStage = _guest.LoadMessages.Find(load => load.IsLaunch);
            Assert.That(launchStage.Placed, Is.True, "The late guest's launch stage was not placed where the host's is.");

            CharacterActor guestBody = _driver.Characters.Ordered[1];
            Assert.That(guestBody.PlayerId, Is.EqualTo(PlayerId.Two));
            Assert.That(_driver.Players.TryGet(PlayerId.Two, out IPlayerCommandSource source) && source is RemoteCommandSource, Is.True,
                "The late guest's body answers to something other than the wire.");
            Assert.That(Mathf.Abs(guestBody.Position.x - _host.Position.x), Is.LessThan(8f), "The guest stood up somewhere other than the host's room.");
            Assert.That(_driver.IsOnline, Is.True);

            yield return Steps(4);
            Assert.That(DropAnnounced(dropId), Is.True, "The drop already on the ground was never announced to the late guest.");
        }

        [UnityTest]
        public IEnumerator A_guest_still_in_their_lobby_never_holds_the_airlock_shut()
        {
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return LaunchSolo();
            _guest = HeadlessGuest.Join(guestSide);
            _guest.AutoPick = false;
            yield return UntilFrames(() => net.IsConnected, "the guest never connected");

            yield return PushHostRight(() => _runner.StageIndex == 1, "stage two, through the airlock");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator A_guest_dropping_in_stays_unseen_until_the_hosts_world_has_them()
        {
            const int Start = 1000;
            const int Appear = Start + 80;
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1, dropIn: true));
            recording.Add((Start - 120, writer.ToArray()));
            for (int frame = Start; frame <= Start + 200; frame += NetProtocol.SnapshotEverySteps)
            {
                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                if (frame >= Appear)
                {
                    world.Players.Add(Standing(1, 3f));
                }

                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            var playback = new PlaybackTransport(recording);
            NetSession.FindOrCreate().Join(playback, "playback");
            NetGuest guest = null;
            for (int i = 0; i < LoadFrameCeiling && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null, "The guest never loaded the host's run.");
            var driver = Object.FindAnyObjectByType<SimulationDriver>();
            yield return UntilFrames(() => guest.RenderFrame >= Start + 10, "the guest never started drawing");
            Assert.That(driver.Characters.Ordered.Count, Is.EqualTo(1), "The guest's own body stood in the world before the host had it.");
            Assert.That(guest.WaitingToAppear, Is.True);
            Assert.That(Object.FindAnyObjectByType<UI.Combat.NetBanner>().Line, Does.Contain("checkpoint"));

            yield return UntilFrames(() => guest.RenderFrame >= Appear + 4, "the picture never reached the guest's arrival");
            Assert.That(driver.Characters.Ordered.Count, Is.EqualTo(2), "The guest never appeared once the host's world had them.");
            foreach (CharacterActor actor in driver.Characters.Ordered)
            {
                if (actor.PlayerId.Value == 1)
                {
                    Assert.That(actor.Position.x, Is.EqualTo(3f).Within(0.05f), "The guest appeared somewhere other than where the host put them.");
                }
            }

            Assert.That(guest.WaitingToAppear, Is.False);
        }

        [UnityTest]
        public IEnumerator A_stage_asked_for_twice_on_the_guest_is_loaded_once()
        {
            const int Start = 1000;
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1));
            recording.Add((Start - 120, writer.ToArray()));
            writer.Reset();
            StageCodec.WriteLoad(writer, new LoadStageMessage(0, true, 0f, -1));
            recording.Add((Start - 120, writer.ToArray()));
            writer.Reset();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, false, 200f, -1));
            byte[] preload = writer.ToArray();
            for (int frame = Start; frame <= Start + 200; frame += NetProtocol.SnapshotEverySteps)
            {
                if (frame == Start + 10)
                {
                    // The same stage asked for twice, back to back, before either load has landed.
                    recording.Add((frame, preload));
                    recording.Add((frame, preload));
                }

                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                world.Players.Add(Standing(1, 2f));
                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            NetSession.FindOrCreate().Join(new PlaybackTransport(recording), "playback");
            NetGuest guest = null;
            for (int i = 0; i < LoadFrameCeiling && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null);
            yield return UntilFrames(() => guest.RenderFrame >= Start + 150, "the guest never drew the recording");
            for (int i = 0; i < 60; i++)
            {
                yield return null;
            }

            int copies = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == "FixtureStage2" && scene.isLoaded)
                {
                    copies++;
                }
            }

            Assert.That(copies, Is.EqualTo(1), "A stage asked for twice stood in the world twice.");
        }

        private IEnumerator LaunchSolo()
        {
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return UntilFrames(() => _runner.IsStageLoaded, "the stage never streamed in");
            yield return UntilFrames(() => _driver.Frame > 5, "the run never started");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1), "A solo launch woke a second player.");

            _host = _driver.Characters.Ordered[0];
            DisableDevices();
            _driver.Players.Unregister(_host.PlayerId);
            _hostInput = _host.gameObject.AddComponent<ScriptedCommandSource>();
            _hostInput.Bind(_host.PlayerId.Value);
            _driver.Players.Register(_hostInput);
        }

        private IEnumerator PushHostRight(System.Func<bool> done, string what)
        {
            _hostInput.Set(Vector2.right, CommandButtons.None);
            int deadline = _driver.Frame + 5000;
            for (int guard = 0; guard < 30000 && _driver.Frame < deadline && !done(); guard++)
            {
                yield return null;
            }

            _hostInput.Release();
            Assert.That(done(), Is.True, $"Pushing right never reached {what}.");
        }

        private IEnumerator Steps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < 30000 && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + 1200;
            for (int guard = 0; guard < 30000 && _driver.Frame < deadline; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited 1200 steps).");
        }

        private bool DropAnnounced(int netId)
        {
            var batch = new List<ReplicatedEvent>();
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Events)
                {
                    continue;
                }

                EventCodec.Read(reader, null, batch);
                foreach (ReplicatedEvent e in batch)
                {
                    if (e.Kind == ReplicatedEventKind.DropSpawned && e.Drop.NetId == netId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static PlayerSnapshot Standing(int playerId, float x) => new PlayerSnapshot(
            playerId, MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready,
            new PlayerCondition(Health.FromValues(100f, 100f), 0, 0), ReviveChannel.Inactive,
            ManaPool.FromValues(50f, 50f), true, null, -1, 0, 0);
```

The drop is spawned before the guest connects, so the only way it can reach the guest is the bind's baseline. The host's headless guest answers every `LoadStage` with `StageReady` at once (`AutoReady`), so the bind happens on the first step both are true.

- [ ] **Step 10: Run the suites** — `recompile`, `console` `level: error` clean. Full EditMode (834 + 2 = 836). PlayMode (async): `OnlineJoinSmokeTests` (seven), `GuestStageSmokeTests` (its launches stand as authored — `Placed` is false for them), `OnlineHostSmokeTests` (a guest bound at the launch behaves as before: `_bound` from the start, the same launch hold), `OnlineLaunchHoldSmokeTests`, `ReplicaReplaySmokeTests`. Then the full PlayMode suite (96 + 4 = 100). Delete `Assets/InitTestScene*`.

- [ ] **Step 11: Commit** — subject `103: drop-in at checkpoint rooms`. Body: the host's half is up whenever the host is hosting; a guest who readies mid-run is sent the run at the next checkpoint room — the stage under the players, placed where the host put it, and the one behind the airlock — and stands up in the room once they have loaded, never before (D59, planning decision 17); they are told every drop, screen and rack a snapshot never carries before the first snapshot with them in it; the guest's own body waits unseen until the host's world has it, with a banner; only a guest in the world holds the airlock; a stage asked for twice on a guest loads once; protocol 8.

---

### Task 104: Leaving, drops, and open by default

D61's leaving rules and D59's "open by default". **The guest leaves or drops:** their body leaves the world with them, so the host carries on solo and solo's rules — the chest pause, the solo camera — come back by themselves, because there is one player again; the abandoned body no longer holds a gate shut (the board's D60/D61 item); the host keeps listening, and a friend can drop in again at the next room. **The host leaves or drops:** the guest lands on its own title, told *"The host left."*, keeping everything up to its last autosave — exactly what a crash costs, so nothing is written on the way out. **About ten seconds of silence is a drop** (Plan 1 built the timer); from one second a banner says *"Connection problem…"* on both machines. **Solo games are open to friends by default**, a couch game never; a settings row closes it, remembered in the save. Two source fixes ride along: a source leaving never takes a newer source under the same player with it, and a seat decided after its source woke is rebuilt (the board's two `Plan 2 — seats` items).

**Files:**
- Modify: `Assets/_BattleBomb/Core/Saves/SaveModel.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Players/PlayerRegistry.cs`, `Gameplay/Players/InputSystemCommandSource.cs`, `Gameplay/Net/RemoteCommandSource.cs`, `Gameplay/Net/NetSession.cs`, `Gameplay/Net/NetHost.cs`, `Gameplay/Session/SessionBinder.cs` (CRLF), `Gameplay/Session/SaveService.cs` (CRLF), `Gameplay/Session/GameSession.cs` (CRLF)
- Modify: `Assets/_BattleBomb/UI/Combat/NetBanner.cs`, `UI/Chest/SettingsRows.cs`, `UI/Chest/SettingsMenu.cs`, `UI/Frontend/FrontendFlow.cs` (CRLF), `UI/Debug/NetDevOverlay.cs`
- Test: create `Tests/EditMode/SeatSourceTests.cs`; modify `Tests/EditMode/PlayerRegistryLocalTests.cs`, `Tests/EditMode/SaveCodecTests.cs`, `Tests/EditMode/SettingsRowsTests.cs`, `Tests/PlayMode/HeadlessGuest.cs`, `Tests/PlayMode/OnlineJoinSmokeTests.cs`

- [ ] **Step 1: Write the failing EditMode tests**

In `Assets/_BattleBomb/Tests/EditMode/PlayerRegistryLocalTests.cs`, before the closing brace of the class add:

```csharp

        [Test]
        public void A_source_that_leaves_never_takes_a_newer_one_with_it()
        {
            var registry = new PlayerRegistry();
            var old = new Wire(1);
            var newer = new Wire(1);
            registry.Register(old);
            registry.Register(newer);

            Assert.That(registry.Unregister(old), Is.False, "The old source's leaving removed something.");
            Assert.That(registry.TryGet(new PlayerId(1), out IPlayerCommandSource held) && held == newer, Is.True,
                "A source leaving took the newer source for the same player with it (a rejoin would lose its body's orders).");
            Assert.That(registry.Unregister(newer), Is.True);
            Assert.That(registry.IsRegistered(new PlayerId(1)), Is.False);
        }
```

`Assets/_BattleBomb/Tests/EditMode/SeatSourceTests.cs`:

```csharp
using System.Reflection;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The guest's seat (HANDOFF-M8 planning decision 18) decided after its source had already woken — a player object
    /// authored inactive and woken by the binder, or any late change — must be rebuilt for the new seat. Neither
    /// component is <c>[ExecuteAlways]</c>, so <c>OnEnable</c>, <c>OnDisable</c> and the internal <c>UseSeat</c> are
    /// reached by reflection, as <see cref="SharedStashWiringTests"/> does.
    /// </summary>
    public sealed class SeatSourceTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void A_seat_changed_after_the_source_woke_is_rebuilt_for_the_new_seat()
        {
            var driverGo = new GameObject("Driver");
            var go = new GameObject("Player Two");
            try
            {
                SimulationDriver driver = driverGo.AddComponent<SimulationDriver>();
                InputSystemCommandSource source = go.AddComponent<InputSystemCommandSource>();
                var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_BattleBomb/Input/BattleBombControls.inputactions");
                Assert.That(controls, Is.Not.Null, "The project's controls moved — update this path.");
                typeof(InputSystemCommandSource).GetField("_controls", Private).SetValue(source, controls);
                typeof(InputSystemCommandSource).GetField("_seat", Private).SetValue(source, 1);
                typeof(InputSystemCommandSource).GetField("_driver", Private).SetValue(source, driver);

                typeof(InputSystemCommandSource).GetMethod("OnEnable", Private).Invoke(source, null);
                Assert.That(driver.Players.IsRegistered(new PlayerId(1)), Is.True);

                typeof(InputSystemCommandSource).GetMethod("UseSeat", Private).Invoke(source, new object[] { 0, 1 });

                Assert.That(driver.Players.TryGet(new PlayerId(1), out IPlayerCommandSource held) && held == source, Is.True,
                    "After the seat changed the source is no longer registered as the player it speaks for.");
                object input = typeof(InputSystemCommandSource).GetField("_input", Private).GetValue(source);
                Assert.That(input, Is.Not.Null);
                Assert.That(typeof(SeatInput).GetField("_seat", Private).GetValue(input), Is.EqualTo(0),
                    "The source still reads the old seat's devices: its controls were not rebuilt.");

                typeof(InputSystemCommandSource).GetMethod("OnDisable", Private).Invoke(source, null);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(driverGo);
            }
        }
    }
}
```

In `Assets/_BattleBomb/Tests/EditMode/SaveCodecTests.cs`, before the closing brace of the class add:

```csharp

        [Test]
        public void Whether_the_game_is_open_to_friends_survives_a_save_and_an_old_save_reads_open()
        {
            SaveGame closed = SaveGame.Fresh().WithClosedToFriends(true);

            Assert.That(SaveCodec.Decode(SaveCodec.Encode(closed)).Save.ClosedToFriends, Is.True);
            Assert.That(SaveCodec.Decode(SaveCodec.Encode(SaveGame.Fresh())).Save.ClosedToFriends, Is.False,
                "D59: a save that never said reads as open.");
            Assert.That(closed.WithVersion(2).ClosedToFriends, Is.True, "A migration step dropped the setting.");
        }
```

In `Assets/_BattleBomb/Tests/EditMode/SettingsRowsTests.cs`, replace

```csharp
            Assert.That(SettingsRows.For(development: false), Is.EqualTo(new[]
            {
                SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters,
            }));
```

with

```csharp
            Assert.That(SettingsRows.For(development: false), Is.EqualTo(new[]
            {
                SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters, SettingsRow.OpenToFriends,
            }));
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors (`Unregister(IPlayerCommandSource)` is ambiguous-free but missing, `WithClosedToFriends`, `SettingsRow.OpenToFriends`). Once they compile (Step 3), `SeatSourceTests` must fail on the old `UseSeat` with "its controls were not rebuilt" — run it once against the old method to see that red before Step 4 changes it.

- [ ] **Step 3: The registry, the save's setting, the settings row**

In `Assets/_BattleBomb/Gameplay/Players/PlayerRegistry.cs`, after `public bool Unregister(PlayerId playerId) => _sources.Remove(playerId.Value);` add:

```csharp

        /// <summary>Removes <paramref name="source"/> — and only it. A source leaving never takes a newer source for the
        /// same player with it: a guest who rejoins, or a device and the wire swapping on one body (HANDOFF-M8 Task 104).</summary>
        public bool Unregister(IPlayerCommandSource source)
        {
            if (source == null || !_sources.TryGetValue(source.PlayerId.Value, out IPlayerCommandSource held)
                || !ReferenceEquals(held, source))
            {
                return false;
            }

            return _sources.Remove(source.PlayerId.Value);
        }
```

In `Assets/_BattleBomb/Core/Saves/SaveModel.cs`, in `SaveGame`:

(a) After `[SerializeField] private StoryProgressSave _story;` add:

```csharp

        /// <summary>D59: solo games are open to platform friends unless the player closed them. Stored as "closed" so a
        /// save from before the setting existed reads as open.</summary>
        [SerializeField] private bool _closedToFriends;
```

(b) Replace the constructor

```csharp
        public SaveGame(
            int version, int coins, bool autoEquip, bool autoSell,
            ItemStackSave[] sack, CharacterSave[] characters, StoryProgressSave story)
        {
```

with

```csharp
        public SaveGame(
            int version, int coins, bool autoEquip, bool autoSell,
            ItemStackSave[] sack, CharacterSave[] characters, StoryProgressSave story, bool closedToFriends = false)
        {
            _closedToFriends = closedToFriends;
```

(c) After `public StoryProgressSave Story => _story ?? new StoryProgressSave();` add:

```csharp
        public bool ClosedToFriends => _closedToFriends;

        /// <summary>The same save with the open-to-friends setting as the player left it.</summary>
        public SaveGame WithClosedToFriends(bool closed) =>
            new SaveGame(_version, _coins, _autoEquip, _autoSell, Sack, Characters, Story, closed);
```

(d) Replace

```csharp
        public SaveGame WithVersion(int version) =>
            new SaveGame(version, _coins, _autoEquip, _autoSell, Sack, Characters, Story);
```

with

```csharp
        public SaveGame WithVersion(int version) =>
            new SaveGame(version, _coins, _autoEquip, _autoSell, Sack, Characters, Story, _closedToFriends);
```

In `Assets/_BattleBomb/UI/Chest/SettingsRows.cs`:

(a) Replace

```csharp
        TierOverlay = 4,
    }
```

with

```csharp
        TierOverlay = 4,
        OpenToFriends = 5,
    }
```

(b) Replace

```csharp
        private static readonly SettingsRow[] Release =
        {
            SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters,
        };

        private static readonly SettingsRow[] Development =
        {
            SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters,
            SettingsRow.GrantTestLoot, SettingsRow.TierOverlay,
        };
```

with

```csharp
        private static readonly SettingsRow[] Release =
        {
            SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters, SettingsRow.OpenToFriends,
        };

        private static readonly SettingsRow[] Development =
        {
            SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters, SettingsRow.OpenToFriends,
            SettingsRow.GrantTestLoot, SettingsRow.TierOverlay,
        };
```

In `Assets/_BattleBomb/Gameplay/Session/GameSession.cs` (CRLF):

(a) After `public StoryProgress Progress { get; set; } = new StoryProgress();` add:

```csharp

        /// <summary>The player closed their solo games to friends (D59) — read from the save, written with it.</summary>
        public bool ClosedToFriends { get; set; }
```

(b) In `LoadFromStore`, replace

```csharp
            LoadedSave = null;
            LoadOutcome = SaveLoadReason.Ok;
            Progress = new StoryProgress();
```

with

```csharp
            LoadedSave = null;
            LoadOutcome = SaveLoadReason.Ok;
            Progress = new StoryProgress();
            ClosedToFriends = false;
```

and replace

```csharp
                LoadedSave = load.Save;
                Progress = SaveMapper.RestoreProgress(load.Save);
```

with

```csharp
                LoadedSave = load.Save;
                Progress = SaveMapper.RestoreProgress(load.Save);
                ClosedToFriends = load.Save.ClosedToFriends;
```

In `Assets/_BattleBomb/Gameplay/Session/SaveService.cs` (CRLF), in `SaveNow`, replace

```csharp
            SaveGame save = SaveMapper.Capture(
                _stash.Sack, _stash.Wallet, _states, _session.Progress,
                _session.LoadedSave != null ? _session.LoadedSave.Characters : null);
```

with

```csharp
            SaveGame save = SaveMapper.Capture(
                _stash.Sack, _stash.Wallet, _states, _session.Progress,
                _session.LoadedSave != null ? _session.LoadedSave.Characters : null)
                .WithClosedToFriends(_session.ClosedToFriends);
```

- [ ] **Step 4: The sources leave cleanly, and a late seat is rebuilt**

In `Assets/_BattleBomb/Gameplay/Players/InputSystemCommandSource.cs`:

(a) Replace the method `UseSeat` (its summary included) with:

```csharp
        /// <summary>
        /// Online (HANDOFF-M8 planning decision 18): the device rules of one seat, speaking as
        /// another player. The guest's machine gives its one local player seat 0's rule — every
        /// device is theirs — while they are Player 2 in the host's game. Called by the binder
        /// before <c>OnEnable</c> in the usual case; if the source is already awake — a player
        /// object authored inactive and woken early, or any late change — its controls were built
        /// for the old seat and it is registered as the old player, so it goes round again.
        /// </summary>
        internal void UseSeat(int seat, int speakAs)
        {
            bool awake = _input != null;
            if (awake)
            {
                OnDisable();
            }

            _seat = seat;
            _speakAs = speakAs;
            if (awake)
            {
                OnEnable();
            }
        }
```

(b) In `OnDisable`, replace `_host?.Players.Unregister(PlayerId);` with `_host?.Players.Unregister(this);`.

In `Assets/_BattleBomb/Gameplay/Net/RemoteCommandSource.cs`, in `Unregister`, replace `_host?.Players.Unregister(_id);` with `_host?.Players.Unregister(this);`.

- [ ] **Step 5: Run EditMode** — `recompile`; `run_tests` EditMode `SeatSourceTests` (red before Step 4 (a), green after), `PlayerRegistryLocalTests`, `SaveCodecTests`, `SettingsRowsTests`, `SettingsMenuReleaseAcceptanceTests`; then the full suite (836 + 3 = 839).

- [ ] **Step 6: The guest's leaving, on the host**

In `Assets/_BattleBomb/Gameplay/Session/SessionBinder.cs` (CRLF):

(a) After the field `private int _remoteSeat = -1;` add:

```csharp

        /// <summary>The stash this binder made for the guest's own save, so it leaves with them — never the couch's.</summary>
        private SharedStash _guestStash;
```

(b) In `BindHost` (Task 101) and in `BindLate` (Task 103), replace each

```csharp
                guestBag.UseStash(new GameObject("Guest Stash").AddComponent<SharedStash>());
```

and

```csharp
                bag.UseStash(new GameObject("Guest Stash").AddComponent<SharedStash>());
```

with, respectively,

```csharp
                _guestStash = new GameObject("Guest Stash").AddComponent<SharedStash>();
                guestBag.UseStash(_guestStash);
```

and

```csharp
                _guestStash = new GameObject("Guest Stash").AddComponent<SharedStash>();
                bag.UseStash(_guestStash);
```

(c) After the method `BindLate` add:

```csharp

        /// <summary>
        /// The guest is gone (D61): their body leaves the world with them, so the host carries on solo — and solo's rules
        /// come back by themselves, because there is one player again; nothing lies at the exit line holding a gate shut.
        /// What the guest carried leaves with them: their own machine saved it at its last autosave. Runs from the
        /// session's pump, before the frame's steps.
        /// </summary>
        internal void Unbind(NetSession net)
        {
            if (_remoteSeat < 0 || _remoteSeat >= _players.Length || _players[_remoteSeat] == null)
            {
                _remoteSeat = -1;
                return;
            }

            _driver.CloseScreen(net.GuestPlayerId.Value);
            _players[_remoteSeat].gameObject.SetActive(false);
            if (_guestStash != null)
            {
                Destroy(_guestStash.gameObject);
                _guestStash = null;
            }

            _remoteSeat = -1;
        }
```

In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

(a) After Task 103's `public bool JoinedMidRun { get; private set; }` add:

```csharp

        /// <summary>Host: when a guest who had joined was last lost (unscaled seconds), for the banner. Minus infinity
        /// before any has.</summary>
        public double GuestLeftAt { get; private set; } = double.NegativeInfinity;

        /// <summary>Why the last game ended on this machine when it was not this machine's choice — "The host left." —
        /// for the front door's title to say once.</summary>
        private string _ending;

        public string TakeEnding()
        {
            string ending = _ending;
            _ending = null;
            return ending;
        }

        /// <summary>Where "open to friends" gets its transport (D59): Steam's lobby from Plan 3, the local socket from
        /// the development panel, and nothing in a build with neither — so nothing opens (rule 6). A factory slot,
        /// like the planned PlatformRegistry, holding no game state.</summary>
        public static Func<INetTransport> FriendsTransport { get; set; }

        /// <summary>Development: solo games open on this machine's local socket (another editor can drop in), or not.</summary>
        public static void UseLocalFriends(bool on, int lagIndex) =>
            FriendsTransport = on ? () => WrapLocal(new LocalSocketTransport(NetProtocol.DevPort), lagIndex) : (Func<INetTransport>)null;

        /// <summary>Host: new friends are turned away — the player closed the game, or it is a couch game. A guest
        /// already here stays.</summary>
        public bool ClosedToFriends { get; private set; }

        /// <summary>
        /// A game opens to friends, or closes (D59, Task 104). Opening hosts through <see cref="FriendsTransport"/> when
        /// this machine is offline and there is one; closing turns new friends away and stops listening unless a guest is
        /// already here.
        /// </summary>
        public void OpenToFriends(bool open)
        {
            ClosedToFriends = !open;
            if (open && Role == NetRole.Offline && FriendsTransport != null)
            {
                Host(FriendsTransport());
            }
            else if (!open && Role == NetRole.Host && Peer.IsNone)
            {
                Close();
            }
        }
```

(b) In `Update`, replace

```csharp
            if (Now - _lastReceived > NetProtocol.DropAfterSeconds)
            {
                Status = "The connection went silent.";
                _transport.Disconnect(Peer);
                return;
            }
```

with

```csharp
            if (Now - _lastReceived > NetProtocol.DropAfterSeconds)
            {
                // About ten seconds of silence is a drop (Michael, design §1). Lost here, by name, so the reason is
                // silence and not the "left" the transport's own disconnect would report.
                _transport.Disconnect(Peer);
                Lost("went quiet");
                return;
            }
```

(c) In `Greet` (Task 102), replace

```csharp
            string refusal = HandshakeCodec.CheckHello(hello, Application.version)
                ?? (IsFull ? "The game is full." : null);
```

with

```csharp
            string refusal = HandshakeCodec.CheckHello(hello, Application.version)
                ?? (IsFull ? "The game is full." : null)
                ?? (ClosedToFriends ? "The game is not open to friends." : null);
```

(d) In `Lost`, replace

```csharp
            if (Role == NetRole.Guest)
            {
                Close();
                Status = $"The host {why}.";
                if (wasJoined)
                {
                    PeerLeft?.Invoke();
                    ReturnToFrontend();
                }

                return;
            }
```

with

```csharp
            if (Role == NetRole.Guest)
            {
                Close();
                Status = $"The host {why}.";
                if (wasJoined)
                {
                    // D61: back to this machine's own title, keeping everything up to its last autosave — what a crash
                    // costs. Nothing is written on the way out.
                    _ending = why == "left" ? "The host left." : "The connection to the host was lost.";
                    PeerLeft?.Invoke();
                    ReturnToFrontend();
                }

                return;
            }

            if (wasJoined)
            {
                GuestLeftAt = Now;
            }
```

(e) Replace

```csharp
        /// <summary>Development: join a host on this machine's local socket.</summary>
        public void JoinLocal(int lagIndex) =>
            Join(WrapLocal(new LocalSocketTransport(0), lagIndex), $"127.0.0.1:{NetProtocol.DevPort}");
```

with

```csharp
        /// <summary>Two editors on one PC share one save folder: the one that joins plays from a save of its own, or its
        /// autosaves (Task 101) would land in the host's file. Development only — a real guest is on its own machine.</summary>
        public const string LocalGuestSave = "local-guest";

        /// <summary>Development: join a host on this machine's local socket, from the joining editor's own save.</summary>
        public void JoinLocal(int lagIndex)
        {
            GameSession session = GetComponent<GameSession>();
            if (session != null && session.SaveName != LocalGuestSave)
            {
                session.SaveName = LocalGuestSave;
                session.LoadFromStore();
            }

            Join(WrapLocal(new LocalSocketTransport(0), lagIndex), $"127.0.0.1:{NetProtocol.DevPort}");
        }
```

`NetSession.cs` already has `using System;` and `using BattleBomb.Platform.Net;`.

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`, replace the method `OnPeerLeft` (its summary included) with:

```csharp
        /// <summary>
        /// The guest is gone (D61): their body leaves the world with them and the host carries on solo, under solo's rules
        /// again. The host keeps listening, so a friend — the same one or another — can drop in at the next room (Task 103).
        /// </summary>
        private void OnPeerLeft()
        {
            if (_remote != null)
            {
                _remote.Stream.Release();
            }

            if (_bound && _binder != null)
            {
                _binder.Unbind(_net);
            }

            Unwatch(_net.GuestPlayerId.Value);
            _remote = null;
            _bound = false;
            _launchSent = false;
            _guestReady.Clear();
            _pending.Clear();
            _answers.Clear();
            _driver.HoldForPeer = false;
            _driver.IsOnline = false;
            _runner?.RefreshBounds();
        }
```

- [ ] **Step 7: The title says why; the banner says what is happening; the game opens by default**

In `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs` (CRLF):

(a) After Task 102's `public GuestLobby Lobby => _lobby;` add:

```csharp

        /// <summary>Why the last online game ended here when it was not this machine's choice, or null — said on the title.</summary>
        public string Ending { get; private set; }
```

(b) In `OnEnable`, replace

```csharp
            if (_session.Chapter != null)
            {
                RejoinTheCouch();
            }
```

with

```csharp
            // Dropped by the host (D61): the title, with the reason, rather than the chapter select a clean return lands on.
            Ending = _session.Net != null ? _session.Net.TakeEnding() : null;
            if (_session.Chapter != null && Ending == null)
            {
                RejoinTheCouch();
            }
```

(c) In `Repaint`, in the `Title` case, replace

```csharp
                    _text.Append("BATTLEBOMB\n\n");
```

with

```csharp
                    _text.Append("BATTLEBOMB\n\n");
                    if (Ending != null)
                    {
                        _text.Append(UiBuild.Tint(Ending, UiBuild.Worse)).Append("\n\n");
                    }
```

(d) In `LaunchNow`, replace

```csharp
            _session.DrawRunSeeds();
            SceneManager.LoadScene(GameplayScene, LoadSceneMode.Single);
```

with

```csharp
            _session.DrawRunSeeds();

            // D59: a solo game is open to platform friends unless the player closed it; a couch game is full and never is.
            // With no friends transport — a release without Steam — nothing opens, and nothing is created to try.
            NetSession net = _session.Net;
            if (net == null && NetSession.FriendsTransport != null)
            {
                net = NetSession.FindOrCreate();
            }

            if (net != null)
            {
                bool couch = _state.IsJoined(1) && !_state.IsRemote(1);
                net.OpenToFriends(!couch && !_session.ClosedToFriends);
            }

            SceneManager.LoadScene(GameplayScene, LoadSceneMode.Single);
```

In `Assets/_BattleBomb/UI/Chest/SettingsMenu.cs`:

(a) In `Toggle`, after the `ReturnToChapters` case add:

```csharp

                case SettingsRow.OpenToFriends:
                    ToggleOpenToFriends();
                    return;
```

(b) After Task 100's `LeaveTheGame` add:

```csharp

        /// <summary>
        /// D59: solo games are open to platform friends unless this says otherwise. On a solo host it takes effect at once —
        /// a guest already here stays — and it is written with the save at the next autosave. A couch game never opens,
        /// and a guest's machine has no game of its own to open.
        /// </summary>
        private void ToggleOpenToFriends()
        {
            Gameplay.Session.GameSession session = Gameplay.Session.GameSession.Find();
            if (session == null || _driver.IsReplica)
            {
                return;
            }

            session.ClosedToFriends = !session.ClosedToFriends;
            if (_driver.Players.LocalCount <= 1 && (session.Net != null || Gameplay.Net.NetSession.FriendsTransport != null))
            {
                Gameplay.Net.NetSession.FindOrCreate().OpenToFriends(!session.ClosedToFriends);
            }
        }
```

(c) In `Repaint`, after the `ReturnToChapters` case add:

```csharp

                    case SettingsRow.OpenToFriends:
                        Gameplay.Session.GameSession session = Gameplay.Session.GameSession.Find();
                        AppendToggle(row, "Open to friends", session == null || !session.ClosedToFriends);
                        break;
```

In `Assets/_BattleBomb/UI/Combat/NetBanner.cs`, replace the getter body of `Line`

```csharp
                // Looked for at most twice a second, never every frame (Plan 1's note about Find in OnGUI).
                if (_guest == null && Time.unscaledTime >= _nextLook)
                {
                    _guest = FindAnyObjectByType<NetGuest>();
                    _nextLook = Time.unscaledTime + 0.5f;
                }

                if (_guest != null && _guest.WaitingToAppear)
                {
                    return "Waiting for the host to reach a checkpoint room.";
                }

                return null;
```

with

```csharp
                // Looked for at most twice a second, never every frame (Plan 1's note about Find in OnGUI).
                if ((_guest == null || _session == null) && Time.unscaledTime >= _nextLook)
                {
                    if (_guest == null)
                    {
                        _guest = FindAnyObjectByType<NetGuest>();
                    }

                    if (_session == null)
                    {
                        _session = Gameplay.Session.GameSession.Find();
                    }

                    _nextLook = Time.unscaledTime + 0.5f;
                }

                NetSession net = _session != null ? _session.Net : null;
                if (net != null && net.HasProblem)
                {
                    // From one second of silence; at ten it is a drop (design §1).
                    return "Connection problem…";
                }

                if (net != null && net.Role == NetRole.Host && Time.unscaledTimeAsDouble - net.GuestLeftAt < LeftSeconds)
                {
                    return "Player 2 left.";
                }

                if (_guest != null && _guest.WaitingToAppear)
                {
                    return "Waiting for the host to reach a checkpoint room.";
                }

                return null;
```

and after the field `private float _nextLook;` add:

```csharp
        private Gameplay.Session.GameSession _session;

        /// <summary>How long the host is told its guest left.</summary>
        private const double LeftSeconds = 3.0;
```

The session outlives every scene, so once found it stays found; `OnSceneChanged` clears only `_guest`.

In `Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs`:

(a) After the field `private int _lag;` add:

```csharp
        private bool _friends;

        /// <summary>When a "go quiet" ends (unscaled seconds), or 0 — while it lasts this window's session is switched
        /// off, so it neither hears nor says anything: a dead link, for the banner and the ten-second drop.</summary>
        private float _quietUntil;
```

(b) Replace

```csharp
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 5f - 8f, width, row * 5f);
```

with

```csharp
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 7f - 8f, width, row * 7f);
```

(c) Replace

```csharp
            GameSession session = _session;
            NetSession net = session != null ? session.Net : null;
            NetRole role = net != null ? net.Role : NetRole.Offline;
```

with

```csharp
            GameSession session = _session;
            NetSession net = session != null ? session.Net : null;
            if (net != null && _quietUntil > 0f && Time.unscaledTime >= _quietUntil)
            {
                net.enabled = true;
                _quietUntil = 0f;
            }

            NetRole role = net != null ? net.Role : NetRole.Offline;
```

(d) Just before the line `GUILayout.EndArea();` add:

```csharp
            if (!_folded && GUILayout.Button(_friends ? "Friends: local socket" : "Friends: off", _style))
            {
                // D59's open-by-default, on the development transport: every solo launch hosts on the local socket, so
                // a second editor can drop in (Plan 3's Steam lobby fills the same slot).
                _friends = !_friends;
                NetSession.UseLocalFriends(_friends, _lag);
            }

            if (!_folded && net != null && role != NetRole.Offline)
            {
                if (_quietUntil > 0f)
                {
                    GUILayout.Label($"Quiet for {Mathf.CeilToInt(_quietUntil - Time.unscaledTime)} s", _style);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GoQuietButton(net, 4f);
                    GoQuietButton(net, 12f);
                    GUILayout.EndHorizontal();
                }
            }

```

(e) After the method `OnGUI` add:

```csharp

        /// <summary>Michael's pass (Task 105): this window goes silent for a few seconds, as a pulled cable would — the
        /// other window shows the banner after one and drops it after ten (design §1).</summary>
        private void GoQuietButton(NetSession net, float seconds)
        {
            if (GUILayout.Button($"Go quiet {seconds:0} s", _style))
            {
                net.enabled = false;
                _quietUntil = Time.unscaledTime + seconds;
            }
        }
```

Switching the session component off stops its `Update` — no pump, no keep-alive — and nothing else: `NetSession` has no `OnDisable`, so the connection itself is left exactly as it was.

- [ ] **Step 8: The headless guest can use a real socket**

In `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`, replace

```csharp
        internal static HeadlessGuest Join(INetTransport transport)
        {
            var go = new GameObject("Headless Guest");
            DontDestroyOnLoad(go);
            var guest = go.AddComponent<HeadlessGuest>();
            guest._transport = transport;
            transport.Connect("loopback");
            return guest;
        }
```

with

```csharp
        internal static HeadlessGuest Join(INetTransport transport) => Join(transport, "loopback");

        /// <summary>Over a real transport — a socket, for a host that must take a second guest after the first left.</summary>
        internal static HeadlessGuest Join(INetTransport transport, string address)
        {
            var go = new GameObject("Headless Guest");
            DontDestroyOnLoad(go);
            var guest = go.AddComponent<HeadlessGuest>();
            guest._transport = transport;
            transport.Connect(address);
            return guest;
        }
```

- [ ] **Step 9: Leaving, drops and opening, tested**

In `Assets/_BattleBomb/Tests/PlayMode/OnlineJoinSmokeTests.cs`:

(a) Add `using BattleBomb.Core.Saves;`, `using BattleBomb.Core.Progression;` and `using BattleBomb.UI.Combat;` to the usings, and after the field `private ScriptedCommandSource _hostInput;` add `private MemorySaveStore _store;`; in the set-up replace `session.Store = new MemorySaveStore();` with:

```csharp
            _store = new MemorySaveStore();
            session.Store = _store;
            NetSession.FriendsTransport = null;
```

and in the tear-down, before `yield return null;`, add `NetSession.FriendsTransport = null;`.

(b) Before `private IEnumerator LaunchSolo()` add:

```csharp
        [UnityTest]
        public IEnumerator A_guest_who_leaves_takes_their_body_and_the_solo_rules_come_back()
        {
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            _guest = HeadlessGuest.Join(guestSide);
            yield return LaunchTogether();

            _guest.Leave();
            yield return Until(() => _driver.Characters.Ordered.Count == 1, "the guest's body stayed in the world after they left");

            Assert.That(_driver.IsOnline, Is.False);
            Assert.That(net.Role, Is.EqualTo(NetRole.Host), "The host stopped listening; a friend could not drop in again.");
            Assert.That(Object.FindAnyObjectByType<NetBanner>().Line, Does.Contain("left"), "The host was not told its guest left.");
            _driver.HoldMenuPause(true);
            try
            {
                Assert.That(_driver.PausedForScreen, Is.True, "Alone again, a menu must pause the world as it always has (D42).");
            }
            finally
            {
                _driver.HoldMenuPause(false);
            }
        }

        [UnityTest]
        public IEnumerator After_a_guest_leaves_another_can_drop_in()
        {
            var hostSocket = new LocalSocketTransport(0);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSocket);
            Assert.That(net.Role, Is.EqualTo(NetRole.Host));
            string address = $"127.0.0.1:{hostSocket.BoundPort}";
            yield return LaunchSolo();

            _guest = HeadlessGuest.Join(new LocalSocketTransport(0), address);
            yield return UntilFrames(() => net.GuestReady, "the first guest never readied");
            yield return PushHostRight(() => _runner.Phase == StagePhase.AtCheckpoint, "the first checkpoint room");
            yield return Until(() => _driver.Characters.Ordered.Count == 2, "the first guest never dropped in");

            _guest.Leave();
            yield return Until(() => _driver.Characters.Ordered.Count == 1, "the first guest's body stayed");
            Object.Destroy(_guest.gameObject);
            yield return null;

            _guest = HeadlessGuest.Join(new LocalSocketTransport(0), address);
            yield return UntilFrames(() => net.GuestReady, "the second guest never readied");
            yield return Until(() => _driver.Characters.Ordered.Count == 2,
                "a second guest could not drop in where the first had left (the rejoin item)");
        }

        [UnityTest]
        public IEnumerator Silence_raises_the_banner_and_a_voice_clears_it()
        {
            HostOverLoopback(out LoopbackTransport guestSide);
            _guest = HeadlessGuest.Join(guestSide);
            yield return LaunchTogether();
            NetBanner banner = Object.FindAnyObjectByType<NetBanner>();

            _guest.Sending = false;
            yield return Until(() => banner.Line != null && banner.Line.Contains("Connection"), "a second of silence never raised the banner");
            _guest.Sending = true;
            yield return Until(() => banner.Line == null, "the banner stayed up once the guest spoke again");
        }

        [UnityTest]
        public IEnumerator A_solo_game_opens_to_friends_by_default_and_closes_when_told()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport unused);
            NetSession.FriendsTransport = () => hostSide;
            yield return LaunchSolo();

            NetSession net = GameSession.Find().Net;
            Assert.That(net, Is.Not.Null);
            Assert.That(net.Role, Is.EqualTo(NetRole.Host), "A solo game did not open to friends (D59).");

            net.OpenToFriends(false);
            Assert.That(net.Role, Is.EqualTo(NetRole.Offline), "Closing a game with nobody in it left it listening.");
            unused.Dispose();
        }

        [UnityTest]
        public IEnumerator A_couch_game_never_opens()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport unused);
            NetSession.FriendsTransport = () => hostSide;
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(1);
            flow.State.Confirm(0);
            flow.State.Confirm(1);
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Chapters));
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");

            NetSession net = GameSession.Find().Net;
            Assert.That(net == null || net.Role == NetRole.Offline, Is.True, "A couch game opened to friends; it is full (D59).");
            hostSide.Dispose();
            unused.Dispose();
        }

        [UnityTest]
        public IEnumerator When_the_host_drops_the_guest_lands_on_its_title_told_why_and_writes_nothing()
        {
            const int Start = 1000;
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1));
            recording.Add((Start - 120, writer.ToArray()));
            var bag = new Inventory();
            bag.Add(new ItemInstance(new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], requiredLevel: 1), 99);
            writer.Reset();
            ParticipantCodec.Write(writer, 1, 5, true, SaveMapper.Participant(
                bag.Sack, new Wallet(40), new CharacterState(ElementId.None, XpLedger.Fresh, bag), withSack: true));
            recording.Add((Start - 120, writer.ToArray()));
            for (int frame = Start; frame <= Start + 400; frame += NetProtocol.SnapshotEverySteps)
            {
                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                world.Players.Add(Standing(1, 2f));
                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            var playback = new PlaybackTransport(recording);
            NetSession.FindOrCreate().Join(playback, "playback");
            NetGuest guest = null;
            for (int i = 0; i < LoadFrameCeiling && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null);
            var driver = Object.FindAnyObjectByType<SimulationDriver>();
            yield return UntilFrames(() => driver.InventoryOf(1) != null && driver.InventoryOf(1).Inventory.Items.Count > 0,
                "the guest's copy of itself never arrived");

            playback.Disconnect(default);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Frontend", "the guest never went home");
            yield return null;

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Title));
            Assert.That(flow.Ending, Does.Contain("host"), "The guest's title never said why the game ended.");
            Assert.That(_store.Names(), Is.Empty, "The guest wrote a save on the way out; D61 says a drop costs what a crash does.");
        }

        /// <summary>A host with a guest seated in the lobby, launched together (Task 102's front door).</summary>
        private IEnumerator LaunchTogether()
        {
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.State.IsReady(1), "the guest never readied in the lobby");
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            _runner.SpawnsEnabled = false;
            yield return UntilFrames(() => _runner.IsStageLoaded, "the stage never streamed in");
            yield return UntilFrames(() => _driver.Frame > 5, "the launch hold never released");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(2));

            _host = _driver.Characters.Ordered[0];
            DisableDevices();
            _driver.Players.Unregister(_host.PlayerId);
            _hostInput = _host.gameObject.AddComponent<ScriptedCommandSource>();
            _hostInput.Bind(_host.PlayerId.Value);
            _driver.Players.Register(_hostInput);
        }
```

- [ ] **Step 10: Run the suites** — `recompile`, `console` `level: error` clean. Full EditMode (839). PlayMode (async): `OnlineJoinSmokeTests` (thirteen), `GuestReplicaSmokeTests` (its couch-comes-back case: a clean host loss still restores the couch — it now also restores the chapter), `OnlineHostSmokeTests` (`A_guest_who_leaves_stops_driving_their_body` — the body now leaves the world entirely: if that case reads the body's position after the leave, its object is inactive and the position is simply the last one, so it still holds; say so in the `DONE` if its wording needs a line), `SeatJoinSmokeTests`, `LootLoopSmokeTests`, `ChapterLoopSmokeTests`. Then the full PlayMode suite (100 + 6 = 106). Delete `Assets/InitTestScene*`.

- [ ] **Step 11: Live check (settled state, one editor — no QUIET).** Enter play mode on `Frontend`, open the development panel, tick **Friends: local socket**, launch solo: the panel says *Hosting — waiting for a guest*. Open settings: the *Open to friends* row is ticked; untick it — the panel says *Offline*; tick it again — hosting again. `editor_stop`.

- [ ] **Step 12: Commit** — subject `104: leaving, drops, and open by default`. Body: a guest who leaves or drops takes their body with them, so the host plays on solo under solo's rules and nothing holds a gate shut, and the host keeps listening for the next friend (D61); a host who leaves or drops sends the guest to its own title with the reason, writing nothing (what a crash costs); a banner from one second of silence, a drop at ten; solo games open to friends by default through a transport slot Plan 3's Steam lobby fills, a couch game never, and a settings row closes it, kept in the save (D59); a source leaving never takes a newer one with it, and a seat decided late is rebuilt.

---

### Task 105: Michael's pass (stage D) and the close-out — **needs QUIET**

Everything a harness can prove is proven by Tasks 97–104. What is left needs eyes, hands and a feel judgement in the second Unity window, which the bridge cannot click in — so it is Michael's, in one sitting, from `docs/team/m8-plan2-pass.md` (written by the Netcode lane alongside this plan; the orchestrator owns it from here). Two editors under Multiplayer Play Mode are heavy on the machine: this is the one task in Plan 2 that needs `QUIET`.

**Files:**
- Modify: `docs/team/builder.md` (the close-out draft). No code, unless a check fails — then see Step 4.

- [ ] **Step 1: The gates at the tip.** Full EditMode (expected 839) and full PlayMode, async (expected 106). Nothing uncommitted under `Assets/`. Record both counts for the close-out.

- [ ] **Step 2: Hand the sitting over.** Tell the orchestrator Plan 2's code is complete and the pass is ready: `docs/team/m8-plan2-pass.md`, about 30 minutes, keyboard only, needs `QUIET ON`. Do not reach the clone window any other way (Michael's standing "no" to screen control).

- [ ] **Step 3: Record the verdicts.** When the orchestrator relays Michael's results, put each section's verdict in the close-out draft (Step 5).

- [ ] **Step 4: A failed check is a fix task, not a note.** Triage it — which task's code, which file — and build it as `105a`, `105b`, … : red first where a harness can show it, then the fix, both gates, one `DONE` each. The pass is re-run for the failed checks only.

- [ ] **Step 5: The close-out draft** in `docs/team/builder.md`, in the shape of Plan 1's: a build log row per task (97–105, with commits), where the plan was wrong, what felt wrong to build, what was deferred, Michael's verdicts. HANDOFF-M8 and ROADMAP are the orchestrator's: the `DONE` carries, for them, (a) the build log rows; (b) this plan's "Where this plan departs from HANDOFF-M8" list, for the spec's planning decisions 11–17 and the message table; (c) the paper table's protocol row → **8**; (d) the ROADMAP §4 M8 line: *Stages C–D (menus and saves; joining and leaving) complete — `<97's commit>`..`<105's>`.*

- [ ] **Step 6: Commit** — subject `105: M8 stages C–D — Michael's pass and Plan 2's close-out`. `builder.md` only (plus any `105x` fixes, each already committed on its own).

---

## Checks for Michael (Stage C and Stage D)

Every check that needs eyes lives in **`docs/team/m8-plan2-pass.md`**, not in the tasks above — the tasks prove what a harness can. The sheet's sections, in the order of the sitting:

| Section | What it covers | From tasks |
|---|---|---|
| Setting up | Two windows; the Net panel's **Friends: local socket**; the joining window's own save | 104 |
| C1 — The guest's chest | Opens on the guest's own screen over the guest's own bag; the world keeps running; sell, equip, lock, upgrade, combine each show after a short round trip; A/X/Y wait while one is on its way | 97, 99 |
| C2 — The shop | The guest's rack; buying; the rack keeps its pieces through the visit | 98, 99 |
| C3 — Nothing pauses | The host's settings and chest never stop the guest; the host stands still while its own menu is up; each camera frames its own player at a chest | 100 |
| C4 — The guest's settings | Auto-sell toggles through the host; "Leave the game" | 100, 101 |
| C5 — Results and saves | The guest's results follow the host's; only the host's key leaves; afterwards the guest's own save has its loot, gold, level and the chapter's credit; the host's save has none of the guest's | 100, 101 |
| D1 — The lobby | The guest picks from its own save and readies; the host waits for it; a couch pair turns a guest away with the reason on both screens | 102 |
| D2 — Dropping in | Mid-fight: the guest waits with the message; at a checkpoint room the guest's game loads and they appear in the room; if the host walks on first, they appear at the next room | 103 |
| D3 — Leaving and drops | The guest leaves → the host plays on solo (a chest pauses again) and the guest can drop in again; the host leaves → the guest's title says so; pulling the plug on either side | 104 |
| D4 — At Bad lag | C1 and D2 once more at the Bad profile | all |

**Known and expected — the sheet lists them so they are not reported:** a guest waiting to appear cannot open its own settings (its body is not in the world yet — the Net panel's **Leave** is the way out); the banner and the lobby are placeholder text, restyled in M9; online in a release build nothing opens without Plan 3's Steam lobby; the guest's moves still take a round trip to show on its own screen (Plan 3's prediction).

---

## Self-review

### Spec coverage — HANDOFF-M8 Stages C and D, planning decisions 11–17

| Spec item | Task |
|---|---|
| 97 — requests seam, explicit player, `Sack.Revision`, the host's request phase, `RequestResult` | 97 |
| 98 — the rack in the simulation | 98 |
| 99 — `InventoryState` to the guest; the guest's chest, shop, hero panel | 99 (as `Participant`) |
| 100 — no pause online, local-player layout and camera, host-driven session moments | 100 (layout in 99) |
| 101 — two stashes, the save payload, each machine saves its own participant, `AutosaveNow`, chapter credit | 101 (payload in `LobbyPick`, `AutosaveNow` as `Moment`) |
| 102 — the lobby at character select, host-driven chapter select, launch both | 102 |
| 103 — drop-in: waiting, baseline, `BindLate`, appear-when-loaded | 103 |
| 104 — `Bye`, the banner, the 10 s rule, the host's live switch to solo, the guest's return to title, open-by-default with its setting | 104 |
| 105 — Michael's pass | 105 |
| PD 11 — requests seam; player explicit and overwritten by the host; revision; first phase; screens redraw from `Changed` | 97 |
| PD 12 — the rack rolled into the driver from the loot stream; buy = slot; price from the simulation | 98 |
| PD 13 — two stashes online, one on the couch; the binder assigns each player's | 101 (host), 99's design note (no guest-side second stash) |
| PD 14 — each machine saves only its own; the guest from its copy at the host's moments; resume point host-only | 101 |
| PD 15 — "local players on this display"; `LocalCount`; screens only for local players; no pause online; a solo host with nobody in is not online | 99, 100 |
| PD 16 — session moments are the host's; the guest's results show and follow | 100 |
| PD 17 — late join is a bind; only at a checkpoint room; appear once ready | 103 |
| D59 — two players any shape; couch full; open by default with a setting | 102, 104 |
| D60 — nothing pauses online | 100 |
| D61 — host holds the guest's gear; each keeps loot/XP/levels/gear/credit; host-only resume; leaving rules | 99, 101, 104 |
| Board: every Plan 2 item | see "Where the board's carried items land" above |

### Placeholder scan

No "TBD", "TODO", "later" or "similar to". Every code step shows the code. Every anchored edit quotes the text it replaces. Where a step depends on something a premise check may contradict — `System.IO.Compression` in Core (Task 99 Step 3) — the step says what to do and whom to ask.

### Type consistency — names defined here and where

- Core/Items: `PlayerRequestKind` (1–17), `PlayerRequest` (`Kind`, `PlayerId`, `Sequence`, `Revision`, `A`–`D`; factories; `For`, `WithSequence`, `WithRevision`, `NamesASackPlace`, `Target`), `RequestRefusal` (None, Refused, StaleSack, NoScreen, Busy), `RequestOutcome` (`Ok`, `Refusal`, `A`–`C`; `Done`, `No`, `From`) — 97. `Sack.Revision`, `Touch`, `AdoptRevision` — 97.
- Core/Saves: `SaveMapper.RestoreSack(save, sack, catalog, revision)` and `ClearLoadout` (99), `Participant` (99), `ParticipantFrom` (101); `SaveGame.ClosedToFriends`, `WithClosedToFriends` (104).
- Core/Net: `RequestCodec` (97); `NetDeflate`, `ParticipantCodec` (`Write`, `Read`, `WriteState`, `ReadState`), `ParticipantMessage` (99); `ScreenRecord`, `RackRecord`, `ReplicatedEvent.OfScreen`, `OfRack`, `IsMenuState` (99); `MomentKind` (ChapterCompleted, CheckpointReached, StageCompleted), `SessionCodec` (100); `LobbyPick`, `LobbyCodec.WritePick/ReadPick` (101), `LobbyState` (`Same`), `WriteLobby/ReadLobby` (102); `LoadStageMessage.Placed` (103); `LaunchMessage.DropIn` (103). `NetWriter.WriteBlob`, `NetReader.ReadBlob` (99). `NetMessageKind`: Request 14, RequestResult 15, Participant 16, Moment 17, LobbyPick 18, LobbyState 19. `NetProtocol`: `Version` 3→8, `MaxParticipantBytes`, `MaxParticipantJsonBytes`, `MaxRack`, `ParticipantMinSteps` (99), `RefusalGraceSeconds` (102).
- Core/Chapters: `FrontendState.SetRemote`, `IsRemote` (102); `GuestLobby` (102).
- Gameplay/Items: `IPlayerRequests`, `LocalPlayerRequests`, `PlayerRequestRunner.Run` (97; `DebugGrant` 100); `PlayerInventory.ApplyMirror` (99), `UseStash`, `MirroredFromHost` (101); `RequestBuy` internal (98).
- Gameplay/Players: `IRemotePlayerSource`, `PlayerRegistry.IsLocal`, `LocalCount` (99), `Unregister(IPlayerCommandSource)` (104); `InputSystemCommandSource.UseSeat` rebuilds (104).
- Gameplay/Simulation (`SimulationDriver`): `RequestRoute`, `RemoteRequestAnswered`, `RequestsFor`, `RequestClose`, `InventoryOf`, `QueueRemoteRequest`, `ApplyRemoteRequests` (97); `RackSize`, `RackFor`, `RackChanged`, `BuyFromRack`, `RollRack`, `ClearRack` (98); `ApplyReplicaScreen`, `ApplyReplicaRack`, `NearestInteractable`, `ApplyReplicaPlayerSide(id, grab, refused)`, `MayOpenScreen` removed (99); `IsReplica` public, `IsOnline` (100).
- Gameplay/World (`StageRunner`): `ReplicaChapterCompleted` (100), `TryDescribeForDropIn` (103).
- Gameplay/Session: `SessionBinder.GuestDefinition`, `RestoreGuest` (101), `BindLate` (103), `Unbind`, `_guestStash` (104); `GameSession.ClosedToFriends` (104); `SaveService.GuestCopyArrived` (101).
- Gameplay/Net: `RemotePlayerRequests` (97); `NetSession.GuestPick`, `GuestReady`, `GuestBrought`, `GuestLobbyChanged`, `SendLobbyPick` (101), `IsFull`, `SetFull`, `HostLobby`, `LobbyPick`, `LobbyReady`, `PublishLobby` (102), `JoinedMidRun` (103), `GuestLeftAt`, `TakeEnding`, `FriendsTransport`, `UseLocalFriends`, `ClosedToFriends`, `OpenToFriends`, `LocalGuestSave` (104); `NetHost.Begin(net, session, driver, runner, remote, binder)` (103); `NetGuest.Begin(net, driver, local, hidden)`, `WaitingToAppear` (103); `ReplicaWorld.HideUntilSeen`, `IsHidingLocal` (103).
- Presentation: `CameraRig.FramingOneScreen` (100).
- UI: `ChestScreenHost.RequestsFor`, `RackFor` (97, 98); `ChestScreen.OnRackChanged` (98, 99); `SettingsRow.OpenToFriends` (104); `FrontendFlow.Lobby` (102), `Ending` (104); `NetBanner` (`Line`) (103, 104).
- Tests: `HeadlessGuest.Results`, `SendRequest` (97), `Pick`, `Bring`, `AutoPick`, `SendPick` (101), `HostLobby`, `Refusal` (102), `Join(transport, address)` (104). Fixtures: `OnlineMenuSmokeTests` (97–102), `GuestMenuSmokeTests` (99–101), `OnlineJoinSmokeTests` (102–104).

### Test counts, task by task

| After | EditMode | PlayMode |
|---|---|---|
| f393d98 (baseline) | 791 | 65 |
| 97 | 806 | 69 |
| 98 | 807 | 71 |
| 99 | 817 | 80 |
| 100 | 820 | 87 |
| 101 | 825 | 93 |
| 102 | 834 | 96 |
| 103 | 836 | 100 |
| 104 | 839 | 106 |

The counts assume Task 96 adds no tests; a `96x` fix moves every row by what it adds.
