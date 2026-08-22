using System;
using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// "The game" half of a stage (D48), authored. Enemies are a roster on the asset and
    /// waves point into it by index, so Core never names an enemy asset; the runner maps the
    /// index back to the <see cref="EnemyDefinition"/> at spawn time.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleBomb/Stage Definition", fileName = "Stage")]
    public sealed class StageDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class WaveAuthoring
        {
            [Tooltip("Index into this stage's enemy roster.")]
            public int EnemyIndex;
            public int Count = 3;

            [Tooltip("Steps after the arena starts — or, if it waits, after the previous wave died.")]
            public int DelaySteps;
            public bool WaitsForPreviousWave;
        }

        /// <summary>
        /// What follows an arena. One authored choice rather than two checkboxes, because
        /// <see cref="ArenaSpec"/> forces a checkpoint whenever there is a shopkeeper — with
        /// two booleans the inspector could show "shopkeeper yes, checkpoint no", which is a
        /// state that cannot survive <c>ToRuntime</c>. Michael hand-authors every real chapter
        /// (D48), so the authoring surface must not be able to disagree with the struct.
        /// </summary>
        public enum ArenaEnding
        {
            /// <summary>Straight on to the next fight.</summary>
            None = 0,

            /// <summary>A checkpoint room with a chest (D42).</summary>
            Checkpoint = 1,

            /// <summary>A checkpoint room with a chest and a shopkeeper (D42/D43).</summary>
            CheckpointWithShopkeeper = 2,
        }

        [Serializable]
        public sealed class ArenaAuthoring
        {
            public WaveAuthoring[] Waves = new WaveAuthoring[0];

            [Tooltip("What sits between this arena and the next.")]
            public ArenaEnding Ending;
        }

        [SerializeField] private string _id = "stage";
        [SerializeField] private string _displayName = "Stage";

        [Tooltip("The additive scene holding this stage's geometry and markers, by name.")]
        [SerializeField] private string _geometryScene = "FixtureStage1";

        [Tooltip("Every enemy this stage can spawn; waves index into this.")]
        [SerializeField] private EnemyDefinition[] _enemies = new EnemyDefinition[0];

        [SerializeField] private ArenaAuthoring[] _arenas = new ArenaAuthoring[0];

        [Header("The three numbers M6 left at scene level")]
        [Tooltip("D36's level stamp on drops and shop stock.")]
        [SerializeField] private int _levelStamp = 1;

        [Tooltip("D23's story-progress multiplier on drop quality.")]
        [SerializeField] private float _lootProgress = 2f;

        [Tooltip("D41's climate: multiplies elemental damage from every side.")]
        [SerializeField] private ElementMultiplierSpec[] _climate = new ElementMultiplierSpec[0];

        public string Id => _id;

        public IReadOnlyList<EnemyDefinition> Enemies => _enemies;

        public EnemyDefinition EnemyAt(int index) =>
            _enemies != null && index >= 0 && index < _enemies.Length ? _enemies[index] : null;

        public StageSpec ToRuntime()
        {
            var arenas = new ArenaSpec[_arenas != null ? _arenas.Length : 0];
            for (int i = 0; i < arenas.Length; i++)
            {
                ArenaAuthoring arena = _arenas[i];
                var waves = new WaveSpec[arena.Waves != null ? arena.Waves.Length : 0];
                for (int w = 0; w < waves.Length; w++)
                {
                    WaveAuthoring wave = arena.Waves[w];
                    waves[w] = new WaveSpec(wave.EnemyIndex, wave.Count, wave.DelaySteps, wave.WaitsForPreviousWave);
                }

                arenas[i] = new ArenaSpec(
                    waves,
                    arena.Ending != ArenaEnding.None,
                    arena.Ending == ArenaEnding.CheckpointWithShopkeeper);
            }

            return new StageSpec(
                _id, _displayName, _geometryScene, arenas, _levelStamp, _lootProgress,
                ElementMultiplierSpec.ToTable(_climate));
        }
    }
}
