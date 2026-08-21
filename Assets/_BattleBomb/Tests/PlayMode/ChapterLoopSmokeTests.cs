using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Players;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Gameplay.World.Markers;
using BattleBomb.Platform;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D45's second tripwire, this one for M7's wiring: the front door, the stage stream, the
    /// checkpoint save, the airlock hand-over, the wipe, the resume, and the end of the chapter.
    /// Like its M6 sibling it asks exactly one question — is anything completely broken? — and it
    /// is not a feel check and never will be; that is Michael's job, by his standing rule.
    /// </summary>
    /// <remarks>
    /// D45 exists because M4 and M5 each shipped a bug a green EditMode suite could not see: the
    /// break was in Gameplay wiring rather than in logic, and M5's elemental leap was visibly
    /// dead in the game while 401 tests passed. M7's airlock had the same shape of hole — every
    /// check of it so far was an agent driving the live game by hand, which does not survive the
    /// session. This is that check, made permanent.
    /// <para>
    /// Two rules the suite lives by. First, it paces by <see cref="SimulationDriver.Frame"/> and
    /// not by render frames: a test run renders far faster than the fixed 60 Hz step, so a button
    /// set and released across two <c>yield return null</c>s can live and die inside one
    /// simulation step and never be sampled. Second, a screen holding the pause stops the step
    /// clock entirely, so every wait also carries a render-frame ceiling — otherwise the moment
    /// the results screen opens, a five-second check becomes a three-minute spin.
    /// </para>
    /// <para>
    /// The save goes to a <see cref="MemorySaveStore"/> the test owns and the session is destroyed
    /// after every case, so nothing here touches the player's disk or leaks a launch request into
    /// the next suite.
    /// </para>
    /// </remarks>
    public sealed class ChapterLoopSmokeTests
    {
        /// <summary>Simulation steps any single link may take: twenty seconds of game time.</summary>
        private const int PatienceSteps = 1200;

        /// <summary>A hard ceiling on render frames so a stalled simulation cannot hang the run.</summary>
        private const int FrameCeiling = 30000;

        /// <summary>Render frames a wait may spend while the world is paused, where the step clock
        /// is stopped and the step deadline can never arrive.</summary>
        private const int PausedFrameCeiling = 1500;

        /// <summary>
        /// How far a player who was given no orders at all may be found from where they were
        /// left. Loose enough to absorb the crowding nudge (task 37) two bodies give each other
        /// in passing, and still more than an order of magnitude under either teleport it exists
        /// to catch — both of which moved a player twelve units or more in a single step.
        /// </summary>
        private const float StoodStillTolerance = 0.5f;

        /// <summary>How far across the depth band player one steps to get out of their partner's
        /// lane. Comfortably wider than two personal radii and comfortably inside the band's
        /// three units, so the walk past never crowds and never hits the depth clamp.</summary>
        private const float LaneOffset = 2f;

        /// <summary>The starter knife — something real to carry through a wipe.</summary>
        private const int KnifeDefinitionId = 7;

        /// <summary>Data/Elements/Fire.asset's authored id — stage two's climate row names it.</summary>
        private const int FireElementId = 1;

        private const string SaveName = "smoke";
        private const string FrontendScene = "Frontend";
        private const string MachineScene = "Gameplay";
        private const string StageOneScene = "FixtureStage1";
        private const string StageTwoScene = "FixtureStage2";

        private GameSession _session;
        private MemorySaveStore _store;
        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _player;
        private PlayerInventory _bag;
        private ScriptedCommandSource _input;

        /// <summary>Player two, on the cases that join one; null on the rest. Given a source of
        /// its own but never a command — its whole job is to stand somewhere and stay there.</summary>
        private CharacterActor _partner;
        private ScriptedCommandSource _partnerInput;

        [UnitySetUp]
        public IEnumerator OpenTheFrontDoor()
        {
            _driver = null;
            _runner = null;
            _player = null;
            _bag = null;
            _input = null;
            _partner = null;
            _partnerInput = null;

            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            // Made before the scene loads, because FrontendFlow reads the save in its OnEnable —
            // a session handed a store one frame late would already have fallen back to the real
            // one on disk, which is the player's.
            _store = new MemorySaveStore();
            _session = GameSession.FindOrCreate();
            _session.Store = _store;
            _session.SaveName = SaveName;

            yield return Boot();
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            if (_input != null)
            {
                _input.Release();
            }

            if (_partnerInput != null)
            {
                _partnerInput.Release();
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        // ── The front door ───────────────────────────────────────────────────────────

        /// <summary>
        /// The whole boot: a title screen with no save offers no Continue, choosing through it
        /// launches the fixture into the machine, the stage geometry streams in, and walking into
        /// the first checkpoint room writes the run to the store (D52).
        /// </summary>
        [UnityTest]
        public IEnumerator The_front_door_launches_the_fixture_and_the_first_checkpoint_saves()
        {
            yield return LaunchFromTheFrontDoor(expectContinue: false);

            Assert.That(_runner.Chapter, Is.Not.Null, "The runner launched no chapter at all.");
            Assert.That(_runner.Chapter.Id, Is.EqualTo("fixture"),
                "The machine is not running the chapter the front door asked for.");
            Assert.That(_runner.StageIndex, Is.Zero, "A fresh launch starts on the chapter's first stage.");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1),
                "Nobody joined slot two, so the machine must hold exactly one player (D51).");
            Assert.That(SceneManager.GetSceneByName(StageOneScene).isLoaded, Is.True,
                "The stage's geometry scene never streamed in behind the launch (D48).");

            yield return Until(() => _runner.Phase == StagePhase.GateOpen, "the first arena's gate never opened");

            CheckpointRoomMarker room = RoomAfter(0);
            yield return WalkToX(room.EntryX + 0.5f, "the first checkpoint room");
            yield return Until(() => _runner.Run.CheckpointArena == 0, "the checkpoint was never reached");

            SaveService saves = Object.FindAnyObjectByType<SaveService>();
            Assert.That(saves, Is.Not.Null, "The machine has no SaveService, so nothing can autosave.");
            Assert.That(saves.Writes, Is.GreaterThan(0), "Reaching a checkpoint wrote nothing (D52).");
            Assert.That(_store.Exists(SaveName), Is.True, "The autosave never reached the store.");

            SaveLoad load = SaveCodec.Decode(Blob());
            Assert.That(load.Ok, Is.True, $"The autosave cannot be read back: {load.Reason}.");
            Assert.That(load.Save.Story.ResumeChapterId, Is.EqualTo("fixture"),
                "The save does not know which chapter the run is in.");
            Assert.That(load.Save.Story.ResumeStageIndex, Is.Zero);
            Assert.That(load.Save.Story.ResumeCheckpointArena, Is.Zero,
                "The save must name the room just entered, or Continue lands somewhere else (D49).");
        }

        // ── The airlock ──────────────────────────────────────────────────────────────

        /// <summary>
        /// The milestone's marquee feature (D48), and until now covered only by hand: the last
        /// checkpoint room is a loading airlock, the next stage streams in behind it while the
        /// players stand in it, walking out hands the machine over, and the stage behind them
        /// goes with its scene. Then D49's other half — a wipe with no checkpoint behind it
        /// restarts the stage at its own spawn, and takes none of the loot with it.
        /// </summary>
        [UnityTest]
        public IEnumerator The_airlock_hands_the_machine_over_and_a_wipe_keeps_the_loot()
        {
            yield return LaunchFromTheFrontDoor(expectContinue: false);
            yield return ClearInto(theRoomAfterArena: 0);

            ArenaMarker second = Arena(1);
            yield return WalkToX(second.MinX + 1f, "the second arena");
            yield return Until(() => _runner.Run.ArenaIndex == 1, "the second arena never began");
            yield return Until(() => _runner.Phase == StagePhase.GateOpen, "the second arena's gate never opened");

            CheckpointRoomMarker last = RoomAfter(1);
            yield return WalkToX(last.EntryX + 0.5f, "the last checkpoint room");
            yield return Until(() => _runner.Run.IsAirlock, "the last room never became the airlock");
            yield return Until(() => SceneManager.GetSceneByName(StageTwoScene).isLoaded,
                "the next stage never streamed in behind the airlock");

            // Something to keep across the wipe, taken through the real command pipe.
            _driver.SpawnDebugDrop(
                _player.Position + new Vector3(0.8f, 0f, 0f),
                _driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            yield return null;
            Assert.That(_driver.Pickups.Count, Is.GreaterThan(0), "The debug drop never landed.");

            int before = _bag.Inventory.SlotsUsed;
            yield return WalkTo(_driver.Pickups[0].Position, "the drop");
            yield return Press(CommandButtons.Light);
            yield return Until(() => _bag.Inventory.SlotsUsed > before, "the grab never landed");
            int kept = _bag.Inventory.SlotsUsed;

            // Out through the exit: the clamp has to reach into the stage that just landed, or
            // this walk stops dead at a door that will not open.
            StageExitMarker door = InStage<StageExitMarker>();
            yield return WalkToX(door.X + 1f, "past the stage exit");
            yield return Until(() => _runner.StageIndex == 1, "the machine never handed over to the next stage");
            yield return Until(() => !SceneManager.GetSceneByName(StageOneScene).isLoaded,
                "the finished stage never unloaded, so two stages' geometry are in the world at once");

            // The stage the run is now on is the one the chapter asset names, with its own
            // numbers, and the airlock hand-over has set the driver's encounter to match — this
            // reads driver.Encounter directly rather than re-deriving what the wiring was
            // supposed to produce, which is the only thing that actually proves FinishStage
            // called SetEncounter and not just OnStageReady (task 79).
            StageSpec stage = _runner.Run.Spec;
            Assert.That(stage.Id, Is.EqualTo("fixture-2"), "The hand-over kept the old stage's spec.");
            EncounterInputs effective = _driver.Encounter;
            Assert.That(effective.LevelStamp, Is.EqualTo(6), "Stage two's level stamp on Normal (D50).");
            Assert.That(effective.LootProgress, Is.EqualTo(3f), "Stage two's loot progress (D23/D50).");
            Assert.That(effective.Climate.For(new ElementId(FireElementId)), Is.EqualTo(1.25f).Within(0.001f),
                "Stage two's Fire climate multiplier (D41/D50) — the hand-over, not just the first adoption, must set it.");

            // The wipe (D49). Walk away from the spawn first so the reset has somewhere to move
            // the player back from, then take everyone down at once.
            Vector3 home = InStage<PlayerSpawnMarker>().PositionFor(0);
            yield return WalkToX(home.x + 4f, "somewhere past the stage spawn");
            _driver.DebugDownPlayers();
            yield return Until(
                () => !_player.Condition.IsDown && Vector3.Distance(_player.Position, home) < 2f,
                "the wipe never stood the player back up at the stage spawn");

            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(kept),
                "A wipe confiscated loot. D49 keeps it: it was never the run's to take back.");
            Assert.That(_runner.Run.ArenaIndex, Is.Zero,
                "No checkpoint had been reached in this stage, so a wipe restarts it at its first arena.");
        }

        /// <summary>
        /// D49 in Michael's words: "you will respawn at your most recent checkpoint or shopkeeper
        /// and have a chance to try to upgrade and equip stuff to go again." A run that came back
        /// <see cref="StagePhase.Fighting"/> in the arena past the room would spend that chance
        /// before the players stood up — its waves spawn while they are still in the room, and the
        /// clamp holding the room open for them holds those enemies in it too. So the wipe lands
        /// standing <em>in</em> the room, with nothing coming.
        /// </summary>
        [UnityTest]
        public IEnumerator A_wipe_stands_the_players_up_in_the_room_with_nothing_spawning()
        {
            yield return LaunchFromTheFrontDoor(expectContinue: false);
            yield return ClearInto(theRoomAfterArena: 0);

            CheckpointRoomMarker room = RoomAfter(0);
            Vector3 respawn = room.RespawnPosition;

            // Out of the room and into the fight past it, so the wipe has somewhere to come back from.
            ArenaMarker second = Arena(1);
            yield return WalkToX(second.MinX + 1f, "the second arena");
            yield return Until(() => _runner.Run.ArenaIndex == 1, "the second arena never began");

            _driver.DebugDownPlayers();
            yield return Until(() => !_player.Condition.IsDown, "the wipe never stood the player back up");

            Assert.That(_runner.Run.ArenaIndex, Is.Zero,
                "A wipe goes back to the arena the last checkpoint room follows (D49).");
            Assert.That(_runner.Phase, Is.EqualTo(StagePhase.AtCheckpoint),
                "A wipe comes back standing in the checkpoint room, not fighting in the arena past it (D49).");
            Assert.That(_runner.Run.Alive, Is.Zero,
                "Enemies are alive in the room the players just respawned in (D49).");
            Assert.That(Vector3.Distance(_player.Position, respawn), Is.LessThan(2f),
                "The wipe put the player somewhere other than the room's respawn point.");
        }

        // ── The couch ────────────────────────────────────────────────────────────────

        /// <summary>
        /// The first of M7's two shipped co-op bugs, made permanent. A player standing still in
        /// the arena was flung to the checkpoint room's edge — about fifteen units, the whole
        /// width of the arena plus the doorway — in a single simulation step, the instant their
        /// partner walked into the room without them.
        /// </summary>
        /// <remarks>
        /// The cause is worth stating in full, because the shape of it will come back. The
        /// checkpoint transition fires on <em>any</em> player reaching the room (it has to: a
        /// couch where one player must wait at the door for the other is a couch that deadlocks),
        /// and the version that shipped also raised the clamp's lower bound to the room's edge
        /// when it fired. <c>ArenaBounds.ClampHorizontal</c> is a <c>Mathf.Clamp</c> applied to
        /// every character every step, so raising the floor under someone does not walk them
        /// anywhere — it puts them there, in one step, at no speed. Measured before the fix:
        /// −7.5 to 8.0.
        /// <para>
        /// This is exactly the class of bug D45 exists for. Every rule involved is correct on
        /// paper and the whole EditMode suite is green through it; the break is two correct
        /// pieces of wiring meeting, and it takes a second body in the world to see at all —
        /// which is why nothing in this gate saw it until now.
        /// </para>
        /// </remarks>
        [UnityTest]
        public IEnumerator A_partner_left_in_the_arena_is_not_flung_when_the_other_reaches_the_room()
        {
            yield return LaunchFromTheFrontDoor(expectContinue: false, players: 2);
            yield return Until(() => _runner.Phase == StagePhase.GateOpen, "the first arena's gate never opened");

            // Player two is given no orders for the rest of this test. Everything below is
            // player one walking away from them.
            yield return StepOutOfTheirLane();

            CheckpointRoomMarker room = RoomAfter(0);
            Vector3 stayed = _partner.Position;
            Assert.That(stayed.x, Is.LessThan(room.MinX - 1f),
                "Player two has to still be back in the arena for this to mean anything — the "
                + $"room starts at x={room.MinX:F2} and they are at x={stayed.x:F2}.");

            yield return WalkToX(room.EntryX + 0.5f, "the first checkpoint room");
            yield return Until(() => _runner.Run.CheckpointArena == 0, "the checkpoint was never reached");
            yield return Steps(30);

            float moved = Vector3.Distance(stayed, _partner.Position);
            Assert.That(moved, Is.LessThan(StoodStillTolerance),
                $"Player two was standing still in the arena and moved {moved:F2} units "
                + $"(x {stayed.x:F2} → {_partner.Position.x:F2}) because player one walked into "
                + "the checkpoint room. Reaching a checkpoint must leave the cleared arena open "
                + "behind the players: the clamp is applied every step, so raising its floor to "
                + "the room's edge teleports whoever is behind it rather than walking them (D48).");
        }

        /// <summary>
        /// The second of the two, and the reason <c>AllPlayersPastX</c> counts the downed. A body
        /// on the floor of the checkpoint room slid out of it on its own — 11.6 to 14.0, through
        /// the doorway and into the arena — the moment the surviving partner walked out, because
        /// the check that closes the room behind them counted only players still standing.
        /// </summary>
        /// <remarks>
        /// The room is held open by the clamp's floor for as long as anyone is still in it (D49),
        /// and a body left behind is the case that needs that most: it cannot walk itself out, so
        /// closing the room on the living drags it through the door at clamp speed, which is to
        /// say instantly. Its partner then has to fight their way back to a corpse the room
        /// spat out.
        /// <para>
        /// The room only becomes "the room behind" on a wipe or a resume — walking forward
        /// through a checkpoint does not open it — so this case has to come through the wipe to
        /// exist at all. That is also the state a real one happens in: someone goes down, the
        /// other keeps going.
        /// </para>
        /// </remarks>
        [UnityTest]
        public IEnumerator A_downed_partner_is_not_dragged_out_of_the_room_when_the_other_leaves()
        {
            yield return LaunchFromTheFrontDoor(expectContinue: false, players: 2);
            yield return ClearInto(theRoomAfterArena: 0);

            // The wipe, which is what puts the room behind them and stands them both up in it.
            _driver.DebugDownPlayers();
            yield return Until(
                () => !_player.Condition.IsDown && !_partner.Condition.IsDown,
                "the wipe never stood both players back up");
            Assert.That(_runner.Phase, Is.EqualTo(StagePhase.AtCheckpoint),
                "The wipe did not come back standing in the checkpoint room (D49).");

            // Both stood up side by side in the room, and player one has to get past their
            // partner to leave it. Going round rather than through keeps the crowding nudge out
            // of a measurement that is about a body nobody touched.
            yield return StepOutOfTheirLane();
            yield return WalkToX(_partner.Position.x + 2f, "past their partner");

            // Now player two alone goes down. One player still standing means no second wipe:
            // the body simply lies there while its partner walks off.
            _driver.DebugDownPlayer(_partner.PlayerId.Value);
            yield return Steps(15);
            Assert.That(_partner.Condition.IsDown, Is.True,
                "Player two would not stay down, so there is no body to leave behind.");

            ArenaMarker second = Arena(1);
            Vector3 body = _partner.Position;
            Assert.That(body.x, Is.LessThan(second.MinX - 1f),
                "The body has to be inside the room for this to mean anything — the arena past "
                + $"it starts at x={second.MinX:F2} and the body is at x={body.x:F2}.");

            yield return WalkToX(second.MinX + 1f, "the second arena");
            yield return Until(() => _runner.Run.ArenaIndex == 1, "the second arena never began");
            yield return Steps(30);

            float slid = Vector3.Distance(body, _partner.Position);
            Assert.That(slid, Is.LessThan(StoodStillTolerance),
                $"A downed player slid {slid:F2} units out of the checkpoint room on their own "
                + $"(x {body.x:F2} → {_partner.Position.x:F2}) because their partner walked into "
                + "the next arena. The room stays inside the clamp until every player has left "
                + "it, downed included — a body cannot walk itself out, so closing the room on "
                + "the living is what drags it through the doorway (D49).");
        }

        // ── Continue ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// A save on disk offers Continue, and Continue resumes where the save says — the stage,
        /// its geometry, the checkpoint room inside it, and the couch's one shared wallet
        /// (D51/D52). Resuming into the wrong stage is the failure that costs a player their
        /// evening rather than their attempt.
        /// </summary>
        /// <remarks>
        /// The resume lands <em>at a checkpoint</em> rather than at the stage's plain spawn,
        /// because D49 makes a resume and a wipe the same boundary and the bug behind
        /// <see cref="A_wipe_stands_the_players_up_in_the_room_with_nothing_spawning"/> was
        /// reported as hitting "a wipe or a resume" identically: a run that came back
        /// <see cref="StagePhase.Fighting"/> in the arena past the room spawns its waves while
        /// the players are still standing in it, and the clamp holding the room open for them
        /// holds those enemies in it too. Only the wipe half of that was pinned; this is the
        /// other half, and it is the literal repro from the report.
        /// </remarks>
        [UnityTest]
        public IEnumerator Continue_resumes_the_stage_and_the_wallet_the_save_names()
        {
            // The last stage's only checkpoint room is the one after its final arena — the
            // fixture authors no other, and a room the stage has no marker for would be a
            // resume point nobody could stand in.
            const int ResumeCheckpointArena = 2;

            var progress = new StoryProgress();
            progress.SetResume("fixture", 1, ResumeCheckpointArena);
            SaveGame seeded = SaveMapper.Capture(
                new Sack(), new Wallet(77), new CharacterState[0], progress);
            _store.Write(SaveName, SaveCodec.Encode(seeded));

            yield return Boot();
            yield return LaunchFromTheFrontDoor(expectContinue: true);

            Assert.That(_runner.StageIndex, Is.EqualTo(1), "Continue did not resume at the saved stage.");
            Assert.That(SceneManager.GetSceneByName(StageTwoScene).isLoaded, Is.True,
                "The resumed stage's geometry is not the one that streamed in.");
            Assert.That(_bag.Wallet.Balance, Is.EqualTo(77),
                "The saved wallet never came back into the machine (D51).");

            Assert.That(_runner.Run.CheckpointArena, Is.EqualTo(ResumeCheckpointArena),
                "Continue did not resume at the checkpoint the save names (D49).");
            Assert.That(_runner.Run.ArenaIndex, Is.EqualTo(ResumeCheckpointArena),
                "A resume comes back at the arena its checkpoint room follows (D49).");
            Assert.That(_runner.Phase, Is.EqualTo(StagePhase.AtCheckpoint),
                "A resume comes back standing in the checkpoint room, not fighting in the arena "
                + "past it (D49).");
            Assert.That(_runner.Run.Alive, Is.Zero,
                "Enemies are alive in the room the players just resumed into (D49).");

            CheckpointRoomMarker room = RoomAfter(ResumeCheckpointArena);
            Assert.That(Vector3.Distance(_player.Position, room.RespawnPosition), Is.LessThan(2f),
                "The resume put the player somewhere other than the room's respawn point.");
        }

        // ── The lifecycle two ────────────────────────────────────────────────────────

        /// <summary>
        /// The guard that keeps a save alive, walked end to end (D52). A file this build read and
        /// <em>refused</em> — written by a newer version, or corrupt — is somebody's whole save
        /// sitting on disk in a shape we merely failed to parse, and the session that refused it
        /// starts with an empty stash. Autosaving that emptiness back would destroy the file
        /// within a minute of play, silently, after the player had done nothing but open the game
        /// once with the wrong build. So a refusal stops every write for the run, and the file is
        /// still byte-for-byte what it was when the run reached its first checkpoint.
        /// </summary>
        /// <remarks>
        /// The counterpart — a session with no save at all writing normally — is
        /// <see cref="The_front_door_launches_the_fixture_and_the_first_checkpoint_saves"/>.
        /// Together they are the two sides of <c>SaveService.CanWrite</c>, which had no runtime
        /// coverage at all: a guard that never fires and a guard that always fires look identical
        /// from EditMode.
        /// </remarks>
        [UnityTest]
        public IEnumerator A_refused_save_is_never_written_over()
        {
            string untouchable = SaveCodec.Encode(SaveGame.Fresh().WithVersion(SaveCodec.CurrentVersion + 1));
            _store.Write(SaveName, untouchable);

            yield return Boot();
            yield return LaunchFromTheFrontDoor(expectContinue: false);

            SaveService saves = Object.FindAnyObjectByType<SaveService>();
            Assert.That(saves, Is.Not.Null, "The machine has no SaveService.");
            Assert.That(saves.WritingBlocked, Is.True,
                "A save this build refused did not block writing, so the next checkpoint overwrites it (D52).");

            // The service says so once per run, loudly, and this is the assertion that it does.
            LogAssert.Expect(LogType.Error, new Regex("refusing to autosave"));

            yield return Until(() => _runner.Phase == StagePhase.GateOpen, "the first arena's gate never opened");
            CheckpointRoomMarker room = RoomAfter(0);
            yield return WalkToX(room.EntryX + 0.5f, "the first checkpoint room");
            yield return Until(() => _runner.Run.CheckpointArena == 0, "the checkpoint was never reached");

            Assert.That(saves.Writes, Is.Zero, "A refused save was written over at the first checkpoint (D52).");
            Assert.That(Blob(), Is.EqualTo(untouchable), "The refused file on disk was modified.");
        }

        /// <summary>
        /// The chapter ends, and the results screen that ends it never leaks the pause it holds.
        /// This is the only test that walks a whole chapter to its last exit, so it is also the
        /// only proof that a chapter can be finished at all: the final stage has no stage behind
        /// its exit, and that path — preload asked for a stage that does not exist, clamp stopping
        /// exactly on the exit line — exists nowhere else in the suite.
        /// </summary>
        /// <remarks>
        /// The leak is the failure worth pinning: <see cref="ResultsScreen"/> takes a counted hold
        /// on the driver's pause while it is up, and the object may be disabled and never come
        /// back. A hold left behind is a frozen game with no menu on screen — unrecoverable for
        /// the player, invisible to every EditMode test, and exactly the kind of lifecycle gap
        /// this milestone already shipped a bug from.
        /// </remarks>
        [UnityTest]
        public IEnumerator The_chapter_ends_and_the_results_screen_gives_its_pause_back()
        {
            yield return LaunchFromTheFrontDoor(expectContinue: false);

            // Stage one, both arenas, out through the airlock.
            yield return ClearInto(theRoomAfterArena: 0);
            yield return WalkToX(Arena(1).MinX + 1f, "the second arena");
            yield return Until(() => _runner.Run.ArenaIndex == 1, "the second arena never began");
            yield return ClearInto(theRoomAfterArena: 1);
            yield return WalkToX(InStage<StageExitMarker>().X + 1f, "past the first stage's exit");
            yield return Until(() => _runner.StageIndex == 1, "the machine never handed over to the second stage");

            // Stage two: three arenas, then its own last room.
            for (int arena = 1; arena < _runner.Run.Spec.ArenaCount; arena++)
            {
                yield return Until(() => _runner.Phase == StagePhase.GateOpen,
                    $"the gate out of arena {arena} never opened");
                yield return WalkToX(Arena(arena).MinX + 1f, $"arena {arena + 1} of the second stage");
                yield return Until(() => _runner.Run.ArenaIndex == arena, $"arena {arena + 1} never began");
            }

            yield return ClearInto(theRoomAfterArena: _runner.Run.Spec.ArenaCount - 1);

            ResultsScreen results = Object.FindAnyObjectByType<ResultsScreen>();
            Assert.That(results, Is.Not.Null, "The machine has no ResultsScreen, so a chapter cannot end.");

            // The last exit has nothing behind it, so the clamp stops on the line exactly and a
            // walk *to* a point past it would never arrive. Steer, and wait for the ending.
            StageExitMarker last = InStage<StageExitMarker>();
            yield return PushToX(last.X + 4f, () => results.IsOpen, "the end of the chapter");

            Assert.That(results.IsOpen, Is.True, "Walking out of the last stage did not end the chapter.");
            Assert.That(_driver.PausedForScreen, Is.True,
                "The results screen is up and the world is still running underneath it.");

            results.enabled = false;
            yield return null;
            Assert.That(_driver.PausedForScreen, Is.False,
                "A disabled results screen kept the pause. That is a frozen game with no menu on "
                + "screen, and the player cannot recover from it.");

            results.enabled = true;
            yield return null;
            Assert.That(_driver.PausedForScreen, Is.True,
                "The results screen came back up over a world nobody stopped.");
        }

        // ── The walk in ──────────────────────────────────────────────────────────────

        /// <summary>Loads the front door and checks it came up on the title. Called again by the
        /// cases that seed a save first, because the save is read in the flow's OnEnable.</summary>
        private IEnumerator Boot()
        {
            SceneManager.LoadScene(FrontendScene, LoadSceneMode.Single);

            // Two frames: one for the load to take, one for every OnEnable to run.
            yield return null;
            yield return null;

            // The devices are taken out of the loop here too: the front door reads the same
            // command stream the machine does, and a stray press would choose a screen for us.
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            Assert.That(flow, Is.Not.Null, "The Frontend scene has no FrontendFlow.");
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Title),
                "The front door did not come up on the title screen.");
        }

        /// <summary>
        /// Title → characters → chapters → launch, then take the machine over. The screens are
        /// driven through their state machine rather than through the devices: the command pipe
        /// is proven by the M6 suite and by every walk below, and what is under test here is the
        /// front door's own logic and the hand-off it makes to the machine.
        /// </summary>
        /// <param name="players">
        /// How many sit on the couch (D51). Two joins through the front door rather than by
        /// waking the machine's second player object by hand, because joining is the only path
        /// that makes the machine two-player everywhere at once: it is what fills the session's
        /// second character slot, which is what stops <see cref="SessionBinder"/> deactivating
        /// that object, which is what puts a second <see cref="CharacterActor"/> in the driver
        /// and a second body in front of every gate. Reaching past it would leave the parts that
        /// count players disagreeing with the parts that hold them.
        /// </param>
        private IEnumerator LaunchFromTheFrontDoor(bool expectContinue, int players = 1)
        {
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            Assert.That(flow.State.TitleOptions, Is.EqualTo(expectContinue ? 2 : 1),
                expectContinue
                    ? "A readable save exists, so the title must offer Continue."
                    : "No save the build can read, so the title must not offer Continue.");

            flow.State.Confirm(0);
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Characters),
                "Choosing on the title did not reach character select.");

            // Slot two joins with a press at character select, Castle Crashers style (D51), and
            // it has to join before slot one is readied: readying the last slot that is present
            // moves the screen on, and a join arriving after that would land on a screen with no
            // join in it. With a one-character roster both slots hold the same face, which is
            // fine — what these cases are about is two bodies, not two portraits.
            for (int slot = 1; slot < players; slot++)
            {
                flow.State.Confirm(slot);
                Assert.That(flow.State.IsJoined(slot), Is.True,
                    $"A press on device {slot + 1} did not join slot {slot + 1} to the couch (D51).");
            }

            flow.State.Confirm(0);
            for (int slot = 1; slot < players; slot++)
            {
                flow.State.Confirm(slot);
            }

            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Chapters),
                $"Readying all {players} player(s) did not reach chapter select.");
            Assert.That(flow.Selection.CanLaunch, Is.True,
                "The fixture chapter on the first tier must be launchable, or nothing can be played.");
            flow.State.Launch(flow.Selection.CanLaunch);

            yield return UntilFrames(
                () => SceneManager.GetActiveScene().name == MachineScene, "the machine never loaded");
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            Assert.That(_driver, Is.Not.Null, "The machine has no SimulationDriver.");
            Assert.That(_runner, Is.Not.Null, "The machine has no StageRunner.");

            // A quiet chapter, for the same reason the M6 suite runs one: this is a wiring
            // tripwire, not a fight. An encounter underneath it would stagger the player mid-press
            // and make a stray grunt and a genuine break the same red. With spawns off the run
            // still announces its waves and counts them cleared, so every gate opens on schedule.
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return UntilFrames(() => _runner.IsStageLoaded, "the stage's geometry never streamed in");

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            Assert.That(actors.Count, Is.EqualTo(players),
                $"{players} player(s) launched from the front door but the machine holds "
                + $"{actors.Count} (D51). A slot nobody sits in must not wake, and a slot "
                + "somebody does sit in must.");
            _player = actors[0];
            _bag = _player.GetComponent<PlayerInventory>();
            Assert.That(_bag, Is.Not.Null, "Player one has no inventory.");

            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            _input = Drive(_player);
            if (players > 1)
            {
                _partner = actors[1];
                _partnerInput = Drive(_partner);
                Assert.That(_partner.PlayerId.Value, Is.Not.EqualTo(_player.PlayerId.Value),
                    "Both players answer to the same id, so the couch is one player twice over.");
            }

            yield return null;
        }

        /// <summary>
        /// Takes one player's device out of the loop and binds a scripted source in its place —
        /// the same <c>IPlayerCommandSource</c> the device implements (rule 3), so every walk
        /// below goes through the real command pipe rather than around it.
        /// </summary>
        private ScriptedCommandSource Drive(CharacterActor actor)
        {
            _driver.Players.Unregister(actor.PlayerId);
            var source = actor.gameObject.AddComponent<ScriptedCommandSource>();
            source.Bind(actor.PlayerId.Value);
            _driver.Players.Register(source);
            return source;
        }

        /// <summary>Waits out an arena, then walks into the checkpoint room past it.</summary>
        private IEnumerator ClearInto(int theRoomAfterArena)
        {
            yield return Until(() => _runner.Phase == StagePhase.GateOpen,
                $"the gate out of arena {theRoomAfterArena + 1} never opened");
            CheckpointRoomMarker room = RoomAfter(theRoomAfterArena);
            yield return WalkToX(room.EntryX + 0.5f, $"the room after arena {theRoomAfterArena + 1}");
            yield return Until(() => _runner.Run.CheckpointArena == theRoomAfterArena,
                $"the room after arena {theRoomAfterArena + 1} was never reached");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private string Blob()
        {
            _store.TryRead(SaveName, out string text);
            return text;
        }

        private ArenaMarker Arena(int index) => InStage<ArenaMarker>(a => a.Index == index);

        private CheckpointRoomMarker RoomAfter(int arena) =>
            InStage<CheckpointRoomMarker>(r => r.AfterArena == arena);

        private T InStage<T>() where T : Component => InStage<T>(_ => true);

        /// <summary>
        /// Markers in the runner's <em>current</em> stage scene only. In the airlock two stages
        /// are loaded at once and both carry a full set of markers, so an unscoped search lets the
        /// stage ahead answer for the one the players are still standing in — and a walk to the
        /// wrong stage's exit is a walk into a wall with no explanation.
        /// </summary>
        private T InStage<T>(System.Func<T, bool> match) where T : Component
        {
            Scene scene = _runner.StageScene;
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True,
                $"No stage scene is loaded, so it cannot be searched for a {typeof(T).Name}.");

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T marker in root.GetComponentsInChildren<T>(true))
                {
                    if (match(marker))
                    {
                        return marker;
                    }
                }
            }

            Assert.Fail($"The current stage scene has no {typeof(T).Name} matching.");
            return null;
        }

        /// <summary>
        /// Walks player one across the depth band, out of the lane their partner is standing in.
        /// Two bodies inside <c>BodySeparation.PersonalRadius</c> of each other nudge apart every
        /// step (task 37), and a nudge is real movement — small, but the two cases above measure
        /// a partner who was told to stand still, and the only honest way to read "they did not
        /// move" is for nothing to have touched them. So player one goes round, not through.
        /// </summary>
        private IEnumerator StepOutOfTheirLane() =>
            WalkTo(
                new Vector3(_player.Position.x, _player.Position.y, _partner.Position.z + LaneOffset),
                "out of their partner's lane");

        /// <summary>One deliberate press, held long enough for the simulation to sample it.</summary>
        private IEnumerator Press(CommandButtons button)
        {
            _input.Set(Vector2.zero, button);
            yield return Steps(3);
            _input.Release();
            yield return Steps(2);
        }

        /// <summary>Waits until the simulation has advanced this many fixed steps. A paused world
        /// ends the wait — the command that caused the pause was already sampled to cause it.</summary>
        private IEnumerator Steps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < target; guard++)
            {
                yield return null;
                if (_driver.PausedForScreen)
                {
                    yield break;
                }
            }
        }

        private IEnumerator WalkToX(float x, string what) =>
            WalkTo(new Vector3(x, _player.Position.y, _player.Position.z), what);

        /// <summary>
        /// Walks the player onto a spot by steering, not teleporting. Slower than setting a
        /// position, and that is the point: it proves the motor, the arena clamp, and the runner's
        /// idea of where the gate is all still agree with each other. A marker outside the current
        /// clamp stalls this against the bounds and fails with a distance — which is the gate
        /// being wrong, and precisely what the walk is for.
        /// </summary>
        private IEnumerator WalkTo(Vector3 target, string what)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                Vector3 to = target - _player.Position;
                to.y = 0f;
                if (to.magnitude <= 0.45f)
                {
                    _input.Release();
                    yield return null;
                    yield break;
                }

                _input.Set(
                    new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f)),
                    CommandButtons.None);
                yield return null;
            }

            _input.Release();
            Assert.Fail($"The player never reached {what} — walked for {PatienceSteps} steps and "
                + $"stopped {Vector3.Distance(target, _player.Position):F2} away.");
        }

        /// <summary>
        /// Steers toward a point and stops when something happens, rather than on arrival. The
        /// chapter's last exit needs this: the clamp stops exactly on the exit line because a
        /// stage lays no floor past its own exit, so a walk *to* anywhere beyond it can never
        /// arrive even when everything works.
        /// </summary>
        private IEnumerator PushToX(float x, System.Func<bool> done, string what)
        {
            int deadline = _driver.Frame + PatienceSteps;
            int paused = 0;
            for (int guard = 0; guard < FrameCeiling; guard++)
            {
                if (done())
                {
                    _input.Release();
                    yield return null;
                    yield break;
                }

                if (_driver.PausedForScreen)
                {
                    if (++paused > PausedFrameCeiling)
                    {
                        break;
                    }
                }
                else if (_driver.Frame >= deadline)
                {
                    break;
                }

                float dx = x - _player.Position.x;
                _input.Set(new Vector2(Mathf.Clamp(dx, -1f, 1f), 0f), CommandButtons.None);
                yield return null;
            }

            _input.Release();
            Assert.Fail($"Steering toward {what} never got there — the player stopped at "
                + $"x={_player.Position.x:F2}, {PatienceSteps} steps in.");
        }

        /// <summary>
        /// Waits on the simulation clock, with a render-frame escape. A screen holding the pause
        /// stops the step clock, and a step deadline that can never arrive is what turns a
        /// five-second check into a three-minute spin.
        /// </summary>
        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + PatienceSteps;
            int paused = 0;
            for (int guard = 0; guard < FrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                if (_driver.PausedForScreen)
                {
                    if (++paused > PausedFrameCeiling)
                    {
                        break;
                    }
                }
                else if (_driver.Frame >= deadline)
                {
                    break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {PatienceSteps} steps).");
        }

        /// <summary>Waits on render frames alone, for the moments before there is a driver to
        /// count steps with: a scene load, and a stage still streaming in.</summary>
        private IEnumerator UntilFrames(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < PausedFrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {PausedFrameCeiling} frames).");
        }
    }
}
