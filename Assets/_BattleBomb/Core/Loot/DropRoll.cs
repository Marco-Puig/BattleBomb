using UnityEngine;

namespace BattleBomb.Core.Loot
{
    /// <summary>What one kill's roll decided: whether anything fell, and how good it is.</summary>
    public readonly struct DropDecision
    {
        public readonly bool Dropped;

        /// <summary>Carried even though nothing consumes it until M4/M6 — the D23 seam's shape.</summary>
        public readonly float Quality;

        public DropDecision(bool dropped, float quality)
        {
            Dropped = dropped;
            Quality = quality > 0f ? quality : 0f;
        }
    }

    /// <summary>
    /// D23's loot flow as one pure roll per kill: first *whether* — chance scales with the
    /// enemy's rank and clamps — then *quality*, compounding story progress, difficulty, and any
    /// active multipliers, with the elite bonus on top. The difficulty and progress systems are
    /// M7's; their slots exist now so nothing is retrofitted. Both draws are always consumed, so
    /// the random stream advances identically whether or not anything drops (D10).
    /// </summary>
    public static class DropRoll
    {
        public const float BaseChance = 0.10f;
        public const float ChancePerRank = 0.05f;
        public const float ChanceCap = 0.60f;

        /// <summary>The quality spread's floor — one roll spans [0.5, 1.5) before the inputs scale it.</summary>
        public const float SpreadMin = 0.5f;

        public static float Chance(int rank) =>
            Mathf.Min(ChanceCap, BaseChance + ChancePerRank * Mathf.Max(0, rank));

        public static DeterministicRandom Roll(
            in DeterministicRandom rng,
            int rank,
            float progress,
            float difficulty,
            float multipliers,
            bool isElite,
            float eliteBonus,
            out DropDecision decision)
        {
            DeterministicRandom next = rng.NextFloat(out float whether);
            next = next.NextFloat(out float spread);

            bool dropped = whether < Chance(rank);
            float quality = (SpreadMin + spread)
                * Mathf.Max(0f, progress)
                * Mathf.Max(0f, difficulty)
                * Mathf.Max(0f, multipliers)
                * (isElite ? Mathf.Max(1f, eliteBonus) : 1f);

            decision = new DropDecision(dropped, dropped ? quality : 0f);
            return next;
        }
    }
}
