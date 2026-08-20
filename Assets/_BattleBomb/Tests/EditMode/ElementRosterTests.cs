using BattleBomb.Core.Combat;
using BattleBomb.Gameplay.Data;
using NUnit.Framework;
using UnityEditor;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The authored roster (D46, task 60C): Fire, Ice, Earth, Air as assets, each element's
    /// signature cast pinned at its paper numbers, and the character kit composing the element's
    /// press with its own aura and leap. These are the values Michael's pass plays against.
    /// </summary>
    public sealed class ElementRosterTests
    {
        private static ElementSpec Load(string name)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ElementDefinition>(
                $"Assets/_BattleBomb/Data/Elements/{name}.asset");
            Assert.That(definition, Is.Not.Null, $"{name}.asset exists");
            return definition.ToRuntime();
        }

        [Test]
        public void The_roster_is_four_distinct_authored_elements()
        {
            ElementSpec fire = Load("Fire");
            ElementSpec ice = Load("Ice");
            ElementSpec earth = Load("Earth");
            ElementSpec air = Load("Air");

            Assert.That(fire.Id.Value, Is.EqualTo(1));
            Assert.That(ice.Id.Value, Is.EqualTo(2));
            Assert.That(earth.Id.Value, Is.EqualTo(3));
            Assert.That(air.Id.Value, Is.EqualTo(4));
            Assert.That(fire.Name, Is.EqualTo("Fire"));
            Assert.That(ice.Name, Is.EqualTo("Ice"));
            Assert.That(earth.Name, Is.EqualTo("Earth"));
            Assert.That(air.Name, Is.EqualTo("Air"));
        }

        [Test]
        public void Fire_keeps_the_line_and_the_burn()
        {
            ElementSpec fire = Load("Fire");

            Assert.That(fire.SignatureCast.IsAuthored, Is.True);
            Assert.That(fire.SignatureCast.Delivery, Is.EqualTo(CastDelivery.Hitbox));
            Assert.That(fire.SignatureCast.Attack.Damage, Is.EqualTo(22f));
            Assert.That(fire.SignatureCast.Attack.ReachX, Is.EqualTo(3.2f));
            Assert.That(fire.SignatureCast.ManaCost, Is.EqualTo(20));
            Assert.That(fire.Status.Name, Is.EqualTo("Burn"));
            Assert.That(fire.Status.DamageShare, Is.EqualTo(0.5f));
            Assert.That(fire.Status.MoveScale, Is.EqualTo(1f), "Burn never slows.");
        }

        [Test]
        public void Ice_fires_a_long_bolt_that_chills()
        {
            ElementSpec ice = Load("Ice");

            Assert.That(ice.SignatureCast.Delivery, Is.EqualTo(CastDelivery.Projectile));
            Assert.That(ice.SignatureCast.ProjectileSpeed, Is.EqualTo(14f));
            Assert.That(ice.SignatureCast.Attack.Damage, Is.EqualTo(10f), "Low on purpose.");
            Assert.That(ice.SignatureCast.ManaCost, Is.EqualTo(15));
            Assert.That(ice.Status.Name, Is.EqualTo("Chill"));
            Assert.That(ice.Status.MoveScale, Is.EqualTo(0.55f).Within(1e-4f));
            Assert.That(ice.Status.DamageShare, Is.EqualTo(0f), "A chill takes speed, not health.");
            Assert.That(ice.Status.DurationSteps, Is.EqualTo(180));
        }

        [Test]
        public void Earth_erupts_a_short_wall_that_stuns()
        {
            ElementSpec earth = Load("Earth");

            Assert.That(earth.SignatureCast.Delivery, Is.EqualTo(CastDelivery.Hitbox));
            Assert.That(earth.SignatureCast.Attack.ReachX, Is.EqualTo(1.6f), "Short on purpose.");
            Assert.That(earth.SignatureCast.Attack.Damage, Is.EqualTo(30f));
            Assert.That(earth.SignatureCast.Attack.StunSteps, Is.EqualTo(45));
            Assert.That(earth.SignatureCast.ManaCost, Is.EqualTo(25));
            Assert.That(earth.Status.IsIdle, Is.True, "Earth marks nothing yet — an open dial (D46).");
        }

        [Test]
        public void Air_launches_for_the_juggle()
        {
            ElementSpec air = Load("Air");

            Assert.That(air.SignatureCast.Delivery, Is.EqualTo(CastDelivery.Hitbox));
            Assert.That(air.SignatureCast.Attack.LaunchSpeed, Is.EqualTo(11f),
                "The same pop the L-L-H launcher throws.");
            Assert.That(air.SignatureCast.Attack.ReachX, Is.EqualTo(2.4f));
            Assert.That(air.SignatureCast.Attack.StunSteps, Is.EqualTo(0));
            Assert.That(air.Status.IsIdle, Is.True, "Air marks nothing yet — an open dial (D46).");
        }

        [Test]
        public void The_default_character_casts_its_elements_signature()
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                "Assets/_BattleBomb/Data/Characters/Default.asset");
            Assert.That(character, Is.Not.Null);

            MagicKit kit = character.MagicKitToRuntime();
            ElementSpec fire = Load("Fire");

            Assert.That(kit.Splash.Attack.Damage, Is.EqualTo(fire.SignatureCast.Attack.Damage),
                "The press slot is the element's signature (D46).");
            Assert.That(kit.Splash.ManaCost, Is.EqualTo(fire.SignatureCast.ManaCost));
            Assert.That(kit.Aura.IsAuthored, Is.True, "The aura stays the character's.");
            Assert.That(kit.Leap.LiftSpeed, Is.GreaterThan(0f), "So does the leap.");
        }
    }
}
