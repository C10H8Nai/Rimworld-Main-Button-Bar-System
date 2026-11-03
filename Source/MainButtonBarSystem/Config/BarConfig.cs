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
    /// What the user changed on one button. A button without an override uses the defaults its mod declared
    /// (<see cref="MainButtonBarDefaults"/>), else stays exactly as the game and other mods made it (spec 3.7).
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

        public bool IsEmpty => SameSettings(Empty);

        public bool SameSettings(ButtonOverride other) =>
            hidden == other.hidden && inDrawer == other.inDrawer && NullIfEmpty(label) == NullIfEmpty(other.label) &&
            width == other.width && pin == other.pin && forceShow == other.forceShow && iconMode == other.iconMode &&
            NullIfEmpty(icon) == NullIfEmpty(other.icon);

        public ButtonOverride Clone() => (ButtonOverride)MemberwiseClone();

        public ButtonOverride CloneFor(string newDefName)
        {
            var copy = Clone();
            copy.defName = newDefName;
            return copy;
        }

        internal static readonly ButtonOverride Empty = new ButtonOverride();

        private static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

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
    /// One complete arrangement of the bar. Stored per save, as the shared template, as the export format and as
    /// the content of a <see cref="MainButtonArrangementDef"/> (spec 3.4, 4.15). Bump
    /// <see cref="CurrentFormatVersion"/> whenever the meaning of a field changes.
    /// </summary>
    public class BarConfig : IExposable
    {
        public const int CurrentFormatVersion = 1;
        public const int DefaultDefaultWidth = 120;
        public const int DefaultMinWidth = 100;
        public const string VanillaMode = "Vanilla";
        public const string VanillaAppearance = "Vanilla";

        public int formatVersion = CurrentFormatVersion;

        /// <summary>defName of a <see cref="BarDisplayModeDef"/>.</summary>
        public string displayMode = VanillaMode;

        /// <summary>defName of a <see cref="BarAppearanceDef"/>.</summary>
        public string appearance = VanillaAppearance;

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

        public BarDisplayModeDef DisplayModeDef =>
            DefDatabase<BarDisplayModeDef>.GetNamedSilentFail(displayMode ?? VanillaMode) ?? DefDatabase<BarDisplayModeDef>.GetNamedSilentFail(VanillaMode);

        public BarAppearanceDef AppearanceDef =>
            DefDatabase<BarAppearanceDef>.GetNamedSilentFail(appearance ?? VanillaAppearance) ?? DefDatabase<BarAppearanceDef>.GetNamedSilentFail(VanillaAppearance);

        /// <summary>
        /// True when the bar would look exactly like the game draws it, so the game can keep drawing it.
        /// </summary>
        public bool IsUntouched =>
            (displayMode ?? VanillaMode) == VanillaMode && (appearance ?? VanillaAppearance) == VanillaAppearance && order == null &&
            labelAlign == LabelAlign.Default && overrides.All(IsRedundant) && !ButtonDefaults.AnyChangesVanillaBar;

        /// <summary>Effective settings of a button: the user's override, else its mod's defaults, else empty. Never null; do not modify.</summary>
        public ButtonOverride Get(string defName)
        {
            EnsureIndex();
            if (defName != null && index.TryGetValue(defName, out var o)) return o;
            return ButtonDefaults.For(defName) ?? ButtonOverride.Empty;
        }

        /// <summary>The user's override for editing, created from the button's defaults if there is none yet.</summary>
        public ButtonOverride GetOrCreate(string defName)
        {
            EnsureIndex();
            if (!index.TryGetValue(defName, out var o))
            {
                o = ButtonDefaults.For(defName)?.CloneFor(defName) ?? new ButtonOverride(defName);
                overrides.Add(o);
                index[defName] = o;
            }
            return o;
        }

        /// <summary>Drops overrides that equal what the button would have anyway, so files only list real changes.</summary>
        public void Prune()
        {
            overrides.RemoveAll(o => o == null || o.defName.NullOrEmpty() || IsRedundant(o));
            index = null;
        }

        private static bool IsRedundant(ButtonOverride o) =>
            o == null || o.SameSettings(ButtonDefaults.For(o.defName) ?? ButtonOverride.Empty);

        public BarConfig Clone()
        {
            var copy = (BarConfig)MemberwiseClone();
            copy.order = order?.ToList();
            copy.overrides = (overrides ?? new List<ButtonOverride>()).Where(o => o != null).Select(o => o.Clone()).ToList();
            copy.index = null;
            return copy;
        }

        public void ResetToUntouched()
        {
            var fresh = new BarConfig();
            displayMode = fresh.displayMode;
            appearance = fresh.appearance;
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
            overrides ??= new List<ButtonOverride>();
            index = new Dictionary<string, ButtonOverride>();
            foreach (var o in overrides)
            {
                if (o?.defName != null) index[o.defName] = o;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref formatVersion, "formatVersion", 0);
            Scribe_Values.Look(ref displayMode, "displayMode", VanillaMode);
            Scribe_Values.Look(ref appearance, "appearance", VanillaAppearance);
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
                overrides ??= new List<ButtonOverride>();
                overrides.RemoveAll(o => o == null || o.defName.NullOrEmpty());
                if (formatVersion <= 0) formatVersion = CurrentFormatVersion;
                displayMode ??= VanillaMode;
                appearance ??= VanillaAppearance;
                index = null;
            }
        }
    }
}
