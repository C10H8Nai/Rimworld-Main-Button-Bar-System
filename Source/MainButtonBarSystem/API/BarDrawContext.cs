using System.Collections.Generic;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;

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
            Appearance.DrawButton(button, rect);
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
}
