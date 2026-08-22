using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class RadialHitTests
    {
        private static AttackTuning Slam(float radius = 2f, int maxTargets = 8) => new AttackTuning(
            startupSteps: 4, activeSteps: 2, recoverySteps: 12,
            damage: 14f, reachX: radius, depthTolerance: 1f, lungeDistance: 0f,
            maxTargets: maxTargets, knockbackSpeed: 10f, launchSpeed: 0f, hitstopSteps: 5,
            moveSpeedScale: 0.3f, resolvesOnLanding: true);

        private readonly List<int> _hits = new List<int>();

        [Test]
        public void The_burst_hits_all_around_the_landing_point()
        {
            AttackTuning slam = Slam();
            List<Vector3> candidates = new List<Vector3>
            {
                new Vector3(1.5f, 0f, 0f),     // in front
                new Vector3(-1.5f, 0f, 0f),    // behind — a facing-free burst still reaches it
                new Vector3(0f, 0f, 1.8f),     // deep
            };

            int count = HitResolver.ResolveRadial(Vector3.zero, slam, candidates, _hits);

            Assert.That(count, Is.EqualTo(3), "A slam has no front (D19).");
        }

        [Test]
        public void The_radius_is_the_edge_of_the_burst()
        {
            AttackTuning slam = Slam(radius: 2f);
            List<Vector3> candidates = new List<Vector3>
            {
                new Vector3(1.9f, 0f, 0f),
                new Vector3(2.1f, 0f, 0f),
            };

            HitResolver.ResolveRadial(Vector3.zero, slam, candidates, _hits);

            Assert.That(_hits, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Depth_beyond_melee_tolerance_is_still_hit_inside_the_radius()
        {
            AttackTuning slam = Slam(radius: 2f);
            Vector3 deep = new Vector3(0f, 0f, 1.8f);   // outside DepthTolerance 1, inside the radius
            List<Vector3> candidates = new List<Vector3> { deep };

            HitResolver.Resolve(Vector3.zero, Facing.Right, slam, candidates, _hits);
            Assert.That(_hits, Is.Empty, "The directional swing is depth-limited (§2.2)…");

            HitResolver.ResolveRadial(Vector3.zero, slam, candidates, _hits);
            Assert.That(_hits, Is.EqualTo(new[] { 0 }), "…the slam's burst is not.");
        }

        [Test]
        public void Nearest_candidates_win_when_the_burst_is_capped()
        {
            AttackTuning slam = Slam(radius: 3f, maxTargets: 2);
            List<Vector3> candidates = new List<Vector3>
            {
                new Vector3(1.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
            };

            HitResolver.ResolveRadial(Vector3.zero, slam, candidates, _hits);

            Assert.That(_hits, Is.EqualTo(new[] { 1, 2 }), "Nearest first on planar distance.");
        }

        [Test]
        public void A_target_far_above_the_landing_is_missed()
        {
            AttackTuning slam = Slam();
            List<Vector3> candidates = new List<Vector3>
            {
                new Vector3(1f, HitResolver.VerticalTolerance + 0.5f, 0f),
            };

            HitResolver.ResolveRadial(Vector3.zero, slam, candidates, _hits);

            Assert.That(_hits, Is.Empty);
        }
    }
}
