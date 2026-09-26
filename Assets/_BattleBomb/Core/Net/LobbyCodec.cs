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
            RosterIndex = rosterIndex;
            Ready = ready;
            Brought = ready ? brought : null;
        }
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
    }
}
