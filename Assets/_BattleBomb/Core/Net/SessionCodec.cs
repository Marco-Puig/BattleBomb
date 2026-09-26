namespace BattleBomb.Core.Net
{
    /// <summary>A moment in the host's run that the guest acts on too. Values are wire format: never renumber, only add.</summary>
    public enum MomentKind : byte
    {
        /// <summary>The chapter is done: the guest's results open and its own save records the credit (D61).</summary>
        ChapterCompleted = 1,

        /// <summary>D52's run moments — the guest writes its own save at each (Task 101). A chest closing needs no
        /// moment: the guest's own chest closes on the guest, which saves there and then.</summary>
        CheckpointReached = 2,
        StageCompleted = 3,
    }

    /// <summary>The host's session moments (D60): the guest follows them and never starts one. Readers take a reader
    /// positioned after the kind byte.</summary>
    public static class SessionCodec
    {
        public static void WriteMoment(NetWriter w, MomentKind moment)
        {
            w.WriteByte((byte)NetMessageKind.Moment);
            w.WriteByte((byte)moment);
        }

        public static MomentKind ReadMoment(NetReader r)
        {
            var moment = (MomentKind)r.ReadByte();
            if (moment < MomentKind.ChapterCompleted || moment > MomentKind.StageCompleted)
            {
                throw new NetFormatException($"A moment of kind {(byte)moment}.");
            }

            return moment;
        }
    }
}
