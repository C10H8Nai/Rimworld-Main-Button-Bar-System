using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Settings shared by every save. The bar arrangement itself lives in each save (BarConfig) and in the
    /// template file, not here.
    /// </summary>
    public class MainButtonBarSystemSettings : ModSettings
    {
        /// <summary>
        /// Draw the bar with this mod even when nothing is customised. For comparing against the game's own
        /// drawing while testing; off by default so an untouched install changes nothing.
        /// </summary>
        public bool alwaysDrawBar;

        /// <summary>The settings window's Simple / Advanced switch; remembered for every save.</summary>
        public bool showAdvanced;

        /// <summary>CompatBridgeDef defName -> who draws the bar when that mod is active.</summary>
        public Dictionary<string, CompatPriority> compatPriority = new Dictionary<string, CompatPriority>();

        /// <summary>
        /// Hotkeys for buttons the game gave no KeyBindingDef (spec 3.6). Buttons that have one are stored in the
        /// game's own KeyPrefs.xml instead, so Options > Keyboard shows the same key.
        /// </summary>
        public Dictionary<string, KeyCode> customHotkeys = new Dictionary<string, KeyCode>();

        public CompatPriority PriorityFor(CompatBridgeDef def) =>
            compatPriority.TryGetValue(def.defName, out var p) ? p : def.defaultPriority;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref alwaysDrawBar, "alwaysDrawBar");
            Scribe_Values.Look(ref showAdvanced, "showAdvanced");
            Scribe_Collections.Look(ref compatPriority, "compatPriority", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref customHotkeys, "customHotkeys", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                compatPriority ??= new Dictionary<string, CompatPriority>();
                customHotkeys ??= new Dictionary<string, KeyCode>();
            }
        }
    }
}
