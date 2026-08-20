using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>What an elemental hit did beyond its damage: a mark, a reaction, or neither.</summary>
    public readonly struct ElementalStrikeResult
    {
        public readonly bool Marked;
        public readonly bool Reacted;

        /// <summary>The mark that reacted — what Presentation flashes and UI names.</summary>
        public readonly ElementId ReactedWith;

        /// <summary>Reaction damage, already priced from the triggering hit.</summary>
        public readonly float BurstDamage;

        public readonly int StunSteps;

        public ElementalStrikeResult(
            bool marked, bool reacted, ElementId reactedWith, float burstDamage, int stunSteps)
        {
            Marked = marked;
            Reacted = reacted;
            ReactedWith = reactedWith;
            BurstDamage = burstDamage;
            StunSteps = stunSteps;
        }
    }

    /// <summary>
    /// One elemental hit's whole effect on a target's statuses (D40/D41), in the order the design
    /// requires: the reaction resolves against what was already burning, and only then does the
    /// incoming element leave its own mark. That order is what stops an element reacting with
    /// itself, and it is why a co-op partner's element is a third source rather than noise.
    /// </summary>
    public static class ElementalStrike
    {
        /// <summary>
        /// Applies one elemental hit. <paramref name="hitDamage"/> is the already-resolved damage
        /// the strike dealt — every number here is priced from it, so gear scaling flows through
        /// statuses and reactions alike without a second tuning axis.
        /// </summary>
        public static ElementalStrikeResult Apply(
            StatusTrack statuses,
            in ElementSpec element,
            ElementId defenderElement,
            ReactionTable reactions,
            float hitDamage,
            float sourceScale,
            float resistanceMultiplier)
        {
            if (statuses == null || !ElementalExchange.CanApplyStatus(element.Id, defenderElement))
            {
                return default;
            }

            bool reacted = false;
            ElementId reactedWith = ElementId.None;
            float burst = 0f;
            int stun = 0;

            if (reactions != null && !reactions.IsEmpty)
            {
                // Track order, so a target carrying two marks reacts the same way every replay.
                for (int i = 0; i < statuses.Active.Count; i++)
                {
                    ElementId carried = statuses.Active[i].Element;
                    if (carried == element.Id
                        || !reactions.TryFind(carried, element.Id, out ReactionSpec spec))
                    {
                        continue;
                    }

                    reacted = true;
                    reactedWith = carried;
                    burst = Mathf.Max(0f, hitDamage) * spec.BurstDamageFraction;
                    stun = spec.StunSteps;
                    if (spec.ConsumesStatus)
                    {
                        statuses.Remove(carried);
                    }

                    break;
                }
            }

            bool marked = false;
            if (!element.Status.IsIdle)
            {
                element.Status.Price(
                    hitDamage, sourceScale, resistanceMultiplier,
                    out int duration, out float damagePerTick);
                marked = statuses.Apply(element.Id, duration, element.Status.TickSteps, damagePerTick);
            }

            return new ElementalStrikeResult(marked, reacted, reactedWith, burst, stun);
        }
    }
}
