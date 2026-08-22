using System;
using BattleBomb.Core.Spatial;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class ArenaBoundsTests
    {
        [Test]
        public void X_is_clamped_at_each_edge_and_left_alone_inside()
        {
            ArenaBounds bounds = new ArenaBounds(-5f, 5f);

            Assert.That(bounds.ClampHorizontal(new Vector3(-9f, 0f, 0f)).x, Is.EqualTo(-5f));
            Assert.That(bounds.ClampHorizontal(new Vector3(9f, 0f, 0f)).x, Is.EqualTo(5f));
            Assert.That(bounds.ClampHorizontal(new Vector3(1.25f, 0f, 0f)).x, Is.EqualTo(1.25f));
        }

        [Test]
        public void Z_is_clamped_to_the_depth_band_whatever_the_arena_width()
        {
            ArenaBounds narrow = new ArenaBounds(-2f, 2f);
            ArenaBounds wide = new ArenaBounds(-100f, 100f);

            Assert.That(narrow.ClampHorizontal(new Vector3(0f, 0f, 50f)).z, Is.EqualTo(DepthBand.Max));
            Assert.That(wide.ClampHorizontal(new Vector3(0f, 0f, -50f)).z, Is.EqualTo(DepthBand.Min));
            Assert.That(wide.ClampHorizontal(new Vector3(0f, 0f, 1.5f)).z, Is.EqualTo(1.5f));
        }

        [Test]
        public void Y_passes_through_untouched_even_far_above_the_ground()
        {
            ArenaBounds bounds = ArenaBounds.Default;

            Assert.That(bounds.ClampHorizontal(new Vector3(50f, 123.4f, 50f)).y, Is.EqualTo(123.4f));
        }

        [Test]
        public void Center_sits_mid_x_at_ground_height_at_zero_depth()
        {
            ArenaBounds bounds = new ArenaBounds(2f, 10f, groundY: 1f);

            Assert.That(bounds.Center, Is.EqualTo(new Vector3(6f, 1f, 0f)));
        }

        [Test]
        public void Contains_tests_x_and_z_and_ignores_y()
        {
            ArenaBounds bounds = ArenaBounds.Default;

            Assert.That(bounds.Contains(new Vector3(0f, 999f, 0f)), Is.True);
            Assert.That(bounds.Contains(new Vector3(11f, 0f, 0f)), Is.False);
            Assert.That(bounds.Contains(new Vector3(0f, 0f, DepthBand.Max + 0.1f)), Is.False);
        }

        [Test]
        public void A_backwards_x_extent_throws()
        {
            Assert.Throws<ArgumentException>(() => _ = new ArenaBounds(5f, -5f));
        }

        [Test]
        public void The_depth_band_is_six_units_centred_on_zero()
        {
            Assert.That(DepthBand.Width, Is.EqualTo(6f));
            Assert.That(DepthBand.Min, Is.EqualTo(-DepthBand.Max));
            Assert.That(DepthBand.Clamp(4f), Is.EqualTo(DepthBand.Max));
            Assert.That(DepthBand.Clamp(-4f), Is.EqualTo(DepthBand.Min));
        }
    }
}
