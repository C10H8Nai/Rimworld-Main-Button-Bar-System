using System.Collections.Generic;
using System.Linq;
using MainButtonBarSystem.Core;
using RimWorld;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>Shared rules the bar and the settings window both use to read a <see cref="BarConfig"/>.</summary>
    public static class BarModel
    {
        public const string MenuDefName = "Menu";

        internal static readonly HarmonyLib.AccessTools.FieldRef<MainButtonsRoot, List<MainButtonDef>> AllButtonsInOrder =
            SafeAccess.FieldRef<MainButtonsRoot, List<MainButtonDef>>("allButtonsInOrder");

        private static List<string> currentNames;

        /// <summary>The game's list <see cref="currentNames"/> was built from; empty when it came from the baseline.</summary>
        private static readonly List<MainButtonDef> currentSource = new List<MainButtonDef>();

        /// <summary>
        /// Every MainButtonDef in the game's own order right now: in a game, the order MainButtonsRoot draws (so mods
        /// that reorder or add buttons at runtime are followed while the player has not set an order); at the main
        /// menu, the baseline order. Defs missing from that list follow in def order.
        /// </summary>
        public static List<string> CurrentNames
        {
            get
            {
                var live = LiveOrder();
                if (currentNames != null && SameSource(live)) return currentNames;

                var names = new List<string>();
                var known = new HashSet<string>();
                currentSource.Clear();
                if (live != null)
                {
                    currentSource.AddRange(live);
                    foreach (var def in live)
                        if (def != null && known.Add(def.defName)) names.Add(def.defName);
                }
                else
                {
                    foreach (var b in ButtonBaseline.All)
                        if (known.Add(b.DefName)) names.Add(b.DefName);
                }
                foreach (var def in DefDatabase<MainButtonDef>.AllDefs.OrderBy(d => d.order))
                    if (known.Add(def.defName)) names.Add(def.defName);
                currentNames = names;
                return currentNames;
            }
        }

        private static readonly System.WeakReference<MainButtonsRoot> ignoredRoot = new System.WeakReference<MainButtonsRoot>(null);

        /// <summary>
        /// The order of this root's list is not the game's own: a mod rewrote it before a compatibility bridge took
        /// the bar over (TooManyButtons with this mod first). The baseline order is used for it instead; the next
        /// game builds a fresh list from the restored defs.
        /// </summary>
        internal static void IgnoreLiveOrderOf(MainButtonsRoot root)
        {
            ignoredRoot.SetTarget(root);
            currentNames = null;
            currentSource.Clear();
        }

        private static List<MainButtonDef> LiveOrder()
        {
            if (AllButtonsInOrder == null || Current.ProgramState != ProgramState.Playing) return null;
            var root = (Find.UIRoot as UIRoot_Play)?.mainButtonsRoot;
            if (root == null || ignoredRoot.TryGetTarget(out var ignored) && ReferenceEquals(root, ignored)) return null;
            var list = AllButtonsInOrder(root);
            return list == null || list.Count == 0 ? null : list;
        }

        /// <summary>Whether the cached names were built from exactly this list, in this order (no allocation).</summary>
        private static bool SameSource(List<MainButtonDef> live)
        {
            if (live == null) return currentSource.Count == 0;
            if (live.Count != currentSource.Count) return false;
            for (int i = 0; i < live.Count; i++)
                if (!ReferenceEquals(live[i], currentSource[i])) return false;
            return true;
        }

        internal static void ResetCache()
        {
            currentNames = null;
            currentSource.Clear();
            orderCache = null;
            orderConfig = null;
        }

        public static bool IsLocked(BarConfig config, string defName) => config.MenuLocked && defName == MenuDefName;

        public static PinPosition PinOf(BarConfig config, string defName) =>
            IsLocked(config, defName) ? PinPosition.Right : config.Get(defName).pin;

        private static BarConfig orderConfig;
        private static List<string> orderNames;
        private static int orderDefaultsVersion;
        private static List<string> orderCache;

        /// <summary>
        /// Order on screen, left to right: saved order merged with current buttons, then pins applied. Do not modify the
        /// result. Stored configs only change by being replaced, so their result is kept until the config, the game's
        /// button list or the declared defaults change; the settings window's working copy changes in place and is
        /// computed every time.
        /// </summary>
        public static List<string> EffectiveOrder(BarConfig config)
        {
            var names = CurrentNames;
            bool cacheable = !ReferenceEquals(config, BarConfigManager.Preview);
            if (cacheable && orderCache != null && ReferenceEquals(config, orderConfig) && ReferenceEquals(names, orderNames) &&
                orderDefaultsVersion == ButtonDefaults.Version)
                return orderCache;

            var result = ButtonOrder.Effective(ButtonOrder.Merge(config.order, names, MenuDefName), n => PinOf(config, n));
            if (cacheable)
            {
                orderConfig = config;
                orderNames = names;
                orderDefaultsVersion = ButtonDefaults.Version;
                orderCache = result;
            }
            return result;
        }

        /// <summary>Whether a button can go into the drawer in the current mode.</summary>
        public static bool DrawerApplies(BarConfig config, string defName) =>
            (config.DisplayModeDef?.usesDrawer ?? false) && PinOf(config, defName) == PinPosition.None && !IsLocked(config, defName);

        /// <summary>Buttons the game hides on purpose (buttonVisible=false); listed separately (spec 4.11).</summary>
        public static bool OriginallyHidden(string defName) => ButtonBaseline.Get(defName) is { ButtonVisible: false };

        /// <summary>True when the button should be on the bar (or in the drawer) right now.</summary>
        public static bool ShouldShow(BarConfig config, MainButtonDef def)
        {
            var o = config.Get(def.defName);
            if (o.hidden && !IsLocked(config, def.defName)) return false;
            if (o.forceShow) return true;
            return SafeVisible(def);
        }

        /// <summary>Worker.Visible, tolerating workers that assume a map or game exists.</summary>
        public static bool SafeVisible(MainButtonDef def)
        {
            try
            {
                return def.Worker != null && def.Worker.Visible;
            }
            catch
            {
                return false;
            }
        }

        public static IEnumerable<BarDisplayModeDef> DisplayModes => DefDatabase<BarDisplayModeDef>.AllDefs.OrderBy(d => d.displayOrder);

        public static IEnumerable<BarAppearanceDef> Appearances => DefDatabase<BarAppearanceDef>.AllDefs.OrderBy(d => d.displayOrder);
    }
}
