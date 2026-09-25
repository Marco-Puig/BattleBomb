using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Saves;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using UnityEngine;

namespace BattleBomb.Gameplay.Session
{
    /// <summary>
    /// Autosave (D52): checkpoint entry, stage completion, chapter completion, chest close,
    /// clean quit. Those five and no others, because the rule the player has to learn is that
    /// losing costs what a wipe costs — a crash between two of these moments must never take
    /// more than dying would. Each of them also moves the story's resume point or its beaten
    /// tiers, so the gate (D50) advances here too. Without a session — the bare scene, the M6
    /// smoke suite — there is nothing to save to, and this stays silent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaveService : MonoBehaviour
    {
        [Tooltip("The machine's driver. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("The runner whose checkpoints and completions are the save moments.")]
        [SerializeField] private StageRunner _runner;

        [Tooltip("The couch's one sack and wallet (D51).")]
        [SerializeField] private SharedStash _stash;

        private readonly List<CharacterState> _states = new List<CharacterState>();
        private GameSession _session;
        private bool _refusalReported;

        /// <summary>How many times this session wrote. The smoke suite counts it.</summary>
        public int Writes { get; private set; }

        /// <summary>The next chapter and tier that a completion just opened — for the results
        /// screen. Empty strings mean nothing new.</summary>
        public string UnlockedChapter { get; private set; } = string.Empty;

        public string UnlockedTier { get; private set; } = string.Empty;

        /// <summary>There is a session with somewhere to write to. Not the same as being allowed
        /// to write — see <see cref="CanWrite"/>.</summary>
        public bool IsActive => _session != null && _session.Store != null;

        /// <summary>
        /// The guard that matters. <see cref="GameSession.LoadedSave"/> is null for two opposite
        /// reasons and only one of them is safe to write over: no file yet, which is a new game;
        /// or a file this build read and <em>refused</em> — written by a newer version, or
        /// corrupt (D52). The second is somebody's whole save sitting on disk in a shape we
        /// merely failed to parse, and the session that refused it starts with an empty stash.
        /// Autosaving that emptiness back would destroy the file within a minute of play,
        /// silently, when the player had done nothing but open the game once with the wrong
        /// build. So a refusal stops every write for the run; a missing file does not.
        /// </summary>
        public bool CanWrite => IsActive && _session.LoadedCleanly;

        /// <summary>A save exists that this build refused, so nothing will be written this run.</summary>
        public bool WritingBlocked => IsActive && !_session.LoadedCleanly;

        public void SaveNow()
        {
            if (!IsActive)
            {
                return;
            }

            // The guest writes its own save only when the host says a D52 moment happened (Plan 2,
            // D61). Its own copy of the session is a picture of the host's run, and saving a picture
            // over a real file is how a guest loses their gear. Read from what this scene was built
            // as, not from the connection: a guest who leaves mid-match still stands in a picture.
            if (_driver != null && _driver.IsReplica)
            {
                return;
            }

            if (!_session.LoadedCleanly)
            {
                ReportRefusalOnce();
                return;
            }

            if (_driver == null || _stash == null)
            {
                return;
            }

            _states.Clear();
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag != null)
                {
                    _states.Add(new CharacterState(actors[i].Element, bag.Ledger, bag.Inventory));
                }
            }

            SaveGame save = SaveMapper.Capture(
                _stash.Sack, _stash.Wallet, _states, _session.Progress,
                _session.LoadedSave != null ? _session.LoadedSave.Characters : null);
            _session.Store.Write(_session.SaveName, SaveCodec.Encode(save));

            // What was just written becomes what we carry: the next autosave's carried roster has
            // to be the file as it stands now, not as it stood when the scene booted.
            _session.LoadedSave = save;
            Writes++;
        }

        /// <summary>Once per run, not once per autosave moment — five identical errors between
        /// the first checkpoint and the quit would bury the one that explains itself.</summary>
        private void ReportRefusalOnce()
        {
            if (_refusalReported)
            {
                return;
            }

            _refusalReported = true;
            Debug.LogError(
                $"{name}: refusing to autosave. The save '{_session.SaveName}' was read and rejected " +
                $"({_session.LoadOutcome}), so this run began with an empty stash that is not the " +
                "player's. Writing it back would overwrite a file that may still be recoverable — by " +
                "a newer build, or by hand. Nothing will be saved for the rest of this run.", this);
        }

        private void OnCheckpointReached(int stageIndex, int arena)
        {
            if (!TryChapterId(out string chapterId))
            {
                return;
            }

            _session.Progress.SetResume(chapterId, stageIndex, arena);
            SaveNow();
        }

        private void OnStageCompleted(int stageIndex)
        {
            if (!TryChapterId(out string chapterId))
            {
                return;
            }

            // The stage behind is done, so a resume lands at the top of the one ahead with no
            // checkpoint room reached in it yet.
            _session.Progress.SetResume(chapterId, stageIndex + 1, -1);
            SaveNow();
        }

        private void OnChapterCompleted()
        {
            if (!TryChapterId(out string chapterId))
            {
                return;
            }

            UnlockedChapter = string.Empty;
            UnlockedTier = string.Empty;

            int tiersBefore = _session.Progress.HighestTierBeaten(chapterId);
            int chapterIndex = IndexOf(chapterId);
            ChapterSpec[] specs = ChapterDefinition.ToRuntime(_session.Chapters);

            // The gate is bounded by the session's own tier list, never by the default three:
            // the tier count is data (D50), and a gate that assumed otherwise would disagree
            // with the picker the moment a fourth row was authored.
            int tierCount = _session.Tiers.Length;
            bool nextChapterWasOpen = ProgressGate.IsUnlocked(
                _session.Progress, specs, chapterIndex + 1, 0, tierCount);

            _session.Progress.RecordCompletion(chapterId, _runner.TierIndex);

            if (_session.Progress.HighestTierBeaten(chapterId) > tiersBefore
                && _runner.TierIndex + 1 < tierCount)
            {
                UnlockedTier = _session.Tiers[_runner.TierIndex + 1].DisplayName;
            }

            if (!nextChapterWasOpen && chapterIndex >= 0 && chapterIndex + 1 < _session.Chapters.Length
                && ProgressGate.IsUnlocked(_session.Progress, specs, chapterIndex + 1, 0, tierCount))
            {
                UnlockedChapter = _session.Chapters[chapterIndex + 1].DisplayName;
            }

            SaveNow();
        }

        /// <summary>The chapter the runner is actually running. None means a launch that never
        /// happened, and there is no progress to record against nothing.</summary>
        private bool TryChapterId(out string chapterId)
        {
            chapterId = _runner != null && _runner.Chapter != null ? _runner.Chapter.Id : null;
            return _session != null && !string.IsNullOrEmpty(chapterId);
        }

        private int IndexOf(string chapterId)
        {
            for (int i = 0; i < _session.Chapters.Length; i++)
            {
                if (_session.Chapters[i] != null && _session.Chapters[i].Id == chapterId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>D52's fourth moment. Any checkpoint-room screen closing counts, chest or
        /// shopkeeper: both spend the run's loot, and both are the player putting the run down
        /// for a moment — which is exactly when a crash would feel like theft.</summary>
        private void OnScreenChanged(int playerId, InteractionKind kind, bool opened)
        {
            if (!opened)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit() => SaveNow();

        private void OnEnable()
        {
            _session = GameSession.Find();
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_runner == null)
            {
                _runner = FindAnyObjectByType<StageRunner>();
            }

            if (_stash == null)
            {
                _stash = FindAnyObjectByType<SharedStash>();
            }

            if (!IsActive)
            {
                // No session: pressing Play in this scene, or the smoke suite. Nothing to save to.
                return;
            }

            if (_driver == null || _runner == null || _stash == null)
            {
                Debug.LogError(
                    $"{name}: a session launched this scene but there is no " +
                    $"{(_driver == null ? "SimulationDriver" : _runner == null ? "StageRunner" : "SharedStash")} " +
                    "to save from — every D52 autosave moment will pass unrecorded and the run will " +
                    "end where it started.", this);
                return;
            }

            // Subscribed even when the save was refused, because the run still moves the
            // in-memory progress the results screen reads. SaveNow is where the refusal stops.
            _runner.CheckpointReached += OnCheckpointReached;
            _runner.StageCompleted += OnStageCompleted;
            _runner.ChapterCompleted += OnChapterCompleted;
            _driver.ScreenChanged += OnScreenChanged;
        }

        private void OnDisable()
        {
            if (_runner != null)
            {
                _runner.CheckpointReached -= OnCheckpointReached;
                _runner.StageCompleted -= OnStageCompleted;
                _runner.ChapterCompleted -= OnChapterCompleted;
            }

            if (_driver != null)
            {
                _driver.ScreenChanged -= OnScreenChanged;
            }
        }
    }
}
