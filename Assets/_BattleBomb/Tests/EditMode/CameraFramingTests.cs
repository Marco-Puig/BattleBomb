using System.Collections.Generic;
using BattleBomb.Core.Cameras;
using BattleBomb.Core.Spatial;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CameraFramingTests
    {
        private static readonly CameraTuning Tuning = CameraTuning.Default;
        private static readonly ArenaBounds Arena = new ArenaBounds(-10f, 10f);

        private static float FocusHeight => Arena.GroundY + Tuning.FocusHeight;

        [Test]
        public void No_targets_centres_the_arena_at_base_distance()
        {
            CameraFrame fromEmpty = CameraFraming.Compute(new List<Vector3>(), Tuning, Arena);
            CameraFrame fromNull = CameraFraming.Compute(null, Tuning, Arena);

            Assert.That(fromEmpty.Focus, Is.EqualTo(new Vector3(Arena.Center.x, FocusHeight, 0f)));
            Assert.That(fromEmpty.Distance, Is.EqualTo(Tuning.BaseDistance));
            Assert.That(fromNull.Focus, Is.EqualTo(fromEmpty.Focus));
            Assert.That(fromNull.Distance, Is.EqualTo(fromEmpty.Distance));
        }

        [Test]
        public void One_target_is_centred_at_base_distance()
        {
            CameraFrame frame = CameraFraming.Compute(
                new List<Vector3> { new Vector3(2f, 0f, -1f) }, Tuning, Arena);

            Assert.That(frame.Focus, Is.EqualTo(new Vector3(2f, FocusHeight, 0f)));
            Assert.That(frame.Distance, Is.EqualTo(Tuning.BaseDistance));
        }

        [Test]
        public void Focus_takes_the_midpoint_of_the_extremes_not_a_count_weighted_average()
        {
            CameraFrame frame = CameraFraming.Compute(
                new List<Vector3>
                {
                    new Vector3(-4f, 0f, 0f),
                    new Vector3(-3.5f, 0f, 1f),
                    new Vector3(4f, 0f, -2f),
                },
                Tuning, Arena);

            Assert.That(frame.Focus.x, Is.EqualTo(0f),
                "Two targets clustered on one side must not drag the focus toward them.");
        }

        [Test]
        public void Distance_grows_only_past_the_comfort_width()
        {
            CameraFrame cosy = CameraFraming.Compute(
                new List<Vector3> { new Vector3(-2f, 0f, 0f), new Vector3(2f, 0f, 0f) }, Tuning, Arena);
            CameraFrame spread = CameraFraming.Compute(
                new List<Vector3> { new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f) }, Tuning, Arena);

            Assert.That(cosy.Distance, Is.EqualTo(Tuning.BaseDistance));
            Assert.That(spread.Distance,
                Is.EqualTo(Tuning.BaseDistance + (10f - Tuning.ComfortWidth) * Tuning.DistancePerUnitSpread));
        }

        [Test]
        public void Distance_clamps_at_both_ends()
        {
            ArenaBounds huge = new ArenaBounds(-100f, 100f);
            CameraFrame far = CameraFraming.Compute(
                new List<Vector3> { new Vector3(-50f, 0f, 0f), new Vector3(50f, 0f, 0f) }, Tuning, huge);

            CameraTuning highBase = new CameraTuning(
                new Vector3(0f, 0.45f, -1f), 5f, 10f, 22f, 8f, 0.8f, 1.4f, 4f);
            CameraFrame near = CameraFraming.Compute(
                new List<Vector3> { Vector3.zero }, highBase, Arena);

            Assert.That(far.Distance, Is.EqualTo(Tuning.MaxDistance));
            Assert.That(near.Distance, Is.EqualTo(highBase.MinDistance));
        }

        [Test]
        public void Focus_clamps_at_the_edge_inset()
        {
            CameraFrame frame = CameraFraming.Compute(
                new List<Vector3> { new Vector3(9.5f, 0f, 0f) }, Tuning, Arena);

            Assert.That(frame.Focus.x, Is.EqualTo(Arena.MaxX - Tuning.EdgeInset));
        }

        [Test]
        public void An_arena_narrower_than_twice_the_inset_centres_instead_of_inverting()
        {
            ArenaBounds narrow = new ArenaBounds(-3f, 3f);

            CameraFrame frame = CameraFraming.Compute(
                new List<Vector3> { new Vector3(-2.5f, 0f, 0f) }, Tuning, narrow);

            Assert.That(frame.Focus.x, Is.EqualTo(narrow.Center.x));
        }

        [Test]
        public void Focus_depth_is_always_zero()
        {
            CameraFrame frame = CameraFraming.Compute(
                new List<Vector3> { new Vector3(0f, 0f, 2.9f), new Vector3(1f, 0f, -2.9f) }, Tuning, Arena);

            Assert.That(frame.Focus.z, Is.EqualTo(0f),
                "The depth band is fixed (§2.1) — the camera never tracks depth.");
        }

        [Test]
        public void Camera_position_sits_along_the_offset_direction()
        {
            CameraFrame frame = new CameraFrame(new Vector3(1f, 2f, 0f), 10f);

            Vector3 position = frame.PositionFor(Tuning);

            Assert.That(position, Is.EqualTo(frame.Focus + Tuning.OffsetDirection * 10f));
            Assert.That(Tuning.OffsetDirection.magnitude, Is.EqualTo(1f).Within(1e-5f),
                "The constructor must normalise the offset direction.");
        }
    }
}
