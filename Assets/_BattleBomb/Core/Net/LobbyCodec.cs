using BattleBomb.Core.Saves;

namespace BattleBomb.Core.Net
{
    /// <summary>A guest's choice at character select (D59): a hero by roster index, ready or not, and — once ready —
    /// their own save cut down to that hero (<see cref="SaveMapper.ParticipantFrom"/>).</summary>
    public readonly struct LobbyPick
    {
        public readonly int RosterIndex;
        public readonly bool Ready;
        public readonly SaveGame Brought;

        public LobbyPick(int rosterIndex, bool ready, SaveGame brought)
        {
            // Ready means brought (D61): the host restores the guest from it, and a guest restored from nothing would
            // have an empty stash saved over its real file.
            RosterIndex = rosterIndex;
            Ready = ready && brought != null;
            Brought = Ready ? brought : null;
        }
    }

    /// <summary>What the guest's lobby shows of the host (D59): where the host's front door is, the host's hero and
    /// readiness, and whether the host is already playing — a guest who joins then waits for a checkpoint room.</summary>
    public readonly struct LobbyState
    {
        public readonly Chapters.FrontendScreen HostScreen;
        public readonly int HostPick;
        public readonly bool HostReady;
        public readonly bool InMatch;

        public LobbyState(Chapters.FrontendScreen hostScreen, int hostPick, bool hostReady, bool inMatch)
        {
            HostScreen = hostScreen;
            HostPick = hostPick;
            HostReady = hostReady;
            InMatch = inMatch;
        }

        public bool Same(in LobbyState other) =>
            HostScreen == other.HostScreen && HostPick == other.HostPick
            && HostReady == other.HostReady && InMatch == other.InMatch;
    }

    /// <summary>The lobby's messages (HANDOFF-M8 Tasks 101–102). Readers take a reader positioned after the kind byte.</summary>
    public static class LobbyCodec
    {
        public static void WritePick(NetWriter w, in LobbyPick pick)
        {
            w.WriteByte((byte)NetMessageKind.LobbyPick);
            w.WriteInt(pick.RosterIndex);
            w.WriteBool(pick.Ready);
            w.WriteBool(pick.Brought != null);
            if (pick.Brought != null)
            {
                ParticipantCodec.WriteState(w, pick.Brought);
            }
        }

        public static LobbyPick ReadPick(NetReader r)
        {
            int roster = r.ReadInt();
            bool ready = r.ReadBool();
            SaveGame brought = r.ReadBool() ? ParticipantCodec.ReadState(r) : null;
            return new LobbyPick(roster, ready, brought);
        }

        public static void WriteLobby(NetWriter w, in LobbyState state)
        {
            w.WriteByte((byte)NetMessageKind.LobbyState);
            w.WriteByte((byte)state.HostScreen);
            w.WriteInt(state.HostPick);
            w.WriteBool(state.HostReady);
            w.WriteBool(state.InMatch);
        }

        public static LobbyState ReadLobby(NetReader r)
        {
            var screen = (Chapters.FrontendScreen)r.ReadByte();
            if (screen > Chapters.FrontendScreen.Launching)
            {
                throw new NetFormatException($"A front-door screen of {(byte)screen}.");
            }

            return new LobbyState(screen, r.ReadInt(), r.ReadBool(), r.ReadBool());
        }
    }
}
