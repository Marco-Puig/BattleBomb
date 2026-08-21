using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Saves
{
    /// <summary>One roster character's live state, for the mapper to capture.</summary>
    public readonly struct CharacterState
    {
        public readonly ElementId Element;
        public readonly XpLedger Ledger;
        public readonly Inventory Inventory;

        public CharacterState(ElementId element, in XpLedger ledger, Inventory inventory)
        {
            Element = element;
            Ledger = ledger;
            Inventory = inventory;
        }
    }

    /// <summary>
    /// Live state ↔ <see cref="SaveGame"/>, both directions (D52). Items go out as their id
    /// plus every rolled value and come back re-stamped with the catalog's current name, so
    /// renaming a definition renames the saved item and deleting one never deletes it.
    /// </summary>
    public static class SaveMapper
    {
        private static readonly ItemSlot[] WornSlots =
        {
            ItemSlot.Helmet, ItemSlot.Chest, ItemSlot.Boots, ItemSlot.Weapon, ItemSlot.Pet,
        };

        // ── Capture ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// The whole machine's state as one file. <paramref name="carried"/> is the previous
        /// save's roster: D51 gives the couch one shared save, so a character who did not play
        /// this session has no live state to capture and would otherwise be written out of
        /// existence by whoever did play. Present characters are captured first and win; the
        /// rest ride through byte-identical.
        /// </summary>
        public static SaveGame Capture(
            Sack sack, in Wallet wallet, IReadOnlyList<CharacterState> characters, StoryProgress progress,
            IReadOnlyList<CharacterSave> carried = null)
        {
            var stacks = new List<ItemStackSave>(sack.Items.Count);
            for (int i = 0; i < sack.Items.Count; i++)
            {
                stacks.Add(new ItemStackSave(ToSave(sack.Items[i].Item), sack.Items[i].Count));
            }

            var roster = new List<CharacterSave>(characters.Count);
            for (int i = 0; i < characters.Count; i++)
            {
                roster.Add(CaptureCharacter(characters[i]));
            }

            if (carried != null)
            {
                // Whoever stayed home this session keeps their place (D51's shared roster).
                for (int i = 0; i < carried.Count; i++)
                {
                    if (carried[i] != null && !HasElement(roster, carried[i].ElementId))
                    {
                        roster.Add(carried[i]);
                    }
                }
            }

            return new SaveGame(
                SaveCodec.CurrentVersion, wallet.Balance, sack.AutoEquip, sack.AutoSell,
                stacks.ToArray(), roster.ToArray(), CaptureProgress(progress));
        }

        private static bool HasElement(List<CharacterSave> roster, int elementId)
        {
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i].ElementId == elementId)
                {
                    return true;
                }
            }

            return false;
        }

        private static CharacterSave CaptureCharacter(in CharacterState state)
        {
            var worn = new List<WornSave>();
            Loadout loadout = state.Inventory.Loadout;
            for (int i = 0; i < WornSlots.Length; i++)
            {
                ItemInstance item = loadout.Worn(WornSlots[i]);
                if (!item.IsEmpty)
                {
                    worn.Add(new WornSave((int)WornSlots[i], 0, ToSave(item)));
                }
            }

            for (int i = 0; i < Loadout.EquipmentSlots; i++)
            {
                ItemInstance item = loadout.Equipment(i);
                if (!item.IsEmpty)
                {
                    worn.Add(new WornSave((int)ItemSlot.Equipment, i, ToSave(item)));
                }
            }

            XpLedger ledger = state.Ledger;
            return new CharacterSave(
                state.Element.Value, ledger.Level, ledger.XpIntoLevel, ledger.UnspentPoints,
                ledger.Allocations.Strength, ledger.Allocations.Hp, ledger.Allocations.Mana, ledger.Allocations.Speed,
                ledger.PrestigeCount, worn.ToArray(),
                (int)state.Inventory.QuickKind, state.Inventory.QuickConsumableId, state.Inventory.QuickEquipmentIndex);
        }

        private static StoryProgressSave CaptureProgress(StoryProgress progress)
        {
            var chapters = new List<ChapterProgressSave>();
            foreach (KeyValuePair<string, int> entry in progress.TiersBeaten)
            {
                chapters.Add(new ChapterProgressSave(entry.Key, entry.Value));
            }

            return new StoryProgressSave(
                chapters.ToArray(), progress.ResumeChapterId, progress.ResumeStageIndex, progress.ResumeCheckpointArena);
        }

        public static ItemSave ToSave(in ItemInstance item)
        {
            GearContribution c = item.CoreStats;
            var core = new GearContributionSave(
                weaponDamage: c.WeaponDamage, swingSpeedBonus: c.SwingSpeedBonus, defence: c.Defence,
                weight: c.Weight, critChance: c.CritChance, critDamageBonus: c.CritDamageBonus,
                lifeSteal: c.LifeSteal, maxHealthBonus: c.MaxHealthBonus, maxManaBonus: c.MaxManaBonus,
                manaRegen: c.ManaRegen, weightReduction: c.WeightReduction, knockbackBonus: c.KnockbackBonus,
                magicDamage: c.MagicDamage, magicRange: c.MagicRange);

            var affixes = new AffixSave[item.AffixCount];
            for (int i = 0; i < affixes.Length; i++)
            {
                AffixRoll roll = item.Affixes[i];
                affixes[i] = new AffixSave((int)roll.Id, roll.Magnitude, roll.Element.Value);
            }

            return new ItemSave(
                item.DefinitionId, item.DisplayName, (int)item.Slot, (int)item.WeaponClass, (int)item.PetClass,
                (int)item.Quality, core, affixes, item.RequiredLevel,
                item.UpgradeCapacity, item.UpgradesSpent, item.Locked, item.ShotSpeed,
                (int)item.Consumable.Kind, item.Consumable.Fraction,
                item.Active.WeaponDamageShare, item.Active.Element.Value, item.Active.Radius, item.Active.CooldownSteps);
        }

        // ── Restore ──────────────────────────────────────────────────────────────────

        public static void RestoreSack(SaveGame save, Sack sack, IReadOnlyList<ItemSpec> catalog)
        {
            sack.Entries.Clear();
            sack.AutoEquip = save.AutoEquip;
            sack.AutoSell = save.AutoSell;
            ItemStackSave[] stacks = save.Sack;
            for (int i = 0; i < stacks.Length; i++)
            {
                if (stacks[i] == null || stacks[i].Item == null || stacks[i].Item.IsEmpty)
                {
                    continue;
                }

                sack.Entries.Add(new ItemStack(ToInstance(stacks[i].Item, catalog), stacks[i].Count));
            }
        }

        public static Wallet RestoreWallet(SaveGame save) => new Wallet(save.Coins);

        /// <summary>Puts a character's worn gear and quick slot onto an inventory and returns its
        /// ledger. The inventory must already sit over the restored sack.</summary>
        public static XpLedger RestoreCharacter(CharacterSave character, Inventory inventory, IReadOnlyList<ItemSpec> catalog)
        {
            WornSave[] worn = character.Worn;
            for (int i = 0; i < worn.Length; i++)
            {
                if (worn[i] == null || worn[i].Item == null || worn[i].Item.IsEmpty)
                {
                    continue;
                }

                inventory.Loadout.Swap(
                    (ItemSlot)worn[i].Slot, worn[i].EquipmentIndex, ToInstance(worn[i].Item, catalog));
            }

            inventory.RestoreQuickSlot(
                (QuickSlotKind)character.QuickKind, character.QuickConsumableId, character.QuickEquipmentIndex);

            return new XpLedger(
                character.Level, character.XpIntoLevel, character.UnspentPoints,
                new BaseStats(character.Strength, character.Hp, character.Mana, character.Speed),
                character.PrestigeCount);
        }

        public static StoryProgress RestoreProgress(SaveGame save)
        {
            var progress = new StoryProgress();
            StoryProgressSave story = save.Story;
            ChapterProgressSave[] chapters = story.Chapters;
            for (int i = 0; i < chapters.Length; i++)
            {
                if (chapters[i] != null)
                {
                    progress.RestoreTiersBeaten(chapters[i].ChapterId, chapters[i].TiersBeaten);
                }
            }

            if (story.ResumeChapterId.Length > 0)
            {
                progress.SetResume(story.ResumeChapterId, story.ResumeStageIndex, story.ResumeCheckpointArena);
            }

            return progress;
        }

        public static ItemInstance ToInstance(ItemSave save, IReadOnlyList<ItemSpec> catalog)
        {
            var slot = (ItemSlot)save.Slot;
            var quality = (QualityRank)save.Quality;
            string name = save.Name;
            var weaponClass = (WeaponClass)save.WeaponClass;
            var petClass = (PetClass)save.PetClass;

            if (catalog != null)
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    if (catalog[i].Id == save.DefinitionId)
                    {
                        // The catalog's word on what this thing is called today (D52).
                        name = ItemNaming.Compose(quality, slot, catalog[i].Name);
                        weaponClass = catalog[i].WeaponClass;
                        petClass = catalog[i].PetClass;
                        break;
                    }
                }
            }

            GearContributionSave g = save.CoreStats;
            var core = new GearContribution(
                weaponDamage: g.WeaponDamage, swingSpeedBonus: g.SwingSpeedBonus, defence: g.Defence,
                weight: g.Weight, critChance: g.CritChance, critDamageBonus: g.CritDamageBonus,
                lifeSteal: g.LifeSteal, maxHealthBonus: g.MaxHealthBonus, maxManaBonus: g.MaxManaBonus,
                manaRegen: g.ManaRegen, weightReduction: g.WeightReduction, knockbackBonus: g.KnockbackBonus,
                magicDamage: g.MagicDamage, magicRange: g.MagicRange);

            AffixSave[] saved = save.Affixes;
            var affixes = new AffixRoll[saved.Length];
            for (int i = 0; i < saved.Length; i++)
            {
                affixes[i] = new AffixRoll((AffixId)saved[i].Id, saved[i].Magnitude, new ElementId(saved[i].Element));
            }

            return new ItemInstance(
                new ItemIdentity(save.DefinitionId, name, slot, weaponClass, petClass),
                quality, core, affixes, save.RequiredLevel,
                new ItemInvestment(save.Capacity, save.Spent, save.Locked),
                save.ShotSpeed,
                new RestorePayload((RestoreKind)save.RestoreKind, save.RestoreFraction),
                new ActivePayload(save.ActiveShare, new ElementId(save.ActiveElement), save.ActiveRadius, save.ActiveCooldown));
        }
    }
}
