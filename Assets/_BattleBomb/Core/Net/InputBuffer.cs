using System.Collections.Generic;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// One remote player's commands waiting to be played: ordered by the sender's frame, each frame
    /// kept once however many redundant copies arrive, and nothing older than what was already
    /// taken. Pure policy on a list, so every edge of it is an EditMode test.
    /// </summary>
    public sealed class InputBuffer
    {
        /// <summary>A second of commands. A sender that floods past this loses its oldest.</summary>
        public const int MaxQueued = 60;

        private readonly List<WireCommand> _queue = new List<WireCommand>();

        public int Count => _queue.Count;

        public int LastTakenFrame { get; private set; } = -1;

        public bool Add(in WireCommand command)
        {
            if (command.Frame <= LastTakenFrame)
            {
                return false;
            }

            int index = _queue.Count;
            while (index > 0 && _queue[index - 1].Frame >= command.Frame)
            {
                if (_queue[index - 1].Frame == command.Frame)
                {
                    return false;
                }

                index--;
            }

            _queue.Insert(index, command);
            if (_queue.Count > MaxQueued)
            {
                TryTake(out _);
            }

            return true;
        }

        public bool TryTake(out WireCommand command)
        {
            if (_queue.Count == 0)
            {
                command = default;
                return false;
            }

            command = _queue[0];
            _queue.RemoveAt(0);
            LastTakenFrame = command.Frame;
            return true;
        }
    }
}
