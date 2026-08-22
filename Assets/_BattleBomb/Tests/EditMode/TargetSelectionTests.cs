using System.Collections.Generic;
using BattleBomb.Core.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class TargetSelectionTests
    {
        private const float Margin = 1.5f;

        private static readonly List<Vector3> Pair = new List<Vector3>
        {
            new Vector3(2f, 0f, 0f),   // player 0, nearer
            new Vector3(5f, 0f, 0f),   // player 1
        };

        private static readonly List<bool> BothUp = new List<bool> { false, false };

        [Test]
        public void Uncommitted_picks_the_nearest()
        {
            Assert.That(TargetSelection.Choose(Vector3.zero, Pair, BothUp, -1, Margin), Is.EqualTo(0));
        }

        [Test]
        public void The_current_target_is_kept_inside_the_margin()
        {
            int kept = TargetSelection.Choose(Vector3.zero, Pair, BothUp, 1, Margin + 1.6f);

            Assert.That(kept, Is.EqualTo(1),
                "A pair straddling the enemy must not cause flip-flopping — stickiness wins.");
        }

        [Test]
        public void A_clearly_closer_player_takes_the_aggro()
        {
            int switched = TargetSelection.Choose(Vector3.zero, Pair, BothUp, 1, Margin);

            Assert.That(switched, Is.EqualTo(0), "Three units closer beats a 1.5 margin.");
        }

        [Test]
        public void Downed_players_are_never_targets()
        {
            List<bool> firstDown = new List<bool> { true, false };

            Assert.That(TargetSelection.Choose(Vector3.zero, Pair, firstDown, 0, Margin), Is.EqualTo(1),
                "The current target going down forces the switch (D25).");
        }

        [Test]
        public void Everyone_down_means_no_target()
        {
            List<bool> bothDown = new List<bool> { true, true };

            Assert.That(TargetSelection.Choose(Vector3.zero, Pair, bothDown, 0, Margin), Is.EqualTo(-1));
        }

        [Test]
        public void Depth_counts_in_the_distance()
        {
            List<Vector3> deepAndShallow = new List<Vector3>
            {
                new Vector3(1f, 0f, 2.9f),   // planar ~3.07
                new Vector3(3f, 0f, 0f),     // planar 3.0 — nearer once depth counts
            };

            Assert.That(TargetSelection.Choose(Vector3.zero, deepAndShallow, BothUp, -1, Margin),
                Is.EqualTo(1));
        }
    }
}
