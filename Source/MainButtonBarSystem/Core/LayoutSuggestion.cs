using System;
using System.Collections.Generic;

namespace MainButtonBarSystem.Core
{
    /// <summary>The bar settings a theme's suggested layout touches, as the bar uses them now.</summary>
    public struct LayoutValues
    {
        /// <summary>Display mode (a BarDisplayModeDef defName).</summary>
        public string Mode;

        public int DefaultWidth;
        public int MinWidth;

        /// <summary>Half-width buttons (MainButtonDef.minimized) get the full width.</summary>
        public bool MinimizedFullWidth;

        public bool SameAs(LayoutValues other) =>
            string.Equals(Mode, other.Mode, StringComparison.Ordinal) && DefaultWidth == other.DefaultWidth && MinWidth == other.MinWidth &&
            MinimizedFullWidth == other.MinimizedFullWidth;
    }

    /// <summary>
    /// A theme's suggested layout: the theme never changes the bar by itself; the Appearance tab offers the suggestion
    /// while the bar differs from it, and applies it only when the player presses the button.
    /// </summary>
    public static class LayoutSuggestion
    {
        /// <summary>
        /// The values after applying a suggestion. The display mode stays when it is one of <paramref name="modes"/>
        /// (or none are given), else becomes the first of them; the default and minimum button widths are raised to at
        /// least <paramref name="width"/> (0: no width); half-width buttons get the full width when
        /// <paramref name="minimizedFullWidth"/> is set. Nothing is lowered or turned off.
        /// </summary>
        public static LayoutValues Apply(LayoutValues current, IReadOnlyList<string> modes, int width, bool minimizedFullWidth)
        {
            var result = current;
            if (modes != null && modes.Count > 0 && !Contains(modes, current.Mode)) result.Mode = modes[0];
            if (width > 0)
            {
                result.DefaultWidth = Math.Max(current.DefaultWidth, width);
                result.MinWidth = Math.Max(current.MinWidth, width);
            }
            result.MinimizedFullWidth = current.MinimizedFullWidth || minimizedFullWidth;
            return result;
        }

        private static bool Contains(IReadOnlyList<string> modes, string mode)
        {
            for (int i = 0; i < modes.Count; i++)
                if (string.Equals(modes[i], mode, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
