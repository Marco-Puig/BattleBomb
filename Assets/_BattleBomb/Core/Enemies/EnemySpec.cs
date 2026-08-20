using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;

namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// Everything one authored enemy is, as plain data: the brain's tuning, the motor's tuning,
    /// vitals, resistances (§4 — real authored slots from M3, player-visible when M5 gives
    /// elements teeth), and the reward inputs D23's drop roll reads (rank; XP is a payload with
    /// no ladder until M4). ScriptableObject in, this struct out — a new enemy is data (D22).
    /// </summary>
    public readonly struct EnemySpec
    {
        public readonly EnemyTuning Tuning;
        public readonly MovementTuning Movement;
        public readonly float MaxHealth;
        public readonly ElementalMultipliers Resistances;

        /// <summary>Resistances plus the skin's own element — what the damage pipeline asks for.</summary>
        public ElementalDefence Defence => new ElementalDefence(Tuning.Element, Resistances);
        public readonly int Rank;
        public readonly int XpReward;

        public EnemySpec(
            EnemyTuning tuning,
            MovementTuning movement,
            float maxHealth,
            ElementalMultipliers resistances,
            int rank,
            int xpReward)
        {
            Tuning = tuning;
            Movement = movement;
            MaxHealth = maxHealth;
            Resistances = resistances;
            Rank = rank;
            XpReward = xpReward;
        }
    }
}
