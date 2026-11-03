using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    [HarmonyPatch(typeof(MainButtonsRoot), "DoButtons")]
    public static class MainButtonsRoot_DoButtons_Patch
    {
        // Run after every other prefix so we can see whether one of them already took over the bar.
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(MainButtonsRoot __instance, bool __runOriginal)
        {
            StartupDiagnostics.ReportPatchesOnce();

            // Another mod (for example Custom Bottom Bar) already skipped the original and draws the bar itself.
            // When this mod has priority, that mod's patch is removed instead (CompatBridgeWorker.TakeOver).
            if (!__runOriginal)
            {
                BarRenderer.ReportGameDrawn(__instance);
                return false;
            }

            return !BarRenderer.TryDraw(__instance);
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

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D))]
    public static class Widgets_DrawAtlas_Patch
    {
        public static void Prefix(Rect rect, ref Texture2D atlas, out Color? __state) => ThemeAtlasSwap.Prefix(rect, ref atlas, out __state);

        public static void Postfix(Color? __state) => ThemeAtlasSwap.Postfix(__state);
    }

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
