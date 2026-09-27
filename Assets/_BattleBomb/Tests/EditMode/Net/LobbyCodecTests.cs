using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>What a guest brings to the host's game (D59/D61): the hero they picked, whether they are ready, and
    /// their own save cut down to that hero.</summary>
    public sealed class LobbyCodecTests
    {
        private static SaveGame SaveWithTwoHeroes()
        {
            var inventory = new Inventory();
            inventory.Add(new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], requiredLevel: 1), 99);
            var fire = new CharacterState(new ElementId(1), new XpLedger(7, 0f, 0, BaseStats.Zero, 0), new Inventory());
            var ice = new CharacterState(new ElementId(2), new XpLedger(3, 0f, 0, BaseStats.Zero, 0), new Inventory());
            var story = new Core.Chapters.StoryProgress();
            story.SetResume("fixture", 1, 0);
            return SaveMapper.Capture(inventory.Sack, new Wallet(640), new[] { fire, ice }, story);
        }

        [Test]
        public void A_guest_brings_their_sack_their_wallet_and_only_the_hero_they_picked()
        {
            SaveGame brought = SaveMapper.ParticipantFrom(SaveWithTwoHeroes(), 2);

            Assert.That(brought.Coins, Is.EqualTo(640));
            Assert.That(brought.Sack.Length, Is.EqualTo(1));
            Assert.That(brought.Characters.Length, Is.EqualTo(1));
            Assert.That(brought.Characters[0].ElementId, Is.EqualTo(2));
            Assert.That(brought.Characters[0].Level, Is.EqualTo(3));
            Assert.That(brought.Story.ResumeChapterId, Is.Empty, "The guest's resume point went to the host; it is the guest's alone.");
        }

        [Test]
        public void A_hero_never_played_is_brought_as_nobody_yet()
        {
            SaveGame brought = SaveMapper.ParticipantFrom(SaveWithTwoHeroes(), 9);

            Assert.That(brought.Characters, Is.Empty);
            Assert.That(brought.Coins, Is.EqualTo(640), "A new hero still plays from the player's own sack and wallet.");
        }

        [Test]
        public void No_save_at_all_brings_an_empty_one()
        {
            SaveGame brought = SaveMapper.ParticipantFrom(null, 1);

            Assert.That(brought.Coins, Is.Zero);
            Assert.That(brought.Sack, Is.Empty);
        }

        [Test]
        public void A_pick_travels_with_what_it_brings()
        {
            var writer = new NetWriter();
            LobbyCodec.WritePick(writer, new LobbyPick(3, true, SaveMapper.ParticipantFrom(SaveWithTwoHeroes(), 1)));
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.LobbyPick));
            LobbyPick back = LobbyCodec.ReadPick(reader);

            Assert.That(back.RosterIndex, Is.EqualTo(3));
            Assert.That(back.Ready, Is.True);
            Assert.That(back.Brought, Is.Not.Null);
            Assert.That(back.Brought.Characters[0].Level, Is.EqualTo(7));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void A_pick_that_is_not_ready_brings_nothing()
        {
            var writer = new NetWriter();
            LobbyCodec.WritePick(writer, new LobbyPick(1, false, SaveMapper.ParticipantFrom(SaveWithTwoHeroes(), 1)));
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            LobbyPick back = LobbyCodec.ReadPick(reader);

            Assert.That(back.Ready, Is.False);
            Assert.That(back.Brought, Is.Null);
        }

        [Test]
        public void A_pick_that_brings_nothing_is_not_ready()
        {
            // Ready off the wire with no save behind it: the host would restore the guest from nothing (D61).
            var writer = new NetWriter();
            writer.WriteByte((byte)NetMessageKind.LobbyPick);
            writer.WriteInt(0);
            writer.WriteBool(true);
            writer.WriteBool(false);
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();

            Assert.That(LobbyCodec.ReadPick(reader).Ready, Is.False,
                "A ready pick that brought nothing would seat the guest over an empty stash.");
        }

        [Test]
        public void The_hosts_lobby_travels_whole()
        {
            var writer = new NetWriter();
            LobbyCodec.WriteLobby(writer, new LobbyState(Core.Chapters.FrontendScreen.Chapters, 2, true, false));
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.LobbyState));
            LobbyState back = LobbyCodec.ReadLobby(reader);

            Assert.That(back.HostScreen, Is.EqualTo(Core.Chapters.FrontendScreen.Chapters));
            Assert.That(back.HostPick, Is.EqualTo(2));
            Assert.That(back.HostReady, Is.True);
            Assert.That(back.InMatch, Is.False);
            Assert.That(reader.Remaining, Is.Zero);
        }
    }
}
