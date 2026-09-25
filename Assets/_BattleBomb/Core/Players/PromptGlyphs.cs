using System;

namespace BattleBomb.Core.Players
{
    /// <summary>What a prompt is asking for — a menu role or a fight verb, never a device button.</summary>
    public enum PromptKey
    {
        Move = 0,
        Confirm = 1,
        Back = 2,
        Option = 3,
        Lock = 4,
        Tabs = 5,
        Pause = 6,
        Light = 7,
        Heavy = 8,
        Magic = 9,
        QuickUse = 10,
        Jump = 11,
    }

    /// <summary>Round for a pad's face buttons; square for its system and shoulder buttons and
    /// for every keyboard key — the design's two badge shapes.</summary>
    public enum GlyphShape
    {
        Round = 0,
        Square = 1,
    }

    /// <summary>The badge's fill. Colour itself belongs to the UI; Core only says which.</summary>
    public enum GlyphTone
    {
        Brass = 0,
        Green = 1,
        Red = 2,
        Blue = 3,
        Yellow = 4,
        Key = 5,
    }

    public readonly struct PromptGlyph
    {
        public readonly string Label;
        public readonly GlyphShape Shape;
        public readonly GlyphTone Tone;

        public PromptGlyph(string label, GlyphShape shape, GlyphTone tone)
        {
            Label = label;
            Shape = shape;
            Tone = tone;
        }
    }

    /// <summary>
    /// The picture for each prompt, per device family (D57; UI Pass 01's hint row). Xbox letters
    /// on every controller, by Michael's choice (2026-09-24). An acceptance test keeps this in
    /// step with the input asset, so a rebound action cannot quietly go on showing the old button.
    /// </summary>
    public static class PromptGlyphs
    {
        public static PromptGlyph For(InputFamily family, PromptKey key) =>
            family == InputFamily.Gamepad ? Pad(key) : Keys(key);

        private static PromptGlyph Pad(PromptKey key)
        {
            switch (key)
            {
                case PromptKey.Confirm:
                case PromptKey.Jump:
                    return Face("A", GlyphTone.Green);

                case PromptKey.Back:
                case PromptKey.Magic:
                    return Face("B", GlyphTone.Red);

                case PromptKey.Option:
                case PromptKey.Light:
                    return Face("X", GlyphTone.Blue);

                case PromptKey.Lock:
                case PromptKey.Heavy:
                    return Face("Y", GlyphTone.Yellow);

                case PromptKey.Tabs:
                    return SystemButton("LB/RB");

                case PromptKey.QuickUse:
                    return SystemButton("RB");

                case PromptKey.Pause:
                    return SystemButton("≡");

                case PromptKey.Move:
                    // The stick: the design draws it as a blank brass disc.
                    return new PromptGlyph(string.Empty, GlyphShape.Round, GlyphTone.Brass);

                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, "No pad picture for this prompt.");
            }
        }

        private static PromptGlyph Keys(PromptKey key)
        {
            switch (key)
            {
                case PromptKey.Confirm:
                    return Cap("Enter");

                case PromptKey.Back:
                case PromptKey.Pause:
                    return Cap("Esc");

                case PromptKey.Option:
                case PromptKey.Light:
                    return Cap("J");

                case PromptKey.Lock:
                case PromptKey.Heavy:
                    return Cap("K");

                case PromptKey.Tabs:
                    return Cap("Q/E");

                case PromptKey.Magic:
                    return Cap("L");

                case PromptKey.QuickUse:
                    return Cap("I");

                case PromptKey.Jump:
                    return Cap("Space");

                case PromptKey.Move:
                    return Cap("WASD");

                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, "No key cap for this prompt.");
            }
        }

        private static PromptGlyph Face(string label, GlyphTone tone) =>
            new PromptGlyph(label, GlyphShape.Round, tone);

        private static PromptGlyph SystemButton(string label) =>
            new PromptGlyph(label, GlyphShape.Square, GlyphTone.Brass);

        private static PromptGlyph Cap(string label) =>
            new PromptGlyph(label, GlyphShape.Square, GlyphTone.Key);
    }
}
