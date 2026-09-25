using System.Collections.Generic;
using System.Text;
using BattleBomb.Core.Items;
using BattleBomb.Core.Players;
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

            // At a shopkeeper the mode tabs take a band off the top, exactly as the design has
            // them: above the header rather than beside the filters, which are Sell mode's own
            // row and would have to move aside for them.
            float shopBand = IsShop ? ShopTabBand : 0f;

            RectTransform pad = UiBuild.Rect("Pad", _sackRoot);
            pad.anchorMin = Vector2.zero;
            pad.anchorMax = Vector2.one;
            pad.offsetMin = new Vector2(30f, 26f);
            pad.offsetMax = new Vector2(-27f, -(26f + shopBand));

            if (IsShop)
            {
                BuildModeTabs();
            }

            BuildSackHead(pad);
            BuildFilterRow(pad);

            // The grid and its popover share a parent so the popover can overhang the cells.
            RectTransform gridArea = UiBuild.Rect("GridArea", pad);
            gridArea.anchorMin = new Vector2(0f, 0f);
            gridArea.anchorMax = new Vector2(1f, 1f);
            gridArea.offsetMin = new Vector2(0f, IsShop ? ShopBodyBottom : 300f);
            gridArea.offsetMax = new Vector2(0f, -GridTop);

            _gridRoot = UiBuild.Rect("Grid", gridArea);
            UiBuild.Stretch(_gridRoot);
            BuildGrid();
            _popover = new ActionPopover(gridArea, 6);

            // The rack occupies the same band as the grid and the two are never both up: Buy
            // shows four priced rows, Sell shows the sack.
            if (IsShop)
            {
                _rackRoot = UiBuild.Rect("Rack", gridArea);
                UiBuild.Stretch(_rackRoot);

                // Buy draws no filter row, so the rack reaches up into the band the chips would
                // have taken. Left alone it opens with a strip of empty board under the rule.
                _rackRoot.offsetMax = new Vector2(0f, GridTop - FilterTop);
                for (int i = 0; i < ShopStockCount; i++)
                {
                    _rackRows.Add(new RackRow(_rackRoot, $"Roll {i}"));
                }

                _rackEmpty = UiBuild.Label("Bare", _rackRoot,
                    "The rack is bare. Come back after the next stage.", 14, UiBuild.Muted,
                    TextAnchor.UpperLeft, UiBuild.Ui);
                UiBuild.Pin(_rackEmpty.rectTransform, 4f, 6f, 520f, 24f);
            }

            RectTransform compare = UiBuild.Rect("CompareArea", pad);
            compare.anchorMin = new Vector2(0f, 0f);
            compare.anchorMax = new Vector2(1f, 0f);
            compare.pivot = new Vector2(0.5f, 0f);
            compare.sizeDelta = new Vector2(0f, 232f);
            compare.anchoredPosition = new Vector2(0f, 44f);
            _compare = new ComparePanel(compare);

            if (IsShop)
            {
                _junkArea = UiBuild.Rect("JunkArea", pad);
                _junkArea.anchorMin = new Vector2(0f, 0f);
                _junkArea.anchorMax = new Vector2(1f, 0f);
                _junkArea.pivot = new Vector2(0.5f, 0f);
                _junkArea.sizeDelta = new Vector2(0f, JunkBar.Height);
                _junkArea.anchoredPosition = new Vector2(0f, 288f);
                _junkBar = new JunkBar(_junkArea);
            }

            // ── Hero ─────────────────────────────────────────────────────────────────
            // Not at a shopkeeper (Michael, 2026-08-23). The counter is about their rack, not
            // about the player's doll, so the free half is left to the world and the camera puts
            // the shopkeeper in it.
            if (!IsShop)
            {
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

            // The hint row belongs to the screen, not to a half: split co-op hides the sack half
            // behind the hero tab, and the row has to stay up either way — so it is built last,
            // over the hero's board rather than under it.
            _promptRow = new PromptRow(root);
            _promptRow.Place(0f, _split ? 1f : 0.5f, 30f, 26f, 27f);
        }

        /// <summary>Title, the wallet, and how full the sack is.</summary>
        private void BuildSackHead(RectTransform pad)
        {
            if (HasTabs)
            {
                _tabSack = BuildTab(pad, "SACK", 0f);
                _tabHero = BuildTab(pad, "HERO", 132f);
            }

            _sackTitle = UiBuild.Label("Title", pad, "ITEM SACK", 38, UiBuild.Bone,
                TextAnchor.UpperLeft, UiBuild.Display);
            UiBuild.Pin(_sackTitle.rectTransform, 0f, HasTabs ? 42f : 0f, 420f, 44f);

            if (IsShop)
            {
                _subtitle = UiBuild.Label("Subtitle", pad, string.Empty, 10, UiBuild.Brass,
                    TextAnchor.LowerLeft, UiBuild.Mono);
                UiBuild.Pin(_subtitle.rectTransform, 226f, (HasTabs ? 42f : 0f) + 14f, 430f, 26f);
            }

            float headTop = HasTabs ? 46f : 4f;

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

            // Leaving must be reachable by every input the game has: a tap or click on this, B,
            // Escape, or Start. M6's pass found a screen with no visible way out, which is
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
            UiBuild.PointerOnly(button);

            Image rule = UiBuild.Rule("Rule", pad, 3f, UiBuild.BrassDim);
            rule.rectTransform.anchoredPosition = new Vector2(0f, HasTabs ? -96f : -54f);
        }

        /// <summary>
        /// BUY and SELL, drawn as the design's raised tabs. They are a readout, not a control:
        /// the switch is on its own button, because in split co-op the tab row underneath is
        /// already carrying sack-versus-hero and cannot also carry this.
        /// </summary>
        private void BuildModeTabs()
        {
            RectTransform band = UiBuild.Rect("ModeTabs", _sackRoot);
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.sizeDelta = new Vector2(-60f, ShopTabBand);
            band.anchoredPosition = new Vector2(0f, -20f);

            _tabBuy = BuildTab(band, "BUY", 0f);
            _tabSell = BuildTab(band, "SELL", 132f);
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
            // Held in one rect so Buy mode, which has no categories to filter, can put the whole
            // row away in a single call rather than walking the chips.
            _filterRow = UiBuild.Rect("FilterRow", pad);
            UiBuild.Stretch(_filterRow);

            float top = FilterTop;
            float x = 0f;
            for (int i = 0; i < FilterNames.Length; i++)
            {
                float width = 34f + FilterNames[i].Length * 8f;
                Image chip = UiBuild.Box($"Filter {i}", _filterRow, UiBuild.Well);
                UiBuild.Pin(chip.rectTransform, x, top, width, ChipHeight);
                _filterChips.Add(chip);
                _filterLabels.Add(UiBuild.Label("Text", chip.rectTransform, FilterNames[i], 12,
                    UiBuild.Muted, TextAnchor.MiddleCenter, UiBuild.Ui));
                x += width + 6f;
            }

            Image sortBox = UiBuild.Box("Sort", _filterRow, UiBuild.Well);
            RectTransform sort = UiBuild.Pin(sortBox.rectTransform, 0f, top, 150f, ChipHeight);
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

        /// <summary>The band the BUY / SELL tabs take off the top of the shop panel.</summary>
        private const float ShopTabBand = 40f;

        private const float ChipHeight = 30f;

        /// <summary>The room between the chips and the grid's first row. The grid used to start at
        /// a fixed 128 whatever the chips did, so in couch co-op — chips 42px lower under the tabs —
        /// their bottom edge ran 10px into the top row.</summary>
        private const float ChipsToGrid = 32f;

        /// <summary>Where the filter chips start: under the head, or under the tabs as well.</summary>
        private float FilterTop => HasTabs ? 108f : 66f;

        private float GridTop => FilterTop + ChipHeight + ChipsToGrid;

        /// <summary>Where the body stops at a shop, leaving room for the sweep and the compare
        /// panel beneath it.</summary>
        private const float ShopBodyBottom = 388f;

        private RectTransform _sackRoot;

        /// <summary>The sack-versus-hero tab row exists only where there is a hero half to
        /// reach: local co-op, and never at a shopkeeper.</summary>
        private bool HasTabs => _split && !IsShop;

        /// <summary>The cursor is in the worn loadout, including while its verb list or its
        /// upgrade list is open over it.</summary>
        private bool OnLoadout =>
            _nav.Focus == ChestFocus.Loadout
            || (_nav.OnWorn && (_nav.Focus == ChestFocus.Menu || _nav.Focus == ChestFocus.Upgrade));

        private RectTransform _filterRow;
        private RectTransform _rackRoot;
        private readonly List<RackRow> _rackRows = new List<RackRow>();
        private Text _rackEmpty;
        private RectTransform _junkArea;
        private JunkBar _junkBar;
        private Text _tabBuy;
        private Text _tabSell;
        private Text _subtitle;

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

            bool buying = Buying;
            _sackTitle.text = buying ? "THE RACK" : "ITEM SACK";
            _coin.text = _bag.Wallet.Balance.ToString();

            if (IsShop)
            {
                _subtitle.text = buying
                    ? $"THIS VISIT · {_stock.Count} {(_stock.Count == 1 ? "ROLL" : "ROLLS")}"
                    : "MONEY ENTERS ONLY BY SELLING · YOU RECEIVE HALF OF VALUE";
                SetTab(_tabBuy, buying);
                SetTab(_tabSell, !buying);
            }

            int used = inventory.SlotsUsed;
            int capacity = Mathf.Max(1, inventory.Rules.Capacity);
            _sackMeterText.text = $"SACK {used} / {capacity}";
            UiBuild.SetFill(_sackMeter, used / (float)capacity);

            // Split shows one half at a time and the tabs say which. Solo shows both at once, so
            // there is nothing to tab between and the strip is not built at all. A shopkeeper has
            // no hero half either way, so it never tabs.
            bool sack = !HasTabs || _nav.Tab == ChestTab.ItemSack;
            if (HasTabs)
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

            if (_heroPanel != null && (!HasTabs || !sack))
            {
                RefreshHero();
            }

            if (_flash.Length > 0)
            {
                _promptRow.ShowText(_flash);
            }
            else
            {
                _promptRow.Show(Prompts(), _family);
            }
        }

        /// <summary>
        /// The hint row (UI Pass 01), in the buttons of the device this player last pressed. What
        /// each button does depends on where the cursor is, so the row is rebuilt from the focus
        /// every refresh rather than naming one verb and being wrong on three of four blocks.
        /// </summary>
        private IReadOnlyList<Prompt> Prompts()
        {
            _prompts.Clear();
            _prompts.Add(new Prompt(PromptKey.Move, "Move"));

            // Mid-combine the screen is one question, so the row answers only that one.
            if (_nav.PendingCombine >= 0)
            {
                _prompts.Add(new Prompt(PromptKey.Confirm, "Combine with this"));
                _prompts.Add(new Prompt(PromptKey.Option, "Combine the whole pile", gold: true));
                _prompts.Add(new Prompt(PromptKey.Back, "Cancel"));
                return _prompts;
            }

            switch (_nav.Focus)
            {
                case ChestFocus.Grid:
                    // Only what would act: on an empty or filtered-empty sack A, X and Y do nothing.
                    if (_visible.Count > 0)
                    {
                        _prompts.Add(new Prompt(PromptKey.Confirm, "Actions"));
                        _prompts.Add(new Prompt(PromptKey.Option, "Sell"));
                        _prompts.Add(new Prompt(PromptKey.Lock, "Lock"));
                    }

                    break;

                case ChestFocus.Loadout:
                    if (!WornItem.IsEmpty)
                    {
                        _prompts.Add(new Prompt(PromptKey.Confirm, "Upgrade / take off"));
                        _prompts.Add(new Prompt(PromptKey.Lock, "Lock"));
                    }

                    break;

                case ChestFocus.Stats:
                    if (_bag.Ledger.UnspentPoints > 0)
                    {
                        _prompts.Add(new Prompt(PromptKey.Confirm, "Spend a point"));
                    }

                    break;

                case ChestFocus.Upgrade:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Spend"));
                    break;

                case ChestFocus.Stock:
                    if (_stock.Count > 0)
                    {
                        _prompts.Add(new Prompt(PromptKey.Confirm, "Buy"));
                    }

                    break;

                case ChestFocus.Junk:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Clear the junk"));
                    break;

                default:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Choose"));
                    break;
            }

            string tab = TabCaption();
            if (tab.Length > 0)
            {
                // Gold solo, where the other half is easy to miss: the hero panel went a whole
                // pass without anyone finding it (Michael, 2026-08-23).
                _prompts.Add(new Prompt(PromptKey.Tabs, tab, gold: HeroBeside));
            }

            string leave = IsShop ? "Leave" : "Leave the chest";
            _prompts.Add(new Prompt(PromptKey.Back, BackLeaves ? leave : "Back"));
            if (!BackLeaves)
            {
                _prompts.Add(new Prompt(PromptKey.Pause, leave));
            }

            return _prompts;
        }

        /// <summary>Whether B here closes the screen: the top level, where
        /// <see cref="ChestNavigation.Cancel"/> has nothing left to back out of.</summary>
        private bool BackLeaves =>
            _nav.PendingCombine < 0
            && (_nav.Focus == ChestFocus.Grid
                || _nav.Focus == ChestFocus.Filters
                || _nav.Focus == ChestFocus.Tabs
                || (_nav.Focus == ChestFocus.Stock && Buying));

        /// <summary>What LB / RB would switch to here, or nothing where there is one half.</summary>
        private string TabCaption()
        {
            if (_nav.Focus == ChestFocus.Menu || _nav.Focus == ChestFocus.Upgrade)
            {
                return string.Empty;
            }

            if (IsShop)
            {
                return Buying ? "Sell side" : "Buy side";
            }

            if (HasTabs)
            {
                return _nav.Tab == ChestTab.Hero ? "Sack" : "Hero";
            }

            if (HeroBeside)
            {
                return OnLoadout || _nav.Focus == ChestFocus.Stats ? "Your sack" : "Your gear";
            }

            return string.Empty;
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

        private void RefreshSack()
        {
            IReadOnlyList<ItemStack> items = _bag.Inventory.Items;

            // Buy replaces the sack outright — the rack rows and a five-row grid do not both fit
            // the half, which is what makes the counter two modes instead of one screen.
            bool buying = Buying;
            if (IsShop)
            {
                _rackRoot.gameObject.SetActive(buying);
                _filterRow.gameObject.SetActive(!buying);
                _gridRoot.gameObject.SetActive(!buying);
                RefreshJunk();
            }

            if (buying)
            {
                RefreshRack();
                _popover.Hide();
                _menu.Clear();

                if (_nav.StockCursor < _stock.Count)
                {
                    SetCompare(_stock[_nav.StockCursor]);
                }
                else
                {
                    _compare.SetEmpty("The rack is bare.");
                }

                return;
            }

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
            if (!_nav.OnWorn)
            {
                BuildMenu(bagIndex);
            }

            RefreshPopover(bagIndex);

            if (bagIndex >= 0)
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
            // A worn piece has its own popover over in the hero panel; this one belongs to the
            // grid and must get out of the way rather than float over the wrong half.
            if (OnLoadout)
            {
                _popover.Hide();
                RefreshWornPopover();
                return;
            }

            if (_nav.Focus == ChestFocus.Upgrade && bagIndex >= 0)
            {
                ShowUpgradeList(
                    _bag.Inventory.Items[bagIndex].Item, GridAnchor(), _cellSize, GridWidth(), _popover);
                return;
            }

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

            _popover.Show(item, _popLabels, _popPrices, _nav.Action, GridAnchor(), _cellSize, GridWidth());
        }

        private Vector2 GridAnchor()
        {
            int cursor = Mathf.Clamp(_nav.Cursor, 0, Mathf.Max(0, _cells.Count - 1));
            return _cells.Count > 0 ? _cells[cursor].AnchoredPosition : Vector2.zero;
        }

        private float GridWidth() => _gridRoot != null ? _gridRoot.rect.width : 900f;

        /// <summary>
        /// The stat targets a point can go into, drawn as the same floating list the verbs use.
        /// Until now this focus drew nothing at all — the popover hid the moment "Upgrade" was
        /// chosen, so the player was moving a cursor down an invisible list and spending coin on
        /// whatever it happened to be sitting on.
        /// </summary>
        private void ShowUpgradeList(
            in ItemInstance item, Vector2 at, float cellSize, float width, ActionPopover popover)
        {
            if (_upgradeTargets.Count == 0)
            {
                popover.Hide();
                return;
            }

            int price = _bag.Inventory.Prices.UpgradeCost(item);
            _popLabels.Clear();
            _popPrices.Clear();
            for (int i = 0; i < _upgradeTargets.Count; i++)
            {
                string name = NameOf(item, _upgradeTargets[i]);
                float value = ValueOf(item, _upgradeTargets[i]);
                _popLabels.Add($"{name}  {value:F2}");
                _popPrices.Add(price);
            }

            popover.Show(item, _popLabels, _popPrices, _nav.UpgradeCursor, at, cellSize, width);
        }

        /// <summary>The worn piece's verbs, and its upgrade list, floated over the hero half.</summary>
        private void RefreshWornPopover()
        {
            if (_heroPanel == null)
            {
                return;
            }

            ItemInstance worn = WornItem;
            if (worn.IsEmpty || !_nav.OnWorn || _nav.Focus == ChestFocus.Loadout)
            {
                _heroPanel.Popover.Hide();
                return;
            }

            Vector2 at = _heroPanel.SlotAnchor(_nav.LoadoutCursor);
            if (_nav.Focus == ChestFocus.Upgrade)
            {
                ShowUpgradeList(
                    worn, at, _heroPanel.SlotPixels, _heroPanel.DollWidth, _heroPanel.Popover);
                return;
            }

            _popLabels.Clear();
            _popPrices.Clear();
            for (int i = 0; i < _menu.Count; i++)
            {
                _popLabels.Add(NameFor(_menu[i]));
                _popPrices.Add(PriceFor(_menu[i], worn, -1));
            }

            _heroPanel.Popover.Show(
                worn, _popLabels, _popPrices, _nav.Action, at,
                _heroPanel.SlotPixels, _heroPanel.DollWidth);
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
                case ItemAction.Unequip: return "Take off";
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

        /// <summary>
        /// The rack: one row per roll, priced against the wallet and compared against what the
        /// player already wears. Rows are laid out from the area's measured width so the price
        /// column stays pinned to the right edge at either panel size.
        /// </summary>
        private void RefreshRack()
        {
            Rect area = _rackRoot.rect;
            float width = area.width < 1f ? 820f : area.width;
            float available = area.height < 1f ? RackRow.Height * ShopStockCount : area.height;

            _rackEmpty.enabled = _stock.Count == 0;

            // The design's 108px row is the ceiling, not the rule: the rack shrinks to whatever
            // the band can hold rather than spilling onto the sweep below it.
            int rows = Mathf.Max(1, _stock.Count);
            float rowHeight = Mathf.Min(
                RackRow.Height, (available - (rows - 1) * RackRow.Gap) / rows);

            int balance = _bag.Wallet.Balance;
            for (int i = 0; i < _rackRows.Count; i++)
            {
                if (i >= _stock.Count)
                {
                    _rackRows[i].SetActive(false);
                    continue;
                }

                ItemInstance item = _stock[i];
                ItemInstance worn = item.IsConsumable
                    ? default
                    : _bag.Inventory.Loadout.Worn(item.Slot);
                int price = _bag.Inventory.Prices.BuyPrice(item);

                _rackRows[i].Place(i * (rowHeight + RackRow.Gap), width, rowHeight);
                _rackRows[i].Set(
                    item, worn, Host != null ? Host.IconFor(item.DefinitionId) : null,
                    price, balance >= price,
                    _nav.Focus == ChestFocus.Stock && i == _nav.StockCursor);
            }
        }

        /// <summary>The sweep and what it would take, previewed before the press that runs it.</summary>
        private void RefreshJunk()
        {
            float width = _junkArea.rect.width;
            _junkBar.Place(0f, width < 1f ? 820f : width);
            _junkBar.Set(
                JunkThreshold, _bag.PreviewJunk(JunkThreshold), _nav.Focus == ChestFocus.Junk);
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
                _bag, sheet, _nav.StatCursor,
                statsFocused: _nav.Focus == ChestFocus.Stats,
                framed: !_split,
                host: Host,
                loadoutCursor: OnLoadout ? _nav.LoadoutCursor : -1);
        }
    }
}
