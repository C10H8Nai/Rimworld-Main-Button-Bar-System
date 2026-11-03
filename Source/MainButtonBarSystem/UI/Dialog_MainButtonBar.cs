using System;
using System.Collections.Generic;
using System.Linq;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// The settings window (spec 3.1, 3.2). It edits a working copy that the bar draws live; Save stores it,
    /// Cancel or closing throws it away (spec 4.7).
    /// </summary>
    public class Dialog_MainButtonBar : Window
    {
        private const string BarKey = "<bar>";
        private const float RowHeight = 30f;
        private const float HeaderHeight = 36f;
        private const float ColumnHeaderHeight = 22f;
        private const float FooterButtonHeight = 34f;
        private const float Gap = 10f;

        private enum Tab
        {
            Simple,
            Advanced,
        }

        private static Tab tab = Tab.Simple;

        private readonly bool editingTemplate;
        private BarConfig working;
        private string selected = BarKey;
        private Vector2 listScroll;
        private bool showOriginallyHidden;

        private readonly List<string> mainRows = new List<string>();
        private readonly List<string> hiddenRows = new List<string>();

        private bool Advanced => tab == Tab.Advanced;

        public override Vector2 InitialSize => new Vector2(Mathf.Min(1180f, UI.screenWidth - 40f), Mathf.Min(780f, UI.screenHeight - 60f));

        public Dialog_MainButtonBar()
        {
            editingTemplate = !BarConfigManager.InGame;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            draggable = true;
            onlyOneOfTypeAllowed = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Window_ButtonDrawer.CloseIfOpen();
            working = BarConfigManager.Saved.Clone();
            BarConfigManager.Preview = working;
        }

        public override void PostClose()
        {
            base.PostClose();
            if (BarConfigManager.Preview == working) BarConfigManager.Preview = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            BuildRows();

            var header = new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight);
            DrawHeader(header);

            var body = new Rect(inRect.x, header.yMax + 4f, inRect.width, inRect.height - header.height - 4f);
            var left = new Rect(body.x, body.y, Mathf.Floor(body.width * 0.6f), body.height);
            var right = new Rect(left.xMax + Gap, body.y, body.width - left.width - Gap, body.height);

            DrawList(left);
            DrawRight(right);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        // ---------------------------------------------------------------- header

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 300f, rect.height),
                (editingTemplate ? "MBBS_Title_Template" : "MBBS_Title_Save").Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            var advancedRect = new Rect(rect.xMax - 40f - 110f, rect.y + 2f, 110f, 30f);
            var simpleRect = new Rect(advancedRect.x - 114f, rect.y + 2f, 110f, 30f);
            if (Widgets.ButtonTextSubtle(simpleRect, "MBBS_Tab_Simple".Translate(), 0f, -1f, null, default, null, tab == Tab.Simple))
                tab = Tab.Simple;
            if (Widgets.ButtonTextSubtle(advancedRect, "MBBS_Tab_Advanced".Translate(), 0f, -1f, null, default, null, tab == Tab.Advanced))
                tab = Tab.Advanced;
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
        }

        private struct Columns
        {
            public Rect Handle, Visible, Icon, Name, Source, HotKey, Drawer;
        }

        private Columns ColumnsFor(Rect row)
        {
            const float handle = 22f, visible = 32f, icon = 30f, drawer = 46f;
            float hotKey = Advanced ? 80f : 0f;
            float source = Mathf.Min(170f, row.width * 0.26f);
            float name = row.width - handle - visible - icon - source - hotKey - drawer;

            var c = new Columns();
            float x = row.x;
            c.Handle = new Rect(x, row.y, handle, row.height); x += handle;
            c.Visible = new Rect(x, row.y, visible, row.height); x += visible;
            c.Icon = new Rect(x, row.y, icon, row.height); x += icon;
            c.Name = new Rect(x, row.y, name, row.height); x += name;
            c.Source = new Rect(x, row.y, source, row.height); x += source;
            c.HotKey = new Rect(x, row.y, hotKey, row.height); x += hotKey;
            c.Drawer = new Rect(x, row.y, drawer, row.height);
            return c;
        }

        private void DrawList(Rect rect)
        {
            var buttonsRow = new Rect(rect.x, rect.yMax - FooterButtonHeight, rect.width, FooterButtonHeight);
            var frame = new Rect(rect.x, rect.y, rect.width, rect.height - FooterButtonHeight - 6f);
            Widgets.DrawMenuSection(frame);
            var inner = frame.ContractedBy(4f);

            // Column titles.
            var titles = ColumnsFor(new Rect(inner.x, inner.y, inner.width - 16f, ColumnHeaderHeight));
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titles.Visible, "MBBS_Col_Visible".Translate());
            Widgets.Label(titles.Icon, "MBBS_Col_Icon".Translate());
            Widgets.Label(titles.Drawer, "MBBS_Col_Drawer".Translate());
            if (Advanced) Widgets.Label(titles.HotKey, "MBBS_Col_HotKey".Translate());
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titles.Name, "MBBS_Col_Name".Translate());
            Widgets.Label(titles.Source, "MBBS_Col_Source".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            var outRect = new Rect(inner.x, inner.y + ColumnHeaderHeight, inner.width, inner.height - ColumnHeaderHeight);
            float height = RowHeight * (1 + mainRows.Count);
            if (hiddenRows.Count > 0) height += RowHeight + (showOriginallyHidden ? RowHeight * hiddenRows.Count : 0f);
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, height);

            Widgets.BeginScrollView(outRect, ref listScroll, viewRect);
            float y = 0f;
            DrawBarRow(new Rect(0f, y, viewRect.width, RowHeight));
            y += RowHeight;

            var groupRect = new Rect(0f, y, viewRect.width, RowHeight * mainRows.Count);
            int group = ReorderableWidget.NewGroup(OnReorder, ReorderableDirection.Vertical, groupRect);
            for (int i = 0; i < mainRows.Count; i++)
            {
                var row = new Rect(0f, y, viewRect.width, RowHeight);
                ReorderableWidget.Reorderable(group, row);
                DrawButtonRow(row, mainRows[i], i);
                y += RowHeight;
            }

            if (hiddenRows.Count > 0)
            {
                var toggle = new Rect(0f, y, viewRect.width, RowHeight);
                Widgets.DrawHighlightIfMouseover(toggle);
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(toggle.x + 8f, toggle.y, toggle.width - 8f, toggle.height),
                    (showOriginallyHidden ? "▼ " : "▶ ") + "MBBS_OriginallyHidden".Translate(hiddenRows.Count));
                GUI.color = Color.white;
                TooltipHandler.TipRegion(toggle, "MBBS_OriginallyHiddenTip".Translate());
                if (Widgets.ButtonInvisible(toggle)) showOriginallyHidden = !showOriginallyHidden;
                y += RowHeight;

                if (showOriginallyHidden)
                {
                    foreach (var name in hiddenRows)
                    {
                        DrawButtonRow(new Rect(0f, y, viewRect.width, RowHeight), name, -1);
                        y += RowHeight;
                    }
                }
            }
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;

            // Move buttons. Top to bottom in the list is left to right on screen.
            int index = mainRows.IndexOf(selected);
            bool movable = index >= 0 && !BarModel.IsLocked(working, selected);
            var up = new Rect(buttonsRow.x, buttonsRow.y, 110f, buttonsRow.height);
            var down = new Rect(up.xMax + 6f, buttonsRow.y, 110f, buttonsRow.height);
            if (Widgets.ButtonText(up, "MBBS_MoveUp".Translate(), true, true, movable) && movable) Step(index, -1);
            if (Widgets.ButtonText(down, "MBBS_MoveDown".Translate(), true, true, movable) && movable) Step(index, 1);
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(down.xMax + 12f, buttonsRow.y, buttonsRow.xMax - down.xMax - 12f, buttonsRow.height), "MBBS_OrderHint".Translate());
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

            if (selected == name) Widgets.DrawHighlightSelected(row);
            else Widgets.DrawHighlightIfMouseover(row);

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            if (index >= 0 && !locked) Widgets.Label(c.Handle, "≡");
            GUI.color = Color.white;

            // Visible
            if (locked)
            {
                GUI.color = Color.gray;
                Widgets.Label(c.Visible, "—");
                GUI.color = Color.white;
            }
            else
            {
                bool visible = !o.hidden;
                Widgets.Checkbox(c.Visible.x + (c.Visible.width - 24f) / 2f, c.Visible.y + (c.Visible.height - 24f) / 2f, ref visible);
                if (visible == o.hidden) working.GetOrCreate(name).hidden = !visible;
            }

            // Icon (blank when the button has none)
            var icon = SafeIcon(def);
            if (icon != null) Widgets.DrawTextureFitted(c.Icon.ContractedBy(3f), icon, 1f);

            // Name
            bool showingNow = !BarConfigManager.InGame || def == null || BarModel.SafeVisible(def);
            Text.Anchor = TextAnchor.MiddleLeft;
            if (!showingNow || o.hidden) GUI.color = new Color(1f, 1f, 1f, 0.5f);
            string label = DisplayLabel(name, o);
            if (locked) label += "  " + "MBBS_LockedSuffix".Translate();
            else if (o.pin == PinPosition.Left) label += "  " + "MBBS_PinnedLeftSuffix".Translate();
            else if (o.pin == PinPosition.Right) label += "  " + "MBBS_PinnedRightSuffix".Translate();
            Widgets.Label(new Rect(c.Name.x + 2f, c.Name.y, c.Name.width - 2f, c.Name.height), label.Truncate(c.Name.width - 4f));
            GUI.color = Color.white;
            if (!showingNow) TooltipHandler.TipRegion(c.Name, "MBBS_NotShowingNowTip".Translate());

            // Source mod
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(c.Source, (ButtonBaseline.Get(name)?.SourceModName ?? "?").Truncate(c.Source.width - 2f));
            Text.Font = GameFont.Small;

            // Hotkey (advanced)
            if (Advanced)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(c.HotKey, HotKeyLabel(def));
            }
            GUI.color = Color.white;

            // Drawer
            Text.Anchor = TextAnchor.MiddleCenter;
            if (!locked && BarModel.DrawerApplies(working, name))
            {
                bool inDrawer = o.inDrawer;
                Widgets.Checkbox(c.Drawer.x + (c.Drawer.width - 24f) / 2f, c.Drawer.y + (c.Drawer.height - 24f) / 2f, ref inDrawer);
                if (inDrawer != o.inDrawer) working.GetOrCreate(name).inDrawer = inDrawer;
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

        private void OnReorder(int from, int to)
        {
            if (from < 0 || from >= mainRows.Count || BarModel.IsLocked(working, mainRows[from])) return;
            string moved = mainRows[from];
            var list = new List<string>(mainRows);
            ButtonOrder.Reorder(list, from, to);
            CommitOrder(list);
            selected = moved;
        }

        private void Step(int index, int delta)
        {
            var list = new List<string>(mainRows);
            if (ButtonOrder.Step(list, index, delta)) CommitOrder(list);
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
            float footerHeight = FooterButtonHeight * (editingTemplate ? 1 : 2) + 6f;
            var detail = new Rect(rect.x, rect.y, rect.width, rect.height - footerHeight - 6f);
            var footer = new Rect(rect.x, rect.yMax - footerHeight, rect.width, footerHeight);

            Widgets.DrawMenuSection(detail);
            var inner = detail.ContractedBy(10f);
            if (selected == BarKey) DrawBarDetail(inner);
            else DrawButtonDetail(inner, selected);

            DrawFooter(footer);
        }

        private void DrawBarDetail(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Medium;
            listing.Label("MBBS_BarRow".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine();

            listing.Label("MBBS_DisplayMode".Translate());
            foreach (DisplayMode mode in Enum.GetValues(typeof(DisplayMode)))
            {
                if (listing.RadioButton(ModeLabel(mode), working.displayMode == mode, 12f, ("MBBS_Mode_" + mode + "_Desc").Translate()))
                    working.displayMode = mode;
            }

            if (BarLayout.Scrolls(working.displayMode))
            {
                listing.Gap();
                listing.Label("MBBS_MinWidth".Translate(working.minWidth));
                working.minWidth = Mathf.RoundToInt(listing.Slider(working.minWidth, 20f, 300f) / 5f) * 5;
            }

            if (Advanced)
            {
                listing.GapLine();
                listing.Label("MBBS_DefaultWidth".Translate(working.defaultWidth));
                working.defaultWidth = Mathf.RoundToInt(listing.Slider(working.defaultWidth, 40f, 400f) / 5f) * 5;
                listing.Label("MBBS_Gap".Translate(working.gap));
                working.gap = Mathf.RoundToInt(listing.Slider(working.gap, 0f, 30f));
                listing.CheckboxLabeled("MBBS_FillScreen".Translate(), ref working.fillScreen, "MBBS_FillScreenTip".Translate());
                listing.CheckboxLabeled("MBBS_MenuLocked".Translate(), ref working.menuLocked, "MBBS_MenuLockedTip".Translate());
            }
            else if (!working.menuLocked)
            {
                listing.Gap();
                GUI.color = Color.gray;
                listing.Label("MBBS_MenuUnlockedNote".Translate());
                GUI.color = Color.white;
            }

            listing.End();
        }

        private void DrawButtonDetail(Rect rect, string name)
        {
            var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
            var baseline = ButtonBaseline.Get(name);
            var o = working.Get(name);
            bool locked = BarModel.IsLocked(working, name);

            var listing = new Listing_Standard();
            listing.Begin(rect);

            var titleRow = listing.GetRect(36f);
            var icon = SafeIcon(def);
            if (icon != null) Widgets.DrawTextureFitted(new Rect(titleRow.x, titleRow.y, 36f, 36f), icon, 1f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(titleRow.x + 42f, titleRow.y, titleRow.width - 42f, titleRow.height), DisplayLabel(name, o).Truncate(titleRow.width - 42f));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            listing.Label("MBBS_Info_ButtonSource".Translate(baseline?.SourceModName ?? "?"));
            listing.Label("MBBS_Info_IconSource".Translate(baseline?.IconSourceModName ?? (string.IsNullOrEmpty(baseline?.IconPath) ? "MBBS_None".Translate().ToString() : "MBBS_Unknown".Translate().ToString())));
            listing.Label("MBBS_Info_DefName".Translate(name));
            if (BarModel.OriginallyHidden(name)) listing.Label("MBBS_Info_OriginallyHidden".Translate());
            else if (BarConfigManager.InGame && def != null && !BarModel.SafeVisible(def)) listing.Label("MBBS_NotShowingNowTip".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            listing.GapLine();

            if (locked)
            {
                GUI.color = Color.gray;
                listing.Label("MBBS_MenuLockedNote".Translate());
                GUI.color = Color.white;
            }
            else
            {
                bool visible = !o.hidden;
                listing.CheckboxLabeled("MBBS_Visible".Translate(), ref visible);
                if (visible == o.hidden) working.GetOrCreate(name).hidden = !visible;

                if (BarModel.DrawerApplies(working, name))
                {
                    bool inDrawer = o.inDrawer;
                    listing.CheckboxLabeled("MBBS_InDrawer".Translate(), ref inDrawer);
                    if (inDrawer != o.inDrawer) working.GetOrCreate(name).inDrawer = inDrawer;
                }
            }

            listing.Gap();
            listing.Label("MBBS_CustomName".Translate(baseline?.Label?.CapitalizeFirst() ?? name));
            var nameRow = listing.GetRect(30f);
            string current = o.label ?? "";
            string edited = Widgets.TextField(new Rect(nameRow.x, nameRow.y, nameRow.width - 90f, nameRow.height), current, 40);
            if (edited != current) working.GetOrCreate(name).label = edited.NullOrEmpty() ? null : edited;
            if (Widgets.ButtonText(new Rect(nameRow.xMax - 84f, nameRow.y, 84f, nameRow.height), "MBBS_ResetName".Translate(), true, true, !o.label.NullOrEmpty()))
                working.GetOrCreate(name).label = null;

            if (Advanced)
            {
                listing.GapLine();
                listing.Label(o.width > 0 ? "MBBS_ButtonWidth".Translate(o.width) : "MBBS_ButtonWidthFollow".Translate());
                int width = Mathf.RoundToInt(listing.Slider(o.width, 0f, 400f) / 5f) * 5;
                if (width != o.width) working.GetOrCreate(name).width = width;

                if (!locked)
                {
                    listing.Label("MBBS_Pin".Translate());
                    foreach (PinPosition pin in Enum.GetValues(typeof(PinPosition)))
                    {
                        if (listing.RadioButton(("MBBS_Pin_" + pin).Translate(), o.pin == pin, 12f) && o.pin != pin)
                        {
                            working.GetOrCreate(name).pin = pin;
                            if (pin != PinPosition.None) working.GetOrCreate(name).inDrawer = false;
                        }
                    }
                }

                bool forceShow = o.forceShow;
                listing.CheckboxLabeled("MBBS_ForceShow".Translate(), ref forceShow, "MBBS_ForceShowTip".Translate());
                if (forceShow != o.forceShow) working.GetOrCreate(name).forceShow = forceShow;

                listing.Label("MBBS_HotKeyReadOnly".Translate(HotKeyLabel(def)));
            }

            listing.End();
        }

        private void DrawFooter(Rect rect)
        {
            float y = rect.y;
            if (!editingTemplate)
            {
                float half = (rect.width - 6f) / 2f;
                if (Widgets.ButtonText(new Rect(rect.x, y, half, FooterButtonHeight), "MBBS_SaveAsTemplate".Translate()))
                {
                    BarConfigManager.SaveTemplate(working);
                    Messages.Message("MBBS_TemplateSaved".Translate(), MessageTypeDefOf.NeutralEvent, false);
                }
                bool hasTemplate = BarConfigManager.HasTemplate;
                if (Widgets.ButtonText(new Rect(rect.x + half + 6f, y, half, FooterButtonHeight), "MBBS_ApplyTemplate".Translate(), true, true, hasTemplate) && hasTemplate)
                    ReplaceWorking(BarConfigManager.Template.Clone());
                y += FooterButtonHeight + 6f;
            }

            float third = (rect.width - 12f) / 3f;
            if (Widgets.ButtonText(new Rect(rect.x, y, third, FooterButtonHeight), "MBBS_ResetAll".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("MBBS_ResetAllConfirm".Translate(), () => working.ResetToUntouched(), destructive: true));
            }
            if (Widgets.ButtonText(new Rect(rect.x + third + 6f, y, third, FooterButtonHeight), "CancelButton".Translate()))
                Close();
            if (Widgets.ButtonText(new Rect(rect.x + (third + 6f) * 2f, y, third, FooterButtonHeight), "MBBS_Save".Translate()))
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

        // ---------------------------------------------------------------- helpers

        public static string ModeLabel(DisplayMode mode) => ("MBBS_Mode_" + mode).Translate();

        private static string DisplayLabel(string name, ButtonOverride o)
        {
            if (!o.label.NullOrEmpty()) return o.label;
            var label = ButtonBaseline.Get(name)?.Label;
            return label.NullOrEmpty() ? name : label.CapitalizeFirst();
        }

        private static string HotKeyLabel(MainButtonDef def)
        {
            try
            {
                if (def?.hotKey == null) return "—";
                var key = KeyPrefs.KeyPrefsData.GetBoundKeyCode(def.hotKey, KeyPrefs.BindingSlot.A);
                return key == KeyCode.None ? "—" : key.ToStringReadable();
            }
            catch
            {
                return "?";
            }
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
