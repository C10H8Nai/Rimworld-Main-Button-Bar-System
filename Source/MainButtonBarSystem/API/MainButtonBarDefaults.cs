using System.Collections.Generic;
using MainButtonBarSystem.Core;
using RimWorld;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Slot "button defaults": another mod declares how its own button should start out, in XML only
    /// (spec 3.8). Players can still change every value; these only replace "not changed".
    /// <code>
    /// &lt;MainButtonDef&gt;
    ///   &lt;defName&gt;MyTab&lt;/defName&gt;
    ///   &lt;modExtensions&gt;
    ///     &lt;li Class="MainButtonBarSystem.MainButtonBarDefaults"&gt;
    ///       &lt;inDrawer&gt;true&lt;/inDrawer&gt;
    ///     &lt;/li&gt;
    ///   &lt;/modExtensions&gt;
    /// &lt;/MainButtonDef&gt;
    /// </code>
    /// </summary>
    public class MainButtonBarDefaults : DefModExtension
    {
        public bool hidden;
        public bool inDrawer;
        public int width;
        public PinPosition pin;
        public IconMode iconMode;

        /// <summary>Texture path of a built-in icon, as for MainButtonDef.iconPath.</summary>
        public string icon;

        public ButtonSettings ToSettings(string defName) =>
            new ButtonSettings(defName, hidden, inDrawer, null, System.Math.Max(0, width), pin, false, iconMode, icon);
    }

    /// <summary>Lookup of the defaults that mods declared on their buttons.</summary>
    public static class ButtonDefaults
    {
        private static Dictionary<string, ButtonSettings> byDefName;

        /// <summary>Changes whenever the declared or runtime defaults may have changed, for caches built from them.</summary>
        public static int Version { get; private set; }

        /// <summary>
        /// What a button inherits before the player's changes: runtime defaults over its mod's XML defaults over the
        /// game's own (not hidden, not in the drawer, following the bar).
        /// </summary>
        public static ButtonSettings Inherited(string defName)
        {
            if (byDefName == null) Build();
            return defName != null && byDefName.TryGetValue(defName, out var s) ? s : ButtonSettings.Plain(defName);
        }

        /// <summary>True when some declared default changes the bar even in vanilla display mode.</summary>
        public static bool AnyChangesVanillaBar
        {
            get
            {
                if (byDefName == null) Build();
                foreach (var o in byDefName.Values)
                {
                    if (o.hidden || o.pin != PinPosition.None || o.iconMode != IconMode.Default || o.icon != null || o.width > 0)
                        return true;
                }
                return false;
            }
        }

        private static readonly Dictionary<string, ButtonOverride> runtime = new Dictionary<string, ButtonOverride>();

        /// <summary>
        /// A default set while the game runs, for example by a compatibility bridge (TooManyButtons' own button
        /// starts hidden when this mod has priority, spec 4.14). Its changed fields win over XML defaults; fields left
        /// "not changed" keep the XML default.
        /// </summary>
        public static void SetRuntimeDefault(ButtonOverride defaults)
        {
            runtime[defaults.defName] = defaults;
            byDefName = null;
            Version++;
        }

        /// <summary>Rebuild from the (new) defs on next use; runtime defaults are kept.</summary>
        internal static void ResetCache()
        {
            byDefName = null;
            Version++;
        }

        private static void Build()
        {
            byDefName = new Dictionary<string, ButtonSettings>();
            foreach (var def in DefDatabase<MainButtonDef>.AllDefs)
            {
                var ext = def.GetModExtension<MainButtonBarDefaults>();
                if (ext != null) byDefName[def.defName] = ext.ToSettings(def.defName);
            }
            foreach (var pair in runtime)
            {
                var below = byDefName.TryGetValue(pair.Key, out var xml) ? xml : ButtonSettings.Plain(pair.Key);
                byDefName[pair.Key] = pair.Value.Over(below);
            }
        }
    }
}
