using System.Collections.Generic;
using BattleBomb.Core.Movement;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The soft crowding seam (task 37): gentle, capped, planar, deterministic — and never a
    /// cushion against knockback or the launcher.
    /// </summary>
    public sealed class BodySeparationTests
    {
        private static List<Vector3> Resolve(List<Vector3> positions, List<Vector3> velocities)
        {
            var displacements = new List<Vector3>();
            BodySeparation.Resolve(positions, velocities, displacements);
            return displacements;
        }

        private static List<Vector3> AtRest(int count)
        {
            var velocities = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                velocities.Add(Vector3.zero);
            }

            return velocities;
        }

        [Test]
        public void Overlapping_bodies_push_apart()
        {
            var positions = new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(0.4f, 0f, 0f) };

            var displacements = Resolve(positions, AtRest(2));

            Assert.That(displacements[0].x, Is.Negative);
            Assert.That(displacements[1].x, Is.Positive);
        }

        [Test]
        public void Separated_bodies_stay_put()
        {
            var positions = new List<Vector3> { Vector3.zero, new Vector3(2f, 0f, 0f) };

            var displacements = Resolve(positions, AtRest(2));

            Assert.That(displacements[0], Is.EqualTo(Vector3.zero));
            Assert.That(displacements[1], Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void The_push_never_exceeds_its_cap()
        {
            var positions = new List<Vector3> { Vector3.zero, new Vector3(0.05f, 0f, 0f) };

            var displacements = Resolve(positions, AtRest(2));

            Assert.That(displacements[0].magnitude, Is.LessThanOrEqualTo(BodySeparation.MaxPushPerStep + 1e-5f));
            Assert.That(displacements[1].magnitude, Is.LessThanOrEqualTo(BodySeparation.MaxPushPerStep + 1e-5f));
        }

        [Test]
        public void Height_is_never_touched()
        {
            var positions = new List<Vector3> { new Vector3(0f, 1.5f, 0f), new Vector3(0.3f, 0f, 0.2f) };

            var displacements = Resolve(positions, AtRest(2));

            Assert.That(displacements[0].y, Is.Zero);
            Assert.That(displacements[1].y, Is.Zero);
        }

        [Test]
        public void A_fast_body_receives_no_push_but_still_gives_one()
        {
            var positions = new List<Vector3> { Vector3.zero, new Vector3(0.4f, 0f, 0f) };
            var velocities = new List<Vector3> { new Vector3(8f, 0f, 0f), Vector3.zero };

            var displacements = Resolve(positions, velocities);

            Assert.That(displacements[0], Is.EqualTo(Vector3.zero), "the shoved body is never cushioned");
            Assert.That(displacements[1].x, Is.Positive, "the crowd still parts around it");
        }

        [Test]
        public void A_launched_body_is_also_left_alone()
        {
            var positions = new List<Vector3> { Vector3.zero, new Vector3(0.4f, 0f, 0f) };
            var velocities = new List<Vector3> { new Vector3(0f, 6f, 0f), Vector3.zero };

            var displacements = Resolve(positions, velocities);

            Assert.That(displacements[0], Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Perfectly_stacked_bodies_split_deterministically()
        {
            var positions = new List<Vector3> { Vector3.zero, Vector3.zero };

            var first = Resolve(positions, AtRest(2));
            var second = Resolve(positions, AtRest(2));

            Assert.That(first[0].x, Is.Negative);
            Assert.That(first[1].x, Is.Positive);
            Assert.That(first[0], Is.EqualTo(second[0]));
            Assert.That(first[1], Is.EqualTo(second[1]));
        }

        [Test]
        public void A_pile_spreads_outward()
        {
            var positions = new List<Vector3>
            {
                new Vector3(-0.3f, 0f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0.3f, 0f, 0f),
            };

            var displacements = Resolve(positions, AtRest(3));

            Assert.That(displacements[0].x, Is.Negative, "the left edge moves left");
            Assert.That(displacements[2].x, Is.Positive, "the right edge moves right");
            Assert.That(Mathf.Abs(displacements[1].x), Is.LessThan(1e-5f), "the middle is squeezed evenly");
        }
    }
}
