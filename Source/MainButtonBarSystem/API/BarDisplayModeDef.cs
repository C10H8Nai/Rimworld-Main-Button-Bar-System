using System;
using System.Collections.Generic;
using MainButtonBarSystem.Core;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Slot "display mode": how buttons are arranged on the bar (spec 3.8). The four built-in modes are defs of
    /// this type too. An add-on mod adds a def with its own <see cref="workerClass"/>.
    /// </summary>
    public class BarDisplayModeDef : Def
    {
        public Type workerClass = typeof(BarDisplayModeWorker_Standard);

        /// <summary>Layout used by <see cref="BarDisplayModeWorker_Standard"/>; ignored by other workers.</summary>
        public DisplayMode layout = DisplayMode.Vanilla;

        /// <summary>The settings window shows the "Drawer" column only when this is true.</summary>
        public bool usesDrawer;

        /// <summary>The settings window shows the minimum-width slider only when this is true.</summary>
        public bool usesMinWidth;

        /// <summary>Position in the settings window list.</summary>
        public int displayOrder;

        [Unsaved] private BarDisplayModeWorker workerInt;

        public BarDisplayModeWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (BarDisplayModeWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (workerClass == null || !typeof(BarDisplayModeWorker).IsAssignableFrom(workerClass))
                yield return "workerClass must derive from " + nameof(BarDisplayModeWorker);
        }
    }

    public abstract class BarDisplayModeWorker
    {
        public BarDisplayModeDef def;

        /// <summary>Called for every GUI event while this mode is active. Draw buttons with <see cref="BarDrawContext.DrawButton"/>.</summary>
        public abstract void DrawBar(BarDrawContext ctx);
    }

    /// <summary>The built-in modes: vanilla split, collapse, scroll, collapse + scroll.</summary>
    public class BarDisplayModeWorker_Standard : BarDisplayModeWorker
    {
        private const float WheelPixelsPerUnit = 25f;

        private readonly List<LayoutItem> items = new List<LayoutItem>();
        private readonly Dictionary<string, BarButton> byKey = new Dictionary<string, BarButton>();
        private readonly List<BarButton> drawer = new List<BarButton>();
        private float scrollOffset;

        public override void DrawBar(BarDrawContext ctx)
        {
            items.Clear();
            byKey.Clear();
            drawer.Clear();

            foreach (var b in ctx.Buttons)
            {
                byKey[b.Key] = b;
                items.Add(new LayoutItem(b.Key, b.Minimized, b.Width, b.Pin, b.InDrawer));
            }

            var config = ctx.Config;
            var layout = BarLayout.Compute(items, new LayoutOptions
            {
                Mode = def.layout,
                BarWidth = (int)ctx.BarRect.width,
                DefaultWidth = config.defaultWidth,
                MinWidth = config.minWidth,
                Gap = config.gap,
                FillScreen = config.fillScreen,
            });

            float x0 = ctx.BarRect.x, y = ctx.BarRect.y, h = ctx.BarRect.height;
            ctx.Appearance.DrawBarBackground(ctx.BarRect);

            foreach (var p in layout.Left) ctx.DrawButton(byKey[p.Key], new Rect(x0 + p.X, y, p.Width, h));

            if (layout.NeedsScroll) DrawScrolling(ctx, layout, x0, y, h);
            else foreach (var p in layout.Center) ctx.DrawButton(byKey[p.Key], new Rect(x0 + layout.ViewportX + p.X, y, p.Width, h));

            foreach (var key in layout.Drawer)
            {
                drawer.Add(byKey[key]);
                ctx.ReportInDrawer(byKey[key]);
            }

            if (layout.More.HasValue)
            {
                var more = layout.More.Value;
                var rect = new Rect(x0 + more.X, y, more.Width, h);
                if (ctx.Appearance.DrawMoreButton(rect, drawer.Count, Window_ButtonDrawer.IsShowing))
                    Window_ButtonDrawer.Toggle(rect);
            }
            else
            {
                Window_ButtonDrawer.CloseIfOpen();
            }

            foreach (var p in layout.Right) ctx.DrawButton(byKey[p.Key], new Rect(x0 + p.X, y, p.Width, h));
        }

        private void DrawScrolling(BarDrawContext ctx, LayoutResult layout, float x0, float y, float h)
        {
            var area = new Rect(x0 + layout.LeftArrowX, y, layout.RightArrowX + layout.ArrowWidth - layout.LeftArrowX, h);
            ctx.CaptureScrollWheel(area);

            var ev = Event.current;
            if (ev.type == EventType.ScrollWheel && Mouse.IsOver(area))
            {
                scrollOffset += (ev.delta.y + ev.delta.x) * WheelPixelsPerUnit;
                ev.Use();
            }

            float page = Mathf.Max(layout.ViewportWidth * 0.6f, 1f);
            if (ctx.Appearance.DrawScrollArrow(new Rect(x0 + layout.LeftArrowX, y, layout.ArrowWidth, h), left: true))
                scrollOffset -= page;
            if (ctx.Appearance.DrawScrollArrow(new Rect(x0 + layout.RightArrowX, y, layout.ArrowWidth, h), left: false))
                scrollOffset += page;

            scrollOffset = layout.ClampScroll(scrollOffset);

            var viewport = new Rect(x0 + layout.ViewportX, y, layout.ViewportWidth, h);
            GUI.BeginGroup(viewport);
            try
            {
                foreach (var p in layout.Center)
                {
                    float x = p.X - scrollOffset;
                    if (x + p.Width <= 0f || x >= viewport.width) ctx.ReportOnBarNotDrawn(byKey[p.Key]);
                    else ctx.DrawButton(byKey[p.Key], new Rect(x, 0f, p.Width, h));
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }
    }
}
