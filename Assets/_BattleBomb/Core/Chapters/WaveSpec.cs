using UnityEngine;

namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// One spawn wave inside an arena (D48). <see cref="EnemyIndex"/> indexes the stage's
    /// enemy roster — Core never names an enemy asset. A wave either comes on a timer from
    /// the arena's start or waits for the wave before it to die and then counts its delay.
    /// </summary>
    public readonly struct WaveSpec
    {
        public readonly int EnemyIndex;
        public readonly int Count;
        public readonly int DelaySteps;
        public readonly bool WaitsForPreviousWave;

        public WaveSpec(int enemyIndex, int count, int delaySteps, bool waitsForPreviousWave)
        {
            EnemyIndex = Mathf.Max(0, enemyIndex);
            Count = Mathf.Max(1, count);
            DelaySteps = Mathf.Max(0, delaySteps);
            WaitsForPreviousWave = waitsForPreviousWave;
        }
    }
}
