using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The first equipment active and the mana potion (task 58). D37 said equipment actives were
    /// machinery-only until M5 authored one; this is that one, and D19's guards are what these
    /// tests are really pinning — derived damage and a real cooldown.
    /// </summary>
    public sealed class EquipmentActiveTests
    {
        private static readonly ElementId Fire = new ElementId(1);

        private static ItemInstance Stone(float share = 0.75f, int cooldown = 900) => new ItemInstance(
            definitionId: 13, displayName: "Ember Stone", slot: ItemSlot.Equipment,
            weaponClass: WeaponClass.None, petClass: PetClass.None, quality: QualityRank.Shiny,
            coreStats: new GearContribution(maxManaBonus: 10f), affixes: new AffixRoll[0],
            requiredLevel: 1, upgradeCapacity: 0, upgradesSpent: 0, shotSpeed: 0f,
            consumableHealFraction: 0f, restores: RestoreKind.Health,
            activeWeaponDamageShare: share, activeElement: Fire, activeRadius: 2.8f,
            activeCooldownSteps: cooldown);

        private static ItemInstance Potion(RestoreKind restores) => new ItemInstance(
            definitionId: restores == RestoreKind.Mana ? 12 : 9,
            displayName: "Vial", slot: ItemSlot.Consumable,
            weaponClass: WeaponClass.None, petClass: PetClass.None, quality: QualityRank.Rusty,
            coreStats: default, affixes: new AffixRoll[0], requiredLevel: 1, upgradeCapacity: 0,
            upgradesSpent: 0, shotSpeed: 0f, consumableHealFraction: 0.35f, restores: restores);

        private static Inventory Wearing(in ItemInstance equipment)
        {
            var inventory = new Inventory();
            inventory.Add(equipment, currentLevel: 99);
            inventory.TryEquip(0, currentLevel: 99);
            inventory.AssignQuickEquipment(0);
            return inventory;
        }

        [Test]
        public void A_worn_active_fires_from_the_quick_slot()
        {
            Inventory inventory = Wearing(Stone());

            QuickUseResult fired = inventory.UseQuickSlot(180);

            Assert.That(fired.Used, Is.True);
            Assert.That(fired.FiredActive, Is.True);
            Assert.That(fired.ActiveWeaponDamageShare, Is.EqualTo(0.75f));
            Assert.That(fired.ActiveElement, Is.EqualTo(Fire));
            Assert.That(fired.RestoreFraction, Is.EqualTo(0f), "An active is not a drink.");
        }

        [Test]
        public void An_active_uses_its_own_cooldown_not_the_potion_one()
        {
            Inventory inventory = Wearing(Stone(cooldown: 900));

            inventory.UseQuickSlot(180);

            Assert.That(inventory.QuickCooldownRemaining, Is.EqualTo(900),
                "The item's cooldown is the item's, so an active stays a moment (D19/D37).");
            Assert.That(inventory.UseQuickSlot(180).Used, Is.False, "And it cannot be spammed.");
        }

        [Test]
        public void The_cooldown_runs_down_and_the_active_returns()
        {
            Inventory inventory = Wearing(Stone(cooldown: 3));

            inventory.UseQuickSlot(180);
            for (int i = 0; i < 3; i++)
            {
                inventory.Step();
            }

            Assert.That(inventory.UseQuickSlot(180).Used, Is.True);
        }

        [Test]
        public void Unequipping_the_piece_empties_the_slot_rather_than_firing_a_ghost()
        {
            Inventory inventory = Wearing(Stone());

            inventory.Unequip(ItemSlot.Equipment, 0);

            Assert.That(inventory.QuickKind, Is.EqualTo(QuickSlotKind.Empty));
            Assert.That(inventory.UseQuickSlot(180).Used, Is.False);
        }

        [Test]
        public void A_passive_equipment_piece_still_fires_nothing()
        {
            Inventory inventory = Wearing(Stone(share: 0f));

            Assert.That(inventory.UseQuickSlot(180).Used, Is.False,
                "Most equipment is passive (D27); only the rare piece has an active.");
        }

        [Test]
        public void The_ladder_scales_how_big_the_moment_is()
        {
            var spec = new ItemSpec(
                13, "Ember Stone", ItemSlot.Equipment, new GearContribution(maxManaBonus: 10f),
                activeWeaponDamageShare: 0.75f, activeElement: Fire, activeRadius: 2.8f,
                activeCooldownSteps: 900);
            var catalog = new[] { spec };
            var elements = new[] { Fire };

            ItemGenerator.Roll(
                new DeterministicRandom(4u),
                new GenerationContext(0.1f, 1, catalog, QualityTable.Default, DropWeights.Default, elements),
                out ItemInstance weak);
            ItemGenerator.Roll(
                new DeterministicRandom(4u),
                new GenerationContext(99f, 1, catalog, QualityTable.Default, DropWeights.Default, elements),
                out ItemInstance godly);

            Assert.That(godly.ActiveWeaponDamageShare, Is.GreaterThan(weak.ActiveWeaponDamageShare));
            Assert.That(godly.ActiveElement, Is.EqualTo(Fire), "The element is authored, never rolled.");
            Assert.That(godly.ActiveCooldownSteps, Is.EqualTo(900),
                "Quality makes an active bigger, never more frequent.");
        }

        [Test]
        public void A_mana_potion_names_the_pool_it_refills()
        {
            var inventory = new Inventory();
            inventory.Add(Potion(RestoreKind.Mana), currentLevel: 1);
            inventory.AssignQuickConsumable(12);

            QuickUseResult drunk = inventory.UseQuickSlot(180);

            Assert.That(drunk.Used, Is.True);
            Assert.That(drunk.Restores, Is.EqualTo(RestoreKind.Mana));
            Assert.That(drunk.RestoreFraction, Is.EqualTo(0.35f));
        }

        [Test]
        public void Health_and_mana_potions_stack_separately()
        {
            var inventory = new Inventory();
            inventory.Add(Potion(RestoreKind.Health), currentLevel: 1);
            inventory.Add(Potion(RestoreKind.Mana), currentLevel: 1);

            Assert.That(inventory.Items.Count, Is.EqualTo(2),
                "Different definitions never merge, whatever they look like.");
        }

        [Test]
        public void Mana_restores_without_overfilling()
        {
            ManaPool pool = ManaPool.Full(100f).Spent(60f);

            Assert.That(pool.Restored(35f).Current, Is.EqualTo(75f));
            Assert.That(pool.Restored(500f).Current, Is.EqualTo(100f));
        }
    }
}
