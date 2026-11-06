using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MainButtonBarSystem.Core;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// A theme mod folder made by "New theme" (design spec 2026-11-07): a folder in the game's Mods folder with
    /// <see cref="ThemeProjectMarker.FileName"/> at its root.
    /// </summary>
    public sealed class ThemeProject
    {
        public string Folder { get; internal set; }
        public ThemeProjectMarker Marker { get; internal set; }

        /// <summary>The game loaded it as a mod this session (its packageId is in the running mods): this mod leaves its defs alone.</summary>
        public bool LoadedByGame { get; internal set; }

        /// <summary>Its themes, as in the def database (read from its Defs folder, or loaded by the game).</summary>
        public List<BarAppearanceDef> Themes { get; } = new List<BarAppearanceDef>();

        /// <summary>Last-write times of its files when they were last read; null to read them on the next check.</summary>
        internal FileSnapshot Snapshot;

        /// <summary>Reads in a row that kept a file's previous version (being written, or broken); see <see cref="ThemeProjects.Reload"/>.</summary>
        internal int retries;

        public string FolderName => Path.GetFileName(Folder);

        /// <summary>For messages: its first theme's label, else the folder name.</summary>
        public string Name => Themes.Count > 0 && !Themes[0].label.NullOrEmpty() ? Themes[0].LabelCap.ToString() : FolderName;
    }

    /// <summary>Result of reading a theme project again.</summary>
    public struct ThemeProjectReload
    {
        /// <summary>Files read (PNG and XML).</summary>
        public int files;

        /// <summary>A file could not be read right now; the previous version is kept and the next check tries again.</summary>
        public bool retry;

        /// <summary>The project is an active mod: only its PNG files were read.</summary>
        public bool active;
    }

    /// <summary>
    /// Finds, loads, reloads and deletes theme projects. Projects that are not active mods are read by this mod (their
    /// themes go into the def database while the game runs); active ones are the game's, and only their PNGs are read
    /// again. Only folders with the marker file are ever changed or moved.
    /// </summary>
    public static class ThemeProjects
    {
        private static readonly List<ThemeProject> projects = new List<ThemeProject>();
        private static readonly Dictionary<BarAppearanceDef, ThemeProject> byDef = new Dictionary<BarAppearanceDef, ThemeProject>();

        public static IReadOnlyList<ThemeProject> All => projects;

        public static ThemeProject ProjectOf(BarAppearanceDef def) => def != null && byDef.TryGetValue(def, out var p) ? p : null;

        /// <summary>A theme that was taken out of the lists (a deleted or changed project) but stays in the def database.</summary>
        public static bool IsHidden(BarAppearanceDef def) => ThemeProjectLoader.IsHidden(def);

        /// <summary>packageIds of every known project (the game's mod list learns about new folders only after a restart).</summary>
        public static IEnumerable<string> KnownPackageIds => projects.Select(p => p.Marker.packageId);

        /// <summary>After the defs are loaded and before any save is read: theme projects a save may have chosen are in the database.</summary>
        public static void ScanAtStartup()
        {
            try
            {
                Scan();
            }
            catch (Exception e)
            {
                Log.Warning(MainButtonBarSystemMod.LogPrefix + "Could not look for theme projects in " + GenFilePaths.ModsFolderPath + ": " + e.Message);
            }
        }

        /// <summary>Looks for project folders not known yet (after "New theme").</summary>
        public static void Rescan() => ScanAtStartup();

        /// <summary>
        /// Play data was reloaded (a language change): every def is a new object and the database was rebuilt without
        /// the themes this mod added. Forgets every project (taking any of its themes still in the database out) so the
        /// next scan reads them all again.
        /// </summary>
        public static void Reset()
        {
            string problem = beforeClearProblem;
            if (problem != null)
            {
                beforeClearProblem = null;
                Log.Warning(MainButtonBarSystemMod.LogPrefix + "Could not take theme projects out of the def database before play data was reloaded: " + problem);
            }
            foreach (var project in projects)
            {
                foreach (var def in project.Themes)
                {
                    if (!def.IsFromInactiveProject) continue;
                    if (DefDatabase<BarAppearanceDef>.GetNamedSilentFail(def.defName) == def) ThemeProjectLoader.Remove(def);
                    else def.ReleaseTextures();
                }
            }
            projects.Clear();
            byDef.Clear();
            ThemeProjectLoader.ForgetHidden();
        }

        /// <summary>The game is about to clear and reload play data: see <see cref="ThemeProjectLoader.TakeOutBeforeReload"/>.</summary>
        public static void BeforePlayDataClear()
        {
            // On the loading thread, inside the game's ClearAllPlayData: never throw (that would stop the reload) and
            // never log here; Reset reports a problem on the main thread.
            try
            {
                foreach (var project in projects.ToList())
                    foreach (var def in project.Themes.ToList())
                        if (def.IsFromInactiveProject) ThemeProjectLoader.TakeOutBeforeReload(def);
                ThemeProjectLoader.TakeOutHiddenBeforeReload();
            }
            catch (Exception e)
            {
                beforeClearProblem = e.Message;
            }
        }

        private static volatile string beforeClearProblem;

        private static void Scan()
        {
            string mods = GenFilePaths.ModsFolderPath;
            if (!Directory.Exists(mods)) return;
            // A project whose files could not be read before (XML being edited, an empty Defs folder) has no themes and
            // so no card to reload it from: try it again on every scan once its files changed.
            foreach (var project in projects.ToList())
                if (!project.LoadedByGame && project.Themes.Count == 0 && Directory.Exists(project.Folder) && Changed(project)) Reload(project);

            foreach (var dir in Directory.GetDirectories(mods))
            {
                if (projects.Any(p => SamePath(p.Folder, dir))) continue;
                string markerFile = Path.Combine(dir, ThemeProjectMarker.FileName);
                if (!File.Exists(markerFile)) continue;
                var project = Load(dir, markerFile);
                if (project != null) projects.Add(project);
            }
        }

        private static ThemeProject Load(string dir, string markerFile)
        {
            ThemeProjectMarker marker;
            try
            {
                marker = ThemeProjectMarker.Parse(File.ReadAllText(markerFile));
            }
            catch (Exception)
            {
                marker = null;
            }
            string folderName = Path.GetFileName(dir);
            if (marker == null)
            {
                ThemeProjectLoader.Warn(folderName, ThemeProjectMarker.FileName + " cannot be read; the folder is left alone.");
                return null;
            }
            if (marker.IsNewerThanSupported)
            {
                ThemeProjectLoader.Warn(folderName, "made by a newer version of Main Button Bar System; the folder is left alone.");
                return null;
            }

            var project = new ThemeProject { Folder = dir, Marker = marker, LoadedByGame = IsRunning(marker.packageId) };
            if (project.LoadedByGame)
            {
                foreach (var def in DefDatabase<BarAppearanceDef>.AllDefs)
                    if (def.modContentPack != null && ThemeProjectRules.SamePackageId(def.modContentPack.PackageId, marker.packageId))
                        Attach(project, def);
                // The game just read these files: nothing to reload until they change.
                project.Snapshot = TakeSnapshot(dir);
                return project;
            }

            var snapshot = TakeSnapshot(dir);
            var parsed = ThemeProjectLoader.ParseFolder(dir, folderName);
            if (parsed == null) return project;   // read again when it is reloaded
            foreach (var def in parsed)
            {
                var added = ThemeProjectLoader.TryAdd(def, folderName);
                if (added != null) Attach(project, added);
            }
            project.Snapshot = snapshot;
            return project;
        }

        private static void Attach(ThemeProject project, BarAppearanceDef def)
        {
            if (!project.Themes.Contains(def)) project.Themes.Add(def);
            byDef[def] = project;
        }

        private static void Detach(ThemeProject project, BarAppearanceDef def)
        {
            project.Themes.Remove(def);
            byDef.Remove(def);
        }

        public static bool IsRunning(string packageId) =>
            LoadedModManager.RunningMods.Any(m => ThemeProjectRules.SamePackageId(m.PackageId, packageId) || ThemeProjectRules.SamePackageId(m.PackageIdPlayerFacing, packageId));

        /// <summary>Ticked in the mod list (loaded by the game after a restart).</summary>
        public static bool IsActiveInModsConfig(string packageId)
        {
            try
            {
                return ModsConfig.IsActive(packageId) || ModsConfig.IsActive(packageId + "_steam");
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static ThemeProjectRules.DeleteState DeleteState(ThemeProject project) =>
            ThemeProjectRules.CanDelete(File.Exists(Path.Combine(project.Folder, ThemeProjectMarker.FileName)),
                project.LoadedByGame || IsRunning(project.Marker.packageId), IsActiveInModsConfig(project.Marker.packageId));

        /// <summary>
        /// Last-write times of the project's PNG and XML files; null when the folder cannot be listed right now. Only
        /// called at the check interval, never every frame.
        /// </summary>
        internal static FileSnapshot TakeSnapshot(string dir)
        {
            try
            {
                var files = new List<(string, long)>();
                foreach (var (sub, pattern) in new[] { ("Textures", "*.png"), ("Defs", "*.xml") })
                {
                    string folder = Path.Combine(dir, sub);
                    if (!Directory.Exists(folder)) continue;
                    foreach (var file in Directory.GetFiles(folder, pattern, SearchOption.AllDirectories))
                        files.Add((file, File.GetLastWriteTimeUtc(file).Ticks));
                }
                return FileSnapshot.Of(files);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>True when the project's files changed since they were last read (or it still needs a retry).</summary>
        internal static bool Changed(ThemeProject project)
        {
            var now = TakeSnapshot(project.Folder);
            return now != null && !now.SameAs(project.Snapshot);
        }

        /// <summary>
        /// Reads a project again (design spec §5.3). Not active: its Defs XML and every PNG; every field of a theme takes
        /// effect (the same def object is updated in place and its caches cleared), new themes are added and themes no
        /// longer in the XML are removed. Active: only its PNGs (Defs need a restart).
        /// </summary>
        public static ThemeProjectReload Reload(ThemeProject project)
        {
            var result = new ThemeProjectReload { active = project.LoadedByGame };
            var snapshot = TakeSnapshot(project.Folder);
            if (project.LoadedByGame)
            {
                foreach (var def in project.Themes) result.files += def.ReloadTextures();
                project.Snapshot = snapshot;
                FinishReload(project);
                return result;
            }

            var parsed = ThemeProjectLoader.ParseFolder(project.Folder, project.FolderName);
            if (parsed == null)
            {
                result.retry = true;
                CountRetry(project, snapshot);
                return result;
            }
            var keep = new List<BarAppearanceDef>();
            foreach (var fresh in parsed)
            {
                var existing = project.Themes.FirstOrDefault(d => d.defName == fresh.defName);
                BarAppearanceDef def;
                if (existing != null)
                {
                    existing.CopyFrom(fresh);
                    existing.ClearCaches();
                    def = existing;
                }
                else
                {
                    def = ThemeProjectLoader.TryAdd(fresh, project.FolderName);
                    if (def == null) continue;
                }
                keep.Add(def);
                result.files += def.ReloadProjectTextures(out bool again);
                result.retry |= again;
            }
            foreach (var gone in project.Themes.Where(d => !keep.Contains(d)).ToList())
            {
                ThemeProjectLoader.Remove(gone);
                Detach(project, gone);
            }
            foreach (var def in keep) Attach(project, def);
            result.files += snapshot?.CountWithExtension(".xml") ?? 0;
            if (result.retry)
            {
                CountRetry(project, snapshot);
            }
            else
            {
                project.retries = 0;
                project.Snapshot = snapshot;
            }
            FinishReload(project);
            return result;
        }

        /// <summary>
        /// A file kept its previous version. While it is being written the next check reads it again; a file that still
        /// cannot be read after the tuning def's number of tries is broken, not busy: it is reported once and left
        /// alone until the files change again (else the whole project would be read again every check).
        /// </summary>
        private static void CountRetry(ThemeProject project, FileSnapshot snapshot)
        {
            project.retries++;
            if (project.retries < MainButtonBarTuningDef.Current.behaviour.themeProjectRetryLimit)
            {
                project.Snapshot = null;
                return;
            }
            project.retries = 0;
            project.Snapshot = snapshot;
            ThemeProjectLoader.Warn(project.FolderName, "a file still cannot be read (a PNG that is not a valid image, or an XML with an error); the last good version is kept until the files change.");
        }

        private static void FinishReload(ThemeProject project)
        {
            foreach (var def in project.Themes) ThemeValidator.Validate(def);
            CustomThemes.ResetCache();
        }

        /// <summary>
        /// Moves a project that is not an active mod to <c>DeletedThemes</c> in this mod's data folder (design spec §5.4)
        /// and takes its themes out of the lists. Nothing changes when the folder cannot be moved. Returns where it went.
        /// </summary>
        public static string Delete(ThemeProject project, out string error) => Delete(project, out error, out _);

        /// <summary>
        /// <see cref="Delete(ThemeProject, out string)"/>; <paramref name="leftover"/> is a part of the original folder
        /// that could not be removed after a complete copy across drives (the project still counts as deleted).
        /// </summary>
        public static string Delete(ThemeProject project, out string error, out string leftover)
        {
            error = null;
            leftover = null;
            var state = DeleteState(project);
            if (state != ThemeProjectRules.DeleteState.Allowed)
            {
                error = state.ToString();
                return null;
            }
            string parent = Path.Combine(ModPaths.DataFolder, "DeletedThemes");
            string target;
            try
            {
                Directory.CreateDirectory(parent);
                target = SharingNames.Unique(Path.Combine(parent, ThemeProjectRules.DeletedFolderName(project.FolderName, DateTime.Now)), Directory.Exists);
                MoveFolder(project.Folder, target, out leftover);
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }

            foreach (var def in project.Themes.ToList())
            {
                ThemeProjectLoader.Remove(def);
                Detach(project, def);
            }
            projects.Remove(project);
            CustomThemes.ResetCache();
            return target;
        }

        /// <summary>
        /// Moves a folder; across drives (the Mods folder and the save folder often are) it copies, then deletes the
        /// original. The original is first renamed in place, which fails while any file in it is open, so a locked
        /// folder is never left half moved: on a failed copy the copy is removed, the name restored and the error passed
        /// on. Once the copy is complete the move counts as done; whatever of the original then cannot be deleted
        /// (a read-only file, an antivirus scan) is returned in <paramref name="leftover"/> for the player to remove.
        /// </summary>
        private static void MoveFolder(string source, string target, out string leftover)
        {
            leftover = null;
            if (string.Equals(Path.GetPathRoot(Path.GetFullPath(source)), Path.GetPathRoot(Path.GetFullPath(target)), StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(source, target);
                return;
            }
            string parked = SharingNames.Unique(source + ".mbbs-deleting", Directory.Exists);
            Directory.Move(source, parked);   // throws while a file inside is open: nothing changed yet
            try
            {
                CopyFolder(parked, target);
            }
            catch (Exception copyError)
            {
                TryDeleteFolder(target);
                try
                {
                    Directory.Move(parked, source);
                }
                catch (Exception)
                {
                    throw new IOException(copyError.Message + " (" + parked + ")", copyError);
                }
                throw;
            }
            if (!TryDeleteFolder(parked)) leftover = parked;
        }

        private static void CopyFolder(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            foreach (var dir in Directory.GetDirectories(source))
                CopyFolder(dir, Path.Combine(target, Path.GetFileName(dir)));
        }

        /// <summary>Deletes a folder, read-only files included; false when something in it could not be deleted.</summary>
        private static bool TryDeleteFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return true;
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(path, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool SamePath(string a, string b) =>
            string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
