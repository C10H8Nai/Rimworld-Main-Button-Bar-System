using System;
using System.Collections.Generic;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Picks an image for a custom look (design spec 2026-11-05 §3.6, §6): an installed theme's button background or
    /// button picture, or a PNG imported into <see cref="ModPaths.ImagesFolder"/>. Calls back with the image reference
    /// (<see cref="ImageRef"/>). For a button background, images larger than the game's nine-slice can use are marked
    /// (spec 4.12): they still work, the corners just stop growing.
    /// </summary>
    public class Dialog_LookImagePicker : Window
    {
        private enum Source
        {
            Themes,
            Imported,
        }

        private static Source source = Source.Themes;

        private readonly Action<string> onPick;
        private readonly string current;
        private readonly bool forBackground;
        private readonly List<TabRecord> tabs = new List<TabRecord>();
        private Vector2 scroll;
        private List<(BarAppearanceDef theme, string reference)> themeImages;
        private List<string> imported;

        private static MainButtonBarTuningDef.SettingsWindowLayout L => MainButtonBarTuningDef.Current.settingsWindow;

        public override Vector2 InitialSize => L.imagePickerSize;

        /// <param name="forBackground">A button background (nine-slice), so oversized images are marked.</param>
        public Dialog_LookImagePicker(string current, bool forBackground, Action<string> onPick)
        {
            this.current = current;
            this.forBackground = forBackground;
            this.onPick = onPick;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;
            forcePause = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Reload();
        }

        private void Reload()
        {
            CustomThemes.RefreshImages();
            themeImages = CustomThemes.ThemeImages();
            imported = CustomThemes.ImportedImages();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - L.closeButtonSpace, L.headerHeight), "MBBS_ImagePickerTitle".Translate());
            Text.Font = GameFont.Small;

            float top = inRect.y + L.headerHeight + TabDrawer.TabHeight;
            float footer = L.footerButtonHeight + L.footerGap;
            var content = new Rect(inRect.x, top, inRect.width, inRect.yMax - top - footer);
            tabs.Clear();
            tabs.Add(new TabRecord("MBBS_ImagePicker_Themes".Translate(), () => source = Source.Themes, source == Source.Themes));
            tabs.Add(new TabRecord("MBBS_IconPicker_Imported".Translate(), () => source = Source.Imported, source == Source.Imported));
            TabDrawer.DrawTabs(content, tabs, L.tabButtonWidth);
            Widgets.DrawMenuSection(content);

            var inner = content.ContractedBy(L.detailPadding);
            int count = source == Source.Themes ? themeImages.Count : imported.Count;
            if (count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(inner, source == Source.Themes ? "MBBS_Look_NoAtlas".Translate().ToString() : "MBBS_ImagePicker_Empty".TranslateKeepCase(ModPaths.ImagesFolder));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                DrawGrid(inner, count);
            }

            if (source != Source.Imported) return;
            float y = inRect.yMax - L.footerButtonHeight;
            string open = "MBBS_IconPicker_OpenFolder".Translate(), refresh = "MBBS_IconPicker_Refresh".Translate();
            float refreshWidth = UIUtil.FitButton(L.moveButtonWidth, refresh);
            float openWidth = UIUtil.FitButton(L.moveButtonWidth, open);
            if (Widgets.ButtonText(new Rect(inRect.xMax - refreshWidth - L.footerGap - openWidth, y, openWidth, L.footerButtonHeight), open))
                CustomThemes.OpenImagesFolder();
            if (Widgets.ButtonText(new Rect(inRect.xMax - refreshWidth, y, refreshWidth, L.footerButtonHeight), refresh))
                Reload();
        }

        private void DrawGrid(Rect rect, int count)
        {
            float cell = L.imagePickerCell;
            int columns = Mathf.Max(1, Mathf.FloorToInt((rect.width - GenUI.ScrollBarWidth) / cell));
            int rows = Mathf.CeilToInt(count / (float)columns);
            var view = new Rect(0f, 0f, rect.width - GenUI.ScrollBarWidth, rows * cell);
            int maxSize = MainButtonBarTuningDef.Current.behaviour.themeAtlasMaxSize;
            Widgets.BeginScrollView(rect, ref scroll, view);
            for (int i = 0; i < count; i++)
            {
                string reference = source == Source.Themes ? themeImages[i].reference : imported[i];
                var r = new Rect((i % columns) * cell, (i / columns) * cell, cell, cell);
                var tex = CustomThemes.Texture(reference);
                if (reference == current) Widgets.DrawHighlightSelected(r);
                else Widgets.DrawHighlightIfMouseover(r);
                var box = r.ContractedBy(L.rowIconPadding * 2f);
                if (tex != null)
                {
                    if (forBackground && ImageRef.Parse(reference).Kind == ImageRefKind.ThemeField) Widgets.DrawAtlas(box, tex);
                    else Widgets.DrawTextureFitted(box, tex, 1f);
                }

                string tip = CustomThemes.Describe(reference);
                if (tex != null)
                {
                    tip += "\n" + tex.width + "×" + tex.height;
                    if (forBackground && (tex.width > maxSize || tex.height > maxSize))
                    {
                        tip += "\n" + "MBBS_ImagePicker_TooBig".Translate(maxSize);
                        GUI.color = L.reportWarningColor;
                        Widgets.DrawBox(r);
                        GUI.color = Color.white;
                    }
                }
                TooltipHandler.TipRegion(r, tip);
                if (Widgets.ButtonInvisible(r))
                {
                    onPick(reference);
                    Close();
                }
            }
            Widgets.EndScrollView();
        }
    }
}
