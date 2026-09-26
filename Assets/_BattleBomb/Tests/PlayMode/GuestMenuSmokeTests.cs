using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// The guest's menus (HANDOFF-M8 Task 99), on a real guest machine fed a hand-built host: the guest's
    /// chest and shop open on its own display over the copy of its bag the host sent, an action becomes a
    /// request that waits for the host, and closing is at once. The host itself is Task 97's, tested from
    /// the other side in <see cref="OnlineMenuSmokeTests"/>.
    /// </summary>
    public sealed class GuestMenuSmokeTests
    {
        private const int Start = 1000;
        private const int Length = 400;
        private const int LoadMargin = 120;
        private const int OpenAt = Start + 20;
        private const int GuestRevision = 41;

        private PlaybackTransport _playback;
        private NetGuest _guest;
        private SimulationDriver _driver;

        [UnitySetUp]
        public IEnumerator OpenTheFrontDoor()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "guest-menu";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator The_guests_chest_opens_on_its_own_display_over_its_own_bag()
        {
            // The host's own chest opens too: the guest holds it open for the host's body and draws nothing for it.
            List<(int, byte[])> extra = ChestOpens();
            extra.Add((OpenAt, Events(new NetWriter(), ReplicatedEvent.OfScreen(0, (int)InteractionKind.Chest, true).At(OpenAt))));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The host said the guest's chest opened; it never did.");
            Assert.That(_driver.TryGetOpenScreen(0, out _), Is.True, "The host's own chest never opened on the guest, so the case proves nothing.");
            yield return AdvanceSteps(2);

            Assert.That(GameObject.Find("Chest Screen P2"), Is.Not.Null, "The guest's own chest is not on its display.");
            Assert.That(GameObject.Find("Chest Screen P1"), Is.Null);
            PlayerInventory bag = _driver.InventoryOf(1);
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(2), "The guest's copy of its bag is not the host's.");
            Assert.That(bag.Wallet.Balance, Is.EqualTo(100));
            Assert.That(bag.Inventory.Sack.Revision, Is.EqualTo(GuestRevision), "The copy did not take the host's revision.");
            Assert.That(_driver.InventoryOf(0).Inventory.Loadout.Weapon.IsEmpty, Is.False,
                "The guest's copy of its partner is not wearing the partner's knife.");
        }

        [UnityTest]
        public IEnumerator A_sale_on_the_guest_is_sent_to_the_host_and_waits_for_its_answer()
        {
            yield return Join(Recording(ChestOpens()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceSteps(3);

            hands.Set(Vector2.zero, CommandButtons.Light | CommandButtons.Option);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceSteps(2);

            PlayerRequest sent = default;
            bool found = false;
            foreach (byte[] message in _playback.Sent)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Request)
                {
                    sent = RequestCodec.ReadRequest(reader);
                    found = true;
                }
            }

            Assert.That(found, Is.True, "X on the guest's chest sent the host nothing.");
            Assert.That(sent.Kind, Is.EqualTo(PlayerRequestKind.Sell));
            Assert.That(sent.Revision, Is.EqualTo(GuestRevision), "The request does not name the sack the host holds.");
            Assert.That(_driver.RequestsFor(1).Pending, Is.True, "The screen is not waiting for the host's answer.");
            Assert.That(_driver.InventoryOf(1).Inventory.Items.Count, Is.EqualTo(2),
                "The guest changed its own bag; only the host's answer may.");
        }

        [UnityTest]
        public IEnumerator Closing_on_the_guest_is_at_once_and_the_host_is_told()
        {
            yield return Join(Recording(ChestOpens()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceSteps(3);

            hands.Set(Vector2.zero, CommandButtons.Back);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceSteps(1);

            Assert.That(_driver.TryGetOpenScreen(1, out _), Is.False, "Back did not close the guest's chest at once.");
            bool told = false;
            foreach (byte[] message in _playback.Sent)
            {
                var reader = new NetReader(message);
                told |= (NetMessageKind)reader.ReadByte() == NetMessageKind.Request
                    && RequestCodec.ReadRequest(reader).Kind == PlayerRequestKind.CloseScreen;
            }

            Assert.That(told, Is.True, "The host was never told the guest closed its chest; the body would stand idle.");
        }

        [UnityTest]
        public IEnumerator The_guests_shop_draws_the_hosts_rack()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            extra.Add((OpenAt, Events(writer,
                ReplicatedEvent.OfRack(1, new[] { Knife(), Knife() }).At(OpenAt),
                ReplicatedEvent.OfScreen(1, (int)InteractionKind.Shopkeeper, true).At(OpenAt))));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out InteractionKind kind) && kind == InteractionKind.Shopkeeper,
                "The guest's shop never opened.");

            Assert.That(_driver.RackFor(1).Count, Is.EqualTo(2), "The guest's shop does not show the host's rack.");
            Assert.That(GameObject.Find("Chest Screen P2"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator A_screen_the_host_closes_closes_on_the_guest()
        {
            var extra = ChestOpens();
            var writer = new NetWriter();
            extra.Add((OpenAt + 60, Events(writer, ReplicatedEvent.OfScreen(1, (int)InteractionKind.Chest, false).At(OpenAt + 60))));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceUntil(() => !_driver.TryGetOpenScreen(1, out _), "The host closed the guest's chest; the guest's stayed open.");

            // The screen's Destroy lands at the end of the frame the close arrived in.
            yield return null;
            Assert.That(GameObject.Find("Chest Screen P2"), Is.Null);
        }

        [UnityTest]
        public IEnumerator A_screen_opened_on_the_guest_itself_leaves_the_hosts_rack_alone()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            extra.Add((OpenAt, Events(writer, ReplicatedEvent.OfRack(1, new[] { Knife(), Knife() }).At(OpenAt))));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _driver.RackFor(1).Count == 2, "The host's rack never reached the guest.");

            _driver.OpenScreen(1, InteractionKind.Chest);

            Assert.That(_driver.RackFor(1).Count, Is.EqualTo(2), "A screen opened on the guest wiped a rack only the host may change.");
        }

        [UnityTest]
        public IEnumerator B_that_closes_the_guests_chest_never_reaches_the_host_as_magic()
        {
            yield return Join(Recording(ChestOpens()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceSteps(3);
            int sentBefore = _playback.Sent.Count;

            // A pad's East is Back in the menu map and Magic in the fight map, and both maps are live at once.
            hands.Set(Vector2.zero, CommandButtons.Back | CommandButtons.Magic);
            yield return AdvanceSteps(4);
            Assert.That(_driver.TryGetOpenScreen(1, out _), Is.False, "B did not close the guest's chest.");
            hands.Release();
            yield return AdvanceSteps(4);

            var batch = new List<WireCommand>();
            for (int i = sentBefore; i < _playback.Sent.Count; i++)
            {
                var reader = new NetReader(_playback.Sent[i]);
                if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Commands)
                {
                    continue;
                }

                CommandCodec.Read(reader, batch);
                foreach (WireCommand command in batch)
                {
                    Assert.That(command.Held & CommandButtons.Magic, Is.EqualTo(CommandButtons.None),
                        $"Frame {command.Frame}: the B that closed the guest's chest reached the host as Magic.");
                }
            }
        }

        private IEnumerator Join(List<(int Frame, byte[] Payload)> recording)
        {
            _playback = new PlaybackTransport(recording);
            NetSession.FindOrCreate().Join(_playback, "playback");
            for (int i = 0; i < 1500 && _guest == null; i++)
            {
                yield return null;
                _guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(_guest, Is.Not.Null, "The guest never loaded the host's run.");
            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            yield return AdvanceUntil(() => _guest.RenderFrame >= Start, "The guest never started drawing.");
        }

        /// <summary>The guest's own pad out of the loop, a script in its place, speaking as Player 2.</summary>
        private ScriptedCommandSource TakeTheGuestsHands()
        {
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            CharacterActor own = null;
            foreach (CharacterActor actor in _driver.Characters.Ordered)
            {
                if (actor.PlayerId.Value == 1)
                {
                    own = actor;
                }
            }

            Assert.That(own, Is.Not.Null);
            _driver.Players.Unregister(own.PlayerId);
            var hands = own.gameObject.AddComponent<ScriptedCommandSource>();
            hands.Bind(1);
            _driver.Players.Register(hands);
            return hands;
        }

        private IEnumerator AdvanceSteps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < 6000 && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private static IEnumerator AdvanceUntil(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < 6000 && !condition(); guard++)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, failure);
        }

        private static ItemInstance Knife() => new ItemInstance(
            new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
            new GearContribution(weaponDamage: 9f), new AffixRoll[0], requiredLevel: 1, new ItemInvestment(3));

        /// <summary>Both players' inventories, then the guest's chest opening.</summary>
        private static List<(int, byte[])> ChestOpens()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();

            var guestBag = new Inventory();
            guestBag.Add(Knife(), 99);
            guestBag.Add(Knife(), 99);
            SaveGame guest = SaveMapper.Participant(
                guestBag.Sack, new Wallet(100), new CharacterState(ElementId.None, XpLedger.Fresh, guestBag), withSack: true);
            ParticipantCodec.Write(writer, 1, GuestRevision, true, guest);
            extra.Add((Start + 10, writer.ToArray()));

            var hostBag = new Inventory();
            hostBag.Add(Knife(), 99);
            hostBag.TryEquip(0, 99);
            SaveGame host = SaveMapper.Participant(
                hostBag.Sack, Wallet.Empty, new CharacterState(ElementId.None, XpLedger.Fresh, hostBag), withSack: false);
            writer.Reset();
            ParticipantCodec.Write(writer, 0, 3, false, host);
            extra.Add((Start + 10, writer.ToArray()));

            extra.Add((OpenAt, Events(writer, ReplicatedEvent.OfScreen(1, (int)InteractionKind.Chest, true).At(OpenAt))));
            return extra;
        }

        /// <summary>A launch, both players standing still in a snapshot every second step, and the extras — merged
        /// in step order, as a host would have sent them.</summary>
        private static List<(int Frame, byte[] Payload)> Recording(List<(int, byte[])> extra)
        {
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1));
            recording.Add((Start - LoadMargin, writer.ToArray()));

            for (int frame = Start; frame <= Start + Length; frame += NetProtocol.SnapshotEverySteps)
            {
                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                world.Players.Add(Standing(1, 2f));
                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            foreach ((int frame, byte[] payload) in extra)
            {
                recording.Add((frame, payload));
            }

            // Stable: a snapshot and an extra on one step keep the order they were added in.
            var ordered = new List<(int Frame, byte[] Payload)>();
            for (int i = 0; i < recording.Count; i++)
            {
                int at = ordered.Count;
                while (at > 0 && ordered[at - 1].Frame > recording[i].Frame)
                {
                    at--;
                }

                ordered.Insert(at, recording[i]);
            }

            return ordered;
        }

        private static PlayerSnapshot Standing(int playerId, float x) => new PlayerSnapshot(
            playerId, MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready,
            new PlayerCondition(Health.FromValues(100f, 100f), 0, 0), ReviveChannel.Inactive,
            ManaPool.FromValues(50f, 50f), true, null, -1, 0, 0);

        private static byte[] Events(NetWriter writer, params ReplicatedEvent[] events)
        {
            writer.Reset();
            EventCodec.Write(writer, events);
            return writer.ToArray();
        }
    }
}
