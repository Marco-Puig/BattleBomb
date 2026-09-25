using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Turns a remote player's arrivals into exactly one <see cref="PlayerCommand"/> per host step
    /// (HANDOFF-M8 paper numbers): waits until <c>target</c> commands are buffered, then plays one
    /// per step; if it runs dry it repeats what was held and never a press; if it backs up past
    /// <c>max</c> it plays two in one step, merging their edges so neither press is lost. Edges are
    /// always re-derived from consecutive held states, so a redundant copy cannot press twice.
    /// </summary>
    public sealed class RemoteCommandStream
    {
        private readonly int _target;
        private readonly int _max;
        private InputBuffer _buffer = new InputBuffer();
        private CommandButtons _held;
        private Vector2 _move;
        private bool _primed;

        public RemoteCommandStream(int target = NetProtocol.InputBufferTarget, int max = NetProtocol.InputBufferMax)
        {
            _target = Mathf.Max(1, target);
            _max = Mathf.Max(_target, max);
        }

        public int Buffered => _buffer.Count;

        /// <summary>The sender's frame of the newest command played — what a snapshot acknowledges.</summary>
        public int LastConsumedFrame => _buffer.LastTakenFrame;

        public int StarvedSteps { get; private set; }

        public int MergedSteps { get; private set; }

        public void Receive(in WireCommand command) => _buffer.Add(command);

        /// <summary>Forgets the sender entirely — a disconnect, or a rejoin whose frames start again at zero.</summary>
        public void Release()
        {
            _buffer = new InputBuffer();
            _held = CommandButtons.None;
            _move = Vector2.zero;
            _primed = false;
        }

        public PlayerCommand Next(int hostFrame)
        {
            if (!_primed)
            {
                if (_buffer.Count < _target)
                {
                    return PlayerCommand.Idle(hostFrame);
                }

                _primed = true;
            }

            if (!_buffer.TryTake(out WireCommand first))
            {
                StarvedSteps++;
                return new PlayerCommand(hostFrame, _move, _held, CommandButtons.None, CommandButtons.None);
            }

            CommandButtons previous = _held;
            CommandButtons held = first.Held;
            Vector2 move = first.Move;
            CommandButtons pressed = held & ~previous;
            CommandButtons released = previous & ~held;

            if (_buffer.Count > _max && _buffer.TryTake(out WireCommand second))
            {
                MergedSteps++;
                pressed |= second.Held & ~held;
                released |= held & ~second.Held;
                held = second.Held;
                move = second.Move;
            }

            _held = held;
            _move = move;
            return new PlayerCommand(hostFrame, move, held, pressed, released);
        }
    }
}
