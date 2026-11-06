using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    internal static class UIUtil
    {
        private static MainButtonBarTuningDef.SettingsWindowLayout L => MainButtonBarTuningDef.Current.settingsWindow;

        /// <summary>A button width that fits its label in the current language, never below <paramref name="minimum"/>.</summary>
        public static float FitButton(float minimum, string label)
        {
            var font = Text.Font;
            Text.Font = GameFont.Small;
            float width = Text.CalcSize(label).x + L.buttonTextPadding * 2f;
            Text.Font = font;
            return Mathf.Max(minimum, width);
        }

        /// <summary>
        /// The font for a label in a button of this width: small, or tiny when the label does not fit in the small font
        /// (a long translation in a button whose width the layout fixes).
        /// </summary>
        public static GameFont ButtonFont(string label, float width)
        {
            var font = Text.Font;
            Text.Font = GameFont.Small;
            bool fits = Text.CalcSize(label).x + L.buttonTextPadding * 2f <= width;
            Text.Font = font;
            return fits ? GameFont.Small : GameFont.Tiny;
        }

        /// <summary>
        /// Translates <paramref name="key"/> with the arguments inserted exactly as given. The game's formatter capitalises
        /// the first letter after ':', '!', '?' or a line break (GenText.CapitalizeSentences), which turns names written
        /// after a colon, such as the author "boblin00215" in Japanese "作者: {0}", a file name or a package id, into
        /// "Boblin00215". Each argument goes through the formatter as a placeholder that has no letters, then replaces it.
        /// </summary>
        public static string TranslateKeepCase(this string key, params object[] args)
        {
            var placeholders = new NamedArgument[args.Length];
            for (int i = 0; i < args.Length; i++) placeholders[i] = Placeholder(i);
            string text = key.Translate(placeholders).Resolve();
            for (int i = 0; i < args.Length; i++) text = text.Replace(Placeholder(i), args[i]?.ToString() ?? "");
            return text;
        }

        // Private-use characters around a digit: not a letter, so the formatter capitalises nothing inside an argument.
        private static string Placeholder(int index) => "" + index + "";

        /// <summary>The game's text button, with the font from <see cref="ButtonFont"/>.</summary>
        public static bool ButtonText(Rect rect, string label, bool active = true)
        {
            var font = Text.Font;
            Text.Font = ButtonFont(label, rect.width);
            bool clicked = Widgets.ButtonText(rect, label, true, true, active);
            Text.Font = font;
            return clicked;
        }
    }
}
