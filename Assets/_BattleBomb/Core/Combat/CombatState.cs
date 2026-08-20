using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// One character's combat state between steps. A value, like <c>MotorState</c>: the machine
    /// returns a new one every step, so combat is replayable and testable (D10).
    /// </summary>
    public readonly struct CombatState
    {
        public readonly AttackPhase Phase;

        /// <summary>Completed steps in the current phase.</summary>
        public readonly int StepsInPhase;

        /// <summary>Chain position: how many chain Lights the current combo has started.</summary>
        public readonly int ComboIndex;

        /// <summary>Steps left, while Ready, before the combo forgets its position.</summary>
        public readonly int ComboWindowLeft;

        /// <summary>A follow-up press remembered mid-attack — Light, Heavy, or None.</summary>
        public readonly CommandButtons Buffered;

        /// <summary>Steps left on that memory; 0 means nothing is buffered.</summary>
        public readonly int BufferedFor;

        /// <summary>Steps Heavy has been held while Charging.</summary>
        public readonly int ChargeSteps;

        /// <summary>While above zero the whole character is frozen, buffer countdown included.</summary>
        public readonly int HitstopSteps;

        /// <summary>The attack in flight; valid during Startup, Active, and Recovery.</summary>
        public readonly AttackTuning CurrentAttack;

        /// <summary>
        /// Which cast a buffered Magic press meant (D39), decided from the stick at the press
        /// itself. A flick that has ended by the time the buffer spends must not change the spell.
        /// </summary>
        public readonly MagicCastKind BufferedCast;

        /// <summary>The cast in flight, or None when the attack in flight is an ordinary swing.</summary>
        public readonly MagicCastKind CurrentCast;

        public CombatState(
            AttackPhase phase,
            int stepsInPhase,
            int comboIndex,
            int comboWindowLeft,
            CommandButtons buffered,
            int bufferedFor,
            int chargeSteps,
            int hitstopSteps,
            AttackTuning currentAttack,
            MagicCastKind bufferedCast = MagicCastKind.None,
            MagicCastKind currentCast = MagicCastKind.None)
        {
            BufferedCast = bufferedCast;
            CurrentCast = currentCast;
            Phase = phase;
            StepsInPhase = stepsInPhase;
            ComboIndex = comboIndex;
            ComboWindowLeft = comboWindowLeft;
            Buffered = buffered;
            BufferedFor = bufferedFor;
            ChargeSteps = chargeSteps;
            HitstopSteps = hitstopSteps;
            CurrentAttack = currentAttack;
        }

        public static CombatState Ready => new CombatState(
            AttackPhase.Ready, 0, 0, 0, CommandButtons.None, 0, 0, 0, default);

        /// <summary>Freezes the character for at least the given steps — landing or taking a hit.</summary>
        public CombatState WithHitstop(int steps) => new CombatState(
            Phase, StepsInPhase, ComboIndex, ComboWindowLeft, Buffered, BufferedFor, ChargeSteps,
            Mathf.Max(HitstopSteps, steps), CurrentAttack, BufferedCast, CurrentCast);

        /// <summary>True while a cast rather than a swing is in flight.</summary>
        public bool IsCasting => CurrentCast != MagicCastKind.None && Phase != AttackPhase.Ready;
    }
}
