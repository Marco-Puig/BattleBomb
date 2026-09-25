using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Items;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D45's tripwire, and the reason it exists: M4 and M5 each shipped a bug that a green
    /// EditMode suite could not see, because the break was in Gameplay wiring rather than in
    /// logic. M5's elemental leap was visibly dead in the game while 401 tests passed.
    ///
    /// This boots the real scene and walks the whole loop through the real command pipe — kill,
    /// drop, grab, chest, equip, sell — asserting only that each link actually connects. It is
    /// not a feel check and never will be; that is Michael's job (his standing rule). It answers
    /// exactly one question: is anything completely broken?
    /// </summary>
    public sealed class LootLoopSmokeTests
    {
        /// <summary>The real gameplay scene, loaded by name — it is in the build settings, so
        /// this works in a player too and needs no editor-only API.</summary>
        private const string SceneName = "Gameplay";

        /// <summary>Simulation steps any single link may take: fifteen seconds of game time.</summary>
        private const int PatienceSteps = 900;

        /// <summary>A hard ceiling on render frames so a stalled simulation cannot hang the run.</summary>
        private const int FrameCeiling = 20000;

        /// <summary>The starter knife — something wearable, so the equip link has a subject.</summary>
        private const int KnifeDefinitionId = 7;

        /// <summary>What a real X or J press carries: the fight's Light and the menu's Option at
        /// once (D57). Opening the chest with it proves the opening press cannot sell anything.</summary>
        private const CommandButtons OpenPress = CommandButtons.Light | CommandButtons.Option;

        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _player;
        private PlayerInventory _bag;
        private ScriptedCommandSource _input;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);

            // Two frames: one for the load to take, one for every actor's OnEnable to register.
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            Assert.That(_driver, Is.Not.Null, "The gameplay scene has no SimulationDriver.");

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            Assert.That(actors.Count, Is.GreaterThan(0), "No players registered with the driver.");
            _player = actors[0];
            _bag = _player.GetComponent<PlayerInventory>();
            Assert.That(_bag, Is.Not.Null, "Player one has no inventory.");

            // Take the device out of the loop and drive the same interface by hand.
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            _driver.Players.Unregister(_player.PlayerId);
            _input = _player.gameObject.AddComponent<ScriptedCommandSource>();
            _input.Bind(_player.PlayerId.Value);
            _driver.Players.Register(_input);

            // A quiet arena. This suite is a wiring tripwire, not a fight: an encounter running
            // underneath it would stagger the player mid-press and turn a real failure and a
            // stray grunt into the same red. With spawns off, the stage run still announces
            // its waves and counts them cleared, so the gate opens on schedule (D48) and the
            // chest in the room beyond it is reachable.
            _runner = Object.FindAnyObjectByType<StageRunner>();
            Assert.That(_runner, Is.Not.Null, "The gameplay scene has no StageRunner.");
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in
                Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return Until(() => _runner.IsStageLoaded, "the fixture stage never streamed in");
            yield return Until(() => _runner.Phase == StagePhase.GateOpen, "the first arena's gate never opened");
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            if (_input != null)
            {
                _input.Release();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator The_whole_loop_connects_end_to_end()
        {
            // ── A drop exists to be taken ────────────────────────────────────────────
            Vector3 where = _player.Position + new Vector3(1.5f, 0f, 0f);
            _driver.SpawnDebugDrop(where, _driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            yield return null;

            Assert.That(_driver.Pickups.Count, Is.GreaterThan(0), "The debug drop never landed.");

            Vector3 drop = _driver.Pickups[0].Position;
            int slotsBefore = _bag.Inventory.SlotsUsed;

            // ── Walk to it and take it with contextual Light (D30) ───────────────────
            yield return WalkTo(drop, "the drop");
            yield return Press(CommandButtons.Light);
            yield return Until(
                () => _bag.Inventory.SlotsUsed > slotsBefore,
                "Light beside a drop did not put anything in the sack");

            // ── Walk to the chest and open it (D42) ──────────────────────────────────
            WorldInteractable chest = FindChest();
            Assert.That(chest, Is.Not.Null, "The scene has no chest to open.");

            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(
                () => _driver.TryGetOpenScreen(_player.PlayerId.Value, out InteractionKind kind)
                    && kind == InteractionKind.Chest,
                "Light beside the chest did not open the chest screen");

            // ── Equip something out of the sack ──────────────────────────────────────
            int wearable = FindWearable();
            Assert.That(wearable, Is.GreaterThanOrEqualTo(0),
                "Nothing in the sack could be worn, so the equip link cannot be tested.");

            ItemSlot slot = _bag.Inventory.Items[wearable].Item.Slot;
            Assert.That(_bag.RequestEquip(wearable), Is.True, "The equip request was refused.");
            Assert.That(_bag.Inventory.Loadout.Worn(slot).IsEmpty, Is.False,
                "The equip request succeeded but nothing is being worn.");

            // The sheet must have rebuilt itself off the changed event — no caller asked it to.
            Assert.That(_player.Sheet.MaxHealth, Is.GreaterThan(0f),
                "The stat sheet is empty after equipping, so the changed event never landed.");

            // ── Sell something, and see the money arrive ─────────────────────────────
            int sellable = FindSellable();
            if (sellable >= 0)
            {
                int coinsBefore = _bag.Wallet.Balance;
                int paid = _bag.RequestSell(sellable);
                Assert.That(paid, Is.GreaterThan(0), "Selling paid nothing.");
                Assert.That(_bag.Wallet.Balance, Is.EqualTo(coinsBefore + paid),
                    "The sale's coins never reached the wallet.");
            }

            // ── Close it again ───────────────────────────────────────────────────────
            _driver.CloseScreen(_player.PlayerId.Value);
            Assert.That(_driver.TryGetOpenScreen(_player.PlayerId.Value, out _), Is.False,
                "The chest screen would not close.");
        }

        [UnityTest]
        public IEnumerator A_player_at_a_chest_takes_no_orders()
        {
            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(
                () => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            Vector3 before = _player.Position;
            _input.Set(new Vector2(1f, 0f), CommandButtons.None);
            yield return SimulationFrames(60);
            _input.Release();
            Assert.That(Vector3.Distance(before, _player.Position), Is.LessThan(0.05f),
                "A player browsing a chest walked away while their hands were on the menu (D42).");
        }

        /// <summary>
        /// Every route out of the chest screen, because M6's pass found a screen a player could
        /// not leave: the logic was fine and nothing had ever exercised it. A menu you can enter
        /// and not exit is worse than one that never opens, so every route is pinned.
        /// </summary>
        [UnityTest]
        public IEnumerator Every_way_out_of_the_chest_screen_works(
            [Values("start", "back", "escape", "close button")] string route)
        {
            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(
                () => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            switch (route)
            {
                case "start":
                    yield return Press(CommandButtons.Pause);
                    break;

                case "back":
                    yield return Press(CommandButtons.Back);
                    break;

                case "escape":
                    yield return Press(CommandButtons.Back | CommandButtons.Pause);
                    break;

                default:
                    Button close = FindCloseButton();
                    Assert.That(close, Is.Not.Null,
                        "The chest screen has no close button — a pointer or a tap has no way out.");
                    close.onClick.Invoke();
                    yield return null;
                    break;
            }

            Assert.That(_driver.TryGetOpenScreen(_player.PlayerId.Value, out _), Is.False,
                $"'{route}' did not close the chest screen.");
        }

        [UnityTest]
        public IEnumerator The_press_that_opens_the_chest_sells_nothing()
        {
            yield return TakeAKnife();
            int slots = _bag.Inventory.SlotsUsed;
            int coins = _bag.Wallet.Balance;

            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");
            yield return SimulationFrames(10);

            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(slots),
                "Opening the chest sold something: X is Light to the fight and Option to the screen.");
            Assert.That(_bag.Wallet.Balance, Is.EqualTo(coins));
        }

        [UnityTest]
        public IEnumerator X_sells_and_Y_locks_the_item_under_the_cursor()
        {
            yield return TakeAKnife();

            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            int slots = _bag.Inventory.SlotsUsed;
            yield return Press(CommandButtons.Lock);
            Assert.That(_bag.Inventory.Items[0].Item.Locked, Is.True, "Y did not lock the item.");

            yield return Press(CommandButtons.Option);
            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(slots), "X sold a locked item.");

            yield return Press(CommandButtons.Lock);
            Assert.That(_bag.Inventory.Items[0].Item.Locked, Is.False, "Y did not release it.");

            int coins = _bag.Wallet.Balance;
            yield return Press(CommandButtons.Option);
            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(slots - 1), "X did not sell the item.");
            Assert.That(_bag.Wallet.Balance, Is.GreaterThan(coins), "The sale paid nothing.");
        }

        [UnityTest]
        public IEnumerator A_stick_held_through_the_opening_press_does_not_move_the_cursor()
        {
            yield return TakeAKnife();

            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");

            // Still pushing up as X opens the chest, and still pushing after X comes up. Up, because
            // from the knife's row it is the filter row; with a partner the hero is a tab, not a
            // neighbour, so pushing right from a one-item sack would have nowhere to go.
            _input.Set(Vector2.up, OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");
            _input.Set(Vector2.up, CommandButtons.None);
            yield return SimulationFrames(40);
            _input.Release();
            yield return SimulationFrames(2);

            yield return Press(CommandButtons.Lock);
            Assert.That(_bag.Inventory.Items[0].Item.Locked, Is.True,
                "The cursor left the knife: a stick held while opening the chest counted as a push.");
        }

        [UnityTest]
        public IEnumerator Escape_backs_out_one_level_and_Start_leaves_from_anywhere()
        {
            yield return TakeAKnife();

            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            yield return Press(CommandButtons.Confirm);
            yield return Press(CommandButtons.Back | CommandButtons.Pause);
            Assert.That(_driver.TryGetOpenScreen(_player.PlayerId.Value, out _), Is.True,
                "Escape inside the knife's menu closed the whole chest; it backs out one level (D57).");

            // Y acts only on the sack's grid, so a lock here proves Escape landed exactly there.
            yield return Press(CommandButtons.Lock);
            Assert.That(_bag.Inventory.Items[0].Item.Locked, Is.True,
                "Escape did not come back out of the knife's menu to the sack.");

            yield return Press(CommandButtons.Confirm);
            yield return Press(CommandButtons.Pause);
            yield return Until(() => !_driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "Start inside the knife's menu did not leave the chest");
        }

        /// <summary>Drops the starter knife beside the player and grabs it, so the sack holds exactly
        /// one known item in cell 0.</summary>
        private IEnumerator TakeAKnife()
        {
            int before = _bag.Inventory.SlotsUsed;
            Vector3 where = _player.Position + new Vector3(1.5f, 0f, 0f);
            _driver.SpawnDebugDrop(where, _driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            yield return null;
            yield return WalkTo(where, "the knife");
            yield return Press(CommandButtons.Light);
            yield return Until(() => _bag.Inventory.SlotsUsed > before, "the knife was never taken");
            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(1),
                "The sack did not start empty, so the knife is not the only item in cell 0.");
        }

        private static Button FindCloseButton()
        {
            foreach (Button candidate in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                if (candidate.name == "Close")
                {
                    return candidate;
                }
            }

            return null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// The nearest chest, not the first one the engine happens to list. A stage carries one
        /// per checkpoint room now (D48), and only the one in the room past this arena's gate is
        /// inside the clamp — walking at any other is a walk into a wall.
        /// </summary>
        private WorldInteractable FindChest()
        {
            WorldInteractable nearest = null;
            float best = float.MaxValue;
            foreach (WorldInteractable candidate in
                Object.FindObjectsByType<WorldInteractable>(FindObjectsInactive.Exclude))
            {
                if (candidate.Kind != InteractionKind.Chest)
                {
                    continue;
                }

                float distance = Mathf.Abs(candidate.Position.x - _player.Position.x);
                if (distance < best)
                {
                    best = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private int FindWearable()
        {
            IReadOnlyList<ItemStack> items = _bag.Inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (!items[i].Item.IsConsumable && items[i].Item.RequiredLevel <= _bag.Level)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSellable()
        {
            IReadOnlyList<ItemStack> items = _bag.Inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (!items[i].Item.Locked)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// One deliberate press, held long enough for the simulation to actually see it.
        ///
        /// Paced by <see cref="SimulationDriver.Frame"/> rather than by render frames, and that
        /// distinction is the whole trick: a test run renders far faster than the fixed 60 Hz
        /// step, so a button set and released across two <c>yield return null</c>s can live and
        /// die entirely inside one simulation step and never be sampled at all. Counting steps
        /// makes the press real regardless of how fast the machine is.
        /// </summary>
        private IEnumerator Press(CommandButtons button)
        {
            _input.Set(Vector2.zero, button);
            yield return SimulationFrames(3);
            _input.Release();
            yield return SimulationFrames(2);
        }

        /// <summary>
        /// Waits until the simulation has advanced this many fixed steps.
        ///
        /// A global menu stops the world (D42), and it stops the step clock with it — the Pause
        /// route out of the chest opens exactly such a menu. Waiting for steps that are never
        /// coming is what turns a five-second check into a three-minute one, so a paused driver
        /// ends the wait. The command that caused the pause was already sampled to cause it.
        /// </summary>
        private IEnumerator SimulationFrames(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < target; guard++)
            {
                yield return null;
                if (_driver.PausedForScreen)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Walks the player onto a spot by steering, not teleporting. Slower than setting a
        /// position, and that is the point: it proves movement, bounds, and the reach test all
        /// still agree with each other.
        /// </summary>
        private IEnumerator WalkTo(Vector3 target, string what)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                Vector3 to = target - _player.Position;
                to.y = 0f;
                if (to.magnitude <= 0.45f)
                {
                    _input.Release();
                    yield return null;
                    yield break;
                }

                _input.Set(new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f)),
                    CommandButtons.None);
                yield return null;
            }

            _input.Release();
            Assert.Fail($"The player never reached {what} — walked for {PatienceSteps} steps "
                + $"and stopped {Vector3.Distance(target, _player.Position):F2} away.");
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                if (_driver.Frame >= deadline)
                {
                    break;
                }

                yield return null;
            }

            Assert.Fail(failure + $" (waited {PatienceSteps} steps).");
        }
    }
}
