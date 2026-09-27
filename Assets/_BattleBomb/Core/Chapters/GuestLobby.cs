namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// The guest's front door while connected (D59): this machine's one player picks a hero from their own save and
    /// readies; the host chooses the chapter and launches both. Back un-readies, and from not-ready leaves the game.
    /// Built again from what it was when the front door comes back after a match.
    /// </summary>
    public sealed class GuestLobby
    {
        private readonly int _rosterCount;

        public GuestLobby(int rosterCount, int pick, bool ready)
        {
            _rosterCount = rosterCount < 1 ? 1 : rosterCount;
            Pick = Wrap(pick);
            Ready = ready;
        }

        public int Pick { get; private set; }

        public bool Ready { get; private set; }

        /// <summary>Back from not-ready: the player wants out of the host's game.</summary>
        public bool LeaveRequested { get; private set; }

        public void MovePick(int delta)
        {
            if (Ready || delta == 0)
            {
                return;
            }

            Pick = Wrap(Pick + delta);
        }

        public void Confirm() => Ready = true;

        public void Back()
        {
            if (Ready)
            {
                Ready = false;
            }
            else
            {
                LeaveRequested = true;
            }
        }

        private int Wrap(int pick) => ((pick % _rosterCount) + _rosterCount) % _rosterCount;
    }
}
