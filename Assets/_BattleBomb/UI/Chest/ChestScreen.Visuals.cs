using System.Collections.Generic;
using System.Text;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.World;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// The chest screen's drawing half: builds the UGUI hierarchy once, then repaints it from
    /// simulation state whenever anything moves. Nothing here mutates — every effect went
    /// through a request first (M6 planning decision 2).
    /// </summary>
    internal sealed partial class ChestScreen
    {
        private readonly StringBuilder _text = new StringBuilder();

        private void Build()
        {
            // The root's placement belongs to the host — solo it fills the display, in couch
            // co-op it takes one half. Stretching it here would silently undo that.
            var root = (RectTransform)transform;
            UiBuild.Box("Backdrop", root, UiBuild.Panel);

            RectTransform headerRow = UiBuild.Place(UiBuild.Rect("Header", root), 0f, 0.93f, 1f, 1f, 6f);
            _header = UiBuild.Label("Text", headerRow, string.Empty, 15, UiBuild.Ink, TextAnchor.MiddleLeft);

            RectTransform tabRow = UiBuild.Place(UiBuild.Rect("Tabs", root), 0f, 0.87f, 1f, 0.93f, 6f);
            _tabStrip = UiBuild.Label("Text", tabRow, string.Empty, 14, UiBuild.Ink, TextAnchor.MiddleLeft);

            // ── Item Sack ────────────────────────────────────────────────────────────
            RectTransform sack = UiBuild.Place(UiBuild.Rect("ItemSack", root), 0f, 0f, 1f, 0.87f);

            RectTransform filterRow = UiBuild.Place(UiBuild.Rect("Filters", sack), 0f, 0.9f, 0.6f, 1f, 6f);
            _filterStrip = UiBuild.Label("Text", filterRow, string.Empty, 12, UiBuild.InkDim, TextAnchor.MiddleLeft);

            _gridRoot = UiBuild.Place(UiBuild.Rect("Grid", sack), 0f, 0.22f, 0.6f, 0.9f, 6f);
            BuildGrid();

            RectTransform actionRow = UiBuild.Place(UiBuild.Rect("Actions", sack), 0f, 0.10f, 0.6f, 0.22f, 6f);
            UiBuild.Box("Back", actionRow, UiBuild.PanelInner);
            _actionStrip = UiBuild.Label("Text", actionRow, string.Empty, 12, UiBuild.Ink, TextAnchor.UpperLeft);

            RectTransform hintRow = UiBuild.Place(UiBuild.Rect("Hint", sack), 0f, 0f, 1f, 0.10f, 6f);
            _hint = UiBuild.Label("Text", hintRow, string.Empty, 11, UiBuild.InkDim, TextAnchor.MiddleLeft);

            RectTransform detail = UiBuild.Place(UiBuild.Rect("Detail", sack), 0.6f, 0.10f, 1f, 1f, 6f);
            UiBuild.Box("Back", detail, UiBuild.PanelInner);
            RectTransform detailPad = UiBuild.Place(UiBuild.Rect("Pad", detail), 0f, 0f, 1f, 1f, 10f);
            _detail = UiBuild.Label("Text", detailPad, string.Empty, 12, UiBuild.Ink, TextAnchor.UpperLeft);

            // ── Hero ─────────────────────────────────────────────────────────────────
            _heroRoot = UiBuild.Place(UiBuild.Rect("Hero", root), 0f, 0f, 1f, 0.87f);
            RectTransform heroLeft = UiBuild.Place(UiBuild.Rect("Left", _heroRoot), 0f, 0f, 0.5f, 1f, 6f);
            UiBuild.Box("Back", heroLeft, UiBuild.PanelInner);
            RectTransform heroLeftPad = UiBuild.Place(UiBuild.Rect("Pad", heroLeft), 0f, 0f, 1f, 1f, 10f);
            _heroLeft = UiBuild.Label("Text", heroLeftPad, string.Empty, 13, UiBuild.Ink, TextAnchor.UpperLeft);

            RectTransform heroRight = UiBuild.Place(UiBuild.Rect("Right", _heroRoot), 0.5f, 0f, 1f, 1f, 6f);
            UiBuild.Box("Back", heroRight, UiBuild.PanelInner);
            RectTransform heroRightPad = UiBuild.Place(UiBuild.Rect("Pad", heroRight), 0f, 0f, 1f, 1f, 10f);
            _heroRight = UiBuild.Label("Text", heroRightPad, string.Empty, 13, UiBuild.Ink, TextAnchor.UpperLeft);

            _sackRoot = sack;
        }

        private RectTransform _sackRoot;

        private void BuildGrid()
        {
            _cells.Clear();
            _cellLabels.Clear();
            int columns = Columns;
            float cellW = 1f / columns;
            float cellH = 1f / GridRows;

            for (int row = 0; row < GridRows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    RectTransform cell = UiBuild.Place(
                        UiBuild.Rect($"Cell {row}x{column}", _gridRoot),
                        column * cellW, 1f - (row + 1) * cellH,
                        (column + 1) * cellW, 1f - row * cellH,
                        3f);
                    Image box = UiBuild.Box("Back", cell, UiBuild.PanelInner);
                    _cells.Add(box);
                    _cellLabels.Add(UiBuild.Label(
                        "Text", cell, string.Empty, 9, UiBuild.Ink, TextAnchor.MiddleCenter));
                }
            }
        }

        private void Refresh()
        {
            if (_bag == null || _header == null)
            {
                return;
            }

            CollectVisible();
            Inventory inventory = _bag.Inventory;

            _header.text =
                $"P{_playerId + 1} — {(_kind == InteractionKind.Chest ? "Chest" : "Shopkeeper")}   "
                + UiBuild.Tint($"{_bag.Wallet.Balance} coin", UiBuild.Coin)
                + $"   Sack {inventory.SlotsUsed}/{inventory.Rules.Capacity}";

            _tabStrip.text = TabLine();

            bool sack = _tab == ChestTab.ItemSack;
            _sackRoot.gameObject.SetActive(sack);
            _heroRoot.gameObject.SetActive(!sack);

            if (sack)
            {
                RefreshSack();
            }
            else
            {
                RefreshHero();
            }

            _hint.text = _flash.Length > 0
                ? UiBuild.Tint(_flash, UiBuild.Coin)
                : "Stick: move   Light: select   Heavy: back / close";
        }

        private string TabLine()
        {
            string sack = _tab == ChestTab.ItemSack ? "[ ITEM SACK ]" : "  Item Sack  ";
            string hero = _tab == ChestTab.Hero ? "[ HERO ]" : "  Hero  ";
            if (_focus == ChestFocus.Tabs)
            {
                sack = UiBuild.Tint(sack, UiBuild.Focus);
                hero = UiBuild.Tint(hero, UiBuild.Focus);
            }

            return sack + "   " + hero;
        }

        private void RefreshSack()
        {
            _text.Clear();
            for (int i = 0; i < FilterNames.Length; i++)
            {
                string name = i == _filter ? $"[{FilterNames[i]}]" : $" {FilterNames[i]} ";
                _text.Append(_focus == ChestFocus.Filters && i == _filter
                    ? UiBuild.Tint(name, UiBuild.Focus)
                    : name);
            }

            _filterStrip.text = _text.ToString();

            IReadOnlyList<ItemStack> items = _bag.Inventory.Items;
            for (int cell = 0; cell < _cells.Count; cell++)
            {
                if (cell >= _visible.Count)
                {
                    _cells[cell].color = UiBuild.PanelInner;
                    _cellLabels[cell].text = string.Empty;
                    continue;
                }

                ItemStack stack = items[_visible[cell]];
                Color quality = QualityColors.For(stack.Item.Quality);
                bool selected = cell == _cursor;
                bool pending = _visible[cell] == _pendingCombine;

                _cells[cell].color = selected && _focus == ChestFocus.Grid
                    ? Color.Lerp(quality, UiBuild.Focus, 0.45f)
                    : quality * 0.75f;

                _text.Clear();
                _text.Append(Abbreviate(stack.Item));
                if (stack.Count > 1)
                {
                    _text.Append("\nx").Append(stack.Count);
                }

                if (stack.Item.Locked)
                {
                    _text.Append("\nL");
                }

                if (pending)
                {
                    _text.Append("\n*");
                }

                _cellLabels[cell].text = _text.ToString();
            }

            int bagIndex = _visible.Count > 0 ? _visible[Mathf.Clamp(_cursor, 0, _visible.Count - 1)] : -1;
            BuildActions(bagIndex);
            RefreshActions();
            _detail.text = bagIndex >= 0 ? DetailFor(items[bagIndex].Item) : "Nothing here.";
        }

        private void RefreshActions()
        {
            _text.Clear();
            for (int i = 0; i < _actions.Count; i++)
            {
                string label = i == _action ? $"[{_actions[i]}]" : $" {_actions[i]} ";
                _text.Append(_focus == ChestFocus.Actions && i == _action
                    ? UiBuild.Tint(label, UiBuild.Focus)
                    : label);
                if (i % 3 == 2)
                {
                    _text.Append('\n');
                }
            }

            _actionStrip.text = _text.ToString();
        }

        private static string Abbreviate(in ItemInstance item)
        {
            switch (item.Slot)
            {
                case ItemSlot.Helmet: return "HELM";
                case ItemSlot.Chest: return "CHEST";
                case ItemSlot.Boots: return "BOOT";
                case ItemSlot.Weapon: return item.WeaponClass == WeaponClass.Bow ? "BOW" : "BLADE";
                case ItemSlot.Pet: return "PET";
                case ItemSlot.Equipment: return "EQUIP";
                default: return "POTION";
            }
        }

        /// <summary>
        /// The selected item, and how it compares to what is already worn — one glance, no
        /// separate compare view (D42's screen, section 1).
        /// </summary>
        private string DetailFor(in ItemInstance item)
        {
            _text.Clear();
            _text.Append(UiBuild.Tint(item.DisplayName, QualityColors.For(item.Quality))).Append('\n');
            _text.Append(item.Quality).Append("  ·  requires level ").Append(item.RequiredLevel);
            if (item.Locked)
            {
                _text.Append("  ·  ").Append(UiBuild.Tint("LOCKED", UiBuild.Coin));
            }

            _text.Append("\n\n");

            if (item.IsConsumable)
            {
                _text.Append("Restores ").Append((item.ConsumableHealFraction * 100f).ToString("F0"))
                    .Append("% ").Append(item.Restores == RestoreKind.Mana ? "mana" : "health");
                return _text.ToString();
            }

            ItemInstance worn = _bag.Inventory.Loadout.Worn(item.Slot);
            GearContribution mine = item.TotalContribution();
            GearContribution theirs = worn.IsEmpty ? default : worn.TotalContribution();
            _text.Append(worn.IsEmpty ? "Nothing worn in this slot.\n" : $"vs {worn.DisplayName}\n");

            AppendStat("Damage", mine.WeaponDamage, theirs.WeaponDamage, "F0");
            AppendStat("Swing", mine.SwingSpeedBonus, theirs.SwingSpeedBonus, "F2");
            AppendStat("Defence", mine.Defence * 100f, theirs.Defence * 100f, "F0");
            AppendStat("Weight", mine.Weight, theirs.Weight, "F1", lowerIsBetter: true);
            AppendStat("Crit", mine.CritChance * 100f, theirs.CritChance * 100f, "F0");
            AppendStat("Crit dmg", mine.CritDamageBonus * 100f, theirs.CritDamageBonus * 100f, "F0");
            AppendStat("Life steal", mine.LifeSteal * 100f, theirs.LifeSteal * 100f, "F0");
            AppendStat("Max HP", mine.MaxHealthBonus, theirs.MaxHealthBonus, "F0");
            AppendStat("Max mana", mine.MaxManaBonus, theirs.MaxManaBonus, "F0");
            AppendStat("Mana regen", mine.ManaRegen, theirs.ManaRegen, "F1");
            AppendStat("Knockback", mine.KnockbackBonus * 100f, theirs.KnockbackBonus * 100f, "F0");
            AppendStat("Magic dmg", mine.MagicDamage, theirs.MagicDamage, "F0");
            AppendStat("Magic range", mine.MagicRange, theirs.MagicRange, "F1");

            for (int i = 0; i < item.AffixCount; i++)
            {
                AffixRoll roll = item.Affixes[i];
                if (roll.Id == AffixId.WeaponInfusion || roll.Id == AffixId.ElementalResistance)
                {
                    _text.Append(roll.Id).Append(' ').Append(roll.Magnitude.ToString("F2")).Append('\n');
                }
            }

            _text.Append("\nUpgrade capacity ")
                .Append(item.UpgradesSpent).Append('/').Append(item.UpgradeCapacity);
            if (ItemUpgrade.CanUpgrade(item))
            {
                _text.Append("  ·  next point ")
                    .Append(UiBuild.Tint(_bag.Inventory.Prices.UpgradeCost(item).ToString(), UiBuild.Coin));
            }

            return _text.ToString();
        }

        private void AppendStat(string name, float mine, float theirs, string format, bool lowerIsBetter = false)
        {
            if (Mathf.Approximately(mine, 0f) && Mathf.Approximately(theirs, 0f))
            {
                return;
            }

            _text.Append(name).Append(' ').Append(mine.ToString(format));
            float delta = mine - theirs;
            if (Mathf.Abs(delta) > 0.005f)
            {
                bool better = lowerIsBetter ? delta < 0f : delta > 0f;
                string arrow = better ? "▲" : "▼";
                _text.Append("  ").Append(UiBuild.Tint(
                    $"{arrow} {(delta > 0f ? "+" : string.Empty)}{delta.ToString(format)}",
                    better ? UiBuild.Better : UiBuild.Worse));
            }

            _text.Append('\n');
        }

        private void RefreshHero()
        {
            StatSheet sheet = _sheetSource != null ? _sheetSource.Sheet : default;
            _text.Clear();
            _text.Append("Level ").Append(_bag.Level);
            if (_bag.Ledger.PrestigeCount > 0)
            {
                _text.Append("   ").Append(UiBuild.Tint($"★{_bag.Ledger.PrestigeCount}", UiBuild.Coin));
            }

            _text.Append("\nXP ").Append(_bag.Ledger.XpIntoLevel.ToString("F0")).Append(" / ")
                .Append(_bag.Curve.XpToNext(_bag.Ledger.Level, _bag.Ledger.PrestigeCount).ToString("F0"));
            _text.Append("\nPoints to spend: ")
                .Append(UiBuild.Tint(_bag.Ledger.UnspentPoints.ToString(), UiBuild.Coin));
            _text.Append("\n\n");

            string[] names = { "Strength", "HP", "Mana", "Speed" };
            BaseStats allocations = _bag.Ledger.Allocations;
            int[] values =
            {
                allocations.Strength, allocations.Hp, allocations.Mana, allocations.Speed,
            };

            for (int i = 0; i < names.Length; i++)
            {
                string line = $"{(i == _cursor && _focus != ChestFocus.Tabs ? ">" : " ")} {names[i]}  {values[i]}";
                _text.Append(i == _cursor && _focus != ChestFocus.Tabs
                    ? UiBuild.Tint(line, UiBuild.Focus)
                    : line).Append('\n');
            }

            _text.Append("\nWith gear:\n");
            _text.Append("Damage ").Append(sheet.WeaponDamage.ToString("F0"))
                .Append("   Defence ").Append((sheet.Defence * 100f).ToString("F0")).Append("%\n");
            _text.Append("Crit ").Append((sheet.CritChance * 100f).ToString("F0"))
                .Append("%   Swing x").Append(sheet.SwingSpeedMultiplier.ToString("F2")).Append('\n');
            _text.Append("Speed x").Append(sheet.NetMoveSpeedMultiplier.ToString("F2"))
                .Append("   Mana ").Append(sheet.MaxMana.ToString("F0"));
            _heroLeft.text = _text.ToString();

            _text.Clear();
            _text.Append("EQUIPPED\n\n");
            AppendWorn("Helmet", ItemSlot.Helmet, 0);
            AppendWorn("Chest", ItemSlot.Chest, 0);
            AppendWorn("Boots", ItemSlot.Boots, 0);
            AppendWorn("Weapon", ItemSlot.Weapon, 0);
            AppendWorn("Pet", ItemSlot.Pet, 0);
            AppendWorn("Equipment 1", ItemSlot.Equipment, 0);
            AppendWorn("Equipment 2", ItemSlot.Equipment, 1);
            _text.Append("\nQuick-use: ").Append(_bag.Inventory.QuickKind);
            _heroRight.text = _text.ToString();
        }

        private void AppendWorn(string label, ItemSlot slot, int equipmentIndex)
        {
            ItemInstance worn = _bag.Inventory.Loadout.Worn(slot, equipmentIndex);
            _text.Append(label).Append(": ");
            _text.Append(worn.IsEmpty
                ? UiBuild.Tint("—", UiBuild.InkDim)
                : UiBuild.Tint(worn.DisplayName, QualityColors.For(worn.Quality)));
            _text.Append('\n');
        }
    }
}
