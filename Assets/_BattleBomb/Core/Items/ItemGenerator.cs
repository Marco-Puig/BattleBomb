using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Stats;
using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// §5.3's whole pipeline in one pure entry point: score → rank, kind, definition, core stat
    /// roll, affix rolls, level stamp. Deterministic from the injected stream — same seed and
    /// context, same item, on every machine (D10) — and identically callable by story and
    /// endless (D12). Draw counts vary per item; the replayed sequence is what's deterministic
    /// (planning decision 1).
    /// </summary>
    public static class ItemGenerator
    {
        /// <summary>Within a rank the roll spans this fraction of the budget up to the full budget.</summary>
        public const float RollSpreadMin = 0.85f;

        public static DeterministicRandom Roll(
            in DeterministicRandom rng,
            in GenerationContext context,
            out ItemInstance item)
        {
            DeterministicRandom next = rng;
            if (context.Catalog == null || context.Catalog.Count == 0)
            {
                item = default;
                return next;
            }

            QualityRank rank = context.Table.RankFor(context.QualityScore);
            if (rank < context.MinQuality)
            {
                rank = context.MinQuality;
            }

            QualityRow row = context.Table.For(rank);

            next = PickSpec(next, context, out ItemSpec spec, out bool found);
            if (!found)
            {
                item = default;
                return next;
            }

            next = next.NextFloat(out float spreadDraw);
            float spread = RollSpreadMin + (1f - RollSpreadMin) * spreadDraw;
            GearContribution core = spec.BaseStats.Scaled(row.StatBudget * spread);

            AffixRoll[] affixes = System.Array.Empty<AffixRoll>();
            if (spec.Slot != ItemSlot.Consumable && row.AffixMax > 0)
            {
                next = next.NextFloat(out float countDraw);
                int count = row.AffixMin + (int)(countDraw * (row.AffixMax - row.AffixMin + 1));
                count = Mathf.Min(count, row.AffixMax);
                if (count > 0)
                {
                    next = RollAffixes(next, spec.Slot, count, row.StatBudget, context.Elements, out affixes);
                }
            }

            // The container is the potency (Michael's mega pass): a Vial heals a sip, an Elixir
            // heals most of a pool — the rank's budget scales the effect like any other stat.
            float heal = spec.ConsumableHealFraction > 0f
                ? Mathf.Clamp01(spec.ConsumableHealFraction * row.StatBudget)
                : 0f;

            item = new ItemInstance(
                spec.Id,
                ItemNaming.Compose(rank, spec.Slot, spec.Name),
                spec.Slot,
                spec.WeaponClass,
                spec.PetClass,
                rank,
                core,
                affixes,
                requiredLevel: Mathf.Max(1, context.ProgressLevel),
                upgradeCapacity: row.UpgradeCapacity,
                upgradesSpent: 0,
                shotSpeed: spec.ShotSpeed,
                consumableHealFraction: heal,
                restores: spec.Restores,
                // An active's damage is a share of the wearer's weapon damage (D19/D37), so the
                // ladder scales how big the moment is without ever letting it outgrow the build.
                activeWeaponDamageShare: spec.ActiveWeaponDamageShare * row.StatBudget,
                activeElement: spec.ActiveElement,
                activeRadius: spec.ActiveRadius,
                activeCooldownSteps: spec.ActiveCooldownSteps);
            return next;
        }

        private static DeterministicRandom PickSpec(
            in DeterministicRandom rng,
            in GenerationContext context,
            out ItemSpec spec,
            out bool found)
        {
            DeterministicRandom next = rng;
            IReadOnlyList<ItemSpec> catalog = context.Catalog;

            if (context.HasForcedDefinition)
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    if (catalog[i].Id == context.ForcedDefinitionId)
                    {
                        spec = catalog[i];
                        found = true;
                        return next;
                    }
                }
            }

            ItemSlot slot;
            if (context.HasForcedSlot && CountInSlot(catalog, context.ForcedSlot) > 0)
            {
                slot = context.ForcedSlot;
            }
            else
            {
                next = PickSlot(next, context, out slot, out bool anySlot);
                if (!anySlot)
                {
                    spec = default;
                    found = false;
                    return next;
                }
            }

            int matches = CountInSlot(catalog, slot);
            next = next.NextFloat(out float pickDraw);
            int target = Mathf.Min(matches - 1, (int)(pickDraw * matches));
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].Slot != slot)
                {
                    continue;
                }

                if (target == 0)
                {
                    spec = catalog[i];
                    found = true;
                    return next;
                }

                target--;
            }

            spec = default;
            found = false;
            return next;
        }

        private static DeterministicRandom PickSlot(
            in DeterministicRandom rng,
            in GenerationContext context,
            out ItemSlot slot,
            out bool any)
        {
            float total = 0f;
            for (int s = 0; s <= (int)ItemSlot.Consumable; s++)
            {
                if (CountInSlot(context.Catalog, (ItemSlot)s) > 0)
                {
                    total += Mathf.Max(0f, context.Weights.For((ItemSlot)s));
                }
            }

            DeterministicRandom next = rng.NextFloat(out float draw);
            if (total <= 0f)
            {
                slot = default;
                any = false;
                return next;
            }

            float cursor = draw * total;
            for (int s = 0; s <= (int)ItemSlot.Consumable; s++)
            {
                if (CountInSlot(context.Catalog, (ItemSlot)s) == 0)
                {
                    continue;
                }

                cursor -= Mathf.Max(0f, context.Weights.For((ItemSlot)s));
                if (cursor < 0f)
                {
                    slot = (ItemSlot)s;
                    any = true;
                    return next;
                }
            }

            slot = ItemSlot.Consumable;
            any = true;
            return next;
        }

        private static int CountInSlot(IReadOnlyList<ItemSpec> catalog, ItemSlot slot)
        {
            int count = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].Slot == slot)
                {
                    count++;
                }
            }

            return count;
        }

        private static DeterministicRandom RollAffixes(
            in DeterministicRandom rng,
            ItemSlot slot,
            int count,
            float budget,
            IReadOnlyList<ElementId> elements,
            out AffixRoll[] affixes)
        {
            bool hasElements = elements != null && elements.Count > 0;
            var pool = new List<AffixId>
            {
                AffixId.CritChance,
                AffixId.CritDamage,
                AffixId.LifeSteal,
                AffixId.MaxHealth,
                AffixId.MaxMana,
                AffixId.ManaRegen,
                AffixId.ReducedWeight,
                AffixId.KnockbackPower,
                AffixId.MagicDamage,
                AffixId.MagicRange,
            };

            // The element-flavoured affixes only exist while a roster does (D38) — with nothing
            // authored they leave the pool rather than rolling an affix that names no element.
            if (hasElements)
            {
                pool.Add(AffixId.ElementalResistance);
                if (slot == ItemSlot.Weapon)
                {
                    pool.Add(AffixId.WeaponInfusion);
                }
            }

            DeterministicRandom next = rng;
            count = Mathf.Min(count, pool.Count);
            affixes = new AffixRoll[count];

            for (int i = 0; i < count; i++)
            {
                next = next.NextFloat(out float pickDraw);
                int index = Mathf.Min(pool.Count - 1, (int)(pickDraw * pool.Count));
                AffixId id = pool[index];
                pool.RemoveAt(index);

                next = next.NextFloat(out float magnitudeDraw);
                MagnitudeRange(id, out float min, out float max);
                float magnitude = Mathf.Lerp(min, max, magnitudeDraw) * budget;

                ElementId element = ElementId.None;
                if (hasElements && (id == AffixId.ElementalResistance || id == AffixId.WeaponInfusion))
                {
                    next = next.NextFloat(out float elementDraw);
                    element = elements[Mathf.Min(elements.Count - 1, (int)(elementDraw * elements.Count))];
                }

                affixes[i] = new AffixRoll(id, magnitude, element);
            }

            return next;
        }

        private static void MagnitudeRange(AffixId id, out float min, out float max)
        {
            switch (id)
            {
                case AffixId.CritChance: min = 0.02f; max = 0.06f; break;
                case AffixId.CritDamage: min = 0.10f; max = 0.30f; break;
                case AffixId.LifeSteal: min = 0.01f; max = 0.04f; break;
                case AffixId.MaxHealth: min = 10f; max = 30f; break;
                case AffixId.MaxMana: min = 10f; max = 30f; break;
                case AffixId.ManaRegen: min = 0.2f; max = 0.6f; break;
                case AffixId.ReducedWeight: min = 0.05f; max = 0.15f; break;
                case AffixId.KnockbackPower: min = 0.05f; max = 0.20f; break;
                case AffixId.MagicDamage: min = 5f; max = 15f; break;
                case AffixId.MagicRange: min = 0.5f; max = 1.5f; break;
                case AffixId.ElementalResistance: min = 0.05f; max = 0.15f; break;
                // Infusion's magnitude IS its share of a cast's mark, so the ladder decides how
                // hard the sword burns: a bottom roll is a tenth of a cast, a Godly one matches it.
                case AffixId.WeaponInfusion: min = 0.2f; max = 0.4f; break;
                default: min = 0f; max = 0f; break;
            }
        }
    }
}
