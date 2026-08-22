using System;
using System.Collections.Generic;

namespace BattleBomb.Core.Chapters
{
    /// <summary>One fight: its waves in order, and whether a checkpoint room (with or without
    /// a shopkeeper, D42/D43) follows it before the next arena.</summary>
    public readonly struct ArenaSpec
    {
        public readonly WaveSpec[] Waves;
        public readonly bool CheckpointAfter;
        public readonly bool ShopkeeperAfter;

        public ArenaSpec(WaveSpec[] waves, bool checkpointAfter, bool shopkeeperAfter)
        {
            Waves = waves ?? Array.Empty<WaveSpec>();
            CheckpointAfter = checkpointAfter || shopkeeperAfter;
            ShopkeeperAfter = shopkeeperAfter;
        }

        public int WaveCount => Waves.Length;
    }
}
