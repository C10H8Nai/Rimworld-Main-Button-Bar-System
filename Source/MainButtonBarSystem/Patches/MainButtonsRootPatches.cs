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
            // Priority between such mods is a later stage; for now never draw on top of them.
            if (!__runOriginal) return false;

            return !BarRenderer.TryDraw(__instance);
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
