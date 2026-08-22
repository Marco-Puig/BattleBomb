namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// What happens when one element meets another (D40) — the same-element rule, in one place so
    /// no damage path can forget it.
    /// </summary>
    /// <remarks>
    /// Matching elements halve elemental damage and block the status outright, both ways: a Fire
    /// being cannot be burned, and its own fire hurts you less. Halved rather than nulled on
    /// purpose — immunity would make whole matchups feel dead, while half damage prices the
    /// matchup without deleting it. Kinetic damage (<see cref="ElementId.None"/>) is never
    /// touched, so an infused blade against its own element behaves exactly like a plain one and
    /// never rolls as a penalty.
    /// </remarks>
    public static class ElementalExchange
    {
        /// <summary>What a matching element's damage is worth, in both directions (paper value).</summary>
        public const float SameElementDamageScale = 0.5f;

        public static bool SameElement(ElementId attacker, ElementId defender) =>
            !attacker.IsNone && attacker == defender;

        public static float DamageScale(ElementId attacker, ElementId defender) =>
            SameElement(attacker, defender) ? SameElementDamageScale : 1f;

        /// <summary>A status never marks something already made of that element.</summary>
        public static bool CanApplyStatus(ElementId attacker, ElementId defender) =>
            !attacker.IsNone && !SameElement(attacker, defender);
    }
}
