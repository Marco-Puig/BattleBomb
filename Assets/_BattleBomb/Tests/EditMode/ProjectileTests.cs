using System.Collections.Generic;
using BattleBomb.Core.Combat;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class ProjectileTests
    {
        private const float Dt = 1f / 60f;
        private const float Radius = 0.6f;

        [Test]
        public void Flight_is_a_straight_line_at_constant_speed()
        {
            ProjectileState p = ProjectileState.Fired(
                Vector3.zero, new Vector3(10f, 0f, 0f), speed: 8f, damage: 5f, Element.None, lifeSteps: 120);

            Vector3 before = p.Velocity;
            p = ProjectileSimulation.Step(p, Dt);
            p = ProjectileSimulation.Step(p, Dt);

            Assert.That(p.Velocity, Is.EqualTo(before), "No homing, no gravity — a straight bolt.");
            Assert.That(p.Position.x, Is.EqualTo(2f * 8f * Dt).Within(1e-4f));
        }

        [Test]
        public void Firing_aims_at_the_target_at_that_moment()
        {
            ProjectileState p = ProjectileState.Fired(
                Vector3.zero, new Vector3(3f, 0f, 4f), speed: 10f, damage: 5f, Element.None, lifeSteps: 120);

            Assert.That(p.Velocity.magnitude, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(p.Velocity.x, Is.EqualTo(6f).Within(1e-4f), "3-4-5 triangle, scaled to speed.");
            Assert.That(p.Velocity.z, Is.EqualTo(8f).Within(1e-4f));
        }

        [Test]
        public void A_bolt_crosses_depth_to_reach_its_target()
        {
            Vector3 deepMuzzle = new Vector3(6f, 0f, 2.5f);
            Vector3 shallowTarget = new Vector3(0f, 0f, -1f);
            ProjectileState p = ProjectileState.Fired(
                deepMuzzle, shallowTarget, speed: 8f, damage: 5f, Element.None, lifeSteps: 120);
            List<Vector3> targets = new List<Vector3> { shallowTarget };

            bool hit = false;
            for (int i = 0; i < 120 && !hit; i++)
            {
                p = ProjectileSimulation.Step(p, Dt);
                hit = ProjectileSimulation.HitTest(p, targets, Radius) == 0;
            }

            Assert.That(hit, Is.True, "Depth is no obstacle — that is ranged's identity (§2.2).");
        }

        [Test]
        public void The_hit_test_respects_its_radius()
        {
            ProjectileState p = new ProjectileState(
                Vector3.zero, Vector3.right, 5f, Element.None, 60);
            List<Vector3> targets = new List<Vector3> { new Vector3(Radius + 0.05f, 0f, 0f) };

            Assert.That(ProjectileSimulation.HitTest(p, targets, Radius), Is.EqualTo(-1));

            targets[0] = new Vector3(Radius - 0.05f, 0f, 0f);
            Assert.That(ProjectileSimulation.HitTest(p, targets, Radius), Is.EqualTo(0));
        }

        [Test]
        public void The_nearest_target_takes_the_hit()
        {
            ProjectileState p = new ProjectileState(
                Vector3.zero, Vector3.right, 5f, Element.None, 60);
            List<Vector3> targets = new List<Vector3>
            {
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0.2f, 0f, 0f),
            };

            Assert.That(ProjectileSimulation.HitTest(p, targets, Radius), Is.EqualTo(1));
        }

        [Test]
        public void Jumping_over_a_bolt_works()
        {
            ProjectileState p = new ProjectileState(
                Vector3.zero, Vector3.right, 5f, Element.None, 60);
            List<Vector3> airborne = new List<Vector3> { new Vector3(0f, 1.5f, 0f) };

            Assert.That(ProjectileSimulation.HitTest(p, airborne, Radius), Is.EqualTo(-1),
                "The bolt flies at firing height — jump is the defence (D26).");
        }

        [Test]
        public void Life_runs_out_and_the_bolt_expires()
        {
            ProjectileState p = ProjectileState.Fired(
                Vector3.zero, Vector3.right, speed: 8f, damage: 5f, Element.None, lifeSteps: 3);

            for (int i = 0; i < 3; i++)
            {
                Assert.That(p.IsExpired, Is.False, $"Still flying at step {i}.");
                p = ProjectileSimulation.Step(p, Dt);
            }

            Assert.That(p.IsExpired, Is.True);
        }

        [Test]
        public void A_degenerate_zero_length_shot_still_flies_somewhere()
        {
            ProjectileState p = ProjectileState.Fired(
                Vector3.one, Vector3.one, speed: 8f, damage: 5f, Element.None, lifeSteps: 60);

            Assert.That(p.Velocity.magnitude, Is.EqualTo(8f).Within(1e-4f),
                "Muzzle on top of the target must not divide by zero.");
        }
    }
}
