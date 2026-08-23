using System.Text;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// The selected item read against what is already worn (UI Pass 01). Sits under the grid
    /// rather than beside it: the sack is the wide thing on this screen, and a column down the
    /// right would have squeezed it.
    ///
    /// Twelve rows, three columns of four, every one of them a field of
    /// <see cref="GearContribution"/> — the design was built from the struct, so this is a
    /// straight mapping rather than a chosen subset. Rows with nothing on either side still draw,
    /// so the panel's height never changes as the cursor moves.
    /// </summary>
    internal sealed class ComparePanel
    {
        private const int Rows = 4;
        private const int Columns = 3;
        private const int RowCount = Rows * Columns;

        private static readonly Color Flat = new Color(0.965f, 0.937f, 0.886f, 0.28f);

        private readonly RectTransform _root;
        private readonly Image _thumbFrame;
        private readonly Image _thumbIcon;
        private readonly Image _thumbPlate;
        private readonly Text _name;
        private readonly Text _meta;
        private readonly Text _versus;
        private readonly Text[] _rowName = new Text[RowCount];
        private readonly Text[] _rowValue = new Text[RowCount];
        private readonly Text[] _rowDelta = new Text[RowCount];
        private readonly Image[] _pips = new Image[8];
        private readonly Text _pipLabel;
        private readonly Text _footLabel;
        private readonly Text _footValue;
        private readonly StringBuilder _text = new StringBuilder(64);

        internal ComparePanel(RectTransform parent)
        {
            Image back = UiBuild.Box("Compare", parent, UiBuild.BoardDeep);
            _root = back.rectTransform;
            UiBuild.Stretch(_root);

            RectTransform pad = UiBuild.Rect("Pad", _root);
            UiBuild.Stretch(pad, 14f);

            // ── Head: thumbnail, name, the one-line context under it ──
            _thumbFrame = UiBuild.Box("Thumb", pad, UiBuild.Brass);
            UiBuild.Pin(_thumbFrame.rectTransform, 0f, 0f, 62f, 62f);
            Image socket = UiBuild.Box("Socket", _thumbFrame.rectTransform, UiBuild.Well);
            UiBuild.Stretch(socket.rectTransform, 4f);
            _thumbPlate = UiBuild.Box("Plate", socket.rectTransform, Color.white);
            _thumbPlate.sprite = UiBuild.Hatch;
            _thumbPlate.type = Image.Type.Tiled;
            UiBuild.Stretch(_thumbPlate.rectTransform, 5f);
            _thumbIcon = UiBuild.Box("Icon", socket.rectTransform, Color.white);
            _thumbIcon.preserveAspect = true;
            UiBuild.Stretch(_thumbIcon.rectTransform, 6f);

            RectTransform head = UiBuild.Rect("Head", pad);
            UiBuild.Pin(head, 74f, 0f, 760f, 62f);
            _name = UiBuild.Label("Name", head, string.Empty, 26, UiBuild.Bone,
                TextAnchor.UpperLeft, UiBuild.Display);
            UiBuild.Pin(_name.rectTransform, 0f, 0f, 760f, 30f);
            _meta = UiBuild.Label("Meta", head, string.Empty, 12, UiBuild.Muted,
                TextAnchor.UpperLeft, UiBuild.Ui);
            UiBuild.Pin(_meta.rectTransform, 0f, 32f, 480f, 18f);
            _versus = UiBuild.Label("Versus", head, string.Empty, 12, UiBuild.Muted,
                TextAnchor.UpperLeft, UiBuild.Ui);
            UiBuild.Pin(_versus.rectTransform, 0f, 50f, 620f, 18f);

            // ── The twelve rows ──
            RectTransform table = UiBuild.Rect("Table", pad);
            table.anchorMin = new Vector2(0f, 0f);
            table.anchorMax = new Vector2(1f, 1f);
            table.offsetMin = new Vector2(0f, 34f);
            table.offsetMax = new Vector2(0f, -74f);

            float columnWidth = 1f / Columns;
            for (int i = 0; i < RowCount; i++)
            {
                int column = i / Rows;
                int row = i % Rows;
                RectTransform line = UiBuild.Rect($"Row {i}", table);
                line.anchorMin = new Vector2(column * columnWidth, 1f - (row + 1) / (float)Rows);
                line.anchorMax = new Vector2((column + 1) * columnWidth, 1f - row / (float)Rows);
                line.offsetMin = new Vector2(column == 0 ? 0f : 14f, 1f);
                line.offsetMax = new Vector2(-8f, -1f);

                _rowName[i] = UiBuild.Label("N", line, string.Empty, 12, UiBuild.Muted,
                    TextAnchor.MiddleLeft, UiBuild.Ui);
                _rowValue[i] = UiBuild.Label("V", line, string.Empty, 13, UiBuild.Bone,
                    TextAnchor.MiddleRight, UiBuild.Ui);
                UiBuild.Stretch(_rowValue[i].rectTransform);
                _rowValue[i].rectTransform.offsetMax = new Vector2(-56f, 0f);
                _rowDelta[i] = UiBuild.Label("D", line, string.Empty, 11, Flat,
                    TextAnchor.MiddleRight, UiBuild.Ui);

                Image underline = UiBuild.Box("Rule", line, new Color(0.79f, 0.67f, 0.42f, 0.11f));
                RectTransform rule = underline.rectTransform;
                rule.anchorMin = new Vector2(0f, 0f);
                rule.anchorMax = new Vector2(1f, 0f);
                rule.pivot = new Vector2(0.5f, 0f);
                rule.sizeDelta = new Vector2(0f, 1f);
                rule.anchoredPosition = Vector2.zero;
            }

            // ── Foot: upgrade capacity on the left, the next point's price on the right ──
            RectTransform foot = UiBuild.Rect("Foot", pad);
            foot.anchorMin = new Vector2(0f, 0f);
            foot.anchorMax = new Vector2(1f, 0f);
            foot.pivot = new Vector2(0.5f, 0f);
            foot.sizeDelta = new Vector2(0f, 26f);

            _pipLabel = UiBuild.Label("CapacityLabel", foot, "CAPACITY", 9, UiBuild.Faint,
                TextAnchor.MiddleLeft, UiBuild.Mono);
            UiBuild.Pin(_pipLabel.rectTransform, 0f, 4f, 70f, 18f);
            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i] = UiBuild.Box($"Pip {i}", foot, UiBuild.Brass);
                UiBuild.Pin(_pips[i].rectTransform, 74f + i * 14f, 8f, 11f, 11f);
                _pips[i].enabled = false;
            }

            _footValue = UiBuild.Label("FootValue", foot, string.Empty, 13, UiBuild.Gold,
                TextAnchor.MiddleRight, UiBuild.Ui);
            UiBuild.Stretch(_footValue.rectTransform);
            _footLabel = UiBuild.Label("FootLabel", foot, string.Empty, 9, UiBuild.Faint,
                TextAnchor.MiddleRight, UiBuild.Mono);
            UiBuild.StretchX(_footLabel.rectTransform, 0f);
            _footLabel.rectTransform.offsetMax = new Vector2(-52f, 0f);
        }

        internal void SetEmpty(string message)
        {
            _thumbFrame.enabled = false;
            _thumbIcon.enabled = false;
            _thumbPlate.enabled = false;
            _name.text = message ?? string.Empty;
            _name.color = UiBuild.Muted;
            _meta.text = string.Empty;
            _versus.text = string.Empty;
            for (int i = 0; i < RowCount; i++)
            {
                _rowName[i].text = string.Empty;
                _rowValue[i].text = string.Empty;
                _rowDelta[i].text = string.Empty;
            }

            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i].enabled = false;
            }

            _pipLabel.enabled = false;
            _footLabel.text = string.Empty;
            _footValue.text = string.Empty;
        }

        internal void Set(in ItemInstance item, in ItemInstance worn, Sprite icon, int nextPointCost)
        {
            Color rank = QualityColors.For(item.Quality);
            _thumbFrame.enabled = true;
            _thumbFrame.color = rank;
            _thumbIcon.enabled = icon != null;
            _thumbIcon.sprite = icon;
            _thumbPlate.enabled = icon == null;
            _thumbPlate.color = new Color(rank.r, rank.g, rank.b, 0.30f);

            _name.color = QualityColors.TextFor(item.Quality);
            _name.text = item.DisplayName;

            _text.Clear();
            _text.Append("Requires lv ").Append(item.RequiredLevel);
            if (item.Locked)
            {
                _text.Append("   ").Append(UiBuild.Tint("LOCKED", UiBuild.Gold));
            }

            _meta.text = _text.ToString();

            _text.Clear();
            if (item.IsConsumable)
            {
                _text.Append("Restores ")
                    .Append((item.ConsumableHealFraction * 100f).ToString("F0"))
                    .Append("% ").Append(item.Restores == RestoreKind.Mana ? "mana" : "health");
            }
            else if (worn.IsEmpty)
            {
                _text.Append("Nothing worn in this slot");
            }
            else
            {
                _text.Append("vs worn  ")
                    .Append(UiBuild.Tint(worn.DisplayName, QualityColors.TextFor(worn.Quality)));
            }

            _versus.text = _text.ToString();

            GearContribution mine = item.TotalContribution();
            GearContribution theirs = worn.IsEmpty ? default : worn.TotalContribution();

            int r = 0;
            Row(ref r, "Damage", mine.WeaponDamage, theirs.WeaponDamage, "F0", false, false);
            Row(ref r, "Swing speed", mine.SwingSpeedBonus, theirs.SwingSpeedBonus, "F2", false, false);
            Row(ref r, "Weight", mine.Weight, theirs.Weight, "F1", false, true);
            Row(ref r, "Crit chance", mine.CritChance, theirs.CritChance, "F0", true, false);
            Row(ref r, "Crit damage", mine.CritDamageBonus, theirs.CritDamageBonus, "F0", true, false);
            Row(ref r, "Life steal", mine.LifeSteal, theirs.LifeSteal, "F0", true, false);
            Row(ref r, "Defence", mine.Defence, theirs.Defence, "F0", true, false);
            Row(ref r, "Max HP", mine.MaxHealthBonus, theirs.MaxHealthBonus, "F0", false, false);
            Row(ref r, "Max mana", mine.MaxManaBonus, theirs.MaxManaBonus, "F0", false, false);
            Row(ref r, "Mana regen", mine.ManaRegen, theirs.ManaRegen, "F1", false, false);
            Row(ref r, "Knockback", mine.KnockbackBonus, theirs.KnockbackBonus, "F0", true, false);
            Row(ref r, "Magic damage", mine.MagicDamage, theirs.MagicDamage, "F0", false, false);

            int capacity = Mathf.Clamp(item.UpgradeCapacity, 0, _pips.Length);
            _pipLabel.enabled = capacity > 0;
            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i].enabled = i < capacity;
                _pips[i].color = i < item.UpgradesSpent
                    ? UiBuild.Brass
                    : new Color(UiBuild.Brass.r, UiBuild.Brass.g, UiBuild.Brass.b, 0.28f);
            }

            bool canUpgrade = nextPointCost > 0;
            _footLabel.text = canUpgrade ? "NEXT POINT" : string.Empty;
            _footValue.text = canUpgrade ? nextPointCost.ToString() : string.Empty;
        }

        /// <summary>
        /// One row. Percentages are stored as fractions and shown as whole percents, which is why
        /// the scale travels with the row rather than being applied at every call site.
        /// </summary>
        private void Row(
            ref int index, string name, float mine, float theirs, string format,
            bool percent, bool lowerIsBetter)
        {
            if (index >= RowCount)
            {
                return;
            }

            float scale = percent ? 100f : 1f;
            float a = mine * scale;
            float b = theirs * scale;
            string suffix = percent ? "%" : string.Empty;

            _rowName[index].text = name;
            _rowValue[index].text = a.ToString(format) + suffix;

            float delta = a - b;
            if (Mathf.Abs(delta) < 0.005f)
            {
                _rowDelta[index].text = "—";
                _rowDelta[index].color = Flat;
            }
            else
            {
                bool better = lowerIsBetter ? delta < 0f : delta > 0f;
                _rowDelta[index].color = better ? UiBuild.Up : UiBuild.Down;
                _rowDelta[index].text = (delta > 0f ? "▲ +" : "▼ ") + delta.ToString(format) + suffix;
            }

            index++;
        }
    }
}
