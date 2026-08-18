using BattleBomb.Core.Combat;
using BattleBomb.Gameplay.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CombatKitDefinitionTests
    {
        [Test]
        public void A_fresh_definition_matches_the_default_kit()
        {
            CombatKitDefinition definition = ScriptableObject.CreateInstance<CombatKitDefinition>();
            try
            {
                CombatKit kit = definition.ToRuntime();
                CombatKit expected = CombatKit.Default;

                Assert.That(kit.ChainLength, Is.EqualTo(expected.ChainLength));
                for (int i = 0; i < expected.ChainLength; i++)
                {
                    Assert.That(kit.StepAt(i).OnLight, Is.EqualTo(expected.StepAt(i).OnLight), $"Light link {i + 1}");
                    Assert.That(kit.StepAt(i).HasHeavy, Is.EqualTo(expected.StepAt(i).HasHeavy), $"Ender flag {i + 1}");
                    if (expected.StepAt(i).HasHeavy)
                    {
                        Assert.That(kit.StepAt(i).OnHeavy, Is.EqualTo(expected.StepAt(i).OnHeavy), $"Ender {i + 1}");
                    }
                }

                Assert.That(kit.Heavy, Is.EqualTo(expected.Heavy));
                Assert.That(kit.ChargedHeavy, Is.EqualTo(expected.ChargedHeavy));
                Assert.That(kit.ChargeThresholdSteps, Is.EqualTo(expected.ChargeThresholdSteps));
                Assert.That(kit.ComboWindowSteps, Is.EqualTo(expected.ComboWindowSteps));
                Assert.That(kit.InputBufferSteps, Is.EqualTo(expected.InputBufferSteps));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Authored_values_carry_through_to_the_runtime_kit()
        {
            CombatKitDefinition definition = ScriptableObject.CreateInstance<CombatKitDefinition>();
            try
            {
                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("_chargeThresholdSteps").intValue = 45;
                serialized.FindProperty("_chain.Array.data[2]._ender._launchSpeed").floatValue = 11f;
                serialized.FindProperty("_chain.Array.data[0]._light._damage").floatValue = 7f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                CombatKit kit = definition.ToRuntime();

                Assert.That(kit.ChargeThresholdSteps, Is.EqualTo(45));
                Assert.That(kit.StepAt(2).OnHeavy.LaunchSpeed, Is.EqualTo(11f));
                Assert.That(kit.StepAt(0).OnLight.Damage, Is.EqualTo(7f));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void An_emptied_chain_falls_back_to_the_default_kit()
        {
            CombatKitDefinition definition = ScriptableObject.CreateInstance<CombatKitDefinition>();
            try
            {
                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("_chain").arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                CombatKit kit = definition.ToRuntime();

                Assert.That(kit.ChainLength, Is.EqualTo(CombatKit.Default.ChainLength),
                    "An author mistake must degrade to defaults, not break combat.");
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void A_character_without_a_kit_uses_the_default()
        {
            CharacterDefinition definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            try
            {
                CombatKit kit = definition.CombatKitToRuntime();

                Assert.That(kit.ChainLength, Is.EqualTo(CombatKit.Default.ChainLength));
                Assert.That(kit.Heavy, Is.EqualTo(CombatKit.Default.Heavy));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
