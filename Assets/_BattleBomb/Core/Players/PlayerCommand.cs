using UnityEngine;

namespace BattleBomb.Core.Players
{
    /// <summary>
    /// One player's intent for one simulation step. The simulation consumes only this — never input
    /// devices — so a local pad, a replay, and (later) a remote peer are indistinguishable to it (§4).
    /// </summary>
    public readonly struct PlayerCommand
    {
        /// <summary>Simulation step this command applies to.</summary>
        public readonly int Frame;

        /// <summary>Desired movement, clamped to unit length. X is lateral, Y is the depth axis (D13).</summary>
        public readonly Vector2 Move;

        /// <summary>Buttons held during this step.</summary>
        public readonly CommandButtons Held;

        /// <summary>Buttons that became held on this step.</summary>
        public readonly CommandButtons Pressed;

        /// <summary>Buttons that stopped being held on this step.</summary>
        public readonly CommandButtons Released;

        public PlayerCommand(int frame, Vector2 move, CommandButtons held, CommandButtons pressed, CommandButtons released)
        {
            Frame = frame;
            Move = move;
            Held = held;
            Pressed = pressed;
            Released = released;
        }

        /// <summary>
        /// Builds a command from the raw state of one step plus the buttons held on the previous one,
        /// deriving the press and release edges. Pure, so edge detection is unit-testable and identical
        /// for every kind of command source.
        /// </summary>
        public static PlayerCommand FromState(int frame, Vector2 move, CommandButtons held, CommandButtons previouslyHeld)
        {
            if (move.sqrMagnitude > 1f)
            {
                move = move.normalized;
            }

            return new PlayerCommand(
                frame,
                move,
                held,
                held & ~previouslyHeld,
                previouslyHeld & ~held);
        }

        /// <summary>A neutral command — no movement, no buttons. What a disconnected source contributes.</summary>
        public static PlayerCommand Idle(int frame) =>
            new PlayerCommand(frame, Vector2.zero, CommandButtons.None, CommandButtons.None, CommandButtons.None);

        public bool IsHeld(CommandButtons button) => button != CommandButtons.None && (Held & button) == button;

        public bool WasPressed(CommandButtons button) => button != CommandButtons.None && (Pressed & button) == button;

        public bool WasReleased(CommandButtons button) => button != CommandButtons.None && (Released & button) == button;
    }
}
