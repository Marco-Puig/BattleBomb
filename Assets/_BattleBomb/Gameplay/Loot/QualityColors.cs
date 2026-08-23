using BattleBomb.Core.Items;
using UnityEngine;

namespace BattleBomb.Gameplay.Loot
{
    /// <summary>
    /// One colour per rank of D33's ladder, shared by the drop token and every card that names
    /// an item. Kept as hex so a reviewer can diff these against the design document directly.
    ///
    /// The ramp alternates warm and cool and swings lightness hard at every step, so the eight
    /// launch ranks stay countable at a 48px cell and survive deuteranopia, tritanopia and
    /// greyscale. Godly's cyan is reserved rather than reachable — see <see cref="QualityRank"/>.
    /// </summary>
    public static class QualityColors
    {
        /// <summary>Indexed by <see cref="QualityRank"/>, Nothing first.</summary>
        private static readonly string[] Hex =
        {
            "#8c8c8c",  // Nothing
            "#8a8a8f",  // Battlescarred — grey
            "#a75c28",  // Rusty — rust
            "#f2ede2",  // Torn — bone
            "#5fbf4f",  // Clean — green
            "#2b62d9",  // Shiny — blue
            "#ffd83a",  // Pristine — yellow
            "#ff7a1a",  // Legendary — orange
            "#a855f7",  // Mythical — purple
            "#73e6ff",  // Godly — cyan (reserved)
        };

        /// <summary>
        /// Shiny's blue is dark enough to fail as display type on the near-black panel, so item
        /// names lift it. Null means "the frame colour reads fine as text" — only exceptions here.
        /// </summary>
        private static readonly string[] TextHex =
        {
            null, null, null, null, null,
            "#5b8ff0",  // Shiny
            null, null, null, null,
        };

        private static Color[] _frame;
        private static Color[] _text;

        /// <summary>The rank's colour, for frames, borders and the drop token.</summary>
        public static Color For(QualityRank rank)
        {
            Build();
            return _frame[Index(rank)];
        }

        /// <summary>The rank's colour for text, lifted where the frame colour would not read.</summary>
        public static Color TextFor(QualityRank rank)
        {
            Build();
            return _text[Index(rank)];
        }

        private static int Index(QualityRank rank)
        {
            int index = (int)rank;
            return index >= 0 && index < Hex.Length ? index : 0;
        }

        private static void Build()
        {
            if (_frame != null)
            {
                return;
            }

            var frame = new Color[Hex.Length];
            var text = new Color[Hex.Length];
            for (int i = 0; i < Hex.Length; i++)
            {
                frame[i] = Parse(Hex[i]);
                text[i] = TextHex[i] != null ? Parse(TextHex[i]) : frame[i];
            }

            _text = text;
            _frame = frame;
        }

        private static Color Parse(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out Color parsed) ? parsed : Color.magenta;
    }
}
