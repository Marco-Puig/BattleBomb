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
using BattleBomb.UI.Chest;
using BattleBomb.UI.Frontend;
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
        private MemorySaveStore _store;

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
            _store = new MemorySaveStore();
            session.Store = _store;
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

        [UnityTest]
        public IEnumerator The_hosts_chapter_end_opens_the_guests_results_which_only_the_host_can_end()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.ChapterCompleted);
            extra.Add((Start + 20, writer.ToArray()));
            yield return Join(Recording(extra));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            ResultsScreen results = Object.FindAnyObjectByType<ResultsScreen>();
            yield return AdvanceUntil(() => results.IsOpen, "The host's chapter ended and the guest's results never opened.");
            Assert.That(GameObject.Find("Continue"), Is.Null, "The guest's results offer a Continue only the host may press.");

            // Past the dwell, then the guest's A.
            yield return AdvanceSteps(90);
            hands.Set(Vector2.zero, CommandButtons.Confirm | CommandButtons.Jump);
            yield return AdvanceSteps(3);
            hands.Release();
            yield return AdvanceSteps(10);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(NetSession.GameplayScene),
                "The guest's A ended the host's results; only the host's may (D60).");
            Assert.That(results.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator The_hosts_chapter_end_closes_the_guests_settings_before_its_results_take_the_screen()
        {
            // Online the world no longer pauses under the settings (D60), so the chapter can end beneath them.
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.ChapterCompleted);
            extra.Add((Start + 150, writer.ToArray()));
            yield return Join(Recording(extra));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            SettingsMenu settings = Object.FindAnyObjectByType<SettingsMenu>();
            ResultsScreen results = Object.FindAnyObjectByType<ResultsScreen>();
            yield return OpenSettings(hands);
            Assert.That(settings.IsOpen, Is.True, "The guest's settings never opened, so the case proves nothing.");
            Assert.That(results.IsOpen, Is.False, "The results opened before the settings, so the case proves nothing.");

            yield return AdvanceUntil(() => results.IsOpen, "The host's chapter ended and the guest's results never opened.");
            Assert.That(settings.IsOpen, Is.False, "The results opened over the guest's settings menu, which still listens to A.");

            int sentBefore = _playback.Sent.Count;
            hands.Set(Vector2.zero, CommandButtons.Confirm);
            yield return AdvanceSteps(3);
            hands.Release();
            yield return AdvanceSteps(3);
            for (int i = sentBefore; i < _playback.Sent.Count; i++)
            {
                var reader = new NetReader(_playback.Sent[i]);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Request)
                {
                    Assert.That(RequestCodec.ReadRequest(reader).Kind, Is.Not.EqualTo(PlayerRequestKind.SetAutoEquip),
                        "An A pressed at the results toggled a setting hidden behind them.");
                }
            }
        }

        [UnityTest]
        public IEnumerator On_a_guest_the_auto_flags_are_the_hosts_to_change()
        {
            yield return Join(Recording(new List<(int, byte[])>()));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return OpenSettings(hands);

            hands.Set(Vector2.zero, CommandButtons.Confirm);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceSteps(2);

            bool asked = false;
            foreach (byte[] message in _playback.Sent)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Request)
                {
                    PlayerRequest request = RequestCodec.ReadRequest(reader);
                    asked |= request.Kind == PlayerRequestKind.SetAutoEquip && request.A == 1;
                }
            }

            Assert.That(asked, Is.True, "Toggling auto-equip on a guest never asked the host.");
            Assert.That(_driver.InventoryOf(1).Inventory.AutoEquip, Is.False,
                "The guest changed its own copy; only the host's answer may.");
        }

        [UnityTest]
        public IEnumerator On_a_guest_the_session_row_leaves_the_game()
        {
            // The guest's own copy, and nothing that would write it: leaving is the clean exit that saves (D52).
            var extra = new List<(int, byte[])>();
            var bag = new Inventory();
            bag.Add(Knife(), 99);
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, 1, GuestRevision, true, SaveMapper.Participant(
                bag.Sack, new Wallet(100), new CharacterState(ElementId.None, XpLedger.Fresh, bag), withSack: true));
            extra.Add((Start + 10, writer.ToArray()));
            yield return Join(Recording(extra));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return AdvanceUntil(() => _driver.InventoryOf(1).Wallet.Balance == 100, "The guest's copy of itself never arrived.");
            yield return OpenSettings(hands);

            // AutoEquip, AutoSell, then the session row.
            for (int i = 0; i < 2; i++)
            {
                hands.Set(new Vector2(0f, -1f), CommandButtons.None);
                yield return AdvanceSteps(2);
                hands.Release();
                yield return AdvanceSteps(2);
            }

            hands.Set(Vector2.zero, CommandButtons.Confirm);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceUntil(() => SceneManager.GetActiveScene().name == NetSession.FrontendScene,
                "The guest's session row never took it out of the game.");

            Assert.That(GameSession.Find().Net.Role, Is.EqualTo(NetRole.Offline), "The guest left the picture but not the game.");
            bool bye = false;
            foreach (byte[] message in _playback.Sent)
            {
                bye |= message.Length > 0 && (NetMessageKind)message[0] == NetMessageKind.Bye;
            }

            Assert.That(bye, Is.True, "The host was never told the guest left.");
            Assert.That(_store.TryRead("guest-menu", out string text), Is.True, "Leaving the game did not save the guest first (D52).");
            Assert.That(SaveCodec.Decode(text).Save.Coins, Is.EqualTo(100));
        }

        private IEnumerator OpenSettings(ScriptedCommandSource hands)
        {
            hands.Set(Vector2.zero, CommandButtons.Pause);
            yield return AdvanceSteps(1);
            hands.Release();
            yield return AdvanceUntil(() => _driver.MenuPauseHeld, "Pause never opened the guest's own settings.");
            yield return AdvanceSteps(2);
        }

        [UnityTest]
        public IEnumerator When_the_host_says_so_the_guest_saves_its_own_copy_and_not_the_resume_point()
        {
            List<(int, byte[])> extra = ChestOpens();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.CheckpointReached);
            extra.Add((Start + 40, writer.ToArray()));

            // The guest's own run of this chapter, a stage further on than the host's.
            GameSession.Find().Progress.SetResume("fixture", 1, 0);
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _store.Names().Count > 0, "The host's checkpoint never wrote the guest's save.");

            Assert.That(_store.TryRead("guest-menu", out string text), Is.True);
            SaveGame written = SaveCodec.Decode(text).Save;
            Assert.That(written.Coins, Is.EqualTo(100), "The guest's save is not its copy from the host.");
            Assert.That(written.Sack.Length, Is.EqualTo(2));
            Assert.That(written.Story.ResumeChapterId, Is.EqualTo("fixture"), "The guest's own resume point was lost.");
            Assert.That(written.Story.ResumeStageIndex, Is.EqualTo(1), "The host's position was written over the guest's own resume point.");
            Assert.That(written.Story.ResumeCheckpointArena, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Before_its_copy_arrives_the_guest_writes_nothing()
        {
            var extra = new List<(int, byte[])>();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.CheckpointReached);
            extra.Add((Start + 20, writer.ToArray()));
            yield return Join(Recording(extra));
            yield return AdvanceSteps(60);

            Assert.That(_store.Names(), Is.Empty,
                "The guest wrote a save before the host had sent it anything of its own — an empty stash over its file.");
        }

        [UnityTest]
        public IEnumerator The_chapters_credit_goes_into_the_guests_own_save()
        {
            List<(int, byte[])> extra = ChestOpens();
            var writer = new NetWriter();
            SessionCodec.WriteMoment(writer, MomentKind.ChapterCompleted);
            extra.Add((Start + 40, writer.ToArray()));

            // The guest's own run of this chapter, a stage further on than the host's.
            GameSession.Find().Progress.SetResume("fixture", 1, 0);
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _store.Names().Count > 0, "The chapter's end never wrote the guest's save.");

            _store.TryRead("guest-menu", out string text);
            SaveGame written = SaveCodec.Decode(text).Save;
            Assert.That(SaveMapper.RestoreProgress(written).HighestTierBeaten("fixture"), Is.GreaterThan(0),
                "The chapter the guest finished with the host is not in the guest's own save (D61).");
            Assert.That(written.Story.ResumeChapterId, Is.EqualTo("fixture"),
                "Finishing the host's run cleared the guest's own resume point in that chapter; only the credit is theirs (D61).");
            Assert.That(written.Story.ResumeStageIndex, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator A_chest_the_guest_closes_is_saved_again_from_the_copy_the_host_closes_it_behind()
        {
            // The guest closes at once and saves then, before the host has answered the close (and perhaps its last
            // press); the host's close arrives right behind the copy that answer forced, and is saved from.
            const int ClosedAt = OpenAt + 120;
            List<(int, byte[])> extra = ChestOpens();
            var sold = new Inventory();
            sold.Add(Knife(), 99);
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, 1, GuestRevision + 1, true, SaveMapper.Participant(
                sold.Sack, new Wallet(160), new CharacterState(ElementId.None, XpLedger.Fresh, sold), withSack: true));
            extra.Add((ClosedAt, writer.ToArray()));
            extra.Add((ClosedAt, Events(writer, ReplicatedEvent.OfScreen(1, (int)InteractionKind.Chest, false).At(ClosedAt))));
            yield return Join(Recording(extra));
            ScriptedCommandSource hands = TakeTheGuestsHands();
            yield return AdvanceUntil(() => _driver.TryGetOpenScreen(1, out _), "The guest's chest never opened.");
            yield return AdvanceSteps(3);

            hands.Set(Vector2.zero, CommandButtons.Back);
            yield return AdvanceSteps(2);
            hands.Release();
            yield return AdvanceSteps(1);
            Assert.That(_driver.TryGetOpenScreen(1, out _), Is.False, "Back did not close the guest's chest.");
            Assert.That(_store.TryRead("guest-menu", out string early), Is.True, "Closing its own chest did not save the guest.");
            Assert.That(SaveCodec.Decode(early).Save.Coins, Is.EqualTo(100),
                "The host's last copy had already arrived, so the case proves nothing.");

            yield return AdvanceUntil(() => _driver.InventoryOf(1).Wallet.Balance == 160, "The host's last copy never arrived.");
            yield return AdvanceSteps(2);
            _store.TryRead("guest-menu", out string text);
            Assert.That(SaveCodec.Decode(text).Save.Coins, Is.EqualTo(160),
                "The guest's save is of the bag from before the host answered it.");
        }

        [UnityTest]
        public IEnumerator A_copy_of_the_guests_character_alone_leaves_its_bag_as_it_was()
        {
            // Task 101a: a kill's XP sends the guest its own character without the sack.
            List<(int, byte[])> extra = ChestOpens();
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, 1, GuestRevision + 5, false, SaveMapper.Participant(
                new Inventory().Sack, Wallet.Empty,
                new CharacterState(ElementId.None, new XpLedger(7, 0f, 0, BaseStats.Zero, 0), new Inventory()), withSack: false));
            extra.Add((Start + 40, writer.ToArray()));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => _driver.InventoryOf(1).Level == 7, "The guest's character never arrived.");

            PlayerInventory bag = _driver.InventoryOf(1);
            Assert.That(bag.Inventory.Items.Count, Is.EqualTo(2), "A copy of the character alone emptied the guest's sack.");
            Assert.That(bag.Wallet.Balance, Is.EqualTo(100));
            Assert.That(bag.Inventory.Sack.Revision, Is.EqualTo(GuestRevision), "A copy of the character alone moved the sack's revision.");
        }

        [UnityTest]
        public IEnumerator When_the_host_ends_the_match_cleanly_the_guest_saves_first()
        {
            // The host's "Return to chapters" is D52's clean exit, and so the guest's moment too (D61).
            var extra = new List<(int, byte[])>();
            var bag = new Inventory();
            bag.Add(Knife(), 99);
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, 1, GuestRevision, true, SaveMapper.Participant(
                bag.Sack, new Wallet(100), new CharacterState(ElementId.None, XpLedger.Fresh, bag), withSack: true));
            extra.Add((Start + 10, writer.ToArray()));
            writer.Reset();
            HandshakeCodec.WriteBare(writer, NetMessageKind.SessionEnd);
            extra.Add((Start + 40, writer.ToArray()));
            yield return Join(Recording(extra));
            yield return AdvanceUntil(() => SceneManager.GetActiveScene().name == NetSession.FrontendScene,
                "The host's end of the match never took the guest back to its front door.");

            Assert.That(_store.TryRead("guest-menu", out string text), Is.True,
                "The host ended the match cleanly and the guest saved nothing (D61).");
            Assert.That(SaveCodec.Decode(text).Save.Coins, Is.EqualTo(100));

            // The next match's pick brings what was just saved, re-read by the front door (D61).
            FrontendFlow flow = null;
            yield return AdvanceUntil(() => (flow = Object.FindAnyObjectByType<FrontendFlow>()) != null && flow.Lobby != null,
                "The guest's front door never became its lobby again.");
            int sentBefore = _playback.Sent.Count;
            flow.Lobby.Confirm();
            yield return null;
            yield return null;
            LobbyPick pick = default;
            for (int i = sentBefore; i < _playback.Sent.Count; i++)
            {
                var reader = new NetReader(_playback.Sent[i]);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.LobbyPick)
                {
                    pick = LobbyCodec.ReadPick(reader);
                }
            }

            Assert.That(pick.Ready, Is.True, "The guest's lobby never sent its ready pick.");
            Assert.That(pick.Brought.Coins, Is.EqualTo(100), "The next match would bring an older save than the one just written.");
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
            // A guest that has left its picture has no driver to step; waiting on one only burns the guard.
            for (int guard = 0; guard < 6000 && _driver != null && _driver.Frame < target; guard++)
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
