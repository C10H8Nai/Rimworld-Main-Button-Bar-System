using System.Collections.Generic;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Appearance tab (design spec 2026-11-05): a "preset themes | custom theme" switch at the top. Preset themes are
    /// the theme cards; custom theme is a list (all buttons, then each button) and the selected entry's look. Both
    /// have the Buttons tab's footer at the bottom right.
    /// </summary>
    public partial class Dialog_MainButtonBar
    {
        /// <summary>Selected entry of the custom theme list; null is "all buttons".</summary>
        private string lookSelected;

        private Vector2 lookListScroll;
        private Vector2 lookDetailScroll;
        private float lookDetailHeight;

        /// <summary>"Copy style": a copy of one layer, pasted onto another (spec 4.7); kept while the game runs.</summary>
        private static ButtonLook lookClipboard;

        /// <summary>What each colour code box shows while it has focus, keyed by the setting it edits.</summary>
        private static readonly Dictionary<string, string> hexBuffers = new Dictionary<string, string>();

        private static readonly ButtonVisualState[] LookStates =
            { ButtonVisualState.Normal, ButtonVisualState.Mouseover, ButtonVisualState.Active, ButtonVisualState.Disabled };

        private static MainButtonBarTuningDef.Behaviour Bh => T.behaviour;

        private float FooterHeight
        {
            get
            {
                int rows = editingTemplate ? 2 : 3;
                return L.footerButtonHeight * rows + L.footerGap * (rows - 1);
            }
        }

        private void DrawAppearance(Rect body)
        {
            var switchRow = new Rect(body.x, body.y, body.width, L.textRowHeight);
            DrawAppearanceSwitch(switchRow);
            float top = switchRow.yMax + L.footerGap;
            var area = new Rect(body.x, top, body.width, body.yMax - top);
            var left = new Rect(area.x, area.y, Mathf.Floor(area.width * L.listFraction), area.height);
            var right = new Rect(left.xMax + L.panelGap, area.y, area.width - left.width - L.panelGap, area.height);

            if (working.appearanceMode == AppearanceMode.Custom)
            {
                working.customTheme ??= new CustomThemeData();
                DrawLookList(left);
                var detail = new Rect(right.x, right.y, right.width, right.height - FooterHeight - L.footerGap);
                Widgets.DrawMenuSection(detail);
                DrawLookDetail(detail.ContractedBy(L.detailPadding));
            }
            else
            {
                DrawAppearanceTab(new Rect(area.x, area.y, area.width, area.height - FooterHeight - L.footerGap));
            }
            DrawFooter(new Rect(right.x, area.yMax - FooterHeight, right.width, FooterHeight));
        }

        private void DrawAppearanceSwitch(Rect row)
        {
            var area = new Rect(row.x, row.y, Mathf.Min(L.viewSwitchWidth, row.width), row.height);
            float half = Mathf.Floor(area.width / 2f);
            DrawModePart(new Rect(area.x, area.y, half, area.height), "MBBS_Mode_Preset".Translate(), AppearanceMode.Preset);
            DrawModePart(new Rect(area.x + half, area.y, area.width - half, area.height), "MBBS_Mode_Custom".Translate(), AppearanceMode.Custom);
            TooltipHandler.TipRegion(area, "MBBS_ModeTip".Translate());
        }

        private void DrawModePart(Rect rect, string label, AppearanceMode mode)
        {
            bool chosen = working.appearanceMode == mode;
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
                working.appearanceMode = mode;
                // Starts empty: every field follows the chosen theme until it is changed.
                if (mode == AppearanceMode.Custom) working.customTheme ??= new CustomThemeData();
            }
        }

        // ---------------------------------------------------------------- list

        private void DrawLookList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(L.listFramePadding);
            var names = BarModel.EffectiveOrder(working);
            float rowH = L.rowHeight;
            var view = new Rect(0f, 0f, inner.width - GenUI.ScrollBarWidth, rowH * (names.Count + 1));
            Widgets.BeginScrollView(inner, ref lookListScroll, view);
            DrawLookRow(new Rect(0f, 0f, view.width, rowH), null);
            for (int i = 0; i < names.Count; i++)
                DrawLookRow(new Rect(0f, rowH * (i + 1), view.width, rowH), names[i]);
            Widgets.EndScrollView();
        }

        private void DrawLookRow(Rect row, string name)
        {
            if (lookSelected == name) Widgets.DrawHighlightSelected(row);
            else Widgets.DrawHighlightIfMouseover(row);

            float x = row.x + L.textInset;
            if (name != null)
            {
                var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
                var icon = def == null ? null : ButtonPainter.EffectiveIcon(def, working.Get(name));
                if (icon != null) Widgets.DrawTextureFitted(new Rect(x, row.y, row.height, row.height).ContractedBy(L.rowIconPadding), icon, 1f);
                x += row.height;
            }

            var data = working.customTheme;
            var layer = data.Layer(name);
            string label = name == null ? "MBBS_Look_All".Translate().ToString() : DisplayLabel(name, working.Get(name));
            if (layer != null && !layer.IsEmpty) label += "  " + "MBBS_Look_ChangedSuffix".Translate();
            // Spec 4.6: how many buttons do not simply follow "all buttons".
            if (name == null && data.OwnButtonCount > 0) label += "  " + "MBBS_Look_OwnCount".Translate(data.OwnButtonCount);
            Text.Anchor = TextAnchor.MiddleLeft;
            var labelRect = new Rect(x, row.y, row.xMax - x, row.height);
            Widgets.Label(labelRect, label.Truncate(labelRect.width));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(row)) lookSelected = name;
        }

        // ---------------------------------------------------------------- detail

        private BarAppearanceWorker BaseWorker => working.AppearanceDef?.Worker ?? new BarAppearanceWorker();

        /// <summary>The custom look's worker for the working settings (it draws the preview the way the bar would).</summary>
        private BarAppearanceWorker_Custom LookWorker => CustomThemes.WorkerFor(working.customTheme, working.AppearanceDef) as BarAppearanceWorker_Custom;

        private void DrawLookDetail(Rect outer)
        {
            var data = working.customTheme;
            string key = lookSelected;
            var listing = BeginScrolledListing(outer, ref lookDetailScroll, lookDetailHeight);

            Text.Font = GameFont.Medium;
            listing.Label(key == null ? "MBBS_Look_All".Translate().ToString() : DisplayLabel(key, working.Get(key)));
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            listing.Label((key == null ? "MBBS_Look_AllTip" : "MBBS_Look_ButtonTip").TranslateKeepCase(working.AppearanceDef?.LabelCap.ToString() ?? "?"));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            DrawLookLayerButtons(listing, data, key);

            // Preview: the four states side by side, drawn by this mod (other mods' DoButton effects show only on the bar).
            float h = Bh.buttonRectHeight;
            float cell = (listing.ColumnWidth - L.footerGap * 3f) / 4f;
            // The state names wrap onto more lines when a translation is wider than a cell.
            Text.Font = GameFont.Tiny;
            float labelHeight = L.textRowHeight;
            foreach (var state in LookStates)
                labelHeight = Mathf.Max(labelHeight, Text.CalcHeight(("MBBS_Look_State_" + state).Translate(), cell));
            Text.Font = GameFont.Small;
            var previewRow = listing.GetRect(h + labelHeight);
            for (int i = 0; i < LookStates.Length; i++)
            {
                var r = new Rect(previewRow.x + (cell + L.footerGap) * i, previewRow.y, cell, h);
                DrawLookPreview(r, key, LookStates[i]);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(r.x, r.yMax, r.width, labelHeight), ("MBBS_Look_State_" + LookStates[i]).Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            listing.GapLine();
            listing.Label("MBBS_Look_Text".Translate());
            DrawLookAlign(listing, data, key);
            DrawLookFont(listing, data, key);
            for (int which = 0; which < 3; which++) DrawLookColor(listing, data, key, which);

            listing.GapLine();
            listing.Label("MBBS_Look_Icon".Translate());
            Note(listing, "MBBS_Look_IconNote".Translate());
            DrawLookIcon(listing, data, key);

            listing.GapLine();
            listing.Label("MBBS_Look_Background".Translate());
            foreach (var state in LookStates) DrawLookAtlas(listing, data, key, state);

            listing.GapLine();
            listing.Label("MBBS_Look_Image".Translate());
            Note(listing, "MBBS_Look_ImageNote".Translate());
            DrawLookImage(listing, data, key);

            data.Prune();
            // Fields are written directly while editing: cached results (button pictures) are rebuilt while this is open.
            data.Touch();
            lookDetailHeight = EndScrolledListing(listing);
        }

        /// <summary>Reset, copy and paste for the selected layer; on "all buttons" also the count and reset of the others (spec 4.6, 4.7).</summary>
        private static void DrawLookLayerButtons(Listing_Standard listing, CustomThemeData data, string key)
        {
            var layer = data.Layer(key);
            string reset = "MBBS_Look_Reset".Translate(), copy = "MBBS_Look_Copy".Translate(), paste = "MBBS_Look_Paste".Translate();
            // Side by side while they fit; a button that would run past the panel (longer translations) starts a new row.
            var row = listing.GetRect(L.textRowHeight);
            float x = row.x;
            Rect Place(float width)
            {
                if (x > row.x && x + width > row.xMax)
                {
                    row = listing.GetRect(L.textRowHeight);
                    x = row.x;
                }
                var placed = new Rect(x, row.y, Mathf.Min(width, row.width), row.height);
                x = placed.xMax + L.footerGap;
                return placed;
            }

            if (DimmableButton(Place(UIUtil.FitButton(L.moveButtonWidth, reset)), reset, layer != null && !layer.IsEmpty)) data.Reset(key);
            if (Widgets.ButtonText(Place(UIUtil.FitButton(L.smallButtonWidth, copy)), copy)) lookClipboard = (layer ?? new ButtonLook()).Clone();
            var r = Place(UIUtil.FitButton(L.smallButtonWidth, paste));
            if (DimmableButton(r, paste, lookClipboard != null)) data.SetLayer(key, lookClipboard);
            TooltipHandler.TipRegion(r, "MBBS_Look_PasteTip".Translate());

            if (key != null || data.OwnButtonCount == 0) return;
            var row2 = listing.GetRect(L.textRowHeight);
            string all = "MBBS_Look_ResetAllButtons".Translate(data.OwnButtonCount);
            if (Widgets.ButtonText(new Rect(row2.x, row2.y, UIUtil.FitButton(L.moveButtonWidth, all), row2.height), all)) data.ResetAllButtons();
        }

        /// <summary>The button "all buttons" previews with: the first on the bar that has an icon, so icon settings show.</summary>
        private MainButtonDef SampleButton()
        {
            foreach (var name in BarModel.EffectiveOrder(working))
            {
                var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
                if (def != null && ButtonPainter.EffectiveIcon(def, working.Get(name)) != null) return def;
            }
            return null;
        }

        /// <summary>
        /// One state of a button the way the bar draws it: the picture, the frame (tinted under the mouse like the game
        /// unless the background is made for that state), then the icon and label where the bar puts them.
        /// </summary>
        private void DrawLookPreview(Rect rect, string key, ButtonVisualState state)
        {
            var data = working.customTheme;
            var theme = working.AppearanceDef;
            var worker = LookWorker;
            var baseWorker = BaseWorker;
            var def = key == null ? null : DefDatabase<MainButtonDef>.GetNamedSilentFail(key);

            var image = worker == null ? null : key == null ? worker.AllButtonsImage() : worker.ButtonImage(def);
            if (image != null) worker.DrawButtonImage(rect, image);

            bool replacesTint;
            var tex = data.HasAtlas(key) ? data.AtlasFor(key, state, theme, out replacesTint) : baseWorker.ButtonAtlas(state, out replacesTint);
            var oldColor = GUI.color;
            if (state == ButtonVisualState.Mouseover && !replacesTint) GUI.color = GenUI.MouseoverColor;
            Widgets.DrawAtlas(rect, tex ?? Widgets.ButtonSubtleAtlas);
            GUI.color = oldColor;
            if (state == ButtonVisualState.Disabled) return;   // the game draws no label on a disabled button

            var color = (data.HasColor(key) ? data.LabelColorFor(key, state, theme) : baseWorker.LabelColor(state)) ?? Color.white;
            var lookFont = data.FontFor(key, out int storedFontSize);
            var font = CustomThemes.GameFontOf(lookFont) ?? baseWorker.LabelFontFor(def) ?? Bh.labelFont;
            int fontSize = lookFont == LookFont.Custom ? CustomThemes.FontSize(storedFontSize) : lookFont == LookFont.Default ? baseWorker.LabelFontSizeFor(def) : 0;
            var align = data.AlignFor(key);
            if (align == LabelAlign.Default) align = baseWorker.LabelAlignFor(def);
            if (align == LabelAlign.Default) align = working.labelAlign;

            string label = key == null ? "MBBS_Look_Sample".Translate().ToString() : DisplayLabel(key, working.Get(key));
            var iconDef = def ?? SampleButton();
            var icon = iconDef == null ? null : ButtonPainter.EffectiveIcon(iconDef, working.Get(iconDef.defName));
            data.IconFor(key, Mathf.RoundToInt(Bh.iconSize), out int size, out var iconAnchor, out var offset);
            var mode = key == null ? working.iconMode : working.IconModeFor(working.Get(key));
            // The preview cannot know whether another mod would add the label next to the icon on the bar.
            if (mode == IconMode.Default) mode = icon != null ? IconMode.Icon : IconMode.Text;

            bool shifted = state == ButtonVisualState.Mouseover;
            var textRect = rect;
            if (icon != null && (mode == IconMode.Icon || mode == IconMode.Both))
            {
                DecorationBox box;
                if (mode == IconMode.Icon) box = IconPlacement.Alone(rect.x, rect.y, rect.width, rect.height, size, iconAnchor, Bh.labelSidePadding, offset.x, offset.y);
                else
                {
                    box = IconPlacement.Beside(rect.x, rect.y, rect.width, rect.height, size, iconAnchor, Bh.labelSidePadding, Bh.iconTextGap, offset.x, offset.y, out float tl, out float tr);
                    textRect = new Rect(tl, rect.y, Mathf.Max(0f, tr - tl), rect.height);
                }
                var iconRect = new Rect(box.X, box.Y, box.Width, box.Height);
                if (shifted) iconRect.position += Bh.hoverLabelOffset;
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, icon);
                GUI.color = oldColor;
                if (mode == IconMode.Icon) return;
            }

            // The label where the bar puts it: with no alignment chosen, where the game would (left, a share of the width in).
            bool followGame = align == LabelAlign.Default;
            float inset = LabelPlacement.LeftInset(followGame, -1f, textRect.width, Bh.gameLabelLeftMarginFraction, Bh.labelSidePadding);
            var area = new Rect(textRect.x + inset, textRect.y, Mathf.Max(0f, textRect.width - inset), textRect.height);
            var anchor = align switch
            {
                LabelAlign.Right => TextAnchor.MiddleRight,
                LabelAlign.Center => TextAnchor.MiddleCenter,
                _ => TextAnchor.MiddleLeft,
            };
            ButtonPainter.DrawLabel(area, label, color, anchor, shifted, font, fontSize);
        }

        private void DrawLookAlign(Listing_Standard listing, CustomThemeData data, string key)
        {
            var layer = data.Layer(key);
            var stored = layer?.align ?? LabelAlign.Default;
            if (SectionHeader(listing, "MBBS_LabelAlign".Translate(), stored != LabelAlign.Default))
                data.GetOrCreate(key).align = LabelAlign.Default;
            foreach (var align in new[] { LabelAlign.Default, LabelAlign.Left, LabelAlign.Center, LabelAlign.Right })
            {
                string label = align == LabelAlign.Default ? FollowBelowLabel(key) : ("MBBS_Align_" + align).Translate().ToString();
                if (listing.RadioButton(label, stored == align, L.radioIndent) && stored != align)
                    data.GetOrCreate(key).align = align;
            }
        }

        private void DrawLookFont(Listing_Standard listing, CustomThemeData data, string key)
        {
            var layer = data.Layer(key);
            var stored = layer?.font ?? LookFont.Default;
            if (SectionHeader(listing, "MBBS_Look_Font".Translate(), stored != LookFont.Default))
                SetLookFont(data.GetOrCreate(key), LookFont.Default);
            foreach (var font in LookFonts)
            {
                string label = font == LookFont.Default ? FollowBelowLabel(key) : ("MBBS_Look_Font_" + font).Translate().ToString();
                if (listing.RadioButton(label, stored == font, L.radioIndent) && stored != font)
                    SetLookFont(data.GetOrCreate(key), font);
            }

            // "Custom size" on this layer: its size, with its own Default button (the tuning def's size).
            if (stored != LookFont.Custom) return;
            int storedSize = layer.fontSize;
            int size = CustomThemes.FontSize(storedSize);
            int newSize = IntSetting(listing, "MBBS_Look_FontSize".Translate(size), storedSize, size, R.lookFontSize, 1, "lookFontSize." + (key ?? "<all>"));
            if (newSize != storedSize) layer.fontSize = newSize;
        }

        private static readonly LookFont[] LookFonts = { LookFont.Default, LookFont.Tiny, LookFont.Small, LookFont.Medium, LookFont.Custom };

        /// <summary>A size only means something with "custom size", so it goes with any other choice.</summary>
        private static void SetLookFont(ButtonLook look, LookFont font)
        {
            look.font = font;
            if (font != LookFont.Custom) look.fontSize = BarConfig.Unset;
        }

        /// <summary>"Follow all buttons" for a button; "follow the theme" for the all-buttons layer.</summary>
        private static string FollowBelowLabel(string key) =>
            (key == null ? "MBBS_Look_FollowTheme" : "MBBS_Look_FollowAll").Translate();

        /// <summary>The label colour the layer below gives a state: all buttons (for a button), else the theme, else white.</summary>
        private Color BelowColor(string key, ButtonVisualState state)
        {
            var data = working.customTheme;
            if (key != null) return data.LabelColorFor(null, state, working.AppearanceDef) ?? Color.white;
            return BaseWorker.LabelColor(state) ?? Color.white;
        }

        private void DrawLookColor(Listing_Standard listing, CustomThemeData data, string key, int which)
        {
            var state = which == 1 ? ButtonVisualState.Mouseover : which == 2 ? ButtonVisualState.Active : ButtonVisualState.Normal;
            var layer = data.Layer(key);
            bool set = layer != null && ButtonLook.IsSet(layer.ColorOf(which));
            var resolved = data.LabelColorFor(key, state, working.AppearanceDef) ?? Color.white;

            if (SectionHeader(listing, ("MBBS_Look_Color_" + which).Translate(), set))
            {
                if (layer != null) layer.SetColor(which, ButtonLook.UnsetColor);
                return;
            }

            // Swatch, colour code box, the game's colour picker, and "follows …" while not set.
            string id = "lookColor." + which + "." + (key ?? "<all>");
            var row = listing.GetRect(L.textRowHeight);
            var swatch = new Rect(row.x, row.y + L.lookSwatchInset, L.lookSwatchWidth, row.height - L.lookSwatchInset * 2f);
            Widgets.DrawBoxSolid(swatch, resolved);
            Widgets.DrawBox(swatch);

            int r255 = Mathf.RoundToInt(resolved.r * 255f), g255 = Mathf.RoundToInt(resolved.g * 255f), b255 = Mathf.RoundToInt(resolved.b * 255f);
            var hexRect = new Rect(swatch.xMax + L.footerGap, row.y, L.lookHexFieldWidth, row.height);
            string hexId = id + ".hex";
            bool focused = GUI.GetNameOfFocusedControl() == hexId;
            string current = ColorHex.Format(r255, g255, b255);
            string shown = focused && hexBuffers.TryGetValue(hexId, out var buffer) ? buffer : current;
            GUI.SetNextControlName(hexId);
            string edited = Widgets.TextField(hexRect, shown);
            if (focused || edited != shown) hexBuffers[hexId] = edited;
            if (edited != shown && ColorHex.TryParse(edited, out int hr, out int hg, out int hb) && (hr != r255 || hg != g255 || hb != b255))
                data.GetOrCreate(key).SetColor(which, new Color(hr / 255f, hg / 255f, hb / 255f, 1f));

            string pick = "MBBS_Look_PickColor".Translate();
            var pickRect = new Rect(hexRect.xMax + L.footerGap, row.y, UIUtil.FitButton(L.smallButtonWidth, pick), row.height);
            if (Widgets.ButtonText(pickRect, pick))
            {
                var data2 = data;
                Find.WindowStack.Add(new Dialog_LookColorPicker(resolved, BelowColor(key, state), c => data2.GetOrCreate(key).SetColor(which, c)));
            }
            if (!set)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(pickRect.xMax + L.footerGap, row.y, Mathf.Max(0f, row.xMax - pickRect.xMax - L.footerGap), row.height), FollowBelowLabel(key));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            float rs = NumberSlider(listing, r255, 0f, 255f, 1f, id + ".r");
            float gs = NumberSlider(listing, g255, 0f, 255f, 1f, id + ".g");
            float bs = NumberSlider(listing, b255, 0f, 255f, 1f, id + ".b");
            if (!Mathf.Approximately(rs, r255) || !Mathf.Approximately(gs, g255) || !Mathf.Approximately(bs, b255))
                data.GetOrCreate(key).SetColor(which, new Color(rs / 255f, gs / 255f, bs / 255f, 1f));
        }

        private void DrawLookIcon(Listing_Standard listing, CustomThemeData data, string key)
        {
            var layer = data.Layer(key);
            int defaultSize = Mathf.RoundToInt(Bh.iconSize);
            data.IconFor(key, defaultSize, out int size, out var anchor, out var offset);
            string suffix = "." + (key ?? "<all>");

            // A button is only buttonRectHeight tall: a bigger icon would just be cut off.
            var sizeRange = new IntRange(R.lookIconSize.min, Mathf.Max(R.lookIconSize.min, Mathf.Min(R.lookIconSize.max, Mathf.RoundToInt(Bh.buttonRectHeight))));
            int storedSize = layer?.iconSize ?? BarConfig.Unset;
            int newSize = IntSetting(listing, "MBBS_Look_IconSize".Translate(size), storedSize, size, sizeRange, 1, "lookIconSize" + suffix);
            if (newSize != storedSize) data.GetOrCreate(key).iconSize = newSize;

            var storedAnchor = layer?.iconAnchor ?? IconAnchor.Default;
            if (SectionHeader(listing, "MBBS_Look_IconAnchor".Translate(), storedAnchor != IconAnchor.Default))
                data.GetOrCreate(key).iconAnchor = IconAnchor.Default;
            var grid = listing.GetRect(L.lookAnchorCell * 3f);
            for (int i = 0; i < 9; i++)
            {
                var a = (IconAnchor)(i + 1);
                if (AnchorCell(grid, i, anchor == a)) data.GetOrCreate(key).iconAnchor = a;
            }

            int storedX = layer?.iconOffsetX ?? BarConfig.UnsetOffset;
            int newX = OffsetSetting(listing, "MBBS_Look_IconOffsetX".Translate((int)offset.x), storedX, (int)offset.x, R.lookIconOffset, "lookIconOffsetX" + suffix);
            if (newX != storedX) data.GetOrCreate(key).iconOffsetX = newX;
            int storedY = layer?.iconOffsetY ?? BarConfig.UnsetOffset;
            int newY = OffsetSetting(listing, "MBBS_Look_IconOffsetY".Translate((int)offset.y), storedY, (int)offset.y, R.lookIconOffset, "lookIconOffsetY" + suffix);
            if (newY != storedY) data.GetOrCreate(key).iconOffsetY = newY;
        }

        /// <summary>One cell (0 top-left … 8 bottom-right) of a 3×3 position picker; true when clicked.</summary>
        private static bool AnchorCell(Rect grid, int i, bool chosen)
        {
            float cellSize = L.lookAnchorCell;
            var cell = new Rect(grid.x + L.radioIndent + (i % 3) * cellSize, grid.y + (i / 3) * cellSize, cellSize, cellSize).ContractedBy(L.lookAnchorCellGap);
            Widgets.DrawBoxSolid(cell, chosen ? L.viewSwitchSelectedColor : L.viewSwitchColor);
            Widgets.DrawBox(cell);
            var dot = IconPlacement.Alone(cell.x, cell.y, cell.width, cell.height, cellSize * L.lookAnchorDotFraction, (IconAnchor)(i + 1), L.lookAnchorDotMargin, 0f, 0f);
            Widgets.DrawBoxSolid(new Rect(dot.X, dot.Y, dot.Width, dot.Height), Color.white);
            if (!chosen) Widgets.DrawHighlightIfMouseover(cell);
            return Widgets.ButtonInvisible(cell);
        }

        /// <summary>A row with a thumbnail, a text that may wrap, "Choose…" and Default; true when Default was pressed.</summary>
        private static bool ImageRow(Listing_Standard listing, string text, Texture2D shown, bool atlas, bool set, System.Action choose)
        {
            string chooseLabel = "MBBS_IconChoose".Translate();
            float chooseWidth = UIUtil.FitButton(L.smallButtonWidth, chooseLabel);
            float right = ResetWidth + L.footerGap + chooseWidth;
            float labelWidth = listing.ColumnWidth - L.lookThumbWidth - L.footerGap - right - L.footerGap;
            // A label wider than its space (some translations) wraps onto more lines; the thumbnail and the buttons
            // stay on the first line. A window too narrow for any label keeps one line instead of a huge row.
            float labelHeight = labelWidth > 0f ? Text.CalcHeight(text, labelWidth) : L.textRowHeight;
            var row = listing.GetRect(Mathf.Max(L.textRowHeight, labelHeight));
            var line = new Rect(row.x, row.y, row.width, L.textRowHeight);

            var thumb = new Rect(line.x, line.y, L.lookThumbWidth, line.height);
            if (shown != null)
            {
                if (atlas) Widgets.DrawAtlas(thumb, shown);
                else Widgets.DrawTextureFitted(thumb, shown, 1f);
            }

            // One line: centred on the row like before. More lines: from the top, level with the buttons.
            Text.Anchor = row.height > L.textRowHeight ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(thumb.xMax + L.footerGap, row.y, Mathf.Max(0f, labelWidth), row.height), text);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(new Rect(line.xMax - right, line.y, chooseWidth, line.height), chooseLabel)) choose();
            return DefaultButton(line, set);
        }

        private void DrawLookAtlas(Listing_Standard listing, CustomThemeData data, string key, ButtonVisualState state)
        {
            var layer = data.Layer(key);
            string stored = layer?.AtlasOf(state);
            bool set = !stored.NullOrEmpty();
            string text = ("MBBS_Look_State_" + state).Translate() + ": " + (set ? CustomThemes.Describe(stored) : FollowBelowLabel(key));
            var shown = set ? CustomThemes.Texture(stored) : data.AtlasFor(key, state, working.AppearanceDef, out _) ?? Widgets.ButtonSubtleAtlas;
            var custom = working.customTheme;
            if (ImageRow(listing, text, shown, true, set,
                    () => Find.WindowStack.Add(new Dialog_LookImagePicker(stored, true, r => custom.GetOrCreate(key).SetAtlas(state, r)))))
                layer?.SetAtlas(state, null);
        }

        private void DrawLookImage(Listing_Standard listing, CustomThemeData data, string key)
        {
            var layer = data.Layer(key);
            var worker = LookWorker;
            var def = key == null ? null : DefDatabase<MainButtonDef>.GetNamedSilentFail(key);
            var resolved = worker == null ? null : key == null ? worker.AllButtonsImage() : worker.ButtonImage(def);
            var values = resolved ?? new BarButtonImage();
            string suffix = "." + (key ?? "<all>");

            // The picture.
            string stored = layer?.image;
            bool set = !stored.NullOrEmpty();
            string text = "MBBS_Look_ImagePicture".Translate() + ": " +
                          (set ? CustomThemes.Describe(stored) : resolved == null ? "MBBS_Look_ImageNone".Translate().ToString() : FollowBelowLabel(key));
            Texture2D shown = set ? CustomThemes.Texture(stored) : resolved is ResolvedButtonImage r ? r.Texture : working.AppearanceDef?.Tex(resolved?.texture);
            var custom = working.customTheme;
            if (ImageRow(listing, text, shown, false, set,
                    () => Find.WindowStack.Add(new Dialog_LookImagePicker(stored, false, reference => custom.GetOrCreate(key).image = reference))))
                if (layer != null) layer.image = null;

            // Fill.
            var storedFit = layer?.imageFit ?? LookImageFit.Default;
            if (SectionHeader(listing, "MBBS_Look_ImageFit".Translate(), storedFit != LookImageFit.Default))
                data.GetOrCreate(key).imageFit = LookImageFit.Default;
            foreach (var fit in LookFits)
            {
                string label = fit == LookImageFit.Default ? FollowBelowLabel(key) : ("MBBS_Look_ImageFit_" + fit).Translate().ToString();
                if (listing.RadioButton(label, storedFit == fit, L.radioIndent) && storedFit != fit)
                    data.GetOrCreate(key).imageFit = fit;
            }

            // Anchor: which side Cover keeps, where Fit puts the picture.
            var storedH = layer?.imageAnchor ?? LookImageAnchor.Default;
            var storedV = layer?.imageVerticalAnchor ?? LookImageVerticalAnchor.Default;
            if (SectionHeader(listing, "MBBS_Look_ImageAnchor".Translate(), storedH != LookImageAnchor.Default || storedV != LookImageVerticalAnchor.Default))
            {
                var l = data.GetOrCreate(key);
                l.imageAnchor = LookImageAnchor.Default;
                l.imageVerticalAnchor = LookImageVerticalAnchor.Default;
            }
            var grid = listing.GetRect(L.lookAnchorCell * 3f);
            for (int i = 0; i < 9; i++)
            {
                var h = (LookImageAnchor)(i % 3 + 1);
                var v = (LookImageVerticalAnchor)(i / 3 + 1);
                bool chosen = CustomThemeData.AnchorOf(h, DecorationAnchor.Center) == values.anchor &&
                              CustomThemeData.VerticalOf(v, DecorationVerticalAnchor.Center) == values.verticalAnchor;
                if (!AnchorCell(grid, i, chosen)) continue;
                var l = data.GetOrCreate(key);
                l.imageAnchor = h;
                l.imageVerticalAnchor = v;
            }

            int opacity = Mathf.RoundToInt(values.opacity * 100f);
            int storedOpacity = layer?.imageOpacity ?? BarConfig.Unset;
            int newOpacity = IntSetting(listing, "MBBS_Look_ImageOpacity".Translate(opacity), storedOpacity, opacity, R.lookImageOpacity, R.percentStep, "lookImageOpacity" + suffix);
            if (newOpacity != storedOpacity) data.GetOrCreate(key).imageOpacity = newOpacity;

            int inset = Mathf.RoundToInt(values.inset);
            int storedInset = layer?.imageInset ?? BarConfig.Unset;
            int newInset = IntSetting(listing, "MBBS_Look_ImageInset".Translate(inset), storedInset, inset, R.lookImageInset, 1, "lookImageInset" + suffix);
            if (newInset != storedInset) data.GetOrCreate(key).imageInset = newInset;
        }

        private static readonly LookImageFit[] LookFits = { LookImageFit.Default, LookImageFit.Cover, LookImageFit.Fit, LookImageFit.Stretch };
    }
}
