using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>
    /// A player's inventory crosses the wire in the save's own format, deflated (HANDOFF-M8 Task 99): a
    /// field added to the save travels without a second codec to keep in step. The guest's copy of its
    /// bag, and of its partner's worn gear, is rebuilt from it every time it changes.
    /// </summary>
    public sealed class ParticipantCodecTests
    {
        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), new GearContribution(weaponDamage: 9f)),
        };

        private static ItemInstance Knife(int affixes = 0)
        {
            var rolls = new AffixRoll[affixes];
            for (int i = 0; i < affixes; i++)
            {
                rolls[i] = new AffixRoll(AffixId.CritChance, 0.01f * (i + 1), ElementId.None);
            }

            return new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), rolls, requiredLevel: 1, new ItemInvestment(3));
        }

        /// <summary>A knife whose every number differs by <paramref name="seed"/>, so deflate cannot fold a sack of them into one.</summary>
        private static ItemInstance Rolled(int seed)
        {
            var rolls = new AffixRoll[4];
            for (int i = 0; i < rolls.Length; i++)
            {
                rolls[i] = new AffixRoll((AffixId)((seed + i) % 8), 0.001f * seed + 0.01f * (i + 1), ElementId.None);
            }

            return new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f + 0.37f * seed), rolls, requiredLevel: 1 + seed % 30, new ItemInvestment(3));
        }

        private static CharacterState Wearing(Inventory inventory) =>
            new CharacterState(new ElementId(2), new XpLedger(4, 12.5f, 1, BaseStats.Zero, 0), inventory);

        private static ParticipantMessage RoundTrip(int playerId, int revision, bool full, SaveGame state)
        {
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, playerId, revision, full, state);
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Participant));
            ParticipantMessage back = ParticipantCodec.Read(reader);
            Assert.That(reader.Remaining, Is.Zero);
            return back;
        }

        [Test]
        public void A_players_whole_inventory_travels_in_the_saves_own_format()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.TryEquip(0, 99);
            inventory.Add(Knife(2), 99);
            inventory.Add(Knife(), 99);
            inventory.AutoSell = true;

            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(250), Wearing(inventory), withSack: true);
            ParticipantMessage back = RoundTrip(1, 41, true, state);

            Assert.That(back.PlayerId, Is.EqualTo(1));
            Assert.That(back.Revision, Is.EqualTo(41));
            Assert.That(back.Full, Is.True);
            Assert.That(back.State.Coins, Is.EqualTo(250));
            Assert.That(back.State.AutoSell, Is.True);
            Assert.That(back.State.Sack.Length, Is.EqualTo(2));
            Assert.That(back.State.Characters.Length, Is.EqualTo(1));
            Assert.That(back.State.Characters[0].Level, Is.EqualTo(4));
            Assert.That(back.State.Characters[0].Worn.Length, Is.EqualTo(1), "The worn knife did not travel.");
            Assert.That(back.State.Sack[0].Item.Affixes.Length, Is.EqualTo(2));
        }

        [Test]
        public void A_partners_gear_travels_without_their_sack()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.TryEquip(0, 99);
            inventory.Add(Knife(), 99);

            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(250), Wearing(inventory), withSack: false);
            ParticipantMessage back = RoundTrip(0, 3, false, state);

            Assert.That(back.Full, Is.False);
            Assert.That(back.State.Sack, Is.Empty, "The partner's sack crossed the wire; only what they wear should.");
            Assert.That(back.State.Coins, Is.Zero);
            Assert.That(back.State.Characters[0].Worn.Length, Is.EqualTo(1));
        }

        [Test]
        public void A_full_sack_fits_one_message_with_room_to_spare()
        {
            var inventory = new Inventory();
            for (int i = 0; i < inventory.Rules.Capacity; i++)
            {
                inventory.Add(Rolled(i), 99);
            }

            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(99999), Wearing(inventory), withSack: true);
            var writer = new NetWriter();
            ParticipantCodec.Write(writer, 1, 1, true, state);

            Assert.That(writer.Length, Is.LessThan(NetProtocol.MaxParticipantBytes),
                $"A full sack is {writer.Length} bytes on the wire.");
            Assert.That(RoundTrip(1, 1, true, state).State.Sack.Length, Is.EqualTo(inventory.Rules.Capacity));
        }

        [Test]
        public void Bytes_that_are_not_a_save_are_refused_at_the_door()
        {
            var writer = new NetWriter();
            writer.WriteByte((byte)NetMessageKind.Participant);
            writer.WriteInt(1);
            writer.WriteInt(1);
            writer.WriteBool(true);
            writer.WriteBlob(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            Assert.Throws<NetFormatException>(() => ParticipantCodec.Read(reader));
        }

        [Test]
        public void A_blob_longer_than_it_may_be_is_refused_before_it_is_read()
        {
            var writer = new NetWriter();
            writer.WriteInt(NetProtocol.MaxParticipantBytes + 1);

            Assert.Throws<NetFormatException>(() => new NetReader(writer.ToArray()).ReadBlob(NetProtocol.MaxParticipantBytes));
        }

        [Test]
        public void A_second_restore_replaces_a_loadout_rather_than_adding_to_it()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            inventory.TryEquip(0, 99);
            Assert.That(inventory.Loadout.Weapon.IsEmpty, Is.False);

            var nothingWorn = new CharacterSave(2, 1, 0f, 0, 0, 0, 0, 0, 0, new WornSave[0], 0, 0, 0);
            SaveMapper.RestoreCharacter(nothingWorn, inventory, Catalog);

            Assert.That(inventory.Loadout.Weapon.IsEmpty, Is.True, "A restore wearing nothing left the old knife on.");
        }

        [Test]
        public void A_guests_copy_of_its_sack_takes_the_hosts_revision()
        {
            var sack = new Sack();
            var inventory = new Inventory();
            inventory.Add(Knife(), 99);
            SaveGame state = SaveMapper.Participant(inventory.Sack, new Wallet(5), Wearing(inventory), withSack: true);

            SaveMapper.RestoreSack(state, sack, Catalog, 777);

            Assert.That(sack.Revision, Is.EqualTo(777));
            Assert.That(sack.Items.Count, Is.EqualTo(1));
        }
    }
}
