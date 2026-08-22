using BattleBomb.Core.Combat;

namespace BattleBomb.Core.Items
{
    /// <summary>What the quick-use slot holds (D37).</summary>
    public enum QuickSlotKind
    {
        Empty = 0,
        Consumable = 1,
        EquipmentActive = 2,
    }

    /// <summary>Which pool a consumable refills (D27: health and mana potions both exist).</summary>
    public enum RestoreKind
    {
        Health = 0,
        Mana = 1,
    }

    /// <summary>
    /// One press of the quick-use button, resolved (D37): whether anything fired, the fraction a
    /// potion restored and which pool it went to, and — for an equipment active — the burst it
    /// asked for. Damage is a <em>share of weapon damage</em>, never a number of its own: D19's
    /// structural guard, so an active can never outgrow the build that carries it.
    /// </summary>
    public readonly struct QuickUseResult
    {
        public readonly bool Used;
        public readonly float RestoreFraction;
        public readonly RestoreKind Restores;

        /// <summary>The active's damage as a share of the wearer's weapon damage. 0 for potions.</summary>
        public readonly float ActiveWeaponDamageShare;

        /// <summary>The active's element, applying its mark like any other elemental hit.</summary>
        public readonly ElementId ActiveElement;

        /// <summary>Radius of the active's burst, in world units.</summary>
        public readonly float ActiveRadius;

        public QuickUseResult(
            bool used,
            float restoreFraction,
            RestoreKind restores = RestoreKind.Health,
            float activeWeaponDamageShare = 0f,
            ElementId activeElement = default,
            float activeRadius = 0f)
        {
            Used = used;
            RestoreFraction = used ? restoreFraction : 0f;
            Restores = restores;
            ActiveWeaponDamageShare = used ? activeWeaponDamageShare : 0f;
            ActiveElement = used ? activeElement : ElementId.None;
            ActiveRadius = used ? activeRadius : 0f;
        }

        public static QuickUseResult Nothing => default;

        /// <summary>True when the press fired an equipment active rather than a drink.</summary>
        public bool FiredActive => Used && ActiveWeaponDamageShare > 0f;
    }
}
