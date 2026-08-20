using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// What one player step produced beyond motion: a completed revive (and the health fraction
    /// the reviver's accuracy earned), or a deliberate loot grab (D30). The driver acts on both —
    /// one actor never rewrites another, and only the driver knows which pickup was in reach.
    /// </summary>
    internal readonly struct ActorStepResult
    {
        public readonly int RevivedPartner;
        public readonly float ReviveFraction;
        public readonly bool GrabbedLoot;

        public ActorStepResult(int revivedPartner, float reviveFraction, bool grabbedLoot)
        {
            RevivedPartner = revivedPartner;
            ReviveFraction = reviveFraction;
            GrabbedLoot = grabbedLoot;
        }
    }

    /// <summary>
    /// The simulation's view of one character: it owns the motor state and writes the transform.
    /// <see cref="Step"/> is internal on purpose — Presentation and UI reference this assembly, and
    /// internal is the compiler-level guarantee they can observe but never drive it (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterActor : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition _definition;

        [Tooltip("Player slot used when no command source sits on this object.")]
        [SerializeField] private int _playerIndex;

        [Tooltip("Driver this character registers with. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private IPlayerCommandSource _source;
        private MovementTuning _tuning;
        private CombatKit _kit;
        private MotorState _state;
        private MotorState _previous;
        private CombatState _combat = CombatState.Ready;
        private Vector3 _lungePerStep;
        private int _lungeStepsLeft;
        private bool _attackRooted;
        private Vector3 _strikeMomentum;
        private PlayerCondition _condition;
        private int _hitStaggerSteps;
        private int _hitGraceSteps;
        private ReviveChannel _revive = ReviveChannel.Inactive;
        private float _reviveRequiredProgress = 10f;
        private int _reviveBeatSteps = 45;
        private int _reviveRushSteps = 15;
        private int _revivePumpDecaySteps = 60;
        private float _reviveMinHealthFraction = 0.25f;
        private float _reviveMaxHealthFraction = 0.65f;
        private float _reviveRange = 1.8f;
        private int _reviveGraceSteps = 60;
        private Vector3 _spawnPosition;
        private bool _spawnCaptured;
        private StatTuning _statTuning;
        private StatSheet _sheet;
        private CombatKit _activeKit;
        private MovementTuning _activeTuning;
        private ManaPool _mana;
        private float _damageScale = 1f;
        private WeaponClass _weaponClass = WeaponClass.None;
        private ElementId _element = ElementId.None;
        private float _shotSpeed = 12f;
        private int _quickUseCooldownSteps = 180;
        private PlayerInventory _bag;
        private MagicKit _magic;
        private MagicKit _activeMagic;
        private bool _leapAvailable = true;
        private ElementId _infusion = ElementId.None;
        private float _infusionScale;
        private readonly List<GearContribution> _gearScratch = new List<GearContribution>();
        private readonly List<ElementalMultiplier> _resistScratch = new List<ElementalMultiplier>();

        /// <summary>
        /// Read live from the command source: PlayerInput assigns its player index after sibling
        /// OnEnable runs, so a snapshot taken there would freeze the wrong id.
        /// </summary>
        public PlayerId PlayerId => _source != null ? _source.PlayerId : new PlayerId(_playerIndex);
        public Vector3 Position => _state.Position;
        public Vector3 PreviousPosition => _previous.Position;
        public Facing Facing => _state.Facing;
        public bool IsGrounded => _state.IsGrounded;

        public AttackPhase CombatPhase => _combat.Phase;
        public AttackTuning CurrentAttack => _combat.CurrentAttack;
        public PlayerCondition Condition => _condition;

        /// <summary>The revive channel this player is running, for the HUD to draw (task 35).</summary>
        public ReviveChannel Revive => _revive;

        /// <summary>Channel progress 0–1, for the HUD's bar — it visibly drains in silence.</summary>
        public float ReviveProgress => _revive.IsActive
            ? Mathf.Clamp01(_revive.Progress / _reviveRequiredProgress)
            : 0f;

        /// <summary>The heartbeat's period, so the HUD's pulse beats on the accuracy clock (D31).</summary>
        public int ReviveBeatSteps => _reviveBeatSteps;

        internal float ReviveRange => _reviveRange;
        public float ChargeFraction => _activeKit == null || _activeKit.ChargeThresholdSteps <= 0
            ? 0f
            : Mathf.Clamp01((float)_combat.ChargeSteps / _activeKit.ChargeThresholdSteps);

        /// <summary>The aggregated build (D32/D35): base points plus everything worn.</summary>
        public StatSheet Sheet => _sheet;

        /// <summary>The mana pool the sheet sizes; M5's Magic spends it.</summary>
        public ManaPool Mana => _mana;

        /// <summary>The character's own element (D38) — what they cast, and what cannot mark them.</summary>
        public ElementId Element => _element;

        /// <summary>Their element plus the resistance their gear rolled (D40).</summary>
        internal ElementalDefence Defence => new ElementalDefence(_element, _sheet.ElementalResistance);

        /// <summary>The worn weapon's infusion (D19): the element every hit carries, or None.</summary>
        internal ElementId Infusion => _infusion;

        /// <summary>How much of a cast's mark the infusion roll is worth — its rolled magnitude.</summary>
        internal float InfusionScale => _infusionScale;

        /// <summary>The elements currently marking them (D40) — enemies burn players too.</summary>
        public StatusTrack Statuses { get; } = new StatusTrack();

        /// <summary>Authored attack damage × this = final base damage; unarmed is exactly 1.</summary>
        internal float DamageScale => _damageScale;

        /// <summary>The worn weapon's class — a bow turns chain Lights into shots (D34).</summary>
        public WeaponClass WeaponClass => _weaponClass;

        /// <summary>Arrow flight speed, from the worn bow's roll.</summary>
        internal float ShotSpeed => _shotSpeed;

        /// <summary>
        /// One fixed step. <paramref name="reviveTarget"/> is the downed partner in revive range
        /// this step (or -1) and <paramref name="lootInReach"/> whether a drop sits in grab range —
        /// both computed by the driver, which alone acts on the result. Contextual Light resolves
        /// in priority order: the revive channel first (D25), then the loot grab (D30), then the
        /// swing.
        /// </summary>
        internal ActorStepResult Step(
            int frame, in PlayerCommand command, in ArenaBounds bounds, float dt,
            int reviveTarget, bool lootInReach)
        {
            float reviveFraction = 0f;
            bool grabbedLoot = false;
            _previous = _state;

            // Staggered or downed, the player's intent goes nowhere — the body still obeys
            // physics (knockback, gravity), it just takes no orders (task 29).
            PlayerCommand effective = _condition.InControl ? command : PlayerCommand.Idle(frame);

            bool frozen = _combat.HitstopSteps > 0;
            int completedRevive = -1;
            if (!frozen)
            {
                // Contextual Light (D17/D25): beside a downed partner the press channels instead
                // of swinging, and only from combat-Ready — a swing or charge in flight keeps its
                // buttons. While the channel runs, attack presses never reach the machine; each
                // press earns by its heartbeat timing, and accuracy prices the revive (D31).
                bool mayChannel = _condition.InControl && _combat.Phase == AttackPhase.Ready;
                _revive = ReviveChannel.Next(
                    _revive,
                    (effective.Pressed & CommandButtons.Light) != 0,
                    reviveTarget,
                    mayChannel,
                    _reviveBeatSteps,
                    _reviveRushSteps,
                    _revivePumpDecaySteps);
                if (_revive.IsComplete(_reviveRequiredProgress))
                {
                    completedRevive = _revive.TargetIndex;
                    reviveFraction = ReviveChannel.RestoredFraction(
                        _revive.AverageAccuracy,
                        _reviveMinHealthFraction, _reviveMaxHealthFraction);
                    _revive = ReviveChannel.Inactive;
                }
            }

            if (_revive.IsActive)
            {
                effective = WithoutAttacks(effective);
            }

            // The deliberate grab (D30): Light beside a drop takes it instead of swinging, but
            // never while a revive channel holds the press — the partner outranks the loot.
            if (!frozen && lootInReach && !_revive.IsActive && completedRevive < 0
                && _condition.InControl && _combat.Phase == AttackPhase.Ready
                && (effective.Pressed & CommandButtons.Light) != 0)
            {
                grabbedLoot = true;
                effective = WithoutLight(effective);
            }

            // The quick-use press (D37): drinks whatever the slot holds. `effective` is already
            // idle without control, so the downed and staggered never quaff.
            if (!frozen && _bag != null && (effective.Pressed & CommandButtons.Equipment) != 0)
            {
                QuickUseResult quick = _bag.Inventory.UseQuickSlot(_quickUseCooldownSteps);
                if (quick.Used && quick.HealFraction > 0f)
                {
                    _condition = _condition.Healed(quick.HealFraction * _sheet.MaxHealth);
                }
            }

            // The leap is once per airborne (D39): touching ground restores it, so it can never
            // become an infinite mana-priced climb.
            if (_state.IsGrounded)
            {
                _leapAvailable = true;
            }

            var magic = new MagicContext(_activeMagic, _mana.Current, _leapAvailable);
            CombatStepResult combat = CombatMachine.Step(
                _combat, effective, _activeKit, magic, _state.IsGrounded);
            _combat = combat.State;
            if (frozen)
            {
                // Hitstop freezes the whole character — the machine above only counted it down.
                return new ActorStepResult(completedRevive, reviveFraction, grabbedLoot);
            }

            _condition = _condition.Step();
            _mana = _mana.Step(_sheet.ManaRegen, dt);
            if (_bag != null)
            {
                _bag.Inventory.Step();
            }

            if (combat.CastStarted)
            {
                // The machine confirmed it was affordable; the pool is ours to spend (D39).
                _mana = _mana.Spent(combat.ManaSpent);
                if (combat.Cast == MagicCastKind.Leap)
                {
                    _leapAvailable = false;
                    _state = WithLift(_state, combat.LiftSpeed);
                }
            }

            if (combat.AttackStarted)
            {
                BeginAttack(effective, combat.Attack);
            }

            AttackPhase phase = _combat.Phase;
            if (phase == AttackPhase.Ready)
            {
                _attackRooted = false;
                _lungeStepsLeft = 0;
                _state = CharacterMotor.Step(_state, effective, _activeTuning, bounds, dt);
            }
            else if (_attackRooted)
            {
                // In lunge range the snap owns the body: scoot toward the target, then hold.
                StepLungeMotion(bounds);
            }
            else
            {
                // Out of range nothing roots (Michael's playtest): Lights move freely, Heavies
                // and charging at their reduced speed, and an airborne swing hangs — vertical
                // speed stays zeroed through startup and the hit window, recovery falls normally.
                // The slam is the exception: it dives instead of hanging.
                // The leap is the one airborne action that must not hang: its whole point is the
                // climb, so a cast never stalls gravity.
                bool stallGravity = !_state.IsGrounded
                    && (phase == AttackPhase.Startup || phase == AttackPhase.Active)
                    && !_combat.CurrentAttack.ResolvesOnLanding
                    && !_combat.IsCasting;
                MovementTuning tuning = stallGravity ? WithoutGravity(_activeTuning) : _activeTuning;
                _state = CharacterMotor.Step(_state, CombatMove(effective, phase), tuning, bounds, dt);
                StepAirLunge(bounds);
            }

            if (combat.HitWindowOpened && _driver != null)
            {
                if (combat.Cast != MagicCastKind.None)
                {
                    // A cast resolves through the same geometry a swing does — the splash's line
                    // in front, the aura's circle around — but priced as magic, not as a weapon.
                    _driver.ResolveCast(this, combat.Attack, combat.Cast);
                }
                else if (_weaponClass == WeaponClass.Bow && IsChainLight(combat.Attack))
                {
                    // The bow's identity (D34/§2.2): a chain Light looses an arrow instead of a
                    // melee window; Heavy and the aerials keep their swings even with a bow worn.
                    _driver.SpawnPlayerShot(this, combat.Attack);
                }
                else
                {
                    _driver.ResolveHits(this, combat.Attack);
                }
            }

            transform.position = _state.Position;
            return new ActorStepResult(completedRevive, reviveFraction, grabbedLoot);
        }

        internal void ApplyHitstop(int steps) => _combat = _combat.WithHitstop(steps);

        /// <summary>
        /// An enemy's landed hit — the seam M3's enemies call (tasks 31–33). Grace and the downed
        /// state swallow it whole; otherwise the defence stat shaves the damage (D26/D35), then
        /// damage, stagger, the shove, and the victim's hitstop land together, and a damaging hit
        /// interrupts whatever swing was in flight. Returns what actually landed, so the number
        /// the couch sees is the number the pool lost.
        /// </summary>
        internal float ApplyEnemyHit(in HitResult hit)
        {
            if (_condition.IsInvulnerable)
            {
                return 0f;
            }

            float damage = hit.Damage * (1f - _sheet.Defence);
            _condition = _condition.Hit(damage, _hitStaggerSteps, _hitGraceSteps);
            if (damage > 0f)
            {
                _combat = CombatState.Ready;
            }

            ApplyImpulse(hit.Impulse);
            ApplyHitstop(hit.HitstopSteps);
            return damage;
        }

        /// <summary>
        /// One status tick (D40). Defence never shaves it — that stat answers hits, and a burn is
        /// not a hit — and it never staggers, shoves, or interrupts a swing: it only removes
        /// health. Grace does not save you either, because waiting out a burn behind i-frames
        /// would make the mark meaningless. Returns what landed.
        /// </summary>
        internal float ApplyStatusDamage(float damage)
        {
            if (damage <= 0f || _condition.IsDown)
            {
                return 0f;
            }

            _condition = _condition.Drained(damage);
            if (_condition.IsDown)
            {
                Statuses.Clear();
            }

            return damage;
        }

        /// <summary>
        /// A reaction's hold (D41) — the one status effect that does take control away, because a
        /// chain-stun that did not would not be a chain-stun. It grants no grace: the hold is the
        /// point, and the setup that earned it deserves the follow-up.
        /// </summary>
        internal void ApplyStun(int steps)
        {
            if (steps <= 0 || _condition.IsDown)
            {
                return;
            }

            _condition = _condition.Held(steps);
            _combat = CombatState.Ready;
        }

        /// <summary>Life steal's return (D35): heals never overfill, never stand the downed up.</summary>
        internal void Heal(float amount) => _condition = _condition.Healed(amount);

        /// <summary>A completed partner channel stands this player back up (D25); the fraction
        /// is what the reviver's mash pace earned (D29). Grace is this player's own.</summary>
        internal void ApplyRevive(float healthFraction) =>
            _condition = _condition.Revived(healthFraction, _reviveGraceSteps);

        /// <summary>
        /// The attempt-over sandbox reset (task 35): back to the spawn point, full health, clean
        /// combat state. A placeholder by design — the run lifecycle is mode-owned (D4, M7).
        /// </summary>
        internal void ResetForAttempt()
        {
            float maxHealth = _sheet.MaxHealth > 0f
                ? _sheet.MaxHealth
                : (_definition != null ? _definition.MaxHealth : 100f);
            _condition = PlayerCondition.Fresh(maxHealth);
            _mana = ManaPool.Full(_sheet.MaxMana);
            _combat = CombatState.Ready;
            _revive = ReviveChannel.Inactive;
            Statuses.Clear();
            _leapAvailable = true;
            _lungePerStep = Vector3.zero;
            _lungeStepsLeft = 0;
            _attackRooted = false;
            _state = MotorState.AtRest(_spawnPosition);
            _previous = _state;
            transform.position = _spawnPosition;
        }

        /// <summary>A partner's shove (D21): replaces velocity, never touches health.</summary>
        internal void ApplyImpulse(Vector3 velocity)
        {
            bool launched = velocity.y > 0.01f;
            _state = new MotorState(
                _state.Position,
                velocity,
                _state.Facing,
                launched ? false : _state.IsGrounded,
                launched ? _tuning.CoyoteSteps + 1 : _state.StepsSinceGrounded,
                _state.JumpBufferedFor);
        }

        /// <summary>
        /// The planar speed carried into the current swing, captured before any lunge zeroes it.
        /// Feeds the momentum-knockback bonus — the scripted snap itself never counts as momentum.
        /// </summary>
        internal Vector3 StrikeMomentum => _strikeMomentum;

        internal Vector3 Velocity => _state.Velocity;

        /// <summary>The crowding nudge (task 37): position only — velocity is never cushioned.</summary>
        internal void ApplySeparation(Vector3 push, in ArenaBounds bounds)
        {
            if (push.sqrMagnitude < 1e-12f)
            {
                return;
            }

            Vector3 position = bounds.ClampHorizontal(_state.Position + push);
            _state = new MotorState(
                position, _state.Velocity, _state.Facing, _state.IsGrounded,
                _state.StepsSinceGrounded, _state.JumpBufferedFor);
            transform.position = _state.Position;
        }

        private void BeginAttack(in PlayerCommand command, in AttackTuning attack)
        {
            _strikeMomentum = new Vector3(_state.Velocity.x, 0f, _state.Velocity.z);

            // The stick aims (D19). Chosen here; a free-moving whiff may still re-aim by moving.
            Facing facing = command.Move.x > 0.01f ? Facing.Right
                : command.Move.x < -0.01f ? Facing.Left
                : _state.Facing;

            _lungePerStep = Vector3.zero;
            _lungeStepsLeft = 0;
            _attackRooted = false;

            Vector3 velocity = _state.Velocity;
            if (!_state.IsGrounded)
            {
                // The air stall: the swing hangs, and recovery resumes the fall from zero.
                // The slam dives at full fall speed instead — the shadow marks where it ends.
                velocity.y = attack.ResolvesOnLanding ? -_tuning.MaxFallSpeed : 0f;
            }

            // The slam never lunges: its aim is the shadow mark (D14), not magnetism — and a
            // bow's Light is a shot, so it never scoots toward what it can hit from here.
            Vector3 target = default;
            bool hasTarget = !attack.ResolvesOnLanding
                && !(_weaponClass == WeaponClass.Bow && IsChainLight(attack))
                && _driver != null
                && _driver.TryPickLungeTarget(this, attack, out target);
            if (hasTarget)
            {
                Vector3 travel = HitResolver.LungeEnd(_state.Position, attack, target) - _state.Position;
                if (travel.sqrMagnitude >= 1e-4f)
                {
                    int steps = Mathf.Max(1, attack.StartupSteps);
                    _lungePerStep = travel / steps;
                    _lungeStepsLeft = steps;
                }

                // Grounded the snap roots; airborne it sheds carried momentum instead, so the
                // scoot pulls at the grounded rate over the grounded range — consistency over
                // overshoot (Michael's retest) — while drift-steering and the fall stay live.
                _attackRooted = _state.IsGrounded;
                velocity = Vector3.zero;
            }

            _state = new MotorState(
                _state.Position, velocity, facing, _state.IsGrounded, _state.StepsSinceGrounded,
                _state.JumpBufferedFor);
        }

        /// <summary>
        /// The air lunge: the grounded snap's travel applied over the free swing — velocity and
        /// stick drift stay live, so the scoot converts the hit without the rooted feel.
        /// </summary>
        private void StepAirLunge(in ArenaBounds bounds)
        {
            if (_lungeStepsLeft <= 0)
            {
                return;
            }

            _lungeStepsLeft -= 1;
            Vector3 position = bounds.ClampHorizontal(_state.Position + _lungePerStep);
            _state = new MotorState(
                position, _state.Velocity, _state.Facing, _state.IsGrounded,
                _state.StepsSinceGrounded, _state.JumpBufferedFor);
        }

        private void StepLungeMotion(in ArenaBounds bounds)
        {
            if (_lungeStepsLeft <= 0)
            {
                return;
            }

            _lungeStepsLeft -= 1;
            Vector3 position = bounds.ClampHorizontal(_state.Position + _lungePerStep);
            _state = new MotorState(
                position, Vector3.zero, _state.Facing, _state.IsGrounded, _state.StepsSinceGrounded,
                _state.JumpBufferedFor);
        }

        private static PlayerCommand WithoutAttacks(in PlayerCommand command) => new PlayerCommand(
            command.Frame,
            command.Move,
            command.Held & ~(CommandButtons.Light | CommandButtons.Heavy),
            command.Pressed & ~(CommandButtons.Light | CommandButtons.Heavy),
            command.Released & ~(CommandButtons.Light | CommandButtons.Heavy));

        private static PlayerCommand WithoutLight(in PlayerCommand command) => new PlayerCommand(
            command.Frame,
            command.Move,
            command.Held & ~CommandButtons.Light,
            command.Pressed & ~CommandButtons.Light,
            command.Released & ~CommandButtons.Light);

        private PlayerCommand CombatMove(in PlayerCommand command, AttackPhase phase)
        {
            float scale = phase == AttackPhase.Charging
                ? _kit.Heavy.MoveSpeedScale
                : _combat.CurrentAttack.MoveSpeedScale;
            return new PlayerCommand(
                command.Frame,
                command.Move * scale,
                command.Held & ~CommandButtons.Jump,
                command.Pressed & ~CommandButtons.Jump,
                command.Released & ~CommandButtons.Jump);
        }

        /// <summary>
        /// Rebuilds the cached sheet and everything derived from it — the swing-scaled kit, the
        /// speed-scaled tuning, the resized pools (planning decision 2: only on loadout or
        /// allocation changes, never per step). The driver calls it after an auto-equip grab; the
        /// debug panel after every mutation.
        /// </summary>
        internal void RefreshStats()
        {
            _gearScratch.Clear();
            _resistScratch.Clear();
            _infusion = ElementId.None;
            _infusionScale = 0f;
            BaseStats allocations = BaseStats.Zero;
            if (_bag != null)
            {
                Loadout loadout = _bag.Inventory.Loadout;
                loadout.CollectContributions(_gearScratch);
                loadout.CollectElementalResistances(_resistScratch);
                loadout.TryGetInfusion(out _infusion, out _infusionScale);
                allocations = _bag.Ledger.Allocations;
            }

            _sheet = StatSheet.Build(allocations, _statTuning, _gearScratch, _resistScratch);
            _damageScale = _statTuning.UnarmedDamage > 0f
                ? _sheet.WeaponDamage / _statTuning.UnarmedDamage
                : 1f;
            ItemInstance weapon = _bag != null ? _bag.Inventory.Loadout.Weapon : default;
            _weaponClass = weapon.IsEmpty ? WeaponClass.None : weapon.WeaponClass;
            _shotSpeed = weapon.ShotSpeed > 0f ? weapon.ShotSpeed : 12f;
            _activeKit = _kit.ScaledBySwingSpeed(_sheet.SwingSpeedMultiplier);
            _activeMagic = _magic.ScaledByGear(_sheet.MagicDamage, _sheet.MagicRange);
            _activeTuning = ScaledSpeed(_tuning, _sheet.NetMoveSpeedMultiplier);
            _condition = _condition.Resized(_sheet.MaxHealth);
            _mana = _mana.Resized(_sheet.MaxMana);
        }

        private bool IsChainLight(in AttackTuning attack)
        {
            for (int i = 0; i < _activeKit.ChainLength; i++)
            {
                if (_activeKit.StepAt(i).OnLight.Equals(attack))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The leap's boost: vertical speed is replaced rather than added, so the climb is the same
        /// whether it is spent rising or falling — the constant-promise rule D18 set for the base
        /// jump, kept for the layered one.
        /// </summary>
        private static MotorState WithLift(in MotorState state, float liftSpeed) => new MotorState(
            state.Position,
            new Vector3(state.Velocity.x, liftSpeed, state.Velocity.z),
            state.Facing,
            false,
            state.StepsSinceGrounded,
            0);

        private static MovementTuning ScaledSpeed(in MovementTuning tuning, float multiplier) => new MovementTuning(
            tuning.MaxSpeed * Mathf.Max(0.05f, multiplier),
            tuning.Acceleration,
            tuning.Deceleration,
            tuning.DepthSpeedScale,
            tuning.Gravity,
            tuning.JumpSpeed,
            tuning.MaxFallSpeed,
            tuning.CoyoteSteps,
            tuning.JumpBufferSteps);

        private static MovementTuning WithoutGravity(in MovementTuning tuning) => new MovementTuning(
            tuning.MaxSpeed,
            tuning.Acceleration,
            tuning.Deceleration,
            tuning.DepthSpeedScale,
            0f,
            tuning.JumpSpeed,
            tuning.MaxFallSpeed,
            tuning.CoyoteSteps,
            tuning.JumpBufferSteps);

        private void Awake()
        {
            _source = GetComponent<IPlayerCommandSource>();
            _tuning = _definition != null ? _definition.ToRuntime() : MovementTuning.Default;
            _kit = _definition != null ? _definition.CombatKitToRuntime() : CombatKit.Default;
            _statTuning = _definition != null ? _definition.ToStatTuning() : StatTuning.Default;
            _magic = _definition != null ? _definition.MagicKitToRuntime() : default;
            _element = _definition != null ? _definition.Element : ElementId.None;
            _activeKit = _kit;
            _activeMagic = _magic;
            _activeTuning = _tuning;
            _mana = ManaPool.Full(_statTuning.BaseMaxMana);
            _condition = PlayerCondition.Fresh(_definition != null ? _definition.MaxHealth : 100f);
            _hitStaggerSteps = _definition != null ? _definition.HitStaggerSteps : 15;
            _hitGraceSteps = _definition != null ? _definition.HitGraceSteps : 30;
            _reviveRequiredProgress = _definition != null ? _definition.ReviveRequiredProgress : 10f;
            _reviveBeatSteps = _definition != null ? _definition.ReviveBeatSteps : 45;
            _reviveRushSteps = _definition != null ? _definition.ReviveRushSteps : 15;
            _revivePumpDecaySteps = _definition != null ? _definition.RevivePumpDecaySteps : 60;
            _reviveMinHealthFraction = _definition != null ? _definition.ReviveMinHealthFraction : 0.25f;
            _reviveMaxHealthFraction = _definition != null ? _definition.ReviveMaxHealthFraction : 0.65f;
            _reviveRange = _definition != null ? _definition.ReviveRange : 1.8f;
            _reviveGraceSteps = _definition != null ? _definition.ReviveGraceSteps : 60;
            _quickUseCooldownSteps = _definition != null ? _definition.QuickUseCooldownSteps : 180;
            _state = MotorState.AtRest(transform.position);
            _previous = _state;
        }

        private void OnEnable()
        {
            if (!_spawnCaptured)
            {
                _spawnPosition = transform.position;
                _spawnCaptured = true;
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_driver == null)
            {
                Debug.LogError($"{name}: no SimulationDriver in the scene — this character will never move.", this);
                return;
            }

            _bag = GetComponent<PlayerInventory>();
            RefreshStats();
            _driver.Characters.Register(this);
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.Characters.Unregister(this);
            }
        }
    }
}
