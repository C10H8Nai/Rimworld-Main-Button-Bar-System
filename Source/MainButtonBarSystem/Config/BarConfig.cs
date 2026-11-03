using System.Collections.Generic;
using System.Linq;
using MainButtonBarSystem.Core;
using Verse;

namespace MainButtonBarSystem
{
    public enum IconMode
    {
        /// <summary>Whatever the game and other mods do (icon when there is one).</summary>
        Default,
        Text,
        Icon,
        Both,
    }

    /// <summary>
    /// What the user changed on one button. Every field's default means "not changed", so an override that
    /// only has defaults is the same as no override and buttons the user never touched stay exactly as the
    /// game and other mods made them (spec 3.7).
    /// </summary>
    public class ButtonOverride : IExposable
    {
        public string defName;
        public bool hidden;
        public bool inDrawer;
        public string label;
        public int width;
        public PinPosition pin;
        public bool forceShow;
        public IconMode iconMode;
        public string icon;

        public ButtonOverride()
        {
        }

        public ButtonOverride(string defName)
        {
            this.defName = defName;
        }

        public bool IsEmpty =>
            !hidden && !inDrawer && label.NullOrEmpty() && width <= 0 && pin == PinPosition.None && !forceShow &&
            iconMode == IconMode.Default && icon.NullOrEmpty();

        public ButtonOverride Clone() => (ButtonOverride)MemberwiseClone();

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref hidden, "hidden");
            Scribe_Values.Look(ref inDrawer, "inDrawer");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref width, "width");
            Scribe_Values.Look(ref pin, "pin", PinPosition.None);
            Scribe_Values.Look(ref forceShow, "forceShow");
            Scribe_Values.Look(ref iconMode, "iconMode", IconMode.Default);
            Scribe_Values.Look(ref icon, "icon");
        }
    }

    public enum LabelAlign
    {
        Default,
        Left,
        Center,
        Right,
    }

    /// <summary>
    /// One complete arrangement of the bar. Stored per save, as the shared template, and as the export format
    /// (spec 3.4, 4.15). <see cref="CurrentFormatVersion"/> is bumped whenever the meaning of a field changes.
    /// </summary>
    public class BarConfig : IExposable
    {
        public const int CurrentFormatVersion = 1;
        public const int DefaultDefaultWidth = 120;
        public const int DefaultMinWidth = 100;

        public int formatVersion = CurrentFormatVersion;
        public DisplayMode displayMode = DisplayMode.Vanilla;

        /// <summary>Saved order of defNames; null means the game's own order.</summary>
        public List<string> order;

        public int defaultWidth = DefaultDefaultWidth;
        public int minWidth = DefaultMinWidth;
        public int gap;
        public bool fillScreen = true;
        public LabelAlign labelAlign = LabelAlign.Default;

        /// <summary>Game menu button stays at the right end and cannot be hidden or moved (spec 3.3, 4.16).</summary>
        public bool menuLocked = true;

        public List<ButtonOverride> overrides = new List<ButtonOverride>();

        [Unsaved] private Dictionary<string, ButtonOverride> index;

        private static readonly ButtonOverride Empty = new ButtonOverride();

        /// <summary>
        /// True when nothing differs from the untouched state, so the game can keep drawing the bar itself.
        /// </summary>
        public bool IsUntouched =>
            displayMode == DisplayMode.Vanilla && order == null && overrides.All(o => o.IsEmpty);

        /// <summary>Read-only view of a button's override; never null.</summary>
        public ButtonOverride Get(string defName)
        {
            EnsureIndex();
            return defName != null && index.TryGetValue(defName, out var o) ? o : Empty;
        }

        public ButtonOverride GetOrCreate(string defName)
        {
            EnsureIndex();
            if (!index.TryGetValue(defName, out var o))
            {
                o = new ButtonOverride(defName);
                overrides.Add(o);
                index[defName] = o;
            }
            return o;
        }

        /// <summary>Drops overrides that hold no change, so the file only lists what the user really changed.</summary>
        public void Prune()
        {
            overrides.RemoveAll(o => o == null || o.defName.NullOrEmpty() || o.IsEmpty);
            index = null;
        }

        public BarConfig Clone()
        {
            var copy = (BarConfig)MemberwiseClone();
            copy.order = order?.ToList();
            copy.overrides = overrides.Where(o => o != null).Select(o => o.Clone()).ToList();
            copy.index = null;
            return copy;
        }

        public void ResetToUntouched()
        {
            var fresh = new BarConfig();
            displayMode = fresh.displayMode;
            order = null;
            defaultWidth = fresh.defaultWidth;
            minWidth = fresh.minWidth;
            gap = fresh.gap;
            fillScreen = fresh.fillScreen;
            labelAlign = fresh.labelAlign;
            menuLocked = fresh.menuLocked;
            overrides.Clear();
            index = null;
        }

        private void EnsureIndex()
        {
            if (index != null) return;
            index = new Dictionary<string, ButtonOverride>();
            foreach (var o in overrides)
            {
                if (o?.defName != null) index[o.defName] = o;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref formatVersion, "formatVersion", 0);
            Scribe_Values.Look(ref displayMode, "displayMode", DisplayMode.Vanilla);
            Scribe_Collections.Look(ref order, "order", LookMode.Value);
            Scribe_Values.Look(ref defaultWidth, "defaultWidth", DefaultDefaultWidth);
            Scribe_Values.Look(ref minWidth, "minWidth", DefaultMinWidth);
            Scribe_Values.Look(ref gap, "gap");
            Scribe_Values.Look(ref fillScreen, "fillScreen", true);
            Scribe_Values.Look(ref labelAlign, "labelAlign", LabelAlign.Default);
            Scribe_Values.Look(ref menuLocked, "menuLocked", true);
            Scribe_Collections.Look(ref overrides, "overrides", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (overrides == null) overrides = new List<ButtonOverride>();
                overrides.RemoveAll(o => o == null || o.defName.NullOrEmpty());
                if (formatVersion <= 0) formatVersion = CurrentFormatVersion;
                index = null;
            }
        }
    }
}
