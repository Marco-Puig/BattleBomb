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

        /// <summary>
        /// Two halves that are never resized (UI Pass 01). Solo and online the sack takes the left
        /// half and the hero half stays mostly clear, because the world camera has pushed the real
        /// character into it. In local co-op this whole screen is already one player's half of the
        /// display, so the two become tabs instead and the camera does not move at all — the other
        /// player is still using it.
        ///
        /// Either way the sack panel is the same width, which is why the grid never reflows.
        /// </summary>
        private void Build()
        {
            // The root's placement belongs to the host — solo it fills the display, in couch
            // co-op it takes one half. Stretching it here would silently undo that.
            var root = (RectTransform)transform;

            _sackRoot = UiBuild.Place(UiBuild.Rect("ItemSack", root), 0f, 0f, _split ? 1f : 0.5f, 1f);
            UiBuild.Box("Board", _sackRoot, UiBuild.Board);

            // The chest's hinge: a brass edge down the seam between the halves.
            Image seam = UiBuild.Box("Seam", _sackRoot, UiBuild.BrassDim);
            RectTransform seamRect = seam.rectTransform;
            seamRect.anchorMin = new Vector2(1f, 0f);
            seamRect.anchorMax = new Vector2(1f, 1f);
            seamRect.pivot = new Vector2(1f, 0.5f);
            seamRect.sizeDelta = new Vector2(5f, 0f);

            RectTransform pad = UiBuild.Rect("Pad", _sackRoot);
            pad.anchorMin = Vector2.zero;
            pad.anchorMax = Vector2.one;
            pad.offsetMin = new Vector2(30f, 26f);
            pad.offsetMax = new Vector2(-27f, -26f);

            BuildSackHead(pad);
            BuildFilterRow(pad);

            // The grid and its popover share a parent so the popover can overhang the cells.
            RectTransform gridArea = UiBuild.Rect("GridArea", pad);
            gridArea.anchorMin = new Vector2(0f, 0f);
            gridArea.anchorMax = new Vector2(1f, 1f);
            gridArea.offsetMin = new Vector2(0f, 300f);
            gridArea.offsetMax = new Vector2(0f, -128f);

            _gridRoot = UiBuild.Rect("Grid", gridArea);
            UiBuild.Stretch(_gridRoot);
            BuildGrid();
            _popover = new ActionPopover(gridArea, 6);

            RectTransform compare = UiBuild.Rect("CompareArea", pad);
            compare.anchorMin = new Vector2(0f, 0f);
            compare.anchorMax = new Vector2(1f, 0f);
            compare.pivot = new Vector2(0.5f, 0f);
            compare.sizeDelta = new Vector2(0f, 232f);
            compare.anchoredPosition = new Vector2(0f, 44f);
            _compare = new ComparePanel(compare);

            // The shopkeeper's rack still reads as a strip. Its designed form — explicit BUY and
            // SELL modes with a row per roll — is the next pass; this keeps the shop working
            // rather than leaving it with nowhere to draw.
            _shopStrip = UiBuild.Label("Shop", pad, string.Empty, 12, UiBuild.Bone,
                TextAnchor.LowerLeft, UiBuild.Ui);
            RectTransform shop = _shopStrip.rectTransform;
            shop.anchorMin = new Vector2(0f, 0f);
            shop.anchorMax = new Vector2(1f, 0f);
            shop.pivot = new Vector2(0.5f, 0f);
            shop.sizeDelta = new Vector2(0f, 40f);
            shop.anchoredPosition = new Vector2(0f, 280f);

            _hint = UiBuild.Label("Hint", pad, string.Empty, 12, UiBuild.Muted,
                TextAnchor.LowerLeft, UiBuild.Ui);
            UiBuild.Stretch(_hint.rectTransform);

            // ── Hero ─────────────────────────────────────────────────────────────────
            _heroRoot = UiBuild.Place(
                UiBuild.Rect("Hero", root), _split ? 0f : 0.5f, 0f, 1f, 1f);

            // Solo there is deliberately no backdrop at all (Michael, 2026-08-22): the camera
            // frames the hero to fill this half, so there is little world left to dim and any
            // scrim would only sit between the player and their own character. Split has no
            // camera move to show through, so it keeps the board.
            if (_split)
            {
                UiBuild.Box("Board", _heroRoot, UiBuild.Board);
            }

            _heroPanel = new HeroPanel(_heroRoot);
        }

        /// <summary>Title, the wallet, and how full the sack is.</summary>
        private void BuildSackHead(RectTransform pad)
        {
            if (_split)
            {
                _tabSack = BuildTab(pad, "SACK", 0f);
                _tabHero = BuildTab(pad, "HERO", 132f);
            }

            _sackTitle = UiBuild.Label("Title", pad, "ITEM SACK", 38, UiBuild.Bone,
                TextAnchor.UpperLeft, UiBuild.Display);
            UiBuild.Pin(_sackTitle.rectTransform, 0f, _split ? 42f : 0f, 420f, 44f);

            float headTop = _split ? 46f : 4f;

            Image coinPill = UiBuild.Box("CoinPill", pad, UiBuild.Well);
            RectTransform pill = UiBuild.Pin(coinPill.rectTransform, 0f, headTop, 108f, 30f);
            pill.anchorMin = new Vector2(1f, 1f);
            pill.anchorMax = new Vector2(1f, 1f);
            pill.pivot = new Vector2(1f, 1f);
            pill.anchoredPosition = new Vector2(-150f, -headTop);
            _coin = UiBuild.Label("Coin", coinPill.rectTransform, string.Empty, 15, UiBuild.Gold,
                TextAnchor.MiddleCenter, UiBuild.Ui);

            _sackMeterText = UiBuild.Label("SackText", pad, string.Empty, 9, UiBuild.Faint,
                TextAnchor.UpperRight, UiBuild.Mono);
            RectTransform meterText = UiBuild.Pin(_sackMeterText.rectTransform, 0f, headTop, 140f, 14f);
            meterText.anchorMin = new Vector2(1f, 1f);
            meterText.anchorMax = new Vector2(1f, 1f);
            meterText.pivot = new Vector2(1f, 1f);
            meterText.anchoredPosition = new Vector2(-28f, -headTop);

            _sackMeter = UiBuild.Meter("SackMeter", pad, UiBuild.Brass);
            RectTransform well = (RectTransform)_sackMeter.transform.parent;
            well.anchorMin = new Vector2(1f, 1f);
            well.anchorMax = new Vector2(1f, 1f);
            well.pivot = new Vector2(1f, 1f);
            well.sizeDelta = new Vector2(118f, 7f);
            well.anchoredPosition = new Vector2(-28f, -(headTop + 18f));

            // Leaving must be reachable by every input the game has: a tap or click on this,
            // Escape/Start, or Heavy. M6's pass found a screen with no visible way out, which is
            // the same as having none.
            Image closeBox = UiBuild.Box("Close", pad, UiBuild.Well);
            closeBox.raycastTarget = true;
            RectTransform close = UiBuild.Pin(closeBox.rectTransform, 0f, headTop, 22f, 22f);
            close.anchorMin = new Vector2(1f, 1f);
            close.anchorMax = new Vector2(1f, 1f);
            close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(0f, -headTop);
            UiBuild.Label("X", closeBox.rectTransform, "✕", 13, UiBuild.Muted,
                TextAnchor.MiddleCenter, UiBuild.Ui);

            var button = closeBox.gameObject.AddComponent<Button>();
            button.targetGraphic = closeBox;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.55f, 0.22f, 0.22f);
            colors.pressedColor = new Color(0.75f, 0.28f, 0.26f);
            button.colors = colors;
            button.onClick.AddListener(CloseFromPointer);

            Image rule = UiBuild.Rule("Rule", pad, 3f, UiBuild.BrassDim);
            rule.rectTransform.anchoredPosition = new Vector2(0f, _split ? -96f : -54f);
        }

        private Text BuildTab(RectTransform pad, string caption, float x)
        {
            Image back = UiBuild.Box($"Tab {caption}", pad, UiBuild.BoardDeep);
            UiBuild.Pin(back.rectTransform, x, 0f, 126f, 34f);
            return UiBuild.Label("Text", back.rectTransform, caption, 19, UiBuild.Muted,
                TextAnchor.MiddleCenter, UiBuild.Display);
        }

        /// <summary>The six category chips, and the sort the design puts opposite them.</summary>
        private void BuildFilterRow(RectTransform pad)
        {
            float top = _split ? 108f : 66f;
            float x = 0f;
            for (int i = 0; i < FilterNames.Length; i++)
            {
                float width = 34f + FilterNames[i].Length * 8f;
                Image chip = UiBuild.Box($"Filter {i}", pad, UiBuild.Well);
                UiBuild.Pin(chip.rectTransform, x, top, width, 30f);
                _filterChips.Add(chip);
                _filterLabels.Add(UiBuild.Label("Text", chip.rectTransform, FilterNames[i], 12,
                    UiBuild.Muted, TextAnchor.MiddleCenter, UiBuild.Ui));
                x += width + 6f;
            }

            Image sortBox = UiBuild.Box("Sort", pad, UiBuild.Well);
            RectTransform sort = UiBuild.Pin(sortBox.rectTransform, 0f, top, 150f, 30f);
            sort.anchorMin = new Vector2(1f, 1f);
            sort.anchorMax = new Vector2(1f, 1f);
            sort.pivot = new Vector2(1f, 1f);
            sort.anchoredPosition = new Vector2(0f, -top);
            UiBuild.Label("Label", sortBox.rectTransform, "SORT", 9, UiBuild.Brass,
                TextAnchor.MiddleLeft, UiBuild.Mono).rectTransform.offsetMin = new Vector2(10f, 0f);
            _sortText = UiBuild.Label("Value", sortBox.rectTransform, "Quality", 12, UiBuild.Bone,
                TextAnchor.MiddleRight, UiBuild.Ui);
            _sortText.rectTransform.offsetMax = new Vector2(-10f, 0f);
        }

        private RectTransform _sackRoot;
        private RectTransform _menuRoot;
        private Text _menuText;

        private void BuildGrid()
        {
            _cells.Clear();
            for (int row = 0; row < GridRows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    _cells.Add(new ItemCell(_gridRoot, $"Cell {row}x{column}"));
                }
            }
        }

        /// <summary>
        /// Square cells, sized from the grid's measured rect rather than a fraction of it. The
        /// rank silhouettes are regular polygons — an oblong octagon reads as a mistake — and the
        /// frame ladder scales against this size so a rank keeps its weight at any panel width.
        /// </summary>
        private float LayOutGrid()
        {
            if (_gridRoot == null || _cells.Count == 0)
            {
                return 96f;
            }

            Rect rect = _gridRoot.rect;
            if (rect.width < 1f || rect.height < 1f)
            {
                return 96f;
            }

            const float gap = 8f;
            float byWidth = (rect.width - (Columns - 1) * gap) / Columns;
            float byHeight = (rect.height - (GridRows - 1) * gap) / GridRows;
            float size = Mathf.Floor(Mathf.Min(byWidth, byHeight));
            if (size < 8f)
            {
                return 96f;
            }

            // Centre the block, so a grid that cannot fill its area does not sit off to one side.
            float blockWidth = Columns * size + (Columns - 1) * gap;
            float left = (rect.width - blockWidth) * 0.5f;

            for (int i = 0; i < _cells.Count; i++)
            {
                int row = i / Columns;
                int column = i % Columns;
                _cells[i].PlaceSquare(
                    left + column * (size + gap), row * (size + gap), size);
            }

            _cellSize = size;
            _cellStride = size + gap;
            _gridLeft = left;
            return size;
        }

        private void Refresh()
        {
            if (_bag == null || _sackTitle == null)
            {
                return;
            }

            CollectVisible();
            Inventory inventory = _bag.Inventory;

            _sackTitle.text = _kind == InteractionKind.Chest ? "ITEM SACK" : "THE RACK";
            _coin.text = _bag.Wallet.Balance.ToString();

            int used = inventory.SlotsUsed;
            int capacity = Mathf.Max(1, inventory.Rules.Capacity);
            _sackMeterText.text = $"SACK {used} / {capacity}";
            UiBuild.SetFill(_sackMeter, used / (float)capacity);

            // Split shows one half at a time and the tabs say which. Solo shows both at once, so
            // there is nothing to tab between and the strip is not built at all.
            bool sack = !_split || _nav.Tab == ChestTab.ItemSack;
            if (_split)
            {
                _sackRoot.gameObject.SetActive(sack);
                _heroRoot.gameObject.SetActive(!sack);
                SetTab(_tabSack, sack);
                SetTab(_tabHero, !sack);
            }

            if (sack)
            {
                RefreshSack();
            }

            if (!_split || !sack)
            {
                RefreshHero();
            }

            _hint.text = _flash.Length > 0
                ? UiBuild.Tint(_flash, UiBuild.Gold)
                : "Stick: move    Light: actions    Heavy: back    "
                    + UiBuild.Tint("Esc / Start or ✕: leave the chest", UiBuild.Gold);
        }

        private void SetTab(Text tab, bool on)
        {
            if (tab == null)
            {
                return;
            }

            tab.color = on ? UiBuild.OnBrass : UiBuild.Muted;
            var back = tab.transform.parent.GetComponent<Image>();
            if (back != null)
            {
                back.color = on ? UiBuild.Brass : UiBuild.BoardDeep;
            }
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

        /// <summary>
        /// The row above the grid. Normally the six category filters; mid-combine it says what
        /// the grid has become instead, because the filters are neither what is being asked nor
        /// reachable while the pick is open.
        /// </summary>
        private string FilterLine(IReadOnlyList<ItemStack> items)
        {
            int pending = _nav.PendingCombine;
            if (pending >= 0 && pending < items.Count)
            {
                return UiBuild.Tint(
                    "COMBINING " + items[pending].Item.DisplayName
                        + " — pick a duplicate.   Heavy: cancel",
                    UiBuild.Coin);
            }

            _text.Clear();
            for (int i = 0; i < FilterNames.Length; i++)
            {
                string name = i == _nav.Filter ? $"[{FilterNames[i]}]" : $" {FilterNames[i]} ";
                _text.Append(_nav.Focus == ChestFocus.Filters && i == _nav.Filter
                    ? UiBuild.Tint(name, UiBuild.Focus)
                    : name);
            }

            return _text.ToString();
        }

        private void RefreshSack()
        {
            IReadOnlyList<ItemStack> items = _bag.Inventory.Items;
            RefreshFilters();

            float cellSize = LayOutGrid();
            for (int cell = 0; cell < _cells.Count; cell++)
            {
                if (cell >= _visible.Count)
                {
                    _cells[cell].SetEmpty();
                    continue;
                }

                ItemStack stack = items[_visible[cell]];
                bool selected = cell == _nav.Cursor && _nav.Focus == ChestFocus.Grid;
                bool pending = _visible[cell] == _nav.PendingCombine;

                _cells[cell].Set(
                    stack.Item,
                    stack.Count,
                    Host != null ? Host.IconFor(stack.Item.DefinitionId) : null,
                    selected,
                    // Worn gear lives in the loadout, never in the sack, so no sack cell can
                    // carry the design's "W" badge. The cell keeps the capability for the
                    // hero doll and the shop rack, which do compare against worn pieces.
                    worn: false,
                    pending,
                    cellSize);
            }

            int bagIndex = _visible.Count > 0 ? _visible[Mathf.Clamp(_nav.Cursor, 0, _visible.Count - 1)] : -1;
            BuildMenu(bagIndex);
            RefreshPopover(bagIndex);
            RefreshShopRow();

            if (_nav.Focus == ChestFocus.Stock && _nav.StockCursor < _stock.Count)
            {
                SetCompare(_stock[_nav.StockCursor]);
            }
            else if (bagIndex >= 0)
            {
                SetCompare(items[bagIndex].Item);
            }
            else
            {
                _compare.SetEmpty("Nothing here.");
            }
        }

        /// <summary>The compare panel needs the worn piece beside it, and what a point would cost.</summary>
        private void SetCompare(in ItemInstance item)
        {
            ItemInstance worn = item.IsConsumable
                ? default
                : _bag.Inventory.Loadout.Worn(item.Slot);
            int next = ItemUpgrade.CanUpgrade(item) ? _bag.Inventory.Prices.UpgradeCost(item) : 0;
            _compare.Set(item, worn, Host != null ? Host.IconFor(item.DefinitionId) : null, next);
        }

        /// <summary>
        /// The verb list, floated under the selected cell so the menu belongs to the thing it
        /// acts on rather than to a strip at the bottom of the screen (Michael, M6 pass).
        /// </summary>
        private void RefreshPopover(int bagIndex)
        {
            if (_nav.Focus != ChestFocus.Menu || bagIndex < 0 || _menu.Count == 0)
            {
                _popover.Hide();
                return;
            }

            ItemInstance item = _bag.Inventory.Items[bagIndex].Item;

            _popLabels.Clear();
            _popPrices.Clear();
            for (int i = 0; i < _menu.Count; i++)
            {
                _popLabels.Add(NameFor(_menu[i]));
                _popPrices.Add(PriceFor(_menu[i], item, bagIndex));
            }

            int cursor = Mathf.Clamp(_nav.Cursor, 0, Mathf.Max(0, _cells.Count - 1));
            Vector2 at = _cells.Count > 0 ? _cells[cursor].AnchoredPosition : Vector2.zero;
            float width = _gridRoot != null ? _gridRoot.rect.width : 900f;
            _popover.Show(item, _popLabels, _popPrices, _nav.Action, at, _cellSize, width);
        }

        private static string NameFor(ItemAction action)
        {
            switch (action)
            {
                case ItemAction.Equip: return "Equip";
                case ItemAction.QuickUse: return "Quick-use";
                case ItemAction.Upgrade: return "Upgrade";
                case ItemAction.Combine: return "Combine…";
                case ItemAction.Sell: return "Sell";
                default: return "Lock";
            }
        }

        /// <summary>The coin a row costs or pays. Zero for the verbs that move no money.</summary>
        private int PriceFor(ItemAction action, in ItemInstance item, int bagIndex)
        {
            switch (action)
            {
                case ItemAction.Upgrade:
                    return ItemUpgrade.CanUpgrade(item) ? _bag.Inventory.Prices.UpgradeCost(item) : 0;
                case ItemAction.Sell:
                    return _bag.Inventory.Prices.SellPrice(item);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// The six category chips. Mid-combine the row says what the grid has become instead,
        /// because the filters are neither what is being asked nor reachable while a pick is open.
        /// </summary>
        private void RefreshFilters()
        {
            bool combining = _nav.PendingCombine >= 0;
            for (int i = 0; i < _filterChips.Count; i++)
            {
                bool on = !combining && i == _nav.Filter;
                bool cursor = !combining && _nav.Focus == ChestFocus.Filters && i == _nav.Filter;
                _filterChips[i].color = on ? UiBuild.Brass : UiBuild.Well;
                _filterLabels[i].color = on ? UiBuild.OnBrass : UiBuild.Muted;
                if (cursor)
                {
                    _filterChips[i].color = UiBuild.Bone;
                    _filterLabels[i].color = UiBuild.OnBrass;
                }
            }

            _sortText.text = combining ? "combining" : "Quality";
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

            _shopStrip.text = _text.ToString();
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

        /// <summary>
        /// The hero half. <c>framed</c> says the world camera has pushed the real character into
        /// this half, which is true solo and online and false in local co-op — there the camera
        /// belongs to both players and cannot be taken.
        /// </summary>
        private void RefreshHero()
        {
            StatSheet sheet = _sheetSource != null ? _sheetSource.Sheet : default;
            _heroPanel.Set(
                _bag, sheet, _nav.Cursor,
                statsFocused: _nav.Tab == ChestTab.Hero && _nav.Focus != ChestFocus.Tabs,
                framed: !_split,
                host: Host);
        }
    }
}
