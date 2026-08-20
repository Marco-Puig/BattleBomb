using System;
using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The whole task-43 inventory surface, pinned: stacking, the equip flow with D36's level
    /// lock, the prestige force-return, D30's auto-equip semantics, and the quick-use slot with
    /// its cooldown (D37). This is the piece 2019 never finished — its scope is these tests.
    /// </summary>
    public sealed class InventoryTests
    {
        private const int PotionId = 100;
        private const int QuickCooldown = 180;

        private static ItemInstance Gear(
            int id, ItemSlot slot, QualityRank quality = QualityRank.Rusty, int requiredLevel = 1)
        {
            return new ItemInstance(
                id, ItemNaming.Compose(quality, "Test Piece"), slot, WeaponClass.None, PetClass.None,
                quality, new GearContribution(defence: 0.05f), Array.Empty<AffixRoll>(),
                requiredLevel, upgradeCapacity: 2);
        }

        private static ItemInstance Potion() => new ItemInstance(
            PotionId, "Health Potion", ItemSlot.Consumable, WeaponClass.None, PetClass.None,
            QualityRank.Rusty, GearContribution.Zero, Array.Empty<AffixRoll>(),
            requiredLevel: 1, upgradeCapacity: 0, upgradesSpent: 0,
            shotSpeed: 0f, consumableHealFraction: 0.35f);

        [Test]
        public void Consumables_stack_and_gear_lists_separately()
        {
            var inventory = new Inventory();
            inventory.Add(Potion(), 1);
            inventory.Add(Potion(), 1);
            inventory.Add(Gear(1, ItemSlot.Helmet), 1);
            inventory.Add(Gear(2, ItemSlot.Helmet), 1);

            Assert.That(inventory.Items.Count, Is.EqualTo(3), "one potion stack, two helmets");
            Assert.That(inventory.Items[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void Equipping_moves_bag_to_loadout_and_displaces_back()
        {
            var inventory = new Inventory();
            inventory.Add(Gear(1, ItemSlot.Helmet), 1);

            Assert.That(inventory.TryEquip(0, currentLevel: 1), Is.True);
            Assert.That(inventory.Items, Is.Empty);
            Assert.That(inventory.Loadout.Helmet.DefinitionId, Is.EqualTo(1));

            inventory.Add(Gear(2, ItemSlot.Helmet, QualityRank.Shiny), 1);
            Assert.That(inventory.TryEquip(0, 1), Is.True);
            Assert.That(inventory.Loadout.Helmet.DefinitionId, Is.EqualTo(2));
            Assert.That(inventory.Items[0].Item.DefinitionId, Is.EqualTo(1), "the old helmet came back");
        }

        [Test]
        public void The_level_lock_blocks_and_unblocks()
        {
            var inventory = new Inventory();
            inventory.Add(Gear(1, ItemSlot.Weapon, QualityRank.Godly, requiredLevel: 40), 1);

            Assert.That(inventory.TryEquip(0, currentLevel: 10), Is.False, "the D36 lock holds");
            Assert.That(inventory.Loadout.Weapon.IsEmpty, Is.True);
            Assert.That(inventory.TryEquip(0, currentLevel: 40), Is.True);
        }

        [Test]
        public void Consumables_never_equip()
        {
            var inventory = new Inventory();
            inventory.Add(Potion(), 1);

            Assert.That(inventory.TryEquip(0, 99), Is.False);
        }

        [Test]
        public void Equipment_occupies_two_addressable_slots()
        {
            var inventory = new Inventory();
            inventory.Add(Gear(1, ItemSlot.Equipment), 1);
            inventory.Add(Gear(2, ItemSlot.Equipment), 1);
            inventory.Add(Gear(3, ItemSlot.Equipment), 1);

            Assert.That(inventory.TryEquip(0, 1, equipmentIndex: 0), Is.True);
            Assert.That(inventory.TryEquip(0, 1, equipmentIndex: 1), Is.True);
            Assert.That(inventory.Loadout.Equipment(0).DefinitionId, Is.EqualTo(1));
            Assert.That(inventory.Loadout.Equipment(1).DefinitionId, Is.EqualTo(2));

            Assert.That(inventory.TryEquip(0, 1, equipmentIndex: 0), Is.True);
            Assert.That(inventory.Loadout.Equipment(0).DefinitionId, Is.EqualTo(3));
            Assert.That(inventory.Items[0].Item.DefinitionId, Is.EqualTo(1));
        }

        [Test]
        public void Auto_equip_only_upgrades_by_rank_and_respects_the_lock()
        {
            var inventory = new Inventory { AutoEquip = true };

            Assert.That(inventory.Add(Gear(1, ItemSlot.Helmet, QualityRank.Rusty), 10), Is.True,
                "an empty slot fills");
            Assert.That(inventory.Add(Gear(2, ItemSlot.Helmet, QualityRank.Torn), 10), Is.False,
                "a downgrade stays in the bag");
            Assert.That(inventory.Loadout.Helmet.DefinitionId, Is.EqualTo(1));

            Assert.That(inventory.Add(Gear(3, ItemSlot.Helmet, QualityRank.Shiny), 10), Is.True,
                "a strictly better rank replaces");
            Assert.That(inventory.Loadout.Helmet.DefinitionId, Is.EqualTo(3));

            Assert.That(inventory.Add(Gear(4, ItemSlot.Helmet, QualityRank.Godly, requiredLevel: 40), 10),
                Is.False, "over-level gear never auto-equips");
            Assert.That(inventory.Loadout.Helmet.DefinitionId, Is.EqualTo(3));
        }

        [Test]
        public void Off_by_default_everything_lands_in_the_bag()
        {
            var inventory = new Inventory();

            Assert.That(inventory.Add(Gear(1, ItemSlot.Helmet, QualityRank.Godly), 99), Is.False);
            Assert.That(inventory.Loadout.Helmet.IsEmpty, Is.True, "D30: inventory by default");
        }

        [Test]
        public void Prestige_returns_every_over_level_piece()
        {
            var inventory = new Inventory();
            inventory.Add(Gear(1, ItemSlot.Helmet, QualityRank.Godly, requiredLevel: 40), 1);
            inventory.Add(Gear(2, ItemSlot.Weapon, QualityRank.Godly, requiredLevel: 35), 1);
            inventory.Add(Gear(3, ItemSlot.Boots, QualityRank.Rusty, requiredLevel: 1), 1);
            inventory.TryEquip(0, 40);
            inventory.TryEquip(0, 40);
            inventory.TryEquip(0, 40);
            Assert.That(inventory.Items, Is.Empty, "everything worn before the reset");

            int returned = inventory.ReturnOverLevelGear(currentLevel: 1);

            Assert.That(returned, Is.EqualTo(2));
            Assert.That(inventory.Loadout.Helmet.IsEmpty, Is.True);
            Assert.That(inventory.Loadout.Weapon.IsEmpty, Is.True);
            Assert.That(inventory.Loadout.Boots.IsEmpty, Is.False, "level-one gear survives");
            Assert.That(inventory.Items.Count, Is.EqualTo(2));
        }

        [Test]
        public void The_quick_slot_drinks_cools_down_and_exhausts()
        {
            var inventory = new Inventory();
            inventory.Add(Potion(), 1);
            inventory.Add(Potion(), 1);

            Assert.That(inventory.AssignQuickConsumable(PotionId), Is.True);

            QuickUseResult first = inventory.UseQuickSlot(QuickCooldown);
            Assert.That(first.Used, Is.True);
            Assert.That(first.HealFraction, Is.EqualTo(0.35f));
            Assert.That(inventory.Items[0].Count, Is.EqualTo(1));

            Assert.That(inventory.UseQuickSlot(QuickCooldown).Used, Is.False, "the cooldown gates");

            for (int i = 0; i < QuickCooldown; i++)
            {
                inventory.Step();
            }

            QuickUseResult second = inventory.UseQuickSlot(QuickCooldown);
            Assert.That(second.Used, Is.True);
            Assert.That(inventory.Items, Is.Empty, "the stack is gone");
            Assert.That(inventory.QuickKind, Is.EqualTo(QuickSlotKind.Empty), "exhaustion clears the slot");
        }

        [Test]
        public void Assigning_a_missing_stack_fails_and_equipment_actives_stay_machinery()
        {
            var inventory = new Inventory();
            Assert.That(inventory.AssignQuickConsumable(PotionId), Is.False);
            Assert.That(inventory.AssignQuickEquipment(0), Is.False, "nothing worn there");

            inventory.Add(Gear(1, ItemSlot.Equipment), 1);
            inventory.TryEquip(0, 1, equipmentIndex: 0);
            Assert.That(inventory.AssignQuickEquipment(0), Is.True);
            Assert.That(inventory.QuickKind, Is.EqualTo(QuickSlotKind.EquipmentActive));
            Assert.That(inventory.UseQuickSlot(QuickCooldown).Used, Is.False,
                "no authored actives until M5 — the seam stays a seam");

            inventory.Unequip(ItemSlot.Equipment, 0);
            Assert.That(inventory.QuickKind, Is.EqualTo(QuickSlotKind.Empty),
                "unequipping the piece clears the slot pointing at it");
        }

        [Test]
        public void The_loadout_collects_every_worn_contribution()
        {
            var inventory = new Inventory();
            inventory.Add(Gear(1, ItemSlot.Helmet), 1);
            inventory.Add(Gear(2, ItemSlot.Chest), 1);
            inventory.TryEquip(0, 1);
            inventory.TryEquip(0, 1);

            var contributions = new List<GearContribution>();
            inventory.Loadout.CollectContributions(contributions);

            Assert.That(contributions.Count, Is.EqualTo(2));
            Assert.That(contributions[0].Defence, Is.EqualTo(0.05f));
        }
    }
}
