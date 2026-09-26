using System.Collections.Generic;
using BattleBomb.Core.Loot;
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
    /// One player's whole item state (task 43, the 2019 antidote): the worn loadout and the
    /// quick-use slot are personal, but since D51 the bag itself is shared storage — a
    /// <see cref="Core.Items.Sack"/> any number of couch players' inventories can wrap. The
    /// complete M4 surface is add, equip/unequip with the D36 level lock, quick-slot assign and
    /// use, the D30 auto-equip flag, and the prestige re-validation — nothing more, by design.
    /// </summary>
    public sealed class Inventory
    {
        private readonly Sack _sack;

        private QuickSlotKind _quickKind;
        private int _quickConsumableId;
        private int _quickEquipmentIndex;
        private int _quickCooldown;

        /// <summary>A private sack — one player alone, and every pre-D51 test.</summary>
        public Inventory() : this(new Sack())
        {
        }

        /// <summary>A loadout over a sack that other inventories may share (D51).</summary>
        public Inventory(Sack sack)
        {
            _sack = sack ?? new Sack();
        }

        /// <summary>The shared storage — the chest screens of both couch players look at this.</summary>
        public Sack Sack => _sack;

        private List<ItemStack> _items => _sack.Entries;

        public Loadout Loadout { get; } = new Loadout();

        public bool AutoEquip
        {
            get => _sack.AutoEquip;
            set => _sack.AutoEquip = value;
        }

        public bool AutoSell
        {
            get => _sack.AutoSell;
            set => _sack.AutoSell = value;
        }

        public SackRules Rules
        {
            get => _sack.Rules;
            set => _sack.Rules = value;
        }

        public PriceBook Prices
        {
            get => _sack.Prices;
            set => _sack.Prices = value;
        }

        public IReadOnlyList<ItemStack> Items => _sack.Items;

        public int SlotsUsed => _sack.SlotsUsed;

        public bool IsFull => _sack.IsFull;

        public QuickSlotKind QuickKind => _quickKind;
        public int QuickConsumableId => _quickConsumableId;
        public int QuickEquipmentIndex => _quickEquipmentIndex;
        public int QuickCooldownRemaining => _quickCooldown;

        /// <summary>
        /// Takes an item into the bag (D43). Consumables stack by definition and quality up to
        /// the stack limit — a sixth identical potion has nowhere to go and the pickup refuses.
        /// A full sack refuses too, unless auto-sell is on and something unlocked can be sold to
        /// make room. With auto-equip on, gear equips itself only when its slot is empty or its
        /// quality rank strictly beats the worn piece's, and the level lock still applies.
        /// </summary>
        public AddResult Add(in ItemInstance item, int currentLevel)
        {
            if (item.IsEmpty)
            {
                return AddResult.Refused;
            }

            if (item.IsConsumable)
            {
                // Stacks split by quality: a Vial and an Elixir heal differently and never merge.
                int stack = FindStackFor(item);
                if (stack >= 0)
                {
                    if (_items[stack].Count >= Rules.StackLimit)
                    {
                        // Five of one potion is the ceiling — drink some before hauling more.
                        return AddResult.Refused;
                    }

                    _items[stack] = new ItemStack(_items[stack].Item, _items[stack].Count + 1);
                    _sack.Touch();
                    return new AddResult(true, false, 0);
                }
            }

            int coins = MakeRoom();
            if (IsFull)
            {
                return AddResult.Refused;
            }

            _items.Add(new ItemStack(item, 1));
            _sack.Touch();

            if (!item.IsConsumable && AutoEquip && item.RequiredLevel <= currentLevel)
            {
                ItemInstance worn = Loadout.Worn(item.Slot);
                if ((worn.IsEmpty || item.Quality > worn.Quality)
                    && TryEquip(_items.Count - 1, currentLevel))
                {
                    return new AddResult(true, true, coins);
                }
            }

            return new AddResult(true, false, coins);
        }

        /// <summary>
        /// Sells the stack at this index outright, returning what the shopkeeper (or the
        /// auto-sell setting) pays — the whole stack, priced per unit. A locked item refuses:
        /// the lock is absolute until the player releases it (D43).
        /// </summary>
        public int Sell(int bagIndex)
        {
            if (bagIndex < 0 || bagIndex >= _items.Count || _items[bagIndex].Item.Locked)
            {
                return 0;
            }

            ItemStack stack = _items[bagIndex];
            _items.RemoveAt(bagIndex);
            _sack.Touch();
            if (stack.Item.IsConsumable && _quickKind == QuickSlotKind.Consumable
                && _quickConsumableId == stack.Item.DefinitionId
                && FindConsumableStack(_quickConsumableId) < 0)
            {
                ClearQuickSlot();
            }

            return Prices.SellPrice(stack.Item) * stack.Count;
        }

        /// <summary>
        /// What "clear the junk" would sell, without selling it: every unlocked, non-consumable
        /// stack below <paramref name="below"/> — a potion stack is not "a piece" here, mirroring
        /// <see cref="FindAutoSellTarget"/> preferring gear over consumables. Worn gear is never
        /// in this count; it lives in the loadout, not the bag. Read-only, so the confirmation
        /// screen can show the number before the player commits to it.
        /// </summary>
        public JunkSale PreviewJunk(QualityRank below)
        {
            int stacks = 0;
            int pieces = 0;
            int coins = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                ItemStack stack = _items[i];
                if (stack.Item.Locked || stack.Item.IsConsumable || stack.Item.Quality >= below)
                {
                    continue;
                }

                stacks++;
                pieces += stack.Count;
                coins += Prices.SellPrice(stack.Item) * stack.Count;
            }

            return new JunkSale(stacks, pieces, coins);
        }

        /// <summary>
        /// Sells everything <see cref="PreviewJunk"/> would report, for the same total. Walks the
        /// bag backwards and calls <see cref="Sell"/> for each qualifying index rather than
        /// removing entries directly, so the quick slot's own clearing logic never drifts from
        /// the one-at-a-time sell path.
        /// </summary>
        public JunkSale SellJunk(QualityRank below)
        {
            int stacks = 0;
            int pieces = 0;
            int coins = 0;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                ItemStack stack = _items[i];
                if (stack.Item.Locked || stack.Item.IsConsumable || stack.Item.Quality >= below)
                {
                    continue;
                }

                stacks++;
                pieces += stack.Count;
                coins += Sell(i);
            }

            return new JunkSale(stacks, pieces, coins);
        }

        /// <summary>Locks or releases the stack at this index — the auto-sell guard (D43).</summary>
        public bool SetLock(int bagIndex, bool locked)
        {
            if (bagIndex < 0 || bagIndex >= _items.Count)
            {
                return false;
            }

            ItemStack stack = _items[bagIndex];
            _items[bagIndex] = new ItemStack(stack.Item.WithLock(locked), stack.Count);
            _sack.Touch();
            return true;
        }

        /// <summary>
        /// Spends one capacity point into a bagged item's stat (D44). The caller has already
        /// taken the money, so a refusal here must be a refusal there too.
        /// </summary>
        public bool TryUpgradeBagged(int bagIndex, in UpgradeTarget target)
        {
            if (bagIndex < 0 || bagIndex >= _items.Count)
            {
                return false;
            }

            ItemStack stack = _items[bagIndex];
            if (!ItemUpgrade.TryApply(stack.Item, target, out ItemInstance upgraded))
            {
                return false;
            }

            _items[bagIndex] = new ItemStack(upgraded, stack.Count);
            _sack.Touch();
            return true;
        }

        /// <summary>The same, for a piece already being worn — deepening your best item should
        /// never require taking it off first.</summary>
        public bool TryUpgradeWorn(ItemSlot slot, int equipmentIndex, in UpgradeTarget target)
        {
            ItemInstance worn = Loadout.Worn(slot, equipmentIndex);
            if (worn.IsEmpty || !ItemUpgrade.TryApply(worn, target, out ItemInstance upgraded))
            {
                return false;
            }

            Loadout.Swap(slot, equipmentIndex, upgraded);
            return true;
        }

        /// <summary>Locks or releases a worn piece, so a keeper stays safe while it is in use.</summary>
        public bool SetWornLock(ItemSlot slot, int equipmentIndex, bool locked)
        {
            ItemInstance worn = Loadout.Worn(slot, equipmentIndex);
            if (worn.IsEmpty)
            {
                return false;
            }

            Loadout.Swap(slot, equipmentIndex, worn.WithLock(locked));
            return true;
        }

        /// <summary>
        /// D44's gamble across two bag slots: both stacks give up one item each, and the reroll
        /// lands in the bag. Refuses anything <see cref="ItemCombine.CanCombine"/> refuses.
        /// </summary>
        public DeterministicRandom TryCombine(
            in DeterministicRandom rng,
            int firstIndex,
            int secondIndex,
            in GenerationContext context,
            out CombineResult result)
        {
            result = CombineResult.Refused;
            DeterministicRandom next = rng;
            if (firstIndex == secondIndex
                || firstIndex < 0 || firstIndex >= _items.Count
                || secondIndex < 0 || secondIndex >= _items.Count)
            {
                return next;
            }

            ItemInstance a = _items[firstIndex].Item;
            ItemInstance b = _items[secondIndex].Item;
            if (!ItemCombine.CanCombine(a, b))
            {
                return next;
            }

            next = ItemCombine.Combine(next, a, b, context, out result);
            if (!result.Combined)
            {
                return next;
            }

            // Highest index first, so removing one never shifts the other out from under us.
            int high = Mathf.Max(firstIndex, secondIndex);
            int low = Mathf.Min(firstIndex, secondIndex);
            RemoveOne(high);
            RemoveOne(low);
            _items.Add(new ItemStack(result.Item, 1));
            _sack.Touch();
            return next;
        }

        /// <summary>
        /// D44's gamble taken to the end of the pile: the piece at <paramref name="anchorIndex"/>
        /// and every unlocked duplicate of it are ground down two at a time until fewer than two
        /// of it remain. What the run holds on to is the anchor's <em>identity</em> — its
        /// definition and its rank — never its index, because every combine removes two stacks and
        /// shifts everything after them.
        /// <para>
        /// Only that captured identity is ground. A promotion comes back a rank higher, so it
        /// leaves the pool the moment it lands and is never fed back in: the rank up is the payoff
        /// D44 is paying, and cascading it would make one press's outcome unpredictable.
        /// </para>
        /// </summary>
        public DeterministicRandom CombineAll(
            in DeterministicRandom rng,
            int anchorIndex,
            in GenerationContext context,
            out CombineRun run)
        {
            run = default;
            DeterministicRandom next = rng;
            if (anchorIndex < 0 || anchorIndex >= _items.Count)
            {
                return next;
            }

            // The captured value carries the definition and the rank, and CanCombine against it is
            // the same rule the single combine and the chest grid already ask.
            ItemInstance anchor = _items[anchorIndex].Item;
            if (anchor.IsEmpty || anchor.IsConsumable || anchor.Locked)
            {
                return next;
            }

            int combines = 0;
            int promotions = 0;

            // A guard, not a rule: gear sits one per stack and every pass removes two of them for
            // at most one back, so the pool always shrinks and this cap should be unreachable.
            int cap = _items.Count;
            while (combines < cap)
            {
                int first = -1;
                int second = -1;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (!ItemCombine.CanCombine(anchor, _items[i].Item))
                    {
                        continue;
                    }

                    if (first < 0)
                    {
                        first = i;
                        continue;
                    }

                    second = i;
                    break;
                }

                if (second < 0)
                {
                    break;
                }

                next = TryCombine(next, first, second, context, out CombineResult result);
                if (!result.Combined)
                {
                    break;
                }

                combines++;
                if (result.Promoted)
                {
                    promotions++;
                }
            }

            run = new CombineRun(combines, promotions);
            return next;
        }

        /// <summary>
        /// The item at <paramref name="bagIndex"/> and every duplicate it can combine with, in
        /// bag order — the whole of what the chest grid offers while a combine is pending. A
        /// duplicate is the only pick that can work, so it is the only pick shown (Michael, M7
        /// pass). An index with nothing to pair with answers with itself alone.
        /// </summary>
        public void CombineChoices(int bagIndex, List<int> into)
        {
            into.Clear();
            if (bagIndex < 0 || bagIndex >= _items.Count)
            {
                return;
            }

            ItemInstance anchor = _items[bagIndex].Item;
            for (int i = 0; i < _items.Count; i++)
            {
                if (i == bagIndex || ItemCombine.CanCombine(anchor, _items[i].Item))
                {
                    into.Add(i);
                }
            }
        }

        /// <summary>Whether anything else in the bag can pair with this one — what decides
        /// whether the chest screen offers Combine at all.</summary>
        public bool HasCombinePartner(int bagIndex)
        {
            if (bagIndex < 0 || bagIndex >= _items.Count)
            {
                return false;
            }

            ItemInstance anchor = _items[bagIndex].Item;
            for (int i = 0; i < _items.Count; i++)
            {
                if (i != bagIndex && ItemCombine.CanCombine(anchor, _items[i].Item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Takes one item off a stack, dropping the stack when it empties.</summary>
        private void RemoveOne(int index)
        {
            ItemStack stack = _items[index];
            if (stack.Count > 1)
            {
                _items[index] = new ItemStack(stack.Item, stack.Count - 1);
            }
            else
            {
                _items.RemoveAt(index);
            }
        }

        /// <summary>
        /// Auto-sell's target: the worst unlocked stack in the bag, gear before consumables so a
        /// stack of potions is never spent to shelter one more helmet. Lowest quality wins,
        /// earliest slot breaks the tie — deterministic, like every other loot decision.
        /// </summary>
        public int FindAutoSellTarget()
        {
            int best = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                ItemInstance candidate = _items[i].Item;
                if (candidate.Locked)
                {
                    continue;
                }

                if (best < 0)
                {
                    best = i;
                    continue;
                }

                ItemInstance incumbent = _items[best].Item;
                if (incumbent.IsConsumable != candidate.IsConsumable)
                {
                    if (incumbent.IsConsumable)
                    {
                        best = i;
                    }

                    continue;
                }

                if (candidate.Quality < incumbent.Quality)
                {
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Sells the worst unlocked stack when the sack is full and the setting is on.</summary>
        private int MakeRoom()
        {
            if (!IsFull || !AutoSell)
            {
                return 0;
            }

            int target = FindAutoSellTarget();
            return target < 0 ? 0 : Sell(target);
        }

        private int FindStackFor(in ItemInstance item)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Item.IsConsumable
                    && _items[i].Item.DefinitionId == item.DefinitionId
                    && _items[i].Item.Quality == item.Quality)
                {
                    return i;
                }
            }

            return -1;
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

            _sack.Touch();
            if (item.Slot == ItemSlot.Equipment && _quickKind == QuickSlotKind.EquipmentActive
                && _quickEquipmentIndex == equipmentIndex)
            {
                ClearQuickSlot();
            }

            return true;
        }

        public bool Unequip(ItemSlot slot, int equipmentIndex = 0) => Unequip(slot, equipmentIndex, false);

        /// <summary>
        /// Takes a worn piece off into the bag. A full sack refuses — there is nowhere to put it
        /// (D43) — except when <paramref name="forced"/>, the prestige re-lock (D36) returning
        /// gear the reset level can no longer carry: overflowing the sack beats losing the gear.
        /// </summary>
        private bool Unequip(ItemSlot slot, int equipmentIndex, bool forced)
        {
            ItemInstance worn = Loadout.Worn(slot, equipmentIndex);
            if (worn.IsEmpty || (IsFull && !forced))
            {
                return false;
            }

            Loadout.Swap(slot, equipmentIndex, default);
            _items.Add(new ItemStack(worn, 1));
            _sack.Touch();

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

            return Unequip(slot, equipmentIndex, forced: true);
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

        /// <summary>A load (D52) putting the quick slot back exactly as it was saved.</summary>
        public void RestoreQuickSlot(QuickSlotKind kind, int consumableId, int equipmentIndex)
        {
            _quickKind = kind;
            _quickConsumableId = consumableId;
            _quickEquipmentIndex = equipmentIndex;
            _quickCooldown = 0;
        }

        /// <summary>
        /// The quick-use press (D37). A slotted potion restores its pool and starts the cooldown;
        /// an exhausted stack clears the slot; a worn equipment piece with an active fires it on
        /// the item's own cooldown, which is what keeps an active a moment rather than a rotation.
        /// </summary>
        public QuickUseResult UseQuickSlot(int cooldownSteps)
        {
            if (_quickCooldown > 0 || _quickKind == QuickSlotKind.Empty)
            {
                return QuickUseResult.Nothing;
            }

            if (_quickKind == QuickSlotKind.EquipmentActive)
            {
                ItemInstance worn = Loadout.Equipment(_quickEquipmentIndex);
                if (worn.IsEmpty || !worn.HasActive)
                {
                    return QuickUseResult.Nothing;
                }

                _quickCooldown = Mathf.Max(0, worn.ActiveCooldownSteps);
                return new QuickUseResult(
                    true, 0f, RestoreKind.Health,
                    worn.ActiveWeaponDamageShare, worn.ActiveElement, worn.ActiveRadius);
            }

            int index = FindConsumableStack(_quickConsumableId);
            if (index < 0)
            {
                ClearQuickSlot();
                return QuickUseResult.Nothing;
            }

            ItemStack stack = _items[index];
            float heal = stack.Item.ConsumableHealFraction;
            RestoreKind restores = stack.Item.Restores;
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

            _sack.Touch();
            _quickCooldown = Mathf.Max(0, cooldownSteps);
            return new QuickUseResult(true, heal, restores);
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
