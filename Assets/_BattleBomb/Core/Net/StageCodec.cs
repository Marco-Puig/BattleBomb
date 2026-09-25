namespace BattleBomb.Core.Net
{
    /// <summary>A stage the guest must load: the launch stage (unload everything first, then load
    /// with its resume point) or the next stage behind the airlock (load beside the current one,
    /// shifted so its first arena begins at the host's exit line).</summary>
    public readonly struct LoadStageMessage
    {
        public readonly int StageIndex;
        public readonly bool IsLaunch;
        public readonly float FirstArenaMinX;
        public readonly int ResumeCheckpointArena;

        public LoadStageMessage(int stageIndex, bool isLaunch, float firstArenaMinX, int resumeCheckpointArena)
        {
            StageIndex = stageIndex;
            IsLaunch = isLaunch;
            FirstArenaMinX = firstArenaMinX;
            ResumeCheckpointArena = resumeCheckpointArena;
        }
    }

    public static class StageCodec
    {
        public static void WriteLoad(NetWriter w, in LoadStageMessage load)
        {
            w.WriteByte((byte)NetMessageKind.LoadStage);
            w.WriteInt(load.StageIndex);
            w.WriteBool(load.IsLaunch);
            w.WriteFloat(load.FirstArenaMinX);
            w.WriteInt(load.ResumeCheckpointArena);
        }

        public static LoadStageMessage ReadLoad(NetReader r) =>
            new LoadStageMessage(r.ReadInt(), r.ReadBool(), r.ReadFloat(), r.ReadInt());

        public static void WriteReady(NetWriter w, int stageIndex)
        {
            w.WriteByte((byte)NetMessageKind.StageReady);
            w.WriteInt(stageIndex);
        }

        /// <summary>The stage behind the airlock became the stage in host step <paramref name="hostFrame"/> —
        /// so the guest hands over when its picture reaches that step, whatever snapshots went missing.</summary>
        public static void WriteHandOver(NetWriter w, int stageIndex, int hostFrame)
        {
            w.WriteByte((byte)NetMessageKind.HandOver);
            w.WriteInt(stageIndex);
            w.WriteInt(hostFrame);
        }

        /// <summary>Reads a <c>StageReady</c> — or just the stage of a <c>HandOver</c> — positioned after its kind byte.</summary>
        public static int ReadStage(NetReader r) => r.ReadInt();

        /// <summary>Reads a <c>HandOver</c> positioned after its kind byte.</summary>
        public static int ReadHandOver(NetReader r, out int hostFrame)
        {
            int stage = r.ReadInt();
            hostFrame = r.ReadInt();
            return stage;
        }
    }
}
