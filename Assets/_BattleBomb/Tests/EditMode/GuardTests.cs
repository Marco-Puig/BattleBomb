using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class GuardTests
    {
        private static readonly CombatKit Kit = CombatKit.Default;

        private static PlayerCommand Idle => PlayerCommand.Idle(0);

        private static PlayerCommand Press(CommandButtons button) =>
            PlayerCommand.FromState(0, Vector2.zero, button, CommandButtons.None);

        private static PlayerCommand Hold(CommandButtons button) =>
            PlayerCommand.FromState(0, Vector2.zero, button, button);

        private static CombatState Guarding(int age)
        {
            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Block), Kit);
            for (int i = 1; i < age; i++)
            {
                r = CombatMachine.Step(r.State, Hold(CommandButtons.Block), Kit);
            }

            return r.State;
        }

        [Test]
        public void Held_block_from_ready_guards()
        {
            CombatState state = Guarding(1);

            Assert.That(state.Phase, Is.EqualTo(AttackPhase.Guarding));
            Assert.That(state.StepsInPhase, Is.EqualTo(1), "The guard's age starts counting.");
        }

        [Test]
        public void Release_returns_to_ready()
        {
            CombatState state = Guarding(5);
            CombatStepResult released = CombatMachine.Step(state, Idle, Kit);

            Assert.That(released.State.Phase, Is.EqualTo(AttackPhase.Ready));
        }

        [Test]
        public void Airborne_block_does_not_guard()
        {
            CombatStepResult r = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Block), Kit, isGrounded: false);

            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Ready),
                "The air already has jump and depth for defence.");
        }

        [Test]
        public void No_attack_starts_while_guarding()
        {
            CombatState state = Guarding(3);
            PlayerCommand blockAndLight = PlayerCommand.FromState(
                0, Vector2.zero, CommandButtons.Block | CommandButtons.Light, CommandButtons.Block);

            CombatStepResult r = CombatMachine.Step(state, blockAndLight, Kit);

            Assert.That(r.AttackStarted, Is.False, "Guarding refuses the swing (§2.7).");
            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Guarding));
            Assert.That(r.State.Buffered, Is.EqualTo(CommandButtons.Light), "But the press is remembered.");
        }

        [Test]
        public void A_press_buffered_during_the_guard_fires_on_release()
        {
            CombatState state = Guarding(3);
            PlayerCommand blockAndLight = PlayerCommand.FromState(
                0, Vector2.zero, CommandButtons.Block | CommandButtons.Light, CommandButtons.Block);

            CombatStepResult r = CombatMachine.Step(state, blockAndLight, Kit);
            r = CombatMachine.Step(r.State, Idle, Kit);
            r = CombatMachine.Step(r.State, Idle, Kit);

            Assert.That(r.AttackStarted, Is.True, "Release, then the buffered press opens the chain.");
            Assert.That(r.Attack.Damage, Is.EqualTo(Kit.StepAt(0).OnLight.Damage));
        }

        [Test]
        public void Block_during_an_attack_changes_nothing()
        {
            CombatStepResult r = CombatMachine.Step(CombatState.Ready, Press(CommandButtons.Light), Kit);
            r = CombatMachine.Step(r.State, Hold(CommandButtons.Block), Kit);

            Assert.That(r.State.Phase, Is.EqualTo(AttackPhase.Startup),
                "An attack is a commitment; Block does not cancel it (no block-cancels in M3).");
        }

        [Test]
        public void Kinetic_and_projectile_hits_are_blocked_and_magic_never_is()
        {
            CombatState guard = Guarding(Kit.PerfectBlockSteps + 5);
            Vector3 front = new Vector3(1f, 0f, 0f);

            Assert.That(GuardResolver.Resolve(guard, Facing.Right, Vector3.zero, front,
                HitKind.Kinetic, Kit.PerfectBlockSteps), Is.EqualTo(GuardOutcome.Blocked));
            Assert.That(GuardResolver.Resolve(guard, Facing.Right, Vector3.zero, front,
                HitKind.Projectile, Kit.PerfectBlockSteps), Is.EqualTo(GuardOutcome.Blocked));
            Assert.That(GuardResolver.Resolve(guard, Facing.Right, Vector3.zero, front,
                HitKind.Magic, Kit.PerfectBlockSteps), Is.EqualTo(GuardOutcome.Hit),
                "Casters always answer turtles (D19).");
        }

        [Test]
        public void A_young_guard_blocks_perfectly_and_an_old_one_merely_blocks()
        {
            Vector3 front = new Vector3(1f, 0f, 0f);
            CombatState young = Guarding(Kit.PerfectBlockSteps);
            CombatState old = Guarding(Kit.PerfectBlockSteps + 1);

            Assert.That(GuardResolver.Resolve(young, Facing.Right, Vector3.zero, front,
                HitKind.Kinetic, Kit.PerfectBlockSteps), Is.EqualTo(GuardOutcome.PerfectBlocked));
            Assert.That(GuardResolver.Resolve(old, Facing.Right, Vector3.zero, front,
                HitKind.Kinetic, Kit.PerfectBlockSteps), Is.EqualTo(GuardOutcome.Blocked),
                "One step late is an ordinary block — the reward lives in the timing.");
        }

        [Test]
        public void A_hit_from_behind_lands_despite_the_guard()
        {
            CombatState guard = Guarding(3);
            Vector3 behind = new Vector3(-1f, 0f, 0f);

            Assert.That(GuardResolver.Resolve(guard, Facing.Right, Vector3.zero, behind,
                HitKind.Kinetic, Kit.PerfectBlockSteps), Is.EqualTo(GuardOutcome.Hit),
                "Block covers the front only — positioning matters while guarding.");
        }

        [Test]
        public void A_flush_origin_still_counts_as_front()
        {
            CombatState guard = Guarding(3);
            Vector3 flush = new Vector3(-HitResolver.BehindTolerance * 0.5f, 0f, 0f);

            Assert.That(GuardResolver.Resolve(guard, Facing.Right, Vector3.zero, flush,
                HitKind.Kinetic, Kit.PerfectBlockSteps), Is.Not.EqualTo(GuardOutcome.Hit));
        }

        [Test]
        public void Not_guarding_means_the_hit_lands()
        {
            Assert.That(GuardResolver.Resolve(CombatState.Ready, Facing.Right, Vector3.zero,
                new Vector3(1f, 0f, 0f), HitKind.Kinetic, Kit.PerfectBlockSteps),
                Is.EqualTo(GuardOutcome.Hit));
        }
    }
}
