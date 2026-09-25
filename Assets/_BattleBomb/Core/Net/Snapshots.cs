using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Net
{
    /// <summary>One player as the host simulates them: everything the guest's copy of the actor holds.
    /// Stats derived from gear never travel — only what they are derived from does (Plan 2).</summary>
    public readonly struct PlayerSnapshot
    {
        public readonly int PlayerId;
        public readonly MotorState Motor;
        public readonly CombatState Combat;
        public readonly PlayerCondition Condition;
        public readonly ReviveChannel Revive;
        public readonly ManaPool Mana;
        public readonly bool LeapAvailable;
        public readonly StatusInstance[] Statuses;

        /// <summary>The screen this player has open, as the driver's interaction kind, or -1.</summary>
        public readonly int OpenScreen;

        public readonly int GrabCount;
        public readonly int RefusedSteps;

        public PlayerSnapshot(
            int playerId, in MotorState motor, in CombatState combat, in PlayerCondition condition,
            in ReviveChannel revive, in ManaPool mana, bool leapAvailable, StatusInstance[] statuses,
            int openScreen, int grabCount, int refusedSteps)
        {
            PlayerId = playerId;
            Motor = motor;
            Combat = combat;
            Condition = condition;
            Revive = revive;
            Mana = mana;
            LeapAvailable = leapAvailable;
            Statuses = statuses ?? System.Array.Empty<StatusInstance>();
            OpenScreen = openScreen;
            GrabCount = grabCount;
            RefusedSteps = refusedSteps;
        }

        public PlayerSnapshot WithMotor(in MotorState motor) => new PlayerSnapshot(
            PlayerId, motor, Combat, Condition, Revive, Mana, LeapAvailable, Statuses, OpenScreen, GrabCount, RefusedSteps);
    }

    /// <summary>One enemy. Its archetype travels as the stage and roster index it was spawned from,
    /// which both machines resolve against the same authored chapter.</summary>
    public readonly struct EnemySnapshot
    {
        public readonly int NetId;
        public readonly int StageIndex;
        public readonly int RosterIndex;
        public readonly bool IsElite;

        /// <summary>The quality of the piece an elite wears (D22), or -1 — all the guest's tint needs.</summary>
        public readonly int CarriedQuality;

        public readonly MotorState Motor;
        public readonly EnemyState Brain;
        public readonly Health Health;
        public readonly int TargetIndex;
        public readonly int DepletedSteps;
        public readonly StatusInstance[] Statuses;

        public EnemySnapshot(
            int netId, int stageIndex, int rosterIndex, bool isElite, int carriedQuality,
            in MotorState motor, in EnemyState brain, in Health health, int targetIndex, int depletedSteps,
            StatusInstance[] statuses)
        {
            NetId = netId;
            StageIndex = stageIndex;
            RosterIndex = rosterIndex;
            IsElite = isElite;
            CarriedQuality = carriedQuality;
            Motor = motor;
            Brain = brain;
            Health = health;
            TargetIndex = targetIndex;
            DepletedSteps = depletedSteps;
            Statuses = statuses ?? System.Array.Empty<StatusInstance>();
        }

        public EnemySnapshot WithMotor(in MotorState motor) => new EnemySnapshot(
            NetId, StageIndex, RosterIndex, IsElite, CarriedQuality, motor, Brain, Health, TargetIndex, DepletedSteps, Statuses);
    }

    /// <summary>A checkpoint room's training dummy, by the stage it stands in and its prop index there.</summary>
    public readonly struct DummySnapshot
    {
        public readonly int StageIndex;
        public readonly int PropIndex;
        public readonly MotorState Motor;
        public readonly Health Health;

        public DummySnapshot(int stageIndex, int propIndex, in MotorState motor, in Health health)
        {
            StageIndex = stageIndex;
            PropIndex = propIndex;
            Motor = motor;
            Health = health;
        }
    }

    /// <summary>
    /// The host's world at one step, as the guest needs it. A class with lists so a buffer can reuse
    /// instances rather than allocate 30 a second.
    /// </summary>
    public sealed class WorldSnapshot
    {
        public int HostFrame;

        /// <summary>The guest command frame the host had played when this was taken (Plan 3's replay point).</summary>
        public int AckGuestFrame;

        public ArenaBounds Bounds = ArenaBounds.Default;
        public int AttemptStepsAllDown;

        public readonly List<PlayerSnapshot> Players = new List<PlayerSnapshot>(2);
        public readonly List<EnemySnapshot> Enemies = new List<EnemySnapshot>(32);
        public readonly List<DummySnapshot> Dummies = new List<DummySnapshot>(4);
        public readonly List<ProjectileState> Projectiles = new List<ProjectileState>(32);
        public readonly List<int> DropIds = new List<int>(16);

        public void Clear()
        {
            HostFrame = 0;
            AckGuestFrame = 0;
            Bounds = ArenaBounds.Default;
            AttemptStepsAllDown = 0;
            Players.Clear();
            Enemies.Clear();
            Dummies.Clear();
            Projectiles.Clear();
            DropIds.Clear();
        }

        public bool TryGetPlayer(int playerId, out PlayerSnapshot player)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].PlayerId == playerId)
                {
                    player = Players[i];
                    return true;
                }
            }

            player = default;
            return false;
        }

        public bool TryGetEnemy(int netId, out EnemySnapshot enemy)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i].NetId == netId)
                {
                    enemy = Enemies[i];
                    return true;
                }
            }

            enemy = default;
            return false;
        }

        public bool TryGetDummy(int stageIndex, int propIndex, out DummySnapshot dummy)
        {
            for (int i = 0; i < Dummies.Count; i++)
            {
                if (Dummies[i].StageIndex == stageIndex && Dummies[i].PropIndex == propIndex)
                {
                    dummy = Dummies[i];
                    return true;
                }
            }

            dummy = default;
            return false;
        }
    }
}
