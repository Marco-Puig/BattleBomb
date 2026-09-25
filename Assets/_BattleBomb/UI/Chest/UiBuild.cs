using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// Small helpers for assembling the chest screen's UGUI hierarchy in code. Built in source
    /// rather than authored in the scene on purpose: the whole screen is placeholder until the
    /// art pass (D47), and a layout that lives in a file is one a diff can review.
    ///
    /// Legacy <see cref="Text"/> rather than TextMeshPro because TMP's essential resources are
    /// not imported into this project, and importing a pile of font assets for UI that gets
    /// redrawn at the art pass buys nothing.
    /// </summary>
    internal static class UiBuild
    {
        internal static readonly Color Ink = new Color(0.86f, 0.90f, 0.97f);
        internal static readonly Color InkDim = new Color(0.56f, 0.62f, 0.74f);
        internal static readonly Color Panel = new Color(0.10f, 0.13f, 0.19f, 0.96f);
        internal static readonly Color PanelInner = new Color(0.14f, 0.17f, 0.25f, 1f);
        internal static readonly Color Focus = new Color(0.42f, 0.60f, 0.92f);
        internal static readonly Color Better = new Color(0.49f, 0.88f, 0.54f);
        internal static readonly Color Worse = new Color(0.88f, 0.49f, 0.49f);
        internal static readonly Color Coin = new Color(0.91f, 0.76f, 0.35f);

        // ── The art pass palette (UI Pass 01) ────────────────────────────────────────
        // Only the item cell reads these so far; the rest of the screen keeps the colours
        // above until the layout pass restyles it.

        /// <summary>The socket an item sits in — near-black, so rank colour is the only hue.</summary>
        internal static readonly Color Bed = Hex("#1c1424");

        /// <summary>Off-white. The die-cut keyline around an icon, and display type.</summary>
        internal static readonly Color Bone = Hex("#f6efe2");

        /// <summary>Pale brass. Chrome is never saturated, so loot always wins the eye.</summary>
        internal static readonly Color Brass = Hex("#c9ab6a");

        /// <summary>Ink on brass.</summary>
        internal static readonly Color OnBrass = Hex("#231a2b");

        /// <summary>The board the sack sits on, and the darker well behind it.</summary>
        internal static readonly Color Board = Hex("#2e2239");
        internal static readonly Color BoardDeep = Hex("#1e1628");
        internal static readonly Color Well = Hex("#120d18");

        /// <summary>Brass at rest, and the shadowed edge under it.</summary>
        internal static readonly Color BrassDim = Hex("#7d6636");

        /// <summary>Coin, and the two delta colours.</summary>
        internal static readonly Color Gold = Hex("#edc65a");
        internal static readonly Color Up = Hex("#63c96e");
        internal static readonly Color Down = Hex("#e0574f");

        // Pad buttons, from the ShopPanel design's hint row (UI Pass 01). A is not in the design —
        // nothing there needed confirming — so its green is the same pastel step as the others.
        internal static readonly Color PadGreen = Hex("#8fd27a");
        internal static readonly Color PadRed = Hex("#e0574f");
        internal static readonly Color PadBlue = Hex("#7fb2e8");
        internal static readonly Color PadYellow = Hex("#e8c95f");

        /// <summary>Body text at its three weights of attention.</summary>
        internal static readonly Color Muted = new Color(0.965f, 0.937f, 0.886f, 0.62f);
        internal static readonly Color Faint = new Color(0.965f, 0.937f, 0.886f, 0.40f);

        private static Font _font;
        private static Sprite _hatch;
        private static Sprite _octagon;
        private static Sprite _hexagon;
        private static Sprite _disc;
        private static Sprite _roundedSquare;

        internal static Font Font =>
            _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Passion One — headings, numbers, anything that shouts. Null until
        /// <see cref="UseFonts"/> has run; <see cref="Label"/> falls back to the built-in font, but
        /// anything assigning it directly must too.</summary>
        internal static Font Display { get; private set; }

        /// <summary>Archivo — body and labels.</summary>
        internal static Font Ui { get; private set; }

        /// <summary>Space Mono — small caps labels, and placeholder plates.</summary>
        internal static Font Mono { get; private set; }

        /// <summary>
        /// Handed the authored font assets by the host. Kept static to match <see cref="Font"/>:
        /// these are read from deep inside the build methods, and threading three font references
        /// through every one of them would be noise.
        /// </summary>
        internal static void UseFonts(Font display, Font ui, Font mono)
        {
            Display = display != null ? display : Font;
            Ui = ui != null ? ui : Font;
            Mono = mono != null ? mono : Font;
        }

        /// <summary>
        /// Diagonal stripes, generated rather than authored. This is the fill for an item that
        /// has no icon yet, and it is meant to look unfinished — a placeholder that could pass
        /// for finished work in a screenshot is a placeholder that never gets replaced.
        /// </summary>
        internal static Sprite Hatch
        {
            get
            {
                if (_hatch != null)
                {
                    return _hatch;
                }

                const int size = 16;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
                {
                    name = "UiBuild.Hatch",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat,
                    hideFlags = HideFlags.HideAndDontSave,
                };

                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // A 45° stripe: on for five pixels of every ten, matching the design's
                        // repeating-linear-gradient.
                        bool on = (x + y) % 10 < 5;
                        pixels[y * size + x] = on
                            ? new Color32(255, 255, 255, 255)
                            : new Color32(255, 255, 255, 0);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                _hatch = Sprite.Create(
                    texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                _hatch.name = "UiBuild.Hatch";
                _hatch.hideFlags = HideFlags.HideAndDontSave;
                return _hatch;
            }
        }

        /// <summary>
        /// The cut silhouette Legendary wears — corners chamfered at 12% of the edge, matching the
        /// design's clip-path. Generated rather than authored, so it costs no art and stays sharp
        /// at any cell size.
        /// </summary>
        internal static Sprite Octagon
        {
            get
            {
                const float c = 0.12f;
                return Polygon(ref _octagon, "UiBuild.Octagon", new[]
                {
                    new Vector2(c, 0f), new Vector2(1f - c, 0f),
                    new Vector2(1f, c), new Vector2(1f, 1f - c),
                    new Vector2(1f - c, 1f), new Vector2(c, 1f),
                    new Vector2(0f, 1f - c), new Vector2(0f, c),
                });
            }
        }

        /// <summary>Mythical's silhouette: a flat-topped hexagon, points at the horizontal edges.</summary>
        internal static Sprite Hexagon => Polygon(ref _hexagon, "UiBuild.Hexagon", new[]
        {
            new Vector2(0.15f, 0f), new Vector2(0.85f, 0f),
            new Vector2(1f, 0.5f),
            new Vector2(0.85f, 1f), new Vector2(0.15f, 1f),
            new Vector2(0f, 0.5f),
        });

        /// <summary>The coin, wherever a price is written. A 32-gon rather than a real circle —
        /// at the sizes this draws, nothing can tell.</summary>
        internal static Sprite Disc => _disc != null ? _disc : Polygon(ref _disc, "UiBuild.Disc", Ring(32));

        /// <summary>
        /// The squared badge: Start, the shoulders, every keyboard key. Nine-sliced, so the design's
        /// 4px corners stay 4px however wide the label makes it — "Enter" is twice the width of "J".
        /// Draw it with <see cref="Image.Type.Sliced"/> and a pixels-per-unit multiplier of
        /// <see cref="RoundedSquareScale"/>.
        /// </summary>
        internal static Sprite RoundedSquare => _roundedSquare != null
            ? _roundedSquare
            : Polygon(ref _roundedSquare, "UiBuild.RoundedSquare", Rounded(0.18f, 8), border: 24f);

        /// <summary>Shrinks the 24-texel slice border to the design's ~4px corner.</summary>
        internal const float RoundedSquareScale = 4f;

        private static Vector2[] Rounded(float radius, int stepsPerCorner)
        {
            var points = new Vector2[stepsPerCorner * 4];
            Vector2[] centres =
            {
                new Vector2(1f - radius, radius),
                new Vector2(1f - radius, 1f - radius),
                new Vector2(radius, 1f - radius),
                new Vector2(radius, radius),
            };

            int n = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                for (int step = 0; step < stepsPerCorner; step++)
                {
                    float degrees = corner * 90f - 90f + 90f * step / (stepsPerCorner - 1);
                    float angle = degrees * Mathf.Deg2Rad;
                    points[n++] = centres[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
            }

            return points;
        }

        private static Vector2[] Ring(int points)
        {
            var ring = new Vector2[points];
            for (int i = 0; i < points; i++)
            {
                float angle = i / (float)points * Mathf.PI * 2f;
                ring[i] = new Vector2(0.5f + 0.5f * Mathf.Cos(angle), 0.5f + 0.5f * Mathf.Sin(angle));
            }

            return ring;
        }

        /// <summary>
        /// A filled convex polygon as a white sprite, for an <see cref="Image"/> to tint. Sampled
        /// four times per axis so the diagonals do not stair-step at cell size.
        /// </summary>
        private static Sprite Polygon(ref Sprite cache, string name, Vector2[] points, float border = 0f)
        {
            if (cache != null)
            {
                return cache;
            }

            const int size = 128;
            const int samples = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            float u = (x + (sx + 0.5f) / samples) / size;
                            float v = (y + (sy + 0.5f) / samples) / size;
                            if (Inside(points, u, v))
                            {
                                hits++;
                            }
                        }
                    }

                    byte alpha = (byte)(255 * hits / (samples * samples));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var rect = new Rect(0f, 0f, size, size);
            var pivot = new Vector2(0.5f, 0.5f);
            cache = border > 0f
                ? Sprite.Create(texture, rect, pivot, size, 0, SpriteMeshType.FullRect,
                    new Vector4(border, border, border, border))
                : Sprite.Create(texture, rect, pivot, size);
            cache.name = name;
            cache.hideFlags = HideFlags.HideAndDontSave;
            return cache;
        }

        /// <summary>Even-odd crossing test. The shapes here are convex, but this costs nothing.</summary>
        private static bool Inside(Vector2[] points, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                if (points[i].y > y != points[j].y > y
                    && x < (points[j].x - points[i].x) * (y - points[i].y)
                        / (points[j].y - points[i].y) + points[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Color Hex(string value) =>
            ColorUtility.TryParseHtmlString(value, out Color parsed) ? parsed : Color.magenta;

        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>A filled box. Stretches to its parent unless the caller re-anchors it.</summary>
        internal static Image Box(string name, Transform parent, Color color)
        {
            RectTransform rect = Rect(name, parent);
            Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// A button for the pointer only (D57). The seats read A and Enter themselves; left to uGUI a
        /// clicked button stays selected, and the UI module's default Submit — every pad's A, and
        /// Enter — would press it a second time, unseen by the seats.
        /// </summary>
        internal static void PointerOnly(Button button) =>
            button.navigation = new Navigation { mode = Navigation.Mode.None };

        internal static Text Label(
            string name, Transform parent, string value, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft, Font font = null)
        {
            RectTransform rect = Rect(name, parent);
            Stretch(rect);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font != null ? font : Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.text = value;
            return text;
        }

        // ── Chrome (UI Pass 01) ──────────────────────────────────────────────────────

        /// <summary>A rect pinned to its parent's top-left, sized in pixels.</summary>
        internal static RectTransform Pin(
            RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        /// <summary>
        /// Gives a label a hard dark edge so it reads over anything. The hero half has no
        /// backdrop — the live world is directly behind its labels — and a fixed text colour can
        /// only ever be right against one background.
        /// </summary>
        internal static Text Legible(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
            return text;
        }

        /// <summary>A horizontal rule in brass — the design's section separator.</summary>
        internal static Image Rule(string name, Transform parent, float thickness, Color color)
        {
            Image bar = Box(name, parent, color);
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, thickness);
            return bar;
        }

        /// <summary>
        /// A filled meter: a dark well with a brass fill inside it. Returns the fill so a caller
        /// can set its width fraction with <see cref="SetFill"/>.
        /// </summary>
        internal static Image Meter(string name, Transform parent, Color fill)
        {
            Image well = Box(name, parent, Well);
            Image bar = Box("Fill", well.rectTransform, fill);
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return bar;
        }

        /// <summary>Sets a <see cref="Meter"/> fill to a 0–1 fraction of its well.</summary>
        internal static void SetFill(Image fill, float fraction)
        {
            RectTransform rect = fill.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Stretches a rect but insets it only on the left and right. Single-line labels in short
        /// rows need this: <see cref="Label"/> truncates vertically, so padding the top and bottom
        /// of a 30px row until the box is shorter than the line makes the text vanish entirely
        /// rather than clip — which looks exactly like a binding that never ran.
        /// </summary>
        internal static void StretchX(RectTransform rect, float pad)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, 0f);
            rect.offsetMax = new Vector2(-pad, 0f);
        }

        internal static void Stretch(RectTransform rect, float pad = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }

        /// <summary>Places a rect by fractions of its parent, then insets it by a pixel margin.</summary>
        internal static RectTransform Place(
            RectTransform rect, float minX, float minY, float maxX, float maxY, float pad = 0f)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
            return rect;
        }

        /// <summary>Rich-text colour wrap — the only formatting the placeholder screen needs.</summary>
        internal static string Tint(string value, Color color) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value}</color>";
    }
}
