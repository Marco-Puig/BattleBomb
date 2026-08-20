using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;

namespace BattleBomb.UI.Items
{
    /// <summary>
    /// Turns one <see cref="ItemInstance"/> into the lines a card prints — the loot panel now,
    /// the debug equip panel next, M6's real comparison UI eventually. Formatting only; every
    /// number is already decided.
    /// </summary>
    public static class ItemText
    {
        public static void BuildLines(
            in ItemInstance item, List<string> lines, ElementCatalog elements = null)
        {
            lines.Clear();

            var core = new List<string>(4);
            GearContribution stats = item.CoreStats;
            if (stats.WeaponDamage > 0f) core.Add($"Damage {stats.WeaponDamage:F0}");
            if (stats.SwingSpeedBonus > 0.0001f) core.Add($"Swing +{stats.SwingSpeedBonus * 100f:F0}%");
            if (item.ShotSpeed > 0f) core.Add($"Shot {item.ShotSpeed:F0}");
            if (stats.Defence > 0f) core.Add($"Defence {stats.Defence * 100f:F0}%");
            if (stats.Weight > 0f) core.Add($"Weight {stats.Weight:F1}");
            if (stats.CritChance > 0f) core.Add($"Crit {stats.CritChance * 100f:F0}%");
            if (stats.CritDamageBonus > 0f) core.Add($"Crit dmg +{stats.CritDamageBonus * 100f:F0}%");
            if (stats.LifeSteal > 0f) core.Add($"Steal {stats.LifeSteal * 100f:F1}%");
            if (stats.MaxHealthBonus > 0f) core.Add($"HP +{stats.MaxHealthBonus:F0}");
            if (stats.MaxManaBonus > 0f) core.Add($"Mana +{stats.MaxManaBonus:F0}");
            if (stats.ManaRegen > 0f) core.Add($"Regen {stats.ManaRegen:F1}/s");
            if (stats.KnockbackBonus > 0f) core.Add($"Knockback +{stats.KnockbackBonus * 100f:F0}%");
            if (stats.MagicDamage > 0f) core.Add($"Magic +{stats.MagicDamage:F0}");
            if (stats.MagicRange > 0f) core.Add($"Magic range +{stats.MagicRange:F1}");
            if (item.ConsumableHealFraction > 0f) core.Add($"Heals {item.ConsumableHealFraction * 100f:F0}%");
            if (core.Count > 0)
            {
                lines.Add(string.Join(" · ", core));
            }

            for (int i = 0; i < item.AffixCount; i++)
            {
                lines.Add(AffixLine(item.Affixes[i], elements));
            }

            if (item.UpgradeCapacity > 0)
            {
                lines.Add($"Upgrades 0/{item.UpgradeCapacity}");
            }

            if (item.RequiredLevel > 1)
            {
                lines.Add($"Requires level {item.RequiredLevel}");
            }
        }

        /// <summary>
        /// One affix as a line. Element names come from the authored catalog (D38) — without one,
        /// the id's debug text stands in rather than a hardcoded roster.
        /// </summary>
        public static string AffixLine(in AffixRoll affix, ElementCatalog elements = null)
        {
            float m = affix.Magnitude;
            string element = elements != null
                ? elements.NameOf(affix.Element)
                : affix.Element.ToString();
            switch (affix.Id)
            {
                case AffixId.CritChance: return $"+{m * 100f:F1}% crit chance";
                case AffixId.CritDamage: return $"+{m * 100f:F0}% crit damage";
                case AffixId.LifeSteal: return $"+{m * 100f:F1}% life steal";
                case AffixId.MaxHealth: return $"+{m:F0} max health";
                case AffixId.MaxMana: return $"+{m:F0} max mana";
                case AffixId.ManaRegen: return $"+{m:F1} mana regen";
                case AffixId.ReducedWeight: return $"-{m * 100f:F0}% weight";
                case AffixId.KnockbackPower: return $"+{m * 100f:F0}% knockback";
                case AffixId.MagicDamage: return $"+{m:F0} magic damage";
                case AffixId.MagicRange: return $"+{m:F1} magic range";
                case AffixId.ElementalResistance: return $"{element} resistance +{m * 100f:F0}%";
                case AffixId.WeaponInfusion: return $"{element} infusion ({m * 100f:F0}%)";
                default: return string.Empty;
            }
        }
    }
}
