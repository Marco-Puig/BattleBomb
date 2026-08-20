using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>One bag entry: gear sits one per stack, consumables pile up by definition.</summary>
    public readonly struct ItemStack
    {
        public readonly ItemInstance Item;
        public readonly int Count;

        public ItemStack(in ItemInstance item, int count)
        {
            Item = item;
            Count = Mathf.Max(1, count);
        }
    }

    /// <summary>
    /// One player's whole item state (task 43, the 2019 antidote): an unbounded bag, the worn
    /// loadout, and the quick-use slot. The complete M4 surface is add, equip/unequip with the
    /// D36 level lock, quick-slot assign and use, the D30 auto-equip flag, and the prestige
    /// re-validation — nothing more, by design.
    /// </summary>
    public sealed class Inventory
    {
        private readonly List<ItemStack> _items = new List<ItemStack>();

        private QuickSlotKind _quickKind;
        private int _quickConsumableId;
        private int _quickEquipmentIndex;
        private int _quickCooldown;

        public Loadout Loadout { get; } = new Loadout();

        /// <summary>D30: off by default — grabbed loot lands in the bag unless the player opts in.</summary>
        public bool AutoEquip { get; set; }

        public IReadOnlyList<ItemStack> Items => _items;

        public QuickSlotKind QuickKind => _quickKind;
        public int QuickConsumableId => _quickConsumableId;
        public int QuickEquipmentIndex => _quickEquipmentIndex;
        public int QuickCooldownRemaining => _quickCooldown;

        /// <summary>
        /// Takes an item into the bag; consumables stack by definition. With auto-equip on, gear
        /// equips itself only when its slot is empty or its quality rank strictly beats the worn
        /// piece's, and the level lock still applies (planning decision 7). Returns whether the
        /// item ended up worn.
        /// </summary>
        public bool Add(in ItemInstance item, int currentLevel)
        {
            if (item.IsEmpty)
            {
                return false;
            }

            if (item.IsConsumable)
            {
                // Stacks split by quality: a Vial and an Elixir heal differently and never merge.
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].Item.IsConsumable
                        && _items[i].Item.DefinitionId == item.DefinitionId
                        && _items[i].Item.Quality == item.Quality)
                    {
                        _items[i] = new ItemStack(_items[i].Item, _items[i].Count + 1);
                        return false;
                    }
                }

                _items.Add(new ItemStack(item, 1));
                return false;
            }

            _items.Add(new ItemStack(item, 1));

            if (AutoEquip && item.RequiredLevel <= currentLevel)
            {
                ItemInstance worn = Loadout.Worn(item.Slot);
                if (worn.IsEmpty || item.Quality > worn.Quality)
                {
                    return TryEquip(_items.Count - 1, currentLevel);
                }
            }

            return false;
        }

        /// <summary>
        /// Wears the bag item at this index; the displaced piece returns to the bag. Refuses
        /// consumables and anything above the current level — the D36 lock lives here.
        /// </summary>
        public bool TryEquip(int bagIndex, int currentLevel, int equipmentIndex = 0)
        {
            if (bagIndex < 0 || bagIndex >= _items.Count)
            {
                return false;
            }

            ItemInstance item = _items[bagIndex].Item;
            if (item.IsConsumable || item.RequiredLevel > currentLevel)
            {
                return false;
            }

            _items.RemoveAt(bagIndex);
            ItemInstance previous = Loadout.Swap(item.Slot, equipmentIndex, item);
            if (!previous.IsEmpty)
            {
                _items.Add(new ItemStack(previous, 1));
            }

            if (item.Slot == ItemSlot.Equipment && _quickKind == QuickSlotKind.EquipmentActive
                && _quickEquipmentIndex == equipmentIndex)
            {
                ClearQuickSlot();
            }

            return true;
        }

        public bool Unequip(ItemSlot slot, int equipmentIndex = 0)
        {
            ItemInstance worn = Loadout.Worn(slot, equipmentIndex);
            if (worn.IsEmpty)
            {
                return false;
            }

            Loadout.Swap(slot, equipmentIndex, default);
            _items.Add(new ItemStack(worn, 1));

            if (slot == ItemSlot.Equipment && _quickKind == QuickSlotKind.EquipmentActive
                && _quickEquipmentIndex == equipmentIndex)
            {
                ClearQuickSlot();
            }

            return true;
        }

        /// <summary>
        /// D36 at the prestige moment: every worn piece the reset level can no longer carry
        /// returns to the bag. No grandfather clause (planning decision 8).
        /// </summary>
        public int ReturnOverLevelGear(int currentLevel)
        {
            int returned = 0;
            returned += ReturnIfLocked(ItemSlot.Helmet, 0, currentLevel) ? 1 : 0;
            returned += ReturnIfLocked(ItemSlot.Chest, 0, currentLevel) ? 1 : 0;
            returned += ReturnIfLocked(ItemSlot.Boots, 0, currentLevel) ? 1 : 0;
            returned += ReturnIfLocked(ItemSlot.Weapon, 0, currentLevel) ? 1 : 0;
            returned += ReturnIfLocked(ItemSlot.Pet, 0, currentLevel) ? 1 : 0;
            for (int i = 0; i < Loadout.EquipmentSlots; i++)
            {
                returned += ReturnIfLocked(ItemSlot.Equipment, i, currentLevel) ? 1 : 0;
            }

            return returned;
        }

        private bool ReturnIfLocked(ItemSlot slot, int equipmentIndex, int currentLevel)
        {
            ItemInstance worn = Loadout.Worn(slot, equipmentIndex);
            if (worn.IsEmpty || worn.RequiredLevel <= currentLevel)
            {
                return false;
            }

            return Unequip(slot, equipmentIndex);
        }

        public bool AssignQuickConsumable(int definitionId)
        {
            if (FindConsumableStack(definitionId) < 0)
            {
                return false;
            }

            _quickKind = QuickSlotKind.Consumable;
            _quickConsumableId = definitionId;
            return true;
        }

        public bool AssignQuickEquipment(int equipmentIndex)
        {
            if (Loadout.Equipment(equipmentIndex).IsEmpty)
            {
                return false;
            }

            _quickKind = QuickSlotKind.EquipmentActive;
            _quickEquipmentIndex = equipmentIndex;
            return true;
        }

        public void ClearQuickSlot()
        {
            _quickKind = QuickSlotKind.Empty;
            _quickConsumableId = 0;
            _quickEquipmentIndex = 0;
        }

        /// <summary>
        /// The quick-use press. A slotted potion heals and starts the cooldown; an exhausted
        /// stack clears the slot; equipment actives fire nothing until M5 authors one.
        /// </summary>
        public QuickUseResult UseQuickSlot(int cooldownSteps)
        {
            if (_quickCooldown > 0 || _quickKind != QuickSlotKind.Consumable)
            {
                return QuickUseResult.Nothing;
            }

            int index = FindConsumableStack(_quickConsumableId);
            if (index < 0)
            {
                ClearQuickSlot();
                return QuickUseResult.Nothing;
            }

            ItemStack stack = _items[index];
            float heal = stack.Item.ConsumableHealFraction;
            if (stack.Count > 1)
            {
                _items[index] = new ItemStack(stack.Item, stack.Count - 1);
            }
            else
            {
                _items.RemoveAt(index);
                if (FindConsumableStack(_quickConsumableId) < 0)
                {
                    ClearQuickSlot();
                }
            }

            _quickCooldown = Mathf.Max(0, cooldownSteps);
            return new QuickUseResult(true, heal);
        }

        /// <summary>One fixed step of cooldown time.</summary>
        public void Step()
        {
            if (_quickCooldown > 0)
            {
                _quickCooldown--;
            }
        }

        /// <summary>The weakest matching stack — the quick slot drinks cheap potions first.</summary>
        private int FindConsumableStack(int definitionId)
        {
            int best = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                if (!_items[i].Item.IsConsumable || _items[i].Item.DefinitionId != definitionId)
                {
                    continue;
                }

                if (best < 0 || _items[i].Item.Quality < _items[best].Item.Quality)
                {
                    best = i;
                }
            }

            return best;
        }
    }
}
