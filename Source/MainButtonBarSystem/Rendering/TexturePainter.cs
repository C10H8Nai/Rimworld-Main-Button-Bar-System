using MainButtonBarSystem.Core;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Draws a theme's background image in one of the <see cref="BackgroundFit"/> ways. Fit and Cover compute their own
    /// rectangles and texture coordinates, and Tile loops over the area inside a clipping group, so nothing depends on
    /// the texture's wrap mode or on scale modes the engine may not honour.
    /// </summary>
    internal static class TexturePainter
    {
        /// <summary>Draws <paramref name="texture"/> into <paramref name="rect"/>, centred; <paramref name="alpha"/> multiplies the current GUI colour's alpha.</summary>
        public static void Draw(Rect rect, Texture2D texture, BackgroundFit fit, float tileScale, float alpha) =>
            DrawAnchored(rect, texture, fit, tileScale, alpha, DecorationAnchor.Center, DecorationVerticalAnchor.Center);

        /// <summary>
        /// Like <see cref="Draw"/>, with anchors for Fit (where the image sits) and Cover (which side is kept when
        /// cropping). Stretch, Tile and Atlas ignore them.
        /// </summary>
        public static void DrawAnchored(Rect rect, Texture2D texture, BackgroundFit fit, float tileScale, float alpha,
            DecorationAnchor anchor, DecorationVerticalAnchor verticalAnchor)
        {
            if (texture == null || rect.width <= 0f || rect.height <= 0f) return;
            var saved = GUI.color;
            if (alpha < 1f) GUI.color = new Color(saved.r, saved.g, saved.b, saved.a * Mathf.Clamp01(alpha));
            try
            {
                switch (fit)
                {
                    case BackgroundFit.Stretch:
                        GUI.DrawTexture(rect, texture);
                        break;
                    case BackgroundFit.Fit:
                        GUI.DrawTexture(FitRect(rect, texture.width, texture.height, anchor, verticalAnchor), texture);
                        break;
                    case BackgroundFit.Cover:
                        // Through the game's DrawTexturePart (part given from the top left), so the texture coordinate
                        // convention is the game's own whatever the engine layer does.
                        var uv = CoverCoords(rect, texture.width, texture.height, anchor, verticalAnchor);
                        Widgets.DrawTexturePart(rect, new Rect(uv.x, 1f - uv.y - uv.height, uv.width, uv.height), texture);
                        break;
                    case BackgroundFit.Tile:
                        DrawTiled(rect, texture, tileScale);
                        break;
                    default:
                        Widgets.DrawAtlas(rect, texture);
                        break;
                }
            }
            finally
            {
                GUI.color = saved;
            }
        }

        /// <summary>The largest rectangle with the texture's aspect ratio that fits inside <paramref name="rect"/>, placed at the anchors (<see cref="ImageFit.FitBox"/>).</summary>
        internal static Rect FitRect(Rect rect, int texWidth, int texHeight,
            DecorationAnchor anchor = DecorationAnchor.Center, DecorationVerticalAnchor verticalAnchor = DecorationVerticalAnchor.Center)
        {
            var box = ImageFit.FitBox(rect.x, rect.y, rect.width, rect.height, texWidth, texHeight, anchor, verticalAnchor);
            return new Rect(box.X, box.Y, box.Width, box.Height);
        }

        /// <summary>The part of the texture (0..1, origin bottom left) that covers <paramref name="rect"/> at the texture's aspect ratio, keeping the anchored side (<see cref="ImageFit.CoverCoords"/>).</summary>
        internal static Rect CoverCoords(Rect rect, int texWidth, int texHeight,
            DecorationAnchor anchor = DecorationAnchor.Center, DecorationVerticalAnchor verticalAnchor = DecorationVerticalAnchor.Center)
        {
            var uv = ImageFit.CoverCoords(rect.width, rect.height, texWidth, texHeight, anchor, verticalAnchor);
            return new Rect(uv.X, uv.Y, uv.Width, uv.Height);
        }

        /// <summary>Repeats the texture from the top-left corner; tiles beyond the tuning def's limit fall back to stretching.</summary>
        private static void DrawTiled(Rect rect, Texture2D texture, float tileScale)
        {
            float scale = tileScale > 0f ? tileScale : 1f;
            float tileWidth = texture.width * scale, tileHeight = texture.height * scale;
            if (tileWidth < 1f || tileHeight < 1f)
            {
                GUI.DrawTexture(rect, texture);
                return;
            }
            int columns = Mathf.CeilToInt(rect.width / tileWidth), rows = Mathf.CeilToInt(rect.height / tileHeight);
            if (columns * rows > MainButtonBarTuningDef.Current.behaviour.themeTileMaxCount)
            {
                GUI.DrawTexture(rect, texture);
                return;
            }
            GUI.BeginGroup(rect);
            try
            {
                for (int row = 0; row < rows; row++)
                    for (int column = 0; column < columns; column++)
                        GUI.DrawTexture(new Rect(column * tileWidth, row * tileHeight, tileWidth, tileHeight), texture);
            }
            finally
            {
                GUI.EndGroup();
            }
        }
    }
}
