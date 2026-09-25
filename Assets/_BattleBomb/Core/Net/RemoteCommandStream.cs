using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Turns a remote player's arrivals into exactly one <see cref="PlayerCommand"/> per host step
    /// (HANDOFF-M8 paper numbers): waits until <c>target</c> commands are buffered, then plays one
    /// per step. Past <c>max</c> deep it plays two in one step, merging their edges so neither press
    /// is lost; held above <c>target</c> for <see cref="NetProtocol.InputBufferDrainSteps"/> it merges
    /// one step, so the lag a stall added drains away. If it runs dry it repeats what was held and
    /// never a press, then lets go after <see cref="NetProtocol.StarvedRepeatSteps"/>. Edges are
    /// always re-derived from consecutive held states, so a redundant copy cannot press twice, and a
    /// step sampled twice — a paused host's menus — consumes nothing the second time.
    /// </summary>
    public sealed class RemoteCommandStream
    {
        private readonly int _target;
        private readonly int _max;
        private InputBuffer _buffer = new InputBuffer();
        private CommandButtons _held;
        private Vector2 _move;
        private bool _primed;
        private bool _letGo;
        private bool _resampled;
        private int _lastHostFrame = int.MinValue;
        private int _stepsAboveTarget;
        private int _starvedRun;

        public RemoteCommandStream(int target = NetProtocol.InputBufferTarget, int max = NetProtocol.InputBufferMax)
        {
            _target = Mathf.Max(1, target);
            _max = Mathf.Max(_target, max);
        }

        public int Buffered => _buffer.Count;

        /// <summary>The sender's frame of the newest command consumed — played, merged into another step,
        /// or skipped as stale — which is what a snapshot acknowledges. Skipped counts on purpose: the
        /// host's world will never take such a command later, so the guest must not replay it either
        /// (Plan 3's replay point).</summary>
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
            _letGo = false;
            _resampled = false;
            _lastHostFrame = int.MinValue;
            _stepsAboveTarget = 0;
            _starvedRun = 0;
        }

        public PlayerCommand Next(int hostFrame)
        {
            if (hostFrame == _lastHostFrame)
            {
                // The same step sampled again — a paused host's menus sample every frame. Nothing is
                // consumed, and nothing is pressed a second time.
                _resampled = true;
                return Unchanged(hostFrame);
            }

            _lastHostFrame = hostFrame;
            if (_resampled)
            {
                // The host was paused while the guest kept sending. What piled up was pressed at a
                // world that was not running, so it is dropped rather than replayed at double speed.
                _resampled = false;
                while (_buffer.Count > _target)
                {
                    _buffer.TryTake(out _);
                }
            }

            if (!_primed)
            {
                if (_buffer.Count < _target)
                {
                    return PlayerCommand.Idle(hostFrame);
                }

                _primed = true;
            }

            int depth = _buffer.Count;
            if (!_buffer.TryTake(out WireCommand first))
            {
                StarvedSteps++;
                _stepsAboveTarget = 0;
                if (_letGo || ++_starvedRun <= NetProtocol.StarvedRepeatSteps)
                {
                    return Unchanged(hostFrame);
                }

                // Silent past the cap: the body lets go rather than running on with the last stick it
                // heard. What was held stays the base for edges, so a button still held when the guest
                // comes back is not pressed a second time.
                _letGo = true;
                return new PlayerCommand(hostFrame, Vector2.zero, CommandButtons.None, CommandButtons.None, _held);
            }

            _starvedRun = 0;
            _letGo = false;
            _stepsAboveTarget = depth > _target ? _stepsAboveTarget + 1 : 0;
            bool catchUp = depth > _max || _stepsAboveTarget >= NetProtocol.InputBufferDrainSteps;

            CommandButtons previous = _held;
            CommandButtons held = first.Held;
            Vector2 move = first.Move;
            CommandButtons pressed = held & ~previous;
            CommandButtons released = previous & ~held;

            if (catchUp && _buffer.TryTake(out WireCommand second))
            {
                MergedSteps++;
                _stepsAboveTarget = 0;
                pressed |= second.Held & ~held;
                released |= held & ~second.Held;
                held = second.Held;
                move = second.Move;
            }

            _held = held;
            _move = move;
            return new PlayerCommand(hostFrame, move, held, pressed, released);
        }

        /// <summary>What the body is doing now, with no new edges.</summary>
        private PlayerCommand Unchanged(int hostFrame) => _letGo
            ? new PlayerCommand(hostFrame, Vector2.zero, CommandButtons.None, CommandButtons.None, CommandButtons.None)
            : new PlayerCommand(hostFrame, _move, _held, CommandButtons.None, CommandButtons.None);
    }
}
