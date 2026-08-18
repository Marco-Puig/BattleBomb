using BattleBomb.Core.Movement;
using BattleBomb.Gameplay.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CharacterDefinitionTests
    {
        [Test]
        public void ToRuntime_carries_every_authored_value_through()
        {
            CharacterDefinition definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            try
            {
                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("_displayName").stringValue = "Roundtrip";
                serialized.FindProperty("_maxSpeed").floatValue = 7.5f;
                serialized.FindProperty("_acceleration").floatValue = 55f;
                serialized.FindProperty("_deceleration").floatValue = 70f;
                serialized.FindProperty("_depthSpeedScale").floatValue = 0.9f;
                serialized.FindProperty("_gravity").floatValue = 40f;
                serialized.FindProperty("_jumpSpeed").floatValue = 11f;
                serialized.FindProperty("_maxFallSpeed").floatValue = 25f;
                serialized.FindProperty("_jumpCutMultiplier").floatValue = 0.5f;
                serialized.FindProperty("_coyoteSteps").intValue = 4;
                serialized.FindProperty("_jumpBufferSteps").intValue = 5;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                MovementTuning tuning = definition.ToRuntime();

                Assert.That(definition.DisplayName, Is.EqualTo("Roundtrip"));
                Assert.That(tuning.MaxSpeed, Is.EqualTo(7.5f));
                Assert.That(tuning.Acceleration, Is.EqualTo(55f));
                Assert.That(tuning.Deceleration, Is.EqualTo(70f));
                Assert.That(tuning.DepthSpeedScale, Is.EqualTo(0.9f));
                Assert.That(tuning.Gravity, Is.EqualTo(40f));
                Assert.That(tuning.JumpSpeed, Is.EqualTo(11f));
                Assert.That(tuning.MaxFallSpeed, Is.EqualTo(25f));
                Assert.That(tuning.JumpCutMultiplier, Is.EqualTo(0.5f));
                Assert.That(tuning.CoyoteSteps, Is.EqualTo(4));
                Assert.That(tuning.JumpBufferSteps, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void A_fresh_definition_matches_the_default_tuning()
        {
            CharacterDefinition definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            try
            {
                Assert.That(definition.ToRuntime(), Is.EqualTo(MovementTuning.Default),
                    "The authoring defaults and MovementTuning.Default must not drift apart.");
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
