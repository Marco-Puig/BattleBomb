using UnityEngine;

namespace BattleBomb.Gameplay.Simulation
{
    /// <summary>
    /// One enemy's death, announced inside the fixed step (task 36). XP is a payload nothing
    /// consumes until M4's stat system; IsElite is always false until M6 can pay an elite's
    /// loot promise — both ride along from day one so nothing is retrofitted.
    /// </summary>
    public readonly struct EnemyDeath
    {
        public readonly int Rank;
        public readonly int XpReward;
        public readonly bool IsElite;
        public readonly Vector3 Position;

        public EnemyDeath(int rank, int xpReward, bool isElite, Vector3 position)
        {
            Rank = rank;
            XpReward = xpReward;
            IsElite = isElite;
            Position = position;
        }
    }
}
