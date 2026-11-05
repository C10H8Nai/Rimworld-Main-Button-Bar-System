using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// The "More" drawer: a grid of the buttons collapsed out of the bar, popping up above the More button,
    /// with its own settings button underneath (spec 3.3). Columns and cell width come from the bar settings,
    /// everything else from the tuning def.
    /// </summary>
    // The attribute tells the game this class's static texture field is intended (it is loaded on first use).
    [StaticConstructorOnStartup]
    public class Window_ButtonDrawer : Window
    {
        private static float lastClosedAt = -1f;

        private readonly Rect anchor;
        private readonly List<BarButton> buttons = new List<BarButton>();

        private static MainButtonBarTuningDef.Behaviour B => MainButtonBarTuningDef.Current.behaviour;

        public static bool IsShowing => Find.WindowStack?.IsOpen<Window_ButtonDrawer>() ?? false;

        public static void Toggle(Rect moreButtonRect)
        {
            if (IsShowing) CloseIfOpen();
            else if (Time.realtimeSinceStartup - lastClosedAt > B.drawerReopenGuardSeconds)
                Find.WindowStack.Add(new Window_ButtonDrawer(moreButtonRect));
        }

        public static void CloseIfOpen()
        {
            Find.WindowStack?.TryRemove(typeof(Window_ButtonDrawer), doCloseSound: false);
        }

        public Window_ButtonDrawer(Rect moreButtonRect)
        {
            anchor = moreButtonRect;
            layer = WindowLayer.GameUI;
            doCloseX = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            drawShadow = true;
            doWindowBackground = false; // the theme draws it (see DoWindowContents)
            soundAppear = SoundDefOf.TabOpen;
            soundClose = SoundDefOf.TabClose;
        }

        public override void PostClose()
        {
            base.PostClose();
            lastClosedAt = Time.realtimeSinceStartup;
        }

        protected override float Margin => B.drawerPadding;

        private static BarConfig Config => BarConfigManager.Active;

        private static float CellWidth => Config.DrawerCellWidth > 0 ? Config.DrawerCellWidth : Config.DefaultWidth;

        private static float CellHeight => B.buttonRectHeight;

        private static int Columns
        {
            get
            {
                int count = Mathf.Max(1, BarRenderer.Drawer.Count);
                int wanted = Config.DrawerColumns > 0 ? Config.DrawerColumns : Mathf.Min(count, B.drawerMaxAutoColumns);
                // Never wider than the screen.
                int fit = Mathf.Max(1, Mathf.FloorToInt((UI.screenWidth - B.drawerPadding * 2f) / Mathf.Max(1f, CellWidth)));
                return Mathf.Clamp(wanted, 1, fit);
            }
        }

        private static int Rows => Mathf.Max(1, Mathf.CeilToInt(BarRenderer.Drawer.Count / (float)Columns));

        public override Vector2 InitialSize =>
            new Vector2(Mathf.Max(Columns * CellWidth, B.drawerSettingsRowHeight) + B.drawerPadding * 2f,
                Rows * CellHeight + B.drawerSettingsRowHeight + B.drawerPadding * 3f);

        private static Texture2D defaultSettingsIcon;

        internal static void ResetCache() => defaultSettingsIcon = null;

        /// <summary>The settings gear: the theme's icon, else the tuning def's (this mod ships one).</summary>
        private static Texture2D SettingsIcon(BarAppearanceWorker appearance)
        {
            var themed = appearance?.DrawerSettingsIcon();
            if (themed != null) return themed;
            if (defaultSettingsIcon == null)
            {
                var path = B.drawerSettingsIcon;
                defaultSettingsIcon = string.IsNullOrEmpty(path) ? null : ContentFinder<Texture2D>.Get(path, false);
                if (defaultSettingsIcon == null)
                    Log.WarningOnce(MainButtonBarSystemMod.LogPrefix + "Drawer settings icon '" + path + "' not found.", 0x4D424254);
            }
            return defaultSettingsIcon;
        }

        protected override void SetInitialSizeAndPosition()
        {
            var size = InitialSize;
            float x = Mathf.Clamp(anchor.xMax - size.x, 0f, Mathf.Max(0f, UI.screenWidth - size.x));
            float y = anchor.y - size.y;
            windowRect = new Rect(x, y, size.x, size.y).Rounded();
        }

        public override void DoWindowContents(Rect inRect)
        {
            buttons.Clear();
            buttons.AddRange(BarRenderer.Drawer);
            if (buttons.Count == 0)
            {
                Close(false);
                return;
            }

            var size = InitialSize;
            if (!Mathf.Approximately(windowRect.height, size.y) || !Mathf.Approximately(windowRect.width, size.x))
            {
                SetInitialSizeAndPosition();
                return;
            }

            var appearance = BarRenderer.Appearance;
            appearance.DrawDrawerBackground(inRect.ExpandedBy(B.drawerPadding));

            int columns = Columns;
            float cellWidth = CellWidth, cellHeight = CellHeight;
            bool activated = false;
            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button?.Def?.Worker == null) continue;

                var cell = new Rect(inRect.x + (i % columns) * cellWidth, inRect.y + (i / columns) * cellHeight, cellWidth, cellHeight);
                var before = Event.current.type;
                try
                {
                    appearance.DrawDrawerButton(button, cell);
                }
                catch (Exception e) when (!(e is ExitGUIException))
                {
                    ButtonGuard.Report(button, e);
                }
                // Buttons fire on mouse up; closing on mouse down would swallow the click.
                if (before == EventType.MouseUp && Event.current.type == EventType.Used && Mouse.IsOver(cell))
                    activated = true;
            }

            // A square gear at the bottom right: fits any drawer width and any language.
            float gear = B.drawerSettingsRowHeight;
            var settingsRect = new Rect(inRect.xMax - gear, inRect.yMax - gear, gear, gear);
            TooltipHandler.TipRegion(settingsRect, "MBBS_OpenSettings".Translate());
            var icon = SettingsIcon(appearance);
            if (icon != null ? Widgets.ButtonImage(settingsRect, icon) : Widgets.ButtonText(settingsRect, "…"))
            {
                MainButtonBarAPI.OpenSettings();
                activated = true;
            }

            appearance.DrawDrawerOverlay(inRect.ExpandedBy(B.drawerPadding));
            GUI.color = Color.white;

            if (activated) Close(false);
        }
    }
}
