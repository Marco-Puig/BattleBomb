using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>One authored row of an <see cref="ElementalMultipliers"/> table.</summary>
    public readonly struct ElementalMultiplier
    {
        public readonly ElementId Element;
        public readonly float Value;

        public ElementalMultiplier(ElementId element, float value)
        {
            Element = element;
            Value = value;
        }
    }

    /// <summary>
    /// One multiplier per element — the shape shared by a defender's resistances and an
    /// environment's climate (§4). 1 is neutral; below 1 resists, above 1 amplifies.
    /// <see cref="ElementId.None"/> is always 1.
    /// </summary>
    /// <remarks>
    /// Indexed by element id (D38), so a table costs nothing to extend when the roster grows. The
    /// default value is neutral for every element: an unauthored table is safe rather than absent,
    /// which matters because these travel inside specs that are often left at their defaults.
    /// </remarks>
    public readonly struct ElementalMultipliers
    {
        private readonly float[] _byElement;

        private ElementalMultipliers(float[] byElement)
        {
            _byElement = byElement;
        }

        public static ElementalMultipliers Neutral => default;

        /// <summary>Builds a table from authored rows; every element no row mentions stays neutral.</summary>
        public static ElementalMultipliers From(IReadOnlyList<ElementalMultiplier> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return default;
            }

            int highest = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                highest = Mathf.Max(highest, entries[i].Element.Value);
            }

            if (highest <= 0)
            {
                return default;
            }

            float[] values = new float[highest + 1];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = 1f;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                int index = entries[i].Element.Value;
                if (index > 0)
                {
                    values[index] = Mathf.Max(0f, entries[i].Value);
                }
            }

            return new ElementalMultipliers(values);
        }

        /// <summary>One element moved off neutral, everything else left alone.</summary>
        public static ElementalMultipliers Single(ElementId element, float multiplier) =>
            From(new[] { new ElementalMultiplier(element, multiplier) });

        public float For(ElementId element)
        {
            int index = element.Value;
            return _byElement == null || index <= 0 || index >= _byElement.Length
                ? 1f
                : _byElement[index];
        }
    }
}
