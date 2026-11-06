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
