namespace BattleBomb.Core.Net
{
    /// <summary>The host's 30 Hz picture of the world (HANDOFF-M8 paper numbers). Unreliable: a lost
    /// one is simply replaced by the next.</summary>
    public static class SnapshotCodec
    {
        public const int MaxPlayers = 4;

        public static void Write(NetWriter w, WorldSnapshot s)
        {
            w.WriteByte((byte)NetMessageKind.Snapshot);
            w.WriteInt(s.HostFrame);
            w.WriteInt(s.AckGuestFrame);
            StateCodec.Write(w, s.Bounds);
            w.WriteInt(s.AttemptStepsAllDown);

            w.WriteCount(s.Players.Count, MaxPlayers);
            for (int i = 0; i < s.Players.Count; i++)
            {
                PlayerSnapshot p = s.Players[i];
                w.WriteInt(p.PlayerId);
                StateCodec.Write(w, p.Motor);
                StateCodec.Write(w, p.Combat);
                StateCodec.Write(w, p.Condition);
                StateCodec.Write(w, p.Revive);
                StateCodec.Write(w, p.Mana);
                w.WriteBool(p.LeapAvailable);
                StateCodec.Write(w, p.Statuses);
                w.WriteInt(p.OpenScreen);
                w.WriteInt(p.GrabCount);
                w.WriteInt(p.RefusedSteps);
            }

            w.WriteCount(s.Enemies.Count, NetProtocol.MaxEntities);
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                EnemySnapshot e = s.Enemies[i];
                w.WriteInt(e.NetId);
                w.WriteInt(e.StageIndex);
                w.WriteInt(e.RosterIndex);
                w.WriteBool(e.IsElite);
                w.WriteInt(e.CarriedQuality);
                StateCodec.Write(w, e.Motor);
                StateCodec.Write(w, e.Brain);
                StateCodec.Write(w, e.Health);
                w.WriteInt(e.TargetIndex);
                w.WriteInt(e.DepletedSteps);
                StateCodec.Write(w, e.Statuses);
            }

            w.WriteCount(s.Dummies.Count, NetProtocol.MaxEntities);
            for (int i = 0; i < s.Dummies.Count; i++)
            {
                DummySnapshot d = s.Dummies[i];
                w.WriteInt(d.StageIndex);
                w.WriteInt(d.PropIndex);
                StateCodec.Write(w, d.Motor);
                StateCodec.Write(w, d.Health);
            }

            w.WriteCount(s.Projectiles.Count, NetProtocol.MaxEntities);
            for (int i = 0; i < s.Projectiles.Count; i++)
            {
                StateCodec.Write(w, s.Projectiles[i]);
            }
        }

        /// <summary>Reads into <paramref name="into"/>, clearing it first. The reader sits after the kind byte.</summary>
        public static void Read(NetReader r, WorldSnapshot into)
        {
            into.Clear();
            into.HostFrame = r.ReadInt();
            into.AckGuestFrame = r.ReadInt();
            into.Bounds = StateCodec.ReadBounds(r);
            into.AttemptStepsAllDown = r.ReadInt();

            int players = r.ReadCount(MaxPlayers);
            for (int i = 0; i < players; i++)
            {
                into.Players.Add(new PlayerSnapshot(
                    r.ReadInt(), StateCodec.ReadMotor(r), StateCodec.ReadCombat(r), StateCodec.ReadCondition(r),
                    StateCodec.ReadRevive(r), StateCodec.ReadMana(r), r.ReadBool(), StateCodec.ReadStatuses(r),
                    r.ReadInt(), r.ReadInt(), r.ReadInt()));
            }

            int enemies = r.ReadCount(NetProtocol.MaxEntities);
            for (int i = 0; i < enemies; i++)
            {
                into.Enemies.Add(new EnemySnapshot(
                    r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadBool(), r.ReadInt(),
                    StateCodec.ReadMotor(r), StateCodec.ReadBrain(r), StateCodec.ReadHealth(r),
                    r.ReadInt(), r.ReadInt(), StateCodec.ReadStatuses(r)));
            }

            int dummies = r.ReadCount(NetProtocol.MaxEntities);
            for (int i = 0; i < dummies; i++)
            {
                into.Dummies.Add(new DummySnapshot(r.ReadInt(), r.ReadInt(), StateCodec.ReadMotor(r), StateCodec.ReadHealth(r)));
            }

            int projectiles = r.ReadCount(NetProtocol.MaxEntities);
            for (int i = 0; i < projectiles; i++)
            {
                into.Projectiles.Add(StateCodec.ReadProjectile(r));
            }
        }
    }
}
