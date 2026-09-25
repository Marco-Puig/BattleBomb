using System;
using BattleBomb.Core.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>The picture for every prompt (D57; UI Pass 01's hint row).</summary>
    public sealed class PromptGlyphsTests
    {
        [Test]
        public void Every_prompt_has_a_picture_on_both_families()
        {
            foreach (PromptKey key in Enum.GetValues(typeof(PromptKey)))
            {
                Assert.That(PromptGlyphs.For(InputFamily.Keyboard, key).Label, Is.Not.Empty,
                    $"No key cap for {key}.");
                Assert.That(PromptGlyphs.For(InputFamily.Gamepad, key).Label, Is.Not.Null,
                    $"No pad picture for {key}.");
            }
        }

        [Test]
        public void Face_buttons_are_round_and_coloured_and_system_buttons_are_square()
        {
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Confirm).Shape, Is.EqualTo(GlyphShape.Round));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Confirm).Tone, Is.EqualTo(GlyphTone.Green));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Back).Tone, Is.EqualTo(GlyphTone.Red));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Option).Tone, Is.EqualTo(GlyphTone.Blue));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Lock).Tone, Is.EqualTo(GlyphTone.Yellow));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Pause).Shape, Is.EqualTo(GlyphShape.Square));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Tabs).Shape, Is.EqualTo(GlyphShape.Square));
        }

        [Test]
        public void Every_keyboard_key_is_a_key_cap()
        {
            foreach (PromptKey key in Enum.GetValues(typeof(PromptKey)))
            {
                PromptGlyph glyph = PromptGlyphs.For(InputFamily.Keyboard, key);
                Assert.That(glyph.Shape, Is.EqualTo(GlyphShape.Square), key.ToString());
                Assert.That(glyph.Tone, Is.EqualTo(GlyphTone.Key), key.ToString());
            }
        }

        [Test]
        public void A_button_the_fight_and_the_menu_share_shows_the_same_picture()
        {
            Assert.That(Label(PromptKey.Confirm), Is.EqualTo(Label(PromptKey.Jump)), "A");
            Assert.That(Label(PromptKey.Back), Is.EqualTo(Label(PromptKey.Magic)), "B");
            Assert.That(Label(PromptKey.Option), Is.EqualTo(Label(PromptKey.Light)), "X");
            Assert.That(Label(PromptKey.Lock), Is.EqualTo(Label(PromptKey.Heavy)), "Y");
        }

        [Test]
        public void Every_pad_prompt_has_its_label_shape_and_colour()
        {
            var expected = new (PromptKey Key, string Label, GlyphShape Shape, GlyphTone Tone)[]
            {
                (PromptKey.Move, "", GlyphShape.Round, GlyphTone.Brass),
                (PromptKey.Confirm, "A", GlyphShape.Round, GlyphTone.Green),
                (PromptKey.Jump, "A", GlyphShape.Round, GlyphTone.Green),
                (PromptKey.Back, "B", GlyphShape.Round, GlyphTone.Red),
                (PromptKey.Magic, "B", GlyphShape.Round, GlyphTone.Red),
                (PromptKey.Option, "X", GlyphShape.Round, GlyphTone.Blue),
                (PromptKey.Light, "X", GlyphShape.Round, GlyphTone.Blue),
                (PromptKey.Lock, "Y", GlyphShape.Round, GlyphTone.Yellow),
                (PromptKey.Heavy, "Y", GlyphShape.Round, GlyphTone.Yellow),
                (PromptKey.Tabs, "LB/RB", GlyphShape.Square, GlyphTone.Brass),
                (PromptKey.QuickUse, "RB", GlyphShape.Square, GlyphTone.Brass),
                (PromptKey.Pause, "≡", GlyphShape.Square, GlyphTone.Brass),
            };

            Assert.That(expected.Length, Is.EqualTo(Enum.GetValues(typeof(PromptKey)).Length),
                "A prompt was added without a row here.");
            foreach (var row in expected)
            {
                PromptGlyph glyph = PromptGlyphs.For(InputFamily.Gamepad, row.Key);
                Assert.That((glyph.Label, glyph.Shape, glyph.Tone), Is.EqualTo((row.Label, row.Shape, row.Tone)),
                    row.Key.ToString());
            }
        }

        private static string Label(PromptKey key) => PromptGlyphs.For(InputFamily.Gamepad, key).Label;
    }
}
