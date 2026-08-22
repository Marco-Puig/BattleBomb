using BattleBomb.Gameplay.Characters;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class TargetRegistryTests
    {
        [Test]
        public void Dummies_come_back_in_name_order_regardless_of_registration_order()
        {
            GameObject b = new GameObject("Dummy B");
            GameObject a = new GameObject("Dummy A");
            try
            {
                // Inactive so OnEnable does not run and hunt for a driver mid-test.
                b.SetActive(false);
                a.SetActive(false);
                TrainingDummy dummyB = b.AddComponent<TrainingDummy>();
                TrainingDummy dummyA = a.AddComponent<TrainingDummy>();

                TargetRegistry registry = new TargetRegistry();
                registry.Register(dummyB);
                registry.Register(dummyA);
                registry.Register(dummyB);

                Assert.That(registry.Ordered.Count, Is.EqualTo(2), "Double registration is ignored.");
                Assert.That(registry.Ordered[0], Is.SameAs(dummyA),
                    "Name order, not registration order (D10).");

                registry.Unregister(dummyA);
                Assert.That(registry.Ordered.Count, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }
}
