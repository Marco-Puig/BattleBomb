using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class EnemyBrainTests
    {
        private static AttackTuning Swing => new AttackTuning(
            startupSteps: 6, activeSteps: 2, recoverySteps: 4,
            damage: 6f, reachX: 1.5f, depthTolerance: 1f, lungeDistance: 0f,
            maxTargets: 1, knockbackSpeed: 5f, launchSpeed: 0f, hitstopSteps: 2,
            moveSpeedScale: 1f);

        private static EnemyTuning Tuning(EnemyArchetype archetype, bool interruptible = true) =>
            new EnemyTuning(
                archetype, Swing, cooldownSteps: 5, interruptible, staggerSteps: 4,
                Element.None, projectileSpeed: 8f, standoffNearX: 4f, standoffFarX: 7f);

        private static EnemyPerception At(Vector3 self, Vector3 target) =>
            new EnemyPerception(self, true, target);

        private static EnemyStepResult Run(EnemyState state, EnemyPerception view, EnemyTuning tuning, int steps)
        {
            EnemyStepResult r = new EnemyStepResult(state, Vector2.zero, false, false, false);
            for (int i = 0; i < steps; i++)
            {
                r = EnemyBrain.Step(r.State, view, tuning);
            }

            return r;
        }

        [Test]
        public void The_grunt_closes_on_both_axes()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyStepResult r = EnemyBrain.Step(
                EnemyState.Fresh, At(Vector3.zero, new Vector3(5f, 0f, 2f)), grunt);

            Assert.That(r.MoveIntent.x, Is.GreaterThan(0f), "Closes laterally…");
            Assert.That(r.MoveIntent.y, Is.GreaterThan(0f), "…and closes depth — melee lives under §2.2.");
        }

        [Test]
        public void The_grunt_stops_closing_once_in_reach_and_swings()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyStepResult r = EnemyBrain.Step(
                EnemyState.Fresh, At(Vector3.zero, new Vector3(1f, 0f, 0.3f)), grunt);

            Assert.That(r.AttackStarted, Is.True, "In reach and in depth: the telegraph opens.");
            Assert.That(r.State.Phase, Is.EqualTo(EnemyPhase.Telegraph));
            Assert.That(r.MoveIntent, Is.EqualTo(Vector2.zero), "The windup commits — no drift.");
        }

        [Test]
        public void The_grunt_never_swings_at_an_aligned_but_deep_target()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyStepResult r = Run(
                EnemyState.Fresh, At(Vector3.zero, new Vector3(1f, 0f, 2.5f)), grunt, 30);

            Assert.That(r.State.Phase, Is.EqualTo(EnemyPhase.Approach),
                "Aligned on X but deep: melee is depth-limited, so it keeps closing instead.");
        }

        [Test]
        public void The_telegraph_runs_its_full_startup_before_the_window()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyPerception view = At(Vector3.zero, new Vector3(1f, 0f, 0f));

            EnemyStepResult r = EnemyBrain.Step(EnemyState.Fresh, view, grunt);
            for (int i = 1; i < Swing.StartupSteps; i++)
            {
                r = EnemyBrain.Step(r.State, view, grunt);
                Assert.That(r.HitWindowOpened, Is.False, $"Telegraph step {i + 1} is still winding.");
            }

            r = EnemyBrain.Step(r.State, view, grunt);
            Assert.That(r.HitWindowOpened, Is.True, "The window opens exactly after the startup.");
        }

        [Test]
        public void The_cooldown_gates_the_next_swing_but_still_chases()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyPerception near = At(Vector3.zero, new Vector3(1f, 0f, 0f));
            EnemyPerception far = At(Vector3.zero, new Vector3(6f, 0f, 0f));

            EnemyStepResult r = Run(EnemyState.Fresh, near,
                grunt, 1 + Swing.StartupSteps + Swing.ActiveSteps + Swing.RecoverySteps);
            Assert.That(r.State.Phase, Is.EqualTo(EnemyPhase.Cooldown));

            EnemyStepResult chasing = EnemyBrain.Step(r.State, far, grunt);
            Assert.That(chasing.MoveIntent.x, Is.GreaterThan(0f), "Cooldown still moves…");
            Assert.That(chasing.AttackStarted, Is.False, "…but cannot swing.");

            r = Run(r.State, near, grunt, grunt.CooldownSteps);
            r = EnemyBrain.Step(r.State, near, grunt);
            Assert.That(r.AttackStarted, Is.True, "The beat ends and the next swing comes.");
        }

        [Test]
        public void The_ranged_holds_its_standoff_band()
        {
            EnemyTuning ranged = Tuning(EnemyArchetype.Ranged);

            EnemyStepResult tooFar = EnemyBrain.Step(
                EnemyState.Fresh, At(Vector3.zero, new Vector3(9f, 0f, 0f)), ranged);
            Assert.That(tooFar.MoveIntent.x, Is.GreaterThan(0f), "Beyond the band it advances.");

            EnemyStepResult tooClose = EnemyBrain.Step(
                EnemyState.Fresh, At(new Vector3(5f, 0f, 0f), new Vector3(7f, 0f, 0f)), ranged);
            Assert.That(tooClose.MoveIntent.x, Is.LessThan(0f), "Inside the band it retreats.");
        }

        [Test]
        public void The_ranged_drifts_depth_without_ever_seeking_the_target_in_it()
        {
            EnemyTuning ranged = Tuning(EnemyArchetype.Ranged);
            EnemyStepResult deep = EnemyBrain.Step(
                EnemyState.Fresh, At(Vector3.zero, new Vector3(9f, 0f, 2.5f)), ranged);
            EnemyStepResult shallow = EnemyBrain.Step(
                EnemyState.Fresh, At(Vector3.zero, new Vector3(9f, 0f, -2.5f)), ranged);

            Assert.That(deep.MoveIntent.y, Is.Not.EqualTo(0f), "Never a statue (D28)…");
            Assert.That(deep.MoveIntent.y, Is.EqualTo(shallow.MoveIntent.y),
                "…but the drift ignores where the target sits in depth — projectiles cross it (§2.2).");
        }

        [Test]
        public void The_ranged_fires_a_projectile_instead_of_a_melee_window()
        {
            EnemyTuning ranged = Tuning(EnemyArchetype.Ranged);
            EnemyPerception view = At(Vector3.zero, new Vector3(5f, 0f, 2f));

            EnemyStepResult r = EnemyBrain.Step(EnemyState.Fresh, view, ranged);
            Assert.That(r.AttackStarted, Is.True, "Inside the band, any depth: it fires.");

            r = Run(r.State, view, ranged, Swing.StartupSteps);
            Assert.That(r.ProjectileFired, Is.True);
            Assert.That(r.HitWindowOpened, Is.False, "No melee box on a standoff archetype.");
        }

        [Test]
        public void A_hit_staggers_the_grunt_and_never_the_brute()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyTuning brute = Tuning(EnemyArchetype.Brute, interruptible: false);
            EnemyState midTelegraph = new EnemyState(EnemyPhase.Telegraph, 3, 0);

            EnemyState flinched = EnemyBrain.Interrupted(midTelegraph, grunt);
            Assert.That(flinched.Phase, Is.EqualTo(EnemyPhase.Staggered), "The swing is dropped.");

            EnemyState unmoved = EnemyBrain.Interrupted(midTelegraph, brute);
            Assert.That(unmoved.Phase, Is.EqualTo(EnemyPhase.Telegraph),
                "The brute never flinches — jump and depth are the answer (D26).");
            Assert.That(unmoved.StepsInPhase, Is.EqualTo(3));
        }

        [Test]
        public void The_stagger_runs_its_steps_then_releases()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyState flinched = EnemyBrain.Interrupted(new EnemyState(EnemyPhase.Telegraph, 3, 0), grunt);
            EnemyPerception far = At(Vector3.zero, new Vector3(9f, 0f, 0f));

            EnemyStepResult r = Run(flinched, far, grunt, grunt.StaggerSteps - 1);
            Assert.That(r.State.Phase, Is.EqualTo(EnemyPhase.Staggered));
            Assert.That(r.MoveIntent, Is.EqualTo(Vector2.zero), "Flinching takes the legs too.");

            r = EnemyBrain.Step(r.State, far, grunt);
            Assert.That(r.State.Phase, Is.EqualTo(EnemyPhase.Approach), "Then the fight resumes.");
        }

        [Test]
        public void Hitstop_freezes_the_brain_without_eating_the_phase()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyState stopped = new EnemyState(EnemyPhase.Telegraph, 2, 0).WithHitstop(3);
            EnemyPerception view = At(Vector3.zero, new Vector3(1f, 0f, 0f));

            EnemyStepResult r = EnemyBrain.Step(stopped, view, grunt);
            Assert.That(r.State.StepsInPhase, Is.EqualTo(2), "Phases do not advance in hitstop.");
            Assert.That(r.State.HitstopSteps, Is.EqualTo(2));
            Assert.That(r.MoveIntent, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void No_target_means_no_movement_and_no_attack()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyStepResult r = Run(
                EnemyState.Fresh, EnemyPerception.NoTarget(Vector3.zero), grunt, 20);

            Assert.That(r.State.Phase, Is.EqualTo(EnemyPhase.Approach));
            Assert.That(r.MoveIntent, Is.EqualTo(Vector2.zero));
            Assert.That(r.AttackStarted, Is.False);
        }

        [Test]
        public void A_waiting_grunt_circles_instead_of_swinging()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyPerception noToken = new EnemyPerception(
                Vector3.zero, true, new Vector3(1f, 0f, 0f), mayAttack: false);

            EnemyStepResult r = EnemyBrain.Step(EnemyState.Fresh, noToken, grunt);

            Assert.That(r.AttackStarted, Is.False, "Someone else holds the attack token (D28).");
            Assert.That(r.MoveIntent.x, Is.LessThan(0f), "Inside the hover ring it backs out…");
            Assert.That(r.MoveIntent.y, Is.Not.EqualTo(0f), "…and strafes depth while it waits.");
        }

        [Test]
        public void The_turn_arrives_and_the_swing_comes()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyPerception token = new EnemyPerception(
                Vector3.zero, true, new Vector3(1f, 0f, 0f), mayAttack: true);

            EnemyStepResult r = EnemyBrain.Step(EnemyState.Fresh, token, grunt);

            Assert.That(r.AttackStarted, Is.True);
        }

        [Test]
        public void The_brute_ignores_the_token_entirely()
        {
            EnemyTuning brute = new EnemyTuning(
                EnemyArchetype.Brute, Swing, cooldownSteps: 5, interruptible: false, staggerSteps: 1,
                Element.None, 0f, 0f, 0f, hoverDistanceX: 3f, strafePeriodSteps: 90,
                hopPulseSteps: 0, takesTurns: false);
            EnemyPerception noToken = new EnemyPerception(
                Vector3.zero, true, new Vector3(1f, 0f, 0f), mayAttack: false);

            EnemyStepResult r = EnemyBrain.Step(EnemyState.Fresh, noToken, brute);

            Assert.That(r.AttackStarted, Is.True, "He never waits — relentlessness is the identity (D28).");
        }

        [Test]
        public void Seeds_desynchronise_the_crowd()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyPerception waiting = new EnemyPerception(
                Vector3.zero, true, new Vector3(6f, 0f, 0f), mayAttack: false);

            EnemyStepResult a = EnemyBrain.Step(EnemyState.Seeded(0), waiting, grunt);
            EnemyStepResult b = EnemyBrain.Step(EnemyState.Seeded(1), waiting, grunt);

            Assert.That(a.MoveIntent.y, Is.Not.EqualTo(b.MoveIntent.y),
                "Two enemies with identical perception must not move in lockstep (D28).");
        }

        [Test]
        public void The_strafe_flips_direction_on_its_beat()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyPerception waiting = new EnemyPerception(
                Vector3.zero, true, new Vector3(6f, 0f, 0f), mayAttack: false);

            EnemyStepResult early = EnemyBrain.Step(EnemyState.Seeded(0), waiting, grunt);
            EnemyState aged = new EnemyState(
                EnemyPhase.Approach, 0, 0, seed: 0, ageSteps: 90);
            EnemyStepResult late = EnemyBrain.Step(aged, waiting, grunt);

            Assert.That(late.MoveIntent.y, Is.EqualTo(-early.MoveIntent.y),
                "One strafe period later the circle reverses.");
        }

        [Test]
        public void The_cooldown_peels_off_instead_of_face_camping()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyState cooling = new EnemyState(EnemyPhase.Cooldown, 1, 0);
            EnemyPerception close = At(Vector3.zero, new Vector3(1f, 0f, 0f));

            EnemyStepResult r = EnemyBrain.Step(cooling, close, grunt);

            Assert.That(r.MoveIntent.x, Is.LessThan(0f),
                "After the swing it backs out to the hover ring — hit and peel (D28).");
        }

        [Test]
        public void Hops_pulse_while_free_and_never_mid_attack()
        {
            EnemyTuning hopper = new EnemyTuning(
                EnemyArchetype.Grunt, Swing, cooldownSteps: 5, interruptible: true, staggerSteps: 4,
                Element.None, 0f, 0f, 0f, hoverDistanceX: 3f, strafePeriodSteps: 90,
                hopPulseSteps: 4, takesTurns: true);
            EnemyPerception waiting = new EnemyPerception(
                Vector3.zero, true, new Vector3(6f, 0f, 0f), mayAttack: false);

            bool hopped = false;
            EnemyState state = EnemyState.Seeded(3);
            for (int i = 0; i < 8; i++)
            {
                EnemyStepResult r = EnemyBrain.Step(state, waiting, hopper);
                hopped |= r.JumpRequested;
                state = r.State;
            }

            Assert.That(hopped, Is.True, "The hop beat fires while circling.");

            EnemyStepResult telegraphing = EnemyBrain.Step(
                new EnemyState(EnemyPhase.Telegraph, 1, 0, seed: 3, ageSteps: 4), waiting, hopper);
            Assert.That(telegraphing.JumpRequested, Is.False, "Never during a commitment.");
        }

        [Test]
        public void Identical_perception_sequences_produce_identical_states()
        {
            EnemyTuning grunt = Tuning(EnemyArchetype.Grunt);
            EnemyPerception[] script =
            {
                At(Vector3.zero, new Vector3(4f, 0f, 1f)),
                At(Vector3.zero, new Vector3(3f, 0f, 0.6f)),
                At(Vector3.zero, new Vector3(1f, 0f, 0.2f)),
                At(Vector3.zero, new Vector3(1f, 0f, 0.2f)),
                At(Vector3.zero, new Vector3(2f, 0f, 0.4f)),
                At(Vector3.zero, new Vector3(1f, 0f, 0.1f)),
            };

            EnemyState a = EnemyState.Fresh;
            EnemyState b = EnemyState.Fresh;
            foreach (EnemyPerception view in script)
            {
                a = EnemyBrain.Step(a, view, grunt).State;
                b = EnemyBrain.Step(b, view, grunt).State;
                Assert.That(b, Is.EqualTo(a));
            }
        }
    }
}
