using BattleBomb.Core.Items;
using BattleBomb.Gameplay.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// "Clear the junk": one press that sells every unlocked piece below a threshold the player
    /// sets here. It sits on both sides of the shop counter because it is the only bulk action in
    /// the game, and hiding it behind the Sell tab would make it feel like a mode rather than a
    /// tool. The count and the payout are previewed before the press — the sweep has no confirm
    /// step, so the numbers have to be on screen before the button is the confirm.
    /// </summary>
    internal sealed class JunkBar
    {
        internal const float Height = 88f;

        private const float PadX = 17f;
        private const float PadY = 14f;
        private const float StepperWidth = 196f;
        private const float TotalsWidth = 132f;

        private readonly RectTransform _root;
        private readonly Image _ring;
        private readonly Image _back;
        private readonly Text _title;
        private readonly Text _blurb;
        private readonly Image _stepper;
        private readonly Text _below;
        private readonly Text _rank;
        private readonly Text _arrows;
        private readonly Text _pieces;
        private readonly Image _coin;
        private readonly Text _total;

        internal JunkBar(RectTransform parent)
        {
            _root = UiBuild.Rect("JunkBar", parent);

            _ring = UiBuild.Box("Ring", _root, UiBuild.Bone);
            UiBuild.Stretch(_ring.rectTransform, -3f);
            _ring.enabled = false;

            _back = UiBuild.Box("Back", _root, UiBuild.Well);
            UiBuild.Stretch(_back.rectTransform);
            _back.color = new Color(UiBuild.Down.r, UiBuild.Down.g, UiBuild.Down.b, 0.09f);

            _title = UiBuild.Label("Title", _root, "CLEAR THE JUNK", 24, UiBuild.Bone,
                TextAnchor.UpperLeft, UiBuild.Display);
            _blurb = UiBuild.Label("Blurb", _root,
                "Sells every unlocked piece below the threshold. Worn gear, locks and "
                    + "consumables are never touched.",
                12, UiBuild.Muted, TextAnchor.UpperLeft, UiBuild.Ui);

            _stepper = UiBuild.Box("Stepper", _root, UiBuild.Well);
            _below = UiBuild.Label("Below", _stepper.rectTransform, "BELOW", 9, UiBuild.Brass,
                TextAnchor.MiddleLeft, UiBuild.Mono);
            _rank = UiBuild.Label("Rank", _stepper.rectTransform, string.Empty, 19, UiBuild.Bone,
                TextAnchor.MiddleCenter, UiBuild.Display);
            _arrows = UiBuild.Label("Arrows", _stepper.rectTransform, "◀ ▶", 11, UiBuild.Brass,
                TextAnchor.MiddleRight, UiBuild.Ui);

            _pieces = UiBuild.Label("Pieces", _root, string.Empty, 9, UiBuild.Faint,
                TextAnchor.UpperRight, UiBuild.Mono);
            _coin = UiBuild.Box("Coin", _root, UiBuild.Gold);
            _coin.sprite = UiBuild.Disc;
            _total = UiBuild.Label("Total", _root, string.Empty, 22, UiBuild.Gold,
                TextAnchor.MiddleRight, UiBuild.Ui);
        }

        internal void SetActive(bool on) => _root.gameObject.SetActive(on);

        /// <summary>Places the bar by pixels down from the top of its parent.</summary>
        internal void Place(float y, float width)
        {
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = new Vector2(width, Height);
            _root.anchoredPosition = new Vector2(0f, -y);

            float textWidth = Mathf.Max(120f, width - PadX * 2f - StepperWidth - TotalsWidth - 32f);
            // 24px of Passion One needs more than 26px of box, or Label drops the line entirely.
            UiBuild.Pin(_title.rectTransform, PadX, PadY - 2f, textWidth, 33f);
            UiBuild.Pin(_blurb.rectTransform, PadX, PadY + 31f, textWidth, 34f);

            RectTransform stepper = UiBuild.Pin(
                _stepper.rectTransform, 0f, PadY + 12f, StepperWidth, 38f);
            stepper.anchorMin = new Vector2(1f, 1f);
            stepper.anchorMax = new Vector2(1f, 1f);
            stepper.pivot = new Vector2(1f, 1f);
            stepper.anchoredPosition = new Vector2(-(PadX + TotalsWidth + 16f), -(PadY + 12f));

            UiBuild.StretchX(_below.rectTransform, 10f);
            UiBuild.StretchX(_rank.rectTransform, 10f);
            UiBuild.StretchX(_arrows.rectTransform, 10f);

            PinRight(_pieces.rectTransform, PadX, PadY + 6f, TotalsWidth, 15f);

            // The total is right-aligned and the coin is pinned, so a narrow box keeps the two
            // together whether the sweep is worth 0 or 1240.
            PinRight(_total.rectTransform, PadX, PadY + 26f, 76f, 28f);
            PinRight(_coin.rectTransform, PadX + 82f, PadY + 33f, 13f, 13f);
        }

        internal void Set(QualityRank below, in JunkSale sale, bool focused)
        {
            // Opaque when focused: the bone ring sits behind this fill, and a translucent one
            // lets the whole ring through, turning the block into a cream slab with unreadable
            // type on it.
            _ring.enabled = focused;
            _back.color = focused
                ? Color.Lerp(UiBuild.Board, UiBuild.Down, 0.34f)
                : new Color(UiBuild.Down.r, UiBuild.Down.g, UiBuild.Down.b, 0.09f);

            _rank.text = below.ToString().ToUpperInvariant();
            _rank.color = QualityColors.TextFor(below);
            _arrows.color = focused ? UiBuild.Bone : UiBuild.Brass;

            _pieces.text = sale.Pieces == 1 ? "1 PIECE" : $"{sale.Pieces} PIECES";
            _total.text = sale.Coins.ToString();

            // Nothing to sweep reads as a dead control rather than a zero worth pressing.
            Color live = sale.IsEmpty ? UiBuild.Faint : UiBuild.Gold;
            _total.color = live;
            _coin.color = live;
        }

        private static void PinRight(RectTransform rect, float x, float y, float width, float height)
        {
            UiBuild.Pin(rect, 0f, y, width, height);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-x, -y);
        }
    }
}
