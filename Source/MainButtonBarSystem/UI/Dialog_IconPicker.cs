using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Picks a button icon (spec 3.2): one the game's buttons already use, or a PNG from the icons folder outside the
    /// mod folder. Calls back with the stored value (texture path or "file:Name.png"), or null for the button's own.
    /// </summary>
    public class Dialog_IconPicker : Window
    {
        private enum Source
        {
            BuiltIn,
            Imported,
        }

        private static Source source = Source.BuiltIn;

        private readonly Action<string> onPick;
        private readonly string current;
        private readonly List<TabRecord> tabs = new List<TabRecord>();
        private Vector2 scroll;
        private List<string> builtIn;
        private List<string> imported;

        private static MainButtonBarTuningDef.SettingsWindowLayout L => MainButtonBarTuningDef.Current.settingsWindow;

        public override Vector2 InitialSize => L.iconPickerSize;

        public Dialog_IconPicker(string current, Action<string> onPick)
        {
            this.current = current;
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
            CustomIcons.Refresh();
            builtIn = CustomIcons.BuiltInIcons();
            imported = CustomIcons.ImportedIcons();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - L.closeButtonSpace, L.headerHeight), "MBBS_IconPickerTitle".Translate());
            Text.Font = GameFont.Small;

            float top = inRect.y + L.headerHeight + TabDrawer.TabHeight;
            float footer = L.footerButtonHeight + L.footerGap;
            var content = new Rect(inRect.x, top, inRect.width, inRect.yMax - top - footer);
            tabs.Clear();
            tabs.Add(new TabRecord("MBBS_IconPicker_BuiltIn".Translate(), () => source = Source.BuiltIn, source == Source.BuiltIn));
            tabs.Add(new TabRecord("MBBS_IconPicker_Imported".Translate(), () => source = Source.Imported, source == Source.Imported));
            TabDrawer.DrawTabs(content, tabs, L.tabButtonWidth);
            Widgets.DrawMenuSection(content);

            var list = source == Source.BuiltIn ? builtIn : imported;
            var inner = content.ContractedBy(L.detailPadding);
            if (list.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(inner, "MBBS_IconPicker_Empty".TranslateKeepCase(ModPaths.IconsFolder));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                DrawGrid(inner, list);
            }

            // Footer: back to the button's own icon; for imported icons, open the folder and reload it.
            float y = inRect.yMax - L.footerButtonHeight;
            string useOriginal = "MBBS_IconPicker_UseOriginal".Translate();
            if (Widgets.ButtonText(new Rect(inRect.x, y, UIUtil.FitButton(L.moveButtonWidth, useOriginal), L.footerButtonHeight), useOriginal))
                Pick(null);
            if (source == Source.Imported)
            {
                string open = "MBBS_IconPicker_OpenFolder".Translate(), refresh = "MBBS_IconPicker_Refresh".Translate();
                float refreshWidth = UIUtil.FitButton(L.moveButtonWidth, refresh);
                float openWidth = UIUtil.FitButton(L.moveButtonWidth, open);
                if (Widgets.ButtonText(new Rect(inRect.xMax - refreshWidth - L.footerGap - openWidth, y, openWidth, L.footerButtonHeight), open))
                    CustomIcons.OpenFolder();
                if (Widgets.ButtonText(new Rect(inRect.xMax - refreshWidth, y, refreshWidth, L.footerButtonHeight), refresh))
                    Reload();
            }
        }

        private void DrawGrid(Rect rect, List<string> icons)
        {
            float cell = L.iconPickerCell;
            int columns = Mathf.Max(1, Mathf.FloorToInt((rect.width - GenUI.ScrollBarWidth) / cell));
            int rows = Mathf.CeilToInt(icons.Count / (float)columns);
            var view = new Rect(0f, 0f, rect.width - GenUI.ScrollBarWidth, rows * cell);
            Widgets.BeginScrollView(rect, ref scroll, view);
            for (int i = 0; i < icons.Count; i++)
            {
                var r = new Rect((i % columns) * cell, (i / columns) * cell, cell, cell);
                var tex = CustomIcons.Resolve(icons[i]);
                if (icons[i] == current) Widgets.DrawHighlightSelected(r);
                else Widgets.DrawHighlightIfMouseover(r);
                if (tex != null) Widgets.DrawTextureFitted(r.ContractedBy(L.rowIconPadding * 2f), tex, 1f);
                TooltipHandler.TipRegion(r, CustomIcons.IsFile(icons[i]) ? CustomIcons.FileName(icons[i]) : icons[i]);
                if (Widgets.ButtonInvisible(r)) Pick(icons[i]);
            }
            Widgets.EndScrollView();
        }

        private void Pick(string icon)
        {
            onPick(icon);
            Close();
        }
    }
}
