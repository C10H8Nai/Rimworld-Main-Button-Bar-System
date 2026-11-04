using System.Collections.Generic;
using System.Linq;
using MainButtonBarSystem.Core;
using UnityEngine;
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
    /// A switch that can also mean "use the default from the tuning def". The names True and False keep files
    /// written when these fields were plain bools readable.
    /// </summary>
    public enum DefaultableBool
    {
        Default,
        True,
        False,
    }

    /// <summary>
    /// One complete arrangement of the bar. Stored per save, as the shared template, as the export format and as
    /// the content of a <see cref="MainButtonArrangementDef"/> (spec 3.4, 4.15). Bump
    /// <see cref="CurrentFormatVersion"/> whenever the meaning of a field changes.
    /// <para>
    /// Number fields hold -1 and switches hold <see cref="DefaultableBool.Default"/> while the player has not
    /// changed them; the effective value then comes from <see cref="MainButtonBarTuningDef"/>. Read the
    /// PascalCase properties, not the fields.
    /// </para>
    /// </summary>
    public class BarConfig : IExposable
    {
        /// <summary>2: number fields may be -1 (use default) and switches may be Default.</summary>
        public const int CurrentFormatVersion = 2;
        public const int Unset = -1;

        /// <summary>"Not changed" for the decoration offsets, which may be negative so -1 cannot mark them.</summary>
        public const int UnsetOffset = int.MinValue;

        public const string VanillaMode = "Vanilla";
        public const string VanillaAppearance = "Vanilla";

        public int formatVersion = CurrentFormatVersion;

        /// <summary>defName of a <see cref="BarDisplayModeDef"/>.</summary>
        public string displayMode = VanillaMode;

        /// <summary>defName of a <see cref="BarAppearanceDef"/>.</summary>
        public string appearance = VanillaAppearance;

        /// <summary>Saved order of defNames; null means the game's own order.</summary>
        public List<string> order;

        public int defaultWidth = Unset;
        public int minWidth = Unset;
        public int gap = Unset;
        public DefaultableBool fillScreen;
        public LabelAlign labelAlign = LabelAlign.Default;

        /// <summary>
        /// Icon / text display for every button whose own setting is Default ("follow the bar"). Default here means
        /// the game's behaviour.
        /// </summary>
        public IconMode iconMode = IconMode.Default;

        /// <summary>The display a button really uses: its own setting, else the bar's.</summary>
        public IconMode IconModeFor(ButtonOverride button) =>
            button != null && button.iconMode != IconMode.Default ? button.iconMode : iconMode;

        /// <summary>Game menu button stays at the right end and cannot be hidden or moved (spec 3.3, 4.16).</summary>
        public DefaultableBool menuLocked;

        public int moreButtonWidth = Unset;
        public int arrowWidth = Unset;
        public float wheelScrollPixels = Unset;
        public int drawerColumns = Unset;
        public int drawerCellWidth = Unset;

        /// <summary>Theme illustration settings (spec 2026-11-04 §3.4): applied on top of the active theme's own values.</summary>
        public DefaultableBool decorationsShown;

        /// <summary>Percent; -1 while unchanged.</summary>
        public int decorationOpacity = Unset;

        /// <summary>Percent; -1 while unchanged.</summary>
        public int decorationScale = Unset;

        /// <summary>Pixels, may be negative; <see cref="UnsetOffset"/> while unchanged.</summary>
        public int decorationOffsetX = UnsetOffset;
        public int decorationOffsetY = UnsetOffset;

        /// <summary>Percent, bar and drawer background images; -1 while unchanged.</summary>
        public int backgroundOpacity = Unset;

        private static MainButtonBarTuningDef.BarDefaults D => MainButtonBarTuningDef.Current.barDefaults;

        public int DefaultWidth => defaultWidth >= 0 ? defaultWidth : D.defaultWidth;
        public int MinWidth => minWidth >= 0 ? minWidth : D.minWidth;
        public int Gap => gap >= 0 ? gap : D.gap;
        public bool FillScreen => Resolve(fillScreen, D.fillScreen);
        public bool MenuLocked => Resolve(menuLocked, D.menuLocked);
        public int MoreButtonWidth => moreButtonWidth >= 0 ? moreButtonWidth : D.moreButtonWidth;
        public int ArrowWidth => arrowWidth >= 0 ? arrowWidth : D.arrowWidth;
        public float WheelScrollPixels => wheelScrollPixels >= 0f ? wheelScrollPixels : D.wheelScrollPixels;
        public int DrawerColumns => drawerColumns >= 0 ? drawerColumns : D.drawerColumns;
        public int DrawerCellWidth => drawerCellWidth >= 0 ? drawerCellWidth : D.drawerCellWidth;

        public bool DecorationsShown => Resolve(decorationsShown, D.decorationsShown);
        public int DecorationOpacityPercent => decorationOpacity >= 0 ? decorationOpacity : D.decorationOpacity;
        public int DecorationScalePercent => decorationScale >= 0 ? decorationScale : D.decorationScale;
        public int DecorationOffsetX => decorationOffsetX != UnsetOffset ? decorationOffsetX : D.decorationOffsetX;
        public int DecorationOffsetY => decorationOffsetY != UnsetOffset ? decorationOffsetY : D.decorationOffsetY;
        public int BackgroundOpacityPercent => backgroundOpacity >= 0 ? backgroundOpacity : D.backgroundOpacity;

        /// <summary>0 to 1.</summary>
        public float DecorationOpacity => DecorationOpacityPercent / 100f;

        /// <summary>1 is the theme's own size.</summary>
        public float DecorationScale => DecorationScalePercent / 100f;

        public Vector2 DecorationOffset => new Vector2(DecorationOffsetX, DecorationOffsetY);

        /// <summary>0 to 1.</summary>
        public float BackgroundOpacity => BackgroundOpacityPercent / 100f;

        public static bool Resolve(DefaultableBool value, bool fallback) =>
            value == DefaultableBool.Default ? fallback : value == DefaultableBool.True;

        public static DefaultableBool Store(bool value, bool fallback) =>
            value == fallback ? DefaultableBool.Default : value ? DefaultableBool.True : DefaultableBool.False;

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
            labelAlign == LabelAlign.Default && iconMode == IconMode.Default && overrides.All(IsRedundant) && !ButtonDefaults.AnyChangesVanillaBar;

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
            iconMode = fresh.iconMode;
            menuLocked = fresh.menuLocked;
            moreButtonWidth = fresh.moreButtonWidth;
            arrowWidth = fresh.arrowWidth;
            wheelScrollPixels = fresh.wheelScrollPixels;
            drawerColumns = fresh.drawerColumns;
            drawerCellWidth = fresh.drawerCellWidth;
            decorationsShown = fresh.decorationsShown;
            decorationOpacity = fresh.decorationOpacity;
            decorationScale = fresh.decorationScale;
            decorationOffsetX = fresh.decorationOffsetX;
            decorationOffsetY = fresh.decorationOffsetY;
            backgroundOpacity = fresh.backgroundOpacity;
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
            Scribe_Values.Look(ref defaultWidth, "defaultWidth", Unset);
            Scribe_Values.Look(ref minWidth, "minWidth", Unset);
            Scribe_Values.Look(ref gap, "gap", Unset);
            Scribe_Values.Look(ref fillScreen, "fillScreen", DefaultableBool.Default);
            Scribe_Values.Look(ref labelAlign, "labelAlign", LabelAlign.Default);
            Scribe_Values.Look(ref iconMode, "iconMode", IconMode.Default);
            Scribe_Values.Look(ref menuLocked, "menuLocked", DefaultableBool.Default);
            Scribe_Values.Look(ref moreButtonWidth, "moreButtonWidth", Unset);
            Scribe_Values.Look(ref arrowWidth, "arrowWidth", Unset);
            Scribe_Values.Look(ref wheelScrollPixels, "wheelScrollPixels", Unset);
            Scribe_Values.Look(ref drawerColumns, "drawerColumns", Unset);
            Scribe_Values.Look(ref drawerCellWidth, "drawerCellWidth", Unset);
            Scribe_Values.Look(ref decorationsShown, "decorationsShown", DefaultableBool.Default);
            Scribe_Values.Look(ref decorationOpacity, "decorationOpacity", Unset);
            Scribe_Values.Look(ref decorationScale, "decorationScale", Unset);
            Scribe_Values.Look(ref decorationOffsetX, "decorationOffsetX", UnsetOffset);
            Scribe_Values.Look(ref decorationOffsetY, "decorationOffsetY", UnsetOffset);
            Scribe_Values.Look(ref backgroundOpacity, "backgroundOpacity", Unset);
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
