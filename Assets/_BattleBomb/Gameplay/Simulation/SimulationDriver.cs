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
    public sealed class SimulationDriver : MonoBehaviour, IPlayerRegistryHost
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

        [Tooltip("Seed for elite spawn decisions and the gear they wear (D22). Its own stream " +
            "again, so an elite appearing never shifts what an ordinary kill would have dropped.")]
        [SerializeField] private int _spawnSeed = 3;

        [Tooltip("D23's story-progress multiplier on drop quality. Chapters own this from M7; " +
            "a scene value until then, exactly as the climate is. At 1 the whole ladder above " +
            "Rusty is unreachable, which makes the loot loop impossible to judge.")]
        [SerializeField] private float _lootProgress = 2f;

        [Tooltip("The authored ladder and drop-kind weights (D33). Empty runs Core's paper defaults.")]
        [SerializeField] private QualityLadder _qualityLadder;

        [Tooltip("Everything the generator may drop (D35). A new item is a new asset here, never code.")]
        [SerializeField] private ItemDefinition[] _itemCatalog;

        [Tooltip("Every element in play (D38). A new element is a new asset here — never code.")]
        [SerializeField] private ElementDefinition[] _elementCatalog;

        [Tooltip("This area's elemental climate (§4, D41) — it multiplies elemental damage from " +
            "every side, yours and theirs alike. Chapters own this from M7; a scene value until then.")]
        [SerializeField] private ElementMultiplierSpec[] _climateRows = new ElementMultiplierSpec[0];

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

        /// <summary>D23's bonus on a *boss* signature roll. Elites carry their own (EliteRules).</summary>
        private const float LootEliteBonus = 1.5f;

        /// <summary>The D36 level stamp's source — a constant until M7 builds real progress.</summary>
        private const int StoryProgressLevel = 1;

        /// <summary>A cast leaves the element's full mark; a weapon infusion is a rolled share of it.</summary>
        private const float CastSourceScale = 1f;

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
        private DeterministicRandom _spawnRng;

        /// <summary>Debug rolls within one frame must differ, or a grant hands out clones.</summary>
        private int _debugRollCounter;
        private readonly List<ItemSpec> _itemSpecs = new List<ItemSpec>();
        private QualityTable _qualityTable;
        private DropWeights _dropWeights;
        private ElementCatalog _elements = ElementCatalog.Empty;
        private ElementalMultipliers _climate = ElementalMultipliers.Neutral;

        /// <summary>D41's pair table. Empty through M5 — the pairs wait on the roster (O11).</summary>
        private ReactionTable _reactions = ReactionTable.Empty;

        /// <summary>D22's elite modifier, on Core's paper numbers until a chapter authors them.</summary>
        private readonly EliteRules _eliteRules = EliteRules.Default;

        /// <summary>What an elite can visibly wear, and therefore what it can drop (D22).</summary>
        private static readonly ItemSlot[] EliteArmorSlots =
        {
            ItemSlot.Helmet,
            ItemSlot.Chest,
            ItemSlot.Boots,
        };
        private readonly List<DropPickup> _pickups = new List<DropPickup>();
        private readonly Dictionary<int, int> _grabCounts = new Dictionary<int, int>();

        /// <summary>Steps a refused grab stays worth showing — long enough to read, short
        /// enough to feel like a rebuke (D43).</summary>
        private const int RefusalFlashSteps = 90;

        /// <summary>Per player, steps left on the "sack full" refusal (D43).</summary>
        private readonly Dictionary<int, int> _refusedGrabs = new Dictionary<int, int>();
        private readonly List<int> _refusalScratch = new List<int>();

        /// <summary>Chests and shopkeepers in the scene (D42), registered like every other actor.</summary>
        private readonly List<WorldInteractable> _interactables = new List<WorldInteractable>();

        /// <summary>Which screen each player has open right now, by player id (D42).</summary>
        private readonly Dictionary<int, InteractionKind> _openScreens =
            new Dictionary<int, InteractionKind>();
        private readonly List<int> _screenScratch = new List<int>();
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

        /// <summary>
        /// Debug only: rolls one authored definition at a quality floor, for the equip panel to
        /// hand over. It draws from its own stream so testing a weapon never shifts the loot the
        /// fight would have dropped. Dies with the debug panel when M6 builds the real UI.
        /// </summary>
        public ItemInstance RollDebugItem(int definitionId, float qualityScore)
        {
            _debugRollCounter++;
            var rng = new DeterministicRandom(
                (uint)(Frame * 2654435761u + 17u) + (uint)(_debugRollCounter * 2246822519u));
            var context = new GenerationContext(
                    qualityScore, StoryProgressLevel, _itemSpecs, _qualityTable, _dropWeights,
                    _elements.Ids)
                .WithSignature(definitionId, QualityRank.Nothing);
            ItemGenerator.Roll(rng, context, out ItemInstance item);
            return item;
        }

        /// <summary>
        /// The spawner's question at spawn time (D22): is this one an elite, and if so what is it
        /// wearing? Rolling the gear now — not at death — is what lets the body advertise its own
        /// reward, and both draws come off the spawn stream so an elite never shifts the loot an
        /// ordinary kill would have produced.
        /// </summary>
        internal bool RollEliteSpawn(out ItemInstance carried)
        {
            carried = default;
            _spawnRng = _eliteRules.Roll(_spawnRng, out bool isElite);
            if (!isElite)
            {
                return false;
            }

            _spawnRng = _spawnRng.NextFloat(out float spread);
            float quality = (DropRoll.SpreadMin + spread) * _lootProgress * _eliteRules.QualityBonus;

            // It drops what it *wears* (D22), so the roll is forced into an armor slot — a
            // consumable would make the visible-armor promise a lie.
            _spawnRng = _spawnRng.NextFloat(out float slotDraw);
            ItemSlot slot = EliteArmorSlots[Mathf.Min(
                EliteArmorSlots.Length - 1, (int)(slotDraw * EliteArmorSlots.Length))];

            var context = new GenerationContext(
                    quality, StoryProgressLevel, _itemSpecs, _qualityTable, _dropWeights,
                    _elements.Ids)
                .WithForcedSlot(slot);
            _spawnRng = ItemGenerator.Roll(_spawnRng, context, out carried);
            return true;
        }

        /// <summary>
        /// D44's gamble, run against the authored catalog. The driver owns the streams and the
        /// catalog, so the reroll is deterministic and the inventory stays a pure rulebook.
        /// </summary>
        internal void RunCombine(
            Inventory inventory, int firstIndex, int secondIndex, out CombineResult result)
        {
            var context = new GenerationContext(
                0f, StoryProgressLevel, _itemSpecs, _qualityTable, _dropWeights, _elements.Ids);
            _lootRng = inventory.TryCombine(_lootRng, firstIndex, secondIndex, context, out result);
        }

        /// <summary>
        /// The shopkeeper's rack (D43): a handful of generator-rolled pieces at current progress
        /// quality, plus the potions always in stock. Rolled fresh per visit from the loot
        /// stream, which is exactly what makes a shop worth walking back to.
        /// </summary>
        public void RollShopStock(List<ItemInstance> stock, int count)
        {
            stock.Clear();
            for (int i = 0; i < count; i++)
            {
                _lootRng = _lootRng.NextFloat(out float spread);
                var context = new GenerationContext(
                    (DropRoll.SpreadMin + spread) * _lootProgress, StoryProgressLevel,
                    _itemSpecs, _qualityTable, _dropWeights, _elements.Ids);
                _lootRng = ItemGenerator.Roll(_lootRng, context, out ItemInstance rolled);
                if (!rolled.IsEmpty)
                {
                    stock.Add(rolled);
                }
            }
        }

        /// <summary>
        /// Debug and tests only: puts a drop on the ground without a kill behind it. The smoke
        /// suite (D45) needs a drop to exist deterministically — a real kill only drops on a
        /// chance roll, and a tripwire that fires at random is not a tripwire.
        /// </summary>
        public void SpawnDebugDrop(Vector3 position, in ItemInstance item)
        {
            if (item.IsEmpty)
            {
                return;
            }

            _pickups.Add(DropPickup.Spawn(position, item));
        }

        /// <summary>Drops this player has grabbed (D23) — the HUD's proof the loop works.</summary>
        public int GrabCountFor(int playerIdValue) =>
            _grabCounts.TryGetValue(playerIdValue, out int count) ? count : 0;

        /// <summary>True while this player's last grab is still being refused (D43), for the
        /// red X and the cap flash to draw.</summary>
        public bool WasGrabRefused(int playerIdValue) =>
            _refusedGrabs.TryGetValue(playerIdValue, out int steps) && steps > 0;

        /// <summary>Drops waiting on the ground, for the inspect panel to read (D30).</summary>
        public IReadOnlyList<DropPickup> Pickups => _pickups;

        internal void RegisterInteractable(WorldInteractable interactable)
        {
            if (interactable != null && !_interactables.Contains(interactable))
            {
                _interactables.Add(interactable);
            }
        }

        internal void UnregisterInteractable(WorldInteractable interactable) =>
            _interactables.Remove(interactable);

        /// <summary>Raised when a player opens or closes a chest or shop screen (D42) — the UI
        /// listens, builds its half of the display, and never reaches into the simulation.</summary>
        public event Action<int, InteractionKind, bool> ScreenChanged;

        /// <summary>
        /// Raised on frames where a solo pause stopped the simulation but commands were still
        /// sampled. Open screens tick their navigation from this, so a paused menu still moves.
        /// </summary>
        public event Action MenuStepped;

        /// <summary>The screen this player has open, if any (D42).</summary>
        public bool TryGetOpenScreen(int playerIdValue, out InteractionKind kind) =>
            _openScreens.TryGetValue(playerIdValue, out kind);

        /// <summary>
        /// This player's sampled command for the current step. The chest screen navigates from
        /// this rather than from an EventSystem, so the menu obeys rule 3 like everything else:
        /// devices become commands exactly once, in one place, and two controllers driving two
        /// screen halves needs no extra machinery at all.
        /// </summary>
        public PlayerCommand CommandFor(int playerIdValue) =>
            _commands.TryGetValue(playerIdValue, out PlayerCommand command)
                ? command
                : PlayerCommand.Idle(Frame);

        public bool AnyScreenOpen => _openScreens.Count > 0;

        /// <summary>
        /// D42's per-mode rule: alone, opening a chest pauses the world, because there is nobody
        /// left to play it. In couch co-op it never pauses — the partner is still fighting, and
        /// the player at the chest simply stands there taking no orders.
        /// </summary>
        public bool PausedForScreen =>
            _menuPauseHolders > 0 || (Characters.Ordered.Count <= 1 && _openScreens.Count > 0);

        private int _menuPauseHolders;

        /// <summary>
        /// A global menu (the settings screen) stops the world for everyone, unlike a chest,
        /// which only stops it when there is nobody left to keep playing. Held as a count so two
        /// overlapping menus cannot un-pause each other.
        /// </summary>
        public void HoldMenuPause(bool held)
        {
            _menuPauseHolders = Mathf.Max(0, _menuPauseHolders + (held ? 1 : -1));
        }

        /// <summary>Opens a screen for this player. The UI draws it; the simulation only records
        /// that their hands are busy.</summary>
        public void OpenScreen(int playerIdValue, InteractionKind kind)
        {
            _openScreens[playerIdValue] = kind;
            ScreenChanged?.Invoke(playerIdValue, kind, true);
        }

        /// <summary>Closes it again — the UI's back button, or walking away from the chest.</summary>
        public void CloseScreen(int playerIdValue)
        {
            if (!_openScreens.TryGetValue(playerIdValue, out InteractionKind kind))
            {
                return;
            }

            _openScreens.Remove(playerIdValue);
            ScreenChanged?.Invoke(playerIdValue, kind, false);
        }

        /// <summary>Everyone out — the attempt reset cannot leave a menu open over a fresh run.</summary>
        private void CloseAllScreens()
        {
            if (_openScreens.Count == 0)
            {
                return;
            }

            _screenScratch.Clear();
            foreach (KeyValuePair<int, InteractionKind> entry in _openScreens)
            {
                _screenScratch.Add(entry.Key);
            }

            for (int i = 0; i < _screenScratch.Count; i++)
            {
                CloseScreen(_screenScratch[i]);
            }
        }

        /// <summary>Every authored element (D38) — the naming authority for UI and Presentation.</summary>
        public ElementCatalog Elements => _elements;

        /// <summary>This area's climate (D41): it scales elemental damage from every side.</summary>
        public ElementalMultipliers Climate => _climate;

        /// <summary>
        /// An element's authored colour, for Presentation to tint with (D38). A new element brings
        /// its own colour, so nothing downstream carries a palette that has to be kept in sync.
        /// </summary>
        public Color ColorOf(ElementId element)
        {
            if (_elementCatalog != null)
            {
                for (int i = 0; i < _elementCatalog.Length; i++)
                {
                    if (_elementCatalog[i] != null && _elementCatalog[i].Id == element)
                    {
                        return _elementCatalog[i].Color;
                    }
                }
            }

            return Color.white;
        }

        public PlayerRegistry Players => _players;

        /// <summary>The authored catalog as runtime specs — what a save restores items
        /// against (D52). Read-only: the driver builds it from the authored assets, and a
        /// loaded item that named a spec nobody authored has to fail loudly, not quietly
        /// extend the catalog.</summary>
        public IReadOnlyList<ItemSpec> ItemSpecs => _itemSpecs;

        public CharacterRegistry Characters { get; } = new CharacterRegistry();

        public TargetRegistry Targets { get; } = new TargetRegistry();

        /// <summary>Bolts in flight, for Presentation to draw. Simulated inside the fixed step.</summary>
        public IReadOnlyList<ProjectileState> Projectiles => _projectiles;

        private ArenaBounds _bounds = ArenaBounds.Default;
        private bool _boundsSet;

        /// <summary>The arena characters are clamped to. Set by the stage runner per arena and
        /// per gate (D48): widening it is how a gate opens. Falls back to the serialized volume
        /// for a bare scene, then to <see cref="ArenaBounds.Default"/>.</summary>
        public ArenaBounds Bounds
        {
            get
            {
                if (_boundsSet)
                {
                    return _bounds;
                }

                if (_arena != null)
                {
                    return _arena.ToRuntime();
                }

                if (!_warnedMissingArena)
                {
                    _warnedMissingArena = true;
                    Debug.LogWarning($"{name}: no arena set — using ArenaBounds.Default.", this);
                }

                return ArenaBounds.Default;
            }
        }

        internal void SetArena(in ArenaBounds bounds)
        {
            _bounds = bounds;
            _boundsSet = true;
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
            _spawnRng = new DeterministicRandom((uint)_spawnSeed);
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
            _elements = ElementDefinition.ToCatalog(_elementCatalog);
            _climate = ElementMultiplierSpec.ToTable(_climateRows);
        }

        private void Update()
        {
            if (PausedForScreen)
            {
                // Solo at a chest: the world stops (D42). The accumulator is deliberately not
                // fed, so no time banks up to be spent in a burst the moment the screen closes.
                // Commands are still sampled every frame — the menu is driven by them, and a
                // paused world with frozen input would be a menu nobody could use.
                SampleCommands(Frame);
                MenuStepped?.Invoke();
                return;
            }

            _clock.Accumulate(Time.deltaTime);

            while (_clock.TryConsumeStep(out int frame))
            {
                RunStep(frame);
            }
        }

        /// <summary>
        /// One fixed simulation step, as an ordered list of named phases. The order is the design:
        /// intent is sampled before anything moves, players act before the enemies who react to
        /// them, bodies separate only once every motor has run, and the world's bookkeeping —
        /// flight, death, loot, the attempt — settles before presentation is told the step happened.
        /// Adding a phase means adding a line here, in the place its ordering requires.
        /// </summary>
        private void RunStep(int frame)
        {
            IReadOnlyList<CharacterActor> actors = Characters.Ordered;
            ArenaBounds bounds = Bounds;

            SampleCommands(frame);
            StepPlayers(frame, actors, bounds);
            CapturePlayerSnapshot(actors);
            StepEnemies(frame, actors, bounds);
            SeparateBodies(actors, bounds);
            StepProjectiles(actors, StepDuration);
            StepStatuses(actors);
            ResolveDeaths();
            StepPickups();
            StepAttemptFlow(actors);
            Stepped?.Invoke(frame);
        }

        /// <summary>Every player's intent for this step — commands, never polling (D10).</summary>
        private void SampleCommands(int frame) => _players.SampleAll(frame, _commands);

        /// <summary>
        /// Each player acts on their command, in registry order (D10). Contextual Light is resolved
        /// here by handing the actor what is in reach — a downed partner, a drop — and applying
        /// what it reports back: one actor never reaches across and rewrites another.
        /// </summary>
        private void StepPlayers(int frame, IReadOnlyList<CharacterActor> actors, in ArenaBounds bounds)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                CharacterActor actor = actors[i];
                int playerId = actor.PlayerId.Value;
                PlayerCommand command = _commands.TryGetValue(playerId, out PlayerCommand sampled)
                    ? sampled
                    : PlayerCommand.Idle(frame);

                int interactable = FindInteractable(actor);
                bool screenOpen = _openScreens.ContainsKey(playerId);
                if (screenOpen)
                {
                    // Their hands are on the menu (D42): the body stands there taking no orders,
                    // exactly like a staggered player, while physics still applies.
                    command = PlayerCommand.Idle(frame);

                    // Walking away is impossible while idle, but the chest can vanish — an
                    // attempt reset, a despawn — and a screen with no chest under it would strand
                    // the player in a menu they cannot leave.
                    if (interactable < 0)
                    {
                        CloseScreen(playerId);
                        screenOpen = false;
                    }
                }

                int grabTarget = FindGrabTarget(actor);
                ActorStepResult result = actor.Step(
                    frame, command, bounds, StepDuration,
                    FindReviveTarget(actors, i), grabTarget >= 0,
                    !screenOpen && interactable >= 0);

                if (result.OpenedInteractable && interactable >= 0 && interactable < _interactables.Count)
                {
                    OpenScreen(playerId, _interactables[interactable].Kind);
                }
                if (result.RevivedPartner >= 0 && result.RevivedPartner < actors.Count)
                {
                    actors[result.RevivedPartner].ApplyRevive(result.ReviveFraction);
                }

                if (result.GrabbedLoot && grabTarget >= 0 && grabTarget < _pickups.Count
                    && _pickups[grabTarget] != null)
                {
                    PlayerInventory bag = actor.GetComponent<PlayerInventory>();
                    AddResult taken = bag != null ? bag.Take(_pickups[grabTarget].Item) : AddResult.Refused;
                    if (taken.Taken)
                    {
                        int id = actor.PlayerId.Value;
                        _grabCounts[id] = GrabCountFor(id) + 1;
                        Destroy(_pickups[grabTarget].gameObject);
                        _pickups.RemoveAt(grabTarget);
                    }
                    else
                    {
                        // A full sack has no overflow valve (D43): the drop stays where it lies
                        // and the refusal becomes presentation's red X.
                        _refusedGrabs[actor.PlayerId.Value] = RefusalFlashSteps;
                    }
                }
            }
        }

        /// <summary>
        /// What the enemies may know this step: player positions and who is down. Taken once, after
        /// the players have moved, and read by every phase that follows — enemy perception, their
        /// melee resolution, and the attempt countdown all see the same picture of the step.
        /// </summary>
        private void CapturePlayerSnapshot(IReadOnlyList<CharacterActor> actors)
        {
            _playerPositions.Clear();
            _playerDowned.Clear();
            for (int i = 0; i < actors.Count; i++)
            {
                _playerPositions.Add(actors[i].Position);
                _playerDowned.Add(actors[i].Condition.IsDown);
            }
        }

        /// <summary>
        /// Every enemy acts, under D28's turn-taking: count who already holds each player's melee
        /// attack token, then walk the registry order handing out what is left — melee without a
        /// token hovers and circles instead of queueing up on the player's face. Brutes never wait.
        /// Dummies are stepped here too; they are tuning props on the same clock.
        /// </summary>
        private void StepEnemies(int frame, IReadOnlyList<CharacterActor> actors, in ArenaBounds bounds)
        {
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
        }

        /// <summary>
        /// Every burning thing pays its tick (D40), players and enemies alike. Ticks land before
        /// deaths resolve, so a burn can be the killing blow. They deal damage and nothing else:
        /// no stagger, no interrupt — a status that broke the player's rhythm would be a
        /// punishment the combat model never agreed to.
        /// </summary>
        private void StepStatuses(IReadOnlyList<CharacterActor> actors)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                float damage = actors[i].Statuses.Step();
                if (damage > 0f)
                {
                    float landed = actors[i].ApplyStatusDamage(damage);
                    if (landed > 0f)
                    {
                        HitLanded?.Invoke(new HitEvent(
                            null, actors[i], landed, actors[i].Position, false, false, true));
                    }
                }
            }

            IReadOnlyList<ISimTarget> targets = Targets.Ordered;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!(targets[i] is EnemyActor enemy) || enemy.IsDepleted)
                {
                    continue;
                }

                float damage = enemy.Statuses.Step();
                if (damage > 0f)
                {
                    enemy.ApplyStatusDamage(damage);
                    HitLanded?.Invoke(new HitEvent(
                        null, enemy, damage, enemy.Position, false, false, true));
                }
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
                EnemyDied?.Invoke(new EnemyDeath(spec.Rank, spec.XpReward, enemy.IsElite, enemy.Position));

                // The kill's XP (task 48, planning decision 3): every living player earns the
                // full reward — co-op never punishes the reviver. Downed players earn nothing.
                IReadOnlyList<CharacterActor> earners = Characters.Ordered;
                for (int p = 0; p < earners.Count; p++)
                {
                    if (earners[p].Condition.IsDown)
                    {
                        continue;
                    }

                    PlayerInventory ledger = earners[p].GetComponent<PlayerInventory>();
                    if (ledger != null)
                    {
                        ledger.Earn(spec.XpReward);
                    }
                }

                if (enemy.IsElite && !enemy.CarriedDrop.IsEmpty)
                {
                    // It drops what it wears (D22) — the piece was rolled at spawn and has been
                    // tinting its armor ever since, so the kill owes exactly that item.
                    _pickups.Add(DropPickup.Spawn(enemy.Position, enemy.CarriedDrop));
                }
                else
                {
                    _lootRng = DropRoll.Roll(
                        _lootRng, spec.Rank, _lootProgress, 1f, 1f, false, LootEliteBonus,
                        out DropDecision drop);
                    if (drop.Dropped)
                    {
                        var context = new GenerationContext(
                            drop.Quality, StoryProgressLevel, _itemSpecs, _qualityTable, _dropWeights,
                            _elements.Ids);
                        _lootRng = ItemGenerator.Roll(_lootRng, context, out ItemInstance item);
                        if (!item.IsEmpty)
                        {
                            _pickups.Add(DropPickup.Spawn(enemy.Position, item));
                        }
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

        /// <summary>
        /// The chest or shopkeeper this player is standing at (D42), or -1. Each carries its own
        /// reach, so a big shop counter can be more welcoming than a small chest.
        /// </summary>
        private int FindInteractable(CharacterActor player)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < _interactables.Count; i++)
            {
                if (_interactables[i] == null)
                {
                    continue;
                }

                Vector3 to = _interactables[i].Position - player.Position;
                to.y = 0f;
                float sq = to.sqrMagnitude;
                float radius = _interactables[i].Radius;
                if (sq <= radius * radius && sq < bestSq)
                {
                    best = i;
                    bestSq = sq;
                }
            }

            return best;
        }

        /// <summary>Drops whose token was destroyed this step leave the list.</summary>
        private void StepPickups()
        {
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                if (_pickups[i] == null)
                {
                    _pickups.RemoveAt(i);
                }
            }

            if (_refusedGrabs.Count > 0)
            {
                _refusalScratch.Clear();
                foreach (KeyValuePair<int, int> entry in _refusedGrabs)
                {
                    if (entry.Value > 0)
                    {
                        _refusalScratch.Add(entry.Key);
                    }
                }

                for (int i = 0; i < _refusalScratch.Count; i++)
                {
                    _refusedGrabs[_refusalScratch[i]] -= 1;
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
            _refusedGrabs.Clear();
            CloseAllScreens();
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
                        attacker, attack, enemy.Position, enemy.Defence, out bool crit);
                    enemy.ApplyHit(hit);
                    enemy.ApplyStun(attack.StunSteps);
                    StealLife(attacker, hit.Damage);
                    attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                    HitLanded?.Invoke(new HitEvent(attacker, enemy, hit.Damage, enemy.Position, false, crit));
                    ApplyElementalRider(
                        attacker.Infusion, attacker.InfusionScale, enemy.Statuses, enemy.Defence,
                        hit.Damage, enemy, enemy.Position);
                }
                else if (owner is TrainingDummy dummy)
                {
                    HitResult hit = RollPlayerHit(
                        attacker, attack, dummy.Position, ElementalDefence.None, out bool crit);
                    dummy.ApplyHit(hit);
                    StealLife(attacker, hit.Damage);
                    attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                    HitLanded?.Invoke(new HitEvent(attacker, dummy, hit.Damage, dummy.Position, false, crit));
                }
                else if (owner is CharacterActor partner)
                {
                    HitResult shove = HitApplication.Apply(
                        attack, attacker.Position, attacker.Facing, attacker.StrikeMomentum,
                        ElementId.None, 1f, TargetKind.Partner, partner.Position,
                        ElementalDefence.None, ElementalMultipliers.Neutral);
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
        /// A cast's hit window (D39). It uses the same reach geometry a swing does — the splash's
        /// line in front, the aura's circle around the caster — but the damage is magic: the cast's
        /// own number plus gear, never the weapon's, and never Strength (D32). The caster's element
        /// rides every hit, so a cast marks and reacts exactly like an infused weapon does.
        /// </summary>
        internal void ResolveCast(CharacterActor caster, in AttackTuning cast, MagicCastKind kind)
        {
            CollectCandidates(caster, includePartners: true);
            if (cast.IsRadial)
            {
                HitResolver.ResolveRadial(caster.Position, cast, _candidatePositions, _hitIndices);
            }
            else
            {
                HitResolver.Resolve(caster.Position, caster.Facing, cast, _candidatePositions, _hitIndices);
            }

            ElementId element = caster.Element;
            int casterHitstop = 0;
            for (int i = 0; i < _hitIndices.Count; i++)
            {
                Component owner = _candidateOwners[_hitIndices[i]];
                if (owner is CharacterActor partner)
                {
                    // D21 holds for magic too: a partner caught in the blast is shoved, never
                    // burned and never hurt. No damage resolves, so no number appears.
                    HitResult shove = HitApplication.Apply(
                        cast, caster.Position, caster.Facing, caster.StrikeMomentum,
                        ElementId.None, 1f, TargetKind.Partner, partner.Position,
                        ElementalDefence.None, ElementalMultipliers.Neutral);
                    partner.ApplyImpulse(shove.Impulse);
                    HitLanded?.Invoke(new HitEvent(caster, partner, 0f, partner.Position, true));
                    continue;
                }

                ISimTarget target = owner as ISimTarget;
                EnemyActor enemy = owner as EnemyActor;
                TrainingDummy dummy = owner as TrainingDummy;
                if (target == null)
                {
                    continue;
                }

                ElementalDefence defence = enemy != null ? enemy.Defence : ElementalDefence.None;
                _combatRng = _combatRng.NextFloat(out float critDraw);
                bool crit = critDraw < caster.Sheet.CritChance;
                float gear = crit ? caster.Sheet.CritDamageMultiplier : 1f;

                HitResult hit = HitApplication.Apply(
                    cast, caster.Position, caster.Facing, caster.StrikeMomentum,
                    element, gear, TargetKind.Enemy, target.Position, defence, _climate);
                float knockback = caster.Sheet.KnockbackMultiplier;
                if (!Mathf.Approximately(knockback, 1f))
                {
                    hit = new HitResult(hit.Damage, hit.Impulse * knockback, hit.HitstopSteps);
                }

                if (enemy != null)
                {
                    enemy.ApplyHit(hit);
                    enemy.ApplyStun(cast.StunSteps);
                }
                else
                {
                    dummy.ApplyHit(hit);
                }

                StealLife(caster, hit.Damage);
                casterHitstop = Mathf.Max(casterHitstop, hit.HitstopSteps);
                HitLanded?.Invoke(new HitEvent(caster, owner, hit.Damage, target.Position, false, crit));
                ApplyElementalRider(
                    element, CastSourceScale, enemy != null ? enemy.Statuses : null, defence,
                    hit.Damage, owner, target.Position);
            }

            if (casterHitstop > 0)
            {
                caster.ApplyHitstop(casterHitstop);
            }
        }

        /// <summary>
        /// An equipment active's burst (D37): a radial hit around the wearer whose damage is a
        /// share of their weapon damage — derived, never its own number, so it scales with the
        /// build and can never outgrow it (D19's surviving guard). It carries the item's element,
        /// so it marks and reacts like every other elemental hit.
        /// </summary>
        internal void ResolveEquipmentActive(CharacterActor wearer, in QuickUseResult active)
        {
            float damage = wearer.Sheet.WeaponDamage * active.ActiveWeaponDamageShare;
            var burst = new AttackTuning(
                startupSteps: 0, activeSteps: 1, recoverySteps: 0,
                damage: damage, reachX: active.ActiveRadius, depthTolerance: active.ActiveRadius,
                lungeDistance: 0f, maxTargets: 8, knockbackSpeed: 5f, launchSpeed: 0f,
                hitstopSteps: 2, moveSpeedScale: 1f, resolvesOnLanding: false, isRadial: true);

            CollectCandidates(wearer, includePartners: false);
            HitResolver.ResolveRadial(wearer.Position, burst, _candidatePositions, _hitIndices);

            for (int i = 0; i < _hitIndices.Count; i++)
            {
                Component owner = _candidateOwners[_hitIndices[i]];
                EnemyActor enemy = owner as EnemyActor;
                ISimTarget target = owner as ISimTarget;
                if (target == null)
                {
                    continue;
                }

                ElementalDefence defence = enemy != null ? enemy.Defence : ElementalDefence.None;
                HitResult hit = HitApplication.Apply(
                    burst, wearer.Position, wearer.Facing, Vector3.zero,
                    active.ActiveElement, 1f, TargetKind.Enemy, target.Position, defence, _climate);

                if (enemy != null)
                {
                    enemy.ApplyHit(hit);
                }
                else if (owner is TrainingDummy dummy)
                {
                    dummy.ApplyHit(hit);
                }

                HitLanded?.Invoke(new HitEvent(wearer, owner, hit.Damage, target.Position, false));
                ApplyElementalRider(
                    active.ActiveElement, CastSourceScale, enemy != null ? enemy.Statuses : null,
                    defence, hit.Damage, owner, target.Position);
            }
        }

        /// <summary>
        /// One player hit through the M4 build (D32/D35): the weapon's damage scale feeds the
        /// pipeline's gear slot, the combat stream prices the crit — always drawn, so the stream
        /// never depends on the chance — and knockback affixes scale the shove.
        /// </summary>
        private HitResult RollPlayerHit(
            CharacterActor attacker, in AttackTuning attack, Vector3 targetPosition,
            in ElementalDefence defence, out bool crit)
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
                ElementId.None, gear, TargetKind.Enemy, targetPosition,
                defence, _climate);

            float knockback = attacker.Sheet.KnockbackMultiplier;
            return Mathf.Approximately(knockback, 1f)
                ? hit
                : new HitResult(hit.Damage, hit.Impulse * knockback, hit.HitstopSteps);
        }

        /// <summary>
        /// The elemental half of a landed hit (D19/D40/D41): the weapon's infusion, or a cast's
        /// own element, leaves its mark and may set off a reaction with whatever was already
        /// burning. Priced from what actually landed, so a resisted hit leaves a weaker mark.
        /// </summary>
        private void ApplyElementalRider(
            ElementId element, float sourceScale, StatusTrack statuses,
            in ElementalDefence defence, float landedDamage, Component target, Vector3 position)
        {
            if (statuses == null || element.IsNone || sourceScale <= 0f || landedDamage <= 0f
                || !_elements.TryGet(element, out ElementSpec spec))
            {
                return;
            }

            ElementalStrikeResult result = ElementalStrike.Apply(
                statuses, spec, defence.Element, _reactions, landedDamage, sourceScale,
                defence.Resistance.For(element));

            if (!result.Reacted)
            {
                return;
            }

            float burst = 0f;
            if (target is EnemyActor enemy)
            {
                enemy.ApplyStatusDamage(result.BurstDamage);
                enemy.ApplyStun(result.StunSteps);
                burst = result.BurstDamage;
            }
            else if (target is CharacterActor player)
            {
                burst = player.ApplyStatusDamage(result.BurstDamage);
                player.ApplyStun(result.StunSteps);
            }

            if (burst > 0f)
            {
                HitLanded?.Invoke(new HitEvent(null, target, burst, position, false, false, true));
            }
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
                    victim.Defence, _climate);
                float landed = victim.ApplyEnemyHit(hit);
                victim.ApplyStun(attack.StunSteps);
                attackerHitstop = Mathf.Max(attackerHitstop, hit.HitstopSteps);
                HitLanded?.Invoke(new HitEvent(attacker, victim, landed, victim.Position, false));
                ApplyElementalRider(
                    attacker.Spec.Tuning.Element, CastSourceScale, victim.Statuses, victim.Defence,
                    landed, victim, victim.Position);
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
                shooter.Position, target, shooter.ShotSpeed, attack.Damage, ElementId.None,
                ProjectileLifeSteps, shooter.PlayerId.Value));
        }

        /// <summary>
        /// A projectile cast's bolt (D46 — Ice): fired straight down the caster's lane, never
        /// aimed across depth, so free depth crossing stays the bow's identity (§2.2). The bolt
        /// carries the caster's element, which is also how impact pricing knows it is magic.
        /// </summary>
        internal void SpawnCastBolt(CharacterActor caster, in AttackTuning cast, float speed)
        {
            float sign = caster.Facing == Facing.Right ? 1f : -1f;
            Vector3 target = caster.Position + new Vector3(sign * PlayerShotAimRangeX, 0f, 0f);
            _projectiles.Add(ProjectileState.Fired(
                caster.Position, target, speed, cast.Damage, caster.Element,
                ProjectileLifeSteps, caster.PlayerId.Value));
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
                        p.Damage, p.Element, victim.Defence, _climate, 1f);
                    Vector3 direction = new Vector3(p.Velocity.x, 0f, p.Velocity.z);
                    direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.right;
                    float landed = victim.ApplyEnemyHit(new HitResult(
                        damage, direction * ProjectileKnockback, ProjectileHitstop));
                    HitLanded?.Invoke(new HitEvent(null, victim, landed, victim.Position, false));

                    // D40's other direction: a caster's bolt marks the player it lands on.
                    ApplyElementalRider(
                        p.Element, CastSourceScale, victim.Statuses, victim.Defence,
                        landed, victim, victim.Position);
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

            // A bolt with its own element is a cast (D46): its damage was priced as magic when
            // the kit was built, so the weapon's damage scale never touches it — only the crit.
            bool isCast = !p.Element.IsNone;
            float gear = 1f;
            float knockback = 1f;
            bool crit = false;
            if (shooter != null)
            {
                gear = isCast ? 1f : shooter.DamageScale;
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
                    p.Damage, p.Element, enemy.Defence, _climate, gear);
                enemy.ApplyHit(new HitResult(damage, impulse, PlayerShotHitstop));
                if (shooter != null)
                {
                    StealLife(shooter, damage);
                }

                HitLanded?.Invoke(new HitEvent(shooter, enemy, damage, enemy.Position, false, crit));
                if (isCast)
                {
                    // The bolt marks with its own element, priced as a cast — Chill lands here.
                    ApplyElementalRider(
                        p.Element, CastSourceScale, enemy.Statuses, enemy.Defence,
                        damage, enemy, enemy.Position);
                }
                else if (shooter != null)
                {
                    // An arrow carries the bow's infusion the same way a swing carries a sword's.
                    ApplyElementalRider(
                        shooter.Infusion, shooter.InfusionScale, enemy.Statuses, enemy.Defence,
                        damage, enemy, enemy.Position);
                }

                return true;
            }

            if (owner is TrainingDummy dummy)
            {
                float damage = DamageCalculator.Resolve(
                    p.Damage, p.Element, ElementalDefence.None, _climate, gear);
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
