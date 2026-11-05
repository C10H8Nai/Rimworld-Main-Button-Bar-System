using System;
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

    /// <summary>A pin as stored on a button: <see cref="Default"/> means "not changed".</summary>
    public enum ButtonPin
    {
        Default,
        None,
        Left,
        Right,
    }

    /// <summary>
    /// An icon display as stored on a button: <see cref="Default"/> means "not changed", <see cref="FollowBar"/>
    /// uses the bar's setting even when the button's mod declared another.
    /// </summary>
    public enum ButtonIconMode
    {
        Default,
        FollowBar,
        Text,
        Icon,
        Both,
    }

    /// <summary>
    /// The settings a button really uses: the player's changes over what its mod declared (and what is set at runtime,
    /// see <see cref="ButtonDefaults"/>), over the game's own. A value, so reading it allocates nothing.
    /// </summary>
    public readonly struct ButtonSettings
    {
        public readonly string defName;
        public readonly bool hidden;
        public readonly bool inDrawer;

        /// <summary>Custom name; null keeps the game's.</summary>
        public readonly string label;

        /// <summary>Pixels; 0 follows the bar.</summary>
        public readonly int width;

        public readonly PinPosition pin;
        public readonly bool forceShow;

        /// <summary><see cref="IconMode.Default"/> follows the bar (<see cref="BarConfig.IconModeFor"/>).</summary>
        public readonly IconMode iconMode;

        /// <summary>Custom icon reference; null keeps the button's own.</summary>
        public readonly string icon;

        public ButtonSettings(string defName, bool hidden, bool inDrawer, string label, int width, PinPosition pin, bool forceShow, IconMode iconMode, string icon)
        {
            this.defName = defName;
            this.hidden = hidden;
            this.inDrawer = inDrawer;
            this.label = string.IsNullOrEmpty(label) ? null : label;
            this.width = width;
            this.pin = pin;
            this.forceShow = forceShow;
            this.iconMode = iconMode;
            this.icon = string.IsNullOrEmpty(icon) ? null : icon;
        }

        /// <summary>A button nobody changed.</summary>
        public static ButtonSettings Plain(string defName) => new ButtonSettings(defName, false, false, null, 0, PinPosition.None, false, IconMode.Default, null);
    }

    /// <summary>
    /// What the user changed on one button (also used for defaults set at runtime). Every field has its own "not
    /// changed" value: <see cref="DefaultableBool.Default"/>, -1 (<see cref="BarConfig.Unset"/>) for the width,
    /// <see cref="ButtonPin.Default"/>, <see cref="ButtonIconMode.Default"/>, null for texts. A field not changed
    /// follows what the button inherits (its mod's defaults, else the game), so a later change there still shows
    /// (design spec 3.10). Read <see cref="BarConfig.Get"/>; write through the BarConfig setters.
    /// </summary>
    public class ButtonOverride : IExposable
    {
        public string defName;
        public DefaultableBool hidden;
        public DefaultableBool inDrawer;
        public string label;

        /// <summary>Pixels, 0 to follow the bar explicitly, -1 not changed.</summary>
        public int width = BarConfig.Unset;

        public ButtonPin pin;
        public DefaultableBool forceShow;
        public ButtonIconMode iconMode;

        /// <summary>Custom icon reference; null not changed; <see cref="OwnIcon"/> the button's own icon even when its mod declared another.</summary>
        public string icon;

        /// <summary>Stored icon meaning "the button's own icon" (the inherited one may be a mod's declared icon).</summary>
        public const string OwnIcon = "@own";

        public ButtonOverride()
        {
        }

        public ButtonOverride(string defName)
        {
            this.defName = defName;
        }

        public bool IsEmpty =>
            hidden == DefaultableBool.Default && inDrawer == DefaultableBool.Default && string.IsNullOrEmpty(label) && width < 0 &&
            pin == ButtonPin.Default && forceShow == DefaultableBool.Default && iconMode == ButtonIconMode.Default && string.IsNullOrEmpty(icon);

        public bool SameSettings(ButtonOverride other) =>
            hidden == other.hidden && inDrawer == other.inDrawer && NullIfEmpty(label) == NullIfEmpty(other.label) &&
            Math.Max(-1, width) == Math.Max(-1, other.width) && pin == other.pin && forceShow == other.forceShow && iconMode == other.iconMode &&
            NullIfEmpty(icon) == NullIfEmpty(other.icon);

        public ButtonOverride Clone() => (ButtonOverride)MemberwiseClone();

        public ButtonOverride CloneFor(string newDefName)
        {
            var copy = Clone();
            copy.defName = newDefName;
            return copy;
        }

        /// <summary>This override's changes on top of <paramref name="inherited"/>.</summary>
        public ButtonSettings Over(in ButtonSettings inherited) => new ButtonSettings(
            defName ?? inherited.defName,
            BarConfig.Resolve(hidden, inherited.hidden),
            BarConfig.Resolve(inDrawer, inherited.inDrawer),
            string.IsNullOrEmpty(label) ? inherited.label : label,
            width >= 0 ? width : inherited.width,
            pin == ButtonPin.Default ? inherited.pin : ToPosition(pin),
            BarConfig.Resolve(forceShow, inherited.forceShow),
            iconMode == ButtonIconMode.Default ? inherited.iconMode : ToIconMode(iconMode),
            string.IsNullOrEmpty(icon) ? inherited.icon : icon == OwnIcon ? null : icon);

        public static PinPosition ToPosition(ButtonPin pin) => pin switch
        {
            ButtonPin.Left => PinPosition.Left,
            ButtonPin.Right => PinPosition.Right,
            _ => PinPosition.None,
        };

        public static ButtonPin FromPosition(PinPosition pin) => pin switch
        {
            PinPosition.Left => ButtonPin.Left,
            PinPosition.Right => ButtonPin.Right,
            _ => ButtonPin.None,
        };

        public static IconMode ToIconMode(ButtonIconMode mode) => mode switch
        {
            ButtonIconMode.Text => IconMode.Text,
            ButtonIconMode.Icon => IconMode.Icon,
            ButtonIconMode.Both => IconMode.Both,
            _ => IconMode.Default,
        };

        public static ButtonIconMode FromIconMode(IconMode mode) => mode switch
        {
            IconMode.Text => ButtonIconMode.Text,
            IconMode.Icon => ButtonIconMode.Icon,
            IconMode.Both => ButtonIconMode.Both,
            _ => ButtonIconMode.FollowBar,
        };

        /// <summary>
        /// Format 1 and 2 stored the whole button once it was edited (its mod's defaults copied in first), and a
        /// field missing from the file meant false, 0, None or "follow the bar". Reads those values and keeps only the
        /// fields that differ from what the button inherits now.
        /// </summary>
        /// <param name="keepAll">
        /// The button's def is missing (its mod is off): what it inherits is unknown, so every value is kept as set,
        /// and the button looks as before when the mod comes back.
        /// </param>
        internal void UpgradeFromSnapshot(in ButtonSettings inherited, bool keepAll)
        {
            bool h = hidden == DefaultableBool.True, d = inDrawer == DefaultableBool.True, f = forceShow == DefaultableBool.True;
            int w = Math.Max(0, width);
            var p = ToPosition(pin);
            var m = ToIconMode(iconMode);
            string ic = NullIfEmpty(icon);
            hidden = keepAll ? Explicit(h) : Changed(h, inherited.hidden);
            inDrawer = keepAll ? Explicit(d) : Changed(d, inherited.inDrawer);
            forceShow = keepAll ? Explicit(f) : Changed(f, inherited.forceShow);
            width = !keepAll && w == inherited.width ? BarConfig.Unset : w;
            pin = !keepAll && p == inherited.pin ? ButtonPin.Default : FromPosition(p);
            iconMode = !keepAll && m == inherited.iconMode ? ButtonIconMode.Default : FromIconMode(m);
            if (NullIfEmpty(label) == inherited.label && !keepAll) label = null;
            icon = !keepAll && ic == inherited.icon ? null : ic ?? OwnIcon;
        }

        private static DefaultableBool Explicit(bool value) => value ? DefaultableBool.True : DefaultableBool.False;

        private static DefaultableBool Changed(bool value, bool inherited) => BarConfig.Store(value, inherited);

        private static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref hidden, "hidden", DefaultableBool.Default);
            Scribe_Values.Look(ref inDrawer, "inDrawer", DefaultableBool.Default);
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref width, "width", BarConfig.Unset);
            Scribe_Values.Look(ref pin, "pin", ButtonPin.Default);
            Scribe_Values.Look(ref forceShow, "forceShow", DefaultableBool.Default);
            Scribe_Values.Look(ref iconMode, "iconMode", ButtonIconMode.Default);
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
        /// <summary>
        /// 2: number fields may be -1 (use default) and switches may be Default. 3: each field of a button override
        /// has its own "not changed" value (older overrides are upgraded on load, see <see cref="UpgradeOverrides"/>).
        /// </summary>
        public const int CurrentFormatVersion = 3;
        public const int Unset = -1;

        /// <summary>"Not changed" for the decoration offsets, which may be negative so -1 cannot mark them.</summary>
        public const int UnsetOffset = int.MinValue;

        public const string VanillaMode = "Vanilla";
        public const string VanillaAppearance = "Vanilla";

        public int formatVersion = CurrentFormatVersion;

        /// <summary>defName of a <see cref="BarDisplayModeDef"/>.</summary>
        public string displayMode = VanillaMode;

        /// <summary>defName of a <see cref="BarAppearanceDef"/>; in custom mode, the theme the custom look is drawn over.</summary>
        public string appearance = VanillaAppearance;

        /// <summary>Preset: the theme as it is. Custom: <see cref="customTheme"/> on top of it.</summary>
        public AppearanceMode appearanceMode = AppearanceMode.Preset;

        /// <summary>The player's custom look; kept when switching back to a preset, so nothing is lost. May be null.</summary>
        public CustomThemeData customTheme;

        public bool UsesCustomTheme => appearanceMode == AppearanceMode.Custom && customTheme != null;

        /// <summary>Draws buttons and the bar: the theme's worker, or the custom look over it.</summary>
        public BarAppearanceWorker AppearanceWorker =>
            UsesCustomTheme ? CustomThemes.WorkerFor(customTheme, AppearanceDef) : AppearanceDef?.Worker;

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
        public IconMode IconModeFor(in ButtonSettings button) =>
            button.iconMode != IconMode.Default ? button.iconMode : iconMode;

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
            (displayMode ?? VanillaMode) == VanillaMode && (appearance ?? VanillaAppearance) == VanillaAppearance && !UsesCustomTheme && order == null &&
            labelAlign == LabelAlign.Default && iconMode == IconMode.Default && AllRedundant() && !ButtonDefaults.AnyChangesVanillaBar;

        // A plain loop: this runs on every GUI event of the bar.
        private bool AllRedundant()
        {
            for (int i = 0; i < overrides.Count; i++)
                if (!IsRedundant(overrides[i])) return false;
            return true;
        }

        /// <summary>Effective settings of a button: the player's changes over what it inherits (see <see cref="ButtonSettings"/>).</summary>
        public ButtonSettings Get(string defName)
        {
            EnsureIndex();
            var inherited = ButtonDefaults.Inherited(defName);
            return defName != null && index.TryGetValue(defName, out var o) ? o.Over(inherited) : inherited;
        }

        /// <summary>The player's changes to one button, or null.</summary>
        public ButtonOverride OverrideOf(string defName)
        {
            EnsureIndex();
            return defName != null && index.TryGetValue(defName, out var o) ? o : null;
        }

        /// <summary>The player's changes to one button for editing, created empty (every field "not changed") if needed.</summary>
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

        // Setters: a value equal to what the button inherits is stored as "not changed", so it keeps following its
        // mod's defaults; an override left with nothing changed is dropped.
        public void SetHidden(string defName, bool value) => Edit(defName, (o, i) => o.hidden = Store(value, i.hidden));
        public void SetInDrawer(string defName, bool value) => Edit(defName, (o, i) => o.inDrawer = Store(value, i.inDrawer));
        public void SetForceShow(string defName, bool value) => Edit(defName, (o, i) => o.forceShow = Store(value, i.forceShow));
        public void SetLabel(string defName, string value) => Edit(defName, (o, i) => o.label = string.IsNullOrEmpty(value) || value == i.label ? null : value);
        public void SetWidth(string defName, int value) => Edit(defName, (o, i) => o.width = value == i.width ? Unset : Math.Max(0, value));
        public void SetPin(string defName, PinPosition value) => Edit(defName, (o, i) => o.pin = value == i.pin ? ButtonPin.Default : ButtonOverride.FromPosition(value));
        /// <summary>null or empty: the button's own icon.</summary>
        public void SetIcon(string defName, string value) => Edit(defName, (o, i) =>
        {
            string v = string.IsNullOrEmpty(value) ? null : value;
            o.icon = v == i.icon ? null : v ?? ButtonOverride.OwnIcon;
        });

        /// <summary><see cref="IconMode.Default"/> means "follow the bar" here.</summary>
        public void SetIconMode(string defName, IconMode value) =>
            Edit(defName, (o, i) => o.iconMode = value == i.iconMode ? ButtonIconMode.Default : ButtonOverride.FromIconMode(value));

        /// <summary>Back to what the button inherits, for every field.</summary>
        public void ResetButton(string defName)
        {
            EnsureIndex();
            if (defName == null || !index.TryGetValue(defName, out var o)) return;
            overrides.Remove(o);
            index.Remove(defName);
        }

        private void Edit(string defName, Action<ButtonOverride, ButtonSettings> edit)
        {
            if (defName == null) return;
            var o = GetOrCreate(defName);
            edit(o, ButtonDefaults.Inherited(defName));
            if (o.IsEmpty) ResetButton(defName);
        }

        /// <summary>Drops overrides that change nothing, so files only list real changes.</summary>
        public void Prune()
        {
            overrides.RemoveAll(o => o == null || o.defName.NullOrEmpty() || o.IsEmpty);
            index = null;
        }

        private static bool IsRedundant(ButtonOverride o) => o == null || o.IsEmpty;

        /// <summary>
        /// Brings button overrides written before format 3 (whole snapshots) to per-field changes, then marks the
        /// config current. Called on load; configs from defs (arrangement mods) go through <see cref="InCurrentFormat"/>.
        /// </summary>
        public void UpgradeOverrides()
        {
            if (formatVersion >= 3) return;
            overrides ??= new List<ButtonOverride>();
            foreach (var o in overrides)
                if (o?.defName != null)
                    o.UpgradeFromSnapshot(ButtonDefaults.Inherited(o.defName), keepAll: DefDatabase<RimWorld.MainButtonDef>.GetNamedSilentFail(o.defName) == null);
            formatVersion = CurrentFormatVersion;
            index = null;
            // Not pruned here: an override left empty still tells an import that the file knew this button.
        }

        /// <summary>This config if it is current, else an upgraded copy (the original stays as loaded, for example in a def).</summary>
        public BarConfig InCurrentFormat()
        {
            if (formatVersion >= 3) return this;
            var copy = Clone();
            copy.UpgradeOverrides();
            return copy;
        }

        public BarConfig Clone()
        {
            var copy = (BarConfig)MemberwiseClone();
            copy.order = order?.ToList();
            copy.customTheme = customTheme?.Clone();
            copy.overrides = (overrides ?? new List<ButtonOverride>()).Where(o => o != null).Select(o => o.Clone()).ToList();
            copy.index = null;
            return copy;
        }

        public void ResetToUntouched()
        {
            var fresh = new BarConfig();
            displayMode = fresh.displayMode;
            appearance = fresh.appearance;
            appearanceMode = fresh.appearanceMode;
            customTheme = null;
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
            Scribe_Values.Look(ref appearanceMode, "appearanceMode", AppearanceMode.Preset);
            Scribe_Deep.Look(ref customTheme, "customTheme");
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
                else if (formatVersion < 3) UpgradeOverrides();
                displayMode ??= VanillaMode;
                appearance ??= VanillaAppearance;
                index = null;
            }
        }
    }
}
