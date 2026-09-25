using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class ItemWireTests
    {
        [Test]
        public void An_item_crosses_with_every_rolled_value()
        {
            var item = new ItemInstance(
                new ItemIdentity(7, "Clean Knife", ItemSlot.Weapon, WeaponClass.Sword, PetClass.None),
                QualityRank.Clean,
                new GearContribution(weaponDamage: 12f, critChance: 0.25f),
                new[] { new AffixRoll((AffixId)1, 0.5f, new ElementId(2)) },
                4,
                new ItemInvestment(3, 1, true),
                0f,
                default,
                new ActivePayload(0.5f, new ElementId(1), 2f, 180));

            var writer = new NetWriter();
            ItemWire.Write(writer, item);
            ItemInstance back = ItemWire.Read(new NetReader(writer.ToArray()), null);

            Assert.That(back.DefinitionId, Is.EqualTo(7));
            Assert.That(back.DisplayName, Is.EqualTo("Clean Knife"));
            Assert.That(back.Quality, Is.EqualTo(QualityRank.Clean));
            Assert.That(back.CoreStats.WeaponDamage, Is.EqualTo(12f));
            Assert.That(back.CoreStats.CritChance, Is.EqualTo(0.25f));
            Assert.That(back.AffixCount, Is.EqualTo(1));
            Assert.That(back.Affixes[0].Magnitude, Is.EqualTo(0.5f));
            Assert.That(back.Affixes[0].Element.Value, Is.EqualTo(2));
            Assert.That(back.RequiredLevel, Is.EqualTo(4));
            Assert.That(back.Locked, Is.True);
            Assert.That(back.UpgradeCapacity, Is.EqualTo(3));
            Assert.That(back.ActiveCooldownSteps, Is.EqualTo(180));
        }

        [Test]
        public void An_empty_item_stays_empty()
        {
            var writer = new NetWriter();
            ItemWire.Write(writer, default);
            Assert.That(ItemWire.Read(new NetReader(writer.ToArray()), null).IsEmpty, Is.True);
        }

        [Test]
        public void Text_that_is_not_an_item_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteString("{ not json");
            Assert.Throws<NetFormatException>(() => ItemWire.Read(new NetReader(writer.ToArray()), null));
        }

        [Test]
        public void A_stand_in_carries_only_its_quality()
        {
            ItemInstance standIn = ItemWire.StandIn(QualityRank.Legendary);
            Assert.That(standIn.IsEmpty, Is.False);
            Assert.That(standIn.Quality, Is.EqualTo(QualityRank.Legendary));
        }
    }
}
