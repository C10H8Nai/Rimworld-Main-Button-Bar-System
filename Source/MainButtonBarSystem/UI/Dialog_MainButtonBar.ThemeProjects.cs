using System.IO;
using System.Linq;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Appearance tab, theme projects (design spec 2026-11-07): "New theme…" after the theme list, and on the selected
    /// project's card Reload, Open folder and Delete. While the tab shows a selected project, its files are checked at
    /// the tuning def's interval and read again when they change.
    /// </summary>
    public partial class Dialog_MainButtonBar
    {
        /// <summary>Time of the next check of the selected project's files (realtime seconds).</summary>
        private static float nextProjectCheck;

        /// <summary>Whether the selected project may be deleted, as of the last check (not looked up every frame).</summary>
        private ThemeProject checkedProject;
        private ThemeProjectRules.DeleteState checkedDeleteState;

        private void DrawNewThemeButton(Listing_Standard listing)
        {
            var row = listing.GetRect(L.textRowHeight);
            string label = "MBBS_ThemeProject_New".Translate();
            var rect = new Rect(row.x, row.y, UIUtil.FitButton(L.moveButtonWidth, label), row.height);
            TooltipHandler.TipRegion(rect, "MBBS_ThemeProject_NewTip".Translate());
            if (Widgets.ButtonText(rect, label))
                Find.WindowStack.Add(new Dialog_ArrangementInfo(ExportTarget.ThemeProject, CreateThemeProject));
        }

        private void CreateThemeProject(string name, string author, string description, string packageId)
        {
            string folder = ThemeProjectWriter.Create(name, author, description, packageId, out string error);
            if (folder == null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }
            ThemeProjects.Rescan();
            var project = ThemeProjects.All.FirstOrDefault(p => string.Equals(Path.GetFullPath(p.Folder), Path.GetFullPath(folder), System.StringComparison.OrdinalIgnoreCase));
            var theme = project?.Themes.FirstOrDefault();
            if (theme != null) working.appearance = theme.defName;
            Messages.Message("MBBS_ThemeProject_Created".TranslateKeepCase(folder), MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>Reload, Open folder and Delete under the selected theme's card, when it is a theme project.</summary>
        private void DrawProjectButtons(Listing_Standard listing, BarAppearanceDef theme)
        {
            var project = ThemeProjects.ProjectOf(theme);
            if (project == null) return;
            if (checkedProject != project) RefreshDeleteState(project);

            listing.Gap(L.footerGap);
            var row = listing.GetRect(L.textRowHeight);
            float x = row.x;
            Rect Place(string label)
            {
                float width = UIUtil.FitButton(L.smallButtonWidth, label);
                if (x > row.x && x + width > row.xMax)
                {
                    row = listing.GetRect(L.textRowHeight);
                    x = row.x;
                }
                var placed = new Rect(x, row.y, Mathf.Min(width, row.width), row.height);
                x = placed.xMax + L.footerGap;
                return placed;
            }

            string reload = "MBBS_ThemeProject_Reload".Translate();
            var reloadRect = Place(reload);
            TooltipHandler.TipRegion(reloadRect, (project.LoadedByGame ? "MBBS_ThemeProject_ReloadActiveTip" : "MBBS_ThemeProject_ReloadTip").Translate());
            if (Widgets.ButtonText(reloadRect, reload)) ReloadProject(project, true);

            string open = "MBBS_OpenFolder".Translate();
            var openRect = Place(open);
            TooltipHandler.TipRegion(openRect, project.Folder);
            if (Widgets.ButtonText(openRect, open)) FolderUtil.Open(project.Folder);

            string delete = "MBBS_ThemeProject_Delete".Translate();
            var deleteRect = Place(delete);
            bool canDelete = checkedDeleteState == ThemeProjectRules.DeleteState.Allowed;
            TooltipHandler.TipRegion(deleteRect, DeleteTip(checkedDeleteState));
            if (DimmableButton(deleteRect, delete, canDelete)) ConfirmDelete(project);
        }

        private static string DeleteTip(ThemeProjectRules.DeleteState state) => state switch
        {
            ThemeProjectRules.DeleteState.Running => "MBBS_ThemeProject_DeleteRunning".Translate(),
            ThemeProjectRules.DeleteState.ActiveInModsConfig => "MBBS_ThemeProject_DeleteActive".Translate(),
            _ => "MBBS_ThemeProject_DeleteTip".TranslateKeepCase(Path.Combine(ModPaths.DataFolder, "DeletedThemes")),
        };

        private void RefreshDeleteState(ThemeProject project)
        {
            checkedProject = project;
            checkedDeleteState = ThemeProjects.DeleteState(project);
        }

        /// <summary>
        /// While the Appearance tab is drawn: at the tuning def's interval (once per frame at most, at layout), checks the
        /// selected project's files and reads them again when they changed. Nothing is read in between.
        /// </summary>
        private void CheckSelectedProject()
        {
            if (Event.current.type != EventType.Layout) return;
            float now = Time.realtimeSinceStartup;
            if (now < nextProjectCheck) return;
            nextProjectCheck = now + T.behaviour.themeProjectPollSeconds;
            var project = ThemeProjects.ProjectOf(working.AppearanceDef);
            if (project == null) return;
            RefreshDeleteState(project);
            if (ThemeProjects.Changed(project)) ReloadProject(project, false);
        }

        private void ReloadProject(ThemeProject project, bool byButton)
        {
            var result = ThemeProjects.Reload(project);
            KeepAppearanceValid();
            if (result.retry && !byButton) return;   // being written: the next check tries again, quietly
            string message = result.retry ? "MBBS_ThemeProject_ReloadRetry".TranslateKeepCase(project.Name)
                : result.active ? "MBBS_ThemeProject_ReloadedActive".TranslateKeepCase(project.Name, result.files)
                : "MBBS_ThemeProject_Reloaded".TranslateKeepCase(project.Name, result.files);
            Messages.Message(message, result.retry ? MessageTypeDefOf.CautionInput : MessageTypeDefOf.NeutralEvent, false);
        }

        /// <summary>The working theme was removed (deleted project, defName changed in its XML): back to the game's look.</summary>
        private void KeepAppearanceValid()
        {
            var def = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(working.appearance ?? BarConfig.VanillaAppearance);
            if (def == null || ThemeProjects.IsHidden(def)) working.appearance = BarConfig.VanillaAppearance;
        }

        private void ConfirmDelete(ThemeProject project)
        {
            string text = "MBBS_ThemeProject_DeleteConfirm".TranslateKeepCase(project.Name, project.Folder, Path.Combine(ModPaths.DataFolder, "DeletedThemes"));
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () => DeleteProject(project), destructive: true));
        }

        private void DeleteProject(ThemeProject project)
        {
            string name = project.Name, folder = project.Folder;
            string movedTo = ThemeProjects.Delete(project, out string error, out string leftover);
            checkedProject = null;
            if (movedTo == null)
            {
                Messages.Message("MBBS_ThemeProject_DeleteFailed".TranslateKeepCase(folder, error), MessageTypeDefOf.RejectInput, false);
                return;
            }
            KeepAppearanceValid();
            if (leftover != null)
                Messages.Message("MBBS_ThemeProject_DeletedLeftover".TranslateKeepCase(name, movedTo, leftover), MessageTypeDefOf.CautionInput, false);
            else
                Messages.Message("MBBS_ThemeProject_Deleted".TranslateKeepCase(name, movedTo), MessageTypeDefOf.NeutralEvent, false);
        }
    }
}
