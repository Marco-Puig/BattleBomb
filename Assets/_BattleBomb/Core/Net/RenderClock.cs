using System;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The host frame the guest draws: <c>delay</c> steps behind the newest snapshot (HANDOFF-M8 paper
    /// numbers). It advances one step per local step and eases the gap back toward the delay by at
    /// most a quarter step per step — invisible — so jitter never shows as a stutter; a long stall
    /// snaps instead of crawling. It never draws past the newest snapshot.
    /// </summary>
    public sealed class RenderClock
    {
        public const float SnapAfterSteps = 30f;
        public const float CorrectionRate = 0.05f;
        public const float MaxCorrection = 0.25f;

        private readonly int _delay;

        public RenderClock(int delaySteps = NetProtocol.InterpolationDelaySteps)
        {
            _delay = Math.Max(0, delaySteps);
            Frame = -1f;
        }

        public float Frame { get; private set; }

        public bool IsRunning => Frame >= 0f;

        public float Advance(int newestHostFrame)
        {
            float target = newestHostFrame - _delay;
            if (!IsRunning || Mathf.Abs(target - Frame) > SnapAfterSteps)
            {
                Frame = Mathf.Max(0f, target);
                return Frame;
            }

            // One step forward first, then the correction measured from where that lands — measured
            // before the step, an in-step clock would read a one-step gap every step and creep ahead.
            Frame += 1f;
            Frame += Mathf.Clamp((target - Frame) * CorrectionRate, -MaxCorrection, MaxCorrection);
            if (Frame > newestHostFrame)
            {
                Frame = newestHostFrame;
            }

            return Frame;
        }

        public void Reset() => Frame = -1f;
    }
}
