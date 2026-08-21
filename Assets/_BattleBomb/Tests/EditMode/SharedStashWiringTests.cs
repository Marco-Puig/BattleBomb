using System.Reflection;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Items;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D51's wiring, not just the Core rules behind it: the real <see cref="SharedStash"/>
    /// MonoBehaviour and two real <see cref="PlayerInventory"/> instances, the way the scene
    /// actually builds them. <see cref="SharedSackTests"/> proves the sack-sharing rules against
    /// bare <see cref="Inventory"/> instances; this proves the event plumbing on top of them —
    /// the seam where a request firing <c>Changed</c> once directly and once more through
    /// <see cref="SharedStash.NotifyChanged"/> would double-count and nothing here would notice
    /// unless it was actually counted.
    ///
    /// Neither component is <c>[ExecuteAlways]</c> (by design — gameplay must not run in the
    /// editor), so <c>OnEnable</c> never fires from <c>AddComponent</c> or a <c>SetActive</c>
    /// cycle outside Play mode; both were tried and verified not to fire it here. The subscription
    /// `OnEnable` wires — <c>Stash.Changed += OnStashChanged</c> — is invoked directly by
    /// reflection instead, which still exercises the real method body Play mode would call.
    /// </summary>
    public sealed class SharedStashWiringTests
    {
        private const BindingFlags OnEnableFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static void FireOnEnable(Object component)
        {
            MethodInfo method = component.GetType().GetMethod("OnEnable", OnEnableFlags);
            Assert.That(method, Is.Not.Null,
                $"{component.GetType().Name} has no private OnEnable — did it get renamed?");
            method.Invoke(component, null);
        }

        private static ItemInstance Helmet(int id) => new ItemInstance(
            new ItemIdentity(id, $"Helm {id}", ItemSlot.Helmet), QualityRank.Shiny,
            new GearContribution(defence: 3f), new AffixRoll[0], requiredLevel: 1);

        [Test]
        public void A_take_and_a_sell_each_fire_changed_exactly_once_per_player()
        {
            var stashGo = new GameObject("Stash");
            var oneGo = new GameObject("Player One");
            var twoGo = new GameObject("Player Two");
            try
            {
                SharedStash stash = stashGo.AddComponent<SharedStash>();
                PlayerInventory one = oneGo.AddComponent<PlayerInventory>();
                PlayerInventory two = twoGo.AddComponent<PlayerInventory>();

                FireOnEnable(stash);
                FireOnEnable(one);
                FireOnEnable(two);

                int oneFired = 0;
                int twoFired = 0;
                one.Changed += () => oneFired++;
                two.Changed += () => twoFired++;

                one.Take(Helmet(1));

                Assert.That(oneFired, Is.EqualTo(1), "The taker's own screen redraws exactly once.");
                Assert.That(twoFired, Is.EqualTo(1), "The partner's screen redraws exactly once, not zero and not twice.");
                Assert.That(two.Inventory.Items.Count, Is.EqualTo(1),
                    "Sharing and notifying are the same mechanism: the partner's view already has it.");

                oneFired = 0;
                twoFired = 0;

                int coins = one.RequestSell(0);

                Assert.That(coins, Is.GreaterThan(0), "The sale actually paid out.");
                Assert.That(oneFired, Is.EqualTo(1), "The seller's screen redraws exactly once.");
                Assert.That(twoFired, Is.EqualTo(1), "The partner's screen redraws exactly once for the sale too.");
            }
            finally
            {
                Object.DestroyImmediate(oneGo);
                Object.DestroyImmediate(twoGo);
                Object.DestroyImmediate(stashGo);
            }
        }
    }
}
