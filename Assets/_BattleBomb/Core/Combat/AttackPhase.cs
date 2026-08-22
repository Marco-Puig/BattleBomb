namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// Where a character is inside the combat step cycle. Ready is the only phase that accepts
    /// movement; Charging is Heavy held from neutral (D19 — the kit's only hold).
    /// </summary>
    public enum AttackPhase
    {
        Ready = 0,
        Startup = 1,
        Active = 2,
        Recovery = 3,
        Charging = 4,
    }
}
