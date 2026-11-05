using System;

namespace MainButtonBarSystem.Core
{
    /// <summary>Where a custom theme puts a button's icon (design spec 2026-11-05 §4.8); Default keeps the game's place.</summary>
    public enum IconAnchor
    {
        Default,
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
    }

    /// <summary>Icon placement inside a button, with no dependency on the game so it can be unit tested.</summary>
    public static class IconPlacement
    {
        /// <summary>0 left, 1 centre, 2 right.</summary>
        public static int Column(IconAnchor anchor) => anchor switch
        {
            IconAnchor.TopLeft or IconAnchor.Left or IconAnchor.BottomLeft => 0,
            IconAnchor.TopRight or IconAnchor.Right or IconAnchor.BottomRight => 2,
            _ => 1,
        };

        /// <summary>0 top, 1 middle, 2 bottom.</summary>
        public static int Row(IconAnchor anchor) => anchor switch
        {
            IconAnchor.TopLeft or IconAnchor.Top or IconAnchor.TopRight => 0,
            IconAnchor.BottomLeft or IconAnchor.Bottom or IconAnchor.BottomRight => 2,
            _ => 1,
        };

        /// <summary>
        /// "Icon only": a square of <paramref name="size"/> (never larger than the button) at one of nine points,
        /// <paramref name="padding"/> away from the edges it is anchored to, then moved by the offset. Default is the centre.
        /// </summary>
        public static DecorationBox Alone(float x, float y, float width, float height, float size, IconAnchor anchor, float padding, float offsetX, float offsetY)
        {
            size = Math.Max(0f, Math.Min(size, Math.Min(width, height)));
            float left = Column(anchor) switch
            {
                0 => x + padding,
                2 => x + width - padding - size,
                _ => x + (width - size) / 2f,
            };
            float top = Row(anchor) switch
            {
                0 => y + padding,
                2 => y + height - padding - size,
                _ => y + (height - size) / 2f,
            };
            return new DecorationBox(left + offsetX, top + offsetY, size, size);
        }

        /// <summary>
        /// "Icon and text": the icon at the left end (default) or the right end (anchors on the right column), centred
        /// vertically, then moved by the offset. <paramref name="textLeft"/> and <paramref name="textRight"/> bound the
        /// label beside it.
        /// </summary>
        public static DecorationBox Beside(float x, float y, float width, float height, float size, IconAnchor anchor, float padding, float gap,
            float offsetX, float offsetY, out float textLeft, out float textRight)
        {
            size = Math.Max(0f, Math.Min(size, height - padding));
            bool right = Column(anchor) == 2;
            float left = right ? x + width - padding - size : x + padding;
            textLeft = right ? x : left + size + gap - padding;
            textRight = right ? left - gap + padding : x + width;
            return new DecorationBox(left + offsetX, y + (height - size) / 2f + offsetY, size, size);
        }
    }
}
