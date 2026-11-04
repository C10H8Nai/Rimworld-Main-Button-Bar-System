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
    }
}
