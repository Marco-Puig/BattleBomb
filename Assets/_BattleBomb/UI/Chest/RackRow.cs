using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// One roll on the shopkeeper's rack: the piece, what it would change about the player, and
    /// what it costs. The rack is a list of these rather than a grid of cells because a price and
    /// a handful of deltas need a line to sit on — which is the whole reason the shop panel and
    /// the sack panel are two modes rather than one screen.
    /// </summary>
    internal sealed class RackRow
    {
        /// <summary>The design's row: an 82px frame with 13px of air above and below it.</summary>
        internal const float FrameSize = 82f;
        internal const float Height = 108f;
        internal const float Gap = 11f;

        private const int MaxChips = 3;
        private const float PadX = 15f;
        private const float PadY = 13f;
        private const float PriceWidth = 168f;
        private const float RankHeight = 17f;
        private const float ChipHeight = 20f;

        private static readonly List<Delta> Scratch = new List<Delta>();

        private readonly RectTransform _root;
        private readonly Image _ring;
        private readonly Image _back;
        private readonly ItemCell _cell;
        private readonly Text _name;
        private readonly Text _rankLine;
        private readonly RectTransform _chipRow;
        private readonly List<Image> _chips = new List<Image>();
        private readonly List<Text> _chipLabels = new List<Text>();
        private readonly Image _coin;
        private readonly Text _price;
        private readonly Text _afford;
        private float _frameSize = FrameSize;

        internal RackRow(RectTransform parent, string name)
        {
            _root = UiBuild.Rect(name, parent);

            _ring = UiBuild.Box("Ring", _root, UiBuild.Bone);
            UiBuild.Stretch(_ring.rectTransform, -3f);
            _ring.enabled = false;

            _back = UiBuild.Box("Back", _root, UiBuild.Well);
            UiBuild.Stretch(_back.rectTransform);

            _cell = new ItemCell(_root, "Piece");

            _name = UiBuild.Label("Name", _root, string.Empty, 25, UiBuild.Bone,
                TextAnchor.UpperLeft, UiBuild.Display);
            _rankLine = UiBuild.Label("Rank", _root, string.Empty, 10, UiBuild.Brass,
                TextAnchor.UpperLeft, UiBuild.Mono);

            _chipRow = UiBuild.Rect("Chips", _root);
            for (int i = 0; i < MaxChips; i++)
            {
                Image chip = UiBuild.Box($"Chip {i}", _chipRow, UiBuild.Well);
                _chips.Add(chip);
                _chipLabels.Add(UiBuild.Label("Text", chip.rectTransform, string.Empty, 11,
                    UiBuild.Up, TextAnchor.MiddleCenter, UiBuild.Ui));
            }

            _coin = UiBuild.Box("Coin", _root, UiBuild.Gold);
            _coin.sprite = UiBuild.Disc;
            _price = UiBuild.Label("Price", _root, string.Empty, 21, UiBuild.Gold,
                TextAnchor.MiddleRight, UiBuild.Ui);
            _afford = UiBuild.Label("Afford", _root, string.Empty, 9, UiBuild.Faint,
                TextAnchor.UpperRight, UiBuild.Mono);
        }

        internal RectTransform Root => _root;

        /// <summary>
        /// Places the row by pixels down from the top of the rack area, at whatever height the
        /// area can actually give it. Four rows at the design's full 108 do not fit every panel
        /// the game runs at, and a rack that overflows lands on the sweep underneath it.
        /// </summary>
        internal void Place(float y, float width, float height)
        {
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = new Vector2(width, height);
            _root.anchoredPosition = new Vector2(0f, -y);

            float pad = Mathf.Min(PadY, height * 0.12f);
            float frame = Mathf.Max(32f, height - pad * 2f);
            _frameSize = frame;
            _cell.PlaceSquare(PadX, pad, frame);

            float textLeft = PadX + frame + 16f;
            float textWidth = Mathf.Max(60f, width - textLeft - PriceWidth - 12f);

            // Label truncates vertically rather than clipping, so the box has to outgrow the
            // glyph, not match it. The size follows the row instead of staying at the design's
            // 25px, which vanished outright the moment four rows had to share a shorter band.
            int nameSize = Mathf.Clamp(Mathf.FloorToInt(height * 0.24f), 13, 25);
            _name.fontSize = nameSize;
            float nameHeight = nameSize + 9f;
            float rankTop = pad + nameHeight;
            UiBuild.Pin(_name.rectTransform, textLeft, pad, textWidth, nameHeight);

            // A 10px line needs more than 10px of box: Label truncates vertically rather than
            // clipping, so a box measured to the glyph loses the line entirely.
            UiBuild.Pin(_rankLine.rectTransform, textLeft, rankTop, textWidth, RankHeight);

            _chipRow.anchorMin = new Vector2(0f, 1f);
            _chipRow.anchorMax = new Vector2(0f, 1f);
            _chipRow.pivot = new Vector2(0f, 1f);
            _chipRow.sizeDelta = new Vector2(textWidth, ChipHeight);
            _chipRow.anchoredPosition = new Vector2(textLeft, -(rankTop + RankHeight + 2f));

            PinRight(_coin.rectTransform, PadX + 74f, pad + 6f, 12f, 12f);
            PinRight(_price.rectTransform, PadX, pad, PriceWidth - 24f, 26f);
            PinRight(_afford.rectTransform, PadX, pad + 30f, PriceWidth - 8f, 15f);
        }

        internal void SetActive(bool on) => _root.gameObject.SetActive(on);

        internal void Set(
            in ItemInstance item, in ItemInstance worn, Sprite icon,
            int price, bool canAfford, bool focused)
        {
            SetActive(true);
            Color rank = QualityColors.For(item.Quality);

            // The bone ring sits behind the row and shows only at its edge, so the fill has to be
            // opaque. A translucent fill lets the whole ring through and the row reads as a
            // cream slab rather than a selected one.
            _ring.enabled = focused;
            _back.color = focused
                ? Color.Lerp(UiBuild.BoardDeep, UiBuild.Brass, 0.22f)
                : new Color(UiBuild.Well.r, UiBuild.Well.g, UiBuild.Well.b, 0.72f);

            _cell.Set(item, 1, icon, focused: false, worn: false, pendingCombine: false, _frameSize);

            _name.text = item.DisplayName.ToUpperInvariant();
            _name.color = QualityColors.TextFor(item.Quality);
            _rankLine.text = $"RANK {(int)item.Quality} · LV {item.RequiredLevel}";

            // The selected row's fill is lighter than the rest, so the rank line has to come up
            // with it or it reads as a smudge on the one row the eye is actually on.
            _rankLine.color = focused
                ? QualityColors.TextFor(item.Quality)
                : new Color(rank.r, rank.g, rank.b, 0.8f);

            LayOutChips(item, worn);

            _price.text = price.ToString();
            _price.color = canAfford ? UiBuild.Gold : UiBuild.Down;
            _coin.color = canAfford
                ? UiBuild.Gold
                : new Color(UiBuild.Down.r, UiBuild.Down.g, UiBuild.Down.b, 0.7f);
            _afford.text = canAfford ? "AFFORDABLE" : "NOT ENOUGH COIN";
            _afford.color = canAfford
                ? new Color(UiBuild.Up.r, UiBuild.Up.g, UiBuild.Up.b, 0.8f)
                : new Color(UiBuild.Down.r, UiBuild.Down.g, UiBuild.Down.b, 0.8f);
        }

        private static void PinRight(RectTransform rect, float x, float y, float width, float height)
        {
            UiBuild.Pin(rect, 0f, y, width, height);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-x, -y);
        }

        /// <summary>
        /// The three deltas most worth reading, chosen by how far each moves relative to what
        /// counts as a lot of that stat. Sorting by the raw number would put max health on every
        /// row and swing speed on none, since one is counted in tens and the other in hundredths.
        /// </summary>
        private void LayOutChips(in ItemInstance item, in ItemInstance worn)
        {
            Collect(item, worn, Scratch);

            float x = 0f;
            for (int i = 0; i < MaxChips; i++)
            {
                if (i >= Scratch.Count)
                {
                    _chips[i].enabled = false;
                    _chipLabels[i].text = string.Empty;
                    continue;
                }

                Delta delta = Scratch[i];
                string sign = delta.Amount > 0f ? "+" : "-";
                string arrow = delta.Better ? "▲" : "▼";
                string text = $"{delta.Label} {arrow} {sign}{delta.Read()}";

                Color tint = delta.Better ? UiBuild.Up : UiBuild.Down;
                _chipLabels[i].text = text;
                _chipLabels[i].color = tint;
                _chips[i].enabled = true;
                _chips[i].color = new Color(tint.r, tint.g, tint.b, 0.16f);

                float width = 16f + text.Length * 6.4f;
                RectTransform chip = _chips[i].rectTransform;
                chip.anchorMin = new Vector2(0f, 0.5f);
                chip.anchorMax = new Vector2(0f, 0.5f);
                chip.pivot = new Vector2(0f, 0.5f);
                chip.sizeDelta = new Vector2(width, 18f);
                chip.anchoredPosition = new Vector2(x, 0f);
                x += width + 6f;
            }
        }

        private static void Collect(in ItemInstance item, in ItemInstance worn, List<Delta> into)
        {
            into.Clear();
            GearContribution mine = item.TotalContribution();
            GearContribution theirs = worn.IsEmpty ? default : worn.TotalContribution();

            Add(into, "DMG", mine.WeaponDamage - theirs.WeaponDamage, "F0", false, 5f, false);
            Add(into, "MAG", mine.MagicDamage - theirs.MagicDamage, "F0", false, 5f, false);
            Add(into, "DEF", mine.Defence - theirs.Defence, "F0", true, 0.05f, false);
            Add(into, "SWING", mine.SwingSpeedBonus - theirs.SwingSpeedBonus, "F2", false, 0.05f, false);
            Add(into, "WEIGHT", mine.Weight - theirs.Weight, "F1", false, 1f, true);
            Add(into, "HP", mine.MaxHealthBonus - theirs.MaxHealthBonus, "F0", false, 20f, false);
            Add(into, "MANA", mine.MaxManaBonus - theirs.MaxManaBonus, "F0", false, 20f, false);
            Add(into, "CRIT", mine.CritChance - theirs.CritChance, "F0", true, 0.05f, false);
            Add(into, "STEAL", mine.LifeSteal - theirs.LifeSteal, "F0", true, 0.05f, false);

            into.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        }

        private static void Add(
            List<Delta> into, string label, float amount, string format, bool percent,
            float scale, bool lowerIsBetter)
        {
            // Below what the row would print as a whole unit there is nothing to say, so the
            // chip is dropped rather than shown reading "+0".
            float floor = percent ? 0.005f : 0.5f;
            if (format == "F2")
            {
                floor = 0.005f;
            }

            if (Mathf.Abs(amount) < floor)
            {
                return;
            }

            bool better = lowerIsBetter ? amount < 0f : amount > 0f;
            into.Add(new Delta(label, amount, format, percent, better, Mathf.Abs(amount) / scale));
        }

        private readonly struct Delta
        {
            internal readonly string Label;
            internal readonly float Amount;
            internal readonly string Format;
            internal readonly bool Percent;
            internal readonly bool Better;
            internal readonly float Weight;

            internal Delta(
                string label, float amount, string format, bool percent, bool better, float weight)
            {
                Label = label;
                Amount = amount;
                Format = format;
                Percent = percent;
                Better = better;
                Weight = weight;
            }

            /// <summary>Defence and the two chance stats are carried as fractions and read as
            /// percentages — formatted raw they all round to a nonsense "+0".</summary>
            internal string Read()
            {
                float shown = Percent ? Mathf.Abs(Amount) * 100f : Mathf.Abs(Amount);
                return shown.ToString(Format) + (Percent ? "%" : string.Empty);
            }
        }
    }
}
