using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// The right half of the chest screen (UI Pass 01): what the hero is wearing, pinned around
    /// the hero themselves, with the allocation and the gear totals underneath.
    ///
    /// The middle is deliberately empty when <c>framed</c> — solo and online, the world camera
    /// pushes the player into this half and the live character shows through the canvas, so
    /// drawing anything here would cover them. In local co-op there is no camera move (the other
    /// player is still using it), so the middle falls back to the design's marked placeholder.
    /// </summary>
    internal sealed class HeroPanel
    {
        private const float SlotSize = 92f;
        private const float SlotGap = 20f;

        internal readonly struct Slot
        {
            internal readonly ItemSlot Which;
            internal readonly int EquipmentIndex;

            internal Slot(ItemSlot which, int equipmentIndex = 0)
            {
                Which = which;
                EquipmentIndex = equipmentIndex;
            }
        }

        private static readonly Slot[] Left =
        {
            new Slot(ItemSlot.Helmet), new Slot(ItemSlot.Chest), new Slot(ItemSlot.Boots),
        };

        private static readonly Slot[] Right =
        {
            new Slot(ItemSlot.Weapon), new Slot(ItemSlot.Pet),
        };

        private static readonly Slot[] Worn =
        {
            new Slot(ItemSlot.Equipment, 0), new Slot(ItemSlot.Equipment, 1),
        };

        /// <summary>
        /// Every worn slot in the order the cells are filled, which is the order
        /// <see cref="ChestNavigation.LoadoutCursor"/> indexes them by. Built from the three
        /// column arrays rather than written out again, so the cursor and the cells cannot drift
        /// apart — a mismatch here would deepen a piece the player was not pointing at.
        /// </summary>
        internal static readonly Slot[] Order = BuildOrder();

        private static Slot[] BuildOrder()
        {
            var all = new Slot[Left.Length + Right.Length + Worn.Length];
            Left.CopyTo(all, 0);
            Right.CopyTo(all, Left.Length);
            Worn.CopyTo(all, Left.Length + Right.Length);
            return all;
        }

        private static readonly string[] LeftLabels = { "HELMET", "CHEST", "BOOTS" };
        private static readonly string[] RightLabels = { "WEAPON", "PET" };
        private static readonly string[] WornLabels = { "EQUIP 1", "EQUIP 2" };
        private static readonly string[] StatNames = { "Strength", "HP", "Mana", "Speed" };

        private int _loadoutCursor = -1;
        private RectTransform _doll;
        private ActionPopover _popover;

        private readonly RectTransform _root;
        private readonly Text _title;
        private readonly Text _levelValue;
        private readonly Image _xpFill;
        private readonly Text _xpText;
        private readonly Text _prestige;
        private readonly List<ItemCell> _cells = new List<ItemCell>();
        private readonly List<Text> _cellLabels = new List<Text>();
        private readonly ItemCell _quick;
        private readonly Text _quickLabel;
        private readonly Image _placeholder;
        private readonly Text _placeholderText;
        private readonly Image _shadow;
        private readonly Text _pointsBadge;
        private readonly Image _pointsBack;
        private readonly List<Text> _statName = new List<Text>();
        private readonly List<Text> _statValue = new List<Text>();
        private readonly List<Image> _statRow = new List<Image>();
        private readonly List<Text> _totalName = new List<Text>();
        private readonly List<Text> _totalValue = new List<Text>();

        internal HeroPanel(RectTransform parent)
        {
            _root = UiBuild.Rect("Hero", parent);
            UiBuild.Stretch(_root);

            RectTransform pad = UiBuild.Rect("Pad", _root);
            UiBuild.Stretch(pad, 30f);

            // ── Head ──
            _title = UiBuild.Legible(UiBuild.Label("Title", pad, "HERO", 38, UiBuild.Bone,
                TextAnchor.UpperLeft, UiBuild.Display));
            UiBuild.Pin(_title.rectTransform, 0f, 0f, 300f, 42f);

            _prestige = UiBuild.Legible(UiBuild.Label("Prestige", pad, string.Empty, 12, UiBuild.Muted,
                TextAnchor.UpperLeft, UiBuild.Ui));
            UiBuild.Pin(_prestige.rectTransform, 2f, 44f, 300f, 18f);

            Text levelLabel = UiBuild.Legible(UiBuild.Label("LevelLabel", pad, "LEVEL", 10, UiBuild.Muted,
                TextAnchor.UpperRight, UiBuild.Mono));
            UiBuild.Pin(levelLabel.rectTransform, 0f, 14f, 320f, 16f);
            levelLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
            levelLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            levelLabel.rectTransform.pivot = new Vector2(1f, 1f);
            levelLabel.rectTransform.anchoredPosition = new Vector2(-46f, -14f);

            _levelValue = UiBuild.Legible(UiBuild.Label("Level", pad, string.Empty, 32, UiBuild.Bone,
                TextAnchor.UpperRight, UiBuild.Display));
            UiBuild.Pin(_levelValue.rectTransform, 0f, 0f, 44f, 40f);
            _levelValue.rectTransform.anchorMin = new Vector2(1f, 1f);
            _levelValue.rectTransform.anchorMax = new Vector2(1f, 1f);
            _levelValue.rectTransform.pivot = new Vector2(1f, 1f);
            _levelValue.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            _xpFill = UiBuild.Meter("Xp", pad, UiBuild.Gold);
            RectTransform xpWell = (RectTransform)_xpFill.transform.parent;
            xpWell.anchorMin = new Vector2(1f, 1f);
            xpWell.anchorMax = new Vector2(1f, 1f);
            xpWell.pivot = new Vector2(1f, 1f);
            xpWell.sizeDelta = new Vector2(300f, 9f);
            xpWell.anchoredPosition = new Vector2(0f, -46f);

            _xpText = UiBuild.Legible(UiBuild.Label("XpText", pad, string.Empty, 10, UiBuild.Muted,
                TextAnchor.UpperRight, UiBuild.Mono));
            UiBuild.Pin(_xpText.rectTransform, 0f, 58f, 300f, 16f);
            _xpText.rectTransform.anchorMin = new Vector2(1f, 1f);
            _xpText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _xpText.rectTransform.pivot = new Vector2(1f, 1f);
            _xpText.rectTransform.anchoredPosition = new Vector2(0f, -58f);

            UiBuild.Pin(UiBuild.Rule("Rule", pad, 3f, UiBuild.BrassDim).rectTransform,
                0f, 76f, 0f, 3f).anchorMax = new Vector2(1f, 1f);

            // ── The doll ──
            RectTransform doll = UiBuild.Rect("Doll", pad);
            _doll = doll;
            doll.anchorMin = new Vector2(0f, 0f);
            doll.anchorMax = new Vector2(1f, 1f);
            doll.offsetMin = new Vector2(0f, 250f);
            doll.offsetMax = new Vector2(0f, -92f);

            BuildColumn(doll, Left, LeftLabels, 0f, 0f);
            BuildColumn(doll, Right, RightLabels, 1f, -SlotSize);
            BuildWornRow(doll);

            _shadow = UiBuild.Box("Shadow", doll, new Color(0f, 0f, 0f, 0.55f));
            Centre(_shadow.rectTransform, 0.5f, 0.16f, 250f, 30f);

            _placeholder = UiBuild.Box("Placeholder", doll, new Color(0.965f, 0.937f, 0.886f, 0.07f));
            _placeholder.sprite = UiBuild.Hatch;
            _placeholder.type = Image.Type.Tiled;
            Centre(_placeholder.rectTransform, 0.5f, 0.56f, 290f, 400f);
            _placeholderText = UiBuild.Label("PlaceholderText", _placeholder.rectTransform,
                "HERO SPRITE\n2D BILLBOARD", 11, UiBuild.Muted, TextAnchor.MiddleCenter, UiBuild.Mono);

            // The verb list for a worn piece floats over this half, not the sack's, so it needs
            // its own popover anchored in the doll's space.
            _popover = new ActionPopover(doll, 6);

            _quick = new ItemCell(doll, "Quick");
            _quickLabel = UiBuild.Legible(UiBuild.Label("QuickLabel", doll, "QUICK-USE", 9, UiBuild.Bone,
                TextAnchor.UpperCenter, UiBuild.Mono));

            // ── Stats ──
            // Opaque: this is a panel the numbers sit on, not a scrim over the character. At less
            // than full alpha the hero showed through the stats, which read as a bug.
            Image statsBack = UiBuild.Box("Stats", pad, new Color(0.07f, 0.05f, 0.09f, 1f));
            RectTransform stats = statsBack.rectTransform;
            stats.anchorMin = new Vector2(0f, 0f);
            stats.anchorMax = new Vector2(1f, 0f);
            stats.pivot = new Vector2(0.5f, 0f);
            stats.sizeDelta = new Vector2(0f, 190f);
            stats.anchoredPosition = new Vector2(0f, 46f);

            Text baseLabel = UiBuild.Label("BaseLabel", stats, "BASE STATS", 10, UiBuild.Brass,
                TextAnchor.UpperLeft, UiBuild.Mono);
            UiBuild.Pin(baseLabel.rectTransform, 18f, 16f, 140f, 16f);

            _pointsBack = UiBuild.Box("PointsBack", stats, UiBuild.Brass);
            UiBuild.Pin(_pointsBack.rectTransform, 118f, 12f, 78f, 20f);
            _pointsBadge = UiBuild.Label("Points", _pointsBack.rectTransform, string.Empty, 10,
                UiBuild.OnBrass, TextAnchor.MiddleCenter, UiBuild.Ui);

            for (int i = 0; i < StatNames.Length; i++)
            {
                Image row = UiBuild.Box($"Stat {i}", stats, Color.clear);
                UiBuild.Pin(row.rectTransform, 14f, 42f + i * 32f, 210f, 30f);
                _statRow.Add(row);

                Text name = UiBuild.Label("N", row.rectTransform, StatNames[i], 13, UiBuild.Bone,
                    TextAnchor.MiddleLeft, UiBuild.Ui);
                UiBuild.StretchX(name.rectTransform, 8f);
                _statName.Add(name);

                Text value = UiBuild.Label("V", row.rectTransform, string.Empty, 15, UiBuild.Bone,
                    TextAnchor.MiddleRight, UiBuild.Ui);
                UiBuild.Stretch(value.rectTransform);
                value.rectTransform.offsetMax = new Vector2(-38f, 0f);
                _statValue.Add(value);

                Image plus = UiBuild.Box("Plus", row.rectTransform, UiBuild.Brass);
                UiBuild.Pin(plus.rectTransform, 182f, 4f, 22f, 22f);
                UiBuild.Label("PlusText", plus.rectTransform, "+", 16, UiBuild.OnBrass,
                    TextAnchor.MiddleCenter, UiBuild.Display);
            }

            Text gearLabel = UiBuild.Label("GearLabel", stats, "WITH GEAR", 10, UiBuild.Brass,
                TextAnchor.UpperLeft, UiBuild.Mono);
            UiBuild.Pin(gearLabel.rectTransform, 260f, 16f, 140f, 16f);

            for (int i = 0; i < 6; i++)
            {
                RectTransform cellRect = UiBuild.Rect($"Total {i}", stats);
                UiBuild.Pin(cellRect, 260f + (i % 2) * 200f, 42f + (i / 2) * 32f, 190f, 26f);

                Text name = UiBuild.Label("N", cellRect, string.Empty, 12, UiBuild.Muted,
                    TextAnchor.MiddleLeft, UiBuild.Ui);
                UiBuild.Stretch(name.rectTransform);
                _totalName.Add(name);

                Text value = UiBuild.Label("V", cellRect, string.Empty, 13, UiBuild.Bone,
                    TextAnchor.MiddleRight, UiBuild.Ui);
                UiBuild.Stretch(value.rectTransform);
                _totalValue.Add(value);
            }

            // Last, so a worn piece's popover draws over the stats panel it can overhang from the
            // bottom row, as the sack's does over its compare panel.
            doll.SetAsLastSibling();
        }

        /// <summary>The verb list that floats over a worn slot.</summary>
        internal ActionPopover Popover => _popover;

        internal float SlotPixels => SlotSize;

        internal float DollWidth => _doll != null && _doll.rect.width > 1f ? _doll.rect.width : 420f;

        /// <summary>
        /// Where a worn cell sits in the doll's own space. The right-hand column is anchored to
        /// the right edge, so its <see cref="RectTransform.anchoredPosition"/> alone is a negative
        /// offset rather than a position — the anchor has to be folded back in or the popover
        /// lands off the panel.
        /// </summary>
        internal Vector2 SlotAnchor(int index)
        {
            if (index < 0 || index >= _cells.Count)
            {
                return Vector2.zero;
            }

            RectTransform rect = _cells[index].Root;
            return new Vector2(
                rect.anchorMin.x * DollWidth + rect.anchoredPosition.x, rect.anchoredPosition.y);
        }

        private void BuildColumn(
            RectTransform parent, Slot[] slots, string[] labels, float anchorX, float offsetX)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                float top = i * (SlotSize + SlotGap + 16f);
                var cell = new ItemCell(parent, labels[i]);
                _cells.Add(cell);
                PlaceCell(cell, anchorX, offsetX, top);
                _cellLabels.Add(SlotLabel(parent, labels[i], anchorX, offsetX, top));
            }
        }

        private void BuildWornRow(RectTransform parent)
        {
            float top = 2f * (SlotSize + SlotGap + 16f);
            for (int i = 0; i < Worn.Length; i++)
            {
                float offsetX = -SlotSize - (1 - i) * (SlotSize + 12f);
                var cell = new ItemCell(parent, WornLabels[i]);
                _cells.Add(cell);
                PlaceCell(cell, 1f, offsetX, top);
                _cellLabels.Add(SlotLabel(parent, WornLabels[i], 1f, offsetX, top));
            }
        }

        private static void PlaceCell(ItemCell cell, float anchorX, float offsetX, float top)
        {
            cell.PlaceSquare(0f, 0f, SlotSize);
            RectTransform rect = cell.Root;
            rect.anchorMin = new Vector2(anchorX, 1f);
            rect.anchorMax = new Vector2(anchorX, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(offsetX, -top);
        }

        private static Text SlotLabel(
            RectTransform parent, string caption, float anchorX, float offsetX, float top)
        {
            Text label = UiBuild.Legible(UiBuild.Label($"{caption} label", parent, caption, 9,
                UiBuild.Muted, TextAnchor.UpperCenter, UiBuild.Mono));
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(anchorX, 1f);
            rect.anchorMax = new Vector2(anchorX, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(SlotSize, 14f);
            rect.anchoredPosition = new Vector2(offsetX, -(top + SlotSize + 3f));
            return label;
        }

        private static void Centre(
            RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Redraws from live state. <paramref name="framed"/> is true when the world camera has
        /// pushed the hero into this half, in which case the middle stays clear for them.
        /// </summary>
        internal void Set(
            PlayerInventory bag, in StatSheet sheet, int cursor, bool statsFocused,
            bool framed, ChestScreenHost host, int loadoutCursor = -1)
        {
            _loadoutCursor = loadoutCursor;

            if (bag == null)
            {
                return;
            }

            _levelValue.text = bag.Level.ToString();
            int prestige = bag.Ledger.PrestigeCount;
            _prestige.text = prestige > 0 ? new string('★', Mathf.Min(prestige, 5)) : string.Empty;

            float into = bag.Ledger.XpIntoLevel;
            float toNext = bag.Curve.XpToNext(bag.Ledger.Level, prestige);
            UiBuild.SetFill(_xpFill, toNext > 0f ? into / toNext : 0f);
            _xpText.text = $"{into:F0} / {toNext:F0} XP";

            _placeholder.enabled = !framed;
            _placeholderText.enabled = !framed;
            _shadow.enabled = !framed;

            int index = 0;
            FillColumn(bag, Left, host, ref index);
            FillColumn(bag, Right, host, ref index);
            FillColumn(bag, Worn, host, ref index);

            SetQuick(bag, host);

            int points = bag.Ledger.UnspentPoints;
            _pointsBack.enabled = points > 0;
            _pointsBadge.text = points > 0 ? $"{points} POINTS" : string.Empty;

            BaseStats allocations = bag.Ledger.Allocations;
            int[] values = { allocations.Strength, allocations.Hp, allocations.Mana, allocations.Speed };
            for (int i = 0; i < _statValue.Count; i++)
            {
                _statValue[i].text = values[i].ToString();
                bool on = statsFocused && i == cursor;
                _statRow[i].color = on
                    ? new Color(UiBuild.Brass.r, UiBuild.Brass.g, UiBuild.Brass.b, 0.18f)
                    : Color.clear;
            }

            SetTotal(0, "Damage", sheet.WeaponDamage.ToString("F0"));
            SetTotal(1, "Defence", (sheet.Defence * 100f).ToString("F0") + "%");
            SetTotal(2, "Crit", (sheet.CritChance * 100f).ToString("F0") + "%");
            SetTotal(3, "Swing", "×" + sheet.SwingSpeedMultiplier.ToString("F2"));
            SetTotal(4, "Speed", "×" + sheet.NetMoveSpeedMultiplier.ToString("F2"));
            SetTotal(5, "Mana", sheet.MaxMana.ToString("F0"));
        }

        private void FillColumn(PlayerInventory bag, Slot[] slots, ChestScreenHost host, ref int index)
        {
            for (int i = 0; i < slots.Length && index < _cells.Count; i++, index++)
            {
                ItemInstance item = bag.Inventory.Loadout.Worn(slots[i].Which, slots[i].EquipmentIndex);
                if (item.IsEmpty)
                {
                    _cells[index].SetEmpty();
                    _cells[index].SetCursor(index == _loadoutCursor);
                    continue;
                }

                _cells[index].Set(
                    item, 1, host != null ? host.IconFor(item.DefinitionId) : null,
                    focused: index == _loadoutCursor, worn: false, pendingCombine: false,
                    cellSize: SlotSize);
            }
        }

        private void SetQuick(PlayerInventory bag, ChestScreenHost host)
        {
            _quick.PlaceSquare(0f, 0f, SlotSize);
            RectTransform rect = _quick.Root;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 22f);

            _quickLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _quickLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _quickLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _quickLabel.rectTransform.sizeDelta = new Vector2(120f, 14f);
            _quickLabel.rectTransform.anchoredPosition = new Vector2(0f, 20f);

            Inventory inventory = bag.Inventory;
            if (inventory.QuickKind == QuickSlotKind.Consumable)
            {
                IReadOnlyList<ItemStack> items = inventory.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Item.DefinitionId != inventory.QuickConsumableId)
                    {
                        continue;
                    }

                    _quick.Set(
                        items[i].Item, items[i].Count,
                        host != null ? host.IconFor(items[i].Item.DefinitionId) : null,
                        focused: false, worn: false, pendingCombine: false, cellSize: SlotSize);
                    return;
                }
            }
            else if (inventory.QuickKind == QuickSlotKind.EquipmentActive)
            {
                ItemInstance active = inventory.Loadout.Equipment(inventory.QuickEquipmentIndex);
                if (!active.IsEmpty)
                {
                    _quick.Set(
                        active, 1, host != null ? host.IconFor(active.DefinitionId) : null,
                        focused: false, worn: false, pendingCombine: false, cellSize: SlotSize);
                    return;
                }
            }

            _quick.SetEmpty();
        }

        private void SetTotal(int index, string name, string value)
        {
            _totalName[index].text = name;
            _totalValue[index].text = value;
        }
    }
}
