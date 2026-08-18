using System;
using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Core.Simulation;
using BattleBomb.Core.Spatial;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.World;
using UnityEngine;

namespace BattleBomb.Gameplay.Simulation
{
    /// <summary>
    /// Pumps the fixed-step simulation. It is the only thing that reads <c>Time</c>: it converts real
    /// time into whole steps, samples every player's command for each step, and announces the step.
    /// Presentation reads <see cref="Alpha"/> to interpolate between them (§4, D10).
    /// </summary>
    /// <remarks>
    /// Not a singleton (§9). A scene has one, and things that need it hold a reference.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SimulationDriver : MonoBehaviour
    {
        [Tooltip("Simulation steps per second. Changing this changes game feel — it is a design value, not a perf dial.")]
        [SerializeField] private int _stepsPerSecond = SimulationClock.DefaultStepsPerSecond;

        [Tooltip("Steps a single frame may run before time is dropped, so a hitch cannot spiral.")]
        [SerializeField] private int _maxStepsPerFrame = SimulationClock.DefaultMaxStepsPerFrame;

        [Tooltip("The arena characters are clamped to. Leave empty to fall back to ArenaBounds.Default.")]
        [SerializeField] private ArenaVolume _arena;

        private readonly PlayerRegistry _players = new PlayerRegistry();
        private readonly Dictionary<int, PlayerCommand> _commands = new Dictionary<int, PlayerCommand>();

        private SimulationClock _clock;
        private bool _warnedMissingArena;

        /// <summary>Raised once per simulation step, with that step's frame number.</summary>
        public event Action<int> Stepped;

        public PlayerRegistry Players => _players;

        public CharacterRegistry Characters { get; } = new CharacterRegistry();

        public ArenaBounds Bounds
        {
            get
            {
                if (_arena != null)
                {
                    return _arena.ToRuntime();
                }

                if (!_warnedMissingArena)
                {
                    _warnedMissingArena = true;
                    Debug.LogWarning($"{name}: no ArenaVolume assigned — using ArenaBounds.Default.", this);
                }

                return ArenaBounds.Default;
            }
        }

        /// <summary>Commands sampled for the most recent step, keyed by <see cref="PlayerId.Value"/>.</summary>
        public IReadOnlyDictionary<int, PlayerCommand> Commands => _commands;

        /// <summary>Fraction into the next step, 0–1. Presentation interpolates by this.</summary>
        public float Alpha => _clock?.Alpha ?? 0f;

        public int Frame => _clock?.Frame ?? 0;

        public float StepDuration => _clock?.StepDuration ?? 1f / Mathf.Max(1, _stepsPerSecond);

        public bool TryGetCommand(PlayerId playerId, out PlayerCommand command) =>
            _commands.TryGetValue(playerId.Value, out command);

        private void OnEnable()
        {
            // OnEnable rather than Awake: it re-runs after a mid-play domain reload, so a script
            // recompile during Play mode rebuilds the clock instead of leaving it null.
            _clock = new SimulationClock(Mathf.Max(1, _stepsPerSecond), Mathf.Max(1, _maxStepsPerFrame));
        }

        private void Update()
        {
            _clock.Accumulate(Time.deltaTime);

            while (_clock.TryConsumeStep(out int frame))
            {
                _players.SampleAll(frame, _commands);

                IReadOnlyList<CharacterActor> actors = Characters.Ordered;
                ArenaBounds bounds = Bounds;
                for (int i = 0; i < actors.Count; i++)
                {
                    CharacterActor actor = actors[i];
                    PlayerCommand command = _commands.TryGetValue(actor.PlayerId.Value, out PlayerCommand sampled)
                        ? sampled
                        : PlayerCommand.Idle(frame);
                    actor.Step(frame, command, bounds, StepDuration);
                }

                Stepped?.Invoke(frame);
            }
        }

        private void OnDisable()
        {
            // Time spent disabled is not time the simulation lived through.
            _clock?.Reset();
        }
    }
}
