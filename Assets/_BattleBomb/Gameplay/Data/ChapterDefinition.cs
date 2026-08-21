using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>An ordered list of stages (D48). When the story arrives, Chapter 1 is one of these.</summary>
    [CreateAssetMenu(menuName = "BattleBomb/Chapter Definition", fileName = "Chapter")]
    public sealed class ChapterDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "chapter";
        [SerializeField] private string _displayName = "Chapter";
        [SerializeField] private StageDefinition[] _stages = new StageDefinition[0];

        public string Id => _id;

        public string DisplayName => _displayName;

        public IReadOnlyList<StageDefinition> Stages => _stages;

        public StageDefinition StageAt(int index) =>
            _stages != null && index >= 0 && index < _stages.Length ? _stages[index] : null;

        public ChapterSpec ToRuntime()
        {
            var stages = new List<StageSpec>(_stages != null ? _stages.Length : 0);
            if (_stages != null)
            {
                for (int i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i] != null)
                    {
                        stages.Add(_stages[i].ToRuntime());
                    }
                }
            }

            return new ChapterSpec(_id, _displayName, stages.ToArray());
        }

        public static ChapterSpec[] ToRuntime(IReadOnlyList<ChapterDefinition> definitions)
        {
            var specs = new List<ChapterSpec>();
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i] != null)
                    {
                        specs.Add(definitions[i].ToRuntime());
                    }
                }
            }

            return specs.ToArray();
        }
    }
}
