using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// Live state → file → live state, with nothing lost. The rolled values on every item are
    /// the chase; a save that drops an affix is a save that steals.
    /// </summary>
    public sealed class SaveMapperTests
    {
        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), new GearContribution(weaponDamage: 5f)),
            new ItemSpec(new ItemIdentity(12, "Cap", ItemSlot.Helmet), new GearContribution(defence: 2f)),
            new ItemSpec(new ItemIdentity(30, "Health", ItemSlot.Consumable), GearContribution.Zero,
                consumable: new RestorePayload(RestoreKind.Health, 0.25f)),
        };

        private static ItemInstance Knife() => new ItemInstance(
            new ItemIdentity(7, "Shiny Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
            new GearContribution(weaponDamage: 9.5f, critChance: 0.04f),
            new[] { new AffixRoll(AffixId.WeaponInfusion, 0.6f, new ElementId(2)) },
            requiredLevel: 12, new ItemInvestment(capacity: 3, spent: 1, locked: true), shotSpeed: 14f);

        private static ItemInstance Cap() => new ItemInstance(
            new ItemIdentity(12, "Rusty Cap", ItemSlot.Helmet), QualityRank.Rusty,
            new GearContribution(defence: 3f), new AffixRoll[0], requiredLevel: 3,
            new ItemInvestment(capacity: 1));

        private static ItemInstance Potion() => new ItemInstance(
            new ItemIdentity(30, "Vial of Health", ItemSlot.Consumable), QualityRank.Battlescarred,
            GearContribution.Zero, new AffixRoll[0], requiredLevel: 1,
            consumable: new RestorePayload(RestoreKind.Health, 0.3f));

        private static CharacterState Fire(Sack sack)
        {
            var inventory = new Inventory(sack);
            inventory.Add(Knife(), 20);
            inventory.TryEquip(0, 20);
            var ledger = new XpLedger(level: 14, xpIntoLevel: 33.5f, unspentPoints: 2,
                new BaseStats(strength: 5, hp: 3, mana: 2, speed: 1), prestigeCount: 1);
            return new CharacterState(new ElementId(1), ledger, inventory);
        }

        [Test]
        public void Everything_in_the_sack_survives_the_round_trip()
        {
            var sack = new Sack { AutoSell = true };
            var inventory = new Inventory(sack);
            inventory.Add(Cap(), 20);
            inventory.Add(Potion(), 20);
            inventory.Add(Potion(), 20);
            var progress = new StoryProgress();

            SaveGame save = SaveMapper.Capture(sack, new Wallet(150), new[] { new CharacterState(new ElementId(1), XpLedger.Fresh, inventory) }, progress);
            SaveLoad load = SaveCodec.Decode(SaveCodec.Encode(save));
            var restoredSack = new Sack();
            SaveMapper.RestoreSack(load.Save, restoredSack, Catalog);

            Assert.That(restoredSack.Items.Count, Is.EqualTo(2), "A cap and one stack of two potions.");
            Assert.That(restoredSack.Items[1].Count, Is.EqualTo(2));
            Assert.That(restoredSack.Items[0].Item.CoreStats.Defence, Is.EqualTo(3f));
            Assert.That(restoredSack.Items[0].Item.UpgradeCapacity, Is.EqualTo(1));
            Assert.That(restoredSack.Items[1].Item.ConsumableHealFraction, Is.EqualTo(0.3f));
            Assert.That(restoredSack.AutoSell, Is.True);
            Assert.That(SaveMapper.RestoreWallet(load.Save).Balance, Is.EqualTo(150));
        }

        [Test]
        public void A_character_keeps_its_ledger_its_worn_gear_and_every_rolled_value()
        {
            var sack = new Sack();
            CharacterState fire = Fire(sack);
            SaveGame save = SaveMapper.Capture(sack, Wallet.Empty, new[] { fire }, new StoryProgress());

            SaveLoad load = SaveCodec.Decode(SaveCodec.Encode(save));
            var restoredSack = new Sack();
            var restored = new Inventory(restoredSack);
            XpLedger ledger = SaveMapper.RestoreCharacter(load.Save.Characters[0], restored, Catalog);

            Assert.That(ledger.Level, Is.EqualTo(14));
            Assert.That(ledger.XpIntoLevel, Is.EqualTo(33.5f));
            Assert.That(ledger.UnspentPoints, Is.EqualTo(2));
            Assert.That(ledger.Allocations.Strength, Is.EqualTo(5));
            Assert.That(ledger.PrestigeCount, Is.EqualTo(1));

            ItemInstance weapon = restored.Loadout.Weapon;
            Assert.That(weapon.IsEmpty, Is.False, "The knife is still worn.");
            Assert.That(weapon.Quality, Is.EqualTo(QualityRank.Shiny));
            Assert.That(weapon.CoreStats.WeaponDamage, Is.EqualTo(9.5f));
            Assert.That(weapon.CoreStats.CritChance, Is.EqualTo(0.04f));
            Assert.That(weapon.AffixCount, Is.EqualTo(1));
            Assert.That(weapon.Affixes[0].Id, Is.EqualTo(AffixId.WeaponInfusion));
            Assert.That(weapon.Affixes[0].Magnitude, Is.EqualTo(0.6f));
            Assert.That(weapon.Affixes[0].Element.Value, Is.EqualTo(2));
            Assert.That(weapon.RequiredLevel, Is.EqualTo(12));
            Assert.That(weapon.UpgradeCapacity, Is.EqualTo(3));
            Assert.That(weapon.UpgradesSpent, Is.EqualTo(1));
            Assert.That(weapon.Locked, Is.True);
            Assert.That(weapon.ShotSpeed, Is.EqualTo(14f));
            Assert.That(load.Save.Characters[0].ElementId, Is.EqualTo(1));
        }

        [Test]
        public void Items_are_saved_by_id_so_a_renamed_definition_renames_the_saved_item()
        {
            var sack = new Sack();
            new Inventory(sack).Add(Cap(), 20);
            SaveGame save = SaveMapper.Capture(sack, Wallet.Empty, new CharacterState[0], new StoryProgress());
            var renamed = new[]
            {
                new ItemSpec(new ItemIdentity(12, "Helm of Renaming", ItemSlot.Helmet), new GearContribution(defence: 2f)),
            };

            var restoredSack = new Sack();
            SaveMapper.RestoreSack(SaveCodec.Decode(SaveCodec.Encode(save)).Save, restoredSack, renamed);

            Assert.That(restoredSack.Items[0].Item.DisplayName, Is.EqualTo("Rusty Helm of Renaming"));
            Assert.That(restoredSack.Items[0].Item.DefinitionId, Is.EqualTo(12));
        }

        [Test]
        public void An_item_whose_definition_vanished_keeps_its_saved_name_rather_than_vanishing()
        {
            var sack = new Sack();
            new Inventory(sack).Add(Cap(), 20);
            SaveGame save = SaveMapper.Capture(sack, Wallet.Empty, new CharacterState[0], new StoryProgress());

            var restoredSack = new Sack();
            SaveMapper.RestoreSack(SaveCodec.Decode(SaveCodec.Encode(save)).Save, restoredSack, new ItemSpec[0]);

            Assert.That(restoredSack.Items.Count, Is.EqualTo(1), "Deleting a definition must never delete a player's item.");
            Assert.That(restoredSack.Items[0].Item.DisplayName, Is.EqualTo("Rusty Cap"));
        }

        [Test]
        public void The_quick_slot_comes_back_pointing_where_it_pointed()
        {
            var sack = new Sack();
            var inventory = new Inventory(sack);
            inventory.Add(Potion(), 20);
            inventory.AssignQuickConsumable(30);
            SaveGame save = SaveMapper.Capture(sack, Wallet.Empty, new[] { new CharacterState(new ElementId(1), XpLedger.Fresh, inventory) }, new StoryProgress());

            SaveLoad load = SaveCodec.Decode(SaveCodec.Encode(save));
            var restoredSack = new Sack();
            SaveMapper.RestoreSack(load.Save, restoredSack, Catalog);
            var restored = new Inventory(restoredSack);
            SaveMapper.RestoreCharacter(load.Save.Characters[0], restored, Catalog);

            Assert.That(restored.QuickKind, Is.EqualTo(QuickSlotKind.Consumable));
            Assert.That(restored.QuickConsumableId, Is.EqualTo(30));
        }

        [Test]
        public void Story_progress_and_the_resume_point_survive()
        {
            var progress = new StoryProgress();
            progress.RecordCompletion("c1", 1);
            progress.SetResume("c2", 1, 0);
            SaveGame save = SaveMapper.Capture(new Sack(), Wallet.Empty, new CharacterState[0], progress);

            StoryProgress restored = SaveMapper.RestoreProgress(SaveCodec.Decode(SaveCodec.Encode(save)).Save);

            Assert.That(restored.HighestTierBeaten("c1"), Is.EqualTo(2));
            Assert.That(restored.HighestTierBeaten("c9"), Is.Zero);
            Assert.That(restored.ResumeChapterId, Is.EqualTo("c2"));
            Assert.That(restored.ResumeStageIndex, Is.EqualTo(1));
            Assert.That(restored.ResumeCheckpointArena, Is.EqualTo(0));
        }

        [Test]
        public void Every_gear_stat_survives_ToSave_and_ToInstance_by_name_not_position()
        {
            // D52 review: ToSave and ToInstance used to line fourteen stats up by array index,
            // with nothing enforcing the two orderings agree. Reordering either list, or
            // GearContribution's own constructor, would have passed the rest of the suite while
            // silently transposing stats on every saved item. Fourteen distinct values, each
            // asserted by name, is what would have caught it.
            var original = new ItemInstance(
                new ItemIdentity(99, "Fourteen Stats", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Godly,
                new GearContribution(
                    weaponDamage: 1f, swingSpeedBonus: 2f, defence: 3f, weight: 4f,
                    critChance: 5f, critDamageBonus: 6f, lifeSteal: 7f, maxHealthBonus: 8f,
                    maxManaBonus: 9f, manaRegen: 10f, weightReduction: 11f, knockbackBonus: 12f,
                    magicDamage: 13f, magicRange: 14f),
                new AffixRoll[0], requiredLevel: 1);

            ItemInstance restored = SaveMapper.ToInstance(SaveMapper.ToSave(original), null);

            Assert.That(restored.CoreStats.WeaponDamage, Is.EqualTo(1f));
            Assert.That(restored.CoreStats.SwingSpeedBonus, Is.EqualTo(2f));
            Assert.That(restored.CoreStats.Defence, Is.EqualTo(3f));
            Assert.That(restored.CoreStats.Weight, Is.EqualTo(4f));
            Assert.That(restored.CoreStats.CritChance, Is.EqualTo(5f));
            Assert.That(restored.CoreStats.CritDamageBonus, Is.EqualTo(6f));
            Assert.That(restored.CoreStats.LifeSteal, Is.EqualTo(7f));
            Assert.That(restored.CoreStats.MaxHealthBonus, Is.EqualTo(8f));
            Assert.That(restored.CoreStats.MaxManaBonus, Is.EqualTo(9f));
            Assert.That(restored.CoreStats.ManaRegen, Is.EqualTo(10f));
            Assert.That(restored.CoreStats.WeightReduction, Is.EqualTo(11f));
            Assert.That(restored.CoreStats.KnockbackBonus, Is.EqualTo(12f));
            Assert.That(restored.CoreStats.MagicDamage, Is.EqualTo(13f));
            Assert.That(restored.CoreStats.MagicRange, Is.EqualTo(14f));
        }

        [Test]
        public void A_character_absent_this_session_is_carried_through_the_save_untouched()
        {
            var sack = new Sack();
            CharacterState fire = Fire(sack);
            SaveGame first = SaveMapper.Capture(sack, Wallet.Empty, new[] { fire }, new StoryProgress());

            // Next session: only Ice plays. Fire's save must ride along.
            var ice = new CharacterState(new ElementId(2), XpLedger.Fresh, new Inventory(sack));
            SaveGame second = SaveMapper.Capture(
                sack, Wallet.Empty, new[] { ice }, new StoryProgress(), carried: first.Characters);

            Assert.That(second.Characters.Length, Is.EqualTo(2));
            Assert.That(second.Characters[0].ElementId, Is.EqualTo(2), "Present characters first.");
            Assert.That(second.Characters[1].ElementId, Is.EqualTo(1));
            Assert.That(second.Characters[1].Level, Is.EqualTo(14), "Fire is exactly as it was left.");

            // Fire plays again: the live state wins over the carried copy.
            SaveGame third = SaveMapper.Capture(
                sack, Wallet.Empty, new[] { fire, ice }, new StoryProgress(), carried: second.Characters);
            Assert.That(third.Characters.Length, Is.EqualTo(2), "No duplicate for a character who is present.");
        }
    }
}
