using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Gameplay.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// The verb list for one item (UI Pass 01). It lands on the item wherever the item is, rather
    /// than in a fixed corner — a menu that appears somewhere else makes the player look away from
    /// the thing they are acting on, and then look back to check it was the right one.
    ///
    /// Prices sit on their own rows because the money question belongs next to the verb that
    /// spends it, not in a status line somewhere else on the screen.
    /// </summary>
    internal sealed class ActionPopover
    {
        private const float Width = 232f;
        private const float HeaderHeight = 30f;
        private const float RowHeight = 32f;

        private readonly RectTransform _root;
        private readonly Image _arrow;
        private readonly Text _title;
        private readonly List<Image> _rowBacks = new List<Image>();
        private readonly List<Text> _rowLabels = new List<Text>();
        private readonly List<Text> _rowPrices = new List<Text>();

        internal ActionPopover(RectTransform parent, int maxRows)
        {
            Image border = UiBuild.Box("Popover", parent, UiBuild.Brass);
            _root = border.rectTransform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);

            Image body = UiBuild.Box("Body", _root, UiBuild.Well);
            UiBuild.Stretch(body.rectTransform, 3f);

            _arrow = UiBuild.Box("Arrow", _root, UiBuild.Brass);
            UiBuild.Pin(_arrow.rectTransform, 22f, -7f, 14f, 8f);

            Image head = UiBuild.Box("Head", body.rectTransform, UiBuild.BoardDeep);
            UiBuild.Pin(head.rectTransform, 0f, 0f, Width - 6f, HeaderHeight);
            _title = UiBuild.Label("Title", head.rectTransform, string.Empty, 15, UiBuild.Bone,
                TextAnchor.MiddleLeft, UiBuild.Display);
            UiBuild.StretchX(_title.rectTransform, 8f);

            for (int i = 0; i < maxRows; i++)
            {
                Image back = UiBuild.Box($"Row {i}", body.rectTransform, Color.clear);
                UiBuild.Pin(back.rectTransform, 0f, HeaderHeight + i * RowHeight, Width - 6f, RowHeight);
                _rowBacks.Add(back);

                _rowLabels.Add(UiBuild.Label("Label", back.rectTransform, string.Empty, 13,
                    UiBuild.Bone, TextAnchor.MiddleLeft, UiBuild.Ui));
                UiBuild.StretchX(_rowLabels[i].rectTransform, 10f);

                _rowPrices.Add(UiBuild.Label("Price", back.rectTransform, string.Empty, 12,
                    UiBuild.Gold, TextAnchor.MiddleRight, UiBuild.Ui));
                UiBuild.StretchX(_rowPrices[i].rectTransform, 10f);
            }

            Hide();
        }

        internal void Hide() => _root.gameObject.SetActive(false);

        /// <summary>
        /// Fills the rows and parks the popover under <paramref name="cell"/>. The x is clamped so
        /// a menu opened on the last column stays on the panel instead of running off its edge.
        /// </summary>
        internal void Show(
            in ItemInstance item, IReadOnlyList<string> labels, IReadOnlyList<int> prices,
            int selected, Vector2 cellPosition, float cellSize, float panelWidth)
        {
            _root.gameObject.SetActive(true);

            int rows = Mathf.Min(labels.Count, _rowBacks.Count);
            _root.sizeDelta = new Vector2(Width, HeaderHeight + rows * RowHeight + 6f);

            _title.color = QualityColors.TextFor(item.Quality);
            _title.text = item.DisplayName;

            for (int i = 0; i < _rowBacks.Count; i++)
            {
                bool live = i < rows;
                _rowBacks[i].gameObject.SetActive(live);
                if (!live)
                {
                    continue;
                }

                bool on = i == selected;
                _rowBacks[i].color = on ? UiBuild.Brass : Color.clear;
                _rowLabels[i].color = on ? UiBuild.OnBrass : UiBuild.Bone;
                _rowLabels[i].text = labels[i];
                _rowPrices[i].color = on ? UiBuild.OnBrass : UiBuild.Gold;
                _rowPrices[i].text = prices != null && i < prices.Count && prices[i] > 0
                    ? prices[i].ToString()
                    : string.Empty;
            }

            float x = Mathf.Clamp(cellPosition.x, 0f, Mathf.Max(0f, panelWidth - Width));
            _root.anchoredPosition = new Vector2(x, cellPosition.y - cellSize - 8f);
        }
    }
}
