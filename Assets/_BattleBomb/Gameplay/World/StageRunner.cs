using System;
using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Spatial;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Combat;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World.Markers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Gameplay.World
{
    /// <summary>
    /// Runs a chapter through the machine (D48). Streams each stage's geometry scene in
    /// additively, spawns waves when <see cref="StageRun"/> says so, clamps the players to the
    /// current arena and widens the clamp to open a gate, notices the checkpoint room, streams
    /// the next stage in behind the airlock, and sends a wipe back to the last checkpoint (D49).
    /// Every decision is the run's; this only does what it decided.
    /// </summary>
    /// <remarks>
    /// The stage under the players and the stage streaming in behind the airlock are both
    /// <see cref="LoadedStage"/> objects, so a hand-over swaps two references instead of copying
    /// eight fields between two parallel sets of state.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class StageRunner : MonoBehaviour
    {
        /// <summary>
        /// Re-exported from <see cref="LoadedStage"/>, which owns it, so the acceptance suite can
        /// name the convention from outside this assembly — <see cref="LoadedStage"/> is internal
        /// and the tests are not. One declaration, in the type that measures against it.
        /// </summary>
        public const float AuthoredFirstArenaMinX = LoadedStage.AuthoredFirstArenaMinX;

        [Tooltip("Driver this runner steps with. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("The spawner that puts waves into the world.")]
        [SerializeField] private EnemySpawner _spawner;

        [Tooltip("Launched when no GameSession brought a chapter — pressing Play in this scene.")]
        [SerializeField] private ChapterDefinition _defaultChapter;

        [Tooltip("Difficulty rows (D50), in order. Empty falls back to TierSpec.Defaults.")]
        [SerializeField] private TierDefinition[] _tiers = new TierDefinition[0];

        [Tooltip("Which of those rows a launch with no session runs at.")]
        [SerializeField] private int _defaultTierIndex;

        [Tooltip("Placed at every checkpoint room the stage asset asks for (D42).")]
        [SerializeField] private GameObject _chestPrefab;

        [Tooltip("Placed at rooms whose arena ends with a shopkeeper (D43).")]
        [SerializeField] private GameObject _shopkeeperPrefab;

        [Tooltip("Something to hit in a checkpoint room while deciding what to wear.")]
        [SerializeField] private GameObject _dummyPrefab;

        /// <summary>How often the players' positions are checked, as a throttle and nothing
        /// more: every transition it guards is one-way, so a check on every step would be
        /// correct too, just twice as much work for a result that changes twenty times a
        /// second at most.</summary>
        private const int PlayerCheckEverySteps = 3;

        /// <summary>How far into the next arena the players must be before the fight starts —
        /// far enough that standing on the seam does not flip the gate back and forth.</summary>
        private const float ArenaEntryMargin = 0.5f;

        private ChapterDefinition _chapter;
        private int _tierIndex;
        private TierSpec _tier;

        private LoadedStage _current;
        private LoadedStage _next;

        /// <summary>Bumped by every launch. A scene still in flight from an abandoned launch
        /// finds its generation stale when it lands and unloads itself.</summary>
        private int _generation;

        /// <summary>Where a launch abandoned mid-load by <see cref="OnDisable"/> picks itself
        /// back up. A pause or a backgrounded app is an ordinary lifecycle event on the platforms
        /// this has to stay viable for, so being disabled between asking for a scene and getting
        /// it must be recoverable rather than terminal. -1 means there is nothing to resume.</summary>
        private int _resumeStageIndex = -1;
        private int _resumeCheckpointArena = -1;

        /// <summary>Set once the stage behind the exit has been asked for. Deliberately *not*
        /// released by a wipe (D49): rewinding past the final arena would otherwise unload a
        /// stage that is already standing and stream it in again at the end of the re-fight,
        /// paying for the airlock twice to arrive at the state we were already in. It costs the
        /// memory of one resident stage for the length of one re-fight, and it can never load
        /// the same stage twice. Only a hand-over or a fresh launch clears it.</summary>
        private bool _preloadRequested;

        /// <summary>What the clamp was last built for. The run can open a gate on a step this
        /// runner did nothing else on — the arena's last enemy dying — and the clamp *is* the
        /// gate, so it has to follow every phase change, not only the ones caused here.</summary>
        private StagePhase _clampedPhase;
        private int _clampedArena = -1;

        /// <summary>The checkpoint room the players were last stood up in (D49), whether by a
        /// wipe or by resuming there. It stays inside the clamp until they have walked out:
        /// clamping players out of the room they are standing in does not walk them anywhere,
        /// it teleports them.</summary>
        private CheckpointRoomMarker _roomBehind;

        private bool _warnedBrokenSpawn;

        // Two latches, not one: a scene missing the arena ahead and a scene missing the arena
        // underneath are separate faults with separate fixes, and sharing a latch means the
        // second one a stage hits is never reported.
        private bool _warnedCannotLeaveArena;
        private bool _warnedClampStuck;

        /// <summary>Tests quiet the arena by turning this off: waves are announced and counted
        /// as already cleared, so gates open on schedule without a fight.</summary>
        public bool SpawnsEnabled { get; set; } = true;

        public StageRun Run => _current?.Run;

        /// <summary>The phase of the stage under the players, or null when none is running —
        /// before the first launch there is no chapter to be part-way through, and saying
        /// "complete" there would be a lie a save could act on.</summary>
        public StagePhase? Phase => _current?.Run.Phase;

        public ChapterDefinition Chapter => _chapter;

        /// <summary>Where in the chapter the players are, or -1 before a launch.</summary>
        public int StageIndex => _current != null ? _current.StageIndex : -1;

        public int TierIndex => _tierIndex;

        public TierSpec Tier => _tier;

        public bool IsStageLoaded => _current != null && _current.IsReady;

        /// <summary>The stage scene currently under the players, for anything that must live and
        /// die with it.</summary>
        public Scene StageScene => _current != null ? _current.Scene : default;

        /// <summary>Raised when the players reach a checkpoint room: stage index and the arena it
        /// follows. Intended for the save service to autosave on (D52, task 81).</summary>
        public event Action<int, int> CheckpointReached;

        /// <summary>Raised when a stage is left behind through its airlock.</summary>
        public event Action<int> StageCompleted;

        /// <summary>Raised when the chapter's last stage is left behind.</summary>
        public event Action ChapterCompleted;

        /// <summary>Starts a chapter at a stage, optionally resuming at a checkpoint room (D49's
        /// respawn rule doubles as the resume rule). Anything already running is unloaded first,
        /// including a scene still in flight.</summary>
        public void Launch(ChapterDefinition chapter, int stageIndex, int tierIndex, int resumeCheckpointArena = -1)
        {
            StageDefinition definition = chapter != null ? chapter.StageAt(stageIndex) : null;
            if (definition == null)
            {
                Debug.LogError($"{name}: nothing to launch — no chapter, or no stage {stageIndex} in it.", this);
                return;
            }

            UnloadEverything();
            if (_spawner != null)
            {
                _spawner.ResetBrood();
            }

            TierSpec[] tiers = TierDefinition.ToRuntime(_tiers);
            _chapter = chapter;
            _tierIndex = Mathf.Clamp(tierIndex, 0, Mathf.Max(0, tiers.Length - 1));
            _tier = tiers[_tierIndex];
            _current = new LoadedStage(
                definition,
                new StageRun(definition.ToRuntime(), resumeCheckpointArena),
                stageIndex,
                _generation);
            BeginLoad(_current, null);
        }

        // ── Loading ──────────────────────────────────────────────────────────────────

        private void BeginLoad(LoadedStage stage, float? firstArenaMinX)
        {
            string sceneName = stage.Definition.ToRuntime().GeometryScene;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"{name}: stage scene '{sceneName}' is not in the build settings.", this);
                return;
            }

            // The handle the load just created, taken now rather than looked up by name when it
            // lands: a chapter may reuse a geometry scene, and GetSceneByName cannot tell two
            // scenes sharing a name apart.
            Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            op.completed += _ =>
            {
                if (this == null)
                {
                    return;
                }

                if (stage.Generation != _generation)
                {
                    // The launch this belonged to was abandoned while the scene was in flight.
                    // Nothing else knows this scene exists, so it has to clean up after itself.
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        SceneManager.UnloadSceneAsync(scene);
                    }

                    return;
                }

                if (firstArenaMinX.HasValue)
                {
                    stage.AdoptAt(scene, firstArenaMinX.Value);
                }
                else
                {
                    stage.Adopt(scene);
                }

                if (stage == _current)
                {
                    OnStageReady();
                }
                else if (stage == _next && _current != null && _current.IsReady)
                {
                    // The airlock is open: the clamp reaches into the stage that just landed.
                    ApplyBounds();
                }
            };
        }

        private void OnStageReady()
        {
            _current.SpawnProps(_chestPrefab, _dummyPrefab, _shopkeeperPrefab);

            // A resume stands the players up in the room it saved at, and the run comes up
            // AtCheckpoint there (D49's respawn rule doubles as the resume rule). The room is
            // theirs until every one of them has walked into the arena past it.
            _roomBehind = _current.RoomAfter(_current.Run.CheckpointArena);

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                Vector3 at = _roomBehind != null
                    ? _roomBehind.RespawnPosition + PartnerOffset(i)
                    : _current.Spawn != null ? _current.Spawn.PositionFor(i) : FallbackSpawn(i);
                actors[i].PlaceAt(at);
                actors[i].SetSpawnPoint(at);

                // A launch is a beginning, so everyone stands up for it. Moving a downed player
                // to the new spawn and leaving them on the floor would start a chapter with the
                // attempt countdown already running — two of them and it is an instant wipe loop
                // before the first command is read. This is the launch path only: walking into
                // the stage after this one is a hand-over, and must not silently full-heal
                // anybody (that path is FinishStage, which sets homes and nothing else).
                // After SetSpawnPoint, never before: the reset stands them at the spawn point.
                actors[i].ResetForAttempt();
            }

            _driver.SetEncounter(EncounterInputs.From(_tier, _current.Run.Spec));
            ApplyBounds();
        }

        // ── Stepping ─────────────────────────────────────────────────────────────────

        private void OnStepped(int frame)
        {
            if (_current == null || !_current.IsReady)
            {
                return;
            }

            StageRun run = _current.Run;
            if (run.Phase == StagePhase.Complete)
            {
                return;
            }

            run.Step();
            while (run.TryNextWave(out WaveSpec wave))
            {
                run.OnEnemiesSpawned(SpawnWave(wave));
            }

            if (run.Phase != _clampedPhase || run.ArenaIndex != _clampedArena)
            {
                ApplyBounds();
            }

            if (!_preloadRequested && ExitIsNext(run))
            {
                _preloadRequested = true;
                PreloadNextStage();
            }

            if (frame % PlayerCheckEverySteps == 0)
            {
                StepPlayerPositions();
            }
        }

        /// <summary>The stage's exit is the next thing ahead of the players: standing in the last
        /// checkpoint room (D48's airlock), or — for a final arena an author gave no room — the
        /// moment its gate opens. Either way the stage after it has to be on its way before
        /// anyone reaches the line, or they walk out of the chapter early.</summary>
        private static bool ExitIsNext(StageRun run) =>
            run.IsAirlock
            || (run.Phase == StagePhase.GateOpen && run.IsFinalArena && !run.NextIsCheckpoint);

        private int SpawnWave(in WaveSpec wave)
        {
            if (!SpawnsEnabled)
            {
                // Deliberately quiet (the smoke suites): the wave is announced and counted as
                // cleared, so the gates open on schedule without a fight.
                return 0;
            }

            StageRun run = _current.Run;
            ArenaMarker arena = _current.Arena(run.ArenaIndex);
            EnemyDefinition definition = _current.Definition.EnemyAt(wave.EnemyIndex);
            if (_spawner == null || arena == null || definition == null)
            {
                string cause = _spawner == null ? "no spawner is wired to this runner"
                    : arena == null ? $"the scene has no marker for arena {run.ArenaIndex}"
                    : $"the stage roster has no enemy {wave.EnemyIndex}";
                WarnOnce(ref _warnedBrokenSpawn,
                    $"{name}: nothing can spawn in '{_current.Definition.name}' — {cause}. Its "
                    + "waves count as cleared the moment they are due, so the stage becomes a "
                    + "walking tour rather than failing.");
                return 0;
            }

            return _spawner.SpawnWave(
                definition, wave.Count, arena.SpawnPoints,
                _tier.HealthMultiplier, _tier.DamageMultiplier);
        }

        private void StepPlayerPositions()
        {
            StageRun run = _current.Run;

            // Not before the run has left the room: standing in it, ArenaIndex is the arena the
            // room *follows*, and everyone in the room is trivially past that arena's floor — so
            // asking there would release the room on the first check, while they are still in it.
            if (_roomBehind != null && run.ArenaIndex > _roomBehind.AfterArena)
            {
                ArenaMarker here = _current.Arena(run.ArenaIndex);

                // Every player, downed included. A body left behind in the room is exactly the
                // case that most needs the room to stay inside the clamp — closing it on the
                // living would drag the downed one through the doorway.
                if (here != null && AllPlayersPastX(here.MinX))
                {
                    _roomBehind = null;
                    ApplyBounds();
                }
            }

            switch (run.Phase)
            {
                case StagePhase.GateOpen:
                    if (run.NextIsCheckpoint)
                    {
                        CheckpointRoomMarker room = _current.RoomAfter(run.ArenaIndex);
                        if (room != null && AnyLivingPlayerPastX(room.EntryX))
                        {
                            run.ReachCheckpoint();
                            SetHome(room.RespawnPosition);
                            ApplyBounds();
                            CheckpointReached?.Invoke(_current.StageIndex, run.ArenaIndex);
                        }
                    }
                    else if (!run.IsFinalArena)
                    {
                        TryEnterNextArena(run);
                    }
                    else
                    {
                        // A final arena an author gave no room: the exit is the airlock line.
                        TryLeaveStage();
                    }

                    break;

                case StagePhase.AtCheckpoint:
                    if (run.IsAirlock)
                    {
                        TryLeaveStage();
                    }
                    else
                    {
                        TryEnterNextArena(run);
                    }

                    break;
            }
        }

        private void TryEnterNextArena(StageRun run)
        {
            ArenaMarker next = NextArena();
            if (next == null)
            {
                WarnOnce(ref _warnedCannotLeaveArena,
                    $"{name}: '{_current.Definition.name}' has no marker for arena "
                    + $"{run.ArenaIndex + 1} — the players cannot leave arena {run.ArenaIndex}.");
                return;
            }

            if (AllLivingPlayersPastX(next.MinX + ArenaEntryMargin))
            {
                run.EnterNextArena();
                ApplyBounds();
            }
        }

        /// <summary>Cross the exit line once the stage behind it is ready — or, on the chapter's
        /// last stage, as soon as everyone is through.</summary>
        private void TryLeaveStage()
        {
            // This can only ever fire because ApplyBounds clamps to this same Exit.X and
            // ArenaBounds.ClampHorizontal has no character-radius inset, so Mathf.Clamp rests a
            // player on the line bit-for-bit. Pad one site and not the other and the clamp stops
            // short of the line this reads: the stage sticks shut at the door, or the overrun
            // this commit removed comes back to make it reachable.
            bool ready = _next == null || _next.IsReady;
            if (ready && _current.Exit != null && AllLivingPlayersPastX(_current.Exit.X))
            {
                FinishStage();
            }
        }

        /// <summary>The marker for the next arena the <em>run</em> believes in. A scene carrying
        /// more arena markers than the stage asset has arenas must not out-vote the asset
        /// (rule 2: gameplay lives in stage data, never in the scene).</summary>
        private ArenaMarker NextArena()
        {
            StageRun run = _current.Run;
            return run.ArenaIndex + 1 < run.Spec.ArenaCount ? _current.Arena(run.ArenaIndex + 1) : null;
        }

        private void SetHome(Vector3 position)
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                actors[i].SetSpawnPoint(position + PartnerOffset(i));
            }
        }

        /// <summary>The clamp is the gate (D48): fighting holds the arena; an open gate reaches
        /// into the room or the next arena; the airlock reaches into the stage behind it.</summary>
        /// <remarks>
        /// Where nothing is ahead, the clamp stops at the exit line exactly, because that is where
        /// this stage's ground stops — a stage lays no floor past its own exit, so that the next
        /// stage's floor can begin there without two of them fighting over the same plane
        /// (<c>No_stage_scene_lays_ground_past_its_own_exit</c>). Any reach past the line is a walk
        /// onto nothing, and on a chapter's last stage it is permanent: the run goes
        /// <see cref="StagePhase.Complete"/> and the clamp is never revised again.
        /// </remarks>
        private void ApplyBounds()
        {
            StageRun run = _current.Run;
            ArenaMarker arena = _current.Arena(run.ArenaIndex);
            if (arena == null)
            {
                WarnOnce(ref _warnedClampStuck,
                    $"{name}: '{_current.Definition.name}' has no marker for arena "
                    + $"{run.ArenaIndex} — the clamp is stuck wherever it last was.");
                return;
            }

            float minX = arena.MinX;
            float maxX = arena.MaxX;
            ArenaMarker next = NextArena();
            switch (run.Phase)
            {
                case StagePhase.GateOpen:
                    CheckpointRoomMarker ahead = run.NextIsCheckpoint ? _current.RoomAfter(run.ArenaIndex) : null;
                    if (ahead != null)
                    {
                        maxX = ahead.MaxX;
                    }
                    else if (next != null)
                    {
                        maxX = next.MaxX;
                    }
                    else if (_current.Exit != null)
                    {
                        maxX = _current.Exit.X;
                    }

                    break;

                case StagePhase.AtCheckpoint:
                    // The cleared arena stays open behind them. Raising the floor to the room's
                    // edge would clamp — which is to say teleport — a partner still standing in
                    // that arena, the instant the other player stepped into the room.
                    if (next != null)
                    {
                        maxX = next.MaxX;
                    }
                    else if (_current.Exit != null)
                    {
                        maxX = _current.Exit.X;
                    }

                    break;

                case StagePhase.Fighting:
                    // The arena alone: the fight begins the moment they cross into it.
                    break;
            }

            // The airlock (D48): once the next stage is standing behind the exit line, the clamp
            // reaches into its first arena, and walking out of this stage is simply walking. Its
            // floor is what makes that reach walkable, which is why this is the only thing allowed
            // to move the clamp past the exit — and why the condition is the same
            // ExitIsNext the preload uses, so a final arena an author gave no room hands over as
            // smoothly as one that ends in a checkpoint.
            if (ExitIsNext(run) && _next != null && _next.IsReady)
            {
                ArenaMarker landing = _next.Arena(0);
                if (landing != null)
                {
                    maxX = Mathf.Max(maxX, landing.MaxX);
                }
            }

            if (_roomBehind != null)
            {
                minX = Mathf.Min(minX, _roomBehind.MinX);
            }

            _clampedPhase = run.Phase;
            _clampedArena = run.ArenaIndex;
            _driver.SetArena(new ArenaBounds(minX, maxX, arena.Bounds.GroundY));
        }

        private bool AnyLivingPlayerPastX(float x)
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (!actors[i].Condition.IsDown && actors[i].Position.x >= x)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AllLivingPlayersPastX(float x)
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            bool any = false;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].Condition.IsDown)
                {
                    continue;
                }

                any = true;
                if (actors[i].Position.x < x)
                {
                    return false;
                }
            }

            return any;
        }

        /// <summary>Standing and downed alike — used only where a body left behind should count
        /// as still being there, because it is.</summary>
        private bool AllPlayersPastX(float x)
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            if (actors.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].Position.x < x)
                {
                    return false;
                }
            }

            return true;
        }

        // ── The airlock ──────────────────────────────────────────────────────────────

        private void PreloadNextStage()
        {
            StageDefinition next = _chapter != null ? _chapter.StageAt(_current.StageIndex + 1) : null;
            if (next == null)
            {
                // The chapter's last stage: nothing behind the exit but the results screen.
                return;
            }

            if (_current.Exit == null)
            {
                Debug.LogError(
                    $"{name}: '{_current.Definition.name}' has no exit marker, so '{next.name}' "
                    + "has nowhere to begin and the chapter cannot go on.", this);
                return;
            }

            _next = new LoadedStage(
                next, new StageRun(next.ToRuntime()), _current.StageIndex + 1, _generation);
            BeginLoad(_next, _current.Exit.X);
        }

        private void FinishStage()
        {
            int finished = _current.StageIndex;
            _current.Run.EnterNextArena();
            StageCompleted?.Invoke(finished);

            if (_next == null)
            {
                StageDefinition ahead = _chapter != null ? _chapter.StageAt(finished + 1) : null;
                if (ahead != null)
                {
                    Debug.LogError(
                        $"{name}: stage {finished} was left behind but '{ahead.name}' never "
                        + "streamed in — the chapter is about to be reported finished with "
                        + "stages still in it.", this);
                }

                // The geometry stays under their feet: there is nowhere else to stand, and the
                // next launch unloads it. What ends here is the chapter, not the scene.
                ChapterCompleted?.Invoke();
                return;
            }

            // Hand over: the stage ahead becomes the stage, and the one behind goes with its
            // props, its markers, and its scene together.
            LoadedStage previous = _current;
            _current = _next;
            _next = null;
            _preloadRequested = false;
            _roomBehind = null;
            if (_spawner != null)
            {
                _spawner.ResetBrood();
            }

            _current.SpawnProps(_chestPrefab, _dummyPrefab, _shopkeeperPrefab);
            SetHomeToStageSpawn();
            _driver.SetEncounter(EncounterInputs.From(_tier, _current.Run.Spec));
            ApplyBounds();
            previous.Unload();
        }

        private void SetHomeToStageSpawn()
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                actors[i].SetSpawnPoint(_current.Spawn != null
                    ? _current.Spawn.PositionFor(i)
                    : FallbackSpawn(i));
            }
        }

        // ── Wipes ────────────────────────────────────────────────────────────────────

        /// <summary>The driver already stood everyone back up at their spawn point (the last
        /// room's respawn) and the spawner already cleared its brood; the run rewinds and the
        /// clamp follows (D49). A wipe in the airlock leaves the preloaded next stage where it
        /// is — the run has nothing to rewind there and says so by not moving.</summary>
        private void OnAttemptReset()
        {
            if (_current == null || !_current.IsReady || _current.Run.Phase == StagePhase.Complete)
            {
                return;
            }

            int checkpoint = _current.Run.ResetToCheckpoint();
            _roomBehind = _current.RoomAfter(checkpoint);
            ApplyBounds();
        }

        private void OnEnemyDied(EnemyDeath death)
        {
            _current?.Run.OnEnemyDied();
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_spawner == null)
            {
                _spawner = FindAnyObjectByType<EnemySpawner>();
            }

            if (_driver == null)
            {
                Debug.LogError($"{name}: no SimulationDriver — no stage can run.", this);
                return;
            }

            _driver.Stepped += OnStepped;
            _driver.EnemyDied += OnEnemyDied;
            _driver.AttemptReset += OnAttemptReset;

            if (_current != null)
            {
                if (_current.IsReady)
                {
                    // A stage the players are standing in the middle of. Relaunching it here
                    // would throw away the fight they are half-way through to fix nothing.
                    return;
                }

                // A plan whose scene never landed and that OnDisable did not get to discard.
                // Take its resume point; the relaunch below unloads the plan on its way past.
                _resumeStageIndex = _current.StageIndex;
                _resumeCheckpointArena = _current.Run.CheckpointArena;
            }

            if (_chapter != null && _resumeStageIndex >= 0)
            {
                // A launch OnDisable abandoned before its scene landed. Resume it where D49 says
                // a lost attempt resumes: the last checkpoint room reached, or the stage's own
                // spawn when there was none. This outranks the session: an interrupted run in
                // this scene is further along than the request that started it.
                Launch(_chapter, _resumeStageIndex, _tierIndex, _resumeCheckpointArena);
                return;
            }

            // Three sources, in order of how much they know. A run already under way (above); the
            // front door's request, which is what a real boot arrives with (D51, task 80); and the
            // authored fallback, which is what pressing Play in this scene with no session gets.
            Session.GameSession session = Session.GameSession.Find();
            if (session != null && session.Chapter != null)
            {
                Launch(session.Chapter, session.StageIndex, session.TierIndex, session.ResumeCheckpointArena);
            }
            else if (_defaultChapter != null)
            {
                Launch(_defaultChapter, 0, _defaultTierIndex);
            }
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.Stepped -= OnStepped;
                _driver.EnemyDied -= OnEnemyDied;
                _driver.AttemptReset -= OnAttemptReset;
            }

            // Two halves, and the second is what makes the first survivable.
            //
            // The generation moves on so a scene still in flight belongs to nobody and unloads
            // itself when it lands. Destroying the runner was already covered by the null check in
            // BeginLoad; disabling it was not, and a load landing after this point adopted a
            // scene, spawned its props and teleported both players while this was unsubscribed
            // from every step that would have made sense of it.
            _generation++;

            // But abandoning the load does not abandon the plan that asked for it, and a stage
            // that never became ready is a plan with nothing behind it. Left in place it is
            // permanent and silent: a _current that is never ready means IsStageLoaded is false
            // and OnStepped bails every frame with nothing logged, and a _next that is never
            // ready means TryLeaveStage's guard never passes and the players stand at the door
            // of a finished stage forever. So discard them, and let OnEnable relaunch.
            if (_current != null && !_current.IsReady)
            {
                _resumeStageIndex = _current.StageIndex;
                _resumeCheckpointArena = _current.Run.CheckpointArena;
                _current.Unload();
                _current = null;
            }

            if (_next != null && !_next.IsReady)
            {
                _next.Unload();
                _next = null;

                // Released with it, or the airlock has already spent its one request and can
                // never ask for that stage again.
                _preloadRequested = false;
            }
        }

        private void UnloadEverything()
        {
            // Bumped first: a scene still in flight belongs to the launch being abandoned, and
            // the generation is how its completion learns that.
            _generation++;
            _next?.Unload();
            _next = null;
            _current?.Unload();
            _current = null;

            _roomBehind = null;
            _preloadRequested = false;
            _resumeStageIndex = -1;
            _resumeCheckpointArena = -1;
            _clampedPhase = default;
            _clampedArena = -1;
            _warnedBrokenSpawn = false;
            _warnedCannotLeaveArena = false;
            _warnedClampStuck = false;
        }

        private void WarnOnce(ref bool alreadyWarned, string message)
        {
            if (alreadyWarned)
            {
                return;
            }

            alreadyWarned = true;
            Debug.LogError(message, this);
        }

        /// <summary>Side by side rather than inside each other, so two players standing up at
        /// one respawn point do not have to push apart first.</summary>
        private static Vector3 PartnerOffset(int slot) => new Vector3(slot * 1.2f - 0.6f, 0f, 0f);

        /// <summary>Where players go in a stage scene that forgot its spawn marker: just inside
        /// the first arena, so at least the stage is playable while someone fixes the scene.</summary>
        private Vector3 FallbackSpawn(int slot) =>
            new Vector3(_current.OffsetX + AuthoredFirstArenaMinX + 3f + slot, 0f, 0f);
    }
}
