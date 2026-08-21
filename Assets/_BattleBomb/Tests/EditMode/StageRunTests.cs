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
        public void A_wipe_stands_the_players_up_in_the_checkpoint_room_not_in_the_fight_past_it()
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
            Assert.That(run.Phase, Is.EqualTo(StagePhase.AtCheckpoint),
                "D49 gives a wipe a chance to upgrade and re-equip, and D42 only opens the chest "
                + "inside the room — so the run comes back in the room, not in the next fight.");
            Assert.That(run.ArenaIndex, Is.EqualTo(0),
                "In a room, the arena is the one it follows — exactly as reaching it leaves things.");
            Assert.That(run.Alive, Is.Zero);

            run.EnterNextArena();

            Assert.That(run.ArenaIndex, Is.EqualTo(1), "Walking out is what starts the refight.");
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting));
            run.Step();
            Assert.That(run.TryNextWave(out WaveSpec wave), Is.True, "The arena's waves start over.");
            Assert.That(wave.Count, Is.EqualTo(1));
        }

        [Test]
        public void After_a_wipe_no_wave_is_due_until_the_players_leave_the_room()
        {
            var run = new StageRun(TwoArenas());
            ClearArena(run);
            run.ReachCheckpoint();
            run.EnterNextArena();
            run.Step();
            run.TryNextWave(out _);
            run.OnEnemiesSpawned(1);

            run.ResetToCheckpoint();

            for (int i = 0; i < 600; i++)
            {
                run.Step();
                Assert.That(run.TryNextWave(out _), Is.False,
                    "Nothing spawns while they stand in the room (D49): the arena clamp still "
                    + "holds the room, so a wave due here would walk in after them.");
            }

            Assert.That(run.Alive, Is.Zero, "Standing in a room is not a fight.");

            run.EnterNextArena();
            run.Step();

            Assert.That(run.TryNextWave(out WaveSpec wave), Is.True,
                "Out of the room and into the arena: now the fight starts.");
            Assert.That(wave.Count, Is.EqualTo(1));
        }

        [Test]
        public void A_wipe_before_any_checkpoint_restarts_the_stage_and_fights_from_its_spawn()
        {
            var run = new StageRun(TwoArenas());
            run.Step();
            run.TryNextWave(out _);
            run.OnEnemiesSpawned(2);

            int respawnAt = run.ResetToCheckpoint();

            Assert.That(respawnAt, Is.EqualTo(-1), "No room reached: the stage's own spawn.");
            Assert.That(run.ArenaIndex, Is.Zero);
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting),
                "There is no room to stand up in, so the first arena simply starts over.");
            Assert.That(run.Alive, Is.Zero);
            run.Step();
            Assert.That(run.TryNextWave(out WaveSpec wave), Is.True);
            Assert.That(wave.Count, Is.EqualTo(2), "From the top of the arena.");
        }

        [Test]
        public void Resuming_at_a_saved_checkpoint_stands_up_in_that_room_by_the_same_rule_as_a_wipe()
        {
            var run = new StageRun(TwoArenas(), resumeCheckpointArena: 0);

            Assert.That(run.CheckpointArena, Is.EqualTo(0));
            Assert.That(run.Phase, Is.EqualTo(StagePhase.AtCheckpoint),
                "D49: quitting mid-stage resumes from the same point by the same rule, and that "
                + "point is the room — with the chest in it (D42) — not the fight past it.");
            Assert.That(run.ArenaIndex, Is.EqualTo(0));
            run.Step();
            Assert.That(run.TryNextWave(out _), Is.False, "Nothing is coming while they are in the room.");

            run.EnterNextArena();

            Assert.That(run.ArenaIndex, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Fighting));
        }

        [Test]
        public void Resuming_at_the_last_checkpoint_stands_up_in_the_airlock_not_past_the_stage()
        {
            var run = new StageRun(TwoArenas(), resumeCheckpointArena: 1);

            Assert.That(run.IsAirlock, Is.True,
                "The final room is a saveable checkpoint like any other; coming back to it must "
                + "leave the stage still there to walk out of.");
            Assert.That(run.Phase, Is.EqualTo(StagePhase.AtCheckpoint));

            run.EnterNextArena();
            Assert.That(run.Phase, Is.EqualTo(StagePhase.Complete));
        }

        [Test]
        public void A_wipe_in_the_airlock_room_leaves_the_players_standing_in_it()
        {
            var run = new StageRun(TwoArenas());
            ClearArena(run);
            run.ReachCheckpoint();
            run.EnterNextArena();
            Clear(run, 1);
            run.ReachCheckpoint();
            Assert.That(run.IsAirlock, Is.True);

            int respawnAt = run.ResetToCheckpoint();

            Assert.That(respawnAt, Is.EqualTo(1), "The room they were already in.");
            Assert.That(run.IsAirlock, Is.True, "Nothing to rewind past the last fight.");
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
