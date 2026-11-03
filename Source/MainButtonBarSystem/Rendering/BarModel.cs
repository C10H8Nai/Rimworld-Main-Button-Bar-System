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

        private static List<string> currentNames;

        /// <summary>Every MainButtonDef in the game's own order (baseline order, then anything added later).</summary>
        public static List<string> CurrentNames
        {
            get
            {
                if (currentNames == null)
                {
                    var names = ButtonBaseline.All.Select(b => b.DefName).ToList();
                    var known = new HashSet<string>(names);
                    names.AddRange(DefDatabase<MainButtonDef>.AllDefs.OrderBy(d => d.order).Select(d => d.defName).Where(n => !known.Contains(n)));
                    currentNames = names;
                }
                return currentNames;
            }
        }

        public static bool IsLocked(BarConfig config, string defName) => config.menuLocked && defName == MenuDefName;

        public static PinPosition PinOf(BarConfig config, string defName) =>
            IsLocked(config, defName) ? PinPosition.Right : config.Get(defName).pin;

        /// <summary>Order on screen, left to right: saved order merged with current buttons, then pins applied.</summary>
        public static List<string> EffectiveOrder(BarConfig config) =>
            ButtonOrder.Effective(ButtonOrder.Merge(config.order, CurrentNames, MenuDefName), n => PinOf(config, n));

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
