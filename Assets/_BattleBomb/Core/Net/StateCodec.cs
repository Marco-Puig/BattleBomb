using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Every Core struct that travels, field by field, in declaration order. Each writer has a reader
    /// that reads the same fields in the same order and rebuilds through the public constructor or a
    /// named factory. The field-coverage tests hold every pair to "nothing forgotten".
    /// </summary>
    public static class StateCodec
    {
        public static void Write(NetWriter w, in MotorState s)
        {
            w.WriteVector3(s.Position);
            w.WriteVector3(s.Velocity);
            w.WriteByte(s.Facing == Facing.Left ? (byte)0 : (byte)1);
            w.WriteBool(s.IsGrounded);
            w.WriteInt(s.StepsSinceGrounded);
            w.WriteInt(s.JumpBufferedFor);
        }

        public static MotorState ReadMotor(NetReader r) => new MotorState(
            r.ReadVector3(), r.ReadVector3(), r.ReadByte() == 0 ? Facing.Left : Facing.Right,
            r.ReadBool(), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in AttackTuning a)
        {
            w.WriteInt(a.StartupSteps);
            w.WriteInt(a.ActiveSteps);
            w.WriteInt(a.RecoverySteps);
            w.WriteFloat(a.Damage);
            w.WriteFloat(a.ReachX);
            w.WriteFloat(a.DepthTolerance);
            w.WriteFloat(a.LungeDistance);
            w.WriteInt(a.MaxTargets);
            w.WriteFloat(a.KnockbackSpeed);
            w.WriteFloat(a.LaunchSpeed);
            w.WriteInt(a.HitstopSteps);
            w.WriteFloat(a.MoveSpeedScale);
            w.WriteBool(a.ResolvesOnLanding);
            w.WriteBool(a.IsRadialAuthored);
            w.WriteInt(a.StunSteps);
        }

        public static AttackTuning ReadAttack(NetReader r) => new AttackTuning(
            startupSteps: r.ReadInt(),
            activeSteps: r.ReadInt(),
            recoverySteps: r.ReadInt(),
            damage: r.ReadFloat(),
            reachX: r.ReadFloat(),
            depthTolerance: r.ReadFloat(),
            lungeDistance: r.ReadFloat(),
            maxTargets: r.ReadInt(),
            knockbackSpeed: r.ReadFloat(),
            launchSpeed: r.ReadFloat(),
            hitstopSteps: r.ReadInt(),
            moveSpeedScale: r.ReadFloat(),
            resolvesOnLanding: r.ReadBool(),
            isRadial: r.ReadBool(),
            stunSteps: r.ReadInt());

        public static void Write(NetWriter w, in CombatState c)
        {
            w.WriteByte((byte)c.Phase);
            w.WriteInt(c.StepsInPhase);
            w.WriteInt(c.ComboIndex);
            w.WriteInt(c.ComboWindowLeft);
            w.WriteUInt((uint)c.Buffered);
            w.WriteInt(c.BufferedFor);
            w.WriteInt(c.ChargeSteps);
            w.WriteInt(c.HitstopSteps);
            Write(w, c.CurrentAttack);
            w.WriteByte((byte)c.BufferedCast);
            w.WriteByte((byte)c.CurrentCast);
        }

        public static CombatState ReadCombat(NetReader r) => new CombatState(
            (AttackPhase)r.ReadByte(), r.ReadInt(), r.ReadInt(), r.ReadInt(), (CommandButtons)r.ReadUInt(),
            r.ReadInt(), r.ReadInt(), r.ReadInt(), ReadAttack(r), (MagicCastKind)r.ReadByte(), (MagicCastKind)r.ReadByte());

        public static void Write(NetWriter w, in Health h)
        {
            w.WriteFloat(h.Max);
            w.WriteFloat(h.Current);
        }

        public static Health ReadHealth(NetReader r) => Health.FromValues(r.ReadFloat(), r.ReadFloat());

        public static void Write(NetWriter w, in PlayerCondition c)
        {
            Write(w, c.Health);
            w.WriteInt(c.StaggerSteps);
            w.WriteInt(c.GraceSteps);
        }

        public static PlayerCondition ReadCondition(NetReader r) =>
            new PlayerCondition(ReadHealth(r), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in ReviveChannel v)
        {
            w.WriteInt(v.TargetIndex);
            w.WriteFloat(v.Progress);
            w.WriteInt(v.StepsElapsed);
            w.WriteInt(v.StepsSincePump);
            w.WriteFloat(v.AccuracySum);
            w.WriteInt(v.Pumps);
        }

        public static ReviveChannel ReadRevive(NetReader r) => new ReviveChannel(
            r.ReadInt(), r.ReadFloat(), r.ReadInt(), r.ReadInt(), r.ReadFloat(), r.ReadInt());

        public static void Write(NetWriter w, in ManaPool m)
        {
            w.WriteFloat(m.Max);
            w.WriteFloat(m.Current);
        }

        public static ManaPool ReadMana(NetReader r) => ManaPool.FromValues(r.ReadFloat(), r.ReadFloat());

        public static void Write(NetWriter w, in EnemyState s)
        {
            w.WriteByte((byte)s.Phase);
            w.WriteInt(s.StepsInPhase);
            w.WriteInt(s.HitstopSteps);
            w.WriteInt(s.Seed);
            w.WriteInt(s.AgeSteps);
        }

        public static EnemyState ReadBrain(NetReader r) =>
            new EnemyState((EnemyPhase)r.ReadByte(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in StatusInstance s)
        {
            w.WriteInt(s.Element.Value);
            w.WriteInt(s.RemainingSteps);
            w.WriteInt(s.StepsToTick);
            w.WriteInt(s.TickSteps);
            w.WriteFloat(s.DamagePerTick);
            w.WriteFloat(s.MoveScale);
        }

        public static StatusInstance ReadStatus(NetReader r) => new StatusInstance(
            new ElementId(r.ReadInt()), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadFloat(), r.ReadFloat());

        public static void Write(NetWriter w, StatusInstance[] statuses)
        {
            w.WriteCount(statuses.Length, NetProtocol.MaxStatuses);
            for (int i = 0; i < statuses.Length; i++)
            {
                Write(w, statuses[i]);
            }
        }

        public static StatusInstance[] ReadStatuses(NetReader r)
        {
            int count = r.ReadCount(NetProtocol.MaxStatuses);
            var statuses = count == 0 ? System.Array.Empty<StatusInstance>() : new StatusInstance[count];
            for (int i = 0; i < statuses.Length; i++)
            {
                statuses[i] = ReadStatus(r);
            }

            return statuses;
        }

        public static void Write(NetWriter w, in ProjectileState p)
        {
            w.WriteVector3(p.Position);
            w.WriteVector3(p.Velocity);
            w.WriteFloat(p.Damage);
            w.WriteInt(p.Element.Value);
            w.WriteInt(p.LifeSteps);
            w.WriteInt(p.OwnerPlayerId);
        }

        public static ProjectileState ReadProjectile(NetReader r) => new ProjectileState(
            r.ReadVector3(), r.ReadVector3(), r.ReadFloat(), new ElementId(r.ReadInt()), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in ArenaBounds b)
        {
            w.WriteFloat(b.MinX);
            w.WriteFloat(b.MaxX);
            w.WriteFloat(b.GroundY);
        }

        public static ArenaBounds ReadBounds(NetReader r)
        {
            float minX = r.ReadFloat();
            float maxX = r.ReadFloat();
            float groundY = r.ReadFloat();
            if (!(maxX >= minX))
            {
                throw new NetFormatException($"Arena bounds {minX}..{maxX} are inverted.");
            }

            return new ArenaBounds(minX, maxX, groundY);
        }
    }
}
