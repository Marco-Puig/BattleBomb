using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// The §4 pipeline: base → attacker element → defender resistance → environment climate →
    /// gear modifiers → final. Callers pass every stage — neutral values, never absent ones — so
    /// the shape M5 fills already exists from M2.
    /// </summary>
    public static class DamageCalculator
    {
        public static float Resolve(
            float baseDamage,
            ElementId element,
            in ElementalMultipliers resistance,
            in ElementalMultipliers climate,
            float gearMultiplier)
        {
            float damage = Mathf.Max(0f, baseDamage)
                * resistance.For(element)
                * climate.For(element)
                * Mathf.Max(0f, gearMultiplier);
            return Mathf.Max(0f, damage);
        }
    }
}
