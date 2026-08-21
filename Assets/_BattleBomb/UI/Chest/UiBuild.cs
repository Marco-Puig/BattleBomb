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

        private static Font _font;

        internal static Font Font =>
            _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

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

        internal static Text Label(
            string name, Transform parent, string value, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            RectTransform rect = Rect(name, parent);
            Stretch(rect);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
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
