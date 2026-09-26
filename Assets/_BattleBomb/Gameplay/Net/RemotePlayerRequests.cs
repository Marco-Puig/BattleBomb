using System;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Items;
using BattleBomb.Platform.Net;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The guest's own player's menu actions (D61): each crosses to the host, runs there, and is answered a
    /// round trip later — after the host's copy of the bag, which travels first on the same channel. One at
    /// a time: while one is out, <see cref="Pending"/> holds the screen's hands. Closing is never held up,
    /// and lets go of whatever was out; nor is a setting nobody waits on.
    /// </summary>
    internal sealed class RemotePlayerRequests : IPlayerRequests
    {
        private readonly NetWriter _writer = new NetWriter(64);
        private readonly NetSession _net;
        private readonly Func<PlayerInventory> _bag;
        private int _nextSequence = 1;
        private int _waitingFor = -1;
        private Action<RequestOutcome> _answered;

        internal RemotePlayerRequests(NetSession net, Func<PlayerInventory> bag)
        {
            _net = net;
            _bag = bag;
        }

        public bool Pending => _waitingFor >= 0;

        public void Send(PlayerRequest request, Action<RequestOutcome> answered)
        {
            // Only an action someone waits on is held to one at a time: a close, or a setting sent with
            // nobody to hear its answer, goes straight out.
            bool close = request.Kind == PlayerRequestKind.CloseScreen;
            bool waits = !close && answered != null;
            if (Pending && waits)
            {
                answered(RequestOutcome.No(RequestRefusal.Busy));
                return;
            }

            PlayerInventory bag = _bag();
            PlayerRequest stamped = request
                .WithSequence(_nextSequence++)
                .WithRevision(bag != null ? bag.Inventory.Sack.Revision : 0);
            _writer.Reset();
            RequestCodec.WriteRequest(_writer, stamped);
            _net.Send(NetChannel.Reliable, _writer);

            if (close)
            {
                // The screen is gone, so nothing is left to hear what is still on the wire: its answer,
                // if it comes, is dropped by its sequence.
                Abandon();
            }
            else if (waits)
            {
                _waitingFor = stamped.Sequence;
                _answered = answered;
            }
        }

        /// <summary>The host's answer. An answer to anything but the one being waited for — a close, or one
        /// abandoned — is dropped.</summary>
        internal void Answer(int sequence, in RequestOutcome outcome)
        {
            if (sequence != _waitingFor)
            {
                return;
            }

            Action<RequestOutcome> answered = _answered;
            _waitingFor = -1;
            _answered = null;
            answered?.Invoke(outcome);
        }

        /// <summary>The match is over: whatever was out will never be answered.</summary>
        internal void Abandon()
        {
            _waitingFor = -1;
            _answered = null;
        }
    }
}
