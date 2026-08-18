using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class HitResolverTests
    {
        private static readonly AttackTuning Attack = new AttackTuning(
            startupSteps: 1, activeSteps: 1, recoverySteps: 1,
            damage: 5f, reachX: 1.6f, depthTolerance: 1f, lungeDistance: 1.2f,
            maxTargets: 2, knockbackSpeed: 5f, launchSpeed: 0f, hitstopSteps: 3);

        private static readonly Vector3 Origin = Vector3.zero;

        private readonly List<int> _hits = new List<int>();

        private int Resolve(Vector3 attacker, Facing facing, params Vector3[] candidates) =>
            HitResolver.Resolve(attacker, facing, Attack, candidates, _hits);

        [Test]
        public void An_aligned_but_deep_candidate_is_missed()
        {
            int count = Resolve(Origin, Facing.Right, new Vector3(1f, 0f, 2f));

            Assert.That(count, Is.EqualTo(0),
                "Depth-limited melee (§2.2): lateral alignment cannot beat the depth band.");
        }

        [Test]
        public void Hits_come_back_nearest_first()
        {
            Resolve(Origin, Facing.Right,
                new Vector3(1.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f));

            Assert.That(_hits, Is.EqualTo(new[] { 1, 0 }));
        }

        [Test]
        public void Max_targets_caps_a_crowd_keeping_the_nearest()
        {
            int count = Resolve(Origin, Facing.Right,
                new Vector3(1.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(1f, 0f, 0.5f));

            Assert.That(count, Is.EqualTo(Attack.MaxTargets));
            Assert.That(_hits, Is.EqualTo(new[] { 1, 2 }), "The farthest of the three is dropped.");
        }

        [Test]
        public void A_candidate_behind_the_attacker_is_missed_but_a_flush_one_is_hit()
        {
            Assert.That(Resolve(Origin, Facing.Right, new Vector3(-1f, 0f, 0f)), Is.EqualTo(0));
            Assert.That(Resolve(Origin, Facing.Right, new Vector3(-0.2f, 0f, 0f)), Is.EqualTo(1),
                "A target flush against the attacker's back edge still counts.");
        }

        [Test]
        public void Facing_left_mirrors_the_reach()
        {
            Assert.That(Resolve(Origin, Facing.Left, new Vector3(-1f, 0f, 0f)), Is.EqualTo(1));
            Assert.That(Resolve(Origin, Facing.Left, new Vector3(1f, 0f, 0f)), Is.EqualTo(0));
        }

        [Test]
        public void A_launched_target_is_still_hittable_but_not_one_far_above()
        {
            Assert.That(Resolve(Origin, Facing.Right, new Vector3(1f, 2f, 0f)), Is.EqualTo(1));
            Assert.That(Resolve(Origin, Facing.Right, new Vector3(1f, 5f, 0f)), Is.EqualTo(0));
        }

        [Test]
        public void Beyond_reach_is_missed()
        {
            Assert.That(Resolve(Origin, Facing.Right, new Vector3(2f, 0f, 0f)), Is.EqualTo(0));
        }

        [Test]
        public void The_lunge_reaches_past_the_attack_but_not_forever()
        {
            var inLungeRange = new[] { new Vector3(2.5f, 0f, 0f) };
            var tooFar = new[] { new Vector3(3.5f, 0f, 0f) };

            Assert.That(HitResolver.LungeTarget(Origin, Facing.Right, Attack, inLungeRange), Is.EqualTo(0));
            Assert.That(HitResolver.LungeTarget(Origin, Facing.Right, Attack, tooFar), Is.EqualTo(-1));
        }

        [Test]
        public void The_lunge_turns_a_depth_near_miss_into_a_target()
        {
            var nearMiss = new[] { new Vector3(1f, 0f, 1.8f) };

            Assert.That(HitResolver.LungeTarget(Origin, Facing.Right, Attack, nearMiss), Is.EqualTo(0),
                "§2.2: the soft lunge closes small depth gaps.");
            Assert.That(HitResolver.Resolve(Origin, Facing.Right, Attack, nearMiss, _hits), Is.EqualTo(0),
                "Without the lunge the same candidate is a miss.");
        }

        [Test]
        public void The_lunge_picks_the_nearest_of_several()
        {
            var candidates = new[]
            {
                new Vector3(2.5f, 0f, 0f),
                new Vector3(1.2f, 0f, 0.4f),
                new Vector3(-0.5f, 0f, 0f),
            };

            Assert.That(HitResolver.LungeTarget(Origin, Facing.Right, Attack, candidates), Is.EqualTo(1));
        }

        [Test]
        public void Null_or_empty_candidates_resolve_to_nothing()
        {
            Assert.That(HitResolver.Resolve(Origin, Facing.Right, Attack, null, _hits), Is.EqualTo(0));
            Assert.That(HitResolver.LungeTarget(Origin, Facing.Right, Attack, null), Is.EqualTo(-1));
            Assert.That(Resolve(Origin, Facing.Right), Is.EqualTo(0));
        }
    }
}
