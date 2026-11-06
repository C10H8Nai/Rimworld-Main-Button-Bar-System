using System;
using System.Globalization;

namespace MainButtonBarSystem.Core
{
    /// <summary>Colour codes for the custom look's colour box: "#RRGGBB" (the "#" is optional), channels 0 to 255.</summary>
    public static class ColorHex
    {
        public static string Format(int r, int g, int b) => "#" + Clamp(r).ToString("X2") + Clamp(g).ToString("X2") + Clamp(b).ToString("X2");

        /// <summary>Reads "#RRGGBB" or "RRGGBB", any case, spaces around ignored. False for anything else.</summary>
        public static bool TryParse(string text, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (text == null) return false;
            var s = text.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal)) s = s.Substring(1);
            if (s.Length != 6) return false;
            return Channel(s, 0, out r) && Channel(s, 2, out g) && Channel(s, 4, out b);
        }

        private static bool Channel(string s, int at, out int value) =>
            int.TryParse(s.Substring(at, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);

        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }
}
