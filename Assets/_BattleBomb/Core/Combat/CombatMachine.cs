using BattleBomb.Core.Players;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// The per-step combat brain (D19): pure like <c>CharacterMotor</c>. Combos chain through the
    /// same press-buffering the jump has; the chain consumes at recovery end, never earlier, so an
    /// attack is a commitment. Hitstop freezes phases and the buffer countdown alike, but a press
    /// during hitstop is still captured.
    /// </summary>
    public static class CombatMachine
    {
        public static CombatStepResult Step(in CombatState state, in PlayerCommand command, CombatKit kit)
        {
            CommandButtons buffered = state.Buffered;
            int bufferedFor = state.BufferedFor;
            bool inHitstop = state.HitstopSteps > 0;

            if (!inHitstop && bufferedFor > 0)
            {
                bufferedFor -= 1;
                if (bufferedFor == 0)
                {
                    buffered = CommandButtons.None;
                }
            }

            if (command.WasPressed(CommandButtons.Light))
            {
                buffered = CommandButtons.Light;
                bufferedFor = kit.InputBufferSteps;
            }
            else if (command.WasPressed(CommandButtons.Heavy))
            {
                buffered = CommandButtons.Heavy;
                bufferedFor = kit.InputBufferSteps;
            }

            if (inHitstop)
            {
                CombatState frozen = new CombatState(
                    state.Phase, state.StepsInPhase, state.ComboIndex, state.ComboWindowLeft,
                    buffered, bufferedFor, state.ChargeSteps, state.HitstopSteps - 1,
                    state.CurrentAttack);
                return new CombatStepResult(frozen, false, false, state.CurrentAttack);
            }

            switch (state.Phase)
            {
                case AttackPhase.Ready:
                    return StepReady(state, command, kit, buffered, bufferedFor);
                case AttackPhase.Charging:
                    return StepCharging(state, command, kit, buffered, bufferedFor);
                default:
                    return StepFlight(state, command, kit, buffered, bufferedFor);
            }
        }

        private static CombatStepResult StepReady(
            in CombatState state, in PlayerCommand command, CombatKit kit,
            CommandButtons buffered, int bufferedFor)
        {
            int comboIndex = state.ComboIndex;
            int comboWindowLeft = state.ComboWindowLeft;
            if (comboIndex > 0)
            {
                comboWindowLeft -= 1;
                if (comboWindowLeft <= 0)
                {
                    comboIndex = 0;
                    comboWindowLeft = 0;
                }
            }

            if (buffered == CommandButtons.Light)
            {
                return StartLight(kit, comboIndex);
            }

            if (buffered == CommandButtons.Heavy)
            {
                return ConsumeHeavy(command, kit, comboIndex);
            }

            CombatState idle = new CombatState(
                AttackPhase.Ready, 0, comboIndex, comboWindowLeft,
                buffered, bufferedFor, 0, 0, default);
            return new CombatStepResult(idle, false, false, default);
        }

        private static CombatStepResult StepCharging(
            in CombatState state, in PlayerCommand command, CombatKit kit,
            CommandButtons buffered, int bufferedFor)
        {
            if (command.IsHeld(CommandButtons.Heavy))
            {
                int charge = state.ChargeSteps < kit.ChargeThresholdSteps
                    ? state.ChargeSteps + 1
                    : state.ChargeSteps;
                CombatState charging = new CombatState(
                    AttackPhase.Charging, 0, 0, 0, buffered, bufferedFor, charge, 0, default);
                return new CombatStepResult(charging, false, false, default);
            }

            AttackTuning released = state.ChargeSteps >= kit.ChargeThresholdSteps
                ? kit.ChargedHeavy
                : kit.Heavy;
            return Start(released, 0, buffered, bufferedFor);
        }

        private static CombatStepResult StepFlight(
            in CombatState state, in PlayerCommand command, CombatKit kit,
            CommandButtons buffered, int bufferedFor)
        {
            AttackTuning attack = state.CurrentAttack;
            AttackPhase phase = state.Phase;
            int steps = state.StepsInPhase;
            bool hitWindowOpened = false;

            if (phase == AttackPhase.Startup && steps >= attack.StartupSteps)
            {
                phase = AttackPhase.Active;
                steps = 0;
                hitWindowOpened = true;
            }
            else if (phase == AttackPhase.Active && steps >= attack.ActiveSteps)
            {
                phase = AttackPhase.Recovery;
                steps = 0;
            }
            else if (phase == AttackPhase.Recovery && steps >= attack.RecoverySteps)
            {
                if (buffered == CommandButtons.Light)
                {
                    return StartLight(kit, state.ComboIndex);
                }

                if (buffered == CommandButtons.Heavy)
                {
                    return ConsumeHeavy(command, kit, state.ComboIndex);
                }

                CombatState ready = new CombatState(
                    AttackPhase.Ready, 0, state.ComboIndex, kit.ComboWindowSteps,
                    buffered, bufferedFor, 0, 0, default);
                return new CombatStepResult(ready, false, false, default);
            }

            CombatState next = new CombatState(
                phase, steps + 1, state.ComboIndex, 0, buffered, bufferedFor, 0, 0, attack);
            return new CombatStepResult(next, false, hitWindowOpened, attack);
        }

        private static CombatStepResult StartLight(CombatKit kit, int comboIndex)
        {
            AttackTuning attack = kit.StepAt(comboIndex).OnLight;
            int next = comboIndex + 1;
            if (next >= kit.ChainLength)
            {
                next = 0;
            }

            return Start(attack, next, CommandButtons.None, 0);
        }

        private static CombatStepResult ConsumeHeavy(in PlayerCommand command, CombatKit kit, int comboIndex)
        {
            if (comboIndex > 0 && kit.StepAt(comboIndex).HasHeavy)
            {
                return Start(kit.StepAt(comboIndex).OnHeavy, 0, CommandButtons.None, 0);
            }

            if (command.IsHeld(CommandButtons.Heavy))
            {
                CombatState charging = new CombatState(
                    AttackPhase.Charging, 0, 0, 0, CommandButtons.None, 0, 0, 0, default);
                return new CombatStepResult(charging, false, false, default);
            }

            return Start(kit.Heavy, 0, CommandButtons.None, 0);
        }

        private static CombatStepResult Start(
            in AttackTuning attack, int comboIndex, CommandButtons buffered, int bufferedFor)
        {
            CombatState started = new CombatState(
                AttackPhase.Startup, 1, comboIndex, 0, buffered, bufferedFor, 0, 0, attack);
            return new CombatStepResult(started, true, false, attack);
        }
    }
}
