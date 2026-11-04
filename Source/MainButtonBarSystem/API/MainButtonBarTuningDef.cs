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
            if (behaviour != null && (behaviour.buttonRectHeight <= 0f || behaviour.barTopFromBottom <= 0f || behaviour.drawerMaxAutoColumns <= 0))
                yield return "behaviour: buttonRectHeight, barTopFromBottom and drawerMaxAutoColumns must be positive";
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

            /// <summary>Width sliders snap to multiples of this.</summary>
            public int widthStep;
        }

        public class Behaviour
        {
            /// <summary>Distance from the screen bottom to the top of the bar. Vanilla: 35.</summary>
            public float barTopFromBottom;

            /// <summary>Height of each button rect. Vanilla draws 36, one pixel past the screen bottom.</summary>
            public float buttonRectHeight;

            /// <summary>Share of the visible scroll area one arrow click moves.</summary>
            public float arrowPageFraction;

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
        }

        public class SettingsWindowLayout
        {
            public Vector2 maxSize;
            public float screenMargin;
            public float listFraction;
            public float panelGap;
            public float headerHeight;
            public float noticeLineHeight;
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
