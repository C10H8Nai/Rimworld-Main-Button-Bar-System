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
        /// <summary>Draws <paramref name="texture"/> into <paramref name="rect"/>; <paramref name="alpha"/> multiplies the current GUI colour's alpha.</summary>
        public static void Draw(Rect rect, Texture2D texture, BackgroundFit fit, float tileScale, float alpha)
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
                        GUI.DrawTexture(FitRect(rect, texture.width, texture.height), texture);
                        break;
                    case BackgroundFit.Cover:
                        GUI.DrawTextureWithTexCoords(rect, texture, CoverCoords(rect, texture.width, texture.height));
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

        /// <summary>The largest centred rectangle with the texture's aspect ratio that fits inside <paramref name="rect"/>.</summary>
        internal static Rect FitRect(Rect rect, int texWidth, int texHeight)
        {
            if (texWidth <= 0 || texHeight <= 0) return rect;
            float scale = Mathf.Min(rect.width / texWidth, rect.height / texHeight);
            float width = texWidth * scale, height = texHeight * scale;
            return new Rect(rect.x + (rect.width - width) / 2f, rect.y + (rect.height - height) / 2f, width, height);
        }

        /// <summary>The centred part of the texture (in 0..1 coordinates) that covers <paramref name="rect"/> at the texture's aspect ratio.</summary>
        internal static Rect CoverCoords(Rect rect, int texWidth, int texHeight)
        {
            if (texWidth <= 0 || texHeight <= 0) return new Rect(0f, 0f, 1f, 1f);
            float scale = Mathf.Max(rect.width / texWidth, rect.height / texHeight);
            float u = Mathf.Clamp01(rect.width / (texWidth * scale));
            float v = Mathf.Clamp01(rect.height / (texHeight * scale));
            return new Rect((1f - u) / 2f, (1f - v) / 2f, u, v);
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
