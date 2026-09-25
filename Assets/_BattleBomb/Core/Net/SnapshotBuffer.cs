using System.Collections.Generic;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The guest's recent snapshots in host-frame order, with a pool so 30 a second allocate nothing
    /// once warm. Sampling finds the pair around a (fractional) render frame; outside the buffer it
    /// holds the nearest edge rather than extrapolating a guess.
    /// </summary>
    public sealed class SnapshotBuffer
    {
        /// <summary>About a second of snapshots — far more than the interpolation delay needs.</summary>
        public const int Capacity = 32;

        private readonly List<WorldSnapshot> _snapshots = new List<WorldSnapshot>(Capacity);
        private readonly Stack<WorldSnapshot> _pool = new Stack<WorldSnapshot>();

        public int Count => _snapshots.Count;

        public int NewestFrame => _snapshots.Count > 0 ? _snapshots[_snapshots.Count - 1].HostFrame : -1;

        public WorldSnapshot Rent() => _pool.Count > 0 ? _pool.Pop() : new WorldSnapshot();

        /// <summary>Keeps it in order. A duplicate, or one older than a full buffer's oldest, goes back
        /// to the pool and false is returned.</summary>
        public bool Add(WorldSnapshot snapshot)
        {
            int index = _snapshots.Count;
            while (index > 0 && _snapshots[index - 1].HostFrame >= snapshot.HostFrame)
            {
                if (_snapshots[index - 1].HostFrame == snapshot.HostFrame)
                {
                    Recycle(snapshot);
                    return false;
                }

                index--;
            }

            if (index == 0 && _snapshots.Count >= Capacity)
            {
                Recycle(snapshot);
                return false;
            }

            _snapshots.Insert(index, snapshot);
            while (_snapshots.Count > Capacity)
            {
                Recycle(_snapshots[0]);
                _snapshots.RemoveAt(0);
            }

            return true;
        }

        public bool TrySample(float frame, out WorldSnapshot from, out WorldSnapshot to, out float t)
        {
            from = null;
            to = null;
            t = 0f;
            if (_snapshots.Count == 0)
            {
                return false;
            }

            if (frame <= _snapshots[0].HostFrame)
            {
                from = _snapshots[0];
                to = from;
                return true;
            }

            for (int i = _snapshots.Count - 1; i >= 0; i--)
            {
                if (_snapshots[i].HostFrame > frame)
                {
                    continue;
                }

                from = _snapshots[i];
                if (i + 1 < _snapshots.Count)
                {
                    to = _snapshots[i + 1];
                    t = (frame - from.HostFrame) / (to.HostFrame - from.HostFrame);
                }
                else
                {
                    to = from;
                }

                return true;
            }

            return false;
        }

        /// <summary>Recycles every snapshot older than the one the picture now starts from.</summary>
        public void DiscardBefore(float frame)
        {
            while (_snapshots.Count > 1 && _snapshots[1].HostFrame <= frame)
            {
                Recycle(_snapshots[0]);
                _snapshots.RemoveAt(0);
            }
        }

        private void Recycle(WorldSnapshot snapshot)
        {
            snapshot.Clear();
            _pool.Push(snapshot);
        }
    }
}
