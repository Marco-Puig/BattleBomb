using BattleBomb.Core.Players;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The protocol version and HANDOFF-M8's paper numbers, in one place. Tuned live; a change to
    /// anything that alters the bytes on the wire bumps <see cref="Version"/>.
    /// 3: menu requests (Plan 2, Task 97).
    /// 4: the guest's screens, racks and inventory (Task 99).
    /// </summary>
    public static class NetProtocol
    {
        /// <summary>A mismatch refuses the join with a readable reason (planning decision 20).</summary>
        public const int Version = 4;

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

        /// <summary>Steps the buffer may sit above its target before one step merges two, draining
        /// the lag a stall left behind — slowly, so jitter keeps its cushion. Tuned in Task 96.</summary>
        public const int InputBufferDrainSteps = 30;

        /// <summary>Steps a silent remote player's last held input is repeated before their body lets
        /// go: a quarter of a second, well inside the one-second problem banner. Tuned in Task 96.</summary>
        public const int StarvedRepeatSteps = 15;

        /// <summary>How far a replica may move between two drawn steps and still be drawn sliding: past
        /// it, it was a teleport — a respawn, an airlock — and is drawn as one.</summary>
        public const float ReplicaTeleportDistance = 3f;

        public const float KeepAliveSeconds = 0.25f;
        public const float ProblemAfterSeconds = 1f;

        /// <summary>Michael's call (design §1): about ten seconds of silence is a drop.</summary>
        public const float DropAfterSeconds = 10f;

        public const int MaxMessageBytes = 256 * 1024;
        public const int MaxEntities = 256;
        public const int MaxStatuses = 16;
        public const int MaxEvents = 512;
        public const int MaxItemJsonBytes = 8192;

        /// <summary>A player's inventory, deflated. A full 200-stack sack of four-affix gear is ~15 KB.</summary>
        public const int MaxParticipantBytes = 192 * 1024;

        /// <summary>What a participant may inflate to — a decompression bomb stops here, not at memory's end.</summary>
        public const int MaxParticipantJsonBytes = 4 * 1024 * 1024;

        /// <summary>Pieces a rack may carry on the wire; the simulation rolls four (D43).</summary>
        public const int MaxRack = 16;

        /// <summary>
        /// Steps between two sends of one player's inventory when nothing forces it: a kill's XP changes the
        /// bag's owner every few seconds in a fight, and a quarter of a second is quick enough for an XP bar.
        /// A request's answer, a screen opening and an autosave send it at once.
        /// </summary>
        public const int ParticipantMinSteps = 15;

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
