using System;

namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Button state carried by a <see cref="PlayerCommand"/>. Packed as flags so a command stays a
    /// small, copyable value — a remote or replayed command is the same shape as a local one (D10).
    /// The five verbs are the game's complete input vocabulary (D17 as amended by D26 — Block was
    /// cut for the mobile control budget; defence is the defence stat and movement).
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
    }
}
