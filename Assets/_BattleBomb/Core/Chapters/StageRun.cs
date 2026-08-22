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
    /// gate opens and where a wipe goes back to (D49, narrowed by D53). The scene shows what this
    /// decided and never decides anything itself (rule 2).
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

        /// <summary>
        /// A fresh stage starts at its first arena; one resumed at a saved checkpoint stands up
        /// in that room, because D49 makes a resume and a wipe the same boundary.
        /// </summary>
        public StageRun(in StageSpec spec, int resumeCheckpointArena = -1)
        {
            _spec = spec;
            CheckpointArena = Mathf.Clamp(resumeCheckpointArena, -1, spec.ArenaCount - 1);
            StandUpAtCheckpoint();
        }

        public StageSpec Spec => _spec;

        public StagePhase Phase { get; private set; }

        public int ArenaIndex => _arena;

        /// <summary>The last checkpoint room <em>banked</em>, by the arena it follows; -1 before
        /// any. D53 makes reaching a room and banking it two things: this is the room a wipe goes
        /// back to, which is the last one the couch walked into whole.</summary>
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

        /// <summary>
        /// The players are in the checkpoint room the open gate led to. <paramref name="couchIsWhole"/>
        /// says whether every player is on their feet; Core cannot see players, so the driver has
        /// to tell it (rule 1).
        /// <para>
        /// D53 splits what used to be one action into two. <em>Entering</em> always works — the
        /// phase advances, so the chest opens and the airlock streams the stage behind it — because
        /// refusing that would make a stage impossible to leave once a partner died in it.
        /// <em>Banking</em> is the half a body on the floor costs: the room becomes the wipe point
        /// only when the couch is whole. That one condition is also the whole of D53's "a wipe
        /// returns to the last room where everyone was alive" — if a room only banks while nobody
        /// is down, <see cref="CheckpointArena"/> already <em>is</em> the last whole one, and a
        /// second field tracking it would be a second source of the same truth.
        /// </para>
        /// </summary>
        public void ReachCheckpoint(bool couchIsWhole)
        {
            if (Phase != StagePhase.GateOpen || !NextIsCheckpoint)
            {
                return;
            }

            if (couchIsWhole)
            {
                CheckpointArena = _arena;
            }

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

        /// <summary>D49: back to the last checkpoint room, waves fresh, loot kept (it was never
        /// ours to touch). Returns the checkpoint arena to respawn at, or -1 for the stage's own
        /// spawn.</summary>
        public int ResetToCheckpoint()
        {
            StandUpAtCheckpoint();
            return CheckpointArena;
        }

        /// <summary>
        /// Where a lost attempt — or a resumed save, which D49 makes the same boundary — comes
        /// back: standing <em>in</em> the last checkpoint room, not in the fight past it. D49's
        /// wipe is meant to be "a chance to try to upgrade and equip stuff to go again", and D42
        /// only opens the chest inside such a room, so a run that came up
        /// <see cref="StagePhase.Fighting"/> in the arena ahead spent that chance before the
        /// players stood up: its waves spawn while they are still in the room, and the clamp that
        /// holds the room open for them holds those enemies in it too.
        /// <para>
        /// So the run lands at <see cref="StagePhase.AtCheckpoint"/> on the arena the room
        /// follows, exactly as <see cref="ReachCheckpoint"/> leaves it, and the fight starts when
        /// <see cref="EnterNextArena"/> says they walked out — the same crossing the ordinary path
        /// uses, rather than a second one. With no room behind them there is nothing to stand up
        /// in and the stage restarts at its first arena, fighting.
        /// </para>
        /// </summary>
        private void StandUpAtCheckpoint()
        {
            if (CheckpointArena < 0)
            {
                BeginArena(0);
                return;
            }

            _arena = CheckpointArena;
            ClearArenaProgress();
            Phase = StagePhase.AtCheckpoint;
        }

        private void BeginArena(int index)
        {
            _arena = Mathf.Clamp(index, 0, Mathf.Max(0, _spec.ArenaCount));
            ClearArenaProgress();
            Phase = _arena >= _spec.ArenaCount ? StagePhase.Complete : StagePhase.Fighting;

            if (Phase == StagePhase.Fighting && _spec.Arenas[_arena].WaveCount == 0)
            {
                // An empty arena is a corridor: nothing to fight, the way is already open.
                Phase = StagePhase.GateOpen;
            }
        }

        private void ClearArenaProgress()
        {
            _nextWave = 0;
            _alive = 0;
            _stepsInArena = 0;
            _stepsSinceClear = 0;
            _previousWaveCleared = true;
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
