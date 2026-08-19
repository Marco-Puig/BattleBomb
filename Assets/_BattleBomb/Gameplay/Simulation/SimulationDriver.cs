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

        private const float ProjectileRadius = 0.6f;
        private const float ProjectileKnockback = 4f;
        private const int ProjectileHitstop = 2;
        private const int ProjectileLifeSteps = 240;

        /// <summary>D28's turn-taking: melee attackers allowed on one player at once (paper value).
        /// Brutes never wait, so the real ceiling a player faces is this plus the brutes.</summary>
        private const int MaxMeleeAttackersPerTarget = 1;

        /// <summary>Steps everyone stays down before the sandbox resets (task 35, paper value).</summary>
        private const int AttemptResetBeatSteps = 120;

        private readonly PlayerRegistry _players = new PlayerRegistry();
        private readonly Dictionary<int, PlayerCommand> _commands = new Dictionary<int, PlayerCommand>();
        private readonly List<Vector3> _candidatePositions = new List<Vector3>();
        private readonly List<Component> _candidateOwners = new List<Component>();
        private readonly List<int> _hitIndices = new List<int>();
        private readonly List<Vector3> _playerPositions = new List<Vector3>();
        private readonly List<bool> _playerDowned = new List<bool>();
        private readonly List<int> _attackTokens = new List<int>();
        private readonly List<ProjectileState> _projectiles = new List<ProjectileState>();

        private SimulationClock _clock;
        private bool _warnedMissingArena;
        private AttemptCountdown _attempt;

        /// <summary>Raised once per simulation step, with that step's frame number.</summary>
        public event Action<int> Stepped;

        /// <summary>Raised inside the fixed step for every landed hit (D20/D21 feedback).</summary>
        public event Action<HitEvent> HitLanded;

        /// <summary>Raised inside the fixed step when the failed attempt resets the sandbox — the
        /// spawner answers by restarting its encounter (task 35).</summary>
        public event Action AttemptReset;

        /// <summary>The attempt-over beat is running: everyone is down, the reset is counting.</summary>
        public bool AttemptEnding => _attempt.StepsAllDown > 0;

        public PlayerRegistry Players => _players;

        public CharacterRegistry Characters { get; } = new CharacterRegistry();

        public TargetRegistry Targets { get; } = new TargetRegistry();

        /// <summary>Bolts in flight, for Presentation to draw. Simulated inside the fixed step.</summary>
        public IReadOnlyList<ProjectileState> Projectiles => _projectiles;

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
                    int revived = actor.Step(
                        frame, command, bounds, StepDuration, FindReviveTarget(actors, i));
                    if (revived >= 0 && revived < actors.Count)
                    {
                        actors[revived].ApplyRevive();
                    }
                }

                // What the enemies may know this step (their perception is built from this).
                _playerPositions.Clear();
                _playerDowned.Clear();
                for (int i = 0; i < actors.Count; i++)
                {
                    _playerPositions.Add(actors[i].Position);
                    _playerDowned.Add(actors[i].Condition.IsDown);
                }

                // D28's turn-taking: count who already holds each player's melee attack token,
                // then walk the registry order — waiting melee hovers and circles instead.
                _attackTokens.Clear();
                for (int i = 0; i < actors.Count; i++)
                {
                    _attackTokens.Add(0);
                }

                IReadOnlyList<ISimTarget> targets = Targets.Ordered;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] is EnemyActor holder && holder.TakesMeleeTurns && holder.IsAttacking
                        && holder.TargetIndex >= 0 && holder.TargetIndex < _attackTokens.Count)
                    {
                        _attackTokens[holder.TargetIndex] += 1;
                    }
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] is EnemyActor enemy)
                    {
                        bool mayAttack = true;
                        if (enemy.TakesMeleeTurns && !enemy.IsAttacking)
                        {
                            int target = enemy.TargetIndex;
                            mayAttack = target < 0 || target >= _attackTokens.Count
                                || _attackTokens[target] < MaxMeleeAttackersPerTarget;
                        }

                        bool started = enemy.Step(
                            frame, _playerPositions, _playerDowned, mayAttack, bounds, StepDuration);
                        if (started && enemy.TakesMeleeTurns
                            && enemy.TargetIndex >= 0 && enemy.TargetIndex < _attackTokens.Count)
                        {
                            _attackTokens[enemy.TargetIndex] += 1;
                        }
                    }
                    else if (targets[i] is TrainingDummy dummy)
                    {
                        dummy.Step(frame, bounds, StepDuration);
                    }
                }

                StepProjectiles(actors, StepDuration);
                StepAttemptFlow(actors);
                Stepped?.Invoke(frame);
            }
        }

        /// <summary>
        /// The downed partner this player's Light would revive right now, or -1. Built from live
        /// registry state in registry order, so the answer is deterministic (D10).
        /// </summary>
        private int FindReviveTarget(IReadOnlyList<CharacterActor> actors, int selfIndex)
        {
            if (actors.Count < 2)
            {
                return -1;
            }

            _playerPositions.Clear();
            _playerDowned.Clear();
            for (int i = 0; i < actors.Count; i++)
            {
                _playerPositions.Add(actors[i].Position);
                _playerDowned.Add(actors[i].Condition.IsDown);
            }

            return ReviveChannel.FindTarget(
                actors[selfIndex].Position, _playerPositions, _playerDowned,
                selfIndex, actors[selfIndex].ReviveRange);
        }

        /// <summary>
        /// The attempt-over beat (task 35): everyone down starts the countdown, anyone standing
        /// back up cancels it, and the trigger resets players, bolts, and the encounter together.
        /// </summary>
        private void StepAttemptFlow(IReadOnlyList<CharacterActor> actors)
        {
            _attempt = _attempt.Step(AttemptCountdown.AllDown(_playerDowned));
            if (!_attempt.Triggers(AttemptResetBeatSteps))
            {
                return;
            }

            _attempt = default;
            for (int i = 0; i < actors.Count; i++)
            {
                actors[i].ResetForAttempt();
            }

            _projectiles.Clear();
            AttemptReset?.Invoke();
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
        /// Resolves one player attack's hit window: enemies and dummies take the full pipeline
        /// (enemies through their authored resistances, plus the flinch), the other player takes a
        /// shove and nothing else (D21). Runs inside the fixed step; Presentation and UI hear
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
                if (owner is EnemyActor enemy)
                {
                    HitResult hit = HitApplication.Apply(
                        attack, attacker.Position, attacker.Facing, attacker.StrikeMomentum,
                        Element.None, 1f, TargetKind.Enemy, enemy.Position,
                        enemy.Resistances, ElementalMultipliers.Neutral);
                    enemy.ApplyHit(hit);
                    attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                    HitLanded?.Invoke(new HitEvent(attacker, enemy, hit.Damage, enemy.Position, false));
                }
                else if (owner is TrainingDummy dummy)
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

        /// <summary>
        /// An enemy's melee window: the same reach geometry players use (§2.2), resolved against
        /// players and applied through their condition — grace and the downed state swallow hits
        /// entirely, and momentum feeds the shove like every other hit.
        /// </summary>
        internal void ResolveEnemyMelee(EnemyActor attacker, in AttackTuning attack)
        {
            IReadOnlyList<CharacterActor> players = Characters.Ordered;
            HitResolver.Resolve(attacker.Position, attacker.Facing, attack, _playerPositions, _hitIndices);

            int attackerHitstop = 0;
            for (int i = 0; i < _hitIndices.Count; i++)
            {
                CharacterActor victim = players[_hitIndices[i]];
                if (victim.Condition.IsInvulnerable)
                {
                    continue;
                }

                HitResult hit = HitApplication.Apply(
                    attack, attacker.Position, attacker.Facing, attacker.StrikeMomentum,
                    attacker.Spec.Tuning.Element, 1f, TargetKind.Enemy, victim.Position,
                    ElementalMultipliers.Neutral, ElementalMultipliers.Neutral);
                victim.ApplyEnemyHit(hit);
                attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                HitLanded?.Invoke(new HitEvent(attacker, victim, hit.Damage, victim.Position, false));
            }

            if (attackerHitstop > 0)
            {
                attacker.ApplyHitstop(attackerHitstop);
            }
        }

        /// <summary>A ranged or caster shot leaves the muzzle aimed at the target's position now.</summary>
        internal void SpawnProjectile(EnemyActor shooter, Vector3 target)
        {
            _projectiles.Add(ProjectileState.Fired(
                shooter.Position, target, shooter.Spec.Tuning.ProjectileSpeed,
                shooter.Spec.Tuning.Attack.Damage, shooter.Spec.Tuning.Element, ProjectileLifeSteps));
        }

        private void StepProjectiles(IReadOnlyList<CharacterActor> players, float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileState p = ProjectileSimulation.Step(_projectiles[i], dt);
                if (p.IsExpired)
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                int hit = ProjectileSimulation.HitTest(p, _playerPositions, ProjectileRadius);
                if (hit >= 0 && hit < players.Count && !players[hit].Condition.IsInvulnerable)
                {
                    CharacterActor victim = players[hit];
                    float damage = DamageCalculator.Resolve(
                        p.Damage, p.Element, ElementalMultipliers.Neutral, ElementalMultipliers.Neutral, 1f);
                    Vector3 direction = new Vector3(p.Velocity.x, 0f, p.Velocity.z);
                    direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.right;
                    victim.ApplyEnemyHit(new HitResult(
                        damage, direction * ProjectileKnockback, ProjectileHitstop));
                    HitLanded?.Invoke(new HitEvent(null, victim, damage, victim.Position, false));
                    _projectiles.RemoveAt(i);
                    continue;
                }

                _projectiles[i] = p;
            }
        }

        private void CollectCandidates(CharacterActor except, bool includePartners)
        {
            _candidatePositions.Clear();
            _candidateOwners.Clear();

            IReadOnlyList<ISimTarget> targets = Targets.Ordered;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsDepleted)
                {
                    continue;
                }

                _candidatePositions.Add(targets[i].Position);
                _candidateOwners.Add(targets[i].Body);
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
