using System;
using System.Collections.Generic;
using System.Linq;
using MainButtonBarSystem.Core;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>Parts of an arrangement a player can import on their own.</summary>
    [Flags]
    public enum ArrangementParts
    {
        None = 0,

        /// <summary>Display mode, order, hidden, drawer, pins, forced visibility, menu lock.</summary>
        Layout = 1,

        /// <summary>Bar and button widths, gap, fill screen, label alignment, More button, arrows, drawer grid, wheel speed.</summary>
        Sizes = 2,

        /// <summary>Custom button names.</summary>
        Names = 4,

        /// <summary>Text / icon display and chosen icons.</summary>
        Icons = 8,

        /// <summary>The theme.</summary>
        Theme = 16,

        All = Layout | Sizes | Names | Icons | Theme,
    }

    /// <summary>
    /// Imports chosen parts of a shared arrangement into the player's own (spec 3.9). Settings of buttons the
    /// exporting game did not have are kept: the arrangement has no opinion about them. A button the exporting game
    /// had but left unchanged takes its default settings for the chosen parts.
    /// </summary>
    public static class ArrangementMerge
    {
        public static BarConfig Merge(BarConfig current, SharedArrangement shared, ArrangementParts parts)
        {
            var result = current.Clone();
            var imported = shared.config;
            var match = shared.Match();

            if ((parts & ArrangementParts.Layout) != 0)
            {
                result.displayMode = imported.displayMode;
                result.order = imported.order?.ToList();
                result.menuLocked = imported.menuLocked;
            }
            if ((parts & ArrangementParts.Sizes) != 0)
            {
                result.defaultWidth = imported.defaultWidth;
                result.minWidth = imported.minWidth;
                result.gap = imported.gap;
                result.fillScreen = imported.fillScreen;
                result.labelAlign = imported.labelAlign;
                result.moreButtonWidth = imported.moreButtonWidth;
                result.arrowWidth = imported.arrowWidth;
                result.wheelScrollPixels = imported.wheelScrollPixels;
                result.drawerColumns = imported.drawerColumns;
                result.drawerCellWidth = imported.drawerCellWidth;
            }
            if ((parts & ArrangementParts.Icons) != 0) result.iconMode = imported.iconMode;
            if ((parts & ArrangementParts.Theme) != 0) result.appearance = imported.appearance;

            if ((parts & (ArrangementParts.Layout | ArrangementParts.Sizes | ArrangementParts.Names | ArrangementParts.Icons)) != 0)
            {
                var theirs = imported.overrides.Where(o => o?.defName != null).GroupBy(o => o.defName).ToDictionary(g => g.Key, g => g.Last());
                var names = new HashSet<string>(theirs.Keys);
                names.UnionWith(current.overrides.Where(o => o?.defName != null).Select(o => o.defName));
                foreach (var defName in names)
                {
                    if (!match.KnownToThem(defName)) continue;
                    // A button the file does not list follows what it inherits, as it did for them.
                    var source = theirs.TryGetValue(defName, out var o) ? o : NothingChanged;
                    Copy(source, result.GetOrCreate(defName), parts);
                }
            }

            result.Prune();
            result.formatVersion = BarConfig.CurrentFormatVersion;
            return result;
        }

        private static readonly ButtonOverride NothingChanged = new ButtonOverride();

        private static void Copy(ButtonOverride from, ButtonOverride to, ArrangementParts parts)
        {
            if ((parts & ArrangementParts.Layout) != 0)
            {
                to.hidden = from.hidden;
                to.inDrawer = from.inDrawer;
                to.pin = from.pin;
                to.forceShow = from.forceShow;
            }
            if ((parts & ArrangementParts.Sizes) != 0) to.width = from.width;
            if ((parts & ArrangementParts.Names) != 0) to.label = from.label;
            if ((parts & ArrangementParts.Icons) != 0)
            {
                to.iconMode = from.iconMode;
                to.icon = from.icon;
            }
        }

        /// <summary>Which parts the arrangement actually sets, so the import window can grey out the rest.</summary>
        public static ArrangementParts PartsSet(BarConfig c)
        {
            var parts = ArrangementParts.None;
            var o = c.overrides.Where(x => x != null).ToList();
            if ((c.displayMode ?? BarConfig.VanillaMode) != BarConfig.VanillaMode || c.order != null || c.menuLocked != DefaultableBool.Default ||
                o.Any(x => x.hidden != DefaultableBool.Default || x.inDrawer != DefaultableBool.Default || x.pin != ButtonPin.Default || x.forceShow != DefaultableBool.Default))
                parts |= ArrangementParts.Layout;
            if (c.defaultWidth >= 0 || c.minWidth >= 0 || c.gap >= 0 || c.fillScreen != DefaultableBool.Default || c.labelAlign != LabelAlign.Default ||
                c.moreButtonWidth >= 0 || c.arrowWidth >= 0 || c.wheelScrollPixels >= 0f || c.drawerColumns >= 0 || c.drawerCellWidth >= 0 || o.Any(x => x.width >= 0))
                parts |= ArrangementParts.Sizes;
            if (o.Any(x => !x.label.NullOrEmpty())) parts |= ArrangementParts.Names;
            if (c.iconMode != IconMode.Default || o.Any(x => x.iconMode != ButtonIconMode.Default || !x.icon.NullOrEmpty())) parts |= ArrangementParts.Icons;
            if ((c.appearance ?? BarConfig.VanillaAppearance) != BarConfig.VanillaAppearance) parts |= ArrangementParts.Theme;
            return parts;
        }
    }
}
