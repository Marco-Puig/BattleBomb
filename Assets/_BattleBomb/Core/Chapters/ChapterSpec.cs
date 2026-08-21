using System;

namespace BattleBomb.Core.Chapters
{
    /// <summary>An ordered list of stages and nothing else (D48). Order is the only sequencing
    /// logic there is; when the story arrives, writing Chapter 1 is authoring one of these.</summary>
    public readonly struct ChapterSpec
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly StageSpec[] Stages;

        public ChapterSpec(string id, string displayName, StageSpec[] stages)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Stages = stages ?? Array.Empty<StageSpec>();
        }

        public int StageCount => Stages.Length;
    }
}
