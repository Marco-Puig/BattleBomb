using BattleBomb.Core.Items;

namespace BattleBomb.Gameplay.Loot
{
    /// <summary>The cell's silhouette. Rank is countable from shape alone by the time it matters.</summary>
    public enum FrameShape
    {
        Rect = 0,
        Octagon = 1,
        Hexagon = 2,
    }

    /// <summary>How one rank's cell frame is drawn. Sizes are fractions of the cell so a frame
    /// reads the same at a 96px solo cell and a 48px split one.</summary>
    public readonly struct QualityFrame
    {
        /// <summary>The outline the cell is cut to.</summary>
        public readonly FrameShape Shape;

        /// <summary>Border thickness as a fraction of the cell's width.</summary>
        public readonly float BorderFraction;

        /// <summary>A second ring inside the border, separated by a gap of bed colour.</summary>
        public readonly bool InnerRidge;

        /// <summary>A halo outside the frame. The top rungs only.</summary>
        public readonly bool Glow;

        public QualityFrame(float borderFraction, bool innerRidge, bool glow, FrameShape shape = FrameShape.Rect)
        {
            BorderFraction = borderFraction;
            InnerRidge = innerRidge;
            Glow = glow;
            Shape = shape;
        }
    }

    /// <summary>
    /// Rank's second channel, alongside hue: frame weight. Colour alone fails a colourblind
    /// player and fails a photograph of a couch; weight is countable in greyscale.
    ///
    /// The bottom three rungs deliberately share one plain frame, so nothing in the early game
    /// suggests a prize. The ladder starts working at Clean and escalates from there — thicker,
    /// then a double ridge, then a cut silhouette, then a halo. By Legendary the rank is readable
    /// from the cell's outline alone, with no colour at all.
    /// </summary>
    public static class QualityFrames
    {
        private static readonly QualityFrame[] Frames =
        {
            new QualityFrame(0.019f, false, false),  // Nothing
            new QualityFrame(0.019f, false, false),  // Battlescarred ─┐
            new QualityFrame(0.019f, false, false),  // Rusty          ├ one plain frame
            new QualityFrame(0.019f, false, false),  // Torn          ─┘
            new QualityFrame(0.028f, false, false),  // Clean — the ladder starts here
            new QualityFrame(0.038f, false, false),  // Shiny
            new QualityFrame(0.047f, true, false),   // Pristine
            new QualityFrame(0.047f, true, false, FrameShape.Octagon),   // Legendary
            new QualityFrame(0.047f, true, true, FrameShape.Hexagon),    // Mythical
            new QualityFrame(0.056f, true, true, FrameShape.Hexagon),    // Godly (reserved)
        };

        public static QualityFrame For(QualityRank rank)
        {
            int index = (int)rank;
            return index >= 0 && index < Frames.Length ? Frames[index] : Frames[0];
        }
    }
}
