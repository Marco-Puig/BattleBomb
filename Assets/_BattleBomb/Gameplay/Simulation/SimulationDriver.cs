using System;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
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
        private readonly List<Vector3> _candidatePositions = new List<Vector3>();
        private readonly List<Component> _candidateOwners = new List<Component>();
        private readonly List<int> _hitIndices = new List<int>();

        private SimulationClock _clock;
        private bool _warnedMissingArena;

        /// <summary>Raised once per simulation step, with that step's frame number.</summary>
        public event Action<int> Stepped;

        /// <summary>Raised inside the fixed step for every landed hit (D20/D21 feedback).</summary>
        public event Action<HitEvent> HitLanded;

        public PlayerRegistry Players => _players;

        public CharacterRegistry Characters { get; } = new CharacterRegistry();

        public TargetRegistry Targets { get; } = new TargetRegistry();

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

                IReadOnlyList<TrainingDummy> dummies = Targets.Ordered;
                for (int i = 0; i < dummies.Count; i++)
                {
                    dummies[i].Step(frame, bounds, StepDuration);
                }

                Stepped?.Invoke(frame);
            }
        }

        /// <summary>
        /// The soft lunge's destination for an attack starting this step, if anything qualifies.
        /// Only enemies — the snap never drags a swing toward the other player (Michael's
        /// playtest), though a partner naturally in reach is still shoved.
        /// </summary>
        internal bool TryPickLungeTarget(CharacterActor attacker, in AttackTuning attack, out Vector3 target)
        {
            CollectCandidates(attacker, includePartners: false);
            int index = HitResolver.LungeTarget(attacker.Position, attacker.Facing, attack, _candidatePositions);
            target = index >= 0 ? _candidatePositions[index] : default;
            return index >= 0;
        }

        /// <summary>
        /// Resolves one attack's hit window: dummies take the full pipeline, the other player takes
        /// a shove and nothing else (D21). Runs inside the fixed step; Presentation and UI hear
        /// about it through <see cref="HitLanded"/>.
        /// </summary>
        internal void ResolveHits(CharacterActor attacker, in AttackTuning attack)
        {
            CollectCandidates(attacker, includePartners: true);
            if (attack.ResolvesOnLanding)
            {
                // The slam bursts radially around the landing point (D14 as the reticle).
                HitResolver.ResolveRadial(attacker.Position, attack, _candidatePositions, _hitIndices);
            }
            else
            {
                HitResolver.Resolve(attacker.Position, attacker.Facing, attack, _candidatePositions, _hitIndices);
            }

            int attackerHitstop = 0;
            for (int i = 0; i < _hitIndices.Count; i++)
            {
                Component owner = _candidateOwners[_hitIndices[i]];
                if (owner is TrainingDummy dummy)
                {
                    HitResult hit = HitApplication.Apply(
                        attack, attacker.Position, attacker.Facing, attacker.StrikeMomentum,
                        Element.None, 1f, TargetKind.Enemy, dummy.Position,
                        ElementalMultipliers.Neutral, ElementalMultipliers.Neutral);
                    dummy.ApplyHit(hit);
                    attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                    HitLanded?.Invoke(new HitEvent(attacker, dummy, hit.Damage, dummy.Position, false));
                }
                else if (owner is CharacterActor partner)
                {
                    HitResult shove = HitApplication.Apply(
                        attack, attacker.Position, attacker.Facing, attacker.StrikeMomentum,
                        Element.None, 1f, TargetKind.Partner, partner.Position,
                        ElementalMultipliers.Neutral, ElementalMultipliers.Neutral);
                    partner.ApplyImpulse(shove.Impulse);
                    HitLanded?.Invoke(new HitEvent(attacker, partner, 0f, partner.Position, true));
                }
            }

            if (attackerHitstop > 0)
            {
                attacker.ApplyHitstop(attackerHitstop);
            }
        }

        private void CollectCandidates(CharacterActor except, bool includePartners)
        {
            _candidatePositions.Clear();
            _candidateOwners.Clear();

            IReadOnlyList<TrainingDummy> dummies = Targets.Ordered;
            for (int i = 0; i < dummies.Count; i++)
            {
                if (dummies[i].IsDepleted)
                {
                    continue;
                }

                _candidatePositions.Add(dummies[i].Position);
                _candidateOwners.Add(dummies[i]);
            }

            if (!includePartners)
            {
                return;
            }

            IReadOnlyList<CharacterActor> actors = Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] == except)
                {
                    continue;
                }

                _candidatePositions.Add(actors[i].Position);
                _candidateOwners.Add(actors[i]);
            }
        }

        private void OnDisable()
        {
            // Time spent disabled is not time the simulation lived through.
            _clock?.Reset();
        }
    }
}
