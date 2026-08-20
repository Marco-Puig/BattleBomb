using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>Which of the three casts one Magic press meant (D39).</summary>
    public enum MagicCastKind
    {
        None = 0,

        /// <summary>A narrow line erupting from the ground ahead — the default press.</summary>
        Splash,

        /// <summary>A radial burst around the caster — stick down. The crowd moment, and the
        /// only cast that answers depth on its own.</summary>
        Aura,

        /// <summary>The elemental double jump — pressed airborne. Mobility with a little chip.</summary>
        Leap,
    }

    /// <summary>
    /// One authored cast: its frame data and shape (the same <see cref="AttackTuning"/> vocabulary
    /// swings use, so a cast is a commitment with a windup like everything else), what it costs,
    /// and the lift it gives. Per character, as data — a character's magic is authored, never coded.
    /// </summary>
    public readonly struct MagicCast
    {
        public readonly AttackTuning Attack;
        public readonly int ManaCost;

        /// <summary>Upward speed the cast grants. Only the leap uses it; 0 everywhere else.</summary>
        public readonly float LiftSpeed;

        public MagicCast(in AttackTuning attack, int manaCost, float liftSpeed = 0f)
        {
            Attack = attack;
            ManaCost = Mathf.Max(0, manaCost);
            LiftSpeed = Mathf.Max(0f, liftSpeed);
        }

        /// <summary>False for a character who simply has no cast in this slot.</summary>
        public bool IsAuthored => Attack.TotalSteps > 0;
    }

    /// <summary>
    /// One character's whole Magic verb (D39): three casts on one button. Stick up plus Magic is
    /// deliberately unassigned — room to grow, not a gap.
    /// </summary>
    public readonly struct MagicKit
    {
        /// <summary>How far down the stick must be at the press to mean the aura rather than the splash.</summary>
        public const float AuraStickThreshold = -0.5f;

        public readonly MagicCast Splash;
        public readonly MagicCast Aura;
        public readonly MagicCast Leap;

        public MagicKit(in MagicCast splash, in MagicCast aura, in MagicCast leap)
        {
            Splash = splash;
            Aura = aura;
            Leap = leap;
        }

        public MagicCast For(MagicCastKind kind)
        {
            switch (kind)
            {
                case MagicCastKind.Splash: return Splash;
                case MagicCastKind.Aura: return Aura;
                case MagicCastKind.Leap: return Leap;
                default: return default;
            }
        }

        /// <summary>
        /// Which cast a press means, decided from the stick at the instant it happened — never at
        /// the instant the buffer spends it, or a flick that ended before the swing recovered would
        /// silently become the wrong spell.
        /// </summary>
        public static MagicCastKind Choose(Vector2 stick, bool isGrounded)
        {
            if (!isGrounded)
            {
                return MagicCastKind.Leap;
            }

            return stick.y <= AuraStickThreshold ? MagicCastKind.Aura : MagicCastKind.Splash;
        }

        /// <summary>
        /// The kit as gear makes it (D39): magic damage and magic range are flat additions to
        /// every cast, which is the entire caster build — Strength never reaches here (D32), so
        /// what a character's magic is worth is decided in loot.
        /// </summary>
        public MagicKit ScaledByGear(float bonusDamage, float bonusRange)
        {
            if (bonusDamage <= 0f && bonusRange <= 0f)
            {
                return this;
            }

            return new MagicKit(
                Boosted(Splash, bonusDamage, bonusRange),
                Boosted(Aura, bonusDamage, bonusRange),
                Boosted(Leap, bonusDamage, bonusRange));
        }

        private static MagicCast Boosted(in MagicCast cast, float bonusDamage, float bonusRange)
        {
            if (!cast.IsAuthored)
            {
                return cast;
            }

            AttackTuning a = cast.Attack;
            var boosted = new AttackTuning(
                a.StartupSteps, a.ActiveSteps, a.RecoverySteps,
                a.Damage + bonusDamage, a.ReachX + bonusRange, a.DepthTolerance,
                a.LungeDistance, a.MaxTargets, a.KnockbackSpeed, a.LaunchSpeed, a.HitstopSteps,
                a.MoveSpeedScale, a.ResolvesOnLanding, a.IsRadial);
            return new MagicCast(boosted, cast.ManaCost, cast.LiftSpeed);
        }

        /// <summary>The paper kit (D39): a splash ahead, an aura around, a leap up.</summary>
        public static MagicKit Default => new MagicKit(
            new MagicCast(
                new AttackTuning(
                    startupSteps: 10, activeSteps: 4, recoverySteps: 16,
                    damage: 22f, reachX: 3.2f, depthTolerance: 0.9f, lungeDistance: 0f,
                    maxTargets: 3, knockbackSpeed: 4f, launchSpeed: 0f, hitstopSteps: 2,
                    moveSpeedScale: 0.4f),
                manaCost: 20),
            new MagicCast(
                new AttackTuning(
                    startupSteps: 16, activeSteps: 5, recoverySteps: 24,
                    damage: 30f, reachX: 2.6f, depthTolerance: 2.6f, lungeDistance: 0f,
                    maxTargets: 6, knockbackSpeed: 7f, launchSpeed: 0f, hitstopSteps: 3,
                    moveSpeedScale: 0.2f, isRadial: true),
                manaCost: 45),
            new MagicCast(
                new AttackTuning(
                    startupSteps: 2, activeSteps: 3, recoverySteps: 6,
                    damage: 8f, reachX: 1.8f, depthTolerance: 1.8f, lungeDistance: 0f,
                    maxTargets: 4, knockbackSpeed: 3f, launchSpeed: 0f, hitstopSteps: 1,
                    moveSpeedScale: 1f, isRadial: true),
                manaCost: 15,
                liftSpeed: 11f));
    }

    /// <summary>
    /// Everything the combat machine needs to answer a Magic press without knowing what a mana
    /// pool or a jump is: the kit, what is affordable, and whether the airborne cast is still
    /// available this jump.
    /// </summary>
    public readonly struct MagicContext
    {
        public readonly MagicKit Kit;
        public readonly float AvailableMana;
        public readonly bool LeapAvailable;

        public MagicContext(in MagicKit kit, float availableMana, bool leapAvailable)
        {
            Kit = kit;
            AvailableMana = availableMana;
            LeapAvailable = leapAvailable;
        }

        /// <summary>No magic at all — the shape every non-casting caller passes.</summary>
        public static MagicContext None => default;

        public bool CanCast(MagicCastKind kind)
        {
            MagicCast cast = Kit.For(kind);
            if (!cast.IsAuthored || AvailableMana < cast.ManaCost)
            {
                return false;
            }

            return kind != MagicCastKind.Leap || LeapAvailable;
        }
    }
}
