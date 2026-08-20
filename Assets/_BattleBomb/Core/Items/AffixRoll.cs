using BattleBomb.Core.Combat;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// One rolled affix on one item — immutable from the moment it drops (D35). The element only
    /// means something on the element-flavoured affixes and stays None everywhere else.
    /// </summary>
    public readonly struct AffixRoll
    {
        public readonly AffixId Id;
        public readonly float Magnitude;
        public readonly Element Element;

        public AffixRoll(AffixId id, float magnitude, Element element = Element.None)
        {
            Id = id;
            Magnitude = magnitude;
            Element = element;
        }
    }
}
