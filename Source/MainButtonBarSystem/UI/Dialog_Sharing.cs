using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    public enum ExportTarget
    {
        Clipboard,
        File,
        ModXml,
        ModFolder,

        /// <summary>The Appearance tab's custom look, saved as a custom theme (no button list, no packageId).</summary>
        Theme,
    }

    /// <summary>
    /// Asks for the name, author and description stored with an export (and the packageId of an exported mod), then
    /// hands them on. The author is remembered between sessions.
    /// </summary>
    public class Dialog_ArrangementInfo : Window
    {
        private static string lastName = "";
        private static string lastDescription = "";

        /// <summary>PackageIds exported this session: the game only learns about new mod folders after a restart.</summary>
        private static readonly HashSet<string> exportedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>For names without ASCII letters: keeps automatic packageIds apart.</summary>
        private readonly string packageNameFallback = "Arrangement" + DateTime.Now.ToString("MMddHHmm");

        private readonly ExportTarget target;
        private readonly Action<string, string, string, string> onConfirm;
        private string name;
        private string author;
        private string description;
        private string packageId;
        private bool packageIdEdited;

        private static MainButtonBarTuningDef.SettingsWindowLayout L => MainButtonBarTuningDef.Current.settingsWindow;

        public override Vector2 InitialSize => L.exportDialogSize;

        /// <param name="onConfirm">Called with name, author, description and packageId (null unless exporting a mod).</param>
        public Dialog_ArrangementInfo(ExportTarget target, Action<string, string, string, string> onConfirm)
        {
            this.target = target;
            this.onConfirm = onConfirm;
            name = lastName;
            description = lastDescription;
            author = MainButtonBarSystemMod.Settings.exportAuthor;
            if (author.NullOrEmpty()) author = PlayerName();
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            onlyOneOfTypeAllowed = true;
            forcePause = false;
        }

        private bool NeedsPackageId => target == ExportTarget.ModFolder;

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - L.closeButtonSpace, L.headerHeight), ("MBBS_ExportTitle_" + target).Translate());
            Text.Font = GameFont.Small;

            float y = inRect.y + L.headerHeight;
            float labelWidth = inRect.width * 0.28f;
            float fieldX = inRect.x + labelWidth;
            float fieldWidth = inRect.width - labelWidth;
            float row = L.rowHeight;

            name = Field(inRect.x, ref y, labelWidth, fieldX, fieldWidth, row, "MBBS_Export_Name", name);
            author = Field(inRect.x, ref y, labelWidth, fieldX, fieldWidth, row, "MBBS_Export_Author", author);

            Widgets.Label(new Rect(inRect.x, y, labelWidth, row), "MBBS_Export_Description".Translate());
            description = Widgets.TextArea(new Rect(fieldX, y, fieldWidth, L.descriptionBoxHeight), description ?? "");
            y += L.descriptionBoxHeight + L.footerGap;

            if (NeedsPackageId)
            {
                if (!packageIdEdited) packageId = SharingNames.PackageId(author, name, packageNameFallback);
                string edited = Field(inRect.x, ref y, labelWidth, fieldX, fieldWidth, row, "MBBS_Export_PackageId", packageId);
                if (edited != packageId)
                {
                    packageId = edited;
                    packageIdEdited = true;
                }
            }

            GUI.color = Color.gray;
            string hint = ("MBBS_ExportHint_" + target).TranslateKeepCase(ModPaths.ExportsFolder, GenFilePaths.ModsFolderPath, ModPaths.ThemesFolder);
            float hintHeight = Text.CalcHeight(hint, inRect.width);
            Widgets.Label(new Rect(inRect.x, y, inRect.width, hintHeight), hint);
            GUI.color = Color.white;
            y += hintHeight + L.footerGap;

            string error = Validate();
            if (error != null)
            {
                GUI.color = L.reportWarningColor;
                Widgets.Label(new Rect(inRect.x, y, inRect.width, Text.CalcHeight(error, inRect.width)), error);
                GUI.color = Color.white;
            }

            float h = L.footerButtonHeight;
            float half = (inRect.width - L.footerGap) / 2f;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - h, half, h), "CancelButton".Translate()))
                Close();
            var confirm = new Rect(inRect.x + half + L.footerGap, inRect.yMax - h, half, h);
            if (error != null) GUI.color = Color.gray;
            bool clicked = Widgets.ButtonText(confirm, ("MBBS_ExportConfirm_" + target).Translate(), active: error == null);
            GUI.color = Color.white;
            if (clicked && error == null)
            {
                lastName = name;
                lastDescription = description;
                MainButtonBarSystemMod.Settings.exportAuthor = author;
                MainButtonBarSystemMod.Instance.WriteSettings();
                if (NeedsPackageId) exportedPackageIds.Add(packageId.Trim());
                Close();
                onConfirm(name?.Trim(), author?.Trim(), description?.Trim(), NeedsPackageId ? packageId.Trim() : null);
            }
        }

        private string Validate()
        {
            if ((target == ExportTarget.ModFolder || target == ExportTarget.ModXml || target == ExportTarget.Theme) && name.NullOrEmpty())
                return "MBBS_Export_NeedName".Translate();
            if (!NeedsPackageId) return null;
            if (!SharingNames.IsValidPackageId(packageId?.Trim())) return "MBBS_Export_BadPackageId".Translate();
            string id = packageId.Trim();
            if (ModLister.GetModWithIdentifier(id, true) != null || exportedPackageIds.Contains(id)) return "MBBS_Export_PackageIdTaken".TranslateKeepCase(id);
            return null;
        }

        private static string Field(float x, ref float y, float labelWidth, float fieldX, float fieldWidth, float row, string key, string value)
        {
            Widgets.Label(new Rect(x, y, labelWidth, row), key.Translate());
            value = Widgets.TextField(new Rect(fieldX, y, fieldWidth, row), value ?? "", L.customNameMaxLength * 2);
            y += row + L.footerGap;
            return value;
        }

        public static string PlayerName()
        {
            try
            {
                return SteamUtility.SteamPersonaName ?? "";
            }
            catch
            {
                return "";
            }
        }
    }

    /// <summary>
    /// Shows what a shared arrangement contains and how it fits this game before importing it: who made it, buttons
    /// both games have, buttons from mods this game lacks, a missing theme or icons. The player picks which parts to
    /// take (spec 3.9). Nothing is saved until the settings window's Save.
    /// </summary>
    public class Dialog_ImportArrangement : Window
    {
        private readonly SharedArrangement shared;
        private readonly BarConfig current;
        private readonly Action<BarConfig> apply;
        private readonly List<(string text, bool warning)> report = new List<(string, bool)>();
        private readonly ArrangementParts available;
        private ArrangementParts chosen;
        private Vector2 scroll;

        private static MainButtonBarTuningDef.SettingsWindowLayout L => MainButtonBarTuningDef.Current.settingsWindow;

        private static readonly ArrangementParts[] Parts =
            { ArrangementParts.Layout, ArrangementParts.Sizes, ArrangementParts.Names, ArrangementParts.Icons, ArrangementParts.Theme };

        public override Vector2 InitialSize => L.importDialogSize;

        public Dialog_ImportArrangement(SharedArrangement shared, BarConfig current, Action<BarConfig> apply)
        {
            this.shared = shared;
            this.current = current;
            this.apply = apply;
            available = ArrangementMerge.PartsSet(shared.config);
            chosen = available;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            onlyOneOfTypeAllowed = true;
            forcePause = false;
            BuildReport();
        }

        private void BuildReport()
        {
            var meta = shared.meta;
            var config = shared.config;
            if (!meta?.author.NullOrEmpty() == true) report.Add(("MBBS_ImportReport_By".TranslateKeepCase(meta.author), false));
            if (!meta?.description.NullOrEmpty() == true) report.Add((meta.description, false));
            if (!meta?.gameVersion.NullOrEmpty() == true)
                report.Add(("MBBS_ImportReport_MadeWith".Translate(meta.gameVersion, meta.modVersion ?? "?"), false));
            report.Add(("MBBS_ImportReport_Source".TranslateKeepCase(shared.source ?? "?"), false));

            var match = shared.Match();
            report.Add(("MBBS_ImportReport_Matched".Translate(match.Matched.Count), false));
            if (match.MissingHere.Count > 0)
            {
                report.Add(("MBBS_ImportReport_Missing".Translate(match.MissingHere.Count), true));
                foreach (var group in match.MissingHere.GroupBy(n => meta?.ModOf(n) ?? "MBBS_ImportReport_UnknownMod".Translate().ToString()))
                    report.Add(("    " + group.Key + ": " + Names(group.ToList()), true));
            }
            if (match.TheirListKnown)
            {
                if (match.OnlyHere.Count > 0)
                    report.Add(("MBBS_ImportReport_OnlyHere".Translate(match.OnlyHere.Count, Names(match.OnlyHere.Select(Label).ToList())), false));
            }
            else
            {
                report.Add(("MBBS_ImportReport_NoList".Translate(), false));
            }

            if ((available & ArrangementParts.Theme) != 0)
            {
                var theme = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(config.appearance);
                if (theme != null) report.Add(("MBBS_ImportReport_Theme".Translate(theme.LabelCap), false));
                else report.Add(("MBBS_ImportReport_ThemeMissing".TranslateKeepCase(config.appearance, meta?.appearanceMod ?? "?"), true));
                if (config.UsesCustomTheme && !config.customTheme.IsEmpty)
                {
                    report.Add(("MBBS_ImportReport_CustomTheme".Translate(config.customTheme.buttons.Count(b => b != null && !b.IsEmpty)), false));
                    // Spec 4.18: images the look uses that are not here; those buttons fall back to the theme's own.
                    var missingImages = config.customTheme.ImageRefs().Distinct().Where(CustomThemes.IsMissing).Select(CustomThemes.ShortName).ToList();
                    if (missingImages.Count > 0)
                        report.Add(("MBBS_ImportReport_ThemeImagesMissing".TranslateKeepCase(missingImages.Count, Names(missingImages), ModPaths.ImagesFolder), true));
                }
            }
            if ((config.displayMode ?? BarConfig.VanillaMode) != BarConfig.VanillaMode && DefDatabase<BarDisplayModeDef>.GetNamedSilentFail(config.displayMode) == null)
                report.Add(("MBBS_ImportReport_ModeMissing".TranslateKeepCase(config.displayMode, meta?.displayModeMod ?? "?"), true));

            var icons = config.overrides.Where(o => o != null && !o.icon.NullOrEmpty() && match.KnownToThem(o.defName)).ToList();
            var missingFiles = icons.Where(o => CustomIcons.IsFile(o.icon) && CustomIcons.IsMissing(o.icon)).Select(o => CustomIcons.FileName(o.icon)).Distinct().ToList();
            var missingTextures = icons.Where(o => !CustomIcons.IsFile(o.icon) && CustomIcons.IsMissing(o.icon)).Select(o => o.icon).Distinct().ToList();
            if (missingFiles.Count > 0)
                report.Add(("MBBS_ImportReport_IconFilesMissing".TranslateKeepCase(missingFiles.Count, Names(missingFiles), ModPaths.IconsFolder), true));
            if (missingTextures.Count > 0)
                report.Add(("MBBS_ImportReport_IconTexturesMissing".TranslateKeepCase(missingTextures.Count, Names(missingTextures)), true));
        }

        private static string Label(string defName) => DefDatabase<MainButtonDef>.GetNamedSilentFail(defName)?.LabelCap.ToString() ?? defName;

        private static string Names(List<string> names)
        {
            int max = Mathf.Max(1, L.importReportMaxNames);
            string shown = string.Join(", ", names.Take(max));
            return names.Count > max ? shown + " " + "MBBS_ImportReport_More".Translate(names.Count - max) : shown;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - L.closeButtonSpace, L.headerHeight), "MBBS_ImportTitle".TranslateKeepCase(shared.Title).Truncate(inRect.width - L.closeButtonSpace));
            Text.Font = GameFont.Small;

            float row = L.rowHeight;
            float partsHeight = row * (Parts.Length + 1) + L.footerGap;
            float footer = L.footerButtonHeight + L.footerGap;
            float top = inRect.y + L.headerHeight;

            // Report
            var reportRect = new Rect(inRect.x, top, inRect.width, inRect.yMax - top - partsHeight - footer);
            Widgets.DrawMenuSection(reportRect);
            var inner = reportRect.ContractedBy(L.detailPadding);
            float width = inner.width - GenUI.ScrollBarWidth;
            float height = report.Sum(r => Text.CalcHeight(r.text, width) + 2f);
            var view = new Rect(0f, 0f, width, height);
            Widgets.BeginScrollView(inner, ref scroll, view);
            float y = 0f;
            foreach (var (text, warning) in report)
            {
                float h = Text.CalcHeight(text, width);
                if (warning) GUI.color = L.reportWarningColor;
                Widgets.Label(new Rect(0f, y, width, h), text);
                GUI.color = Color.white;
                y += h + 2f;
            }
            Widgets.EndScrollView();

            // Parts
            y = reportRect.yMax + L.footerGap;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, row), "MBBS_ImportParts".Translate());
            y += row;
            foreach (var part in Parts)
            {
                bool on = (chosen & part) != 0;
                bool set = (available & part) != 0;
                string label = ("MBBS_ImportPart_" + part).Translate();
                if (!set) label += " " + "MBBS_ImportPart_NotSet".Translate();
                var r = new Rect(inRect.x + L.radioIndent, y, inRect.width - L.radioIndent, row);
                if (!set) GUI.color = new Color(1f, 1f, 1f, L.dimmedAlpha);
                Widgets.CheckboxLabeled(r, label, ref on);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(r, ("MBBS_ImportPart_" + part + "_Desc").Translate());
                chosen = on ? chosen | part : chosen & ~part;
                y += row;
            }

            // Footer
            float bh = L.footerButtonHeight;
            float half = (inRect.width - L.footerGap) / 2f;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - bh, half, bh), "CancelButton".Translate()))
                Close();
            bool any = chosen != ArrangementParts.None;
            if (!any) GUI.color = Color.gray;
            bool clicked = Widgets.ButtonText(new Rect(inRect.x + half + L.footerGap, inRect.yMax - bh, half, bh), "MBBS_ImportConfirm".Translate(), active: any);
            GUI.color = Color.white;
            if (clicked && any)
            {
                apply(ArrangementMerge.Merge(current, shared, chosen));
                Messages.Message("MBBS_Import_Done".Translate(), MessageTypeDefOf.NeutralEvent, false);
                Close();
            }
        }
    }

    /// <summary>Opens a folder in the system's file browser, creating it first.</summary>
    internal static class FolderUtil
    {
        public static void Open(string path)
        {
            Directory.CreateDirectory(path);
            Application.OpenURL("file:///" + path.Replace('\\', '/'));
        }
    }
}
