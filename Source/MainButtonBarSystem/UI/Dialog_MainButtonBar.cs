using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// The settings window (spec 3.1, 3.2). It edits a working copy that the bar draws live; Save stores it,
    /// Cancel or closing throws it away (spec 4.7). Every adjustable value has a "Default" button, and every
    /// layout number comes from <see cref="MainButtonBarTuningDef"/>.
    /// </summary>
    public partial class Dialog_MainButtonBar : Window
    {
        private const string BarKey = "<bar>";

        /// <summary>Pages of the window. Simple / Advanced is not a page but a switch on the Buttons page.</summary>
        private enum Tab
        {
            Buttons,
            Appearance,
        }

        private static Tab tab = Tab.Buttons;

        // Text of each number box while it has focus, keyed by the setting it edits.
        /// <summary>What each number box shows while it has focus, and the value that text was made for.</summary>
        private static readonly Dictionary<string, (string text, float value)> numberBuffers = new Dictionary<string, (string text, float value)>();

        private readonly bool editingTemplate;
        private BarConfig working;
        private string selected = BarKey;
        private Vector2 listScroll;
        private bool showOriginallyHidden;

        /// <summary>The source mod whose buttons the list shows, or null for every button; kept while the game runs.</summary>
        private static string sourceFilter;

        private readonly List<string> mainRows = new List<string>();
        private readonly List<string> hiddenRows = new List<string>();

        // The rows the list shows: mainRows and hiddenRows narrowed to the source filter, in the same order.
        private readonly List<string> shownRows = new List<string>();
        private readonly List<string> shownHiddenRows = new List<string>();

        private float barDetailHeight;
        private Vector2 barDetailScroll;
        private float buttonDetailHeight;
        private Vector2 buttonDetailScroll;

        // Dragging the window by its title row only.
        private bool dragging;
        private Vector2 dragStartMouse;
        private Vector2 dragStartWindow;

        private static MainButtonBarTuningDef T => MainButtonBarTuningDef.Current;
        private static MainButtonBarTuningDef.SettingsWindowLayout L => T.settingsWindow;
        private static MainButtonBarTuningDef.SettingRanges R => T.ranges;
        private static MainButtonBarTuningDef.BarDefaults D => T.barDefaults;

        /// <summary>The Simple / Advanced switch, shared by every save and remembered (mod settings).</summary>
        private static bool Advanced => MainButtonBarSystemMod.Settings.showAdvanced;

        // Button widths: the tuning def's minimum, widened to fit the label in the current language.
        private static float ResetWidth => FitButton(L.resetButtonWidth, "MBBS_Default");
        private static float SmallButtonWidth => Mathf.Max(FitButton(L.smallButtonWidth, "MBBS_HotKeySet"), FitButton(L.smallButtonWidth, "MBBS_HotKeyClear"));
        private static float MoveButtonWidth => Mathf.Max(FitButton(L.moveButtonWidth, "MBBS_MoveUp"), FitButton(L.moveButtonWidth, "MBBS_MoveDown"));

        private static float FitButton(float minimum, string key)
        {
            var font = Text.Font;
            Text.Font = GameFont.Small;
            float width = Text.CalcSize(key.Translate()).x + L.buttonTextPadding * 2f;
            Text.Font = font;
            return Mathf.Max(minimum, width);
        }

        private Vector2 themeScroll;
        private float themeListHeight;
        private readonly List<TabRecord> tabs = new List<TabRecord>();

        // Column widths of the button list, widened to fit their titles in the current language.
        private float visibleColumn, iconColumn, hotKeyColumn, drawerColumn;

        public override Vector2 InitialSize =>
            new Vector2(Mathf.Min(L.maxSize.x, UI.screenWidth - L.screenMargin), Mathf.Min(L.maxSize.y, UI.screenHeight - L.screenMargin));

        public Dialog_MainButtonBar()
        {
            editingTemplate = !BarConfigManager.InGame;
            doCloseX = true;
            // Enter is handled here (it ends number editing) instead of reaching the game's mod settings window below.
            closeOnAccept = true;
            closeOnCancel = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            // Only the title row moves the window (DragByTitle); elsewhere a held mouse paints checkboxes.
            draggable = false;
            onlyOneOfTypeAllowed = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            numberBuffers.Clear();
            CustomThemeFiles.Refresh();
            Window_ButtonDrawer.CloseIfOpen();
            working = BarConfigManager.Saved.Clone();
            BarConfigManager.Preview = working;
        }

        /// <summary>Enter ends typing in a box; it never closes this window or the one below it.</summary>
        public override void OnAcceptKeyPressed()
        {
            UI.UnfocusCurrentControl();
            Event.current.Use();
        }

        /// <summary>A click on the map or the bar ends typing too, so later keys do not change a number.</summary>
        public override void Notify_ClickOutsideWindow()
        {
            base.Notify_ClickOutsideWindow();
            UI.UnfocusCurrentControl();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (BarConfigManager.Preview == working) BarConfigManager.Preview = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            BuildRows();
            MeasureColumns();

            var header = new Rect(inRect.x, inRect.y, inRect.width, L.headerHeight);
            DrawHeader(header);

            float noticeHeight = DrawCompatNotices(new Rect(inRect.x, header.yMax, inRect.width, 0f));
            float tabsTop = header.yMax + noticeHeight + L.footerGap;
            var content = new Rect(inRect.x, tabsTop + TabDrawer.TabHeight, inRect.width, inRect.yMax - tabsTop - TabDrawer.TabHeight);
            DrawTabs(content);

            Widgets.DrawMenuSection(content);
            var body = content.ContractedBy(L.detailPadding);
            if (tab == Tab.Appearance)
            {
                DrawAppearance(body);
            }
            else
            {
                var switchRow = new Rect(body.x, body.y, body.width, L.textRowHeight);
                DrawViewSwitch(switchRow);
                float top = switchRow.yMax + L.footerGap;
                var left = new Rect(body.x, top, Mathf.Floor(body.width * L.listFraction), body.yMax - top);
                float filterX = switchRow.x + Mathf.Min(L.viewSwitchWidth, switchRow.width) + L.panelGap;
                DrawSourceFilter(new Rect(filterX, switchRow.y, Mathf.Min(L.sourceFilterWidth, left.xMax - filterX), switchRow.height));
                var right = new Rect(left.xMax + L.panelGap, top, body.width - left.width - L.panelGap, body.yMax - top);
                DrawList(left);
                DrawRight(right);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // The title row and the free part of the tab strip move the window.
            DragByTitle(new Rect(inRect.x, inRect.y, inRect.width, tabsTop + TabDrawer.TabHeight - inRect.y));
        }

        private void DrawTabs(Rect content)
        {
            tabs.Clear();
            tabs.Add(new TabRecord("MBBS_Tab_Buttons".Translate(), () => tab = Tab.Buttons, tab == Tab.Buttons));
            tabs.Add(new TabRecord("MBBS_Tab_Appearance".Translate(), () => tab = Tab.Appearance, tab == Tab.Appearance));
            TabDrawer.DrawTabs(content, tabs, L.tabButtonWidth);
        }

        /// <summary>Two-part Simple | Advanced switch; the chosen part is filled so the state is obvious.</summary>
        private static void DrawViewSwitch(Rect row)
        {
            var area = new Rect(row.x, row.y, Mathf.Min(L.viewSwitchWidth, row.width), row.height);
            float half = Mathf.Floor(area.width / 2f);
            DrawViewPart(new Rect(area.x, area.y, half, area.height), "MBBS_View_Simple".Translate(), false);
            DrawViewPart(new Rect(area.x + half, area.y, area.width - half, area.height), "MBBS_View_Advanced".Translate(), true);
            TooltipHandler.TipRegion(area, "MBBS_ViewTip".Translate());
        }

        private static void DrawViewPart(Rect rect, string label, bool advanced)
        {
            bool chosen = Advanced == advanced;
            Widgets.DrawBoxSolid(rect, chosen ? L.viewSwitchSelectedColor : L.viewSwitchColor);
            Widgets.DrawBox(rect);
            if (!chosen) Widgets.DrawHighlightIfMouseover(rect);
            var color = GUI.color;
            GUI.color = chosen ? Color.white : Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = color;
            if (!chosen && Widgets.ButtonInvisible(rect))
            {
                MainButtonBarSystemMod.Settings.showAdvanced = advanced;
                MainButtonBarSystemMod.Instance.WriteSettings();
            }
        }

        private void DragByTitle(Rect titleArea)
        {
            var ev = Event.current;
            var mouse = UI.MousePositionOnUIInverted;
            if (dragging)
            {
                if (ev.rawType == EventType.MouseUp || !Input.GetMouseButton(0))
                {
                    dragging = false;
                    return;
                }
                windowRect.position = dragStartWindow + (mouse - dragStartMouse);
                if (ev.type == EventType.MouseDrag) ev.Use();
                return;
            }

            // Reaching here with an unused mouse-down means no control (a tab, the close button) took it.
            if (ev.type == EventType.MouseDown && ev.button == 0 && titleArea.Contains(ev.mousePosition))
            {
                dragging = true;
                dragStartMouse = mouse;
                dragStartWindow = windowRect.position;
                ev.Use();
            }
        }

        // ---------------------------------------------------------------- header

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width - L.closeButtonSpace, rect.height),
                (editingTemplate ? "MBBS_Title_Template" : "MBBS_Title_Save").Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// One line per detected bar-takeover mod (spec 3.7), then the notices of overlapping mods that are left alone
        /// (wrapped, since they explain what to change). Returns the height used.
        /// </summary>
        private float DrawCompatNotices(Rect at)
        {
            float y = at.y;
            foreach (var bridge in CompatBridges.Detected)
            {
                var worker = bridge.Worker;
                string text = worker.SessionPriority == CompatPriority.Theirs
                    ? "MBBS_Compat_TheirsNotice".TranslateKeepCase(bridge.ModNameOrLabel)
                    : "MBBS_Compat_OursNotice".TranslateKeepCase(bridge.ModNameOrLabel);
                if (!Advanced) text += " " + "MBBS_Compat_SeeAdvanced".Translate();
                var line = new Rect(at.x, y, at.width, L.noticeLineHeight);
                GUI.color = ColorLibrary.Gold;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(line, text.Truncate(line.width));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                TooltipHandler.TipRegion(line, text);
                y += line.height;
            }

            foreach (var def in CompatNotices.Detected)
            {
                var notice = CompatNotices.NoticeFor(def, out bool settingOn);
                if (notice == null) continue;
                string text = notice.textKey.Translate(def.ModNameOrLabel);
                string turnOff = settingOn && !notice.turnOffKey.NullOrEmpty() ? notice.turnOffKey.Translate(def.ModNameOrLabel).ToString() : null;
                float buttonWidth = turnOff == null ? 0f : Mathf.Min(UIUtil.FitButton(L.smallButtonWidth, turnOff), at.width * L.noticeButtonMaxFraction);
                float textWidth = turnOff == null ? at.width : at.width - buttonWidth - L.footerGap;
                float height = Mathf.Max(L.noticeLineHeight, Text.CalcHeight(text, textWidth));
                GUI.color = ColorLibrary.Gold;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(at.x, y, textWidth, height), text);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                // Level with the middle of the text, like the label.
                var button = new Rect(at.xMax - buttonWidth, y + (height - L.noticeLineHeight) / 2f, buttonWidth, L.noticeLineHeight);
                if (turnOff != null && UIUtil.ButtonText(button, turnOff))
                    TurnOff(def);
                y += height;
            }
            return y - at.y;
        }

        /// <summary>The notice's button: turns the other mod's switch off and saves its settings.</summary>
        private static void TurnOff(CompatNoticeDef def)
        {
            bool done = def.TryTurnOff(out string problem);
            if (problem != null && Prefs.DevMode)
                Log.Message(MainButtonBarSystemMod.LogPrefix + def.ModNameOrLabel + ": turning off " + def.SettingPath + ": " + problem);
            if (!done)
                Messages.Message("MBBS_Notice_TurnOffFailed".Translate(def.ModNameOrLabel), MessageTypeDefOf.RejectInput, false);
            else if (problem != null)
                Messages.Message("MBBS_Notice_TurnedOffNotSaved".Translate(def.ModNameOrLabel), MessageTypeDefOf.CautionInput, false);
            else
                Messages.Message("MBBS_Notice_TurnedOff".Translate(def.ModNameOrLabel), MessageTypeDefOf.NeutralEvent, false);
        }

        // ---------------------------------------------------------------- appearance tab

        /// <summary>
        /// Theme cards with previews (themes come from this mod and from theme mods), then the illustration settings
        /// of the chosen theme; one scrolled column.
        /// </summary>
        private void DrawAppearanceTab(Rect rect)
        {
            var listing = BeginScrolledListing(rect, ref themeScroll, themeListHeight);
            if (SectionHeader(listing, "MBBS_ThemeHeader".Translate(), working.appearance != BarConfig.VanillaAppearance, "MBBS_ThemeHeaderTip".Translate()))
                working.appearance = BarConfig.VanillaAppearance;
            if (Prefs.DevMode) DrawReloadThemesButton(listing);
            listing.Gap(L.footerGap);
            foreach (var theme in BarModel.Appearances.ToList())
            {
                DrawThemeCard(listing.GetRect(L.themeCardHeight), theme);
                if (theme == working.AppearanceDef) DrawSuggestedLayout(listing, theme);
                listing.Gap(L.footerGap);
            }
            DrawSavedThemes(listing);
            listing.Gap(L.themeSettingsGap);
            DrawIllustrationSettings(listing);
            themeListHeight = EndScrolledListing(listing);
        }

        /// <summary>The bar settings the layout uses now, as a theme's suggestion compares them.</summary>
        private LayoutValues CurrentLayout() => new LayoutValues
        {
            Mode = working.displayMode ?? BarConfig.VanillaMode,
            DefaultWidth = working.DefaultWidth,
            MinWidth = working.MinWidth,
            MinimizedFullWidth = working.MinimizedFullWidth,
        };

        /// <summary>
        /// A theme's suggested layout, while the bar differs from it: a hint and a button that applies it. The theme
        /// never changes the bar by itself; every changed setting keeps its own Default button on the Buttons tab.
        /// </summary>
        private void DrawSuggestedLayout(Listing_Standard listing, BarAppearanceDef theme)
        {
            var s = theme.suggestedLayout;
            if (s == null) return;
            var modes = s.displayModes?.Where(m => DefDatabase<BarDisplayModeDef>.GetNamedSilentFail(m) != null).ToList();
            var now = CurrentLayout();
            var after = LayoutSuggestion.Apply(now, modes, s.buttonWidth, s.halfButtonsFullWidth);
            if (now.SameAs(after)) return;

            var changes = new List<string>();
            if (after.Mode != now.Mode) changes.Add("MBBS_DisplayMode".Translate() + ": " + ModeLabel(after.Mode));
            if (after.DefaultWidth != now.DefaultWidth) changes.Add("MBBS_DefaultWidth".Translate(after.DefaultWidth));
            if (after.MinWidth != now.MinWidth) changes.Add("MBBS_MinWidth".Translate(after.MinWidth));
            if (after.MinimizedFullWidth != now.MinimizedFullWidth) changes.Add("MBBS_MinimizedFullWidth".Translate());

            listing.Gap(L.footerGap);
            GUI.color = L.reportWarningColor;
            listing.Label(s.buttonWidth > 0 ? "MBBS_Suggest_Hint".Translate(theme.LabelCap, s.buttonWidth) : "MBBS_Suggest_HintNoWidth".Translate(theme.LabelCap));
            GUI.color = Color.white;
            var row = listing.GetRect(L.textRowHeight);
            string apply = "MBBS_Suggest_Apply".Translate();
            var button = new Rect(row.x, row.y, UIUtil.FitButton(L.moveButtonWidth, apply), row.height);
            TooltipHandler.TipRegion(button, "MBBS_Suggest_Tip".Translate() + "\n" + string.Join("\n", changes.Select(c => "  • " + c)));
            if (!Widgets.ButtonText(button, apply)) return;
            working.displayMode = after.Mode;
            if (after.DefaultWidth != now.DefaultWidth) working.defaultWidth = after.DefaultWidth;
            if (after.MinWidth != now.MinWidth) working.minWidth = after.MinWidth;
            if (after.MinimizedFullWidth != now.MinimizedFullWidth) working.minimizedFullWidth = BarConfig.Store(after.MinimizedFullWidth, D.minimizedFullWidth);
        }

        /// <summary>The player's saved custom themes, after the installed ones (spec 3.2): picking one copies it in.</summary>
        private void DrawSavedThemes(Listing_Standard listing)
        {
            var saved = CustomThemeFiles.All;
            if (saved.Count == 0) return;
            listing.Label("MBBS_SavedThemes".Translate());
            foreach (var theme in saved)
            {
                DrawSavedThemeCard(listing.GetRect(L.themeCardHeight), theme);
                listing.Gap(L.footerGap);
            }
        }

        private void DrawSavedThemeCard(Rect card, SavedCustomTheme saved)
        {
            Widgets.DrawMenuSection(card);
            Widgets.DrawHighlightIfMouseover(card);
            var inner = card.ContractedBy(L.detailPadding);
            float previewWidth = Mathf.Floor(inner.width * L.themePreviewFraction);
            var preview = new Rect(inner.xMax - previewWidth, inner.y + (inner.height - L.themePreviewHeight) / 2f, previewWidth, L.themePreviewHeight);
            var text = new Rect(inner.x, inner.y, inner.width - previewWidth - L.panelGap, inner.height);

            float tx = text.x + L.checkboxSize + L.footerGap;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(tx, text.y, text.xMax - tx, Text.LineHeight), saved.Name);
            Text.Font = GameFont.Small;
            float ty = text.y + Text.LineHeightOf(GameFont.Medium);

            var baseDef = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(saved.baseTheme);
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            string by = saved.meta?.author.NullOrEmpty() == false ? "MBBS_ImportReport_By".TranslateKeepCase(saved.meta.author) + "  " : "";
            string from = baseDef != null ? "MBBS_SavedTheme_Base".TranslateKeepCase(baseDef.LabelCap.ToString()) : "MBBS_SavedTheme_BaseMissingShort".TranslateKeepCase(saved.baseTheme);
            Widgets.Label(new Rect(tx, ty, text.xMax - tx, Text.LineHeight), by + from);
            ty += Text.LineHeight;
            if (!saved.meta?.description.NullOrEmpty() == true)
                Widgets.Label(new Rect(tx, ty, text.xMax - tx, text.yMax - ty), saved.meta.description);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // The starting theme's picture: the saved look's own PNGs are only copied in when it is picked.
            if (baseDef != null)
            {
                var image = baseDef.Tex(baseDef.preview);
                if (image != null) Widgets.DrawTextureFitted(preview, image, 1f);
                else DrawThemePreview(baseDef, preview);
            }
            TooltipHandler.TipRegion(card, "MBBS_SavedTheme_Tip".Translate());
            if (Widgets.ButtonInvisible(card)) ApplySavedTheme(saved);
        }

        private void DrawThemeCard(Rect card, BarAppearanceDef theme)
        {
            bool selected = (working.appearance.NullOrEmpty() ? BarConfig.VanillaAppearance : working.appearance) == theme.defName;
            Widgets.DrawMenuSection(card);
            if (selected) Widgets.DrawHighlightSelected(card);
            else Widgets.DrawHighlightIfMouseover(card);

            var inner = card.ContractedBy(L.detailPadding);
            float previewWidth = Mathf.Floor(inner.width * L.themePreviewFraction);
            var preview = new Rect(inner.xMax - previewWidth, inner.y + (inner.height - L.themePreviewHeight) / 2f, previewWidth, L.themePreviewHeight);
            var text = new Rect(inner.x, inner.y, inner.width - previewWidth - L.panelGap, inner.height);

            bool clicked = Widgets.RadioButton(text.x, text.y, selected);
            float tx = text.x + L.checkboxSize + L.footerGap;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(tx, text.y, text.xMax - tx, Text.LineHeight), theme.LabelCap);
            Text.Font = GameFont.Small;
            float ty = text.y + Text.LineHeightOf(GameFont.Medium);

            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            string source = theme.IsFromInactiveProject ? "MBBS_ThemeProject_Source".Translate().ToString() : theme.modContentPack?.Name;
            string by = theme.author.NullOrEmpty() ? source : "MBBS_ThemeBy".TranslateKeepCase(theme.author, source ?? "?").ToString();
            if (!by.NullOrEmpty())
            {
                Widgets.Label(new Rect(tx, ty, text.xMax - tx, Text.LineHeight), by);
                ty += Text.LineHeight;
            }
            if (!theme.description.NullOrEmpty())
                Widgets.Label(new Rect(tx, ty, text.xMax - tx, text.yMax - ty), theme.description);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            var image = theme.Tex(theme.preview);
            if (image != null) Widgets.DrawTextureFitted(preview, image, 1f);
            else DrawThemePreview(theme, preview);

            if (clicked | Widgets.ButtonInvisible(card)) working.appearance = theme.defName;
        }

        /// <summary>
        /// A still picture of a theme from its own textures: normal, mouse-over, active and disabled buttons, then More. It does
        /// not run the game's button code, so it works at the main menu too.
        /// </summary>
        private static void DrawThemePreview(BarAppearanceDef theme, Rect rect)
        {
            var worker = theme.Worker;
            // The image only, at full opacity: decorations would grow out of the card (themes with decorations should
            // set a preview), and the player's opacity setting should not fade every card.
            worker.DrawBarBackgroundImage(rect, 1f);
            var samples = ButtonBaseline.All.Where(b => b.ButtonVisible && !b.Label.NullOrEmpty()).Take(L.themePreviewButtons)
                .Select(b => (defName: b.DefName, label: b.Label.CapitalizeFirst())).ToList();

            // Layout as on the bar: < [buttons] > [More].
            float arrow = Mathf.Min(D.arrowWidth, rect.width / 8f);
            int count = samples.Count + 1;
            float w = (rect.width - arrow * 2f) / count;
            DrawPreviewArrow(theme, new Rect(rect.x, rect.y, arrow, rect.height), true);
            DrawPreviewArrow(theme, new Rect(rect.x + arrow + w * samples.Count, rect.y, arrow, rect.height), false);

            // One sample per state; disabled buttons show no label on the bar either.
            var states = new[] { ButtonVisualState.Normal, ButtonVisualState.Mouseover, ButtonVisualState.Active, ButtonVisualState.Disabled };
            for (int i = 0; i < count; i++)
            {
                bool isMore = i == count - 1;
                float x = rect.x + arrow + w * i + (isMore ? arrow : 0f);
                var r = new Rect(x, rect.y, w, rect.height);
                var state = i < states.Length ? states[i] : ButtonVisualState.Normal;
                bool more = isMore;
                var atlas = more ? theme.Tex(theme.moreButton) : null;
                bool replacesTint = atlas != null;
                atlas ??= worker.ButtonAtlas(state, out replacesTint) ?? Widgets.ButtonSubtleAtlas;
                // The sample's own picture first, as on the bar (ThemeAtlasSwap), then its frame.
                var image = more ? null : worker.ButtonImage(DefDatabase<MainButtonDef>.GetNamedSilentFail(samples[i].defName));
                if (image != null) worker.DrawButtonImage(r, image);
                var color = GUI.color;
                if ((state == ButtonVisualState.Mouseover || state == ButtonVisualState.Active) && !replacesTint) GUI.color = GenUI.MouseoverColor;
                Widgets.DrawAtlas(r, atlas);
                GUI.color = color;
                if (state == ButtonVisualState.Disabled) continue;
                string label = more ? worker.MoreButtonLabel(samples.Count) : samples[i].label;
                ButtonPainter.DrawLabel(r, label.Truncate(r.width), worker.LabelColor(state) ?? Color.white, TextAnchor.MiddleCenter, state != ButtonVisualState.Normal);
            }
        }

        private static void DrawPreviewArrow(BarAppearanceDef theme, Rect r, bool left)
        {
            var atlas = theme.Worker.ButtonAtlas(ButtonVisualState.Normal, out _) ?? Widgets.ButtonSubtleAtlas;
            Widgets.DrawAtlas(r, atlas);
            var icon = theme.Tex(left ? theme.arrowLeft : theme.arrowRight);
            if (icon != null)
            {
                float size = Mathf.Min(r.width, r.height);
                Widgets.DrawTextureFitted(new Rect(r.center.x - size / 2f, r.center.y - size / 2f, size, size), icon, 1f);
            }
            else
            {
                ButtonPainter.DrawLabel(r, left ? "<" : ">", theme.Worker.LabelColor(ButtonVisualState.Normal) ?? Color.white, TextAnchor.MiddleCenter, false);
            }
        }

        /// <summary>Settings applied on top of the chosen theme's illustrations (spec 2026-11-04 §3.4); every row has a Default button.</summary>
        private void DrawIllustrationSettings(Listing_Standard listing)
        {
            listing.GapLine();
            listing.Label("MBBS_Section_Illustration".Translate());
            var theme = working.AppearanceDef;
            bool hasBackground = theme != null && (!theme.barBackground.NullOrEmpty() || !theme.drawerBackground.NullOrEmpty());
            bool hasDecorations = theme != null && theme.HasDecorations;
            if (!hasBackground && !hasDecorations)
            {
                Note(listing, "MBBS_IllustrationNone".Translate());
                return;
            }
            if (hasBackground)
                working.backgroundOpacity = IntSetting(listing, "MBBS_BackgroundOpacity".Translate(working.BackgroundOpacityPercent), working.backgroundOpacity, working.BackgroundOpacityPercent, R.backgroundOpacity, R.percentStep);
            if (!hasDecorations) return;
            working.decorationsShown = BoolSetting(listing, "MBBS_DecorationsShown".Translate(), working.decorationsShown, D.decorationsShown, "MBBS_DecorationsShownTip".Translate());
            working.decorationOpacity = IntSetting(listing, "MBBS_DecorationOpacity".Translate(working.DecorationOpacityPercent), working.decorationOpacity, working.DecorationOpacityPercent, R.decorationOpacity, R.percentStep);
            working.decorationScale = IntSetting(listing, "MBBS_DecorationScale".Translate(working.DecorationScalePercent), working.decorationScale, working.DecorationScalePercent, R.decorationScale, R.percentStep);
            working.decorationOffsetX = OffsetSetting(listing, "MBBS_DecorationOffsetX".Translate(working.DecorationOffsetX), working.decorationOffsetX, working.DecorationOffsetX, R.decorationOffset);
            working.decorationOffsetY = OffsetSetting(listing, "MBBS_DecorationOffsetY".Translate(working.DecorationOffsetY), working.decorationOffsetY, working.DecorationOffsetY, R.decorationOffset);
        }

        /// <summary>Development mode: reads every theme's PNG files again (<see cref="BarAppearanceDef.ReloadTextures"/>), then checks the themes.</summary>
        private static void DrawReloadThemesButton(Listing_Standard listing)
        {
            var row = listing.GetRect(L.textRowHeight);
            string label = "MBBS_ReloadThemes".Translate();
            var rect = new Rect(row.x, row.y, UIUtil.FitButton(L.moveButtonWidth, label), row.height);
            TooltipHandler.TipRegion(rect, "MBBS_ReloadThemesTip".Translate());
            if (!Widgets.ButtonText(rect, label)) return;
            int count = 0;
            foreach (var theme in BarModel.Appearances) count += theme.ReloadTextures();
            CustomThemes.ResetCache();
            ThemeValidator.ValidateAll();
            Messages.Message("MBBS_ThemesReloaded".Translate(count), MessageTypeDefOf.NeutralEvent, false);
        }

        // ---------------------------------------------------------------- list

        private void BuildRows()
        {
            mainRows.Clear();
            hiddenRows.Clear();
            foreach (var name in BarModel.EffectiveOrder(working))
            {
                if (BarModel.OriginallyHidden(name) && !working.Get(name).forceShow) hiddenRows.Add(name);
                else mainRows.Add(name);
            }
            if (selected != BarKey && !mainRows.Contains(selected) && !hiddenRows.Contains(selected)) selected = BarKey;

            FillShownRows();
            // A filter for a mod whose buttons are all gone (another save, a mod removed) shows every button again.
            if (sourceFilter != null && shownRows.Count == 0 && shownHiddenRows.Count == 0)
            {
                sourceFilter = null;
                FillShownRows();
            }
        }

        private void FillShownRows()
        {
            shownRows.Clear();
            shownHiddenRows.Clear();
            foreach (var name in mainRows) if (Shown(name)) shownRows.Add(name);
            foreach (var name in hiddenRows) if (Shown(name)) shownHiddenRows.Add(name);
        }

        private static bool Shown(string name) => sourceFilter == null || SourceOf(name) == sourceFilter;

        /// <summary>The mod a button comes from, as the list's source column shows it.</summary>
        private static string SourceOf(string name) => ButtonBaseline.Get(name)?.SourceModName ?? "?";

        /// <summary>A drop-down above the list that narrows it to the buttons of one mod.</summary>
        private void DrawSourceFilter(Rect rect)
        {
            if (rect.width <= 0f) return;
            string current = sourceFilter ?? "MBBS_SourceFilter_All".Translate().ToString();
            string label = "MBBS_SourceFilter".TranslateKeepCase(current);
            TooltipHandler.TipRegion(rect, "MBBS_SourceFilterTip".Translate());
            if (!UIUtil.ButtonText(rect, label.Truncate(rect.width - L.buttonTextPadding * 2f))) return;

            var counts = new Dictionary<string, int>();
            foreach (var name in mainRows.Concat(hiddenRows))
            {
                string mod = SourceOf(name);
                counts[mod] = counts.TryGetValue(mod, out int n) ? n + 1 : 1;
            }
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("MBBS_SourceFilter_All".Translate() + " (" + (mainRows.Count + hiddenRows.Count) + ")", () => sourceFilter = null),
            };
            foreach (var pair in counts.OrderBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase))
            {
                string mod = pair.Key;
                options.Add(new FloatMenuOption(mod + " (" + pair.Value + ")", () => sourceFilter = mod));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private struct Columns
        {
            public Rect Handle, Visible, Icon, Name, Source, HotKey, Drawer;
        }

        /// <summary>Each narrow column is at least as wide as its title in the current language.</summary>
        private void MeasureColumns()
        {
            var font = Text.Font;
            Text.Font = GameFont.Tiny;
            float Fit(float minimum, string key) => Mathf.Max(minimum, Text.CalcSize(key.Translate()).x + L.textInset * 2f);
            visibleColumn = Fit(L.visibleColumn, "MBBS_Col_Visible");
            iconColumn = Fit(L.iconColumn, "MBBS_Col_Icon");
            hotKeyColumn = Fit(L.hotKeyColumn, "MBBS_Col_HotKey");
            drawerColumn = Fit(L.drawerColumn, "MBBS_Col_Drawer");
            Text.Font = font;
        }

        private Columns ColumnsFor(Rect row)
        {
            float hotKey = Advanced ? hotKeyColumn : 0f;
            float source = Mathf.Min(L.sourceColumnMax, row.width * L.sourceColumnFraction);
            float name = row.width - L.handleColumn - visibleColumn - iconColumn - source - hotKey - drawerColumn;

            var c = new Columns();
            float x = row.x;
            c.Handle = new Rect(x, row.y, L.handleColumn, row.height); x += L.handleColumn;
            c.Visible = new Rect(x, row.y, visibleColumn, row.height); x += visibleColumn;
            c.Icon = new Rect(x, row.y, iconColumn, row.height); x += iconColumn;
            c.Name = new Rect(x, row.y, name, row.height); x += name;
            c.Source = new Rect(x, row.y, source, row.height); x += source;
            c.HotKey = new Rect(x, row.y, hotKey, row.height); x += hotKey;
            c.Drawer = new Rect(x, row.y, drawerColumn, row.height);
            return c;
        }

        private void DrawList(Rect rect)
        {
            var buttonsRow = new Rect(rect.x, rect.yMax - L.footerButtonHeight, rect.width, L.footerButtonHeight);
            var frame = new Rect(rect.x, rect.y, rect.width, rect.height - L.footerButtonHeight - L.footerGap);
            Widgets.DrawMenuSection(frame);
            var inner = frame.ContractedBy(L.listFramePadding);

            // Column titles.
            var titles = ColumnsFor(new Rect(inner.x, inner.y, inner.width - GenUI.ScrollBarWidth, L.columnHeaderHeight));
            Text.Font = GameFont.Tiny;
            Text.WordWrap = false;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titles.Visible, "MBBS_Col_Visible".Translate());
            Widgets.Label(titles.Icon, "MBBS_Col_Icon".Translate());
            Widgets.Label(titles.Drawer, "MBBS_Col_Drawer".Translate());
            if (Advanced) Widgets.Label(titles.HotKey, "MBBS_Col_HotKey".Translate());
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(titles.Name.x + L.textInset, titles.Name.y, titles.Name.width - L.textInset, titles.Name.height), "MBBS_Col_Name".Translate());
            Widgets.Label(titles.Source, "MBBS_Col_Source".Translate());
            GUI.color = Color.white;
            Text.WordWrap = true;
            Text.Font = GameFont.Small;

            float rowH = L.rowHeight;
            var outRect = new Rect(inner.x, inner.y + L.columnHeaderHeight, inner.width, inner.height - L.columnHeaderHeight);
            float height = rowH * (1 + shownRows.Count);
            if (shownHiddenRows.Count > 0) height += rowH + (showOriginallyHidden ? rowH * shownHiddenRows.Count : 0f);
            var viewRect = new Rect(0f, 0f, outRect.width - GenUI.ScrollBarWidth, height);

            Widgets.BeginScrollView(outRect, ref listScroll, viewRect);
            float y = 0f;
            DrawBarRow(new Rect(0f, y, viewRect.width, rowH));
            y += rowH;

            var groupRect = new Rect(0f, y, viewRect.width, rowH * shownRows.Count);
            int group = ReorderableWidget.NewGroup(OnReorder, ReorderableDirection.Vertical, groupRect);
            for (int i = 0; i < shownRows.Count; i++)
            {
                var row = new Rect(0f, y, viewRect.width, rowH);
                ReorderableWidget.Reorderable(group, ColumnsFor(row).Handle);
                DrawButtonRow(row, shownRows[i], i);
                y += rowH;
            }

            if (shownHiddenRows.Count > 0)
            {
                var toggle = new Rect(0f, y, viewRect.width, rowH);
                Widgets.DrawHighlightIfMouseover(toggle);
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(toggle.x + L.handleColumn, toggle.y, toggle.width - L.handleColumn, toggle.height),
                    (showOriginallyHidden ? "▼ " : "▶ ") + "MBBS_OriginallyHidden".Translate(shownHiddenRows.Count));
                GUI.color = Color.white;
                TooltipHandler.TipRegion(toggle, "MBBS_OriginallyHiddenTip".Translate());
                if (Widgets.ButtonInvisible(toggle)) showOriginallyHidden = !showOriginallyHidden;
                y += rowH;

                if (showOriginallyHidden)
                {
                    foreach (var name in shownHiddenRows)
                    {
                        DrawButtonRow(new Rect(0f, y, viewRect.width, rowH), name, -1);
                        y += rowH;
                    }
                }
            }
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;

            // Move buttons. Top to bottom in the list is left to right on screen.
            int index = shownRows.IndexOf(selected);
            bool movable = index >= 0 && !BarModel.IsLocked(working, selected);
            var up = new Rect(buttonsRow.x, buttonsRow.y, MoveButtonWidth, buttonsRow.height);
            var down = new Rect(up.xMax + L.footerGap, buttonsRow.y, MoveButtonWidth, buttonsRow.height);
            if (DimmableButton(up, "MBBS_MoveUp".Translate(), movable)) Step(index, -1);
            if (DimmableButton(down, "MBBS_MoveDown".Translate(), movable)) Step(index, 1);
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.gray;
            var hint = new Rect(down.xMax + L.panelGap, buttonsRow.y, buttonsRow.xMax - down.xMax - L.panelGap, buttonsRow.height);
            Widgets.Label(hint, "MBBS_OrderHint".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawBarRow(Rect row)
        {
            if (selected == BarKey) Widgets.DrawHighlightSelected(row);
            else Widgets.DrawHighlightIfMouseover(row);

            var c = ColumnsFor(row);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(c.Handle, "★");
            Text.Anchor = TextAnchor.MiddleLeft;
            var label = new Rect(c.Visible.x, row.y, c.Drawer.xMax - c.Visible.x, row.height);
            Widgets.Label(label, "MBBS_BarRow".Translate() + "  —  " + ModeLabel(working.displayMode));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(row)) selected = BarKey;
        }

        private void DrawButtonRow(Rect row, string name, int index)
        {
            var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
            var o = working.Get(name);
            bool locked = BarModel.IsLocked(working, name);
            var c = ColumnsFor(row);
            float box = L.checkboxSize;

            if (selected == name) Widgets.DrawHighlightSelected(row);
            else Widgets.DrawHighlightIfMouseover(row);

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            if (index >= 0 && !locked) Widgets.Label(c.Handle, "≡");
            GUI.color = Color.white;

            // Visible. Paintable: hold the mouse and drag down the column to set many at once.
            if (locked)
            {
                GUI.color = Color.gray;
                Widgets.Label(c.Visible, "—");
                GUI.color = Color.white;
            }
            else
            {
                bool visible = !o.hidden;
                Widgets.Checkbox(c.Visible.x + (c.Visible.width - box) / 2f, c.Visible.y + (c.Visible.height - box) / 2f, ref visible, box, false, true);
                if (visible == o.hidden) working.SetHidden(name, !visible);
            }

            // Icon the button shows with its settings (blank when it has none)
            var icon = def == null ? null : ButtonPainter.EffectiveIcon(def, o);
            if (icon != null) Widgets.DrawTextureFitted(c.Icon.ContractedBy(L.rowIconPadding), icon, 1f);

            // Name
            bool showingNow = !BarConfigManager.InGame || def == null || BarModel.SafeVisible(def);
            Text.Anchor = TextAnchor.MiddleLeft;
            if (!showingNow || o.hidden) GUI.color = new Color(1f, 1f, 1f, L.dimmedAlpha);
            string label = DisplayLabel(name, o);
            if (locked) label += "  " + "MBBS_LockedSuffix".Translate();
            else if (o.pin == PinPosition.Left) label += "  " + "MBBS_PinnedLeftSuffix".Translate();
            else if (o.pin == PinPosition.Right) label += "  " + "MBBS_PinnedRightSuffix".Translate();
            Widgets.Label(new Rect(c.Name.x + L.textInset, c.Name.y, c.Name.width - L.textInset, c.Name.height), label.Truncate(c.Name.width - L.textInset * 2f));
            GUI.color = Color.white;
            if (!showingNow) TooltipHandler.TipRegion(c.Name, "MBBS_NotShowingNowTip".Translate());

            // Source mod
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(c.Source, SourceOf(name).Truncate(c.Source.width - L.textInset));
            Text.Font = GameFont.Small;

            // Hotkey (advanced)
            if (Advanced)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(c.HotKey, def == null ? "—" : Hotkeys.Label(def));
            }
            GUI.color = Color.white;

            // Drawer (paintable as well)
            Text.Anchor = TextAnchor.MiddleCenter;
            if (!locked && BarModel.DrawerApplies(working, name))
            {
                bool inDrawer = o.inDrawer;
                Widgets.Checkbox(c.Drawer.x + (c.Drawer.width - box) / 2f, c.Drawer.y + (c.Drawer.height - box) / 2f, ref inDrawer, box, false, true);
                if (inDrawer != o.inDrawer) working.SetInDrawer(name, inDrawer);
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(c.Drawer, "—");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;

            var selectArea = new Rect(c.Icon.x, row.y, c.Drawer.x - c.Icon.x, row.height);
            if (Widgets.ButtonInvisible(selectArea, false)) selected = name;
        }

        // Drags and Move up / down count the rows the list shows; with a source filter the other buttons keep their places.
        private void OnReorder(int from, int to)
        {
            if (from < 0 || from >= shownRows.Count || BarModel.IsLocked(working, shownRows[from])) return;
            string moved = shownRows[from];
            var list = new List<string>(mainRows);
            ButtonOrder.ReorderShown(list, shownRows, from, to);
            CommitOrder(list);
            selected = moved;
        }

        private void Step(int index, int delta)
        {
            var list = new List<string>(mainRows);
            if (ButtonOrder.StepShown(list, shownRows, index, delta)) CommitOrder(list);
        }

        /// <summary>Stores a new order. Buttons of mods that are not loaded right now keep their place at the end.</summary>
        private void CommitOrder(List<string> visibleRows)
        {
            var all = visibleRows.Concat(hiddenRows).ToList();
            var newOrder = ButtonOrder.Effective(all, n => BarModel.PinOf(working, n));
            if (working.order != null)
            {
                var present = new HashSet<string>(newOrder);
                newOrder.AddRange(working.order.Where(n => n != null && !present.Contains(n)));
            }
            working.order = newOrder;
        }

        // ---------------------------------------------------------------- right side

        private void DrawRight(Rect rect)
        {
            int footerRows = editingTemplate ? 2 : 3;
            float footerHeight = L.footerButtonHeight * footerRows + L.footerGap * (footerRows - 1);
            var detail = new Rect(rect.x, rect.y, rect.width, rect.height - footerHeight - L.footerGap);
            var footer = new Rect(rect.x, rect.yMax - footerHeight, rect.width, footerHeight);

            Widgets.DrawMenuSection(detail);
            var inner = detail.ContractedBy(L.detailPadding);
            if (selected == BarKey) DrawBarDetail(inner);
            else DrawButtonDetail(inner, selected);

            DrawFooter(footer);
        }

        private Listing_Standard BeginScrolledListing(Rect outer, ref Vector2 scroll, float contentHeight)
        {
            var view = new Rect(0f, 0f, outer.width - GenUI.ScrollBarWidth, Mathf.Max(contentHeight, outer.height));
            Widgets.BeginScrollView(outer, ref scroll, view);
            var listing = new Listing_Standard();
            listing.Begin(new Rect(0f, 0f, view.width, float.MaxValue / 4f));
            return listing;
        }

        private static float EndScrolledListing(Listing_Standard listing)
        {
            listing.End();
            Widgets.EndScrollView();
            return listing.CurHeight;
        }

        private void DrawBarDetail(Rect outer)
        {
            var listing = BeginScrolledListing(outer, ref barDetailScroll, barDetailHeight);
            Text.Font = GameFont.Medium;
            listing.Label("MBBS_BarRow".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine();

            var mode = working.DisplayModeDef;

            // Display mode
            if (SectionHeader(listing, "MBBS_DisplayMode".Translate(), working.displayMode != BarConfig.VanillaMode))
                working.displayMode = BarConfig.VanillaMode;
            foreach (var m in BarModel.DisplayModes)
            {
                if (listing.RadioButton(m.LabelCap, working.displayMode == m.defName, L.radioIndent, m.description))
                    working.displayMode = m.defName;
            }

            if (mode?.usesMinWidth ?? false)
            {
                listing.Gap();
                working.minWidth = IntSetting(listing, "MBBS_MinWidth".Translate(working.MinWidth), working.minWidth, working.MinWidth, R.minWidth, R.widthStep);
                Note(listing, "MBBS_ScrollArrowsNote".Translate());
                if (Advanced)
                {
                    working.arrowWidth = IntSetting(listing, "MBBS_ArrowWidth".Translate(working.ArrowWidth), working.arrowWidth, working.ArrowWidth, R.arrowWidth, 1);
                    working.wheelScrollPixels = FloatSetting(listing, "MBBS_WheelSpeed".Translate(working.WheelScrollPixels.ToString("0")), working.wheelScrollPixels, working.WheelScrollPixels, R.wheelScrollPixels, 1f);
                }
            }

            if (Advanced && (mode?.usesDrawer ?? false))
            {
                listing.GapLine();
                listing.Label("MBBS_Section_Drawer".Translate());
                string more = working.MoreButtonWidth > 0 ? "MBBS_MoreWidth".Translate(working.MoreButtonWidth).ToString() : "MBBS_MoreWidthAuto".Translate().ToString();
                working.moreButtonWidth = IntSetting(listing, more, working.moreButtonWidth, working.MoreButtonWidth, R.moreButtonWidth, R.widthStep);
                string columns = working.DrawerColumns > 0 ? "MBBS_DrawerColumns".Translate(working.DrawerColumns).ToString() : "MBBS_DrawerColumnsAuto".Translate().ToString();
                working.drawerColumns = IntSetting(listing, columns, working.drawerColumns, working.DrawerColumns, R.drawerColumns, 1);
                string cell = working.DrawerCellWidth > 0 ? "MBBS_DrawerCellWidth".Translate(working.DrawerCellWidth).ToString() : "MBBS_DrawerCellWidthFollow".Translate().ToString();
                working.drawerCellWidth = IntSetting(listing, cell, working.drawerCellWidth, working.DrawerCellWidth, R.drawerCellWidth, R.widthStep);
            }

            if (Advanced)
            {
                listing.GapLine();
                listing.Label("MBBS_Section_Size".Translate());
                working.defaultWidth = IntSetting(listing, "MBBS_DefaultWidth".Translate(working.DefaultWidth), working.defaultWidth, working.DefaultWidth, R.defaultWidth, R.widthStep);
                int ownWidths = BarModel.CurrentNames.Count(n => working.Get(n).width > 0);
                if (ownWidths > 0)
                {
                    var row = listing.GetRect(L.textRowHeight);
                    string clear = "MBBS_WidthClearAll".Translate(ownWidths);
                    if (Widgets.ButtonText(new Rect(row.x, row.y, UIUtil.FitButton(L.moveButtonWidth, clear), row.height), clear))
                        foreach (var n in BarModel.CurrentNames) working.SetWidth(n, 0);
                }
                working.gap = IntSetting(listing, "MBBS_Gap".Translate(working.Gap), working.gap, working.Gap, R.gap, 1);
                working.fillScreen = BoolSetting(listing, "MBBS_FillScreen".Translate(), working.fillScreen, D.fillScreen, "MBBS_FillScreenTip".Translate());
                working.minimizedFullWidth = BoolSetting(listing, "MBBS_MinimizedFullWidth".Translate(), working.minimizedFullWidth, D.minimizedFullWidth,
                    "MBBS_MinimizedFullWidthTip".Translate());

                listing.GapLine();
                if (SectionHeader(listing, "MBBS_LabelAlign".Translate(), working.labelAlign != LabelAlign.Default, "MBBS_LabelAlignTip".Translate()))
                    working.labelAlign = LabelAlign.Default;
                foreach (LabelAlign align in Enum.GetValues(typeof(LabelAlign)))
                {
                    if (listing.RadioButton(("MBBS_Align_" + align).Translate(), working.labelAlign == align, L.radioIndent))
                        working.labelAlign = align;
                }

                listing.GapLine();
                if (SectionHeader(listing, "MBBS_IconModeAll".Translate(), working.iconMode != IconMode.Default, "MBBS_IconModeAllTip".Translate()))
                    working.iconMode = IconMode.Default;
                foreach (IconMode iconMode in Enum.GetValues(typeof(IconMode)))
                {
                    if (listing.RadioButton(("MBBS_IconMode_" + iconMode).Translate(), working.iconMode == iconMode, L.radioIndent))
                        working.iconMode = iconMode;
                }
                int individual = BarModel.CurrentNames.Count(n => working.Get(n).iconMode != IconMode.Default);
                if (individual > 0)
                {
                    var row = listing.GetRect(L.textRowHeight);
                    string clear = "MBBS_IconModeClearAll".Translate(individual);
                    if (Widgets.ButtonText(new Rect(row.x, row.y, UIUtil.FitButton(L.moveButtonWidth, clear), row.height), clear))
                        foreach (var n in BarModel.CurrentNames) working.SetIconMode(n, IconMode.Default);
                }

                listing.GapLine();
                working.menuLocked = BoolSetting(listing, "MBBS_MenuLocked".Translate(), working.menuLocked, D.menuLocked, "MBBS_MenuLockedTip".Translate());

                DrawCompatSettings(listing);
            }
            else if (!working.MenuLocked)
            {
                listing.Gap();
                GUI.color = Color.gray;
                listing.Label("MBBS_MenuUnlockedNote".Translate());
                GUI.color = Color.white;
            }

            barDetailHeight = EndScrolledListing(listing);
        }

        private void DrawCompatSettings(Listing_Standard listing)
        {
            foreach (var bridge in CompatBridges.Detected)
            {
                var worker = bridge.Worker;
                listing.GapLine();
                bool notDefault = worker.Priority != bridge.defaultPriority;
                if (SectionHeader(listing, "MBBS_Compat_Header".TranslateKeepCase(bridge.ModNameOrLabel), notDefault))
                    SetPriority(bridge, bridge.defaultPriority);
                foreach (CompatPriority p in Enum.GetValues(typeof(CompatPriority)))
                {
                    if (listing.RadioButton(("MBBS_Compat_" + p).Translate(bridge.ModNameOrLabel), worker.Priority == p, L.radioIndent,
                            ("MBBS_Compat_" + p + "_Desc").Translate(bridge.ModNameOrLabel)) && worker.Priority != p)
                        SetPriority(bridge, p);
                }
                if (worker.Priority != worker.SessionPriority)
                {
                    GUI.color = ColorLibrary.Gold;
                    listing.Label("MBBS_Compat_RestartNeeded".Translate());
                    GUI.color = Color.white;
                }
                foreach (var line in worker.BarReferenceValues()) Note(listing, line);
                if (worker.CanImport && listing.ButtonText("MBBS_Compat_Import".TranslateKeepCase(bridge.ModNameOrLabel)))
                {
                    worker.ImportInto(working);
                    Messages.Message("MBBS_Compat_Imported".TranslateKeepCase(bridge.ModNameOrLabel), MessageTypeDefOf.NeutralEvent, false);
                }
            }
        }

        private static void SetPriority(CompatBridgeDef bridge, CompatPriority p)
        {
            if (p == bridge.defaultPriority) MainButtonBarSystemMod.Settings.compatPriority.Remove(bridge.defName);
            else MainButtonBarSystemMod.Settings.compatPriority[bridge.defName] = p;
            MainButtonBarSystemMod.Instance.WriteSettings();
        }

        private void DrawButtonDetail(Rect outer, string name)
        {
            var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
            var baseline = ButtonBaseline.Get(name);
            var o = working.Get(name);
            var dflt = ButtonDefaults.Inherited(name);
            bool locked = BarModel.IsLocked(working, name);

            var listing = BeginScrolledListing(outer, ref buttonDetailScroll, buttonDetailHeight);

            var titleRow = listing.GetRect(L.titleIconSize);
            var icon = def == null ? null : ButtonPainter.EffectiveIcon(def, o);
            if (icon != null) Widgets.DrawTextureFitted(new Rect(titleRow.x, titleRow.y, L.titleIconSize, L.titleIconSize), icon, 1f);
            float textX = L.titleIconSize + L.footerGap;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(titleRow.x + textX, titleRow.y, titleRow.width - textX, titleRow.height), DisplayLabel(name, o).Truncate(titleRow.width - textX));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            listing.Label("MBBS_Info_ButtonSource".TranslateKeepCase(baseline?.SourceModName ?? "?"));
            listing.Label("MBBS_Info_IconSource".TranslateKeepCase(IconSourceText(baseline)));
            listing.Label("MBBS_Info_DefName".TranslateKeepCase(name));
            if (BarModel.OriginallyHidden(name)) listing.Label("MBBS_Info_OriginallyHidden".Translate());
            else if (BarConfigManager.InGame && def != null && !BarModel.SafeVisible(def)) listing.Label("MBBS_NotShowingNowTip".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // Every field back to what the button inherits (hotkeys are kept elsewhere and stay).
            var resetRow = listing.GetRect(L.textRowHeight);
            string reset = "MBBS_ResetButton".Translate();
            var resetRect = new Rect(resetRow.x, resetRow.y, UIUtil.FitButton(L.moveButtonWidth, reset), resetRow.height);
            bool changed = working.OverrideOf(name) != null;
            TooltipHandler.TipRegion(resetRect, "MBBS_ResetButtonTip".Translate());
            if (DimmableButton(resetRect, reset, changed)) working.ResetButton(name);
            listing.GapLine();

            if (locked)
            {
                GUI.color = Color.gray;
                listing.Label("MBBS_MenuLockedNote".Translate());
                GUI.color = Color.white;
            }
            else
            {
                bool visible = ButtonBool(listing, "MBBS_Visible".Translate(), !o.hidden, !dflt.hidden, null);
                if (visible == o.hidden) working.SetHidden(name, !visible);

                if (BarModel.DrawerApplies(working, name))
                {
                    bool inDrawer = ButtonBool(listing, "MBBS_InDrawer".Translate(), o.inDrawer, dflt.inDrawer, null);
                    if (inDrawer != o.inDrawer) working.SetInDrawer(name, inDrawer);
                }
            }

            // Custom name
            listing.Gap();
            listing.Label("MBBS_CustomName".Translate(baseline?.Label?.CapitalizeFirst() ?? name));
            var nameRow = listing.GetRect(L.textRowHeight);
            string current = o.label ?? "";
            var field = new Rect(nameRow.x, nameRow.y, nameRow.width - ResetWidth - L.footerGap, nameRow.height);
            string edited = Widgets.TextField(field, current, L.customNameMaxLength);
            if (edited != current) working.SetLabel(name, edited);
            if (DefaultButton(nameRow, o.label != dflt.label))
                working.SetLabel(name, null);

            if (Advanced)
            {
                listing.GapLine();
                int resolvedWidth = o.width;
                string widthLabel = resolvedWidth > 0 ? "MBBS_ButtonWidth".Translate(resolvedWidth).ToString() : "MBBS_ButtonWidthFollow".Translate().ToString();
                // Every button's panel has the same layout, so the box is told apart by the button's name.
                int width = IntSetting(listing, widthLabel, o.width == dflt.width ? BarConfig.Unset : o.width, o.width, R.buttonWidth, R.widthStep, "buttonWidth." + name);
                int newWidth = width == BarConfig.Unset ? dflt.width : width;
                if (newWidth != o.width) working.SetWidth(name, newWidth);

                if (!locked)
                {
                    if (SectionHeader(listing, "MBBS_Pin".Translate(), o.pin != dflt.pin))
                        SetPin(name, dflt.pin);
                    foreach (PinPosition pin in Enum.GetValues(typeof(PinPosition)))
                    {
                        if (listing.RadioButton(("MBBS_Pin_" + pin).Translate(), o.pin == pin, L.radioIndent) && o.pin != pin)
                            SetPin(name, pin);
                    }
                }

                bool forceShow = ButtonBool(listing, "MBBS_ForceShow".Translate(), o.forceShow, dflt.forceShow, "MBBS_ForceShowTip".Translate());
                if (forceShow != o.forceShow) working.SetForceShow(name, forceShow);

                DrawIconSettings(listing, name, def, o, dflt);
                DrawReferenceValues(listing, CompatBridges.Detected.SelectMany(b => b.Worker.ReferenceValues(name)));

                if (def != null) DrawHotkey(listing, def);
            }

            buttonDetailHeight = EndScrolledListing(listing);
        }

        /// <summary>Text / icon / both, and a custom icon (spec 3.2).</summary>
        private void DrawIconSettings(Listing_Standard listing, string name, MainButtonDef def, ButtonSettings o, ButtonSettings dflt)
        {
            listing.GapLine();
            if (SectionHeader(listing, "MBBS_IconMode".Translate(), o.iconMode != dflt.iconMode, "MBBS_IconModeTip".Translate()))
                working.SetIconMode(name, dflt.iconMode);
            foreach (IconMode mode in Enum.GetValues(typeof(IconMode)))
            {
                string label = mode == IconMode.Default
                    ? "MBBS_IconMode_FollowBar".Translate(("MBBS_IconMode_" + working.iconMode).Translate())
                    : ("MBBS_IconMode_" + mode).Translate();
                if (listing.RadioButton(label, o.iconMode == mode, L.radioIndent) && o.iconMode != mode)
                    working.SetIconMode(name, mode);
            }

            var row = listing.GetRect(L.textRowHeight);
            string choose = "MBBS_IconChoose".Translate();
            float chooseWidth = UIUtil.FitButton(L.smallButtonWidth, choose);
            float right = ResetWidth + L.footerGap + chooseWidth;
            var preview = ButtonPainter.EffectiveIcon(def, o);
            float x = row.x;
            if (preview != null)
            {
                Widgets.DrawTextureFitted(new Rect(x, row.y, row.height, row.height), preview, 1f);
                x += row.height + L.footerGap;
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            var labelRect = new Rect(x, row.y, row.xMax - right - L.footerGap - x, row.height);
            string text = "MBBS_Icon".TranslateKeepCase(CustomIcons.Describe(o.icon));
            Widgets.Label(labelRect, text.Truncate(labelRect.width));
            TooltipHandler.TipRegion(labelRect, text);
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonText(new Rect(row.xMax - right, row.y, chooseWidth, row.height), choose))
                Find.WindowStack.Add(new Dialog_IconPicker(o.icon, icon => working.SetIcon(name, icon)));
            if (DefaultButton(row, o.icon != dflt.icon))
                working.SetIcon(name, dflt.icon);
            if (CustomIcons.IsMissing(o.icon))
                Note(listing, "MBBS_IconMissingNote".Translate());
        }

        /// <summary>What other bar mods have set for this button, as reference (spec 3.7).</summary>
        private static void DrawReferenceValues(Listing_Standard listing, IEnumerable<string> lines)
        {
            var list = lines.ToList();
            if (list.Count == 0) return;
            listing.GapLine();
            listing.Label("MBBS_RefHeader".Translate());
            foreach (var line in list) Note(listing, line);
        }

        private void SetPin(string name, PinPosition pin)
        {
            working.SetPin(name, pin);
            if (pin != PinPosition.None) working.SetInDrawer(name, false);
        }

        private void DrawHotkey(Listing_Standard listing, MainButtonDef def)
        {
            listing.GapLine();
            var row = listing.GetRect(L.textRowHeight);
            float buttons = SmallButtonWidth * 2f + L.footerGap;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, row.width - buttons - L.footerGap, row.height), "MBBS_HotKey".Translate(Hotkeys.Label(def)));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonText(new Rect(row.xMax - buttons, row.y, SmallButtonWidth, row.height), "MBBS_HotKeySet".Translate()))
                Hotkeys.Define(def);
            if (DimmableButton(new Rect(row.xMax - SmallButtonWidth, row.y, SmallButtonWidth, row.height), "MBBS_HotKeyClear".Translate(), Hotkeys.Get(def) != KeyCode.None))
                Hotkeys.Clear(def);

            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            listing.Label((Hotkeys.UsesGameBinding(def) ? "MBBS_HotKeyGameNote" : "MBBS_HotKeyCustomNote").Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            var conflicts = Hotkeys.Conflicts(def);
            if (conflicts.Count > 0)
            {
                GUI.color = ColorLibrary.RedReadable;
                listing.Label("MBBS_HotKeyConflictHeader".Translate(Hotkeys.Label(def)));
                foreach (var c in conflicts)
                    listing.Label("  • " + (c.Category.NullOrEmpty() ? c.Label : "MBBS_HotKeyConflictItem".Translate(c.Category, c.Label).ToString()));
                GUI.color = Color.white;
                Note(listing, (Hotkeys.UsesGameBinding(def) ? "MBBS_HotKeyConflictExplainGame" : "MBBS_HotKeyConflictExplainCustom").Translate());
            }
        }

        // ---------------------------------------------------------------- rows with a "Default" button

        /// <summary>The "Default" button at the right end of a row. Returns true when clicked while enabled.</summary>
        private static bool DefaultButton(Rect row, bool enabled)
        {
            var rect = new Rect(row.xMax - ResetWidth, row.y, ResetWidth, row.height);
            TooltipHandler.TipRegion(rect, enabled ? "MBBS_DefaultTip".Translate() : "MBBS_AlreadyDefaultTip".Translate());
            return DimmableButton(rect, "MBBS_Default".Translate(), enabled);
        }

        /// <summary>
        /// A button drawn faded while it cannot be used (the game's inactive button looks the same as an active one).
        /// It is always a real control: an inactive game button creates none, so a "Default" button turning active
        /// while a number is typed would shift the control ids and take the focus away from the number box.
        /// </summary>
        private static bool DimmableButton(Rect rect, string label, bool enabled)
        {
            var color = GUI.color;
            if (!enabled) GUI.color = new Color(color.r, color.g, color.b, color.a * L.dimmedAlpha);
            var font = Text.Font;
            Text.Font = UIUtil.ButtonFont(label, rect.width);
            bool clicked = Widgets.ButtonText(rect, label, true, enabled, true);
            Text.Font = font;
            GUI.color = color;
            return clicked && enabled;
        }

        /// <summary>A small grey explanation line.</summary>
        private static void Note(Listing_Standard listing, string text)
        {
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            listing.Label(text);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        /// <summary>A section label with a "Default" button. Returns true when the button is clicked.</summary>
        private static bool SectionHeader(Listing_Standard listing, string label, bool changed, string tooltip = null)
        {
            // A label longer than one line (some translations) gets more lines; the button stays on the first.
            float labelWidth = listing.ColumnWidth - ResetWidth - L.footerGap;
            var row = listing.GetRect(Mathf.Max(L.textRowHeight, Text.CalcHeight(label, labelWidth)));
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            if (tooltip != null) TooltipHandler.TipRegion(row, tooltip);
            return DefaultButton(new Rect(row.x, row.y, row.width, L.textRowHeight), changed);
        }

        /// <summary>
        /// A label row with a "Default" button, then a slider. <paramref name="stored"/> is -1 while unchanged.
        /// Returns the new stored value.
        /// </summary>
        private static int IntSetting(Listing_Standard listing, string label, int stored, int resolved, IntRange range, int step,
            [CallerArgumentExpression("stored")] string id = null)
        {
            if (SectionHeader(listing, label, stored != BarConfig.Unset)) return BarConfig.Unset;
            float raw = NumberSlider(listing, resolved, range.min, range.max, Mathf.Max(1, step), id);
            int value = Mathf.Clamp(Mathf.RoundToInt(raw), range.min, range.max);
            return value != resolved ? value : stored;
        }

        private static float FloatSetting(Listing_Standard listing, string label, float stored, float resolved, FloatRange range, float step,
            [CallerArgumentExpression("stored")] string id = null)
        {
            if (SectionHeader(listing, label, stored >= 0f)) return BarConfig.Unset;
            float value = NumberSlider(listing, resolved, range.min, range.max, step, id);
            return Mathf.Approximately(value, resolved) ? stored : value;
        }

        /// <summary>Like <see cref="IntSetting"/> for a value that may be negative: unchanged is <see cref="BarConfig.UnsetOffset"/>, the step is 1.</summary>
        private static int OffsetSetting(Listing_Standard listing, string label, int stored, int resolved, IntRange range,
            [CallerArgumentExpression("stored")] string id = null)
        {
            if (SectionHeader(listing, label, stored != BarConfig.UnsetOffset)) return BarConfig.UnsetOffset;
            float raw = NumberSlider(listing, resolved, range.min, range.max, 1f, id);
            int value = Mathf.Clamp(Mathf.RoundToInt(raw), range.min, range.max);
            return value != resolved ? value : stored;
        }

        /// <summary>
        /// A slider with a number box at its right. Drawn by this mod rather than Widgets.HorizontalSlider, so
        /// slider mods that patch that method (BetterSliders draws a box over its right end) do not cover it.
        /// </summary>
        private static float NumberSlider(Listing_Standard listing, float value, float min, float max, float step, string id)
        {
            var row = listing.GetRect(L.sliderHeight);
            var field = new Rect(row.xMax - L.numberFieldWidth, row.y, L.numberFieldWidth, row.height);
            var track = new Rect(row.x, row.y, row.width - L.numberFieldWidth - L.footerGap, row.height);
            value = Slider(track, value, min, max, step);
            value = NumberField(field, value, min, max, step, id);
            listing.Gap(listing.verticalSpacing);
            return value;
        }

        private static float Snap(float value, float min, float max, float step) =>
            Mathf.Clamp(step > 0f ? Mathf.Round(value / step) * step : value, min, max);

        private static float Slider(Rect rect, float value, float min, float max, float step)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive, rect);
            var handleSize = L.sliderHandleSize;
            var rail = new Rect(rect.x + handleSize.x / 2f, rect.center.y - L.sliderRailHeight / 2f, Mathf.Max(1f, rect.width - handleSize.x), L.sliderRailHeight);
            float t = max > min ? Mathf.Clamp01((value - min) / (max - min)) : 0f;

            Widgets.DrawBoxSolid(rail, L.sliderRailColor);
            Widgets.DrawBoxSolid(new Rect(rail.x, rail.y, rail.width * t, rail.height), L.sliderFillColor);
            var handle = new Rect(rail.x + rail.width * t - handleSize.x / 2f, rect.center.y - handleSize.y / 2f, handleSize.x, handleSize.y);
            Widgets.DrawBoxSolid(handle, L.sliderHandleColor);
            Widgets.DrawBox(handle);
            if (Mouse.IsOver(rect) || GUIUtility.hotControl == id) Widgets.DrawHighlight(handle);

            var ev = Event.current;
            float FromMouse() => Snap(min + Mathf.Clamp01((ev.mousePosition.x - rail.x) / rail.width) * (max - min), min, max, step);
            switch (ev.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (ev.button == 0 && rect.Contains(ev.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        value = FromMouse();
                        ev.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = FromMouse();
                        ev.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        ev.Use();
                    }
                    break;
            }
            return value;
        }

        /// <summary>
        /// Type a value directly. Only typing changes the value: a keystroke that leaves a number in range applies it
        /// at once, while an empty box or a number still out of range is kept as text until it is one. When the value
        /// changes from elsewhere (the slider, "Default", another button selected) the box shows the new value. A
        /// click outside the box or Enter ends the editing. The game's Widgets.TextFieldNumeric names its own
        /// control, so its focus cannot be checked from outside; this uses the plain text field and parses here.
        /// </summary>
        private static float NumberField(Rect rect, float value, float min, float max, float step, string id)
        {
            string name = "MainButtonBarSystem.Number." + id;
            bool focused = GUI.GetNameOfFocusedControl() == name;
            var ev = Event.current;
            if (focused && (ev.rawType == EventType.MouseDown && !Mouse.IsOver(rect) ||
                            ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter)))
            {
                UI.UnfocusCurrentControl();
                focused = false;
            }

            if (!focused || !numberBuffers.TryGetValue(id, out var state) || !Mathf.Approximately(state.value, value))
                state = (Format(value, step), value);

            GUI.SetNextControlName(name);
            string typed = GUI.TextField(rect, state.text, Text.CurTextFieldStyle);
            float result = value;
            if (typed != state.text && IsPartialNumber(typed, min, step < 1f))
            {
                state.text = typed;
                if (float.TryParse(typed, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && parsed >= min && parsed <= max)
                    result = Snap(parsed, min, max, step);
                state.value = result;
            }
            numberBuffers[id] = state;
            return result;
        }

        private static string Format(float value, float step) =>
            step >= 1f ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>Text that is, or can still become, a number: digits, a leading minus when negatives are allowed, one point for fractions.</summary>
        private static bool IsPartialNumber(string text, float min, bool allowFraction)
        {
            bool point = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '0' && c <= '9') continue;
                if (c == '-' && i == 0 && min < 0f) continue;
                if (c == '.' && allowFraction && !point)
                {
                    point = true;
                    continue;
                }
                return false;
            }
            return true;
        }

        private static DefaultableBool BoolSetting(Listing_Standard listing, string label, DefaultableBool stored, bool fallback, string tooltip)
        {
            var row = listing.GetRect(L.textRowHeight);
            if (DefaultButton(row, stored != DefaultableBool.Default)) return DefaultableBool.Default;
            bool before = BarConfig.Resolve(stored, fallback);
            bool value = before;
            Widgets.CheckboxLabeled(new Rect(row.x, row.y, row.width - ResetWidth - L.footerGap, row.height), label, ref value);
            if (tooltip != null) TooltipHandler.TipRegion(row, tooltip);
            return value == before ? stored : value ? DefaultableBool.True : DefaultableBool.False;
        }

        /// <summary>A per-button switch whose default comes from the button's declared defaults.</summary>
        private static bool ButtonBool(Listing_Standard listing, string label, bool value, bool defaultValue, string tooltip)
        {
            var row = listing.GetRect(L.textRowHeight);
            if (DefaultButton(row, value != defaultValue)) return defaultValue;
            Widgets.CheckboxLabeled(new Rect(row.x, row.y, row.width - ResetWidth - L.footerGap, row.height), label, ref value);
            if (tooltip != null) TooltipHandler.TipRegion(row, tooltip);
            return value;
        }

        // ---------------------------------------------------------------- footer

        private void DrawFooter(Rect rect)
        {
            float gap = L.footerGap, h = L.footerButtonHeight;
            float y = rect.y;
            float half = (rect.width - gap) / 2f;
            if (!editingTemplate)
            {
                if (UIUtil.ButtonText(new Rect(rect.x, y, half, h), "MBBS_SaveAsTemplate".Translate()))
                {
                    BarConfigManager.SaveTemplate(working);
                    Messages.Message("MBBS_TemplateSaved".Translate(), MessageTypeDefOf.NeutralEvent, false);
                }
                bool hasTemplate = BarConfigManager.HasTemplate;
                if (DimmableButton(new Rect(rect.x + half + gap, y, half, h), "MBBS_ApplyTemplate".Translate(), hasTemplate))
                    ReplaceWorking(BarConfigManager.Template.Clone());
                y += h + gap;
            }

            if (UIUtil.ButtonText(new Rect(rect.x, y, half, h), "MBBS_Export".Translate()))
                Find.WindowStack.Add(new FloatMenu(ExportOptions()));
            if (UIUtil.ButtonText(new Rect(rect.x + half + gap, y, half, h), "MBBS_Import".Translate()))
                Find.WindowStack.Add(new FloatMenu(ImportOptions()));
            y += h + gap;

            float third = (rect.width - gap * 2f) / 3f;
            if (UIUtil.ButtonText(new Rect(rect.x, y, third, h), "MBBS_ResetAll".Translate()))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("MBBS_ResetAllConfirm".Translate(), () => working.ResetToUntouched(), destructive: true));
            if (UIUtil.ButtonText(new Rect(rect.x + third + gap, y, third, h), "CancelButton".Translate()))
                Close();
            if (UIUtil.ButtonText(new Rect(rect.x + (third + gap) * 2f, y, third, h), "MBBS_Save".Translate()))
            {
                BarConfigManager.Store(working.Clone());
                Close();
            }
        }

        private void ReplaceWorking(BarConfig config)
        {
            working = config;
            BarConfigManager.Preview = working;
        }

        // ---------------------------------------------------------------- export / import (spec 3.9)

        private List<FloatMenuOption> ExportOptions()
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("MBBS_Export_Clipboard".Translate(), () => AskInfo(ExportTarget.Clipboard, (meta, _) =>
                {
                    GUIUtility.systemCopyBuffer = ArrangementIO.ToText(working, meta);
                    Messages.Message("MBBS_Export_Copied".Translate(), MessageTypeDefOf.NeutralEvent, false);
                })),
                new FloatMenuOption("MBBS_Export_File".Translate(), () => AskInfo(ExportTarget.File, (meta, _) =>
                {
                    var path = ArrangementIO.SaveToFile(working, meta);
                    Messages.Message("MBBS_Export_Saved".TranslateKeepCase(path), MessageTypeDefOf.NeutralEvent, false);
                })),
                new FloatMenuOption("MBBS_Export_ModXml".Translate(), () => AskInfo(ExportTarget.ModXml, (meta, _) =>
                {
                    GUIUtility.systemCopyBuffer = ArrangementIO.ToModXml(working, meta, Core.SharingNames.Identifier(meta.name, "MyArrangement"));
                    Messages.Message("MBBS_Export_ModXmlCopied".Translate(), MessageTypeDefOf.NeutralEvent, false);
                })),
                new FloatMenuOption("MBBS_Export_ModFolder".Translate(), () => AskInfo(ExportTarget.ModFolder, (meta, packageId) =>
                {
                    try
                    {
                        var folder = ArrangementIO.ExportModFolder(working, meta, packageId);
                        Find.WindowStack.Add(new Dialog_MessageBox("MBBS_Export_ModFolderDone".TranslateKeepCase(folder), "MBBS_OpenFolder".Translate(),
                            () => FolderUtil.Open(folder), "OK".Translate()));
                    }
                    catch (System.Exception e)
                    {
                        Messages.Message("MBBS_Export_Failed".Translate(e.Message), MessageTypeDefOf.RejectInput, false);
                    }
                })),
                new FloatMenuOption("MBBS_OpenExportsFolder".Translate(), () => FolderUtil.Open(ModPaths.ExportsFolder)),
            };
            if (tab == Tab.Appearance) options.InsertRange(0, ThemeExportOptions());
            return options;
        }

        /// <summary>Appearance tab: save the custom look as a custom theme (spec 3.5); greyed out without one.</summary>
        private List<FloatMenuOption> ThemeExportOptions()
        {
            bool has = working.UsesCustomTheme && !working.customTheme.IsEmpty;
            var save = new FloatMenuOption("MBBS_Export_Theme".Translate(), has ? () => AskInfo(ExportTarget.Theme, (meta, _) =>
            {
                try
                {
                    var folder = CustomThemeFiles.Save(working, meta);
                    Find.WindowStack.Add(new Dialog_MessageBox("MBBS_Export_ThemeDone".TranslateKeepCase(folder), "MBBS_OpenFolder".Translate(),
                        () => FolderUtil.Open(folder), "OK".Translate()));
                }
                catch (System.Exception e)
                {
                    Messages.Message("MBBS_Export_Failed".Translate(e.Message), MessageTypeDefOf.RejectInput, false);
                }
            }) : (System.Action)null);
            if (!has) save.tooltip = new TipSignal("MBBS_Export_ThemeNone".Translate());
            return new List<FloatMenuOption> { save, new FloatMenuOption("MBBS_OpenThemesFolder".Translate(), CustomThemeFiles.OpenFolder) };
        }

        /// <summary>Appearance tab: each saved custom theme, then the folder (spec 3.5).</summary>
        private List<FloatMenuOption> ThemeImportOptions()
        {
            var options = new List<FloatMenuOption>();
            foreach (var saved in CustomThemeFiles.All)
            {
                var theme = saved;
                string author = theme.meta?.author;
                string label = author.NullOrEmpty() ? "MBBS_Import_Theme".TranslateKeepCase(theme.Name) : "MBBS_Import_ThemeBy".TranslateKeepCase(theme.Name, author);
                options.Add(new FloatMenuOption(label, () => ApplySavedTheme(theme)));
            }
            options.Add(new FloatMenuOption("MBBS_OpenThemesFolder".Translate(), CustomThemeFiles.OpenFolder));
            return options;
        }

        /// <summary>
        /// Copies a saved custom theme into the settings (spec 4.17), after a confirmation when that replaces a custom
        /// look. A starting theme that is not installed becomes vanilla, with a message.
        /// </summary>
        private void ApplySavedTheme(SavedCustomTheme saved)
        {
            void Apply()
            {
                var baseDef = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(saved.baseTheme);
                if (baseDef == null)
                    Messages.Message("MBBS_SavedTheme_BaseMissing".TranslateKeepCase(saved.Name, saved.baseTheme), MessageTypeDefOf.CautionInput, false);
                working.appearance = baseDef?.defName ?? BarConfig.VanillaAppearance;
                working.appearanceMode = AppearanceMode.Custom;
                working.customTheme = CustomThemeFiles.Install(saved);
                lookSelected = null;
                Messages.Message("MBBS_SavedTheme_Applied".TranslateKeepCase(saved.Name), MessageTypeDefOf.NeutralEvent, false);
            }

            if (working.customTheme != null && !working.customTheme.IsEmpty)
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("MBBS_SavedTheme_ReplaceConfirm".TranslateKeepCase(saved.Name), Apply, destructive: true));
            else Apply();
        }

        /// <summary>Asks for name, author and description, then exports with them and this game's button list.</summary>
        private void AskInfo(ExportTarget target, System.Action<ArrangementMeta, string> export)
        {
            Find.WindowStack.Add(new Dialog_ArrangementInfo(target, (name, author, description, packageId) =>
                export(ArrangementMeta.ForExport(working, name, author, description), packageId)));
        }

        private List<FloatMenuOption> ImportOptions()
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("MBBS_Import_Clipboard".Translate(), () =>
                    ImportResult(ArrangementIO.FromText(GUIUtility.systemCopyBuffer, "MBBS_Import_ClipboardSource".Translate(), out var error), error)),
            };
            foreach (var file in ArrangementIO.ExportedFiles().Take(L.importMenuMaxFiles))
            {
                var path = file.FullName;
                var (name, author) = ArrangementIO.PeekTitle(path);
                string label = name.NullOrEmpty()
                    ? "MBBS_Import_File".TranslateKeepCase(file.Name).ToString()
                    : author.NullOrEmpty()
                        ? "MBBS_Import_FileNamed".Translate(name, file.Name).ToString()
                        : "MBBS_Import_FileNamedBy".TranslateKeepCase(name, author, file.Name).ToString();
                options.Add(new FloatMenuOption(label, () => ImportResult(ArrangementIO.FromFile(path, out var error), error)));
            }
            foreach (var def in DefDatabase<MainButtonArrangementDef>.AllDefs.OrderBy(d => d.label))
            {
                var arrangement = def;
                string label = arrangement.author.NullOrEmpty()
                    ? "MBBS_Import_Def".TranslateKeepCase(arrangement.LabelCap)
                    : "MBBS_Import_DefBy".TranslateKeepCase(arrangement.LabelCap, arrangement.author);
                options.Add(new FloatMenuOption(label, () => ImportResult(ArrangementIO.FromDef(arrangement), null)));
            }
            options.Add(new FloatMenuOption("MBBS_OpenExportsFolder".Translate(), () => FolderUtil.Open(ModPaths.ExportsFolder)));
            if (tab == Tab.Appearance) options.InsertRange(0, ThemeImportOptions());
            return options;
        }

        private void ImportResult(SharedArrangement imported, string error)
        {
            if (imported?.config == null)
            {
                Messages.Message(error ?? "MBBS_Import_NoConfig".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new Dialog_ImportArrangement(imported, working, ReplaceWorking));
        }

        // ---------------------------------------------------------------- helpers

        public static string ModeLabel(string defName) =>
            DefDatabase<BarDisplayModeDef>.GetNamedSilentFail(defName ?? BarConfig.VanillaMode)?.LabelCap ?? defName;

        private static string IconSourceText(ButtonBaselineEntry baseline)
        {
            if (baseline == null) return "?";
            if (baseline.IconSourceModName != null) return baseline.IconSourceModName;
            if (string.IsNullOrEmpty(baseline.IconPath)) return "MBBS_None".Translate();
            return baseline.IconFromGameAssets ? "MBBS_IconFromGameAssets".Translate() : "MBBS_Unknown".Translate();
        }

        private static string DisplayLabel(string name, in ButtonSettings o)
        {
            if (!o.label.NullOrEmpty()) return o.label;
            var label = ButtonBaseline.Get(name)?.Label;
            return label.NullOrEmpty() ? name : label.CapitalizeFirst();
        }

        private static Texture2D SafeIcon(MainButtonDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.iconPath)) return null;
            try
            {
                return def.Icon;
            }
            catch
            {
                return null;
            }
        }
    }
}
