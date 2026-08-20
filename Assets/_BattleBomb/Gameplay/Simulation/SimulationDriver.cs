using System;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Simulation;
using BattleBomb.Core.Spatial;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Loot;
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

        [Tooltip("Seed for the loot rolls (D23). Gameplay owns the seed; Core owns the maths.")]
        [SerializeField] private int _lootSeed = 1;

        [Tooltip("Seed for combat rolls (crits). Its own stream, so loot replay never shifts with a fight.")]
        [SerializeField] private int _combatSeed = 2;

        [Tooltip("The authored ladder and drop-kind weights (D33). Empty runs Core's paper defaults.")]
        [SerializeField] private QualityLadder _qualityLadder;

        [Tooltip("Everything the generator may drop (D35). A new item is a new asset here, never code.")]
        [SerializeField] private ItemDefinition[] _itemCatalog;

        private const float ProjectileRadius = 0.6f;
        private const float ProjectileKnockback = 4f;
        private const int ProjectileHitstop = 2;
        private const int ProjectileLifeSteps = 240;
        private const float PlayerShotKnockback = 3f;
        private const int PlayerShotHitstop = 1;
        private const float PlayerShotAimRangeX = 14f;

        /// <summary>D28's turn-taking: melee attackers allowed on one player at once (paper value).
        /// Brutes never wait, so the real ceiling a player faces is this plus the brutes.</summary>
        private const int MaxMeleeAttackersPerTarget = 1;

        /// <summary>Steps everyone stays down before the sandbox resets (task 35, paper value).</summary>
        private const int AttemptResetBeatSteps = 120;

        /// <summary>Planar reach of a grab, and of the drop's inspect panel (D23/D30, paper value).</summary>
        public const float GrabRadius = 0.9f;

        /// <summary>The elite quality bonus slot — no elite spawns until M6 pays it (decision 8).</summary>
        private const float LootEliteBonus = 1.5f;

        /// <summary>The D36 level stamp's source — a constant until M7 builds real progress.</summary>
        private const int StoryProgressLevel = 1;

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
        private DeterministicRandom _lootRng;
        private DeterministicRandom _combatRng;
        private readonly List<ItemSpec> _itemSpecs = new List<ItemSpec>();
        private QualityTable _qualityTable;
        private DropWeights _dropWeights;
        private readonly List<DropPickup> _pickups = new List<DropPickup>();
        private readonly Dictionary<int, int> _grabCounts = new Dictionary<int, int>();
        private readonly List<Vector3> _bodyPositions = new List<Vector3>();
        private readonly List<Vector3> _bodyVelocities = new List<Vector3>();
        private readonly List<Vector3> _bodyPushes = new List<Vector3>();

        /// <summary>Raised once per simulation step, with that step's frame number.</summary>
        public event Action<int> Stepped;

        /// <summary>Raised inside the fixed step for every landed hit (D20/D21 feedback).</summary>
        public event Action<HitEvent> HitLanded;

        /// <summary>Raised inside the fixed step when the failed attempt resets the sandbox — the
        /// spawner answers by restarting its encounter (task 35).</summary>
        public event Action AttemptReset;

        /// <summary>The attempt-over beat is running: everyone is down, the reset is counting.</summary>
        public bool AttemptEnding => _attempt.StepsAllDown > 0;

        /// <summary>Raised inside the fixed step when an enemy's dying beat ends (task 36).</summary>
        public event Action<EnemyDeath> EnemyDied;

        /// <summary>Drops this player has grabbed (D23) — the HUD's proof the loop works.</summary>
        public int GrabCountFor(int playerIdValue) =>
            _grabCounts.TryGetValue(playerIdValue, out int count) ? count : 0;

        /// <summary>Drops waiting on the ground, for the inspect panel to read (D30).</summary>
        public IReadOnlyList<DropPickup> Pickups => _pickups;

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
            _lootRng = new DeterministicRandom((uint)_lootSeed);
            _combatRng = new DeterministicRandom((uint)_combatSeed);

            _itemSpecs.Clear();
            if (_itemCatalog != null)
            {
                for (int i = 0; i < _itemCatalog.Length; i++)
                {
                    if (_itemCatalog[i] != null)
                    {
                        _itemSpecs.Add(_itemCatalog[i].ToRuntime());
                    }
                }
            }

            _qualityTable = _qualityLadder != null ? _qualityLadder.ToTable() : QualityTable.Default;
            _dropWeights = _qualityLadder != null ? _qualityLadder.ToWeights() : DropWeights.Default;
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
                    int grabTarget = FindGrabTarget(actor);
                    ActorStepResult result = actor.Step(
                        frame, command, bounds, StepDuration,
                        FindReviveTarget(actors, i), grabTarget >= 0);
                    if (result.RevivedPartner >= 0 && result.RevivedPartner < actors.Count)
                    {
                        actors[result.RevivedPartner].ApplyRevive(result.ReviveFraction);
                    }

                    if (result.GrabbedLoot && grabTarget >= 0 && grabTarget < _pickups.Count
                        && _pickups[grabTarget] != null)
                    {
                        int id = actor.PlayerId.Value;
                        _grabCounts[id] = GrabCountFor(id) + 1;
                        PlayerInventory bag = actor.GetComponent<PlayerInventory>();
                        if (bag != null && bag.Take(_pickups[grabTarget].Item))
                        {
                            actor.RefreshStats();
                        }

                        Destroy(_pickups[grabTarget].gameObject);
                        _pickups.RemoveAt(grabTarget);
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
                    if (targets[i] is EnemyActor holder && !holder.IsDepleted
                        && holder.TakesMeleeTurns && holder.IsAttacking
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

                SeparateBodies(actors, bounds);
                StepProjectiles(actors, StepDuration);
                ResolveDeaths();
                StepPickups(actors);
                StepAttemptFlow(actors);
                Stepped?.Invoke(frame);
            }
        }

        /// <summary>
        /// The soft crowding pass (task 37): players first, then enemies, both in registry order
        /// (D10), nudged apart after every motor has stepped. Never a Rigidbody — the M1 seam
        /// made real. Dummies are tuning props and stay out of it.
        /// </summary>
        private void SeparateBodies(IReadOnlyList<CharacterActor> players, in ArenaBounds bounds)
        {
            IReadOnlyList<ISimTarget> targets = Targets.Ordered;
            _bodyPositions.Clear();
            _bodyVelocities.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                _bodyPositions.Add(players[i].Position);
                _bodyVelocities.Add(players[i].Velocity);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is EnemyActor enemy)
                {
                    _bodyPositions.Add(enemy.Position);
                    _bodyVelocities.Add(enemy.Velocity);
                }
            }

            if (_bodyPositions.Count < 2)
            {
                return;
            }

            BodySeparation.Resolve(_bodyPositions, _bodyVelocities, _bodyPushes);

            for (int i = 0; i < players.Count; i++)
            {
                players[i].ApplySeparation(_bodyPushes[i], bounds);
            }

            int cursor = players.Count;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is EnemyActor enemy)
                {
                    enemy.ApplySeparation(_bodyPushes[cursor], bounds);
                    cursor++;
                }
            }
        }

        /// <summary>
        /// Ends each finished dying beat: announce the death, roll D23's drop, spawn the token if
        /// it paid out, despawn the corpse. Progress, difficulty, and multipliers hold their
        /// paper value of 1 until M7 builds the systems behind those slots (decision 9).
        /// </summary>
        private void ResolveDeaths()
        {
            IReadOnlyList<ISimTarget> targets = Targets.Ordered;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!(targets[i] is EnemyActor enemy) || !enemy.ConsumeDeath())
                {
                    continue;
                }

                EnemySpec spec = enemy.Spec;
                EnemyDied?.Invoke(new EnemyDeath(spec.Rank, spec.XpReward, false, enemy.Position));

                _lootRng = DropRoll.Roll(
                    _lootRng, spec.Rank, 1f, 1f, 1f, false, LootEliteBonus, out DropDecision drop);
                if (drop.Dropped)
                {
                    var context = new GenerationContext(
                        drop.Quality, StoryProgressLevel, _itemSpecs, _qualityTable, _dropWeights);
                    _lootRng = ItemGenerator.Roll(_lootRng, context, out ItemInstance item);
                    if (!item.IsEmpty)
                    {
                        _pickups.Add(DropPickup.Spawn(enemy.Position, item));
                    }
                }

                Destroy(enemy.gameObject);
            }
        }

        /// <summary>
        /// The drop this player's Light would take right now (D30): the nearest one inside the
        /// grab radius, or -1. Grabs resolve per-actor in registry order, so a simultaneous
        /// couch press has one deterministic winner — the loser's press finds nothing left.
        /// </summary>
        private int FindGrabTarget(CharacterActor player)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            const float radiusSq = GrabRadius * GrabRadius;
            for (int i = 0; i < _pickups.Count; i++)
            {
                if (_pickups[i] == null)
                {
                    continue;
                }

                Vector3 to = _pickups[i].Position - player.Position;
                to.y = 0f;
                float sq = to.sqrMagnitude;
                if (sq <= radiusSq && sq < bestSq)
                {
                    best = i;
                    bestSq = sq;
                }
            }

            return best;
        }

        private void StepPickups(IReadOnlyList<CharacterActor> players)
        {
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                if (_pickups[i] == null)
                {
                    _pickups.RemoveAt(i);
                }
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
            for (int i = 0; i < _pickups.Count; i++)
            {
                if (_pickups[i] != null)
                {
                    Destroy(_pickups[i].gameObject);
                }
            }

            _pickups.Clear();
            _grabCounts.Clear();
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
                    HitResult hit = RollPlayerHit(
                        attacker, attack, enemy.Position, enemy.Resistances, out bool crit);
                    enemy.ApplyHit(hit);
                    StealLife(attacker, hit.Damage);
                    attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                    HitLanded?.Invoke(new HitEvent(attacker, enemy, hit.Damage, enemy.Position, false, crit));
                }
                else if (owner is TrainingDummy dummy)
                {
                    HitResult hit = RollPlayerHit(
                        attacker, attack, dummy.Position, ElementalMultipliers.Neutral, out bool crit);
                    dummy.ApplyHit(hit);
                    StealLife(attacker, hit.Damage);
                    attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                    HitLanded?.Invoke(new HitEvent(attacker, dummy, hit.Damage, dummy.Position, false, crit));
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
        /// One player hit through the M4 build (D32/D35): the weapon's damage scale feeds the
        /// pipeline's gear slot, the combat stream prices the crit — always drawn, so the stream
        /// never depends on the chance — and knockback affixes scale the shove.
        /// </summary>
        private HitResult RollPlayerHit(
            CharacterActor attacker, in AttackTuning attack, Vector3 targetPosition,
            in ElementalMultipliers resistances, out bool crit)
        {
            float gear = attacker.DamageScale;
            _combatRng = _combatRng.NextFloat(out float critDraw);
            crit = critDraw < attacker.Sheet.CritChance;
            if (crit)
            {
                gear *= attacker.Sheet.CritDamageMultiplier;
            }

            HitResult hit = HitApplication.Apply(
                attack, attacker.Position, attacker.Facing, attacker.StrikeMomentum,
                Element.None, gear, TargetKind.Enemy, targetPosition,
                resistances, ElementalMultipliers.Neutral);

            float knockback = attacker.Sheet.KnockbackMultiplier;
            return Mathf.Approximately(knockback, 1f)
                ? hit
                : new HitResult(hit.Damage, hit.Impulse * knockback, hit.HitstopSteps);
        }

        private static void StealLife(CharacterActor attacker, float damage)
        {
            float steal = attacker.Sheet.LifeSteal;
            if (steal > 0f && damage > 0f)
            {
                attacker.Heal(damage * steal);
            }
        }

        /// <summary>
        /// An enemy's melee window: the same reach geometry players use (§2.2), resolved against
        /// players and applied through their condition — grace and the downed state swallow hits
        /// entirely, defence shaves what lands (D26), and momentum feeds the shove like every
        /// other hit.
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
                float landed = victim.ApplyEnemyHit(hit);
                attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                HitLanded?.Invoke(new HitEvent(attacker, victim, landed, victim.Position, false));
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

        /// <summary>
        /// The bow's Light (D34): an arrow aimed at the nearest live target ahead — depth included,
        /// which is the whole ranged identity (§2.2) — or straight ahead when nothing is. Damage
        /// carries the authored attack number; the build's scale, the crit, and the steal price at
        /// impact from the shooter's live sheet.
        /// </summary>
        internal void SpawnPlayerShot(CharacterActor shooter, in AttackTuning attack)
        {
            float sign = shooter.Facing == Facing.Right ? 1f : -1f;
            Vector3 target = shooter.Position + new Vector3(sign * PlayerShotAimRangeX, 0f, 0f);
            float bestSq = float.MaxValue;

            IReadOnlyList<ISimTarget> targets = Targets.Ordered;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsDepleted)
                {
                    continue;
                }

                Vector3 to = targets[i].Position - shooter.Position;
                if (to.x * sign <= 0.1f || Mathf.Abs(to.x) > PlayerShotAimRangeX)
                {
                    continue;
                }

                to.y = 0f;
                float sq = to.sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    target = targets[i].Position;
                }
            }

            _projectiles.Add(ProjectileState.Fired(
                shooter.Position, target, shooter.ShotSpeed, attack.Damage, Element.None,
                ProjectileLifeSteps, shooter.PlayerId.Value));
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

                if (p.FromPlayer)
                {
                    if (ResolvePlayerShot(players, p))
                    {
                        _projectiles.RemoveAt(i);
                        continue;
                    }

                    _projectiles[i] = p;
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
                    float landed = victim.ApplyEnemyHit(new HitResult(
                        damage, direction * ProjectileKnockback, ProjectileHitstop));
                    HitLanded?.Invoke(new HitEvent(null, victim, landed, victim.Position, false));
                    _projectiles.RemoveAt(i);
                    continue;
                }

                _projectiles[i] = p;
            }
        }

        /// <summary>
        /// One arrow against the enemy side (task 46): the same spherical flight test bolts use,
        /// priced at impact from the shooter's live sheet — scale, crit, steal, knockback — so an
        /// arrow in flight is as honest as a swing. True when the arrow connected.
        /// </summary>
        private bool ResolvePlayerShot(IReadOnlyList<CharacterActor> players, in ProjectileState p)
        {
            CollectCandidates(null, includePartners: false);
            int hit = ProjectileSimulation.HitTest(p, _candidatePositions, ProjectileRadius);
            if (hit < 0)
            {
                return false;
            }

            CharacterActor shooter = null;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId.Value == p.OwnerPlayerId)
                {
                    shooter = players[i];
                    break;
                }
            }

            float gear = 1f;
            float knockback = 1f;
            bool crit = false;
            if (shooter != null)
            {
                gear = shooter.DamageScale;
                _combatRng = _combatRng.NextFloat(out float critDraw);
                crit = critDraw < shooter.Sheet.CritChance;
                if (crit)
                {
                    gear *= shooter.Sheet.CritDamageMultiplier;
                }

                knockback = shooter.Sheet.KnockbackMultiplier;
            }

            Vector3 direction = new Vector3(p.Velocity.x, 0f, p.Velocity.z);
            direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.right;
            Vector3 impulse = direction * (PlayerShotKnockback * knockback);

            Component owner = _candidateOwners[hit];
            if (owner is EnemyActor enemy)
            {
                float damage = DamageCalculator.Resolve(
                    p.Damage, p.Element, enemy.Resistances, ElementalMultipliers.Neutral, gear);
                enemy.ApplyHit(new HitResult(damage, impulse, PlayerShotHitstop));
                if (shooter != null)
                {
                    StealLife(shooter, damage);
                }

                HitLanded?.Invoke(new HitEvent(shooter, enemy, damage, enemy.Position, false, crit));
                return true;
            }

            if (owner is TrainingDummy dummy)
            {
                float damage = DamageCalculator.Resolve(
                    p.Damage, p.Element, ElementalMultipliers.Neutral, ElementalMultipliers.Neutral, gear);
                dummy.ApplyHit(new HitResult(damage, impulse, PlayerShotHitstop));
                if (shooter != null)
                {
                    StealLife(shooter, damage);
                }

                HitLanded?.Invoke(new HitEvent(shooter, dummy, damage, dummy.Position, false, crit));
                return true;
            }

            return false;
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
