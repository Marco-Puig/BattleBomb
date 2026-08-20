using System.Collections.Generic;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The reaction framework (task 53, D41), proved with synthetic elements while the authored
    /// table is deliberately empty. Naming these "setup" and "trigger" rather than after any
    /// element is the point: when O11 settles the roster, the pairs become data and not one line
    /// of this changes.
    /// </summary>
    public sealed class ReactionTests
    {
        private static readonly ElementId Setup = new ElementId(1);
        private static readonly ElementId Trigger = new ElementId(2);
        private static readonly ElementId Bystander = new ElementId(3);

        private static readonly StatusSpec SetupMark = new StatusSpec("Setup", 180, 30, 0f);
        private static readonly StatusSpec TriggerMark = new StatusSpec("Trigger", 120, 30, 0.5f);

        /// <summary>D19's anchor pattern: a setup mark met by a trigger chain-stuns.</summary>
        private static ReactionTable ChainStun(bool consumes = true) => new ReactionTable(
            new List<ReactionEntry>
            {
                new ReactionEntry(Setup, Trigger, new ReactionSpec(0.75f, 45, consumes)),
            });

        private static ElementSpec Element(ElementId id, in StatusSpec status) =>
            new ElementSpec(id, id.ToString(), status);

        private static ElementalStrikeResult Strike(
            StatusTrack track, ElementId element, in StatusSpec status, ReactionTable table,
            float damage = 100f) =>
            ElementalStrike.Apply(
                track, Element(element, status), ElementId.None, table, damage, 1f, 1f);

        [Test]
        public void A_setup_mark_met_by_its_trigger_reacts()
        {
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, ChainStun());

            ElementalStrikeResult result = Strike(track, Trigger, TriggerMark, ChainStun());

            Assert.That(result.Reacted, Is.True);
            Assert.That(result.ReactedWith, Is.EqualTo(Setup));
            Assert.That(result.BurstDamage, Is.EqualTo(75f).Within(1e-4f),
                "Reaction damage is priced from the hit that triggered it.");
            Assert.That(result.StunSteps, Is.EqualTo(45));
        }

        [Test]
        public void The_trigger_still_leaves_its_own_mark_after_reacting()
        {
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, ChainStun());

            ElementalStrikeResult result = Strike(track, Trigger, TriggerMark, ChainStun());

            Assert.That(result.Marked, Is.True);
            Assert.That(track.Has(Trigger), Is.True);
            Assert.That(track.Has(Setup), Is.False, "The consuming reaction spent the setup mark.");
        }

        [Test]
        public void A_reaction_that_does_not_consume_leaves_the_mark_burning()
        {
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, ChainStun(consumes: false));

            Strike(track, Trigger, TriggerMark, ChainStun(consumes: false));

            Assert.That(track.Has(Setup), Is.True);
            Assert.That(track.Count, Is.EqualTo(2));
        }

        [Test]
        public void An_element_never_reacts_with_its_own_mark()
        {
            var reflexive = new ReactionTable(new List<ReactionEntry>
            {
                new ReactionEntry(Setup, Setup, new ReactionSpec(1f, 30, true)),
            });
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, reflexive);

            ElementalStrikeResult result = Strike(track, Setup, SetupMark, reflexive);

            Assert.That(result.Reacted, Is.False,
                "Refreshing your own mark is not a combo — otherwise every rhythm would stun.");
        }

        [Test]
        public void The_pair_is_directional()
        {
            var track = new StatusTrack();
            Strike(track, Trigger, TriggerMark, ChainStun());

            ElementalStrikeResult result = Strike(track, Setup, SetupMark, ChainStun());

            Assert.That(result.Reacted, Is.False,
                "Trigger-then-setup is a different moment from setup-then-trigger, by design.");
        }

        [Test]
        public void An_unpaired_element_passes_through()
        {
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, ChainStun());

            ElementalStrikeResult result = Strike(track, Bystander, TriggerMark, ChainStun());

            Assert.That(result.Reacted, Is.False);
            Assert.That(result.Marked, Is.True, "It still leaves its own mark.");
        }

        [Test]
        public void Only_one_reaction_fires_per_hit()
        {
            var table = new ReactionTable(new List<ReactionEntry>
            {
                new ReactionEntry(Setup, Trigger, new ReactionSpec(0.5f, 10, true)),
                new ReactionEntry(Bystander, Trigger, new ReactionSpec(0.5f, 10, true)),
            });
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, table);
            Strike(track, Bystander, SetupMark, table);

            ElementalStrikeResult result = Strike(track, Trigger, TriggerMark, table);

            Assert.That(result.ReactedWith, Is.EqualTo(Setup), "Track order decides, deterministically.");
            Assert.That(track.Has(Bystander), Is.True, "The second pair did not also fire.");
        }

        [Test]
        public void The_empty_table_is_inert_rather_than_broken()
        {
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, ReactionTable.Empty);

            ElementalStrikeResult result = Strike(track, Trigger, TriggerMark, ReactionTable.Empty);

            Assert.That(ReactionTable.Empty.IsEmpty, Is.True);
            Assert.That(result.Reacted, Is.False, "M5 ships this way: one element, no pairs.");
            Assert.That(result.Marked, Is.True, "Statuses still work perfectly well alone.");
        }

        [Test]
        public void A_duplicate_pair_is_dropped_so_lookup_stays_unambiguous()
        {
            var table = new ReactionTable(new List<ReactionEntry>
            {
                new ReactionEntry(Setup, Trigger, new ReactionSpec(0.5f, 10, true)),
                new ReactionEntry(Setup, Trigger, new ReactionSpec(9f, 999, false)),
            });

            Assert.That(table.Count, Is.EqualTo(1));
            table.TryFind(Setup, Trigger, out ReactionSpec spec);
            Assert.That(spec.StunSteps, Is.EqualTo(10), "The first authored pair wins.");
        }

        [Test]
        public void A_pair_naming_none_is_not_a_pair()
        {
            var table = new ReactionTable(new List<ReactionEntry>
            {
                new ReactionEntry(ElementId.None, Trigger, new ReactionSpec(1f, 10, true)),
                new ReactionEntry(Setup, ElementId.None, new ReactionSpec(1f, 10, true)),
            });

            Assert.That(table.IsEmpty, Is.True, "Kinetic damage reacts with nothing.");
        }

        [Test]
        public void The_same_element_rule_outranks_the_reaction_table()
        {
            var track = new StatusTrack();
            var result = ElementalStrike.Apply(
                track, Element(Trigger, TriggerMark), Trigger, ChainStun(), 100f, 1f, 1f);

            Assert.That(result.Marked, Is.False, "A fire being cannot be burned (D40).");
            Assert.That(result.Reacted, Is.False);
            Assert.That(track.IsEmpty, Is.True);
        }

        [Test]
        public void An_element_with_no_status_still_triggers_reactions()
        {
            var track = new StatusTrack();
            Strike(track, Setup, SetupMark, ChainStun());

            ElementalStrikeResult result = Strike(track, Trigger, default, ChainStun());

            Assert.That(result.Reacted, Is.True, "Being a trigger is not the same as leaving a mark.");
            Assert.That(result.Marked, Is.False);
        }
    }
}
