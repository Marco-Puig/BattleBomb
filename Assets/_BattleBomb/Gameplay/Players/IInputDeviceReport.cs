using BattleBomb.Core.Players;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// What a device-backed command source can say about the device behind it — for button
    /// prompts, and for the front door handing out seats (D57). A source that is not a device (a
    /// test script, a replay, later a remote peer) simply does not implement it.
    /// </summary>
    public interface IInputDeviceReport
    {
        InputFamily Family { get; }

        /// <summary>The Input System id of the device this player last pressed a button on, or
        /// <see cref="SeatAssignment.NoDevice"/>. Never a guess — the front door seats players by it.</summary>
        int LastDeviceId { get; }
    }
}
