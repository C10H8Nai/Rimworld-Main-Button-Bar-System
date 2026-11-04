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

        public ButtonOverride ToOverride(string defName) => new ButtonOverride(defName)
        {
            hidden = hidden,
            inDrawer = inDrawer,
            width = width,
            pin = pin,
            iconMode = iconMode,
            icon = icon,
        };
    }

    /// <summary>Lookup of the defaults that mods declared on their buttons.</summary>
    public static class ButtonDefaults
    {
        private static Dictionary<string, ButtonOverride> byDefName;

        /// <summary>Defaults for a button, or null when its mod declared none.</summary>
        public static ButtonOverride For(string defName)
        {
            if (byDefName == null) Build();
            return defName != null && byDefName.TryGetValue(defName, out var o) ? o : null;
        }

        /// <summary>True when some declared default changes the bar even in vanilla display mode.</summary>
        public static bool AnyChangesVanillaBar
        {
            get
            {
                if (byDefName == null) Build();
                foreach (var o in byDefName.Values)
                {
                    if (o.hidden || o.pin != PinPosition.None || o.iconMode != IconMode.Default || !o.icon.NullOrEmpty())
                        return true;
                }
                return false;
            }
        }

        private static readonly Dictionary<string, ButtonOverride> runtime = new Dictionary<string, ButtonOverride>();

        /// <summary>
        /// A default set while the game runs, for example by a compatibility bridge (TooManyButtons' own button
        /// starts hidden when this mod has priority, spec 4.14). It wins over XML defaults.
        /// </summary>
        public static void SetRuntimeDefault(ButtonOverride defaults)
        {
            runtime[defaults.defName] = defaults;
            byDefName = null;
        }

        private static void Build()
        {
            byDefName = new Dictionary<string, ButtonOverride>();
            foreach (var def in DefDatabase<MainButtonDef>.AllDefs)
            {
                var ext = def.GetModExtension<MainButtonBarDefaults>();
                if (ext != null) byDefName[def.defName] = ext.ToOverride(def.defName);
            }
            foreach (var pair in runtime) byDefName[pair.Key] = pair.Value;
        }
    }
}
