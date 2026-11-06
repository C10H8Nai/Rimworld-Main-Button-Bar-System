using System;

namespace MainButtonBarSystem.Core
{
    /// <summary>
    /// Where this mod redraws a bar button's label (alignment, theme font), with no dependency on the game so it can be
    /// unit tested. The game's subtle button takes a <c>textLeftMargin</c>: -1 lets the game use a share of the width,
    /// and a caller that draws something left of the label passes a larger margin (the game passes 2 for a long label;
    /// Vanilla Textures Expanded adds its icon's width when it draws icon and label). The redrawn label keeps that room.
    /// </summary>
    public static class LabelPlacement
    {
        /// <summary>
        /// How far right of the button's left edge the label area starts. The label is then drawn with
        /// <paramref name="sidePadding"/> on both sides, so the text starts where the game would put it.
        /// </summary>
        /// <param name="followGame">The label keeps the game's place (no alignment chosen): the same left edge as the game.</param>
        /// <param name="textLeftMargin">The margin the caller gave the game's subtle button; negative for "use the default".</param>
        /// <param name="width">The button's width.</param>
        /// <param name="gameLeftMarginFraction">The game's default margin as a share of the width.</param>
        /// <param name="sidePadding">The padding the label is drawn with inside its area.</param>
        public static float LeftInset(bool followGame, float textLeftMargin, float width, float gameLeftMarginFraction, float sidePadding)
        {
            if (followGame)
            {
                float margin = textLeftMargin >= 0f ? textLeftMargin : width * gameLeftMarginFraction;
                return margin - sidePadding;
            }
            // An alignment is chosen: only room the caller asked for is kept (none by default), never less than the edge.
            return textLeftMargin >= 0f ? Math.Max(0f, textLeftMargin - sidePadding) : 0f;
        }
    }
}
