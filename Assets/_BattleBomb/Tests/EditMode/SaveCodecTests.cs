using BattleBomb.Core.Saves;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>D52: versioned, conservative. Older migrates forward; newer refuses; garbage refuses.</summary>
    public sealed class SaveCodecTests
    {
        [Test]
        public void A_fresh_save_round_trips_through_text()
        {
            SaveGame save = SaveGame.Fresh();

            string text = SaveCodec.Encode(save);
            SaveLoad load = SaveCodec.Decode(text);

            Assert.That(load.Ok, Is.True, load.Reason.ToString());
            Assert.That(load.Save.Version, Is.EqualTo(SaveCodec.CurrentVersion));
            Assert.That(load.Save.Coins, Is.Zero);
            Assert.That(load.Save.Characters.Length, Is.Zero);
        }

        [Test]
        public void The_envelope_carries_the_current_version()
        {
            string text = SaveCodec.Encode(SaveGame.Fresh());

            Assert.That(text, Does.Contain("\"_version\":" + SaveCodec.CurrentVersion)
                .Or.Contain("\"_version\": " + SaveCodec.CurrentVersion));
        }

        [Test]
        public void A_save_from_the_future_refuses_to_load_rather_than_corrupt()
        {
            string text = SaveCodec.Encode(SaveGame.Fresh())
                .Replace("\"_version\":" + SaveCodec.CurrentVersion, "\"_version\":" + (SaveCodec.CurrentVersion + 1))
                .Replace("\"_version\": " + SaveCodec.CurrentVersion, "\"_version\": " + (SaveCodec.CurrentVersion + 1));

            SaveLoad load = SaveCodec.Decode(text);

            Assert.That(load.Ok, Is.False);
            Assert.That(load.Reason, Is.EqualTo(SaveLoadReason.NewerVersion));
        }

        [Test]
        public void An_unversioned_save_migrates_forward_to_the_current_shape()
        {
            // Version 0: the field is absent entirely, which JsonUtility reads as zero.
            const string text = "{\"_coins\":42}";

            SaveLoad load = SaveCodec.Decode(text);

            Assert.That(load.Ok, Is.True, load.Reason.ToString());
            Assert.That(load.Save.Version, Is.EqualTo(SaveCodec.CurrentVersion));
            Assert.That(load.Save.Coins, Is.EqualTo(42));
            Assert.That(load.Save.Characters, Is.Not.Null, "Migration fills the arrays the old shape lacked.");
            Assert.That(load.Save.Story, Is.Not.Null);
        }

        [Test]
        public void Garbage_is_refused_with_a_reason()
        {
            Assert.That(SaveCodec.Decode("not json at all {{{").Reason, Is.EqualTo(SaveLoadReason.Corrupt));
            Assert.That(SaveCodec.Decode(string.Empty).Reason, Is.EqualTo(SaveLoadReason.Corrupt));
            Assert.That(SaveCodec.Decode(null).Reason, Is.EqualTo(SaveLoadReason.Corrupt));
        }

        [Test]
        public void A_well_formed_but_contentless_payload_loads_as_an_empty_save()
        {
            // Known limitation, deliberately kept (M7/D52 review): "{}" is well-formed JSON, so
            // it decodes Ok as an empty save indistinguishable from a fresh game. The realistic
            // ways a save actually gets damaged already come back malformed and Corrupt (see
            // Garbage_is_refused_with_a_reason), and FileSaveStore's atomic swap closes the
            // crash window that used to produce a half-written file. Telling "truncated down to
            // nothing" apart from "genuinely new" would need a checksum over the payload, which
            // M7 does not carry — it would cost hand-editing saves during development for a
            // threat this project does not yet have. If that judgement turns out wrong, this is
            // the test to change.
            SaveLoad load = SaveCodec.Decode("{}");

            Assert.That(load.Ok, Is.True);
            Assert.That(load.Save.Coins, Is.Zero);
            Assert.That(load.Save.Characters.Length, Is.Zero);
        }
    }
}
