using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D45's tripwire for the menus online (HANDOFF-M8 Stage C): the real Gameplay scene hosted, Player 2 a
    /// headless guest over the loopback. The guest's menu actions reach the host as requests, run there,
    /// and are answered. Each test starts with Player 2 standing at the first chest.
    /// </summary>
    public sealed class OnlineMenuSmokeTests
    {
        private const int PatienceSteps = 1200;
        private const int FrameCeiling = 30000;
        private const int LoadFrameCeiling = 1500;
        private const int KnifeDefinitionId = 7;

        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _host;
        private CharacterActor _guestBody;
        private ScriptedCommandSource _hostInput;
        private HeadlessGuest _guest;
        private WorldInteractable _chest;

        [UnitySetUp]
        public IEnumerator HostWithAGuestAtTheChest()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-menu-smoke";

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            DisableDevices();

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.IsConnected && _guest.IsWelcomed, "the handshake never finished");

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return UntilFrames(() => _runner.IsStageLoaded, "the stage never streamed in");
            yield return UntilFrames(() => _driver.Frame > 5, "the launch hold never released");

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            Assert.That(actors.Count, Is.EqualTo(2));
            _host = actors[0];
            _guestBody = actors[1];

            DisableDevices();
            _driver.Players.Unregister(_host.PlayerId);
            _hostInput = _host.gameObject.AddComponent<ScriptedCommandSource>();
            _hostInput.Bind(_host.PlayerId.Value);
            _driver.Players.Register(_hostInput);

            _chest = FirstChest();
            Assert.That(_chest, Is.Not.Null, "The launch stage has no chest.");
            yield return WalkGuestTo(_chest.Position, "the first chest");
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            if (_guest != null)
            {
                Object.Destroy(_guest.gameObject);
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator A_guests_sale_runs_on_the_host_and_the_answer_comes_back()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            int items = bag.Inventory.Items.Count;
            int coins = bag.Wallet.Balance;
            yield return Steps(2);

            _guest.SendRequest(PlayerRequest.Sell(items - 1).WithSequence(7).WithRevision(bag.Inventory.Sack.Revision));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered the guest's sale");

            Assert.That(_guest.Results[0].Sequence, Is.EqualTo(7), "The answer did not name the request it answers.");
            Assert.That(_guest.Results[0].Outcome.Ok, Is.True, $"The sale was refused: {_guest.Results[0].Outcome.Refusal}.");
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(items - 1), "The host's copy of the guest's bag still has the item.");
            Assert.That(bag.Wallet.Balance, Is.EqualTo(coins + _guest.Results[0].Outcome.A), "The coins the answer names never reached the wallet.");
        }

        [UnityTest]
        public IEnumerator A_guest_can_only_ever_act_as_itself()
        {
            PlayerInventory guestBag = _guestBody.GetComponent<PlayerInventory>();
            PlayerInventory hostBag = _host.GetComponent<PlayerInventory>();
            guestBag.Earn(5000f);
            hostBag.Earn(5000f);
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            yield return Steps(2);

            // Written as Player 1's: the host must stamp the guest's own id over it.
            _guest.SendRequest(PlayerRequest.Allocate(StatId.Strength).For(0).WithSequence(1));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            Assert.That(_guest.Results[0].Outcome.Ok, Is.True, $"The guest's own point was refused: {_guest.Results[0].Outcome.Refusal}.");
            Assert.That(guestBag.Ledger.Allocations.Strength, Is.EqualTo(1), "The guest's point went nowhere.");
            Assert.That(hostBag.Ledger.Allocations.Strength, Is.Zero, "The guest spent the host's point.");
        }

        [UnityTest]
        public IEnumerator A_guests_request_aimed_at_an_old_view_of_the_sack_is_refused()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            int stale = bag.Inventory.Sack.Revision;
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            int items = bag.Inventory.Items.Count;
            yield return Steps(2);

            _guest.SendRequest(PlayerRequest.Sell(0).WithSequence(3).WithRevision(stale));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            Assert.That(_guest.Results[0].Outcome.Refusal, Is.EqualTo(RequestRefusal.StaleSack));
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(items), "A sale aimed at an old view of the sack still sold.");
        }

        [UnityTest]
        public IEnumerator With_no_chest_open_a_guests_sale_does_nothing()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            int items = bag.Inventory.Items.Count;
            yield return Steps(2);

            _guest.SendRequest(PlayerRequest.Sell(0).WithSequence(5).WithRevision(bag.Inventory.Sack.Revision));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            Assert.That(_guest.Results[0].Outcome.Refusal, Is.EqualTo(RequestRefusal.NoScreen));
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(items));
        }

        [UnityTest]
        public IEnumerator The_guests_press_at_the_chest_opens_it_and_the_guest_is_told()
        {
            int before = _guest.Received.Count;
            _guest.Held = CommandButtons.Light;
            yield return Steps(3);
            _guest.Held = CommandButtons.None;
            yield return Until(() => _driver.TryGetOpenScreen(PlayerId.Two.Value, out InteractionKind kind) && kind == InteractionKind.Chest,
                "Player 2's Light beside the chest never opened it on the host");
            yield return Steps(4);

            int opened = IndexOf(before, m => IsEvent(m, ReplicatedEventKind.ScreenOpened, PlayerId.Two.Value));
            int bag = IndexOf(before, m => IsParticipant(m, PlayerId.Two.Value, full: true));
            Assert.That(opened, Is.GreaterThanOrEqualTo(0), "The guest was never told its chest opened.");
            Assert.That(bag, Is.GreaterThanOrEqualTo(0), "The guest's bag never reached it.");
            Assert.That(bag, Is.LessThan(opened), "The chest opened on the guest before its bag arrived.");
        }

        [UnityTest]
        public IEnumerator No_screen_is_drawn_on_the_host_for_a_remote_players_chest()
        {
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            yield return Steps(3);

            Assert.That(_driver.TryGetOpenScreen(PlayerId.Two.Value, out _), Is.True);
            Assert.That(GameObject.Find("Chest Screen P2"), Is.Null,
                "The host drew the guest's chest on the host's display.");
        }

        [UnityTest]
        public IEnumerator A_guests_bag_reaches_it_before_the_answer_that_reads_it()
        {
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            _driver.OpenScreen(PlayerId.Two.Value, InteractionKind.Chest);
            yield return Steps(4);
            int items = bag.Inventory.Items.Count;
            int before = _guest.Received.Count;

            _guest.SendRequest(PlayerRequest.Sell(items - 1).WithSequence(9).WithRevision(bag.Inventory.Sack.Revision));
            yield return Until(() => _guest.Results.Count > 0, "the host never answered");

            int answer = IndexOf(before, m => (NetMessageKind)m[0] == NetMessageKind.RequestResult);
            Assert.That(answer, Is.GreaterThanOrEqualTo(0));
            int copy = LastIndexOf(before, answer, m => IsParticipant(m, PlayerId.Two.Value, full: true));
            Assert.That(copy, Is.GreaterThanOrEqualTo(0), "The answer arrived before the bag it describes.");

            var reader = new NetReader(_guest.Received[copy]);
            reader.ReadByte();
            ParticipantMessage sent = ParticipantCodec.Read(reader);
            Assert.That(sent.State.Sack.Length, Is.EqualTo(items - 1), "The copy the guest got still has the sold item.");
            Assert.That(sent.Revision, Is.EqualTo(bag.Inventory.Sack.Revision));
        }

        [UnityTest]
        public IEnumerator The_guest_is_told_what_its_partner_wears()
        {
            PlayerInventory hostBag = _host.GetComponent<PlayerInventory>();
            hostBag.Earn(5000f);
            Assert.That(hostBag.Inventory.Loadout.Weapon.IsEmpty || hostBag.Inventory.Loadout.Weapon.DefinitionId != KnifeDefinitionId,
                Is.True, "The host already wore a knife, so the case proves nothing.");
            hostBag.Take(_driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            Assert.That(hostBag.RequestEquip(hostBag.Inventory.Items.Count - 1), Is.True, "The host could not wear the knife.");
            int before = _guest.Received.Count;
            yield return Steps(NetProtocol.ParticipantMinSteps + 4);

            int copy = LastIndexOf(before, _guest.Received.Count, m => IsParticipant(m, 0, full: false));
            Assert.That(copy, Is.GreaterThanOrEqualTo(0), "The partner's new gear never reached the guest.");
            var reader = new NetReader(_guest.Received[copy]);
            reader.ReadByte();
            ParticipantMessage sent = ParticipantCodec.Read(reader);
            Assert.That(sent.State.Sack, Is.Empty, "The host's whole sack crossed the wire; only its gear should.");
            bool knife = false;
            foreach (var worn in sent.State.Characters[0].Worn)
            {
                knife |= worn.Item.DefinitionId == KnifeDefinitionId;
            }

            Assert.That(knife, Is.True, "The partner's copy does not wear the knife the host just put on.");
        }

        [UnityTest]
        public IEnumerator A_draught_the_guest_drinks_reaches_its_copy_of_the_bag()
        {
            const int PotionDefinitionId = 9;
            PlayerInventory bag = _guestBody.GetComponent<PlayerInventory>();
            bag.Take(_driver.RollDebugItem(PotionDefinitionId, 2.2f));
            Assert.That(bag.Inventory.AssignQuickConsumable(PotionDefinitionId), Is.True, "The draught would not go in the quick slot.");
            yield return Steps(NetProtocol.ParticipantMinSteps + 4);
            int revision = bag.Inventory.Sack.Revision;
            int before = _guest.Received.Count;

            _guest.Held = CommandButtons.Equipment;
            yield return Steps(3);
            _guest.Held = CommandButtons.None;
            yield return Until(() => bag.Inventory.Sack.Revision != revision, "the guest's quick-use never drank on the host");
            yield return Steps(NetProtocol.ParticipantMinSteps + 4);

            int copy = LastIndexOf(before, _guest.Received.Count, m => IsParticipant(m, PlayerId.Two.Value, full: true));
            Assert.That(copy, Is.GreaterThanOrEqualTo(0), "The drink never reached the guest's copy of its bag.");
            var reader = new NetReader(_guest.Received[copy]);
            reader.ReadByte();
            Assert.That(ParticipantCodec.Read(reader).Revision, Is.EqualTo(bag.Inventory.Sack.Revision),
                "The guest's copy is of the bag from before the drink.");
        }

        private int IndexOf(int from, System.Func<byte[], bool> match)
        {
            for (int i = from; i < _guest.Received.Count; i++)
            {
                if (match(_guest.Received[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The last match in [<paramref name="from"/>, <paramref name="end"/>), or -1.</summary>
        private int LastIndexOf(int from, int end, System.Func<byte[], bool> match)
        {
            for (int i = end - 1; i >= from; i--)
            {
                if (match(_guest.Received[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsParticipant(byte[] message, int playerId, bool full)
        {
            var reader = new NetReader(message);
            if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Participant)
            {
                return false;
            }

            return reader.ReadInt() == playerId && ReadFull(reader) == full;
        }

        private static bool ReadFull(NetReader reader)
        {
            reader.ReadInt();
            return reader.ReadBool();
        }

        private static bool IsEvent(byte[] message, ReplicatedEventKind kind, int playerId)
        {
            var reader = new NetReader(message);
            if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Events)
            {
                return false;
            }

            var batch = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, batch);
            foreach (ReplicatedEvent e in batch)
            {
                if (e.Kind == kind && e.Screen.PlayerId == playerId)
                {
                    return true;
                }
            }

            return false;
        }

        private WorldInteractable FirstChest()
        {
            WorldInteractable first = null;
            foreach (WorldInteractable candidate in Object.FindObjectsByType<WorldInteractable>(FindObjectsInactive.Exclude))
            {
                if (candidate.Kind == InteractionKind.Chest && candidate.gameObject.scene == _runner.StageScene
                    && (first == null || candidate.Position.x < first.Position.x))
                {
                    first = candidate;
                }
            }

            return first;
        }

        /// <summary>Walks Player 2 over the wire until they stand within the chest's reach.</summary>
        private IEnumerator WalkGuestTo(Vector3 target, string what)
        {
            int deadline = _driver.Frame + 5000;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                Vector3 to = target - _guestBody.Position;
                to.y = 0f;
                if (to.magnitude <= 0.8f)
                {
                    _guest.Move = Vector2.zero;
                    yield return Steps(4);
                    yield break;
                }

                _guest.Move = new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f));
                yield return null;
            }

            _guest.Move = Vector2.zero;
            Assert.Fail($"Player 2 never reached {what} — stopped {Vector3.Distance(target, _guestBody.Position):F2} away.");
        }

        private static void DisableDevices()
        {
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }
        }

        private IEnumerator Steps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {PatienceSteps} steps).");
        }

        private static IEnumerator UntilFrames(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < LoadFrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {LoadFrameCeiling} frames).");
        }
    }
}
