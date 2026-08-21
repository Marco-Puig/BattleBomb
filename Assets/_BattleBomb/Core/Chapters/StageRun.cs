using UnityEngine;

namespace BattleBomb.Core.Chapters
{
    public enum StagePhase
    {
        /// <summary>Waves are coming or alive; the arena's bounds hold the players in.</summary>
        Fighting = 0,

        /// <summary>Every wave is out and dead; the way forward is open.</summary>
        GateOpen = 1,

        /// <summary>Standing in a checkpoint room (D42). At the last one, the next stage streams in.</summary>
        AtCheckpoint = 2,

        Complete = 3,
    }

    /// <summary>
    /// Flow through one stage (D48): arena by arena, wave by wave, checkpoint by checkpoint.
    /// Advanced by the driver's step and told about spawns and deaths; it decides when the
    /// gate opens and where a wipe goes back to (D49). The scene shows what this decided and
    /// never decides anything itself (rule 2).
    /// </summary>
    public sealed class StageRun
    {
        private readonly StageSpec _spec;
        private int _arena;
        private int _nextWave;
        private int _alive;
        private int _stepsInArena;
        private int _stepsSinceClear;
        private bool _previousWaveCleared = true;

        public StageRun(in StageSpec spec, int resumeCheckpointArena = -1)
        {
            _spec = spec;
            CheckpointArena = Mathf.Clamp(resumeCheckpointArena, -1, spec.ArenaCount - 1);
            BeginArena(CheckpointArena + 1);
        }

        public StageSpec Spec => _spec;

        public StagePhase Phase { get; private set; }

        public int ArenaIndex => _arena;

        /// <summary>The last checkpoint room reached, by the arena it follows; -1 before any.</summary>
        public int CheckpointArena { get; private set; }

        public int Alive => _alive;

        public bool IsFinalArena => _arena >= _spec.ArenaCount - 1;

        /// <summary>The open gate leads into a checkpoint room rather than the next fight.</summary>
        public bool NextIsCheckpoint =>
            _arena >= 0 && _arena < _spec.ArenaCount && _spec.Arenas[_arena].CheckpointAfter;

        /// <summary>Standing in the last checkpoint room: the airlock to the next stage (D48).</summary>
        public bool IsAirlock => Phase == StagePhase.AtCheckpoint && IsFinalArena;

        /// <summary>One simulation step inside the arena.</summary>
        public void Step()
        {
            if (Phase != StagePhase.Fighting)
            {
                return;
            }

            _stepsInArena++;
            if (_alive == 0)
            {
                _stepsSinceClear++;
            }
        }

        /// <summary>
        /// Asks whether a wave is due now. Call once per step after <see cref="Step"/>, then
        /// report the spawn with <see cref="OnEnemiesSpawned"/>. A wave on a timer counts from the
        /// arena's start; a waiting wave counts from the moment the previous one died.
        /// </summary>
        public bool TryNextWave(out WaveSpec wave)
        {
            wave = default;
            if (Phase != StagePhase.Fighting || _arena >= _spec.ArenaCount)
            {
                return false;
            }

            ArenaSpec arena = _spec.Arenas[_arena];
            if (_nextWave >= arena.WaveCount)
            {
                return false;
            }

            WaveSpec candidate = arena.Waves[_nextWave];
            // A timer wave with no delay still waits for the arena's first step, so a freshly
            // entered arena never spawns before the players have taken a single tick in it.
            bool due = candidate.WaitsForPreviousWave
                ? _previousWaveCleared && _stepsSinceClear >= candidate.DelaySteps
                : _stepsInArena >= Mathf.Max(1, candidate.DelaySteps);
            if (!due)
            {
                return false;
            }

            wave = candidate;
            _nextWave++;
            _previousWaveCleared = false;
            return true;
        }

        public void OnEnemiesSpawned(int count)
        {
            _alive += Mathf.Max(0, count);
            if (_alive == 0)
            {
                // A wave that spawned nothing (tests, a disabled spawner) counts as cleared.
                MarkCleared();
            }
        }

        public void OnEnemyDied()
        {
            if (_alive == 0)
            {
                // A kill resolved after its wave already cleared — a wipe racing an in-flight
                // death. There is nothing left to count down, and clearing again would restart
                // the next wave's wait timer from zero.
                return;
            }

            _alive--;
            if (_alive == 0)
            {
                MarkCleared();
            }
        }

        /// <summary>The players are in the checkpoint room the open gate led to.</summary>
        public void ReachCheckpoint()
        {
            if (Phase != StagePhase.GateOpen || !NextIsCheckpoint)
            {
                return;
            }

            CheckpointArena = _arena;
            Phase = StagePhase.AtCheckpoint;
        }

        /// <summary>The players crossed out of the open gate (or the checkpoint room) into
        /// whatever is next: the next arena, or — past the last one — the stage's end.</summary>
        public void EnterNextArena()
        {
            if (Phase != StagePhase.GateOpen && Phase != StagePhase.AtCheckpoint)
            {
                return;
            }

            if (IsFinalArena)
            {
                Phase = StagePhase.Complete;
                return;
            }

            BeginArena(_arena + 1);
        }

        /// <summary>D49: back to the arena after the last checkpoint, waves fresh, loot kept
        /// (it was never ours to touch). Returns the checkpoint arena to respawn at, or -1 for
        /// the stage's own spawn.</summary>
        public int ResetToCheckpoint()
        {
            if (CheckpointArena + 1 >= _spec.ArenaCount)
            {
                // Already past the last fight (a wipe to a burn in the airlock room): nothing
                // to rewind, the room is where they stand back up.
                return CheckpointArena;
            }

            BeginArena(CheckpointArena + 1);
            return CheckpointArena;
        }

        private void BeginArena(int index)
        {
            _arena = Mathf.Clamp(index, 0, Mathf.Max(0, _spec.ArenaCount));
            _nextWave = 0;
            _alive = 0;
            _stepsInArena = 0;
            _stepsSinceClear = 0;
            _previousWaveCleared = true;
            Phase = _arena >= _spec.ArenaCount ? StagePhase.Complete : StagePhase.Fighting;

            if (Phase == StagePhase.Fighting && _spec.Arenas[_arena].WaveCount == 0)
            {
                // An empty arena is a corridor: nothing to fight, the way is already open.
                Phase = StagePhase.GateOpen;
            }
        }

        private void MarkCleared()
        {
            _previousWaveCleared = true;
            _stepsSinceClear = 0;
            if (_arena < _spec.ArenaCount && _nextWave >= _spec.Arenas[_arena].WaveCount)
            {
                Phase = StagePhase.GateOpen;
            }
        }
    }
}
