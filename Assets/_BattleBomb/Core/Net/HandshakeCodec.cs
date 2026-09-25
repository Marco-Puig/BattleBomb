using BattleBomb.Core.Chapters;

namespace BattleBomb.Core.Net
{
    public readonly struct HelloMessage
    {
        public readonly int Protocol;
        public readonly string BuildId;

        public HelloMessage(int protocol, string buildId)
        {
            Protocol = protocol;
            BuildId = buildId ?? string.Empty;
        }
    }

    public readonly struct WelcomeMessage
    {
        public readonly int GuestPlayerId;

        public WelcomeMessage(int guestPlayerId)
        {
            GuestPlayerId = guestPlayerId;
        }
    }

    /// <summary>
    /// The run the host just launched, for the guest to load the same one: the chapter by its
    /// authored id, the stage, tier and resume point, each seat's roster index (-1 for nobody),
    /// and which seat is the guest's.
    /// </summary>
    public readonly struct LaunchMessage
    {
        public readonly string ChapterId;
        public readonly int StageIndex;
        public readonly int TierIndex;
        public readonly int ResumeCheckpointArena;
        public readonly int[] RosterPicks;
        public readonly int GuestPlayerId;

        public LaunchMessage(
            string chapterId, int stageIndex, int tierIndex, int resumeCheckpointArena,
            int[] rosterPicks, int guestPlayerId)
        {
            ChapterId = chapterId ?? string.Empty;
            StageIndex = stageIndex;
            TierIndex = tierIndex;
            ResumeCheckpointArena = resumeCheckpointArena;
            RosterPicks = rosterPicks ?? new int[0];
            GuestPlayerId = guestPlayerId;
        }
    }

    /// <summary>The messages that open a session and start a run. Readers take a reader positioned
    /// after the kind byte.</summary>
    public static class HandshakeCodec
    {
        public static void WriteHello(NetWriter writer, in HelloMessage hello)
        {
            writer.WriteByte((byte)NetMessageKind.Hello);
            writer.WriteInt(hello.Protocol);
            writer.WriteString(hello.BuildId);
        }

        public static HelloMessage ReadHello(NetReader reader) =>
            new HelloMessage(reader.ReadInt(), reader.ReadString(128));

        public static void WriteWelcome(NetWriter writer, in WelcomeMessage welcome)
        {
            writer.WriteByte((byte)NetMessageKind.Welcome);
            writer.WriteInt(welcome.GuestPlayerId);
        }

        public static WelcomeMessage ReadWelcome(NetReader reader) => new WelcomeMessage(reader.ReadInt());

        public static void WriteRefuse(NetWriter writer, string reason)
        {
            writer.WriteByte((byte)NetMessageKind.Refuse);
            writer.WriteString(reason);
        }

        public static string ReadRefuse(NetReader reader) => reader.ReadString(512);

        public static void WriteLaunch(NetWriter writer, in LaunchMessage launch)
        {
            writer.WriteByte((byte)NetMessageKind.Launch);
            writer.WriteString(launch.ChapterId);
            writer.WriteInt(launch.StageIndex);
            writer.WriteInt(launch.TierIndex);
            writer.WriteInt(launch.ResumeCheckpointArena);
            writer.WriteCount(launch.RosterPicks.Length, FrontendState.Slots);
            for (int i = 0; i < launch.RosterPicks.Length; i++)
            {
                writer.WriteInt(launch.RosterPicks[i]);
            }

            writer.WriteInt(launch.GuestPlayerId);
        }

        public static LaunchMessage ReadLaunch(NetReader reader)
        {
            string chapter = reader.ReadString(256);
            int stage = reader.ReadInt();
            int tier = reader.ReadInt();
            int resume = reader.ReadInt();
            var picks = new int[reader.ReadCount(FrontendState.Slots)];
            for (int i = 0; i < picks.Length; i++)
            {
                picks[i] = reader.ReadInt();
            }

            return new LaunchMessage(chapter, stage, tier, resume, picks, reader.ReadInt());
        }

        /// <summary>A bare message: its kind is its whole content.</summary>
        public static void WriteBare(NetWriter writer, NetMessageKind kind) => writer.WriteByte((byte)kind);

        /// <summary>Null when the guest may join; otherwise the reason both screens show.</summary>
        public static string CheckHello(in HelloMessage hello, string localBuildId)
        {
            if (hello.Protocol != NetProtocol.Version)
            {
                return $"Different network protocol (host {NetProtocol.Version}, guest {hello.Protocol}). Update both games.";
            }

            if (hello.BuildId != (localBuildId ?? string.Empty))
            {
                return $"Different versions of the game (host {localBuildId}, guest {hello.BuildId}).";
            }

            return null;
        }
    }
}
