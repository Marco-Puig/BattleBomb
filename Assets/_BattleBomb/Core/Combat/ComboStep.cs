namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// One position in a combo chain: the Light attack that continues it, and optionally a Heavy
    /// ender authored for this position (D19 — `L-L-H` is the ender at position two). Heavy at a
    /// position with no ender falls back to the kit's standalone Heavy.
    /// </summary>
    public readonly struct ComboStep
    {
        public readonly AttackTuning OnLight;
        public readonly AttackTuning OnHeavy;
        public readonly bool HasHeavy;

        public ComboStep(AttackTuning onLight)
        {
            OnLight = onLight;
            OnHeavy = default;
            HasHeavy = false;
        }

        public ComboStep(AttackTuning onLight, AttackTuning onHeavy)
        {
            OnLight = onLight;
            OnHeavy = onHeavy;
            HasHeavy = true;
        }
    }
}
