using System.Collections.Generic;
using BattleBomb.Core.Combat;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// What one player wears (D34): the five gear slots plus the two worn equipment pieces.
    /// Empty slots hold the empty item. The loadout only stores and enumerates — the equip
    /// rules, level locks included, live in <see cref="Inventory"/>.
    /// </summary>
    public sealed class Loadout
    {
        public const int EquipmentSlots = 2;

        private ItemInstance _helmet;
        private ItemInstance _chest;
        private ItemInstance _boots;
        private ItemInstance _weapon;
        private ItemInstance _pet;
        private readonly ItemInstance[] _equipment = new ItemInstance[EquipmentSlots];

        public ItemInstance Helmet => _helmet;
        public ItemInstance Chest => _chest;
        public ItemInstance Boots => _boots;
        public ItemInstance Weapon => _weapon;
        public ItemInstance Pet => _pet;

        public ItemInstance Equipment(int index) =>
            index >= 0 && index < EquipmentSlots ? _equipment[index] : default;

        public ItemInstance Worn(ItemSlot slot, int equipmentIndex = 0)
        {
            switch (slot)
            {
                case ItemSlot.Helmet: return _helmet;
                case ItemSlot.Chest: return _chest;
                case ItemSlot.Boots: return _boots;
                case ItemSlot.Weapon: return _weapon;
                case ItemSlot.Pet: return _pet;
                case ItemSlot.Equipment: return Equipment(equipmentIndex);
                default: return default;
            }
        }

        internal ItemInstance Swap(ItemSlot slot, int equipmentIndex, in ItemInstance item)
        {
            ItemInstance previous;
            switch (slot)
            {
                case ItemSlot.Helmet: previous = _helmet; _helmet = item; break;
                case ItemSlot.Chest: previous = _chest; _chest = item; break;
                case ItemSlot.Boots: previous = _boots; _boots = item; break;
                case ItemSlot.Weapon: previous = _weapon; _weapon = item; break;
                case ItemSlot.Pet: previous = _pet; _pet = item; break;
                case ItemSlot.Equipment:
                    int index = equipmentIndex >= 0 && equipmentIndex < EquipmentSlots ? equipmentIndex : 0;
                    previous = _equipment[index];
                    _equipment[index] = item;
                    break;
                default: return default;
            }

            return previous;
        }

        /// <summary>Every worn piece's stat block, for the StatSheet rebuild.</summary>
        public void CollectContributions(List<Stats.GearContribution> into)
        {
            if (!_helmet.IsEmpty) into.Add(_helmet.TotalContribution());
            if (!_chest.IsEmpty) into.Add(_chest.TotalContribution());
            if (!_boots.IsEmpty) into.Add(_boots.TotalContribution());
            if (!_weapon.IsEmpty) into.Add(_weapon.TotalContribution());
            if (!_pet.IsEmpty) into.Add(_pet.TotalContribution());
            for (int i = 0; i < EquipmentSlots; i++)
            {
                if (!_equipment[i].IsEmpty) into.Add(_equipment[i].TotalContribution());
            }
        }

        /// <summary>
        /// Every elemental-resistance roll across the loadout, as raw per-element reductions
        /// (D40). They cannot ride in the flat contribution block, so they travel beside it and
        /// the sheet sums and caps them.
        /// </summary>
        public void CollectElementalResistances(List<ElementalMultiplier> into)
        {
            CollectResistance(_helmet, into);
            CollectResistance(_chest, into);
            CollectResistance(_boots, into);
            CollectResistance(_weapon, into);
            CollectResistance(_pet, into);
            for (int i = 0; i < EquipmentSlots; i++)
            {
                CollectResistance(_equipment[i], into);
            }
        }

        private static void CollectResistance(in ItemInstance item, List<ElementalMultiplier> into)
        {
            for (int i = 0; i < item.AffixCount; i++)
            {
                AffixRoll affix = item.Affixes[i];
                if (affix.Id == AffixId.ElementalResistance && !affix.Element.IsNone)
                {
                    into.Add(new ElementalMultiplier(affix.Element, affix.Magnitude));
                }
            }
        }

        /// <summary>
        /// The element the worn weapon infuses into every hit (D19/D35), and how strongly its roll
        /// carries it. None when nothing is infused — the vast majority of weapons.
        /// </summary>
        public bool TryGetInfusion(out ElementId element, out float sourceScale)
        {
            for (int i = 0; i < _weapon.AffixCount; i++)
            {
                AffixRoll affix = _weapon.Affixes[i];
                if (affix.Id == AffixId.WeaponInfusion && !affix.Element.IsNone)
                {
                    element = affix.Element;
                    sourceScale = UnityEngine.Mathf.Clamp01(affix.Magnitude);
                    return true;
                }
            }

            element = ElementId.None;
            sourceScale = 0f;
            return false;
        }
    }
}
