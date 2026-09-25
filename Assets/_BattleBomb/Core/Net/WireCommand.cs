using System;
using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// One command as it crosses the wire: the sender's frame, the quantised stick, and what was
    /// held. Press and release edges are not sent — the host re-derives them from consecutive held
    /// states (<see cref="RemoteCommandStream"/>), which is what lets a redundant copy arrive twice
    /// without pressing twice.
    /// </summary>
    public readonly struct WireCommand : IEquatable<WireCommand>
    {
        public readonly int Frame;
        public readonly Vector2 Move;
        public readonly CommandButtons Held;

        public WireCommand(int frame, Vector2 move, CommandButtons held)
        {
            Frame = frame;
            Move = move;
            Held = held;
        }

        public static WireCommand From(in PlayerCommand command) =>
            new WireCommand(command.Frame, NetQuantize.Move(command.Move), command.Held);

        public bool Equals(WireCommand other) =>
            Frame == other.Frame && Move.Equals(other.Move) && Held == other.Held;

        public override bool Equals(object obj) => obj is WireCommand other && Equals(other);

        public override int GetHashCode() => Frame;

        public override string ToString() => $"#{Frame} {Move} [{Held}]";
    }
}
