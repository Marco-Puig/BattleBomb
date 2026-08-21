using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D48's flow: arenas clear, gates open, checkpoints are reached, a wipe goes back to the
    /// last one. Pure state; the scene only shows what this decided.
    /// </summary>
    public sealed class StageRunTests
    {
        private static WaveSpec Wave(int count, int delay = 0, bool waits = false) =>
            new WaveSpec(enemyIndex: 0, count: count, delaySteps: delay, waitsForPreviousWave: waits);

        private static StageSpec TwoArenas(bool checkpointAfterFirst = true) => new StageSpec(
            "s1", "Stage", "Geo",
            new[]
            {
                new ArenaSpec(new[] { Wave(2), Wave(3, delay: 60, waits: true) }, checkpointAfterFirst, false),
                new ArenaSpec(new[] { Wave(1) }, checkpointAfter: true, shopkeeperAfter: true),
            },
            levelStamp: 1, lootProgress: 1f, climate: ElementalMultipliers.Neutral);

        [Test]
        public void The_first_wave_spawns_on_the_first_step_and_the_gate_stays_shut()
        {
            var run = new StageRun(TwoArenas());

            run.Step();
            Assert.That(run.TryNextWave(out WaveSpec wave), Is.True);
            Assert.That(wave.Count, Is.EqualTo(2));
            run.OnEnemiesSpawned(2);

            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting));
            Assert.That(run.TryNextWave(out _), Is.False, "The second wave waits for the first to die.");
        }

        [Test]
        public void A_waiting_wave_counts_its_delay_from_the_clear_not_the_start()
        {
            var run = new StageRun(TwoArenas());
            run.Step();
            run.TryNextWave(out _);
            run.OnEnemiesSpawned(2);
            for (int i = 0; i < 200; i++)
            {
                run.Step();
            }

            run.OnEnemyDied();
            run.OnEnemyDied();
            Assert.That(run.TryNextWave(out _), Is.False, "Cleared just now: sixty steps of breath first.");

            for (int i = 0; i < 60; i++)
            {
                run.Step();
            }

            Assert.That(run.TryNextWave(out WaveSpec wave), Is.True);
            Assert.That(wave.Count, Is.EqualTo(3));
        }

        [Test]
        public void A_stray_death_after_the_wave_already_cleared_does_not_restart_the_wait()
        {
            var run = new StageRun(TwoArenas());
            run.Step();
            run.TryNextWave(out _);
            run.OnEnemiesSpawned(2);
            run.OnEnemyDied();
            run.OnEnemyDied();

            for (int i = 0; i < 59; i++)
            {
                run.Step();
            }

            run.OnEnemyDied();
            run.Step();

            Assert.That(run.TryNextWave(out WaveSpec wave), Is.True,
                "A kill that resolved after the wave already cleared must not restart the wait.");
            Assert.That(wave.Count, Is.EqualTo(3));
        }

        [Test]
        public void An_arena_with_no_waves_opens_the_gate_immediately()
        {
            var corridor = new StageSpec(
                "s1", "Stage", "Geo",
                new[]
                {
                    new ArenaSpec(new WaveSpec[0], checkpointAfter: false, shopkeeperAfter: false),
                    new ArenaSpec(new[] { Wave(1) }, checkpointAfter: true, shopkeeperAfter: false),
                },
                levelStamp: 1, lootProgress: 1f, climate: ElementalMultipliers.Neutral);

            var run = new StageRun(corridor);

            Assert.That(run.Phase, Is.EqualTo(StagePhase.GateOpen),
                "Nothing to fight: the way is already open.");
            Assert.That(run.NextIsCheckpoint, Is.False);
        }

        [Test]
        public void The_gate_opens_only_when_every_wave_is_out_and_dead()
        {
            var run = new StageRun(TwoArenas());
            Clear(run, 2);
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting), "One wave left to come.");

            for (int i = 0; i < 60; i++)
            {
                run.Step();
            }

            Clear(run, 3);

            Assert.That(run.Phase, Is.EqualTo(StagePhase.GateOpen));
            Assert.That(run.NextIsCheckpoint, Is.True, "Arena one is followed by a checkpoint room.");
        }

        [Test]
        public void Reaching_a_checkpoint_records_it_and_entering_the_next_arena_fights_again()
        {
            var run = new StageRun(TwoArenas());
            ClearArena(run);
            Assert.That(run.CheckpointArena, Is.EqualTo(-1));

            run.ReachCheckpoint();
            Assert.That(run.Phase, Is.EqualTo(StagePhase.AtCheckpoint));
            Assert.That(run.CheckpointArena, Is.EqualTo(0));

            run.EnterNextArena();
            Assert.That(run.ArenaIndex, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting));
            Assert.That(run.TryNextWave(out WaveSpec wave), Is.False, "Step first; a wave needs a tick.");
            run.Step();
            Assert.That(run.TryNextWave(out wave), Is.True);
            Assert.That(wave.Count, Is.EqualTo(1));
        }

        [Test]
        public void An_arena_without_a_checkpoint_opens_straight_into_the_next()
        {
            var run = new StageRun(TwoArenas(checkpointAfterFirst: false));
            ClearArena(run);

            Assert.That(run.Phase, Is.EqualTo(StagePhase.GateOpen));
            Assert.That(run.NextIsCheckpoint, Is.False);

            run.EnterNextArena();
            Assert.That(run.ArenaIndex, Is.EqualTo(1));
            Assert.That(run.CheckpointArena, Is.EqualTo(-1), "Nothing was reached; a wipe restarts the stage.");
        }

        [Test]
        public void The_last_arenas_checkpoint_is_the_airlock_and_leaving_it_completes_the_stage()
        {
            var run = new StageRun(TwoArenas());
            ClearArena(run);
            run.ReachCheckpoint();
            run.EnterNextArena();
            run.Step();
            run.TryNextWave(out _);
            run.OnEnemiesSpawned(1);
            run.OnEnemyDied();

            Assert.That(run.Phase, Is.EqualTo(StagePhase.GateOpen));
            Assert.That(run.IsFinalArena, Is.True);
            run.ReachCheckpoint();
            Assert.That(run.IsAirlock, Is.True, "At the last checkpoint, the next stage streams in.");

            run.EnterNextArena();
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Complete));
        }

        [Test]
        public void A_wipe_rewinds_to_the_arena_after_the_last_checkpoint_with_fresh_waves()
        {
            var run = new StageRun(TwoArenas());
            ClearArena(run);
            run.ReachCheckpoint();
            run.EnterNextArena();
            run.Step();
            run.TryNextWave(out _);
            run.OnEnemiesSpawned(1);

            int respawnAt = run.ResetToCheckpoint();

            Assert.That(respawnAt, Is.EqualTo(0), "Respawn in the room after arena zero (D49).");
            Assert.That(run.ArenaIndex, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting));
            Assert.That(run.Alive, Is.Zero);
            run.Step();
            Assert.That(run.TryNextWave(out WaveSpec wave), Is.True, "The arena's waves start over.");
            Assert.That(wave.Count, Is.EqualTo(1));
        }

        [Test]
        public void A_wipe_before_any_checkpoint_restarts_the_stage()
        {
            var run = new StageRun(TwoArenas());
            run.Step();
            run.TryNextWave(out _);
            run.OnEnemiesSpawned(2);

            int respawnAt = run.ResetToCheckpoint();

            Assert.That(respawnAt, Is.EqualTo(-1), "No room reached: the stage's own spawn.");
            Assert.That(run.ArenaIndex, Is.Zero);
        }

        [Test]
        public void Resuming_at_a_saved_checkpoint_starts_in_the_arena_after_it()
        {
            var run = new StageRun(TwoArenas(), resumeCheckpointArena: 0);

            Assert.That(run.ArenaIndex, Is.EqualTo(1));
            Assert.That(run.CheckpointArena, Is.EqualTo(0));
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting));
        }

        private static void Clear(StageRun run, int count)
        {
            run.Step();
            Assert.That(run.TryNextWave(out _), Is.True);
            run.OnEnemiesSpawned(count);
            for (int i = 0; i < count; i++)
            {
                run.OnEnemyDied();
            }
        }

        private static void ClearArena(StageRun run)
        {
            Clear(run, 2);
            for (int i = 0; i < 60; i++)
            {
                run.Step();
            }

            Clear(run, 3);
        }
    }
}
