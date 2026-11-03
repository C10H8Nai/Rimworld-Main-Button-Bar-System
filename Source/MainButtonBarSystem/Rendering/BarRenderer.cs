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
        public const string MenuDefName = "Menu";
        private const float WheelPixelsPerUnit = 25f;

        private static readonly AccessTools.FieldRef<MainButtonsRoot, List<MainButtonDef>> AllButtonsInOrder =
            AccessTools.FieldRefAccess<MainButtonsRoot, List<MainButtonDef>>("allButtonsInOrder");

        private static readonly List<MainButtonDef> visible = new List<MainButtonDef>();
        private static readonly List<LayoutItem> items = new List<LayoutItem>();
        private static readonly Dictionary<string, MainButtonDef> byKey = new Dictionary<string, MainButtonDef>();

        private static float scrollOffset;
        private static bool failed;

        /// <summary>Screen area of the scrollable part of the bar during the last draw; empty when nothing scrolls.</summary>
        public static Rect ScrollCaptureRect { get; private set; }

        public static List<MainButtonDef> ButtonsInOrder(MainButtonsRoot root) => AllButtonsInOrder(root);

        /// <summary>Draws the bar. Returns false when the game should draw it instead.</summary>
        public static bool TryDraw(MainButtonsRoot root)
        {
            ScrollCaptureRect = Rect.zero;
            var settings = MainButtonBarSystemMod.Settings;
            if (failed || settings == null || settings.testRenderMode == TestRenderMode.GameDraws)
                return false;

            try
            {
                Draw(root, settings);
                return true;
            }
            catch (Exception e)
            {
                failed = true;
                ScrollCaptureRect = Rect.zero;
                Log.Error(MainButtonBarSystemMod.LogPrefix + "Drawing the main button bar failed; handing it back to the game for this session. " + e);
                return false;
            }
        }

        private static void Draw(MainButtonsRoot root, MainButtonBarSystemSettings settings)
        {
            bool scroll = settings.testRenderMode == TestRenderMode.OwnScroll;

            visible.Clear();
            items.Clear();
            byKey.Clear();
            var all = AllButtonsInOrder(root);
            if (all == null) return;

            foreach (var def in all)
            {
                if (def?.Worker == null || !def.Worker.Visible) continue;
                visible.Add(def);
                byKey[def.defName] = def;
                var pin = scroll && settings.pinMenuRight && def.defName == MenuDefName ? PinPosition.Right : PinPosition.None;
                items.Add(new LayoutItem(def.defName, def.minimized, null, pin));
            }

            var options = new LayoutOptions
            {
                Mode = scroll ? DisplayMode.Scroll : DisplayMode.Vanilla,
                BarWidth = UI.screenWidth,
                DefaultWidth = settings.scrollDefaultWidth,
                MinWidth = settings.scrollMinWidth,
                FillScreen = settings.fillScreen,
            };
            var layout = BarLayout.Compute(items, options);

            float y = UI.screenHeight - MainButtonDef.ButtonHeight;
            float h = MainButtonDef.ButtonHeight;
            GUI.color = Color.white;

            foreach (var p in layout.Left) DrawButton(p.Key, new Rect(p.X, y, p.Width, h));

            if (layout.NeedsScroll)
                DrawScrollingCenter(layout, y, h);
            else
                foreach (var p in layout.Center) DrawButton(p.Key, new Rect(layout.ViewportX + p.X, y, p.Width, h));

            foreach (var p in layout.Right) DrawButton(p.Key, new Rect(p.X, y, p.Width, h));
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
