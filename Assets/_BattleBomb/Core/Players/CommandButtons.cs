using System;

namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Button state carried by a <see cref="PlayerCommand"/>. Packed as flags so a command stays a
    /// small, copyable value — a remote or replayed command is the same shape as a local one (D10).
    /// The six verbs are the game's complete input vocabulary (D17).
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
        Block     = 1 << 5,
    }
}
