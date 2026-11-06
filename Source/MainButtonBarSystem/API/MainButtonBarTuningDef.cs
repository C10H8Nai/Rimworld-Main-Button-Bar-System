using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Every tunable number of the mod, in one XML def (Defs/MainButtonBarSystem/Tuning.xml). Nothing tunable is
    /// written in code: the C# fields have no initial values and <see cref="ConfigErrors"/> reports any the XML
    /// leaves out. Add-ons and players can change values with an XML patch.
    /// </summary>
    public class MainButtonBarTuningDef : Def
    {
        public const string DefName = "MainButtonBarTuning";

        private static MainButtonBarTuningDef current;

        public static MainButtonBarTuningDef Current => current ??= DefDatabase<MainButtonBarTuningDef>.GetNamed(DefName);

        /// <summary>Forget the cached def after play data was reloaded (the def is a new object then).</summary>
        internal static void ResetCache() => current = null;

        /// <summary>Defaults of the settings players can change. Each has a "default" button in the settings window.</summary>
        public BarDefaults barDefaults;

        /// <summary>Slider ranges and steps in the settings window.</summary>
        public SettingRanges ranges;

        /// <summary>Fixed behaviour that is not shown in the settings window.</summary>
        public Behaviour behaviour;

        public SettingsWindowLayout settingsWindow;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (barDefaults == null) yield return "barDefaults is missing";
            if (ranges == null) yield return "ranges is missing";
            if (behaviour == null) yield return "behaviour is missing";
            if (settingsWindow == null) yield return "settingsWindow is missing";
            if (barDefaults != null && (barDefaults.defaultWidth <= 0 || barDefaults.minWidth <= 0 || barDefaults.arrowWidth <= 0 || barDefaults.wheelScrollPixels <= 0f))
                yield return "barDefaults: defaultWidth, minWidth, arrowWidth and wheelScrollPixels must be positive";
            if (behaviour != null && (behaviour.buttonRectHeight <= 0f || behaviour.barTopFromBottom <= 0f || behaviour.drawerMaxAutoColumns <= 0 || behaviour.splitMinButtonWidth <= 0))
                yield return "behaviour: buttonRectHeight, barTopFromBottom, drawerMaxAutoColumns and splitMinButtonWidth must be positive";
            if (behaviour != null && (behaviour.themeAtlasMaxSize <= 0 || behaviour.themeDecorationMaxSize <= 0 || behaviour.themeTileMaxCount <= 0))
                yield return "behaviour: themeAtlasMaxSize, themeDecorationMaxSize and themeTileMaxCount must be positive";
            if (ranges != null && (ranges.widthStep <= 0 || ranges.percentStep <= 0))
                yield return "ranges: widthStep and percentStep must be positive";
            if (settingsWindow != null && (settingsWindow.rowHeight <= 0f || settingsWindow.maxSize.x <= 0f || settingsWindow.maxSize.y <= 0f ||
                                           settingsWindow.customNameMaxLength <= 0 || settingsWindow.captureKeyDialogSize.x <= 0f))
                yield return "settingsWindow: rowHeight, maxSize, customNameMaxLength and captureKeyDialogSize must be positive";
        }

        public class BarDefaults
        {
            /// <summary>Natural width of a full button when scrolling, or collapsing without filling the screen.</summary>
            public int defaultWidth;

            /// <summary>No full button is narrower than this when scrolling; half buttons get half.</summary>
            public int minWidth;

            public int gap;
            public bool fillScreen;

            /// <summary>Half-width buttons (MainButtonDef.minimized) get the same width as the others.</summary>
            public bool minimizedFullWidth;
            public bool menuLocked;

            /// <summary>0 fits the "More (N)" label.</summary>
            public int moreButtonWidth;

            public int arrowWidth;

            /// <summary>Pixels scrolled per unit of mouse wheel movement.</summary>
            public float wheelScrollPixels;

            /// <summary>0 picks the column count automatically.</summary>
            public int drawerColumns;

            /// <summary>0 uses the bar's default button width.</summary>
            public int drawerCellWidth;

            /// <summary>Custom theme: size of the "custom size" font until the player changes it; 0 the game's normal font size.</summary>
            public int lookFontSize;

            /// <summary>Theme illustration settings, applied on top of the theme's own values: show decorations at all.</summary>
            public bool decorationsShown;

            /// <summary>Percent multiplied into every decoration's opacity.</summary>
            public int decorationOpacity;

            /// <summary>Percent multiplied into every decoration's size, anchored at its corner.</summary>
            public int decorationScale;

            /// <summary>Pixels added to every decoration's offset.</summary>
            public int decorationOffsetX;
            public int decorationOffsetY;

            /// <summary>Percent opacity of the bar and drawer background images.</summary>
            public int backgroundOpacity;
        }

        public class SettingRanges
        {
            public IntRange defaultWidth;
            public IntRange minWidth;
            public IntRange gap;
            public IntRange buttonWidth;
            public IntRange moreButtonWidth;
            public IntRange arrowWidth;
            public FloatRange wheelScrollPixels;
            public IntRange drawerColumns;
            public IntRange drawerCellWidth;
            public IntRange decorationOpacity;
            public IntRange decorationScale;

            /// <summary>Shared by the X and Y offset sliders; may be negative.</summary>
            public IntRange decorationOffset;

            /// <summary>Custom theme: label size of the "custom size" font (pixels).</summary>
            public IntRange lookFontSize;

            /// <summary>Custom theme: background picture opacity (percent) and inset (pixels).</summary>
            public IntRange lookImageOpacity;
            public IntRange lookImageInset;

            /// <summary>Custom theme: icon size and offset (pixels). The size never goes past the button's height.</summary>
            public IntRange lookIconSize;
            public IntRange lookIconOffset;
            public IntRange backgroundOpacity;

            /// <summary>Width sliders snap to multiples of this.</summary>
            public int widthStep;

            /// <summary>Percent sliders (opacity, scale) snap to multiples of this.</summary>
            public int percentStep;
        }

        public class Behaviour
        {
            /// <summary>Distance from the screen bottom to the top of the bar. Vanilla: 35.</summary>
            public float barTopFromBottom;

            /// <summary>Height of each button rect. Vanilla draws 36, one pixel past the screen bottom.</summary>
            public float buttonRectHeight;

            /// <summary>Share of the visible scroll area one arrow click moves.</summary>
            public float arrowPageFraction;

            /// <summary>
            /// Equal split with per-button widths: the least a button without its own width keeps. When the set widths
            /// leave less than this for each of the others, every button shrinks in proportion instead.
            /// </summary>
            public int splitMinButtonWidth;

            /// <summary>
            /// When the equal split shrinks every button (above), the More button keeps at least this width. 0: its own
            /// width (the player's setting, or its label); negative: no minimum.
            /// </summary>
            public int splitMoreMinWidth;

            /// <summary>The same for the menu button. 0: the width of its label; negative: no minimum.</summary>
            public int splitMenuMinWidth;

            /// <summary>
            /// A button this mod draws that is narrower than this shows only its background: neither the icon nor the
            /// label fits. Applies in every display mode and in the drawer, half-width buttons included (their own
            /// width counts). 0: always draw them.
            /// </summary>
            public int contentMinButtonWidth;

            /// <summary>Upper limit of columns when the drawer picks them automatically.</summary>
            public int drawerMaxAutoColumns;

            public float drawerPadding;
            public float drawerSettingsRowHeight;

            /// <summary>Clicking More while the drawer is open closes it as a click outside, then More fires; ignore that reopen.</summary>
            public float drawerReopenGuardSeconds;

            /// <summary>Extra width around the label when the More button fits its label.</summary>
            public float moreButtonLabelPadding;

            /// <summary>Space between a left- or right-aligned button label and the button edge.</summary>
            public float labelSidePadding;

            /// <summary>Where the game's subtle button starts its label: this share of the button width from the left (vanilla 0.15).</summary>
            public float gameLabelLeftMarginFraction;

            /// <summary>Font of button labels this mod draws itself (the game uses Small).</summary>
            public GameFont labelFont;

            /// <summary>Shift of a label under the mouse, like the game's "pressed" look: (2,-2).</summary>
            public Vector2 hoverLabelOffset;

            /// <summary>Icon size in buttons this mod lays out itself ("icon and text"); the game draws 32.</summary>
            public float iconSize;

            /// <summary>Space between the icon and the label in "icon and text" mode.</summary>
            public float iconTextGap;

            /// <summary>Texture of the drawer's settings gear (themes can replace it). This mod ships MainButtonBarSystem/UI/Gear.</summary>
            public string drawerSettingsIcon;

            /// <summary>Theme checks in dev mode warn about atlases wider than this: on a bar button the corners cannot grow past half its height.</summary>
            public int themeAtlasMaxSize;

            /// <summary>Theme checks in dev mode warn about decoration or background images with a side longer than this.</summary>
            public int themeDecorationMaxSize;

            /// <summary>A tiled background that would need more tiles than this is stretched instead.</summary>
            public int themeTileMaxCount;
        }

        public class SettingsWindowLayout
        {
            public Vector2 maxSize;
            public float screenMargin;
            public float listFraction;
            public float panelGap;
            public float headerHeight;
            public float noticeLineHeight;

            /// <summary>A button next to a compatibility notice is at most this share of the notice's width.</summary>
            public float noticeButtonMaxFraction;
            public float tabButtonWidth;
            public float closeButtonSpace;
            public float columnHeaderHeight;
            public float rowHeight;
            public float footerButtonHeight;
            public float footerGap;
            public float detailPadding;
            public float checkboxSize;
            public float handleColumn;
            public float visibleColumn;
            public float iconColumn;
            public float hotKeyColumn;
            public float drawerColumn;
            public float sourceColumnMax;
            public float sourceColumnFraction;
            public float moveButtonWidth;
            public float resetButtonWidth;

            /// <summary>Space on each side of a button label; buttons widen to fit longer translations.</summary>
            public float buttonTextPadding;
            public float smallButtonWidth;
            public float titleIconSize;
            public float textRowHeight;
            public float sliderHeight;
            public float radioIndent;
            public float listFramePadding;
            public float rowIconPadding;
            public float textInset;
            public float tabButtonInset;

            /// <summary>Opacity of buttons that are hidden or not on the bar right now.</summary>
            public float dimmedAlpha;

            public int customNameMaxLength;

            /// <summary>Most recent exported files listed in the Import menu.</summary>
            public int importMenuMaxFiles;

            public Vector2 captureKeyDialogSize;

            public float themeCardHeight;
            public float themePreviewHeight;

            /// <summary>Share of a theme card's width used by its preview.</summary>
            public float themePreviewFraction;

            /// <summary>Sample buttons in a drawn theme preview (a More button is added).</summary>
            public int themePreviewButtons;

            /// <summary>Space between the theme cards and the illustration settings under them.</summary>
            public float themeSettingsGap;

            /// <summary>Custom theme: one cell of the 3×3 icon anchor picker.</summary>
            public float lookAnchorCell;

            /// <summary>Space around each cell of the anchor picker, and the size of its dot as a share of a cell, kept this far from the edge.</summary>
            public float lookAnchorCellGap;
            public float lookAnchorDotFraction;
            public float lookAnchorDotMargin;

            /// <summary>Custom theme: the colour swatch (width, and its space above and below), the colour code box, the image thumbnails.</summary>
            public float lookSwatchWidth;
            public float lookSwatchInset;
            public float lookHexFieldWidth;
            public float lookThumbWidth;

            /// <summary>Colours offered in the colour picker's palette.</summary>
            public List<Color> lookColorPalette;

            /// <summary>The image picker of custom themes.</summary>
            public Vector2 imagePickerSize;
            public float imagePickerCell;

            /// <summary>Number box to the right of each slider; this mod draws its own sliders so other slider mods do not overlap them.</summary>
            public float numberFieldWidth;
            public float sliderRailHeight;
            public Vector2 sliderHandleSize;
            public Color sliderRailColor;
            public Color sliderFillColor;
            public Color sliderHandleColor;

            /// <summary>The Simple / Advanced switch on the Buttons page.</summary>
            public float viewSwitchWidth;
            public Color viewSwitchSelectedColor;
            public Color viewSwitchColor;

            /// <summary>The source mod drop-down next to the switch; never wider than the button list.</summary>
            public float sourceFilterWidth;

            public Vector2 iconPickerSize;
            public float iconPickerCell;

            /// <summary>The export window (name, author, description) and the import window (report and parts).</summary>
            public Vector2 exportDialogSize;
            public Vector2 importDialogSize;
            public float descriptionBoxHeight;

            /// <summary>Button names listed per line of the import report before "and N more".</summary>
            public int importReportMaxNames;
            public Color reportWarningColor;
        }
    }
}
