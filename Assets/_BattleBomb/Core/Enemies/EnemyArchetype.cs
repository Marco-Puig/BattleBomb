namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// The four standardized pressures (D22). Regions skin these with data — element, attacks,
    /// look — never with new behaviour code. Placeholder names stay abstract (§9): no creatures.
    /// </summary>
    public enum EnemyArchetype
    {
        /// <summary>Depth-limited melee, like the player (§2.2) — crowds you into managing depth.</summary>
        Grunt = 0,

        /// <summary>Holds a standoff and fires across depth (§2.2) — punishes standing still.</summary>
        Ranged,

        /// <summary>Slow, hard-hitting elemental artillery behind the longest telegraphs (D26).</summary>
        Caster,

        /// <summary>Slow, telegraphed, never flinches — dodged with jump and depth, not interrupted.</summary>
        Brute,
    }
}
