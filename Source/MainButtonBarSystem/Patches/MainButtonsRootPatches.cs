using System;
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

        /// <summary>Set after this mod's bar code threw: the game draws the bar for the rest of the session.</summary>
        private static bool broken;

        // Last, so other mods' prefixes run first. If one of them returns false (Custom Bottom Bar, Multiplayer's
        // spectator view), Harmony skips every later prefix that can affect the original, including this one:
        // the bar is then theirs, and the postfix below records what they show. When this mod has priority, the
        // other mod's patch is removed instead (CompatBridgeWorker.TakeOver).
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(MainButtonsRoot __instance)
        {
            if (broken) return true;
            prefixRan = true;
            try
            {
                return !BarRenderer.TryDraw(__instance);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Break(e);
                return true;
            }
        }

        // Postfixes always run, so this is where to observe calls in which another mod drew the bar.
        public static void Postfix(MainButtonsRoot __instance)
        {
            bool ran = prefixRan;
            prefixRan = false;
            if (broken) return;
            try
            {
                StartupDiagnostics.ReportPatchesOnce();
                if (!ran) BarRenderer.ReportGameDrawn(__instance);
                CompatNotices.NoteDraw(ran);
                CompatNotices.LogOnce();
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Break(e);
            }
        }

        private static void Break(Exception e)
        {
            if (broken) return;
            broken = true;
            Log.Error(MainButtonBarSystemMod.LogPrefix + "The main button bar code failed; the game draws the bar for the rest of this session. " + e);
        }
    }

    [HarmonyPatch(typeof(MainButtonsRoot), nameof(MainButtonsRoot.HandleLowPriorityShortcuts))]
    public static class MainButtonsRoot_HandleLowPriorityShortcuts_Patch
    {
        public static void Postfix()
        {
            try
            {
                Hotkeys.HandleCustomHotkeys();
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Log.ErrorOnce(MainButtonBarSystemMod.LogPrefix + "A custom main button hotkey failed. " + e, 0x4D424256);
            }
        }
    }

    /// <summary>
    /// Label alignment (spec 3.2), theme label colour and the icons this mod draws itself: while this mod draws a
    /// bar button, the game's subtle button gets the theme's label colour, and with an alignment chosen, "icon and
    /// text" or "icon only" it draws everything but its text; the postfix then draws the label at the chosen
    /// alignment, the icon and label, or the icon alone. Every other use of the widget is untouched.
    /// </summary>
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonTextSubtle))]
    public static class Widgets_ButtonTextSubtle_Patch
    {
        /// <summary>Set after this patch's code threw: every subtle button is left to the game for the rest of the session.</summary>
        private static bool broken;

        public static void Prefix(Rect rect, ref string label, ref Color? labelColor, out string __state)
        {
            __state = null;
            if (broken) return;
            try
            {
                PrefixBody(rect, ref label, ref labelColor, ref __state);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Break(e);
            }
        }

        private static void PrefixBody(Rect rect, ref string label, ref Color? labelColor, ref string __state)
        {
            var theme = ButtonPainter.ActiveTheme;
            if (theme != null) labelColor = theme.LabelColor(ButtonPainter.StateFor(rect)) ?? labelColor;
            if (ButtonPainter.ActiveBlank)
            {
                label = "";
                return;
            }
            ButtonPainter.NoteGameLabel(label);
            bool ownIcon = ButtonPainter.ActiveIcon != null;
            if (ButtonPainter.ActiveAlign == LabelAlign.Default && !ownIcon && ButtonPainter.ActiveFont == null) return;
            // Icon only: a label put back by another mod is dropped too (the game passes "" when it draws an icon).
            if (string.IsNullOrEmpty(label) && !ownIcon) return;
            __state = label ?? "";
            label = "";
        }

        public static void Postfix(Rect rect, Color? labelColor, string __state)
        {
            if (__state == null || broken) return;
            try
            {
                if (ButtonPainter.ActiveIconOnly) ButtonPainter.DrawIconOnly(rect);
                else if (ButtonPainter.ActiveIcon != null) ButtonPainter.DrawIconAndLabel(rect, __state, labelColor);
                else ButtonPainter.DrawAlignedLabel(rect, __state, labelColor);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Break(e);
            }
        }

        private static void Break(Exception e)
        {
            if (broken) return;
            broken = true;
            Log.Error(MainButtonBarSystemMod.LogPrefix + "Drawing button labels and icons failed; alignment, theme label colours and icon modes are off for this session. " + e);
        }
    }

    /// <summary>
    /// Themes: while this mod draws a bar button, the game's subtle button atlas (drawn by Widgets.ButtonTextSubtle
    /// and by MainButtonWorker.DoButton for disabled buttons) is swapped for the theme's atlas of the current state.
    /// The button's picture, if the theme gives it one, is drawn first, so the atlas frames it (in every state, the
    /// game's own atlas too when the theme has none).
    /// </summary>
    public static class ThemeAtlasSwap
    {
        /// <summary>Set after this patch's code threw: every atlas is left to the game for the rest of the session.</summary>
        private static bool broken;

        public static void Prefix(Rect rect, ref Texture2D atlas, out Color? __state)
        {
            __state = null;
            if (broken) return;
            try
            {
                PrefixBody(rect, ref atlas, ref __state);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                broken = true;
                Log.Error(MainButtonBarSystemMod.LogPrefix + "Drawing theme button backgrounds failed; themes keep the game's button background for this session. " + e);
            }
        }

        private static void PrefixBody(Rect rect, ref Texture2D atlas, ref Color? __state)
        {
            var theme = ButtonPainter.ActiveTheme;
            if (theme == null || atlas != Widgets.ButtonSubtleAtlas) return;
            var button = ButtonPainter.ActiveButton;
            if (button != null && !ButtonPainter.ActiveButtonImageDrawn)
            {
                ButtonPainter.ActiveButtonImageDrawn = true;
                var image = theme.ButtonImage(button);
                if (image != null) theme.DrawButtonImage(rect, image);
            }
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
            try
            {
                var area = BarRenderer.ScrollCaptureRect;
                return area.width <= 0f || !area.Contains(UI.MousePositionOnUIInverted);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Log.ErrorOnce(MainButtonBarSystemMod.LogPrefix + "Checking the bar's scroll area failed; the wheel zooms the map as usual. " + e, 0x4D424257);
                return true;
            }
        }
    }
}
