using BattleBomb.Core.Combat;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class AerialCombatTests
    {
        private static readonly CombatKit Kit = CombatKit.Default;

        private static PlayerCommand Idle => PlayerCommand.Idle(0);

        private static PlayerCommand Press(CommandButtons button) =>
            PlayerCommand.FromState(0, Vector2.zero, button, CommandButtons.None);

        private static PlayerCommand Hold(CommandButtons button) =>
            PlayerCommand.FromState(0, Vector2.zero, button, button);

        private static CombatStepResult Run(CombatStepResult from, PlayerCommand command, int steps, bool grounded)
        {
            CombatStepResult r = from;
            for (int i = 0; i < steps; i++)
            {
                r = CombatMachine.Step(r.State, command, Kit, grounded);
            }

            return r;
        }

        [Test]
        public void Airborne_light_starts_the_pop()
        {
            CombatStepResult r = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Light), Kit, isGrounded: false);

            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.AerialLight.Damage));
            Assert.That(r.Attack.LaunchSpeed, Is.GreaterThan(0f), "The pop pops (D19).");
        }

        [Test]
        public void Airborne_heavy_starts_the_slam_and_never_charges()
        {
            CombatStepResult r = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Heavy), Kit, isGrounded: false);

            Assert.That(r.AttackStarted, Is.True, "Grounded this would charge; airborne it slams.");
            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Startup));
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.AerialHeavy.Damage));
        }

        [Test]
        public void The_slam_holds_its_startup_until_landing()
        {
            CombatStepResult r = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Heavy), Kit, isGrounded: false);

            r = Run(r, Idle, Kit.AerialHeavy.StartupSteps + 10, grounded: false);
            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Startup),
                "Still falling: the windup waits for the ground.");
            Assert.That(r.HitWindowOpened, Is.False);

            r = CombatMachine.Step(r.State, Idle, Kit, isGrounded: true);
            Assert.That(r.HitWindowOpened, Is.True, "The hit resolves on the landing step.");
            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Active));
        }

        [Test]
        public void The_slam_never_resolves_before_its_windup_even_when_landing_early()
        {
            CombatStepResult r = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Heavy), Kit, isGrounded: false);

            for (int i = 0; i < Kit.AerialHeavy.StartupSteps - 1; i++)
            {
                r = CombatMachine.Step(r.State, Idle, Kit, isGrounded: true);
                Assert.That(r.HitWindowOpened, Is.False, $"Windup step {i + 2} is still winding.");
            }

            r = CombatMachine.Step(r.State, Idle, Kit, isGrounded: true);
            Assert.That(r.HitWindowOpened, Is.True);
        }

        [Test]
        public void A_buffered_light_chains_into_another_pop_while_still_airborne()
        {
            int total = Kit.AerialLight.TotalSteps;

            CombatStepResult r = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Light), Kit, isGrounded: false);
            r = Run(r, Idle, 5, grounded: false);
            r = CombatMachine.Step(r.State, Press(CommandButtons.Light), Kit, isGrounded: false);
            r = Run(r, Idle, total - 7, grounded: false);
            Assert.That(r.AttackStarted, Is.False, "The second pop waits for recovery to end.");

            r = CombatMachine.Step(r.State, Idle, Kit, isGrounded: false);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.AerialLight.Damage), "Airborne again: another pop.");
        }

        [Test]
        public void A_buffered_light_lands_into_the_ground_chain()
        {
            int total = Kit.AerialLight.TotalSteps;

            CombatStepResult r = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Light), Kit, isGrounded: false);
            r = Run(r, Idle, 5, grounded: false);
            r = CombatMachine.Step(r.State, Press(CommandButtons.Light), Kit, isGrounded: false);
            r = Run(r, Idle, total - 7, grounded: true);

            r = CombatMachine.Step(r.State, Idle, Kit, isGrounded: true);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.StepAt(0).OnLight.Damage),
                "On the ground the buffered press opens the ground chain instead.");
        }

        [Test]
        public void Grounded_steps_are_the_default_and_leave_existing_behaviour_alone()
        {
            CombatStepResult implicitGround = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Light), Kit);
            CombatStepResult explicitGround = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Light), Kit, isGrounded: true);

            Assert.That(implicitGround.State, Is.EqualTo(explicitGround.State));
            Assert.That(implicitGround.Attack.Damage, Is.EqualTo(Kit.StepAt(0).OnLight.Damage));
        }

        [Test]
        public void Identical_sequences_with_identical_groundedness_produce_identical_states()
        {
            (PlayerCommand command, bool grounded)[] script =
            {
                (Press(CommandButtons.Light), false), (Idle, false), (Idle, false), (Idle, false),
                (Idle, false), (Idle, false), (Idle, false), (Idle, false), (Idle, false),
                (Idle, false), (Idle, false), (Idle, false), (Idle, false), (Idle, false),
                (Idle, false), (Idle, true),
                (Press(CommandButtons.Heavy), false), (Hold(CommandButtons.Heavy), false),
                (Idle, false), (Idle, true), (Idle, true), (Idle, true), (Idle, true),
            };

            CombatStepResult a = new CombatStepResult(CombatState.Ready, false, false, default);
            CombatStepResult b = new CombatStepResult(CombatState.Ready, false, false, default);

            foreach ((PlayerCommand command, bool grounded) in script)
            {
                a = CombatMachine.Step(a.State, command, Kit, grounded);
                b = CombatMachine.Step(b.State, command, Kit, grounded);
                Assert.That(b.State, Is.EqualTo(a.State));
            }
        }
    }
}
