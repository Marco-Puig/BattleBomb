namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// A defender's whole elemental identity: what it <em>is</em>, and what it resists (§4, D40).
    /// Every damage resolution states it, which is what keeps the same-element rule automatic
    /// rather than remembered.
    /// </summary>
    /// <remarks>
    /// The default is the honest neutral: elementless, resisting nothing. Player resistance grows
    /// here from gear (D35's per-element affix); an enemy's comes from its authored definition.
    /// </remarks>
    public readonly struct ElementalDefence
    {
        public readonly ElementId Element;
        public readonly ElementalMultipliers Resistance;

        public ElementalDefence(ElementId element, in ElementalMultipliers resistance)
        {
            Element = element;
            Resistance = resistance;
        }

        public ElementalDefence(in ElementalMultipliers resistance)
            : this(ElementId.None, resistance)
        {
        }

        public static ElementalDefence None => default;

        /// <summary>How much of an incoming element survives this defender's resistance and identity.</summary>
        public float MultiplierFor(ElementId element) =>
            Resistance.For(element) * ElementalExchange.DamageScale(element, Element);
    }
}
