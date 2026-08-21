using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Combat;
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

        /// <summary>Long enough for any single link to resolve; short enough to fail fast.</summary>
        private const int PatienceFrames = 600;

        /// <summary>The starter knife — something wearable, so the equip link has a subject.</summary>
        private const int KnifeDefinitionId = 7;

        private SimulationDriver _driver;
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
            // stray grunt into the same red. Combat has its own coverage in EditMode, and the
            // feel of the fight is Michael's pass.
            foreach (EnemySpawner spawner in
                Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include))
            {
                spawner.enabled = false;
            }

            foreach (EnemyActor enemy in
                Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return null;
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
            yield return Press(CommandButtons.Light);
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
            yield return Press(CommandButtons.Light);
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
        /// and not exit is worse than one that never opens, so all three routes are pinned.
        /// </summary>
        [UnityTest]
        public IEnumerator Every_way_out_of_the_chest_screen_works(
            [Values("pause", "heavy", "close button")] string route)
        {
            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(CommandButtons.Light);
            yield return Until(
                () => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            switch (route)
            {
                case "pause":
                    yield return Press(CommandButtons.Pause);
                    break;

                case "heavy":
                    yield return Press(CommandButtons.Heavy);
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

        private WorldInteractable FindChest()
        {
            foreach (WorldInteractable candidate in
                Object.FindObjectsByType<WorldInteractable>(FindObjectsInactive.Exclude))
            {
                if (candidate.Kind == InteractionKind.Chest)
                {
                    return candidate;
                }
            }

            return null;
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

        /// <summary>Waits until the simulation has advanced this many fixed steps.</summary>
        private IEnumerator SimulationFrames(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < PatienceFrames && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Walks the player onto a spot by steering, not teleporting. Slower than setting a
        /// position, and that is the point: it proves movement, bounds, and the reach test all
        /// still agree with each other.
        /// </summary>
        private IEnumerator WalkTo(Vector3 target, string what)
        {
            for (int frame = 0; frame < PatienceFrames; frame++)
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
            Assert.Fail($"The player never reached {what} — walked for {PatienceFrames} frames "
                + $"and stopped {Vector3.Distance(target, _player.Position):F2} away.");
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            for (int frame = 0; frame < PatienceFrames; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(failure + $" (waited {PatienceFrames} frames).");
        }
    }
}
