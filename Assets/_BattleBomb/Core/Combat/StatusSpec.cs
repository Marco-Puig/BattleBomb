using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// What an element does to whatever it marks (D38) — Fire's is Burn. Authored on the element
    /// asset, so a new element's status is content like the element itself.
    /// </summary>
    /// <remarks>
    /// <see cref="DamageShare"/> is the whole status's damage as a share of the hit that applied
    /// it, not a per-tick number: pricing from the applying hit is what makes gear scaling flow
    /// into statuses for free (a Godly weapon's Burn is a Godly Burn) with no second tuning axis.
    /// </remarks>
    public readonly struct StatusSpec
    {
        public readonly string Name;
        public readonly int DurationSteps;
        public readonly int TickSteps;
        public readonly float DamageShare;

        /// <summary>
        /// Movement multiplier while marked (D46) — 1 leaves movement alone; Chill's 0.55 is the
        /// first authored slow. Clamped to at most 1: a status is an affliction, never a haste.
        /// </summary>
        public readonly float MoveScale;

        public StatusSpec(string name, int durationSteps, int tickSteps, float damageShare,
            float moveScale = 1f)
        {
            Name = string.IsNullOrEmpty(name) ? "Status" : name;
            DurationSteps = Mathf.Max(0, durationSteps);
            TickSteps = Mathf.Max(1, tickSteps);
            DamageShare = Mathf.Max(0f, damageShare);
            MoveScale = Mathf.Clamp01(moveScale);
        }

        /// <summary>An element that marks nothing — legal, and the default.</summary>
        public bool IsIdle => DurationSteps <= 0;

        /// <summary>How many times a full-length application would tick.</summary>
        public int TickCount => Mathf.Max(1, DurationSteps / TickSteps);

        /// <summary>
        /// The pricing rule (D38/D40): the applying hit's already-resolved damage decides the
        /// status's strength, the source decides its share of that — a cast's mark is twice an
        /// infusion's — and the defender's resistance to this element shortens what lands (D40).
        /// </summary>
        public void Price(
            float hitDamage, float sourceScale, float resistanceMultiplier,
            out int durationSteps, out float damagePerTick)
        {
            durationSteps = Mathf.RoundToInt(DurationSteps * Mathf.Max(0f, resistanceMultiplier));
            damagePerTick = Mathf.Max(0f, hitDamage) * DamageShare * Mathf.Max(0f, sourceScale) / TickCount;
        }
    }
}
