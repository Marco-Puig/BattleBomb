using System;
using BattleBomb.Core.Combat;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// Who an item is — the fields that name and classify it, identical between the authored
    /// spec and the generated instance (task 61: the M5 close-out's constructor regrouping).
    /// </summary>
    public readonly struct ItemIdentity
    {
        public readonly int DefinitionId;
        public readonly string Name;
        public readonly ItemSlot Slot;
        public readonly WeaponClass WeaponClass;
        public readonly PetClass PetClass;

        public ItemIdentity(
            int definitionId,
            string name,
            ItemSlot slot,
            WeaponClass weaponClass = WeaponClass.None,
            PetClass petClass = PetClass.None)
        {
            DefinitionId = definitionId;
            Name = name ?? string.Empty;
            Slot = slot;
            WeaponClass = weaponClass;
            PetClass = petClass;
        }

        /// <summary>The same identity under a different display name — the generator stamping
        /// the quality prefix onto the authored essence name.</summary>
        public ItemIdentity Named(string name) =>
            new ItemIdentity(DefinitionId, name, Slot, WeaponClass, PetClass);
    }

    /// <summary>
    /// What a consumable refills and by how much. On a spec the fraction is the authored base;
    /// on an instance it is the rank-scaled roll (the container is the potency).
    /// </summary>
    public readonly struct RestorePayload
    {
        public readonly RestoreKind Kind;
        public readonly float Fraction;

        public RestorePayload(RestoreKind kind, float fraction)
        {
            Kind = kind;
            Fraction = Math.Max(0f, fraction);
        }
    }

    /// <summary>
    /// An equipment active's payload (D37) — zero on the passive majority. Damage is a share of
    /// the wearer's weapon damage and the cooldown is the item's own: D19's structural guards.
    /// </summary>
    public readonly struct ActivePayload
    {
        public readonly float WeaponDamageShare;
        public readonly ElementId Element;
        public readonly float Radius;
        public readonly int CooldownSteps;

        public ActivePayload(float weaponDamageShare, ElementId element, float radius, int cooldownSteps)
        {
            WeaponDamageShare = Math.Max(0f, weaponDamageShare);
            Element = element;
            Radius = Math.Max(0f, radius);
            CooldownSteps = Math.Max(0, cooldownSteps);
        }

        public bool Exists => WeaponDamageShare > 0f;

        /// <summary>The same active with its damage share scaled — the rank's budget deciding
        /// how big the moment is without ever letting it outgrow the build.</summary>
        public ActivePayload Scaled(float factor) =>
            new ActivePayload(WeaponDamageShare * Math.Max(0f, factor), Element, Radius, CooldownSteps);
    }

    /// <summary>
    /// The Dungeon Defenders half of an instance (D35/D44): capacity stamped at the drop, points
    /// spent by the player, and D43's lock keeping the item out of every selling and combining
    /// path until it is deliberately released.
    /// </summary>
    public readonly struct ItemInvestment
    {
        public readonly int Capacity;
        public readonly int Spent;
        public readonly bool Locked;

        public ItemInvestment(int capacity, int spent = 0, bool locked = false)
        {
            Capacity = Math.Max(0, capacity);
            Spent = Math.Max(0, spent);
            Locked = locked;
        }

        public int Remaining => Math.Max(0, Capacity - Spent);

        public ItemInvestment WithLock(bool locked) => new ItemInvestment(Capacity, Spent, locked);

        public ItemInvestment WithSpent(int spent) => new ItemInvestment(Capacity, spent, Locked);
    }
}
