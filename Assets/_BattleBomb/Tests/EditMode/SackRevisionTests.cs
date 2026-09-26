using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// HANDOFF-M8 planning decision 11: a request that names a place in the sack carries the revision
    /// the player was looking at, and the host refuses one aimed at a sack that has changed since. That
    /// only works if every change to what the sack holds moves the number — and nothing else does.
    /// </summary>
    public sealed class SackRevisionTests
    {
        private const int KnifeId = 4;
        private const int DraughtId = 50;

        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(
                new ItemIdentity(KnifeId, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
                new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f)),
        };

        private static GenerationContext Context() =>
            new GenerationContext(3f, 10, Catalog, QualityTable.Default, DropWeights.Default);

        private static ItemInstance Knife() => new ItemInstance(
            new ItemIdentity(KnifeId, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
            QualityRank.Shiny,
            new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f),
            new AffixRoll[0],
            requiredLevel: 10,
            new ItemInvestment(3));

        private static ItemInstance Draught() => new ItemInstance(
            new ItemIdentity(DraughtId, "Draught", ItemSlot.Consumable),
            QualityRank.Shiny,
            GearContribution.Zero,
            new AffixRoll[0],
            requiredLevel: 1,
            consumable: new RestorePayload(RestoreKind.Health, 0.35f));

        [Test]
        public void Every_change_to_what_the_sack_holds_moves_its_revision()
        {
            var inventory = new Inventory();
            int last = inventory.Sack.Revision;

            void Moved(string what)
            {
                Assert.That(inventory.Sack.Revision, Is.GreaterThan(last), $"{what} changed the sack and its revision stayed put.");
                last = inventory.Sack.Revision;
            }

            inventory.Add(Knife(), 99);
            Moved("Adding a knife");
            inventory.Add(Knife(), 99);
            Moved("Adding a second knife");
            inventory.Add(Draught(), 1);
            Moved("Adding a draught");
            inventory.Add(Draught(), 1);
            Moved("Stacking a second draught");
            inventory.Add(Knife(), 99);
            Moved("Adding a third knife");

            // The first two knives combine; the third — with its upgrade capacity known — takes the rest.
            inventory.TryCombine(new DeterministicRandom(7u), 0, 1, Context(), out CombineResult combined);
            Assert.That(combined.Combined, Is.True, "The two knives would not combine, so the combine line proves nothing.");
            Moved("Combining two knives");

            // [draughts, third knife, reroll]: the reroll lands last, so the first piece of gear is the third knife.
            int knife = -1;
            for (int i = 0; i < inventory.Items.Count && knife < 0; i++)
            {
                if (!inventory.Items[i].Item.IsConsumable)
                {
                    knife = i;
                }
            }

            inventory.SetLock(knife, true);
            Moved("Locking");
            inventory.SetLock(knife, false);
            Moved("Releasing");
            Assert.That(inventory.TryUpgradeBagged(knife, UpgradeTarget.Core(CoreStatId.WeaponDamage)), Is.True);
            Moved("Deepening");

            Assert.That(inventory.TryEquip(knife, 99), Is.True);
            Moved("Equipping");
            Assert.That(inventory.Unequip(ItemSlot.Weapon), Is.True);
            Moved("Taking it off");

            Assert.That(inventory.AssignQuickConsumable(DraughtId), Is.True);
            inventory.UseQuickSlot(0);
            Moved("Drinking a draught");

            Assert.That(inventory.Sell(inventory.Items.Count - 1), Is.GreaterThan(0));
            Moved("Selling");
        }

        [Test]
        public void Reading_the_sack_never_moves_it()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.Add(Knife(), 99);
            int before = inventory.Sack.Revision;

            _ = inventory.Items.Count;
            _ = inventory.SlotsUsed;
            _ = inventory.IsFull;
            _ = inventory.PreviewJunk(QualityRank.Legendary);
            _ = inventory.HasCombinePartner(0);
            inventory.CombineChoices(0, new System.Collections.Generic.List<int>());
            _ = inventory.FindAutoSellTarget();

            Assert.That(inventory.Sack.Revision, Is.EqualTo(before), "Looking at the sack moved its revision.");
        }

        [Test]
        public void A_refused_change_leaves_it_alone()
        {
            var inventory = new Inventory();
            inventory.Add(Draught(), 1);
            int before = inventory.Sack.Revision;

            Assert.That(inventory.Sell(5), Is.Zero);
            Assert.That(inventory.SetLock(-1, true), Is.False);
            Assert.That(inventory.TryEquip(0, 99), Is.False, "A draught cannot be worn.");

            Assert.That(inventory.Sack.Revision, Is.EqualTo(before), "A change that did not happen moved the revision.");
        }

        [Test]
        public void A_restore_from_a_save_moves_it()
        {
            var sack = new Sack();
            int before = sack.Revision;

            SaveMapper.RestoreSack(SaveGame.Fresh(), sack, Catalog);

            Assert.That(sack.Revision, Is.GreaterThan(before), "A load replaced the sack's contents and the revision stayed put.");
        }
    }
}
