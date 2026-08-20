namespace BattleBomb.Core.Items
{
    /// <summary>What the quick-use slot holds (D37).</summary>
    public enum QuickSlotKind
    {
        Empty = 0,
        Consumable = 1,
        EquipmentActive = 2,
    }

    /// <summary>
    /// One press of the quick-use button, resolved: whether anything fired, and the heal it
    /// carries when a potion did. Equipment actives are machinery-only until M5 authors the
    /// first one (planning decision 10) — they assign but never fire here.
    /// </summary>
    public readonly struct QuickUseResult
    {
        public readonly bool Used;
        public readonly float HealFraction;

        public QuickUseResult(bool used, float healFraction)
        {
            Used = used;
            HealFraction = used ? healFraction : 0f;
        }

        public static QuickUseResult Nothing => default;
    }
}
