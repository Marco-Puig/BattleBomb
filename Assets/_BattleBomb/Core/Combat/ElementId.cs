using System;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// One element's identity (D38). Elements are authored assets, so Core carries only the id —
    /// nothing in here knows whether id 1 is called Fire, which is exactly what keeps the roster
    /// (O11) a content question rather than a code one. 0 is None: elementless kinetic damage,
    /// which resistances, climates, and statuses never touch.
    /// </summary>
    public readonly struct ElementId : IEquatable<ElementId>
    {
        public readonly int Value;

        public ElementId(int value)
        {
            Value = value > 0 ? value : 0;
        }

        public static ElementId None => default;

        public bool IsNone => Value <= 0;

        public bool Equals(ElementId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ElementId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(ElementId left, ElementId right) => left.Value == right.Value;

        public static bool operator !=(ElementId left, ElementId right) => left.Value != right.Value;

        /// <summary>Debug text only — player-facing names come from the catalog, never from here.</summary>
        public override string ToString() => Value <= 0 ? "None" : $"Element {Value}";
    }
}
