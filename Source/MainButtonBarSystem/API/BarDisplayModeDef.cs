using System;
using System.Collections.Generic;
using MainButtonBarSystem.Core;
using RimWorld;
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
        /// <summary>Where the mouse is moved for buttons it cannot reach, so they neither highlight nor react.</summary>
        private static readonly Vector2 Unreachable = new Vector2(-100000f, -100000f);

        private readonly List<LayoutItem> items = new List<LayoutItem>();
        private readonly List<LayoutItem> itemPool = new List<LayoutItem>();
        private readonly LayoutOptions options = new LayoutOptions();
        private readonly Dictionary<string, BarButton> byKey = new Dictionary<string, BarButton>();
        private readonly List<BarButton> drawer = new List<BarButton>();
        private float scrollOffset;

        private string menuLabel;
        private GameFont menuLabelFont;
        private float menuLabelWidth;

        /// <summary>Least width of the menu button when the equal split shrinks every button (tuning def).</summary>
        private int MenuSplitMinWidth(MainButtonDef menu, BarAppearanceWorker theme)
        {
            var b = MainButtonBarTuningDef.Current.behaviour;
            if (b.splitMenuMinWidth != 0 || menu == null) return Mathf.Max(0, b.splitMenuMinWidth);
            string label = menu.LabelCap;
            // The font the label is drawn in: a custom look may set its own (ButtonPainter.Draw).
            var labelFont = (theme != null && theme.ChangesButtons ? theme.LabelFontFor(menu) : null) ?? b.labelFont;
            if (!ReferenceEquals(label, menuLabel) || labelFont != menuLabelFont)
            {
                var font = Text.Font;
                Text.Font = labelFont;
                menuLabelWidth = Text.CalcSize(label).x;
                Text.Font = font;
                menuLabel = label;
                menuLabelFont = labelFont;
            }
            return Mathf.CeilToInt(menuLabelWidth + b.moreButtonLabelPadding);
        }

        public override void DrawBar(BarDrawContext ctx)
        {
            items.Clear();
            byKey.Clear();
            drawer.Clear();

            // Layout inputs are reused between GUI events instead of allocated for every button each time.
            int drawerCount = 0;
            foreach (var b in ctx.Buttons)
            {
                byKey[b.Key] = b;
                if (items.Count == itemPool.Count) itemPool.Add(new LayoutItem(b.Key));
                var item = itemPool[items.Count];
                item.Key = b.Key;
                item.Minimized = b.Minimized;
                item.Width = b.Width;
                item.Pin = b.Pin;
                item.InDrawer = b.InDrawer;
                item.SplitMinWidth = b.Key == BarModel.MenuDefName ? MenuSplitMinWidth(b.Def, ctx.Appearance) : 0;
                items.Add(item);
                if (b.InDrawer && def.usesDrawer) drawerCount++;
            }

            var config = ctx.Config;
            options.Mode = def.layout;
            options.BarWidth = (int)ctx.BarRect.width;
            options.DefaultWidth = config.DefaultWidth;
            options.MinWidth = config.MinWidth;
            options.Gap = config.Gap;
            options.FillScreen = config.FillScreen;
            options.MoreButtonWidth = Mathf.CeilToInt(ctx.Appearance.MoreButtonWidth(config, drawerCount));
            options.ArrowWidth = config.ArrowWidth;
            var tuning = MainButtonBarTuningDef.Current.behaviour;
            options.SplitMinWidth = tuning.splitMinButtonWidth;
            options.SplitMoreMinWidth = tuning.splitMoreMinWidth < 0 ? 0 : tuning.splitMoreMinWidth > 0 ? tuning.splitMoreMinWidth : options.MoreButtonWidth;
            var layout = BarLayout.Compute(items, options);

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
                scrollOffset += (ev.delta.y + ev.delta.x) * ctx.Config.WheelScrollPixels;
                ev.Use();
            }

            float page = Mathf.Max(layout.ViewportWidth * MainButtonBarTuningDef.Current.behaviour.arrowPageFraction, 1f);
            if (ctx.Appearance.DrawScrollArrow(new Rect(x0 + layout.LeftArrowX, y, layout.ArrowWidth, h), left: true))
                scrollOffset -= page;
            if (ctx.Appearance.DrawScrollArrow(new Rect(x0 + layout.RightArrowX, y, layout.ArrowWidth, h), left: false))
                scrollOffset += page;

            scrollOffset = layout.ClampScroll(scrollOffset);

            // Buttons are clipped to the viewport when drawn, but the game's hit tests are not: the hidden part of a
            // half-visible button would still highlight and take clicks under the arrows or the More button. While
            // the mouse is outside the viewport, move it out of reach for the buttons inside.
            var viewport = new Rect(x0 + layout.ViewportX, y, layout.ViewportWidth, h);
            bool reachable = Mouse.IsOver(viewport);

            GUI.BeginGroup(viewport);
            var saved = ev.mousePosition;
            if (!reachable) ev.mousePosition = Unreachable;
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
                if (!reachable) ev.mousePosition = saved;
                GUI.EndGroup();
            }
        }
    }
}
