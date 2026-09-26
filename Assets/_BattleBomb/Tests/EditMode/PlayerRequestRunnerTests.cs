using System.Reflection;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The one place a request becomes a change (HANDOFF-M8 planning decision 11). The same runner
    /// serves the couch, now, and a guest, a round trip later, so its refusals are the host's rules:
    /// no open chest, no inventory verbs; a stale sack, no index verbs; closing and the settings always.
    /// <c>PlayerInventory.OnEnable</c> is fired by reflection, as <see cref="SharedStashWiringTests"/> does.
    /// </summary>
    public sealed class PlayerRequestRunnerTests
    {
        private const int PlayerId = 1;

        private GameObject _stashGo;
        private GameObject _playerGo;
        private GameObject _driverGo;
        private PlayerInventory _bag;
        private SimulationDriver _driver;

        private static ItemInstance Helmet(int id) => new ItemInstance(
            new ItemIdentity(id, $"Helm {id}", ItemSlot.Helmet), QualityRank.Shiny,
            new GearContribution(defence: 3f), new AffixRoll[0], requiredLevel: 1);

        [SetUp]
        public void Build()
        {
            _stashGo = new GameObject("Stash");
            _stashGo.AddComponent<SharedStash>();
            _playerGo = new GameObject("Player");
            _bag = _playerGo.AddComponent<PlayerInventory>();
            typeof(PlayerInventory).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_bag, null);
            _driverGo = new GameObject("Driver");
            _driver = _driverGo.AddComponent<SimulationDriver>();
            _bag.Take(Helmet(1));
        }

        [TearDown]
        public void Clear()
        {
            Object.DestroyImmediate(_playerGo);
            Object.DestroyImmediate(_stashGo);
            Object.DestroyImmediate(_driverGo);
        }

        private RequestOutcome Run(PlayerRequest request) =>
            PlayerRequestRunner.Run(request.For(PlayerId).WithRevision(_bag.Inventory.Sack.Revision), _bag, _driver);

        [Test]
        public void A_sale_at_an_open_chest_pays_and_says_how_much()
        {
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);
            int before = _bag.Wallet.Balance;

            RequestOutcome outcome = Run(PlayerRequest.Sell(0));

            Assert.That(outcome.Ok, Is.True);
            Assert.That(outcome.A, Is.GreaterThan(0), "The answer does not say what the sale paid.");
            Assert.That(_bag.Wallet.Balance, Is.EqualTo(before + outcome.A));
            Assert.That(_bag.Inventory.Items.Count, Is.Zero);
        }

        [Test]
        public void With_no_chest_open_an_inventory_verb_does_nothing()
        {
            RequestOutcome outcome = Run(PlayerRequest.Sell(0));

            Assert.That(outcome.Ok, Is.False);
            Assert.That(outcome.Refusal, Is.EqualTo(RequestRefusal.NoScreen));
            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(1), "A sale ran with no chest open.");
        }

        [Test]
        public void A_request_aimed_at_a_sack_that_moved_is_refused()
        {
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);
            int stale = _bag.Inventory.Sack.Revision;
            _bag.Take(Helmet(2));

            RequestOutcome outcome = PlayerRequestRunner.Run(
                PlayerRequest.Sell(0).For(PlayerId).WithRevision(stale), _bag, _driver);

            Assert.That(outcome.Refusal, Is.EqualTo(RequestRefusal.StaleSack));
            Assert.That(_bag.Inventory.Items.Count, Is.EqualTo(2), "A sale aimed at an old view of the sack still sold.");
        }

        [Test]
        public void Closing_needs_no_revision_and_always_closes()
        {
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);

            RequestOutcome outcome = PlayerRequestRunner.Run(
                PlayerRequest.CloseScreen().For(PlayerId).WithRevision(-99), _bag, _driver);

            Assert.That(outcome.Ok, Is.True);
            Assert.That(_driver.TryGetOpenScreen(PlayerId, out _), Is.False);
        }

        [Test]
        public void The_auto_flags_need_no_chest()
        {
            RequestOutcome outcome = Run(PlayerRequest.SetAutoEquip(true));

            Assert.That(outcome.Ok, Is.True);
            Assert.That(_bag.Inventory.AutoEquip, Is.True);
        }

        [Test]
        public void A_point_is_spent_only_at_a_chest()
        {
            _bag.Earn(5000f);
            Assert.That(Run(PlayerRequest.Allocate(StatId.Strength)).Refusal, Is.EqualTo(RequestRefusal.NoScreen));

            _driver.OpenScreen(PlayerId, InteractionKind.Chest);
            Assert.That(Run(PlayerRequest.Allocate(StatId.Strength)).Ok, Is.True);
            Assert.That(_bag.Ledger.Allocations.Strength, Is.EqualTo(1));
        }

        [Test]
        public void A_worn_slot_is_named_by_the_slot_so_a_moved_sack_does_not_matter()
        {
            Assert.That(_bag.RequestEquip(0), Is.True, "The helmet would not go on, so the case proves nothing.");
            _driver.OpenScreen(PlayerId, InteractionKind.Chest);

            RequestOutcome outcome = PlayerRequestRunner.Run(
                PlayerRequest.LockWorn(ItemSlot.Helmet, 0, true).For(PlayerId).WithRevision(-99), _bag, _driver);

            Assert.That(outcome.Ok, Is.True, $"Locking a worn piece was refused: {outcome.Refusal}.");
            Assert.That(_bag.Inventory.Loadout.Worn(ItemSlot.Helmet, 0).Locked, Is.True);
        }
    }
}
