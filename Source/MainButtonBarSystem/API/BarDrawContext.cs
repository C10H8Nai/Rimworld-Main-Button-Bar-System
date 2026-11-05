using System;
using System.Collections.Generic;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// One button as a display mode should place it: the user's effective settings are already applied
    /// (hidden buttons are left out, renames are on the def, pins and drawer flags resolved).
    /// </summary>
    public sealed class BarButton
    {
        public MainButtonDef Def;

        /// <summary>Effective settings: the user's override, else the button's XML defaults, else empty.</summary>
        public ButtonOverride Settings;

        public PinPosition Pin;
        public bool InDrawer;
        public bool Minimized;

        /// <summary>Per-button width in pixels, or null to follow the bar.</summary>
        public int? Width;

        public string Key => Def.defName;
    }

    /// <summary>
    /// Everything a display mode needs to draw the bar for one GUI event. Display modes place buttons and call
    /// back into the appearance to draw them, so any display mode works with any appearance.
    /// </summary>
    public sealed class BarDrawContext
    {
        private readonly List<MainButtonDef> onBar = new List<MainButtonDef>();
        private readonly List<MainButtonDef> inDrawer = new List<MainButtonDef>();

        public BarConfig Config { get; internal set; }

        /// <summary>Buttons to show, left to right on screen (pins already partitioned to the ends).</summary>
        public IReadOnlyList<BarButton> Buttons { get; internal set; }

        /// <summary>Screen area of the bar in GUI coordinates (bottom of the screen, full width, one button high).</summary>
        public Rect BarRect { get; internal set; }

        public BarAppearanceWorker Appearance { get; internal set; }

        internal IReadOnlyList<MainButtonDef> ReportedOnBar => onBar;
        internal IReadOnlyList<MainButtonDef> ReportedInDrawer => inDrawer;
        internal Rect ReportedBarRect { get; private set; }
        internal Rect ScrollCaptureRect { get; private set; }

        internal void Reset()
        {
            onBar.Clear();
            inDrawer.Clear();
            ReportedBarRect = BarRect;
            ScrollCaptureRect = Rect.zero;
        }

        /// <summary>Draws one button through the active appearance and records it as on the bar.</summary>
        public void DrawButton(BarButton button, Rect rect)
        {
            onBar.Add(button.Def);
            try
            {
                Appearance.DrawButton(button, rect);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                // Rethrown when the button draws fine on its own: then the bar goes back to the game (BarRenderer).
                var appearanceFailure = ButtonGuard.Recover(button, rect, e, Appearance);
                if (appearanceFailure != null) throw appearanceFailure;
            }
        }

        /// <summary>Records a button that is reachable through a drawer instead of the bar.</summary>
        public void ReportInDrawer(BarButton button) => inDrawer.Add(button.Def);

        /// <summary>
        /// Records a button that is on the bar but not drawn this event (scrolled out of view), so queries still
        /// count it as on the bar.
        /// </summary>
        public void ReportOnBarNotDrawn(BarButton button) => onBar.Add(button.Def);

        /// <summary>Lets a display mode that is taller or shorter than one row report its real area.</summary>
        public void ReportBarRect(Rect rect) => ReportedBarRect = rect;

        /// <summary>While the mouse is inside this area, the mouse wheel scrolls the bar instead of zooming the map.</summary>
        public void CaptureScrollWheel(Rect rect) => ScrollCaptureRect = rect;
    }

    /// <summary>
    /// Tells apart the two ways drawing a button can fail. The button is drawn again plainly, through its own
    /// MainButtonWorker.DoButton as the game would: if that throws too, the button's own worker is at fault, so it is
    /// logged once and skipped while the rest of the bar keeps working. If the plain draw works, the failure was in
    /// the appearance (a theme worker, or this mod's drawing around DoButton), which would hit every button on every
    /// event: the caller gets an exception to hand the bar back to the game for the session.
    /// </summary>
    internal static class ButtonGuard
    {
        /// <summary>Buttons whose own worker threw; not drawn again plainly, so a broken one costs one exception per event.</summary>
        private static readonly HashSet<string> ownFailures = new HashSet<string>();

        public static Exception Recover(BarButton button, Rect rect, Exception e, BarAppearanceWorker appearance)
        {
            ResetGui();
            var def = button?.Def;
            string key = def?.defName ?? "?";
            if (ownFailures.Contains(key)) return null;

            try
            {
                def?.Worker?.DoButton(rect);
            }
            catch (Exception plain) when (!(plain is ExitGUIException))
            {
                ResetGui();
                ownFailures.Add(key);
                string mod = def?.modContentPack?.Name ?? "?";
                Log.Error(MainButtonBarSystemMod.LogPrefix + "Main button " + key + " (from " + mod + ") threw while being drawn; the rest of the bar keeps working. " + plain);
                return null;
            }

            string theme = appearance?.def?.LabelCap.ToString() ?? "?";
            return new InvalidOperationException("Drawing main button " + key + " with the appearance '" + theme + "' failed, while the game draws it fine", e);
        }

        private static void ResetGui()
        {
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
