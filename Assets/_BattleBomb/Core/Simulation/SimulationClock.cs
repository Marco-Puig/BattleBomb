namespace BattleBomb.Core.Simulation
{
    /// <summary>
    /// Turns real elapsed time into a whole number of fixed simulation steps. The simulation advances
    /// only on these steps, so it is deterministic and frame-rate independent; presentation smooths
    /// between them using <see cref="Alpha"/> (§4, D10).
    /// </summary>
    /// <remarks>
    /// Pure and Unity-free — it is handed a delta, it never reads <c>Time</c> (§2).
    /// </remarks>
    public sealed class SimulationClock
    {
        public const int DefaultStepsPerSecond = 60;
        public const int DefaultMaxStepsPerFrame = 5;

        private readonly int _maxStepsPerFrame;
        private float _accumulator;
        private int _stepsAvailable;

        public SimulationClock(int stepsPerSecond = DefaultStepsPerSecond, int maxStepsPerFrame = DefaultMaxStepsPerFrame)
        {
            if (stepsPerSecond <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(stepsPerSecond));
            }

            if (maxStepsPerFrame <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(maxStepsPerFrame));
            }

            StepsPerSecond = stepsPerSecond;
            StepDuration = 1f / stepsPerSecond;
            _maxStepsPerFrame = maxStepsPerFrame;
        }

        public int StepsPerSecond { get; }

        /// <summary>Seconds one simulation step represents.</summary>
        public float StepDuration { get; }

        /// <summary>Index of the next step to be run. Starts at zero and only ever increases.</summary>
        public int Frame { get; private set; }

        /// <summary>
        /// How far into the next step the leftover time sits, 0–1. Presentation interpolates by this
        /// between the previous and current simulation state.
        /// </summary>
        public float Alpha => _accumulator / StepDuration;

        /// <summary>
        /// Banks elapsed real time. Backlog is capped at <c>maxStepsPerFrame</c> steps so a long hitch
        /// (a domain reload, a breakpoint) drops time rather than trying to catch up forever.
        /// </summary>
        /// <returns>Number of whole steps now waiting to be consumed.</returns>
        public int Accumulate(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                _accumulator += deltaTime;
            }

            while (_accumulator >= StepDuration)
            {
                _accumulator -= StepDuration;
                _stepsAvailable++;
            }

            if (_stepsAvailable > _maxStepsPerFrame)
            {
                _stepsAvailable = _maxStepsPerFrame;
            }

            return _stepsAvailable;
        }

        /// <summary>
        /// Takes one banked step. Drive the simulation with
        /// <c>while (clock.TryConsumeStep(out int frame)) { Step(frame); }</c>.
        /// </summary>
        public bool TryConsumeStep(out int frame)
        {
            if (_stepsAvailable <= 0)
            {
                frame = Frame;
                return false;
            }

            _stepsAvailable--;
            frame = Frame;
            Frame++;
            return true;
        }

        /// <summary>Drops all banked time and steps, keeping the frame counter. For pause and scene changes.</summary>
        public void Reset()
        {
            _accumulator = 0f;
            _stepsAvailable = 0;
        }
    }
}
