using System;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// A message that is truncated, oversized, or names something that cannot exist. The wire never
    /// trusts a count it read (HANDOFF-M8 planning decision 20): anything past the bytes it was
    /// given, or past the bound the reader was told, is this — never an out-of-range crash.
    /// </summary>
    public sealed class NetFormatException : Exception
    {
        public NetFormatException(string message) : base(message)
        {
        }
    }
}
