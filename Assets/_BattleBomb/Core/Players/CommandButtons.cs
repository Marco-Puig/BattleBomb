using System;

namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Button state carried by a <see cref="PlayerCommand"/>. Packed as flags so a command stays a
    /// small, copyable value — a remote or replayed command is the same shape as a local one (D10).
    /// </summary>
    [Flags]
    public enum CommandButtons : uint
    {
        None     = 0,
        Attack   = 1 << 0,
        Heavy    = 1 << 1,
        Dodge    = 1 << 2,
        Ability1 = 1 << 3,
        Ability2 = 1 << 4,
        Interact = 1 << 5,
    }
}
