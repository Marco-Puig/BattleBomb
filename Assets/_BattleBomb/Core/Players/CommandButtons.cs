using System;

namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Button state carried by a <see cref="PlayerCommand"/>. Packed as flags so a command stays a
    /// small, copyable value — a remote or replayed command is the same shape as a local one (D10).
    /// The five verbs are the fight's complete vocabulary (D17 as amended by D26 — Block was cut
    /// for the mobile control budget; defence is the defence stat and movement). Pause and D57's
    /// menu flags ride alongside them.
    /// </summary>
    [Flags]
    public enum CommandButtons : uint
    {
        None      = 0,
        Light     = 1 << 0,
        Heavy     = 1 << 1,
        Magic     = 1 << 2,
        Equipment = 1 << 3,
        Jump      = 1 << 4,

        /// <summary>
        /// Opens the settings menu (M6). Not a combat verb — D17's five-button budget is about
        /// what the thumb does mid-fight, and every console game has a Start button besides.
        /// It rides the command stream anyway, because rule 3 has no exceptions: devices become
        /// commands in exactly one place.
        /// </summary>
        Pause     = 1 << 5,

        /// <summary>
        /// The menu layer (D57): what a screen's buttons mean, kept apart from the five verbs.
        /// They ride their own action map, so rebinding a combat verb can never move "confirm" —
        /// and one physical button can carry both, which is how A is Jump in a fight and Confirm
        /// on a screen. Never on the mobile thumb surface: touch taps the thing it wants (D5).
        /// </summary>
        Confirm     = 1 << 6,
        Back        = 1 << 7,

        /// <summary>The one-press verb for the item under the cursor: sell it, or mid-combine grind
        /// the whole pile. A shortcut only — every verb it fires is also in the item's own menu.</summary>
        Option      = 1 << 8,

        /// <summary>Lock or release the item under the cursor. A shortcut, like Option.</summary>
        Lock        = 1 << 9,

        TabPrevious = 1 << 10,
        TabNext     = 1 << 11,
    }
}
