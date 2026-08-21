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
    [DisallowMultipleComponent]
    public sealed class StageRunner : MonoBehaviour
    {
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

        /// <summary>Steps between checks of where the players stand. Cheap anyway; this keeps
        /// the gate from flickering on a single frame of overlap.</summary>
        private const int PlayerCheckEverySteps = 3;

        private readonly List<ArenaMarker> _arenas = new List<ArenaMarker>();
        private readonly List<CheckpointRoomMarker> _rooms = new List<CheckpointRoomMarker>();
        private readonly List<GameObject> _roomProps = new List<GameObject>();

        private ChapterDefinition _chapter;
        private int _stageIndex;
        private int _tierIndex;
        private TierSpec _tier;
        private StageRun _run;
        private StageDefinition _stageDefinition;
        private Scene _stageScene;
        private bool _stageLoaded;
        private float _worldOffsetX;
        private StageExitMarker _exit;
        private PlayerSpawnMarker _spawn;

        private StageRun _nextRun;
        private StageDefinition _nextDefinition;
        private Scene _nextScene;
        private bool _nextLoaded;
        private float _nextOffsetX;

        /// <summary>What the clamp was last built for. The run can open a gate on a step this
        /// runner did nothing else on — the arena's last enemy dying — and the clamp *is* the
        /// gate, so it has to follow every phase change, not only the ones caused here.</summary>
        private StagePhase _clampedPhase;
        private int _clampedArena = -1;

        /// <summary>The checkpoint room a wipe put the players back into (D49). It stays inside
        /// the clamp until they have walked out of it: clamping players out of the room they are
        /// standing in would drag them straight back into the arena mouth.</summary>
        private CheckpointRoomMarker _roomBehind;

        /// <summary>Tests quiet the arena by turning this off: waves are announced and counted
        /// as already cleared, so gates open on schedule without a fight.</summary>
        public bool SpawnsEnabled { get; set; } = true;

        public StageRun Run => _run;

        public StagePhase Phase => _run != null ? _run.Phase : StagePhase.Complete;

        public ChapterDefinition Chapter => _chapter;

        public int StageIndex => _stageIndex;

        public int TierIndex => _tierIndex;

        public TierSpec Tier => _tier;

        public bool IsStageLoaded => _stageLoaded;

        /// <summary>The stage scene currently under the players, for anything that must live and
        /// die with it.</summary>
        public Scene StageScene => _stageScene;

        /// <summary>Raised when the players reach a checkpoint room: stage index and the arena it
        /// follows. The save service listens (D52).</summary>
        public event Action<int, int> CheckpointReached;

        /// <summary>Raised when a stage is left behind through its airlock.</summary>
        public event Action<int> StageCompleted;

        /// <summary>Raised when the chapter's last stage is left behind.</summary>
        public event Action ChapterCompleted;

        /// <summary>Starts a chapter at a stage, optionally resuming at a checkpoint room (D49's
        /// respawn rule doubles as the resume rule). Any running stage is unloaded first.</summary>
        public void Launch(ChapterDefinition chapter, int stageIndex, int tierIndex, int resumeCheckpointArena = -1)
        {
            if (chapter == null || chapter.StageAt(stageIndex) == null)
            {
                Debug.LogError($"{name}: nothing to launch — chapter or stage missing.", this);
                return;
            }

            UnloadEverything();
            _chapter = chapter;
            _stageIndex = stageIndex;
            _tierIndex = Mathf.Clamp(tierIndex, 0, Mathf.Max(0, TierSpecs().Length - 1));
            _tier = TierSpecs()[_tierIndex];
            _worldOffsetX = 0f;
            _stageDefinition = chapter.StageAt(stageIndex);
            _run = new StageRun(_stageDefinition.ToRuntime(), resumeCheckpointArena);
            BeginLoad(_stageDefinition, _worldOffsetX, isNext: false);
        }

        private TierSpec[] TierSpecs() => TierDefinition.ToRuntime(_tiers);

        // ── Loading ──────────────────────────────────────────────────────────────────

        private void BeginLoad(StageDefinition definition, float offsetX, bool isNext)
        {
            string sceneName = definition.ToRuntime().GeometryScene;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"{name}: stage scene '{sceneName}' is not in the build settings.", this);
                return;
            }

            op.completed += _ =>
            {
                Scene scene = SceneManager.GetSceneByName(sceneName);
                ShiftScene(scene, offsetX);
                if (isNext)
                {
                    _nextScene = scene;
                    _nextLoaded = true;
                    WidenForAirlock();
                }
                else
                {
                    OnStageReady(scene);
                }
            };
        }

        /// <summary>Stage scenes are authored around the origin; a streamed stage is slid along X
        /// so its first arena begins where the previous stage's exit was. Markers read world
        /// positions through their transforms, so the shift costs nothing downstream.</summary>
        private static void ShiftScene(Scene scene, float offsetX)
        {
            if (Mathf.Approximately(offsetX, 0f))
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                root.transform.position += new Vector3(offsetX, 0f, 0f);
            }
        }

        private void OnStageReady(Scene scene)
        {
            _stageScene = scene;
            _stageLoaded = true;
            CollectMarkers(scene);
            SpawnRoomProps(scene);

            // A resume starts the run in the arena *after* the room it stands the players up in
            // (D49's respawn rule doubles as the resume rule), so the room they are standing in
            // is behind the clamp from the first step. It stays open until they walk out.
            CheckpointRoomMarker resumeRoom = RoomAfter(_run.CheckpointArena);
            _roomBehind = resumeRoom;

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                Vector3 at = resumeRoom != null
                    ? resumeRoom.RespawnPosition + new Vector3(i * 1.2f - 0.6f, 0f, 0f)
                    : _spawn != null ? _spawn.PositionFor(i) : new Vector3(-5f + i, 0f, 0f);
                actors[i].PlaceAt(at);
                actors[i].SetSpawnPoint(at);
            }

            ApplyBounds();
        }

        /// <summary>
        /// Everything this runner reads out of a stage scene, read once, from that scene alone.
        /// Through the airlock two stages are loaded at once and both carry a full set of
        /// markers; an unscoped search would let the stage ahead answer for the one the players
        /// are still standing in.
        /// </summary>
        private void CollectMarkers(Scene scene)
        {
            _arenas.Clear();
            _rooms.Clear();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                _arenas.AddRange(root.GetComponentsInChildren<ArenaMarker>(true));
                _rooms.AddRange(root.GetComponentsInChildren<CheckpointRoomMarker>(true));
            }

            _arenas.Sort((a, b) => a.Index.CompareTo(b.Index));
            _exit = FindInScene<StageExitMarker>(scene);
            _spawn = FindInScene<PlayerSpawnMarker>(scene);
        }

        private void SpawnRoomProps(Scene scene)
        {
            StageSpec spec = _run.Spec;
            for (int i = 0; i < _rooms.Count; i++)
            {
                CheckpointRoomMarker room = _rooms[i];
                if (room.AfterArena < 0 || room.AfterArena >= spec.ArenaCount)
                {
                    continue;
                }

                ArenaSpec arena = spec.Arenas[room.AfterArena];
                if (!arena.CheckpointAfter)
                {
                    continue;
                }

                Place(_chestPrefab, room.ChestPosition, scene);
                Place(_dummyPrefab, room.DummyPosition, scene);
                if (arena.ShopkeeperAfter)
                {
                    Place(_shopkeeperPrefab, room.ShopkeeperPosition, scene);
                }
            }
        }

        /// <summary>The marker says where on the floor a prop stands; the prefab's own Y says how
        /// far above that floor its body sits. Throwing the prefab's height away would bury a
        /// chest to its lid.</summary>
        private void Place(GameObject prefab, Vector3 at, Scene scene)
        {
            if (prefab == null)
            {
                return;
            }

            at.y += prefab.transform.localPosition.y;
            GameObject go = Instantiate(prefab, at, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, scene);
            _roomProps.Add(go);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private ArenaMarker Arena(int index) =>
            index >= 0 && index < _arenas.Count ? _arenas[index] : null;

        private CheckpointRoomMarker RoomAfter(int arenaIndex)
        {
            if (arenaIndex < 0)
            {
                return null;
            }

            for (int i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].AfterArena == arenaIndex)
                {
                    return _rooms[i];
                }
            }

            return null;
        }

        // ── Stepping ─────────────────────────────────────────────────────────────────

        private void OnStepped(int frame)
        {
            if (_run == null || !_stageLoaded || _run.Phase == StagePhase.Complete)
            {
                return;
            }

            _run.Step();
            while (_run.TryNextWave(out WaveSpec wave))
            {
                _run.OnEnemiesSpawned(SpawnWave(wave));
            }

            if (_run.Phase != _clampedPhase || _run.ArenaIndex != _clampedArena)
            {
                ApplyBounds();
            }

            if (frame % PlayerCheckEverySteps == 0)
            {
                StepPlayerPositions();
            }
        }

        private int SpawnWave(in WaveSpec wave)
        {
            ArenaMarker arena = Arena(_run.ArenaIndex);
            if (!SpawnsEnabled || _spawner == null || arena == null)
            {
                return 0;
            }

            return _spawner.SpawnWave(
                _stageDefinition.EnemyAt(wave.EnemyIndex), wave.Count, arena.SpawnPoints,
                _tier.HealthMultiplier, _tier.DamageMultiplier);
        }

        private void StepPlayerPositions()
        {
            if (_roomBehind != null)
            {
                ArenaMarker here = Arena(_run.ArenaIndex);
                if (here != null && AllLivingPlayersPastX(here.MinX))
                {
                    // Everyone has walked out of the room the wipe stood them up in; it closes.
                    _roomBehind = null;
                    ApplyBounds();
                }
            }

            switch (_run.Phase)
            {
                case StagePhase.GateOpen:
                    if (_run.NextIsCheckpoint)
                    {
                        CheckpointRoomMarker room = RoomAfter(_run.ArenaIndex);
                        if (room != null && AnyLivingPlayerPastX(room.EntryX))
                        {
                            _run.ReachCheckpoint();
                            SetHome(room.RespawnPosition);
                            ApplyBounds();
                            CheckpointReached?.Invoke(_stageIndex, _run.ArenaIndex);
                            if (_run.IsAirlock)
                            {
                                PreloadNextStage();
                            }
                        }
                    }
                    else if (!_run.IsFinalArena)
                    {
                        ArenaMarker next = Arena(_run.ArenaIndex + 1);
                        if (next != null && AllLivingPlayersPastX(next.MinX + 0.5f))
                        {
                            _run.EnterNextArena();
                            ApplyBounds();
                        }
                    }
                    else if (_exit != null && AllLivingPlayersPastX(_exit.X))
                    {
                        // A final arena with no room: the exit is the airlock line itself.
                        FinishStage();
                    }

                    break;

                case StagePhase.AtCheckpoint:
                    if (_run.IsAirlock)
                    {
                        // Cross the exit line once the next stage is ready behind it — or, on
                        // the chapter's last stage, as soon as everyone is through.
                        bool ready = _nextRun == null || _nextLoaded;
                        if (ready && _exit != null && AllLivingPlayersPastX(_exit.X))
                        {
                            FinishStage();
                        }
                    }
                    else
                    {
                        ArenaMarker next = Arena(_run.ArenaIndex + 1);
                        if (next != null && AllLivingPlayersPastX(next.MinX + 0.5f))
                        {
                            _run.EnterNextArena();
                            ApplyBounds();
                        }
                    }

                    break;
            }
        }

        private void SetHome(Vector3 position)
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                actors[i].SetSpawnPoint(position + new Vector3(i * 1.2f - 0.6f, 0f, 0f));
            }
        }

        /// <summary>The clamp is the gate (D48): fighting holds the arena; an open gate reaches
        /// into the room or the next arena; a room reaches into what follows it.</summary>
        private void ApplyBounds()
        {
            ArenaMarker arena = Arena(_run.ArenaIndex);
            if (arena == null)
            {
                return;
            }

            float minX = arena.MinX;
            float maxX = arena.MaxX;
            switch (_run.Phase)
            {
                case StagePhase.GateOpen:
                    CheckpointRoomMarker ahead = _run.NextIsCheckpoint ? RoomAfter(_run.ArenaIndex) : null;
                    ArenaMarker beyond = Arena(_run.ArenaIndex + 1);
                    if (ahead != null)
                    {
                        maxX = ahead.MaxX;
                    }
                    else if (beyond != null)
                    {
                        maxX = beyond.MaxX;
                    }
                    else if (_exit != null)
                    {
                        maxX = _exit.X + 2f;
                    }

                    break;

                case StagePhase.AtCheckpoint:
                    CheckpointRoomMarker room = RoomAfter(_run.ArenaIndex);
                    ArenaMarker after = Arena(_run.ArenaIndex + 1);
                    minX = room != null ? room.MinX : minX;
                    if (after != null)
                    {
                        maxX = after.MaxX;
                    }
                    else if (_exit != null)
                    {
                        // The last room: let them walk out over the exit line. Once the next
                        // stage is loaded, WidenForAirlock reaches further still.
                        maxX = _exit.X + 2f;
                    }
                    else if (room != null)
                    {
                        maxX = room.MaxX;
                    }

                    break;

                case StagePhase.Fighting:
                    // Coming out of a room, the room stays open behind the players until the
                    // fight actually begins — which is this very moment, so: the arena alone.
                    break;
            }

            if (_roomBehind != null)
            {
                minX = Mathf.Min(minX, _roomBehind.MinX);
            }

            _clampedPhase = _run.Phase;
            _clampedArena = _run.ArenaIndex;
            _driver.SetArena(new ArenaBounds(minX, maxX, arena.Bounds.GroundY));
        }

        private void WidenForAirlock()
        {
            if (_run == null || !_run.IsAirlock || _nextRun == null)
            {
                return;
            }

            CheckpointRoomMarker room = RoomAfter(_run.ArenaIndex);
            float minX = room != null ? room.MinX : Arena(_run.ArenaIndex).MinX;
            float maxX = FirstArenaMaxX(_nextScene);
            _driver.SetArena(new ArenaBounds(minX, maxX, 0f));
        }

        private static float FirstArenaMaxX(Scene scene)
        {
            float best = float.MinValue;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ArenaMarker arena in root.GetComponentsInChildren<ArenaMarker>(true))
                {
                    if (arena.Index == 0)
                    {
                        best = Mathf.Max(best, arena.MaxX);
                    }
                }
            }

            return best > float.MinValue ? best : 8f;
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

        // ── The airlock ──────────────────────────────────────────────────────────────

        private void PreloadNextStage()
        {
            StageDefinition next = _chapter.StageAt(_stageIndex + 1);
            if (next == null)
            {
                return;
            }

            const float NextFirstArenaMinX = -8f;
            _nextDefinition = next;
            _nextRun = new StageRun(next.ToRuntime());
            _nextOffsetX = (_exit != null ? _exit.X : 0f) - NextFirstArenaMinX;
            _nextLoaded = false;
            BeginLoad(next, _nextOffsetX, isNext: true);
        }

        private void FinishStage()
        {
            int finished = _stageIndex;
            _run.EnterNextArena();
            StageCompleted?.Invoke(finished);

            if (_nextRun == null)
            {
                _stageLoaded = false;
                ChapterCompleted?.Invoke();
                return;
            }

            // Hand over: the stage behind the players unloads, the one ahead is the stage now.
            Scene previous = _stageScene;
            ClearRoomProps();
            _run = _nextRun;
            _stageDefinition = _nextDefinition;
            _stageScene = _nextScene;
            _worldOffsetX = _nextOffsetX;
            _stageIndex++;
            _nextRun = null;
            _nextDefinition = null;
            _nextLoaded = false;
            _roomBehind = null;

            CollectMarkers(_stageScene);
            SpawnRoomProps(_stageScene);
            SetHome(_spawn != null
                ? _spawn.PositionFor(0)
                : new Vector3(_worldOffsetX - 5f, 0f, 0f));
            ApplyBounds();

            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.UnloadSceneAsync(previous);
            }
        }

        // ── Wipes ────────────────────────────────────────────────────────────────────

        /// <summary>The driver already stood everyone back up at their spawn point (the last
        /// room's respawn) and the spawner already cleared its brood; the run rewinds and the
        /// clamp follows (D49). A wipe in the airlock leaves the preloaded next stage where it
        /// is — the run has nothing to rewind there and says so by not moving.</summary>
        private void OnAttemptReset()
        {
            if (_run == null || !_stageLoaded)
            {
                return;
            }

            int checkpoint = _run.ResetToCheckpoint();
            _roomBehind = RoomAfter(checkpoint);
            ApplyBounds();
        }

        private void OnEnemyDied(EnemyDeath death)
        {
            _run?.OnEnemyDied();
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

            if (_run == null && _defaultChapter != null)
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
        }

        private void ClearRoomProps()
        {
            for (int i = 0; i < _roomProps.Count; i++)
            {
                if (_roomProps[i] != null)
                {
                    Destroy(_roomProps[i]);
                }
            }

            _roomProps.Clear();
        }

        private void UnloadEverything()
        {
            ClearRoomProps();
            if (_stageLoaded && _stageScene.IsValid() && _stageScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_stageScene);
            }

            if (_nextLoaded && _nextScene.IsValid() && _nextScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_nextScene);
            }

            _stageLoaded = false;
            _nextLoaded = false;
            _nextRun = null;
            _run = null;
            _roomBehind = null;
            _exit = null;
            _spawn = null;
            _clampedArena = -1;
        }
    }
}
