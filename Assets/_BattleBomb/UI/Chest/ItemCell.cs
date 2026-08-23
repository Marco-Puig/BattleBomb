using BattleBomb.Core.Items;
using BattleBomb.Gameplay.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// One cell of the sack grid, built as the design's die-cut sticker in an enamel socket
    /// (UI Pass 01). The item's own sprite gets a bone keyline and a hard cast shadow from
    /// <see cref="Outline"/> and <see cref="Shadow"/> — four offset copies of the sprite's own
    /// alpha, which is the same trick the design does with drop-shadows, so every existing PNG
    /// and every future one gets a die-cut edge with no per-item authoring.
    ///
    /// Rank speaks twice: hue on the frame, and frame weight from <see cref="QualityFrames"/>.
    /// Focus speaks in neither — it is bone white and dimensional, so it stays unambiguous on a
    /// Shiny cell and a Mythical one alike.
    ///
    /// Not yet built: the octagon and hexagon frame silhouettes for the top two rungs, and the
    /// halo. Both need a shaped sprite; weight and hue carry the ladder until then.
    /// </summary>
    internal sealed class ItemCell
    {
        private const float IconScale = 0.74f;
        private const float PlateScale = 0.78f;

        private readonly RectTransform _root;
        private readonly RectTransform _lift;
        private readonly Image _focusRing;
        private readonly Image _frame;
        private readonly Image _socket;
        private readonly Image _ridge;
        private readonly Image _inner;
        private readonly Image _icon;
        private readonly Outline _keyline;
        private readonly Shadow _cast;
        private readonly Image _plate;
        private readonly Text _plateText;
        private readonly Text _count;
        private readonly Image _lock;
        private readonly Image _wornChip;
        private readonly Text _wornText;

        internal ItemCell(RectTransform parent, string name)
        {
            _root = UiBuild.Rect(name, parent);

            // Everything visual hangs off the lift, so focus can raise the whole sticker without
            // disturbing the cell's place in the grid.
            _lift = UiBuild.Rect("Lift", _root);
            UiBuild.Stretch(_lift);

            _focusRing = UiBuild.Box("Focus", _lift, UiBuild.Bone);
            UiBuild.Stretch(_focusRing.rectTransform, -3f);
            _focusRing.enabled = false;

            // Four layers, because the ridge is a ring rather than a fill: frame, a gap of bed,
            // the ridge itself, then bed again for the item to sit on. With the ridge off, the
            // last two are simply not drawn and the item sits straight on the socket.
            _frame = UiBuild.Box("Frame", _lift, Color.white);
            _socket = UiBuild.Box("Socket", _frame.rectTransform, UiBuild.Bed);
            _ridge = UiBuild.Box("Ridge", _socket.rectTransform, Color.white);
            _ridge.enabled = false;
            _inner = UiBuild.Box("Inner", _ridge.rectTransform, UiBuild.Bed);
            _inner.enabled = false;

            RectTransform bed = _inner.rectTransform;
            _plate = UiBuild.Box("Plate", bed, Color.white);
            _plate.sprite = UiBuild.Hatch;
            _plate.type = Image.Type.Tiled;
            _plateText = UiBuild.Label(
                "PlateText", _plate.rectTransform, string.Empty, 8, Color.white,
                TextAnchor.MiddleCenter, UiBuild.Mono);

            _icon = UiBuild.Box("Icon", bed, Color.white);
            _icon.preserveAspect = true;
            _keyline = _icon.gameObject.AddComponent<Outline>();
            _keyline.effectColor = UiBuild.Bone;
            _cast = _icon.gameObject.AddComponent<Shadow>();
            _cast.effectColor = new Color(0f, 0f, 0f, 0.6f);

            _count = UiBuild.Label(
                "Count", bed, string.Empty, 16, UiBuild.Bone, TextAnchor.LowerRight, UiBuild.Display);

            _lock = UiBuild.Box("Lock", _lift, UiBuild.Bone);
            _wornChip = UiBuild.Box("Worn", _lift, UiBuild.Brass);
            _wornText = UiBuild.Label(
                "WornText", _wornChip.rectTransform, "W", 10, UiBuild.OnBrass,
                TextAnchor.MiddleCenter, UiBuild.Mono);
        }

        /// <summary>Places the cell in its parent by normalised fractions.</summary>
        internal void Place(float minX, float minY, float maxX, float maxY, float pad) =>
            UiBuild.Place(_root, minX, minY, maxX, maxY, pad);

        /// <summary>
        /// Places a square cell by pixels from the parent's top-left. Cells have to be square —
        /// the rank silhouettes are regular polygons, and an oblong one reads as a mistake.
        /// </summary>
        internal void PlaceSquare(float x, float y, float size)
        {
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = new Vector2(size, size);
            _root.anchoredPosition = new Vector2(x, -y);
        }

        /// <summary>Where this cell sits in its parent, so a popover can anchor beneath it.</summary>
        internal Vector2 AnchoredPosition => _root.anchoredPosition;

        /// <summary>The cell's own rect, for callers that pin it somewhere unusual.</summary>
        internal RectTransform Root => _root;

        internal float Size => _root.sizeDelta.x;

        /// <summary>An empty slot: the design's dashed nothing, holding the grid's shape.</summary>
        internal void SetEmpty()
        {
            _lift.anchoredPosition = Vector2.zero;
            _focusRing.enabled = false;
            Apply(_frame, null);
            Apply(_socket, null);
            Apply(_ridge, null);
            Apply(_inner, null);
            Apply(_focusRing, null);
            // A dark socket rather than a near-transparent one. The hero half has no backdrop, so
            // an empty slot there sits straight on the live world — anything translucent reads as
            // a bright smear rather than an empty socket. Opaque works on both halves: quiet
            // against the sack's board, and unmistakably a hole against the world.
            _frame.enabled = true;
            _frame.color = new Color(UiBuild.Brass.r, UiBuild.Brass.g, UiBuild.Brass.b, 0.30f);
            _socket.color = UiBuild.Well;
            Inset(_socket.rectTransform, 2f);
            _ridge.enabled = false;
            _inner.enabled = false;
            UiBuild.Stretch(_ridge.rectTransform);
            UiBuild.Stretch(_inner.rectTransform);

            _icon.enabled = false;
            _plate.enabled = false;
            _plateText.text = string.Empty;
            _count.text = string.Empty;
            _lock.enabled = false;
            _wornChip.enabled = false;
            _wornText.enabled = false;
        }

        /// <summary>
        /// One item. <paramref name="icon"/> may be null — most items have no art yet, and that
        /// draws the hatched plate rather than an empty socket.
        /// </summary>
        internal void Set(
            in ItemInstance item, int count, Sprite icon,
            bool focused, bool worn, bool pendingCombine, float cellSize)
        {
            Color rank = QualityColors.For(item.Quality);
            QualityFrame frame = QualityFrames.For(item.Quality);
            float border = Mathf.Max(2f, frame.BorderFraction * cellSize);

            // The silhouette is the third channel, after hue and weight: by Legendary the rank is
            // readable from the cell's outline with the colour taken away entirely.
            Sprite shape = ShapeSprite(frame.Shape);
            Apply(_frame, shape);
            Apply(_socket, shape);
            Apply(_ridge, shape);
            Apply(_inner, shape);
            Apply(_focusRing, shape);

            _frame.enabled = true;
            _frame.color = pendingCombine ? UiBuild.Bone : rank;
            _socket.color = UiBuild.Bed;
            Inset(_socket.rectTransform, border);

            _ridge.enabled = frame.InnerRidge;
            _inner.enabled = frame.InnerRidge;
            if (frame.InnerRidge)
            {
                _ridge.color = rank;
                _inner.color = UiBuild.Bed;
                Inset(_ridge.rectTransform, border * 0.45f);
                Inset(_inner.rectTransform, border * 0.85f);
            }
            else
            {
                UiBuild.Stretch(_ridge.rectTransform);
                UiBuild.Stretch(_inner.rectTransform);
            }

            bool hasArt = icon != null;
            _icon.enabled = hasArt;
            _keyline.enabled = hasArt;
            _cast.enabled = hasArt;
            if (hasArt)
            {
                _icon.sprite = icon;
                Fraction(_icon.rectTransform, IconScale);

                // The keyline and its cast shadow are fractions of the cell, not fixed pixels —
                // a die-cut edge that is 1.5px at a solo cell would vanish at a split one.
                float key = Mathf.Max(1f, cellSize * 0.018f);
                _keyline.effectDistance = new Vector2(key, key);
                _cast.effectDistance = new Vector2(0f, -Mathf.Max(2f, cellSize * 0.05f));
            }

            _plate.enabled = !hasArt;
            _plateText.enabled = !hasArt;
            if (!hasArt)
            {
                _plate.color = new Color(rank.r, rank.g, rank.b, 0.20f);
                // Keep the stripe fine rather than letting it tile at its native 16px, which
                // reads as blocks rather than hatching once a cell is this small.
                _plate.pixelsPerUnitMultiplier = Mathf.Max(1f, 320f / Mathf.Max(1f, cellSize));
                Fraction(_plate.rectTransform, PlateScale);
                // Lifted toward bone rather than left at the rank hue: the name has to be read
                // against a hatch of its own colour, and the frame already says what rank it is.
                _plateText.color = Color.Lerp(rank, UiBuild.Bone, 0.4f);
                _plateText.text = PlateName(item);
            }

            _count.text = count > 1 ? count.ToString() : string.Empty;
            _count.rectTransform.offsetMin = new Vector2(0f, 1f);
            _count.rectTransform.offsetMax = new Vector2(-4f, 0f);

            _lock.enabled = item.Locked;
            if (item.Locked)
            {
                Corner(_lock.rectTransform, 0f, 1f, cellSize * 0.22f, new Vector2(-4f, 4f));
            }

            _wornChip.enabled = worn;
            _wornText.enabled = worn;
            if (worn)
            {
                Corner(_wornChip.rectTransform, 1f, 1f, cellSize * 0.20f, new Vector2(-2f, -2f));
            }

            // Three things move at once on focus — a bone keyline, a lift, and the socket
            // brightening — so any one of them survives a bad TV or a photo of a couch.
            _focusRing.enabled = focused;
            _lift.anchoredPosition = focused ? new Vector2(0f, 4f) : Vector2.zero;
            if (focused)
            {
                _socket.color = Color.Lerp(UiBuild.Bed, rank, 0.22f);
            }
        }

        /// <summary>
        /// What the placeholder plate spells out. The rank is already the frame's whole job, and
        /// at this size "SHINY LEATHER HELMET" wraps to mush — so the prefix D33 composes onto
        /// the name comes back off, and the plate carries the part that tells items apart.
        /// </summary>
        private static string PlateName(in ItemInstance item)
        {
            string name = item.DisplayName;
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            // Consumables are named "Flask of Health" — no prefix to strip.
            if (item.Slot != ItemSlot.Consumable && item.Quality != QualityRank.Nothing)
            {
                string prefix = item.Quality + " ";
                if (name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    name = name.Substring(prefix.Length);
                }
            }

            return name.ToUpperInvariant();
        }

        private static Sprite ShapeSprite(FrameShape shape)
        {
            switch (shape)
            {
                case FrameShape.Octagon: return UiBuild.Octagon;
                case FrameShape.Hexagon: return UiBuild.Hexagon;
                default: return null;
            }
        }

        /// <summary>A null sprite is a plain rectangle, which is what the lower rungs want.</summary>
        private static void Apply(Image image, Sprite shape)
        {
            image.sprite = shape;
            image.type = Image.Type.Simple;
        }

        private static void Inset(RectTransform rect, float pad)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }

        /// <summary>Centres a rect at a fraction of its parent.</summary>
        private static void Fraction(RectTransform rect, float scale)
        {
            float edge = (1f - scale) * 0.5f;
            rect.anchorMin = new Vector2(edge, edge);
            rect.anchorMax = new Vector2(1f - edge, 1f - edge);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Pins a square badge to one corner, deliberately overhanging the frame.</summary>
        private static void Corner(RectTransform rect, float x, float y, float size, Vector2 nudge)
        {
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);
            rect.pivot = new Vector2(x, y);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = nudge;
        }
    }
}
