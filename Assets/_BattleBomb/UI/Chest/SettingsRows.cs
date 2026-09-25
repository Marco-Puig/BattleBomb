using System.Collections.Generic;

namespace BattleBomb.UI.Chest
{
    /// <summary>What a settings row does.</summary>
    public enum SettingsRow
    {
        AutoEquip = 0,
        AutoSell = 1,
        ReturnToChapters = 2,
        GrantTestLoot = 3,
        TierOverlay = 4,
    }

    /// <summary>
    /// The settings menu's rows, in order, per build flavour. The debug rows come last and exist
    /// only in a development build or the editor — D50's rule for the tier overlay, now the DEBUG
    /// grant's too (pre-M8 fix F2): a release never offers a row that hands out loot. Last, so the
    /// rows both flavours share sit at the same index in both.
    /// </summary>
    public static class SettingsRows
    {
        private static readonly SettingsRow[] Release =
        {
            SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters,
        };

        private static readonly SettingsRow[] Development =
        {
            SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters,
            SettingsRow.GrantTestLoot, SettingsRow.TierOverlay,
        };

        public static IReadOnlyList<SettingsRow> For(bool development) => development ? Development : Release;

        /// <summary>This build's rows.</summary>
        public static IReadOnlyList<SettingsRow> Current =>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Development;
#else
            Release;
#endif

        /// <summary>The cursor one row up (-1) or down (+1), wrapping at both ends.</summary>
        public static int Step(int cursor, int direction, int count) =>
            count <= 0 ? 0 : ((cursor + direction) % count + count) % count;
    }
}
