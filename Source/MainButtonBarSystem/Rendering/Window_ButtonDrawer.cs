using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// The "More" drawer: a grid of the buttons collapsed out of the bar, popping up above the More button,
    /// with its own settings button underneath (spec 3.3).
    /// </summary>
    public class Window_ButtonDrawer : Window
    {
        private const float CellWidth = 170f;
        private const float CellHeight = MainButtonDef.ButtonHeight;
        private const float SettingsRowHeight = 30f;
        private const float Pad = 6f;
        private const int MaxColumns = 4;

        private readonly Rect anchor;
        private readonly List<string> keys = new List<string>();

        // Clicking More while the drawer is open first closes it as a click outside (mouse down), then the
        // More button fires (mouse up). Without this the drawer would reopen immediately.
        private const float ReopenGuardSeconds = 0.3f;
        private static float lastClosedAt = -1f;

        public static bool IsShowing => Find.WindowStack?.IsOpen<Window_ButtonDrawer>() ?? false;

        public static void Toggle(Rect moreButtonRect)
        {
            if (IsShowing) CloseIfOpen();
            else if (Time.realtimeSinceStartup - lastClosedAt > ReopenGuardSeconds)
                Find.WindowStack.Add(new Window_ButtonDrawer(moreButtonRect));
        }

        public override void PostClose()
        {
            base.PostClose();
            lastClosedAt = Time.realtimeSinceStartup;
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
            soundAppear = SoundDefOf.TabOpen;
            soundClose = SoundDefOf.TabClose;
        }

        protected override float Margin => Pad;

        private int Columns => Mathf.Clamp(BarRenderer.Drawer.Count, 1, MaxColumns);

        private int Rows => Mathf.Max(1, Mathf.CeilToInt(BarRenderer.Drawer.Count / (float)Columns));

        public override Vector2 InitialSize =>
            new Vector2(Columns * CellWidth + Pad * 2f, Rows * CellHeight + SettingsRowHeight + Pad * 3f);

        protected override void SetInitialSizeAndPosition()
        {
            var size = InitialSize;
            float x = Mathf.Clamp(anchor.xMax - size.x, 0f, UI.screenWidth - size.x);
            float y = anchor.y - size.y;
            windowRect = new Rect(x, y, size.x, size.y).Rounded();
        }

        public override void DoWindowContents(Rect inRect)
        {
            keys.Clear();
            keys.AddRange(BarRenderer.Drawer);
            if (keys.Count == 0)
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

            int columns = Columns;
            bool activated = false;
            for (int i = 0; i < keys.Count; i++)
            {
                var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(keys[i]);
                if (def?.Worker == null) continue;

                var cell = new Rect(inRect.x + (i % columns) * CellWidth, inRect.y + (i / columns) * CellHeight, CellWidth, CellHeight);
                var before = Event.current.type;
                def.Worker.DoButton(cell);
                // Buttons fire on mouse up; closing on mouse down would swallow the click.
                if (before == EventType.MouseUp && Event.current.type == EventType.Used && Mouse.IsOver(cell))
                    activated = true;
            }

            var settingsRect = new Rect(inRect.x, inRect.yMax - SettingsRowHeight, inRect.width, SettingsRowHeight);
            if (Widgets.ButtonText(settingsRect, "MBBS_OpenSettings".Translate()))
            {
                Find.WindowStack.Add(new Dialog_MainButtonBar());
                activated = true;
            }

            if (activated) Close(false);
        }
    }
}
