using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// The §4 pipeline: base → attacker element → defender resistance and identity → environment
    /// climate → gear modifiers → final. Callers pass every stage — neutral values, never absent
    /// ones.
    /// </summary>
    /// <remarks>
    /// The climate multiplies elemental damage whoever deals it (D41). That symmetry is the
    /// design: a fire region makes your fire better *and* the enemy caster's, so region knowledge
    /// drives gear and roster choice both ways instead of being a one-sided buff.
    /// </remarks>
    public static class DamageCalculator
    {
        public static float Resolve(
            float baseDamage,
            ElementId element,
            in ElementalDefence defender,
            in ElementalMultipliers climate,
            float gearMultiplier)
        {
            float damage = Mathf.Max(0f, baseDamage)
                * defender.MultiplierFor(element)
                * climate.For(element)
                * Mathf.Max(0f, gearMultiplier);
            return Mathf.Max(0f, damage);
        }
    }
}
