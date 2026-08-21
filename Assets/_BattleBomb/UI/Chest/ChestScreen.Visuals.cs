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

            RectTransform headerRow = UiBuild.Place(UiBuild.Rect("Header", root), 0f, 0.93f, 0.88f, 1f, 6f);
            _header = UiBuild.Label("Text", headerRow, string.Empty, 15, UiBuild.Ink, TextAnchor.MiddleLeft);

            // Leaving must be obvious and reachable by every input the game has: a tap or click
            // on this, Escape/Start, or Heavy. Michael's M6 pass found the screen with no way
            // out that a player could see, which is the same as having no way out.
            RectTransform closeRow = UiBuild.Place(UiBuild.Rect("Close", root), 0.88f, 0.93f, 1f, 1f, 6f);
            Image closeBox = UiBuild.Box("Back", closeRow, new Color(0.30f, 0.15f, 0.16f, 1f));
            closeBox.raycastTarget = true;
            UiBuild.Label("Text", closeRow, "✕  Esc", 14, UiBuild.Ink, TextAnchor.MiddleCenter);

            var button = closeRow.gameObject.AddComponent<Button>();
            button.targetGraphic = closeBox;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.55f, 0.22f, 0.22f);
            colors.pressedColor = new Color(0.75f, 0.28f, 0.26f);
            button.colors = colors;
            button.onClick.AddListener(CloseFromPointer);

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

            // The item dropdown floats over the grid, anchored under whichever cell is selected.
            _menuRoot = UiBuild.Place(UiBuild.Rect("Dropdown", sack), 0f, 0f, 0.34f, 0.34f);
            UiBuild.Box("Back", _menuRoot, new Color(0.07f, 0.09f, 0.14f, 0.98f));
            RectTransform menuPad = UiBuild.Place(UiBuild.Rect("Pad", _menuRoot), 0f, 0f, 1f, 1f, 8f);
            _menuText = UiBuild.Label("Text", menuPad, string.Empty, 12, UiBuild.Ink, TextAnchor.UpperLeft);
            _menuRoot.gameObject.SetActive(false);

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
        private RectTransform _menuRoot;
        private Text _menuText;

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

            bool sack = _nav.Tab == ChestTab.ItemSack;
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
                : "Stick: move   Light: select   Heavy: back   "
                    + UiBuild.Tint("Esc / Start or ✕: leave", UiBuild.Coin);
        }

        private string TabLine()
        {
            string sack = _nav.Tab == ChestTab.ItemSack ? "[ ITEM SACK ]" : "  Item Sack  ";
            string hero = _nav.Tab == ChestTab.Hero ? "[ HERO ]" : "  Hero  ";
            if (_nav.Focus == ChestFocus.Tabs)
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
                string name = i == _nav.Filter ? $"[{FilterNames[i]}]" : $" {FilterNames[i]} ";
                _text.Append(_nav.Focus == ChestFocus.Filters && i == _nav.Filter
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
                bool selected = cell == _nav.Cursor;
                bool pending = _visible[cell] == _nav.PendingCombine;

                _cells[cell].color = selected && _nav.Focus == ChestFocus.Grid
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

            int bagIndex = _visible.Count > 0 ? _visible[Mathf.Clamp(_nav.Cursor, 0, _visible.Count - 1)] : -1;
            BuildMenu(bagIndex);
            RefreshDropdown(bagIndex);
            RefreshShopRow();

            if (_nav.Focus == ChestFocus.Stock && _nav.StockCursor < _stock.Count)
            {
                _detail.text = DetailFor(_stock[_nav.StockCursor]);
            }
            else if (_nav.Focus == ChestFocus.Upgrade && bagIndex >= 0)
            {
                _detail.text = UpgradePanelFor(items[bagIndex].Item);
            }
            else
            {
                _detail.text = bagIndex >= 0 ? DetailFor(items[bagIndex].Item) : "Nothing here.";
            }
        }

        /// <summary>
        /// The item dropdown, floated under the selected cell so the menu belongs to the thing
        /// it acts on rather than to a strip at the bottom of the screen (Michael, M6 pass).
        /// </summary>
        private void RefreshDropdown(int bagIndex)
        {
            bool showing = _nav.Focus == ChestFocus.Menu && bagIndex >= 0 && _menu.Count > 0;
            _menuRoot.gameObject.SetActive(showing);
            if (!showing)
            {
                return;
            }

            ItemInstance item = _bag.Inventory.Items[bagIndex].Item;

            // Anchored to the selected cell, and flipped upward when the cell is low enough that
            // the list would fall off the bottom.
            int columns = Columns;
            int row = _nav.Cursor / columns;
            int column = _nav.Cursor % columns;
            float cellW = 1f / columns;
            float cellH = 1f / GridRows;
            float gridTop = 0.9f;
            float gridBottom = 0.22f;
            float gridHeight = gridTop - gridBottom;

            float left = 0.6f * (column * cellW);
            float cellBottomY = gridTop - (row + 1) * cellH * gridHeight;
            float height = Mathf.Clamp(0.062f * _menu.Count + 0.03f, 0.1f, 0.5f);
            float bottom = cellBottomY - height;
            if (bottom < 0.1f)
            {
                bottom = cellBottomY + cellH * gridHeight;
            }

            left = Mathf.Min(left, 0.6f - 0.30f);
            UiBuild.Place(_menuRoot, left, bottom, left + 0.30f, bottom + height);

            _text.Clear();
            for (int i = 0; i < _menu.Count; i++)
            {
                string label = LabelFor(_menu[i], item);
                string line = i == _nav.Action ? $"> {label}" : $"  {label}";
                _text.Append(i == _nav.Action ? UiBuild.Tint(line, UiBuild.Focus) : line).Append('\n');
            }

            _menuText.text = _text.ToString();
        }

        /// <summary>
        /// The right panel while deepening: every stat this item can raise, the cursor on one of
        /// them, and the price. The choice is made looking at the numbers it changes.
        /// </summary>
        private string UpgradePanelFor(in ItemInstance item)
        {
            _text.Clear();
            _text.Append(UiBuild.Tint(item.DisplayName, QualityColors.For(item.Quality))).Append('\n');
            _text.Append("Capacity ").Append(item.UpgradesSpent).Append('/').Append(item.UpgradeCapacity);
            _text.Append("   next point ")
                .Append(UiBuild.Tint(_bag.Inventory.Prices.UpgradeCost(item).ToString(), UiBuild.Coin));
            _text.Append("\n   you have ")
                .Append(UiBuild.Tint(_bag.Wallet.Balance.ToString(), UiBuild.Coin));
            _text.Append("\n\nDEEPEN WHICH STAT\n\n");

            for (int i = 0; i < _upgradeTargets.Count; i++)
            {
                string name = NameOf(item, _upgradeTargets[i]);
                float current = ValueOf(item, _upgradeTargets[i]);
                float raised = current * (1f + ItemUpgrade.StepFraction);
                string line = $"{(i == _nav.UpgradeCursor ? ">" : " ")} {name}  {current:F2}  "
                    + $"→ {raised:F2}";
                _text.Append(i == _nav.UpgradeCursor
                    ? UiBuild.Tint(line, UiBuild.Focus)
                    : line).Append('\n');
            }

            _text.Append("\nLight: spend a point   Heavy: back");
            return _text.ToString();
        }

        /// <summary>What a target's stat currently reads, so the panel can show the step.</summary>
        private static float ValueOf(in ItemInstance item, in UpgradeTarget target)
        {
            if (target.IsAffix)
            {
                return target.AffixIndex < item.AffixCount
                    ? item.Affixes[target.AffixIndex].Magnitude
                    : 0f;
            }

            switch (target.CoreStat)
            {
                case CoreStatId.WeaponDamage: return item.CoreStats.WeaponDamage;
                case CoreStatId.SwingSpeed: return item.CoreStats.SwingSpeedBonus;
                case CoreStatId.Defence: return item.CoreStats.Defence;
                case CoreStatId.ShotSpeed: return item.ShotSpeed;
                default: return 0f;
            }
        }

        private void RefreshShopRow()
        {
            _text.Clear();
            if (_stock.Count > 0)
            {
                _text.Append("\nFOR SALE  ");
                for (int i = 0; i < _stock.Count; i++)
                {
                    int price = _bag.Inventory.Prices.BuyPrice(_stock[i]);
                    string label = $" {_stock[i].DisplayName} {price} ";
                    if (i == _nav.StockCursor)
                    {
                        label = $"[{_stock[i].DisplayName} {price}]";
                    }

                    _text.Append(_nav.Focus == ChestFocus.Stock && i == _nav.StockCursor
                        ? UiBuild.Tint(label, UiBuild.Focus)
                        : UiBuild.Tint(label, QualityColors.For(_stock[i].Quality)));
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
                string line = $"{(i == _nav.Cursor && _nav.Focus != ChestFocus.Tabs ? ">" : " ")} {names[i]}  {values[i]}";
                _text.Append(i == _nav.Cursor && _nav.Focus != ChestFocus.Tabs
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
