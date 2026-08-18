using System.Collections.Generic;
using BattleBomb.Core.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class PlayerIdTests
    {
        [Test]
        public void Equal_values_compare_equal_through_Equals_and_operators()
        {
            PlayerId first = new PlayerId(3);
            PlayerId second = new PlayerId(3);

            Assert.That(first.Equals(second), Is.True);
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
        }

        [Test]
        public void Different_values_compare_unequal_through_operators()
        {
            PlayerId first = new PlayerId(3);
            PlayerId second = new PlayerId(4);

            Assert.That(first == second, Is.False);
            Assert.That(first != second, Is.True);
        }

        [Test]
        public void Equal_values_have_equal_hash_codes()
        {
            PlayerId first = new PlayerId(6);
            PlayerId second = new PlayerId(6);

            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void Named_player_slots_have_human_readable_names()
        {
            Assert.That(PlayerId.One.ToString(), Is.EqualTo("P1"));
            Assert.That(PlayerId.Two.ToString(), Is.EqualTo("P2"));
        }

        [Test]
        public void An_equal_player_id_resolves_a_dictionary_entry()
        {
            var names = new Dictionary<PlayerId, string>
            {
                [PlayerId.One] = "First player",
            };

            Assert.That(names.TryGetValue(new PlayerId(0), out string name), Is.True);
            Assert.That(name, Is.EqualTo("First player"));
        }
    }
}
