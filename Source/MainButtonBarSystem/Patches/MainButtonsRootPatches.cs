using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    [HarmonyPatch(typeof(MainButtonsRoot), "DoButtons")]
    public static class MainButtonsRoot_DoButtons_Patch
    {
        /// <summary>True while this mod's prefix ran in the current call (so the postfix knows who drew).</summary>
        private static bool prefixRan;

        // Last, so other mods' prefixes run first. If one of them returns false (Custom Bottom Bar, Multiplayer's
        // spectator view), Harmony skips every later prefix that can affect the original, including this one:
        // the bar is then theirs, and the postfix below records what they show. When this mod has priority, the
        // other mod's patch is removed instead (CompatBridgeWorker.TakeOver).
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(MainButtonsRoot __instance)
        {
            prefixRan = true;
            return !BarRenderer.TryDraw(__instance);
        }

        // Postfixes always run, so this is where to observe calls in which another mod drew the bar.
        public static void Postfix(MainButtonsRoot __instance)
        {
            StartupDiagnostics.ReportPatchesOnce();
            if (!prefixRan) BarRenderer.ReportGameDrawn(__instance);
            prefixRan = false;
        }
    }

    [HarmonyPatch(typeof(MainButtonsRoot), nameof(MainButtonsRoot.HandleLowPriorityShortcuts))]
    public static class MainButtonsRoot_HandleLowPriorityShortcuts_Patch
    {
        public static void Postfix()
        {
            Hotkeys.HandleCustomHotkeys();
        }
    }

    /// <summary>
    /// Label alignment (spec 3.2) and theme label colour: while this mod draws a bar button, the game's subtle
    /// button gets the theme's label colour, and with an alignment chosen it draws everything but its text, which
    /// is then drawn at the chosen alignment. Every other use of the widget is untouched.
    /// </summary>
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonTextSubtle))]
    public static class Widgets_ButtonTextSubtle_Patch
    {
        public static void Prefix(Rect rect, ref string label, ref Color? labelColor, out string __state)
        {
            __state = null;
            var theme = ButtonPainter.ActiveTheme;
            if (theme != null) labelColor = theme.LabelColor(ButtonPainter.StateFor(rect)) ?? labelColor;
            if (ButtonPainter.ActiveAlign == LabelAlign.Default || string.IsNullOrEmpty(label)) return;
            __state = label;
            label = "";
        }

        public static void Postfix(Rect rect, Color? labelColor, string __state)
        {
            if (__state != null) ButtonPainter.DrawAlignedLabel(rect, __state, labelColor);
        }
    }

    /// <summary>
    /// Themes: while this mod draws a bar button, the game's subtle button atlas (drawn by Widgets.ButtonTextSubtle
    /// and by MainButtonWorker.DoButton for disabled buttons) is swapped for the theme's atlas of the current state.
    /// </summary>
    public static class ThemeAtlasSwap
    {
        public static void Prefix(Rect rect, ref Texture2D atlas, out Color? __state)
        {
            __state = null;
            var theme = ButtonPainter.ActiveTheme;
            if (theme == null || atlas != Widgets.ButtonSubtleAtlas) return;
            var replacement = theme.ButtonAtlas(ButtonPainter.StateFor(rect), out bool replacesTint);
            if (replacement == null) return;
            atlas = replacement;
            if (replacesTint)
            {
                __state = GUI.color;
                GUI.color = Color.white;
            }
        }

        public static void Postfix(Color? __state)
        {
            if (__state.HasValue) GUI.color = __state.Value;
        }
    }

    // Only the three-parameter overload: the two-parameter one just calls it with drawTop: true.
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D), typeof(bool))]
    public static class Widgets_DrawAtlasDrawTop_Patch
    {
        public static void Prefix(Rect rect, ref Texture2D atlas, out Color? __state) => ThemeAtlasSwap.Prefix(rect, ref atlas, out __state);

        public static void Postfix(Color? __state) => ThemeAtlasSwap.Postfix(__state);
    }

    /// <summary>Keeps the mouse wheel from zooming the map while it scrolls the bar (spec 4.10).</summary>
    [HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CameraDriverOnGUI))]
    public static class CameraDriver_CameraDriverOnGUI_Patch
    {
        public static bool Prefix()
        {
            var ev = Event.current;
            if (ev == null || ev.type != EventType.ScrollWheel) return true;
            var area = BarRenderer.ScrollCaptureRect;
            return area.width <= 0f || !area.Contains(UI.MousePositionOnUIInverted);
        }
    }
}
