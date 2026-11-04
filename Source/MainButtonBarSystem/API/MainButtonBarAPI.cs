using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Slot "query": read-only information for other mods, for example to place something just above the bar
    /// (spec 3.8). Values describe the most recent bar draw, whether this mod or the game drew it.
    /// </summary>
    public static class MainButtonBarAPI
    {
        /// <summary>
        /// Version of the public API (the slots in docs/API.md and this class). The minor number grows with
        /// additions; the major number only changes when something is removed or changes meaning, and removed
        /// members stay one release marked [Obsolete] first. Check it before using newer members. A property, not
        /// a const, so callers read the installed version instead of the one they compiled against.
        /// </summary>
        public static string ApiVersion => "1.1";

        private static readonly List<MainButtonDef> onBar = new List<MainButtonDef>();
        private static readonly List<MainButtonDef> inDrawer = new List<MainButtonDef>();
        private static Rect barRect;
        private static int signature;

        /// <summary>Buttons on the bar, left to right, including ones scrolled out of view.</summary>
        public static IReadOnlyList<MainButtonDef> ButtonsOnBar => onBar;

        /// <summary>Buttons reachable only through the "More" drawer.</summary>
        public static IReadOnlyList<MainButtonDef> ButtonsInDrawer => inDrawer;

        /// <summary>Area the bar occupies, in GUI coordinates (origin top-left, UI scale applied).</summary>
        public static Rect BarRect => barRect;

        public static float BarHeight => barRect.height;

        /// <summary>True while this mod draws the bar; false when the game or another mod does.</summary>
        public static bool DrawnByThisMod { get; private set; }

        /// <summary>Raised after a draw in which the set or order of buttons, or the bar area, changed.</summary>
        public static event Action LayoutChanged;

        /// <summary>Opens the settings window (for the current save in a game, the template otherwise).</summary>
        public static void OpenSettings() => Find.WindowStack.Add(new Dialog_MainButtonBar());

        internal static void Report(IReadOnlyList<MainButtonDef> bar, IReadOnlyList<MainButtonDef> drawer, Rect rect, bool ours)
        {
            int sig = 17;
            foreach (var d in bar) sig = sig * 31 + d.shortHash;
            sig = sig * 31 + 7;
            foreach (var d in drawer) sig = sig * 31 + d.shortHash;
            sig = sig * 31 + rect.GetHashCode();
            sig = sig * 31 + (ours ? 1 : 0);

            DrawnByThisMod = ours;
            if (sig == signature) return;
            signature = sig;

            onBar.Clear();
            onBar.AddRange(bar);
            inDrawer.Clear();
            inDrawer.AddRange(drawer);
            barRect = rect;

            try
            {
                LayoutChanged?.Invoke();
            }
            catch (Exception e)
            {
                Log.ErrorOnce(MainButtonBarSystemMod.LogPrefix + "A LayoutChanged listener threw: " + e, 0x4D424253);
            }
        }
    }
}
