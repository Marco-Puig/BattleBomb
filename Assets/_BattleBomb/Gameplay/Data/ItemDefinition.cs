using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// Authoring format for one item definition (D34/D35): the name quality prefixes, the slot,
    /// and the base stat block the generator scales — a data asset in, an <see cref="ItemSpec"/>
    /// out. A new item is never a new class (pillar 3). Boss signature items are these with a
    /// forced identity at the drop.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleBomb/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable identity for save data and boss signature forcing. Never reuse a number.")]
        [SerializeField] private int _id = 1;

        [Tooltip("The authored name quality prefixes: 'Leather Chestplate' → 'Shiny Leather Chestplate'.")]
        [SerializeField] private string _displayName = "Item";

        [SerializeField] private ItemSlot _slot = ItemSlot.Weapon;

        [SerializeField] private WeaponClass _weaponClass = WeaponClass.None;

        [SerializeField] private PetClass _petClass = PetClass.None;

        [Header("Weapon")]
        [SerializeField] private float _weaponDamage;

        [Tooltip("Fractional swing/draw speed bonus; 0.1 swings a tenth faster.")]
        [SerializeField] private float _swingSpeedBonus;

        [Tooltip("Bow bolt flight speed (bows only).")]
        [SerializeField] private float _shotSpeed;

        [Header("Armor")]
        [Tooltip("Fractional damage reduction this piece's base contributes (D26).")]
        [SerializeField] private float _defence;

        [Tooltip("The speed tax (D35): weight units the piece carries; tankier rolls weigh more.")]
        [SerializeField] private float _weight;

        [Header("Passive bonuses (pets, equipment)")]
        [SerializeField] private float _critChance;
        [SerializeField] private float _critDamageBonus;
        [SerializeField] private float _lifeSteal;
        [SerializeField] private float _maxHealthBonus;
        [SerializeField] private float _maxManaBonus;
        [SerializeField] private float _manaRegen;
        [SerializeField] private float _knockbackBonus;
        [SerializeField] private float _magicDamage;
        [SerializeField] private float _magicRange;

        [Header("Consumable")]
        [Tooltip("Fraction of the pool a use restores (potions).")]
        [SerializeField] private float _healFraction;

        [Tooltip("Which pool it refills — health potions and mana potions differ only here (D27).")]
        [SerializeField] private RestoreKind _restores = RestoreKind.Health;

        [Header("Equipment active (D37) — leave the share at 0 for a passive piece")]
        [Tooltip("Damage as a share of the wearer's weapon damage. Derived on purpose: an active " +
            "scales with the build and can never outgrow it (D19).")]
        [SerializeField] private float _activeWeaponDamageShare;

        [Tooltip("The element the burst carries, marking what it hits like any other elemental hit.")]
        [SerializeField] private ElementDefinition _activeElement;

        [Tooltip("Radius of the burst, in world units.")]
        [SerializeField] private float _activeRadius = 2.5f;

        [Tooltip("The active's own cooldown in steps. A moment, never a rotation.")]
        [SerializeField] private int _activeCooldownSteps = 900;

        public int Id => _id;

        public ItemSpec ToRuntime() => new ItemSpec(
            new ItemIdentity(_id, _displayName, _slot, _weaponClass, _petClass),
            new GearContribution(
                weaponDamage: _weaponDamage,
                swingSpeedBonus: _swingSpeedBonus,
                defence: _defence,
                weight: _weight,
                critChance: _critChance,
                critDamageBonus: _critDamageBonus,
                lifeSteal: _lifeSteal,
                maxHealthBonus: _maxHealthBonus,
                maxManaBonus: _maxManaBonus,
                manaRegen: _manaRegen,
                knockbackBonus: _knockbackBonus,
                magicDamage: _magicDamage,
                magicRange: _magicRange),
            _shotSpeed,
            new RestorePayload(_restores, _healFraction),
            new ActivePayload(
                _activeWeaponDamageShare,
                _activeElement != null ? _activeElement.Id : Core.Combat.ElementId.None,
                _activeRadius,
                _activeCooldownSteps));
    }
}
