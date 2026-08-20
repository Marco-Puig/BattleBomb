using BattleBomb.Core.Combat;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The Magic verb (task 55, D39): one button, three casts, mana gating, and the stick read
    /// taken at the press rather than at the buffer's mercy.
    /// </summary>
    public sealed class MagicKitTests
    {
        private static readonly CombatKit Kit = CombatKit.Default;
        private static readonly MagicKit Magic = MagicKit.Default;

        private static MagicContext Context(float mana = 999f, bool leapAvailable = true) =>
            new MagicContext(Magic, mana, leapAvailable);

        private static PlayerCommand Press(CommandButtons button, Vector2 stick = default) =>
            new PlayerCommand(0, stick, button, button, CommandButtons.None);

        private static PlayerCommand Idle(Vector2 stick = default) =>
            new PlayerCommand(0, stick, CommandButtons.None, CommandButtons.None, CommandButtons.None);

        private static CombatStepResult PressMagic(
            Vector2 stick, bool isGrounded, in MagicContext magic) =>
            CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Magic, stick), Kit, magic, isGrounded);

        [Test]
        public void A_plain_press_is_the_splash()
        {
            CombatStepResult result = PressMagic(Vector2.zero, true, Context());

            Assert.That(result.CastStarted, Is.True);
            Assert.That(result.Cast, Is.EqualTo(MagicCastKind.Splash));
            Assert.That(result.ManaSpent, Is.EqualTo(Magic.Splash.ManaCost));
        }

        [Test]
        public void Stick_down_makes_it_the_aura()
        {
            CombatStepResult result = PressMagic(new Vector2(0f, -1f), true, Context());

            Assert.That(result.Cast, Is.EqualTo(MagicCastKind.Aura));
            Assert.That(result.ManaSpent, Is.EqualTo(Magic.Aura.ManaCost));
            Assert.That(result.Attack.IsRadial, Is.True, "The circle is magic's answer to depth.");
        }

        [Test]
        public void Stick_up_is_still_the_splash_and_stays_free_for_later()
        {
            CombatStepResult result = PressMagic(new Vector2(0f, 1f), true, Context());

            Assert.That(result.Cast, Is.EqualTo(MagicCastKind.Splash),
                "Up is deliberately unassigned — room to grow, not a fourth cast by accident.");
        }

        [Test]
        public void Aiming_sideways_never_picks_a_different_spell()
        {
            Assert.That(PressMagic(new Vector2(-1f, 0f), true, Context()).Cast,
                Is.EqualTo(MagicCastKind.Splash));
            Assert.That(PressMagic(new Vector2(1f, 0f), true, Context()).Cast,
                Is.EqualTo(MagicCastKind.Splash),
                "The stick aims the splash; only down changes what it is.");
        }

        [Test]
        public void Airborne_the_press_is_always_the_leap()
        {
            Assert.That(PressMagic(Vector2.zero, false, Context()).Cast, Is.EqualTo(MagicCastKind.Leap));
            Assert.That(PressMagic(new Vector2(0f, -1f), false, Context()).Cast,
                Is.EqualTo(MagicCastKind.Leap), "In the air there is only one cast to mean.");
        }

        [Test]
        public void The_leap_reports_its_lift_for_the_motor_to_apply()
        {
            CombatStepResult result = PressMagic(Vector2.zero, false, Context());

            Assert.That(result.LiftSpeed, Is.EqualTo(Magic.Leap.LiftSpeed));
            Assert.That(result.LiftSpeed, Is.GreaterThan(0f));
        }

        [Test]
        public void The_leap_is_once_per_airborne()
        {
            CombatStepResult spent = PressMagic(Vector2.zero, false, Context(leapAvailable: false));

            Assert.That(spent.CastStarted, Is.False, "D18 stands: the base jump never varies, and " +
                "the layered one is a single explicit mechanic.");
            Assert.That(spent.ManaSpent, Is.EqualTo(0));
        }

        [Test]
        public void An_unaffordable_press_does_nothing_at_all()
        {
            CombatStepResult result = PressMagic(Vector2.zero, true, Context(mana: 5f));

            Assert.That(result.CastStarted, Is.False);
            Assert.That(result.ManaSpent, Is.EqualTo(0), "An empty pool spends nothing.");
            Assert.That(result.State.Phase, Is.EqualTo(AttackPhase.Ready), "And commits nothing.");
        }

        [Test]
        public void Exactly_enough_mana_casts()
        {
            CombatStepResult result = PressMagic(Vector2.zero, true, Context(mana: Magic.Splash.ManaCost));

            Assert.That(result.CastStarted, Is.True);
        }

        [Test]
        public void The_stick_is_read_at_the_press_not_when_the_buffer_spends_it()
        {
            // Press down+Magic while frozen in hitstop, then let the stick return to neutral
            // before the freeze ends and the buffer is finally spent.
            CombatState state = CombatState.Ready.WithHitstop(6);
            CombatStepResult result = CombatMachine.Step(
                state, Press(CommandButtons.Magic, new Vector2(0f, -1f)), Kit, Context());
            state = result.State;
            Assert.That(state.BufferedCast, Is.EqualTo(MagicCastKind.Aura));

            for (int i = 0; i < 20 && result.Cast == MagicCastKind.None; i++)
            {
                result = CombatMachine.Step(state, Idle(), Kit, Context());
                state = result.State;
            }

            Assert.That(result.Cast, Is.EqualTo(MagicCastKind.Aura),
                "A flick that has ended by the time the buffer spends must not change the spell.");
        }

        [Test]
        public void A_cast_cannot_start_mid_swing()
        {
            CombatStepResult result = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Light), Kit, Context());
            CombatState swinging = result.State;

            result = CombatMachine.Step(swinging, Press(CommandButtons.Magic), Kit, Context());

            Assert.That(result.CastStarted, Is.False, "A swing is a commitment.");
            Assert.That(result.State.Phase, Is.Not.EqualTo(AttackPhase.Ready));
        }

        [Test]
        public void A_swing_cannot_start_mid_cast()
        {
            CombatStepResult result = PressMagic(Vector2.zero, true, Context());
            CombatState casting = result.State;
            Assert.That(casting.IsCasting, Is.True);

            result = CombatMachine.Step(casting, Press(CommandButtons.Light), Kit, Context());

            Assert.That(result.AttackStarted, Is.False, "And a cast is a commitment too.");
            Assert.That(result.State.CurrentCast, Is.EqualTo(MagicCastKind.Splash));
        }

        [Test]
        public void A_cast_runs_startup_active_and_recovery_like_any_swing()
        {
            CombatStepResult result = PressMagic(Vector2.zero, true, Context());
            CombatState state = result.State;
            MagicCast cast = Magic.Splash;

            bool windowOpened = false;
            for (int i = 0; i < cast.Attack.TotalSteps + 4; i++)
            {
                result = CombatMachine.Step(state, Idle(), Kit, Context());
                state = result.State;
                windowOpened |= result.HitWindowOpened;
            }

            Assert.That(windowOpened, Is.True, "The cast resolved its hits exactly once.");
            Assert.That(state.Phase, Is.EqualTo(AttackPhase.Ready), "And handed control back.");
            Assert.That(state.CurrentCast, Is.EqualTo(MagicCastKind.None));
        }

        [Test]
        public void Mana_is_only_spent_on_the_step_the_cast_starts()
        {
            CombatStepResult result = PressMagic(Vector2.zero, true, Context());
            Assert.That(result.ManaSpent, Is.GreaterThan(0));

            CombatState state = result.State;
            int spentAfter = 0;
            for (int i = 0; i < 40; i++)
            {
                result = CombatMachine.Step(state, Idle(), Kit, Context());
                state = result.State;
                spentAfter += result.ManaSpent;
            }

            Assert.That(spentAfter, Is.EqualTo(0), "A cast is charged once, not per step.");
        }

        [Test]
        public void A_character_with_no_magic_authored_never_casts()
        {
            var empty = new MagicContext(default, 999f, true);

            CombatStepResult result = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Magic), Kit, empty);

            Assert.That(result.CastStarted, Is.False);
        }

        [Test]
        public void Gear_adds_flat_damage_and_reach_to_every_cast()
        {
            MagicKit geared = Magic.ScaledByGear(bonusDamage: 12f, bonusRange: 1.5f);

            Assert.That(geared.Splash.Attack.Damage, Is.EqualTo(Magic.Splash.Attack.Damage + 12f));
            Assert.That(geared.Aura.Attack.ReachX, Is.EqualTo(Magic.Aura.Attack.ReachX + 1.5f),
                "Magic range is the aura's radius as much as the splash's length.");
            Assert.That(geared.Leap.Attack.Damage, Is.EqualTo(Magic.Leap.Attack.Damage + 12f));
        }

        [Test]
        public void Gear_never_changes_what_a_cast_costs_or_how_it_is_shaped()
        {
            MagicKit geared = Magic.ScaledByGear(50f, 5f);

            Assert.That(geared.Aura.ManaCost, Is.EqualTo(Magic.Aura.ManaCost),
                "Gear makes magic stronger, never cheaper.");
            Assert.That(geared.Aura.Attack.IsRadial, Is.True);
            Assert.That(geared.Leap.LiftSpeed, Is.EqualTo(Magic.Leap.LiftSpeed));
            Assert.That(geared.Splash.Attack.TotalSteps, Is.EqualTo(Magic.Splash.Attack.TotalSteps),
                "And never faster — swing speed is the weapon's stat, not magic's.");
        }

        [Test]
        public void A_bare_build_leaves_the_kit_exactly_as_authored()
        {
            MagicKit geared = Magic.ScaledByGear(0f, 0f);

            Assert.That(geared.Splash.Attack.Damage, Is.EqualTo(Magic.Splash.Attack.Damage));
            Assert.That(geared.Aura.Attack.ReachX, Is.EqualTo(Magic.Aura.Attack.ReachX));
        }

        [Test]
        public void The_old_two_argument_step_still_behaves_exactly_as_it_did()
        {
            CombatStepResult result = CombatMachine.Step(
                CombatState.Ready, Press(CommandButtons.Magic), Kit);

            Assert.That(result.CastStarted, Is.False,
                "Callers with no magic context are untouched by M5.");
        }
    }
}
