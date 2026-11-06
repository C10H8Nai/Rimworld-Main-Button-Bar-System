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

        /// <summary>Effective settings: the player's changes over what the button inherits.</summary>
        public ButtonSettings Settings;

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
            ButtonGuard.Draw(button, rect, Appearance, drawer: false);
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
    /// Draws one button through the appearance, so that one failing button cannot take the bar down. A failure on an
    /// input event (a click opening the button's window, for example) is the button's own: it is logged once and the
    /// event skipped. A failure on Layout or Repaint is told apart by drawing the button again plainly, through its
    /// own MainButtonWorker.DoButton as the game would: if that throws too, the button's own worker is at fault
    /// (logged once, skipped); if it works, this mod's drawing of that button is at fault (a theme worker, an icon
    /// setting another mod's patch does not expect), and from then on that button is drawn the game's way with this
    /// appearance, so a broken theme costs one exception per button, not one per button and event.
    /// </summary>
    internal static class ButtonGuard
    {
        /// <summary>Buttons whose own worker threw; not drawn again plainly, so a broken one costs one exception per event.</summary>
        private static readonly HashSet<string> ownFailures = new HashSet<string>();

        /// <summary>
        /// Buttons whose own worker threw and was logged: one error per button, whether it threw while being drawn or
        /// while handling input (a worker that throws in DoButton throws in both).
        /// </summary>
        private static readonly HashSet<string> logged = new HashSet<string>();

        /// <summary>Appearance and button pairs that only draw the game's way.</summary>
        private static readonly HashSet<(BarAppearanceWorker, MainButtonDef)> plain = new HashSet<(BarAppearanceWorker, MainButtonDef)>();

        private static bool appearanceFailureLogged;

        public static void Draw(BarButton button, Rect rect, BarAppearanceWorker appearance, bool drawer)
        {
            var def = button.Def;
            if (plain.Count > 0 && plain.Contains((appearance, def)))
            {
                DrawPlain(button, rect);
                return;
            }

            var type = Event.current.type;
            try
            {
                if (drawer) appearance.DrawDrawerButton(button, rect);
                else appearance.DrawButton(button, rect);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                ResetGui();
                string key = def?.defName ?? "?";
                if (type != EventType.Layout && type != EventType.Repaint)
                {
                    if (logged.Add(key)) LogOwn(def, key, e, "while handling input");
                    return;
                }
                if (ownFailures.Contains(key)) return;

                try
                {
                    def?.Worker?.DoButton(rect);
                }
                catch (Exception own) when (!(own is ExitGUIException))
                {
                    ResetGui();
                    ownFailures.Add(key);
                    if (logged.Add(key)) LogOwn(def, key, own, "while being drawn");
                    return;
                }

                plain.Add((appearance, def));
                string theme = appearance?.def?.LabelCap.ToString() ?? "?";
                if (!appearanceFailureLogged)
                {
                    appearanceFailureLogged = true;
                    Log.Error(MainButtonBarSystemMod.LogPrefix + "Main button " + key + " could not be drawn with the appearance '" + theme +
                              "' and this mod's button settings; it, and any other button failing the same way, is drawn the game's way from now on. " + e);
                }
                else if (Prefs.DevMode)
                {
                    Log.Message(MainButtonBarSystemMod.LogPrefix + "Main button " + key + " is also drawn the game's way with the appearance '" + theme + "': " + e.Message);
                }
            }
        }

        private static void DrawPlain(BarButton button, Rect rect)
        {
            try
            {
                button.Def.Worker.DoButton(rect);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                ResetGui();
                string key = button.Def?.defName ?? "?";
                ownFailures.Add(key);
                if (logged.Add(key)) LogOwn(button.Def, key, e, "while being drawn");
            }
        }

        private static void LogOwn(MainButtonDef def, string key, Exception e, string when)
        {
            string mod = def?.modContentPack?.Name ?? "?";
            Log.Error(MainButtonBarSystemMod.LogPrefix + "Main button " + key + " (from " + mod + ") threw " + when + "; the rest of the bar keeps working. " + e);
        }

        private static void ResetGui()
        {
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
