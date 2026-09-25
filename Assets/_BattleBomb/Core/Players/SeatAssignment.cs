using BattleBomb.Core.Chapters;

namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Which input devices belong to which couch seat (D57). One rule: <b>Player 2 owns the
    /// device they joined with; Player 1 owns everything else</b> — so a solo player can pick up
    /// any controller, or the keyboard, at any moment, and that is all "switching" is.
    ///
    /// The exception is character select before Player 2 has joined. There Player 1 is held to
    /// the device they came into it on, and every other device belongs to the empty seat, so a
    /// press on one is a join rather than Player 1 readying up.
    ///
    /// Devices are the Input System's <c>deviceId</c>s, carried as plain ints so Core never sees
    /// a device (rules 1 and 3).
    /// </summary>
    public sealed class SeatAssignment
    {
        public const int NoDevice = -1;

        /// <summary>The device Player 1 came into character select on — the one that pressed to
        /// get there. Only consulted while Player 2 could still join.</summary>
        public int FirstSeatHome { get; private set; } = NoDevice;

        public int SecondSeatDevice { get; private set; } = NoDevice;

        /// <summary>Character select is up, so an empty second seat listens for a join.</summary>
        public bool Joining { get; private set; }

        public bool Owns(int seat, int deviceId)
        {
            if (deviceId == NoDevice)
            {
                return false;
            }

            if (seat == 1)
            {
                if (SecondSeatDevice != NoDevice)
                {
                    return deviceId == SecondSeatDevice;
                }

                return Joining && FirstSeatHome != NoDevice && deviceId != FirstSeatHome;
            }

            if (seat == 0)
            {
                if (Joining && SecondSeatDevice == NoDevice && FirstSeatHome != NoDevice)
                {
                    return deviceId == FirstSeatHome;
                }

                return deviceId != SecondSeatDevice;
            }

            return false;
        }

        /// <summary>
        /// Brings the seats in line with the front door. Called every frame it runs, with the
        /// device each seat last pressed, so it is declarative: whatever the front door's state
        /// says, the seats agree with by the next frame. The title forgets everything — anyone
        /// can start; the first frame past it homes Player 1 on the device that did.
        /// </summary>
        public void Follow(
            FrontendScreen screen, bool secondJoined, int firstSeatLastDevice, int secondSeatLastDevice)
        {
            if (screen == FrontendScreen.Title)
            {
                FirstSeatHome = NoDevice;
                SecondSeatDevice = NoDevice;
                Joining = false;
                return;
            }

            // Re-taken whenever the seat is not listening for a join, so on the frame character
            // select opens it is the device that pressed to get there — from the title or back
            // from chapter select — and dropped if that device goes, so a solo player who changed
            // hands is never locked out. Joining still holds last frame's value here.
            bool listening = Joining && SecondSeatDevice == NoDevice && FirstSeatHome != NoDevice;
            if (!listening || firstSeatLastDevice == NoDevice)
            {
                FirstSeatHome = firstSeatLastDevice;
            }

            Joining = screen == FrontendScreen.Characters;

            if (!secondJoined)
            {
                SecondSeatDevice = NoDevice;
                return;
            }

            if (SecondSeatDevice == NoDevice
                && secondSeatLastDevice != NoDevice
                && secondSeatLastDevice != FirstSeatHome)
            {
                SecondSeatDevice = secondSeatLastDevice;
            }
        }

        /// <summary>
        /// No front door ran — the Gameplay scene was opened on its own. Player 2 stands in on the
        /// first controller and Player 1 has the rest, which is how that scene has always behaved.
        /// </summary>
        public void StandIn(int firstGamepad)
        {
            FirstSeatHome = NoDevice;
            Joining = false;
            SecondSeatDevice = firstGamepad;
        }
    }
}
