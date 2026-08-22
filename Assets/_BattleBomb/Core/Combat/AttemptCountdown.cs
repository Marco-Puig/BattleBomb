using System.Collections.Generic;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// The attempt-over beat: while every player is down the countdown runs; anyone standing back
    /// up resets it. Its trigger is the caller's cue for a sandbox reset — deliberately a
    /// placeholder, because the real run lifecycle is mode-owned and arrives with M7 (D4).
    /// </summary>
    public readonly struct AttemptCountdown
    {
        public readonly int StepsAllDown;

        public AttemptCountdown(int stepsAllDown)
        {
            StepsAllDown = stepsAllDown > 0 ? stepsAllDown : 0;
        }

        public AttemptCountdown Step(bool allDown) =>
            allDown ? new AttemptCountdown(StepsAllDown + 1) : new AttemptCountdown(0);

        public bool Triggers(int beatSteps) => StepsAllDown >= beatSteps;

        /// <summary>True only when players exist and every one of them is down.</summary>
        public static bool AllDown(IReadOnlyList<bool> downed)
        {
            if (downed == null || downed.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < downed.Count; i++)
            {
                if (!downed[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
