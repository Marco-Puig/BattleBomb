using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Gameplay.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class EnemyDefinitionTests
    {
        [Test]
        public void Authored_values_carry_through_to_the_runtime_spec()
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            try
            {
                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("_archetype").enumValueIndex = (int)EnemyArchetype.Caster;
                serialized.FindProperty("_element").enumValueIndex = (int)Element.Fire;
                serialized.FindProperty("_attack._startupSteps").intValue = 36;
                serialized.FindProperty("_attack._damage").floatValue = 10f;
                serialized.FindProperty("_cooldownSteps").intValue = 150;
                serialized.FindProperty("_interruptible").boolValue = true;
                serialized.FindProperty("_staggerSteps").intValue = 18;
                serialized.FindProperty("_projectileSpeed").floatValue = 6f;
                serialized.FindProperty("_standoffNearX").floatValue = 4f;
                serialized.FindProperty("_standoffFarX").floatValue = 8f;
                serialized.FindProperty("_maxSpeed").floatValue = 2.2f;
                serialized.FindProperty("_maxHealth").floatValue = 25f;
                serialized.FindProperty("_rank").intValue = 3;
                serialized.FindProperty("_xpReward").intValue = 40;
                serialized.FindProperty("_resistFire").floatValue = 0.5f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EnemySpec spec = definition.ToRuntime();

                Assert.That(spec.Tuning.Archetype, Is.EqualTo(EnemyArchetype.Caster));
                Assert.That(spec.Tuning.Element, Is.EqualTo(Element.Fire));
                Assert.That(spec.Tuning.Attack.StartupSteps, Is.EqualTo(36), "The telegraph.");
                Assert.That(spec.Tuning.Attack.Damage, Is.EqualTo(10f));
                Assert.That(spec.Tuning.CooldownSteps, Is.EqualTo(150));
                Assert.That(spec.Tuning.StaggerSteps, Is.EqualTo(18));
                Assert.That(spec.Tuning.ProjectileSpeed, Is.EqualTo(6f));
                Assert.That(spec.Tuning.StandoffNearX, Is.EqualTo(4f));
                Assert.That(spec.Tuning.StandoffFarX, Is.EqualTo(8f));
                Assert.That(spec.Movement.MaxSpeed, Is.EqualTo(2.2f));
                Assert.That(spec.Movement.JumpSpeed, Is.EqualTo(0f), "Enemies do not jump.");
                Assert.That(spec.MaxHealth, Is.EqualTo(25f));
                Assert.That(spec.Rank, Is.EqualTo(3));
                Assert.That(spec.XpReward, Is.EqualTo(40));
                Assert.That(spec.Resistances.For(Element.Fire), Is.EqualTo(0.5f));
                Assert.That(spec.Resistances.For(Element.Water), Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void A_fresh_definition_is_playable_without_edits()
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            try
            {
                EnemySpec spec = definition.ToRuntime();

                Assert.That(spec.MaxHealth, Is.GreaterThan(0f));
                Assert.That(spec.Tuning.Attack.StartupSteps, Is.GreaterThan(0));
                Assert.That(spec.Tuning.CooldownSteps, Is.GreaterThan(0));
                Assert.That(spec.Tuning.StaggerSteps, Is.GreaterThan(0));
                Assert.That(spec.Movement.MaxSpeed, Is.GreaterThan(0f));
                Assert.That(spec.Rank, Is.GreaterThanOrEqualTo(1));
                Assert.That(spec.Tuning.StandoffFarX, Is.GreaterThanOrEqualTo(spec.Tuning.StandoffNearX),
                    "A backwards standoff band would make the ranged archetype oscillate.");
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
