namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// One multiplier per element — the shape shared by a defender's resistances and an
    /// environment's climate (§4). 1 is neutral; below 1 resists, above 1 amplifies.
    /// <see cref="Element.None"/> is always 1.
    /// </summary>
    public readonly struct ElementalMultipliers
    {
        public readonly float Fire;
        public readonly float Water;
        public readonly float Electric;
        public readonly float Earth;

        public ElementalMultipliers(float fire, float water, float electric, float earth)
        {
            Fire = fire;
            Water = water;
            Electric = electric;
            Earth = earth;
        }

        public static ElementalMultipliers Neutral => new ElementalMultipliers(1f, 1f, 1f, 1f);

        public float For(Element element)
        {
            switch (element)
            {
                case Element.Fire: return Fire;
                case Element.Water: return Water;
                case Element.Electric: return Electric;
                case Element.Earth: return Earth;
                default: return 1f;
            }
        }
    }
}
