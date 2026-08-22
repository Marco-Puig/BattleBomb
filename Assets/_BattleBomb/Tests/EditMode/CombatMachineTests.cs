using BattleBomb.Core.Combat;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CombatMachineTests
    {
        private static readonly CombatKit Kit = CombatKit.Default;

        private static PlayerCommand Idle => PlayerCommand.Idle(0);

        private static PlayerCommand Press(CommandButtons button) =>
            PlayerCommand.FromState(0, Vector2.zero, button, CommandButtons.None);

        private static PlayerCommand Hold(CommandButtons button) =>
            PlayerCommand.FromState(0, Vector2.zero, button, button);

        private static CombatStepResult Run(CombatStepResult from, PlayerCommand command, int steps)
        {
            CombatStepResult r = from;
            for (int i = 0; i < steps; i++)
            {
                r = CombatMachine.Step(r.State, command, Kit);
            }

            return r;
        }

        [Test]
        public void A_light_press_from_ready_starts_link_one_immediately()
        {
            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);

            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Startup));
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.StepAt(0).OnLight.Damage));
        }

        [Test]
        public void The_hit_window_opens_exactly_after_startup()
        {
            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);
            int startup = Kit.StepAt(0).OnLight.StartupSteps;

            for (int i = 0; i < startup - 1; i++)
            {
                r = CombatMachine.Step(r.State, Idle, Kit);
                Assert.That(r.HitWindowOpened, Is.False, $"Window must stay shut during startup step {i + 2}.");
            }

            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.HitWindowOpened, Is.True, "The first Active step opens the window.");
        }

        [Test]
        public void Three_lights_chain_through_the_buffer()
        {
            AttackTuning l1 = Kit.StepAt(0).OnLight;
            AttackTuning l2 = Kit.StepAt(1).OnLight;
            AttackTuning l3 = Kit.StepAt(2).OnLight;

            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);

            r = Run(r, Idle, 5);
            r = CombatMachine.Step(r.State, Press(CommandButtons.Light), Kit);
            r = Run(r, Idle, l1.TotalSteps - 7);
            Assert.That(r.AttackStarted, Is.False, "Link two must wait for link one's recovery to end.");

            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(l2.Damage), "Link two follows.");

            r = Run(r, Idle, 9);
            r = CombatMachine.Step(r.State, Press(CommandButtons.Light), Kit);
            r = Run(r, Idle, l2.TotalSteps - 11);
            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(l3.Damage), "Link three finishes the chain.");
        }

        [Test]
        public void Heavy_after_two_lights_fires_the_launcher()
        {
            AttackTuning l1 = Kit.StepAt(0).OnLight;
            AttackTuning l2 = Kit.StepAt(1).OnLight;

            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);
            r = Run(r, Idle, 5);
            r = CombatMachine.Step(r.State, Press(CommandButtons.Light), Kit);
            r = Run(r, Idle, l1.TotalSteps - 7);
            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.AttackStarted, Is.True, "Link two should have chained.");
            Assert.That(r.Attack.Damage, Is.EqualTo(l2.Damage));

            r = Run(r, Idle, 9);
            r = CombatMachine.Step(r.State, Press(CommandButtons.Heavy), Kit);
            r = Run(r, Idle, l2.TotalSteps - 11);
            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.AttackStarted, Is.True, "The ender should have chained.");
            Assert.That(r.Attack.LaunchSpeed, Is.GreaterThan(0f), "L-L-H is the launcher (D19).");
        }

        [Test]
        public void Heavy_after_one_light_falls_back_to_the_standalone_heavy()
        {
            AttackTuning l1 = Kit.StepAt(0).OnLight;

            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);
            r = Run(r, Idle, 5);
            r = CombatMachine.Step(r.State, Press(CommandButtons.Heavy), Kit);
            r = Run(r, Idle, l1.TotalSteps - 7);
            Assert.That(r.AttackStarted, Is.False);

            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.Heavy.Damage),
                "No ender is authored after one Light, so Heavy resets to the standalone.");
            Assert.That(r.Attack.LaunchSpeed, Is.EqualTo(0f));
        }

        [Test]
        public void Mashing_light_never_skips_a_link()
        {
            AttackTuning l1 = Kit.StepAt(0).OnLight;

            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);
            for (int call = 2; call <= l1.TotalSteps; call++)
            {
                r = CombatMachine.Step(r.State, Press(CommandButtons.Light), Kit);
                Assert.That(r.AttackStarted, Is.False, $"Call {call} is still inside link one.");
            }

            r = CombatMachine.Step(r.State, Press(CommandButtons.Light), Kit);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.StepAt(1).OnLight.Damage));
        }

        [Test]
        public void The_combo_window_forgets_the_chain_after_it_expires()
        {
            AttackTuning l1 = Kit.StepAt(0).OnLight;

            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);
            r = Run(r, Idle, l1.TotalSteps);
            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Ready), "The attack has fully recovered.");

            CombatStepResult withinWindow = Run(r, Idle, Kit.ComboWindowSteps - 2);
            withinWindow = CombatMachine.Step(withinWindow.State, Press(CommandButtons.Light), Kit);
            Assert.That(withinWindow.Attack.Damage, Is.EqualTo(Kit.StepAt(1).OnLight.Damage),
                "Inside the window the chain continues at link two.");

            CombatStepResult expired = Run(r, Idle, Kit.ComboWindowSteps - 1);
            expired = CombatMachine.Step(expired.State, Press(CommandButtons.Light), Kit);
            Assert.That(expired.Attack.Damage, Is.EqualTo(l1.Damage),
                "Past the window the chain restarts at link one.");
        }

        [Test]
        public void A_short_charge_releases_the_ordinary_heavy()
        {
            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Heavy), Kit);
            Assert.That(r.AttackStarted, Is.False, "Held Heavy from neutral charges instead of swinging.");
            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Charging));

            r = Run(r, Hold(CommandButtons.Heavy), 10);
            Assert.That(r.AttackStarted, Is.False);

            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.Heavy.Damage));
        }

        [Test]
        public void A_full_charge_releases_the_charged_heavy()
        {
            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Heavy), Kit);
            r = Run(r, Hold(CommandButtons.Heavy), Kit.ChargeThresholdSteps);

            r = CombatMachine.Step(r.State, Idle, Kit);
            Assert.That(r.AttackStarted, Is.True);
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.ChargedHeavy.Damage));
        }

        [Test]
        public void Hitstop_freezes_the_phase_and_the_buffer_but_still_captures_a_press()
        {
            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);
            CombatState stopped = r.State.WithHitstop(3);

            CombatStepResult frozen = CombatMachine.Step(stopped, Press(CommandButtons.Light), Kit);
            Assert.That(frozen.State.HitstopSteps, Is.EqualTo(2));
            Assert.That(frozen.State.StepsInPhase, Is.EqualTo(stopped.StepsInPhase), "Phases do not advance.");
            Assert.That(frozen.State.Buffered, Is.EqualTo(CommandButtons.Light), "The press is remembered.");
            Assert.That(frozen.State.BufferedFor, Is.EqualTo(Kit.InputBufferSteps));

            frozen = CombatMachine.Step(frozen.State, Idle, Kit);
            frozen = CombatMachine.Step(frozen.State, Idle, Kit);
            Assert.That(frozen.State.HitstopSteps, Is.EqualTo(0));
            Assert.That(frozen.State.BufferedFor, Is.EqualTo(Kit.InputBufferSteps),
                "Hitstop never eats a buffered input.");

            CombatStepResult resumed = CombatMachine.Step(frozen.State, Idle, Kit);
            Assert.That(resumed.State.BufferedFor, Is.EqualTo(Kit.InputBufferSteps - 1),
                "The countdown resumes with the phases.");
        }

        [Test]
        public void Identical_command_sequences_produce_identical_states()
        {
            PlayerCommand[] script =
            {
                Press(CommandButtons.Light), Idle, Idle, Idle, Idle, Idle,
                Press(CommandButtons.Light), Idle, Idle, Idle, Idle, Idle, Idle, Idle, Idle, Idle, Idle, Idle,
                Press(CommandButtons.Heavy), Hold(CommandButtons.Heavy), Hold(CommandButtons.Heavy), Idle,
                Idle, Idle, Idle, Idle,
            };

            CombatStepResult a = new CombatStepResult(CombatState.Ready, false, false, default);
            CombatStepResult b = new CombatStepResult(CombatState.Ready, false, false, default);

            foreach (PlayerCommand command in script)
            {
                a = CombatMachine.Step(a.State, command, Kit);
                b = CombatMachine.Step(b.State, command, Kit);
                Assert.That(b.State, Is.EqualTo(a.State));
            }
        }
    }
}
