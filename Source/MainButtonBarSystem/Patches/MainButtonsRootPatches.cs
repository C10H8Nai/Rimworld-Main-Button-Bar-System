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
    /// Label alignment (spec 3.2): while this mod draws a bar button with an alignment chosen, the game's subtle
    /// button draws everything but its text, and the text is drawn at the chosen alignment instead. Every other
    /// use of the widget is untouched.
    /// </summary>
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonTextSubtle))]
    public static class Widgets_ButtonTextSubtle_LabelAlign_Patch
    {
        public static void Prefix(ref string label, out string __state)
        {
            __state = null;
            if (ButtonPainter.ActiveAlign == LabelAlign.Default || string.IsNullOrEmpty(label)) return;
            __state = label;
            label = "";
        }

        public static void Postfix(Rect rect, Color? labelColor, string __state)
        {
            if (__state != null) ButtonPainter.DrawAlignedLabel(rect, __state, labelColor);
        }
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
