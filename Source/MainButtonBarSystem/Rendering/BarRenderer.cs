using System;
using System.Collections.Generic;
using HarmonyLib;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Draws the main button bar in place of MainButtonsRoot.DoButtons. Every button is still drawn by
    /// its own MainButtonWorker.DoButton, so icons, labels and windows other mods add keep working.
    /// </summary>
    public static class BarRenderer
    {
        private const float WheelPixelsPerUnit = 25f;

        private static readonly AccessTools.FieldRef<MainButtonsRoot, List<MainButtonDef>> AllButtonsInOrder =
            AccessTools.FieldRefAccess<MainButtonsRoot, List<MainButtonDef>>("allButtonsInOrder");

        private static readonly List<LayoutItem> items = new List<LayoutItem>();
        private static readonly Dictionary<string, MainButtonDef> byKey = new Dictionary<string, MainButtonDef>();
        private static readonly List<string> drawer = new List<string>();

        private static float scrollOffset;
        private static bool failed;

        /// <summary>Screen area of the scrollable part of the bar during the last draw; empty when nothing scrolls.</summary>
        public static Rect ScrollCaptureRect { get; private set; }

        /// <summary>Buttons in the "More" drawer as of the last draw.</summary>
        public static IReadOnlyList<string> Drawer => drawer;

        public static List<MainButtonDef> ButtonsInOrder(MainButtonsRoot root) => AllButtonsInOrder(root);

        /// <summary>Draws the bar. Returns false when the game should draw it instead.</summary>
        public static bool TryDraw(MainButtonsRoot root)
        {
            ScrollCaptureRect = Rect.zero;
            var config = BarConfigManager.Active;

            try
            {
                LabelOverrides.Sync(config);
            }
            catch (Exception e)
            {
                Fail("Applying custom button names failed", e);
            }

            bool forceOwn = MainButtonBarSystemMod.Settings?.alwaysDrawBar ?? false;
            if (failed || (config.IsUntouched && !forceOwn))
            {
                drawer.Clear();
                return false;
            }

            try
            {
                Draw(root, config);
                return true;
            }
            catch (Exception e)
            {
                Fail("Drawing the main button bar failed; handing it back to the game for this session", e);
                ScrollCaptureRect = Rect.zero;
                drawer.Clear();
                return false;
            }
        }

        private static void Fail(string what, Exception e)
        {
            if (failed) return;
            failed = true;
            Log.Error(MainButtonBarSystemMod.LogPrefix + what + ". " + e);
        }

        private static void Draw(MainButtonsRoot root, BarConfig config)
        {
            items.Clear();
            byKey.Clear();
            drawer.Clear();

            // Buttons the root knows about, so a button another mod removed from the bar at runtime stays removed
            // unless the user forces it.
            var rootButtons = AllButtonsInOrder(root);
            if (rootButtons != null)
            {
                foreach (var def in rootButtons)
                    if (def != null) byKey[def.defName] = def;
            }

            foreach (var name in BarModel.EffectiveOrder(config))
            {
                if (!byKey.TryGetValue(name, out var def))
                {
                    if (!config.Get(name).forceShow) continue;
                    def = DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
                    if (def == null) continue;
                    byKey[name] = def;
                }
                if (!BarModel.ShouldShow(config, def)) continue;

                var o = config.Get(name);
                items.Add(new LayoutItem(name, def.minimized, o.width > 0 ? o.width : (int?)null, BarModel.PinOf(config, name), o.inDrawer));
            }

            var layout = BarLayout.Compute(items, new LayoutOptions
            {
                Mode = config.displayMode,
                BarWidth = UI.screenWidth,
                DefaultWidth = config.defaultWidth,
                MinWidth = config.minWidth,
                Gap = config.gap,
                FillScreen = config.fillScreen,
            });
            drawer.AddRange(layout.Drawer);

            float y = UI.screenHeight - MainButtonDef.ButtonHeight;
            float h = MainButtonDef.ButtonHeight;
            GUI.color = Color.white;

            foreach (var p in layout.Left) DrawButton(p.Key, new Rect(p.X, y, p.Width, h));

            if (layout.NeedsScroll)
                DrawScrollingCenter(layout, y, h);
            else
                foreach (var p in layout.Center) DrawButton(p.Key, new Rect(layout.ViewportX + p.X, y, p.Width, h));

            if (layout.More.HasValue)
            {
                var more = layout.More.Value;
                DrawMoreButton(new Rect(more.X, y, more.Width, h));
            }
            else
            {
                Window_ButtonDrawer.CloseIfOpen();
            }

            foreach (var p in layout.Right) DrawButton(p.Key, new Rect(p.X, y, p.Width, h));
        }

        private static void DrawMoreButton(Rect rect)
        {
            bool open = Window_ButtonDrawer.IsShowing;
            if (Widgets.ButtonTextSubtle(rect, "MBBS_More".Translate(drawer.Count), 0f, -1f, SoundDefOf.Mouseover_Category, default, null, open))
                Window_ButtonDrawer.Toggle(rect);
            TooltipHandler.TipRegion(rect, "MBBS_MoreTip".Translate());
        }

        private static void DrawScrollingCenter(LayoutResult layout, float y, float h)
        {
            var area = new Rect(layout.LeftArrowX, y, layout.RightArrowX + layout.ArrowWidth - layout.LeftArrowX, h);
            ScrollCaptureRect = area;

            var ev = Event.current;
            if (ev.type == EventType.ScrollWheel && Mouse.IsOver(area))
            {
                scrollOffset += (ev.delta.y + ev.delta.x) * WheelPixelsPerUnit;
                ev.Use();
            }

            float page = Mathf.Max(layout.ViewportWidth * 0.6f, 1f);
            if (Widgets.ButtonText(new Rect(layout.LeftArrowX, y, layout.ArrowWidth, h), "<"))
                scrollOffset -= page;
            if (Widgets.ButtonText(new Rect(layout.RightArrowX, y, layout.ArrowWidth, h), ">"))
                scrollOffset += page;

            scrollOffset = layout.ClampScroll(scrollOffset);

            var viewport = new Rect(layout.ViewportX, y, layout.ViewportWidth, h);
            GUI.BeginGroup(viewport);
            try
            {
                foreach (var p in layout.Center)
                {
                    float x = p.X - scrollOffset;
                    if (x + p.Width <= 0f || x >= viewport.width) continue;
                    DrawButton(p.Key, new Rect(x, 0f, p.Width, h));
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private static void DrawButton(string key, Rect rect)
        {
            if (byKey.TryGetValue(key, out var def))
                def.Worker.DoButton(rect);
        }
    }
}
