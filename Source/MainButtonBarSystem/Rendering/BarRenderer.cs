using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Replaces MainButtonsRoot.DoButtons when the bar is customised. It resolves the user's settings into
    /// <see cref="BarButton"/>s and hands them to the active display mode, which draws through the active
    /// appearance. Every button is still drawn by its own MainButtonWorker.DoButton by default.
    /// </summary>
    public static class BarRenderer
    {
        private static readonly BarDrawContext ctx = new BarDrawContext();
        private static readonly List<BarButton> buttons = new List<BarButton>();
        private static readonly List<BarButton> pool = new List<BarButton>();
        private static readonly List<BarButton> drawer = new List<BarButton>();
        private static readonly List<MainButtonDef> vanillaVisible = new List<MainButtonDef>();
        private static readonly List<MainButtonDef> none = new List<MainButtonDef>();

        private static bool failed;

        /// <summary>Screen area where the mouse wheel scrolls the bar during the last draw; empty when nothing scrolls.</summary>
        public static Rect ScrollCaptureRect { get; private set; }

        /// <summary>Buttons in the "More" drawer as of the last draw.</summary>
        public static IReadOnlyList<BarButton> Drawer => drawer;

        public static BarAppearanceWorker Appearance => ctx.Appearance ?? BarConfigManager.Active.AppearanceWorker ?? new BarAppearanceWorker();

        /// <summary>The bar exactly where the game draws it (see the tuning def's behaviour section).</summary>
        public static Rect DefaultBarRect
        {
            get
            {
                var b = MainButtonBarTuningDef.Current.behaviour;
                return new Rect(0f, UI.screenHeight - b.barTopFromBottom, UI.screenWidth, b.buttonRectHeight);
            }
        }

        /// <summary>Draws the bar. Returns false when the game should draw it instead.</summary>
        public static bool TryDraw(MainButtonsRoot root)
        {
            ScrollCaptureRect = Rect.zero;
            if (failed)
            {
                SafeReportGameDrawn(root);
                return false;
            }

            BarConfig config;
            try
            {
                config = BarConfigManager.Active;
                LabelOverrides.Sync(config);
                bool forceOwn = MainButtonBarSystemMod.Settings?.alwaysDrawBar ?? false;
                if (CompatBridges.AnyControlsBar || (config.IsUntouched && !forceOwn))
                {
                    ReportGameDrawn(root);
                    return false;
                }
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Fail("Reading the main button bar settings failed; handing the bar back to the game for this session", e);
                SafeReportGameDrawn(root);
                return false;
            }

            try
            {
                Draw(config);
                return true;
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                Fail("Drawing the main button bar failed; handing it back to the game for this session", e);
                SafeReportGameDrawn(root);
                return false;
            }
        }

        private static bool reportBroken;

        /// <summary>After a failure the query API is updated if it can be; if that fails too, it is left alone.</summary>
        private static void SafeReportGameDrawn(MainButtonsRoot root)
        {
            if (reportBroken) return;
            try
            {
                ReportGameDrawn(root);
            }
            catch (Exception e) when (!(e is ExitGUIException))
            {
                reportBroken = true;
                ScrollCaptureRect = Rect.zero;
                drawer.Clear();
                if (Prefs.DevMode) Log.Message(MainButtonBarSystemMod.LogPrefix + "The query API is not updated after the failure above: " + e.Message);
            }
        }

        /// <summary>Records what the game (or another mod) shows, for the query API.</summary>
        public static void ReportGameDrawn(MainButtonsRoot root)
        {
            ScrollCaptureRect = Rect.zero;
            drawer.Clear();
            vanillaVisible.Clear();
            var list = root == null || BarModel.AllButtonsInOrder == null ? null : BarModel.AllButtonsInOrder(root);
            if (list != null)
            {
                foreach (var def in list)
                    if (def != null && BarModel.SafeVisible(def)) vanillaVisible.Add(def);
            }
            MainButtonBarAPI.Report(vanillaVisible, none, DefaultBarRect, ours: false);
        }

        private static void Fail(string what, Exception e)
        {
            if (failed) return;
            failed = true;
            Log.Error(MainButtonBarSystemMod.LogPrefix + what + ". " + e);
        }

        private static void Draw(BarConfig config)
        {
            BuildButtons(config);

            var mode = config.DisplayModeDef;
            ctx.Config = config;
            ctx.Buttons = buttons;
            ctx.BarRect = DefaultBarRect;
            ctx.Appearance = config.AppearanceWorker ?? new BarAppearanceWorker();
            ctx.Reset();

            GUI.color = Color.white;
            try
            {
                mode.Worker.DrawBar(ctx);
                // Decorations above the buttons, whatever display mode drew them: the same rect the standard mode
                // passes to DrawBarBackground, so both layers anchor alike.
                ctx.Appearance.DrawBarOverlay(ctx.BarRect);
            }
            finally
            {
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            ScrollCaptureRect = ctx.ScrollCaptureRect;
            drawer.Clear();
            foreach (var def in ctx.ReportedInDrawer)
            {
                foreach (var b in buttons)
                {
                    if (b.Def == def)
                    {
                        drawer.Add(b);
                        break;
                    }
                }
            }
            MainButtonBarAPI.Report(ctx.ReportedOnBar, ctx.ReportedInDrawer, ctx.ReportedBarRect, ours: true);
        }

        /// <summary>Resolves the config into the buttons to show, in screen order.</summary>
        private static void BuildButtons(BarConfig config)
        {
            buttons.Clear();
            bool drawerMode = config.DisplayModeDef?.usesDrawer ?? false;
            int n = 0;
            foreach (var name in BarModel.EffectiveOrder(config))
            {
                var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
                if (def == null || !BarModel.ShouldShow(config, def)) continue;

                if (n == pool.Count) pool.Add(new BarButton());
                var b = pool[n++];
                var o = config.Get(name);
                b.Def = def;
                b.Settings = o;
                b.Pin = BarModel.PinOf(config, name);
                b.InDrawer = drawerMode && b.Pin == Core.PinPosition.None && o.inDrawer;
                b.Minimized = def.minimized;
                b.Width = o.width > 0 ? o.width : (int?)null;
                buttons.Add(b);
            }
        }
    }
}
