using BattleBomb.Core.Players;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The protocol version and HANDOFF-M8's paper numbers, in one place. Tuned live; a change to
    /// anything that alters the bytes on the wire bumps <see cref="Version"/>.
    /// </summary>
    public static class NetProtocol
    {
        /// <summary>A mismatch refuses the join with a readable reason (planning decision 20).</summary>
        public const int Version = 1;

        /// <summary>Each command packet carries this many of the newest commands, so one lost
        /// packet costs nothing.</summary>
        public const int CommandRedundancy = 4;

        /// <summary>A snapshot every second step: 30 Hz at the 60 Hz simulation.</summary>
        public const int SnapshotEverySteps = 2;

        /// <summary>How far behind the newest snapshot the guest draws: two snapshots and margin.</summary>
        public const int InterpolationDelaySteps = 6;

        /// <summary>The host's input buffer: commands held back to absorb jitter, and the depth past
        /// which it catches up by merging two into one step.</summary>
        public const int InputBufferTarget = 2;
        public const int InputBufferMax = 6;

        public const float KeepAliveSeconds = 0.25f;
        public const float ProblemAfterSeconds = 1f;

        /// <summary>Michael's call (design §1): about ten seconds of silence is a drop.</summary>
        public const float DropAfterSeconds = 10f;

        public const int MaxMessageBytes = 256 * 1024;
        public const int MaxEntities = 256;
        public const int MaxStatuses = 16;
        public const int MaxEvents = 512;
        public const int MaxItemJsonBytes = 8192;

        /// <summary>The local socket transport's port for two editors on one machine.</summary>
        public const int DevPort = 7777;

        /// <summary>
        /// What a remote player's buttons may do on the host: the fight's five verbs (D17 as amended).
        /// Every physical button also carries a menu meaning (D57), and a remote player's menus live on
        /// their own machine — so Pause and the menu buttons never reach the host's screens.
        /// </summary>
        public const CommandButtons RemoteVerbs =
            CommandButtons.Light | CommandButtons.Heavy | CommandButtons.Magic
            | CommandButtons.Equipment | CommandButtons.Jump;
    }
}
