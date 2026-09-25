using System.Collections.Generic;
using System.Text.RegularExpressions;
using BattleBomb.Core.Players;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>One entry in a hint row: which button, and what it does here.</summary>
    internal readonly struct Prompt
    {
        internal readonly PromptKey Key;
        internal readonly string Caption;
        internal readonly bool Gold;

        internal Prompt(PromptKey key, string caption, bool gold = false)
        {
            Key = key;
            Caption = caption;
            Gold = gold;
        }
    }

    /// <summary>
    /// The design's hint row (UI Pass 01, ShopPanel): a 24px badge per button — round for face
    /// buttons, squared for system and shoulder buttons and for keys — coloured per button, with
    /// its caption beside it. Drawn in the buttons of the device the player last pressed (D57).
    /// </summary>
    internal sealed class PromptRow
    {
        internal const float Height = 24f;

        private const float Gap = 7f;
        private const float Spacing = 18f;

        private static readonly Color CaptionColor = new Color(0.965f, 0.937f, 0.886f, 0.75f);

        private readonly RectTransform _root;
        private readonly Text _flash;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly HashSet<string> _labels = new HashSet<string>();

        private sealed class Entry
        {
            internal RectTransform Root;
            internal Image Badge;
            internal Text Key;
            internal Text Caption;
        }

        internal PromptRow(RectTransform parent)
        {
            _root = UiBuild.Rect("Prompts", parent);
            _flash = UiBuild.Label("Flash", _root, string.Empty, 12, UiBuild.Gold,
                TextAnchor.MiddleLeft, UiBuild.Ui);
            UiBuild.StretchX(_flash.rectTransform, 0f);
            _flash.gameObject.SetActive(false);
        }

        /// <summary>Along the bottom of the parent, between two fractions of its width, inset by
        /// pixels. The row is one line, so only its bottom edge and its ends are chosen.</summary>
        internal void Place(float fromX, float toX, float left, float bottom, float right)
        {
            _root.anchorMin = new Vector2(fromX, 0f);
            _root.anchorMax = new Vector2(toX, 0f);
            _root.pivot = Vector2.zero;
            _root.offsetMin = new Vector2(left, bottom);
            _root.offsetMax = new Vector2(-right, bottom + Height);
        }

        internal void Show(IReadOnlyList<Prompt> prompts, InputFamily family)
        {
            _flash.gameObject.SetActive(false);
            _labels.Clear();

            float limit = _root.rect.width;
            float x = 0f;
            int used = 0;
            for (int i = 0; i < prompts.Count; i++)
            {
                PromptGlyph glyph = PromptGlyphs.For(family, prompts[i].Key);

                // One button, one badge. On a keyboard Escape is Back and Pause at once, and
                // "Esc Back" beside "Esc Leave" would say two things about one key; the first wins,
                // so a screen lists Back before Pause — inside a menu Escape is a Back (MenuPress).
                if (!_labels.Add(glyph.Label))
                {
                    continue;
                }

                Entry entry = EntryAt(used);
                float width = Draw(entry, glyph, prompts[i]);
                if (used > 0 && limit > 0f && x + width > limit)
                {
                    entry.Root.gameObject.SetActive(false);
                    break;
                }

                entry.Root.anchoredPosition = new Vector2(x, 0f);
                x += width + Spacing;
                used++;
            }

            for (int i = used; i < _entries.Count; i++)
            {
                _entries[i].Root.gameObject.SetActive(false);
            }
        }

        /// <summary>A one-line message in place of the badges — what just happened, briefly.</summary>
        internal void ShowText(string text)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Root.gameObject.SetActive(false);
            }

            _flash.gameObject.SetActive(true);
            _flash.text = text;
        }

        internal static Color ToneColor(GlyphTone tone)
        {
            switch (tone)
            {
                case GlyphTone.Green:
                    return UiBuild.PadGreen;
                case GlyphTone.Red:
                    return UiBuild.PadRed;
                case GlyphTone.Blue:
                    return UiBuild.PadBlue;
                case GlyphTone.Yellow:
                    return UiBuild.PadYellow;
                case GlyphTone.Key:
                    return UiBuild.Bone;
                default:
                    return UiBuild.Brass;
            }
        }

        /// <summary>
        /// A button written into a sentence — "X to grab" — in its badge's colour, for the screens
        /// that are still one block of text: the front door, and the HUD's IMGUI cards.
        /// </summary>
        internal static string Inline(InputFamily family, PromptKey key)
        {
            PromptGlyph glyph = PromptGlyphs.For(family, key);
            return UiBuild.Tint(glyph.Label.Length > 0 ? glyph.Label : "Stick", ToneColor(glyph.Tone));
        }

        /// <summary>The text with its colour tags taken out, for a drop shadow drawn under a rich
        /// line: IMGUI lets a tag override the style's colour, so a tinted button would otherwise
        /// cast a tinted shadow.</summary>
        internal static string Plain(string text) => ColourTags.Replace(text, string.Empty);

        private static readonly Regex ColourTags = new Regex("</?color[^>]*>", RegexOptions.Compiled);

        private Entry EntryAt(int index)
        {
            while (_entries.Count <= index)
            {
                var entry = new Entry { Root = UiBuild.Rect($"Prompt {_entries.Count}", _root) };
                entry.Badge = UiBuild.Box("Badge", entry.Root, UiBuild.Brass);
                entry.Key = UiBuild.Label("Key", entry.Badge.rectTransform, string.Empty, 12,
                    UiBuild.OnBrass, TextAnchor.MiddleCenter, UiBuild.Display);
                entry.Caption = UiBuild.Label("Caption", entry.Root, string.Empty, 12, CaptionColor,
                    TextAnchor.MiddleLeft, UiBuild.Ui);
                entry.Caption.horizontalOverflow = HorizontalWrapMode.Overflow;
                _entries.Add(entry);
            }

            return _entries[index];
        }

        private static float Draw(Entry entry, in PromptGlyph glyph, in Prompt prompt)
        {
            entry.Root.gameObject.SetActive(true);

            bool round = glyph.Shape == GlyphShape.Round;
            float badge = round ? Height : Mathf.Max(Height, 12f + 7f * glyph.Label.Length);

            entry.Badge.sprite = round ? UiBuild.Disc : UiBuild.RoundedSquare;
            entry.Badge.type = round ? Image.Type.Simple : Image.Type.Sliced;
            entry.Badge.pixelsPerUnitMultiplier = round ? 1f : UiBuild.RoundedSquareScale;
            entry.Badge.color = ToneColor(glyph.Tone);
            UiBuild.Pin(entry.Badge.rectTransform, 0f, 0f, badge, Height);

            entry.Key.text = glyph.Label;
            entry.Key.fontSize = glyph.Label.Length > 2 ? 11 : 12;
            // Passion One has no ≡; Archivo does, and a font fallback would only save it on Windows.
            // Both are null until a chest host loads them, as on the front door at first boot.
            Font keyFont = glyph.Label == "≡" ? UiBuild.Ui : UiBuild.Display;
            entry.Key.font = keyFont != null ? keyFont : UiBuild.Font;

            entry.Caption.text = prompt.Caption;
            entry.Caption.color = prompt.Gold ? UiBuild.Gold : CaptionColor;
            float caption = Mathf.Ceil(entry.Caption.preferredWidth);
            UiBuild.Pin(entry.Caption.rectTransform, badge + Gap, 0f, caption + 2f, Height);

            float width = badge + Gap + caption;
            UiBuild.Pin(entry.Root, 0f, 0f, width, Height);
            return width;
        }
    }
}
