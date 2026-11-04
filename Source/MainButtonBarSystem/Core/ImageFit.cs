namespace MainButtonBarSystem.Core
{
    /// <summary>
    /// Where a whole image goes inside an area when it keeps its aspect ratio (docs/API.md section 4): "Fit" shrinks it
    /// to fit inside and places it at the anchors; "Cover" fills the area and crops what sticks out, keeping the side
    /// the anchors name. No dependency on the game, so it can be unit tested.
    /// </summary>
    public static class ImageFit
    {
        /// <summary>
        /// Fit: the largest box with the texture's aspect ratio inside the area, placed at the anchors. GUI coordinates
        /// (y grows downwards). A texture without a size gets the whole area.
        /// </summary>
        public static DecorationBox FitBox(float x, float y, float width, float height, int texWidth, int texHeight,
            DecorationAnchor anchor, DecorationVerticalAnchor verticalAnchor)
        {
            if (texWidth <= 0 || texHeight <= 0) return new DecorationBox(x, y, width, height);
            float scale = System.Math.Min(width / texWidth, height / texHeight);
            float w = texWidth * scale, h = texHeight * scale;
            float left = anchor switch
            {
                DecorationAnchor.Left => x,
                DecorationAnchor.Right => x + width - w,
                _ => x + (width - w) / 2f,
            };
            float top = verticalAnchor switch
            {
                DecorationVerticalAnchor.Top => y,
                DecorationVerticalAnchor.Bottom => y + height - h,
                _ => y + (height - h) / 2f,
            };
            return new DecorationBox(left, top, w, h);
        }

        /// <summary>
        /// Cover: the part of the texture shown, as texture coordinates from 0 to 1 with the origin at the bottom left
        /// (Unity's texCoords): X and Y are where the part starts, Width and Height its size. Left/Bottom keep the left
        /// and bottom edges of the image; Top keeps the top edge (its v starts at 1 minus the height shown).
        /// </summary>
        public static DecorationBox CoverCoords(float width, float height, int texWidth, int texHeight,
            DecorationAnchor anchor, DecorationVerticalAnchor verticalAnchor)
        {
            if (texWidth <= 0 || texHeight <= 0 || width <= 0f || height <= 0f) return new DecorationBox(0f, 0f, 1f, 1f);
            float scale = System.Math.Max(width / texWidth, height / texHeight);
            float u = Clamp01(width / (texWidth * scale));
            float v = Clamp01(height / (texHeight * scale));
            float u0 = anchor switch
            {
                DecorationAnchor.Left => 0f,
                DecorationAnchor.Right => 1f - u,
                _ => (1f - u) / 2f,
            };
            float v0 = verticalAnchor switch
            {
                DecorationVerticalAnchor.Bottom => 0f,
                DecorationVerticalAnchor.Top => 1f - v,
                _ => (1f - v) / 2f,
            };
            return new DecorationBox(u0, v0, u, v);
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
