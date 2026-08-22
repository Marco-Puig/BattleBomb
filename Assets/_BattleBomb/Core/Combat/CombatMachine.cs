using BattleBomb.Core.Players;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// The per-step combat brain (D19): pure like <c>CharacterMotor</c>. Combos chain through the
    /// same press-buffering the jump has; the chain consumes at recovery end, never earlier, so an
    /// attack is a commitment. Hitstop freezes phases and the buffer countdown alike, but a press
    /// during hitstop is still captured. Airborne, the verbs become the aerials: Light is the pop,
    /// Heavy is the slam, and nothing charges — the caller passes groundedness so the machine
    /// stays pure.
    /// </summary>
    public static class CombatMachine
    {
        public static CombatStepResult Step(
            in CombatState state, in PlayerCommand command, CombatKit kit, bool isGrounded = true) =>
            Step(state, command, kit, MagicContext.None, isGrounded);

        public static CombatStepResult Step(
            in CombatState state, in PlayerCommand command, CombatKit kit, in MagicContext magic,
            bool isGrounded = true)
        {
            CommandButtons buffered = state.Buffered;
            int bufferedFor = state.BufferedFor;
            MagicCastKind bufferedCast = state.BufferedCast;
            bool inHitstop = state.HitstopSteps > 0;

            if (!inHitstop && bufferedFor > 0)
            {
                bufferedFor -= 1;
                if (bufferedFor == 0)
                {
                    buffered = CommandButtons.None;
                    bufferedCast = MagicCastKind.None;
                }
            }

            if (command.WasPressed(CommandButtons.Light))
            {
                buffered = CommandButtons.Light;
                bufferedFor = kit.InputBufferSteps;
                bufferedCast = MagicCastKind.None;
            }
            else if (command.WasPressed(CommandButtons.Heavy))
            {
                buffered = CommandButtons.Heavy;
                bufferedFor = kit.InputBufferSteps;
                bufferedCast = MagicCastKind.None;
            }
            else if (command.WasPressed(CommandButtons.Magic))
            {
                // The stick is read here, at the press, and remembered — never re-read when the
                // buffer is finally spent (D39).
                buffered = CommandButtons.Magic;
                bufferedFor = kit.InputBufferSteps;
                bufferedCast = MagicKit.Choose(command.Move, isGrounded);
            }

            if (inHitstop)
            {
                CombatState frozen = new CombatState(
                    state.Phase, state.StepsInPhase, state.ComboIndex, state.ComboWindowLeft,
                    buffered, bufferedFor, state.ChargeSteps, state.HitstopSteps - 1,
                    state.CurrentAttack, bufferedCast, state.CurrentCast);
                return new CombatStepResult(
                    frozen, false, false, state.CurrentAttack, state.CurrentCast);
            }

            switch (state.Phase)
            {
                case AttackPhase.Ready:
                    return StepReady(state, command, kit, magic, isGrounded, buffered, bufferedFor, bufferedCast);
                case AttackPhase.Charging:
                    return StepCharging(state, command, kit, buffered, bufferedFor, bufferedCast);
                default:
                    return StepFlight(state, command, kit, magic, isGrounded, buffered, bufferedFor, bufferedCast);
            }
        }

        private static CombatStepResult StepReady(
            in CombatState state, in PlayerCommand command, CombatKit kit, in MagicContext magic,
            bool isGrounded, CommandButtons buffered, int bufferedFor, MagicCastKind bufferedCast)
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
                return isGrounded ? StartLight(kit, comboIndex) : StartAerial(kit.AerialLight);
            }

            if (buffered == CommandButtons.Heavy)
            {
                // Airborne Heavy is always the slam — no launcher ender, no charging in the air.
                return isGrounded ? ConsumeHeavy(command, kit, comboIndex) : StartAerial(kit.AerialHeavy);
            }

            if (buffered == CommandButtons.Magic && magic.CanCast(bufferedCast))
            {
                return StartCast(magic, bufferedCast);
            }

            CombatState idle = new CombatState(
                AttackPhase.Ready, 0, comboIndex, comboWindowLeft,
                buffered, bufferedFor, 0, 0, default, bufferedCast);
            return new CombatStepResult(idle, false, false, default);
        }

        private static CombatStepResult StepCharging(
            in CombatState state, in PlayerCommand command, CombatKit kit,
            CommandButtons buffered, int bufferedFor, MagicCastKind bufferedCast)
        {
            if (command.IsHeld(CommandButtons.Heavy))
            {
                int charge = state.ChargeSteps < kit.ChargeThresholdSteps
                    ? state.ChargeSteps + 1
                    : state.ChargeSteps;
                CombatState charging = new CombatState(
                    AttackPhase.Charging, 0, 0, 0, buffered, bufferedFor, charge, 0, default, bufferedCast);
                return new CombatStepResult(charging, false, false, default);
            }

            AttackTuning released = state.ChargeSteps >= kit.ChargeThresholdSteps
                ? kit.ChargedHeavy
                : kit.Heavy;
            return Start(released, 0, buffered, bufferedFor, bufferedCast);
        }

        private static CombatStepResult StepFlight(
            in CombatState state, in PlayerCommand command, CombatKit kit, in MagicContext magic,
            bool isGrounded, CommandButtons buffered, int bufferedFor, MagicCastKind bufferedCast)
        {
            AttackTuning attack = state.CurrentAttack;
            AttackPhase phase = state.Phase;
            int steps = state.StepsInPhase;
            bool hitWindowOpened = false;

            // A landing-resolved attack (the slam) serves its startup, then waits for the ground:
            // the hit window opens on the landing step, where the shadow said it would (D14).
            if (phase == AttackPhase.Startup && steps >= attack.StartupSteps
                && (isGrounded || !attack.ResolvesOnLanding))
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
                    return isGrounded ? StartLight(kit, state.ComboIndex) : StartAerial(kit.AerialLight);
                }

                if (buffered == CommandButtons.Heavy)
                {
                    return isGrounded
                        ? ConsumeHeavy(command, kit, state.ComboIndex)
                        : StartAerial(kit.AerialHeavy);
                }

                if (buffered == CommandButtons.Magic && magic.CanCast(bufferedCast))
                {
                    return StartCast(magic, bufferedCast);
                }

                CombatState ready = new CombatState(
                    AttackPhase.Ready, 0, state.ComboIndex, kit.ComboWindowSteps,
                    buffered, bufferedFor, 0, 0, default, bufferedCast);
                return new CombatStepResult(ready, false, false, default);
            }

            CombatState next = new CombatState(
                phase, steps + 1, state.ComboIndex, 0, buffered, bufferedFor, 0, 0, attack,
                bufferedCast, state.CurrentCast);
            return new CombatStepResult(
                next, false, hitWindowOpened, attack, state.CurrentCast);
        }

        private static CombatStepResult StartAerial(in AttackTuning attack) =>
            Start(attack, 0, CommandButtons.None, 0, MagicCastKind.None);

        private static CombatStepResult StartLight(CombatKit kit, int comboIndex)
        {
            AttackTuning attack = kit.StepAt(comboIndex).OnLight;
            int next = comboIndex + 1;
            if (next >= kit.ChainLength)
            {
                next = 0;
            }

            return Start(attack, next, CommandButtons.None, 0, MagicCastKind.None);
        }

        private static CombatStepResult ConsumeHeavy(in PlayerCommand command, CombatKit kit, int comboIndex)
        {
            if (comboIndex > 0 && kit.StepAt(comboIndex).HasHeavy)
            {
                return Start(kit.StepAt(comboIndex).OnHeavy, 0, CommandButtons.None, 0, MagicCastKind.None);
            }

            if (command.IsHeld(CommandButtons.Heavy))
            {
                CombatState charging = new CombatState(
                    AttackPhase.Charging, 0, 0, 0, CommandButtons.None, 0, 0, 0, default);
                return new CombatStepResult(charging, false, false, default);
            }

            return Start(kit.Heavy, 0, CommandButtons.None, 0, MagicCastKind.None);
        }

        /// <summary>
        /// A cast begins: it drops the combo like any other commitment, and reports what it costs
        /// so the caller can spend the mana it already confirmed was there.
        /// </summary>
        private static CombatStepResult StartCast(in MagicContext magic, MagicCastKind kind)
        {
            MagicCast cast = magic.Kit.For(kind);
            CombatState started = new CombatState(
                AttackPhase.Startup, 1, 0, 0, CommandButtons.None, 0, 0, 0, cast.Attack,
                MagicCastKind.None, kind);
            return new CombatStepResult(
                started, true, false, cast.Attack, kind, cast.ManaCost, cast.LiftSpeed);
        }

        private static CombatStepResult Start(
            in AttackTuning attack, int comboIndex, CommandButtons buffered, int bufferedFor,
            MagicCastKind bufferedCast)
        {
            CombatState started = new CombatState(
                AttackPhase.Startup, 1, comboIndex, 0, buffered, bufferedFor, 0, 0, attack,
                bufferedCast);
            return new CombatStepResult(started, true, false, attack);
        }
    }
}
