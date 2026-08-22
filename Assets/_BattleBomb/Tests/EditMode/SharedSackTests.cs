using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D51: the couch shares one sack. Two inventories — two players' loadouts — over the same
    /// bag, acting in the same step, must never disagree about what is in it.
    /// </summary>
    public sealed class SharedSackTests
    {
        private static ItemInstance Helmet(int id, QualityRank rank = QualityRank.Shiny) => new ItemInstance(
            new ItemIdentity(id, $"Helm {id}", ItemSlot.Helmet), rank,
            new GearContribution(defence: 3f), new AffixRoll[0], requiredLevel: 1);

        [Test]
        public void Two_inventories_over_one_sack_see_the_same_items()
        {
            var sack = new Sack();
            var one = new Inventory(sack);
            var two = new Inventory(sack);

            one.Add(Helmet(1), currentLevel: 1);

            Assert.That(two.Items.Count, Is.EqualTo(1), "Player two sees what player one grabbed.");
            Assert.That(two.Items[0].Item.DefinitionId, Is.EqualTo(1));
            Assert.That(ReferenceEquals(one.Sack, two.Sack), Is.True);
        }

        [Test]
        public void A_sale_by_one_player_shrinks_the_other_players_view()
        {
            var sack = new Sack();
            var one = new Inventory(sack);
            var two = new Inventory(sack);
            one.Add(Helmet(1), 1);
            one.Add(Helmet(2), 1);

            int paid = two.Sell(0);

            Assert.That(paid, Is.GreaterThan(0));
            Assert.That(one.Items.Count, Is.EqualTo(1));
            Assert.That(one.Items[0].Item.DefinitionId, Is.EqualTo(2));
        }

        [Test]
        public void Equipping_moves_from_the_shared_sack_to_a_personal_loadout()
        {
            var sack = new Sack();
            var one = new Inventory(sack);
            var two = new Inventory(sack);
            one.Add(Helmet(1), 1);

            Assert.That(one.TryEquip(0, currentLevel: 1), Is.True);

            Assert.That(one.Loadout.Helmet.IsEmpty, Is.False, "Player one wears it.");
            Assert.That(two.Loadout.Helmet.IsEmpty, Is.True, "Player two does not.");
            Assert.That(two.Items.Count, Is.Zero, "It left the shared sack.");
            Assert.That(two.TryEquip(0, 1), Is.False, "Nothing left at that index for player two.");
        }

        [Test]
        public void The_displaced_piece_returns_to_the_shared_sack()
        {
            var sack = new Sack();
            var one = new Inventory(sack);
            var two = new Inventory(sack);
            one.Add(Helmet(1, QualityRank.Rusty), 1);
            one.Add(Helmet(2, QualityRank.Godly), 1);
            one.TryEquip(0, 1);

            one.TryEquip(0, 1);

            Assert.That(one.Loadout.Helmet.DefinitionId, Is.EqualTo(2));
            Assert.That(two.Items.Count, Is.EqualTo(1));
            Assert.That(two.Items[0].Item.DefinitionId, Is.EqualTo(1), "The rusty one is back for anyone.");
        }

        [Test]
        public void Settings_live_on_the_sack_and_are_therefore_shared()
        {
            var sack = new Sack();
            var one = new Inventory(sack);
            var two = new Inventory(sack);

            one.AutoSell = true;
            two.AutoEquip = true;

            Assert.That(two.AutoSell, Is.True);
            Assert.That(one.AutoEquip, Is.True);
            Assert.That(sack.AutoSell, Is.True);
        }

        [Test]
        public void The_cap_is_the_sacks_not_the_players()
        {
            var sack = new Sack { Rules = new SackRules(capacity: 2, stackLimit: 5) };
            var one = new Inventory(sack);
            var two = new Inventory(sack);
            one.Add(Helmet(1), 1);
            two.Add(Helmet(2), 1);

            Assert.That(one.Add(Helmet(3), 1).Taken, Is.False, "Two players filled two slots between them.");
            Assert.That(two.IsFull, Is.True);
        }

        [Test]
        public void A_parameterless_inventory_still_owns_a_private_sack()
        {
            var alone = new Inventory();
            alone.Add(Helmet(1), 1);

            Assert.That(alone.Items.Count, Is.EqualTo(1));
            Assert.That(new Inventory().Items.Count, Is.Zero, "Separate inventories, separate sacks.");
        }
    }
}
