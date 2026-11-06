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

        private static readonly ButtonVisualState[] LookStates =
            { ButtonVisualState.Normal, ButtonVisualState.Mouseover, ButtonVisualState.Active, ButtonVisualState.Disabled };

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

            var layer = working.customTheme.Layer(name);
            string label = name == null ? "MBBS_Look_All".Translate().ToString() : DisplayLabel(name, working.Get(name));
            if (layer != null && !layer.IsEmpty) label += "  " + "MBBS_Look_ChangedSuffix".Translate();
            Text.Anchor = TextAnchor.MiddleLeft;
            var labelRect = new Rect(x, row.y, row.xMax - x, row.height);
            Widgets.Label(labelRect, label.Truncate(labelRect.width));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(row)) lookSelected = name;
        }

        // ---------------------------------------------------------------- detail

        private BarAppearanceWorker BaseWorker => working.AppearanceDef?.Worker ?? new BarAppearanceWorker();

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
            listing.Label((key == null ? "MBBS_Look_AllTip" : "MBBS_Look_ButtonTip").Translate(working.AppearanceDef?.LabelCap ?? "?"));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            var resetRow = listing.GetRect(L.textRowHeight);
            string reset = "MBBS_Look_Reset".Translate();
            var layer = data.Layer(key);
            if (DimmableButton(new Rect(resetRow.x, resetRow.y, UIUtil.FitButton(L.moveButtonWidth, reset), resetRow.height), reset, layer != null && !layer.IsEmpty))
                data.Reset(key);

            // Preview: the four states side by side, drawn by this mod (other mods' DoButton effects show only on the bar).
            float h = MainButtonBarTuningDef.Current.behaviour.buttonRectHeight;
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

            data.Prune();
            lookDetailHeight = EndScrolledListing(listing);
        }

        private void DrawLookPreview(Rect rect, string key, ButtonVisualState state)
        {
            var data = working.customTheme;
            var baseWorker = BaseWorker;
            var tex = data.AtlasFor(key, state, working.AppearanceDef, out bool replacesTint) ?? Widgets.ButtonSubtleAtlas;
            Widgets.DrawAtlas(rect, tex);
            if (!replacesTint && (state == ButtonVisualState.Mouseover || state == ButtonVisualState.Active)) Widgets.DrawHighlight(rect);
            if (state == ButtonVisualState.Disabled) return;   // the game draws no label on a disabled button

            var color = data.LabelColorFor(key, state, working.AppearanceDef) ?? Color.white;
            var font = data.FontFor(key) switch
            {
                LookFont.Tiny => GameFont.Tiny,
                LookFont.Medium => GameFont.Medium,
                _ => GameFont.Small,
            };
            var align = data.AlignFor(key);
            if (align == LabelAlign.Default) align = working.labelAlign;
            var anchor = align switch
            {
                LabelAlign.Left => TextAnchor.MiddleLeft,
                LabelAlign.Right => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleCenter,
            };
            string label = key == null ? "MBBS_Look_Sample".Translate().ToString() : DisplayLabel(key, working.Get(key));
            var def = key == null ? null : DefDatabase<MainButtonDef>.GetNamedSilentFail(key);
            var icon = def == null ? null : ButtonPainter.EffectiveIcon(def, working.Get(key));
            bool iconLook = data.IconFor(key, Mathf.RoundToInt(MainButtonBarTuningDef.Current.behaviour.iconSize), out int size, out var iconAnchor, out var offset);
            var mode = def == null ? IconMode.Text : working.IconModeFor(working.Get(key));
            // The preview cannot know whether another mod would add the label next to the icon on the bar.
            if (mode == IconMode.Default) mode = icon != null ? IconMode.Icon : IconMode.Text;

            var oldFont = Text.Font;
            var oldColor = GUI.color;
            var textRect = rect;
            if (icon != null && (mode == IconMode.Icon || mode == IconMode.Both))
            {
                var b = MainButtonBarTuningDef.Current.behaviour;
                DecorationBox box;
                if (mode == IconMode.Icon) box = IconPlacement.Alone(rect.x, rect.y, rect.width, rect.height, size, iconAnchor, b.labelSidePadding, offset.x, offset.y);
                else
                {
                    box = IconPlacement.Beside(rect.x, rect.y, rect.width, rect.height, size, iconAnchor, b.labelSidePadding, b.iconTextGap, offset.x, offset.y, out float tl, out float tr);
                    textRect = new Rect(tl, rect.y, Mathf.Max(0f, tr - tl), rect.height);
                }
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(box.X, box.Y, box.Width, box.Height), icon);
                if (mode == IconMode.Icon) label = null;
            }
            if (label != null)
            {
                Text.Font = font;
                Text.Anchor = anchor;
                GUI.color = color;
                Widgets.Label(textRect.ContractedBy(MainButtonBarTuningDef.Current.behaviour.labelSidePadding, 0f), label.Truncate(textRect.width));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            Text.Font = oldFont;
            GUI.color = oldColor;
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
                data.GetOrCreate(key).font = LookFont.Default;
            foreach (var font in new[] { LookFont.Default, LookFont.Tiny, LookFont.Small, LookFont.Medium })
            {
                string label = font == LookFont.Default ? FollowBelowLabel(key) : ("MBBS_Look_Font_" + font).Translate().ToString();
                if (listing.RadioButton(label, stored == font, L.radioIndent) && stored != font)
                    data.GetOrCreate(key).font = font;
            }
        }

        /// <summary>"Follow all buttons" for a button; "follow the theme" for the all-buttons layer.</summary>
        private static string FollowBelowLabel(string key) =>
            (key == null ? "MBBS_Look_FollowTheme" : "MBBS_Look_FollowAll").Translate();

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
            var swatchRow = listing.GetRect(L.textRowHeight);
            var swatch = new Rect(swatchRow.x, swatchRow.y + 2f, swatchRow.height * 2f, swatchRow.height - 4f);
            Widgets.DrawBoxSolid(swatch, resolved);
            Widgets.DrawBox(swatch);
            if (!set)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(swatch.xMax + L.footerGap, swatchRow.y, swatchRow.width - swatch.width - L.footerGap, swatchRow.height),
                    FollowBelowLabel(key));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            string id = "lookColor." + which + "." + (key ?? "<all>");
            float r = NumberSlider(listing, Mathf.Round(resolved.r * 255f), 0f, 255f, 1f, id + ".r");
            float g = NumberSlider(listing, Mathf.Round(resolved.g * 255f), 0f, 255f, 1f, id + ".g");
            float b = NumberSlider(listing, Mathf.Round(resolved.b * 255f), 0f, 255f, 1f, id + ".b");
            if (!Mathf.Approximately(r, Mathf.Round(resolved.r * 255f)) || !Mathf.Approximately(g, Mathf.Round(resolved.g * 255f)) ||
                !Mathf.Approximately(b, Mathf.Round(resolved.b * 255f)))
                data.GetOrCreate(key).SetColor(which, new Color(r / 255f, g / 255f, b / 255f, 1f));
        }

        private void DrawLookIcon(Listing_Standard listing, CustomThemeData data, string key)
        {
            var layer = data.Layer(key);
            int defaultSize = Mathf.RoundToInt(MainButtonBarTuningDef.Current.behaviour.iconSize);
            data.IconFor(key, defaultSize, out int size, out var anchor, out var offset);
            string suffix = "." + (key ?? "<all>");

            int storedSize = layer?.iconSize ?? BarConfig.Unset;
            int newSize = IntSetting(listing, "MBBS_Look_IconSize".Translate(size), storedSize, size, R.lookIconSize, 1, "lookIconSize" + suffix);
            if (newSize != storedSize) data.GetOrCreate(key).iconSize = newSize;

            var storedAnchor = layer?.iconAnchor ?? IconAnchor.Default;
            if (SectionHeader(listing, "MBBS_Look_IconAnchor".Translate(), storedAnchor != IconAnchor.Default))
                data.GetOrCreate(key).iconAnchor = IconAnchor.Default;
            float cellSize = L.lookAnchorCell;
            var grid = listing.GetRect(cellSize * 3f);
            for (int i = 0; i < 9; i++)
            {
                var a = (IconAnchor)(i + 1);
                var cell = new Rect(grid.x + L.radioIndent + (i % 3) * cellSize, grid.y + (i / 3) * cellSize, cellSize, cellSize).ContractedBy(1f);
                bool chosen = anchor == a;
                Widgets.DrawBoxSolid(cell, chosen ? L.viewSwitchSelectedColor : L.viewSwitchColor);
                Widgets.DrawBox(cell);
                var dot = IconPlacement.Alone(cell.x, cell.y, cell.width, cell.height, cellSize / 3f, a, 2f, 0f, 0f);
                Widgets.DrawBoxSolid(new Rect(dot.X, dot.Y, dot.Width, dot.Height), Color.white);
                if (!chosen) Widgets.DrawHighlightIfMouseover(cell);
                if (Widgets.ButtonInvisible(cell)) data.GetOrCreate(key).iconAnchor = a;
            }

            int storedX = layer?.iconOffsetX ?? BarConfig.UnsetOffset;
            int newX = OffsetSetting(listing, "MBBS_Look_IconOffsetX".Translate((int)offset.x), storedX, (int)offset.x, R.lookIconOffset, "lookIconOffsetX" + suffix);
            if (newX != storedX) data.GetOrCreate(key).iconOffsetX = newX;
            int storedY = layer?.iconOffsetY ?? BarConfig.UnsetOffset;
            int newY = OffsetSetting(listing, "MBBS_Look_IconOffsetY".Translate((int)offset.y), storedY, (int)offset.y, R.lookIconOffset, "lookIconOffsetY" + suffix);
            if (newY != storedY) data.GetOrCreate(key).iconOffsetY = newY;
        }

        private void DrawLookAtlas(Listing_Standard listing, CustomThemeData data, string key, ButtonVisualState state)
        {
            var layer = data.Layer(key);
            string stored = layer?.AtlasOf(state);
            bool set = !stored.NullOrEmpty();

            string choose = "MBBS_IconChoose".Translate();
            float chooseWidth = UIUtil.FitButton(L.smallButtonWidth, choose);
            float right = ResetWidth + L.footerGap + chooseWidth;
            float thumbWidth = L.textRowHeight * 2f;
            float labelWidth = listing.ColumnWidth - thumbWidth - L.footerGap - right - L.footerGap;
            string text = ("MBBS_Look_State_" + state).Translate() + ": " + (set ? AtlasName(stored) : FollowBelowLabel(key));
            // A label wider than its space (some translations) wraps onto more lines; the thumbnail and the buttons
            // stay on the first line. A window too narrow for any label keeps one line instead of a huge row.
            float labelHeight = labelWidth > 0f ? Text.CalcHeight(text, labelWidth) : L.textRowHeight;
            var row = listing.GetRect(Mathf.Max(L.textRowHeight, labelHeight));
            var line = new Rect(row.x, row.y, row.width, L.textRowHeight);

            var shown = set ? CustomThemes.AtlasTexture(stored) : data.AtlasFor(key, state, working.AppearanceDef, out _) ?? Widgets.ButtonSubtleAtlas;
            var thumb = new Rect(line.x, line.y, thumbWidth, line.height);
            if (shown != null) Widgets.DrawAtlas(thumb, shown);

            // One line: centred on the row like before. More lines: from the top, level with the buttons.
            Text.Anchor = row.height > L.textRowHeight ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            var labelRect = new Rect(thumb.xMax + L.footerGap, row.y, Mathf.Max(0f, labelWidth), row.height);
            Widgets.Label(labelRect, text);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(new Rect(line.xMax - right, line.y, chooseWidth, line.height), choose))
                Find.WindowStack.Add(new FloatMenu(AtlasOptions(key, state)));
            if (DefaultButton(line, set))
                layer?.SetAtlas(state, null);
        }

        private static string AtlasName(string reference)
        {
            int bar = reference.IndexOf('|');
            var theme = bar > 0 ? DefDatabase<BarAppearanceDef>.GetNamedSilentFail(reference.Substring(0, bar)) : null;
            if (theme == null) return "MBBS_Look_AtlasMissing".Translate(reference);
            return "MBBS_Look_AtlasFrom".Translate(theme.LabelCap, ("MBBS_Look_Field_" + reference.Substring(bar + 1)).Translate());
        }

        /// <summary>Every background an installed theme has, for one state of one layer.</summary>
        private List<FloatMenuOption> AtlasOptions(string key, ButtonVisualState state)
        {
            var options = new List<FloatMenuOption>();
            foreach (var theme in BarModel.Appearances)
            {
                foreach (var field in CustomThemes.AtlasFields)
                {
                    string path = CustomThemes.FieldValue(theme, field);
                    if (path.NullOrEmpty()) continue;
                    string reference = CustomThemes.AtlasRef(theme, field);
                    var tex = CustomThemes.AtlasTexture(reference);
                    if (tex == null) continue;
                    options.Add(new FloatMenuOption(AtlasName(reference), () => (working.customTheme ??= new CustomThemeData()).GetOrCreate(key).SetAtlas(state, reference), tex, Color.white));
                }
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("MBBS_Look_NoAtlas".Translate(), null));
            return options;
        }
    }
}
