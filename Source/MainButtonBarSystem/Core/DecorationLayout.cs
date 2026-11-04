namespace MainButtonBarSystem.Core
{
    /// <summary>Horizontal anchor of a theme decoration on its target area (the bar or the drawer).</summary>
    public enum DecorationAnchor
    {
        Left,
        Center,
        Right,
    }

    /// <summary>Vertical anchor of a theme decoration. Bottom is the default: a portrait grows upwards out of the bar.</summary>
    public enum DecorationVerticalAnchor
    {
        Bottom,
        Center,
        Top,
    }

    /// <summary>Where a decoration is drawn, in the same coordinates as its target area. Empty when it should not be drawn.</summary>
    public readonly struct DecorationBox
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public DecorationBox(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool IsEmpty => Width <= 0f || Height <= 0f;
    }

    /// <summary>
    /// Placement rule for theme decorations (docs/API.md section 4), with no dependency on the game so it can be unit
    /// tested. The box takes its size from the XML <c>size</c> (0 on both axes: the texture's pixel size; 0 on one axis:
    /// keep the texture's aspect ratio), times the player's scale; then its anchor corner is put on the matching edge of
    /// the target area, and the theme's offset and the player's offset are added.
    /// </summary>
    public static class DecorationLayout
    {
        public static DecorationBox Place(float targetX, float targetY, float targetWidth, float targetHeight,
            DecorationAnchor anchor, DecorationVerticalAnchor verticalAnchor, float sizeX, float sizeY, int texWidth, int texHeight,
            float offsetX, float offsetY, float playerScale, float playerOffsetX, float playerOffsetY)
        {
            float width, height;
            if (sizeX > 0f && sizeY > 0f)
            {
                width = sizeX;
                height = sizeY;
            }
            else if (sizeX > 0f)
            {
                width = sizeX;
                height = texWidth > 0 ? sizeX * texHeight / texWidth : 0f;
            }
            else if (sizeY > 0f)
            {
                height = sizeY;
                width = texHeight > 0 ? sizeY * texWidth / texHeight : 0f;
            }
            else
            {
                width = texWidth;
                height = texHeight;
            }

            float scale = playerScale > 0f ? playerScale : 1f;
            width *= scale;
            height *= scale;
            if (width <= 0f || height <= 0f) return default;

            float x;
            switch (anchor)
            {
                case DecorationAnchor.Center: x = targetX + (targetWidth - width) / 2f; break;
                case DecorationAnchor.Right: x = targetX + targetWidth - width; break;
                default: x = targetX; break;
            }
            float y;
            switch (verticalAnchor)
            {
                case DecorationVerticalAnchor.Top: y = targetY; break;
                case DecorationVerticalAnchor.Center: y = targetY + (targetHeight - height) / 2f; break;
                default: y = targetY + targetHeight - height; break;
            }
            return new DecorationBox(x + offsetX + playerOffsetX, y + offsetY + playerOffsetY, width, height);
        }
    }
}
