using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MainButtonBarSystem.Core;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// A custom theme saved to a file (design spec 2026-11-05 §3.5, §4.16, §4.17): the "all buttons" layer and every
    /// button's own look, the theme it starts from, and name, author and description like an exported arrangement.
    /// </summary>
    public class SavedCustomTheme : IExposable
    {
        public const int CurrentFormatVersion = 1;

        public int formatVersion = CurrentFormatVersion;
        public ArrangementMeta meta;

        /// <summary>defName of the theme the look starts from (the theme chosen when it was saved).</summary>
        public string baseTheme;

        public CustomThemeData customTheme;

        /// <summary>The folder it was read from; its PNGs are there.</summary>
        [Unsaved] public string folder;

        public string Name => meta?.name.NullOrEmpty() == false ? meta.name : Path.GetFileName(folder);

        public void ExposeData()
        {
            Scribe_Values.Look(ref formatVersion, "formatVersion", 0);
            Scribe_Deep.Look(ref meta, "meta");
            Scribe_Values.Look(ref baseTheme, "baseTheme");
            Scribe_Deep.Look(ref customTheme, "customTheme");
        }
    }

    /// <summary>
    /// Saving custom themes to <c>Themes/&lt;name&gt;/theme.xml</c> with the PNGs they use, listing them, and copying
    /// one into the settings (its PNGs go into the Images folder, renamed when a different file has the name).
    /// </summary>
    public static class CustomThemeFiles
    {
        public const string RootNode = "MainButtonBarSystemTheme";
        public const string FileName = "theme.xml";

        private static List<SavedCustomTheme> list;

        /// <summary>Saved custom themes, by name; read once, again after <see cref="Refresh"/>.</summary>
        public static List<SavedCustomTheme> All => list ??= ReadAll();

        public static void Refresh() => list = null;

        /// <summary>Saves the working settings' custom look and its imported PNGs; returns the folder.</summary>
        public static string Save(BarConfig config, ArrangementMeta meta)
        {
            Directory.CreateDirectory(ModPaths.ThemesFolder);
            string name = SharingNames.FileName(meta?.name, "Theme_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string folder = SharingNames.Unique(Path.Combine(ModPaths.ThemesFolder, name), Directory.Exists);
            Directory.CreateDirectory(folder);

            var data = (config.customTheme ?? new CustomThemeData()).Clone();
            data.Prune();
            // Imported PNGs travel with the theme; their references stay "file:Name.png", now meaning this folder.
            foreach (var reference in data.ImageRefs().Where(ImageRef.IsFile).Distinct())
            {
                string file = ImageRef.Parse(reference).Value;
                if (file == null) continue;
                string source = Path.Combine(ModPaths.ImagesFolder, file);
                if (File.Exists(source)) File.Copy(source, Path.Combine(folder, file), true);
            }

            var saved = new SavedCustomTheme { meta = meta, baseTheme = config.appearance ?? BarConfig.VanillaAppearance, customTheme = data };
            Scribe.saver.InitSaving(Path.Combine(folder, FileName), RootNode);
            try
            {
                saved.ExposeData();
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
            Refresh();
            return folder;
        }

        private static List<SavedCustomTheme> ReadAll()
        {
            var result = new List<SavedCustomTheme>();
            var dir = new DirectoryInfo(ModPaths.ThemesFolder);
            if (!dir.Exists) return result;
            foreach (var sub in dir.GetDirectories())
            {
                var theme = Read(sub.FullName, out string error);
                if (theme != null) result.Add(theme);
                else Log.WarningOnce(MainButtonBarSystemMod.LogPrefix + "Skipped the custom theme in " + sub.FullName + ": " + error, ("MBBS_Theme_" + sub.FullName).GetHashCode());
            }
            return result.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        /// <summary>Reads one saved theme; null and a message (as the import report shows it) when it cannot be used.</summary>
        public static SavedCustomTheme Read(string folder, out string error)
        {
            error = null;
            string path = Path.Combine(folder, FileName);
            if (!File.Exists(path))
            {
                error = "MBBS_Import_NoConfig".Translate();
                return null;
            }
            try
            {
                // A shared theme is untrusted: it never gets to pick classes to instantiate.
                // No DTDs and no external resources: a shared file never gets to fetch or expand anything.
                var doc = new XmlDocument { XmlResolver = null };
                using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    doc.Load(reader);
                if (doc.DocumentElement?.Name != RootNode)
                {
                    error = "MBBS_Import_NoConfig".Translate();
                    return null;
                }
                if (doc.SelectSingleNode("//*[@Class]") != null)
                {
                    error = "MBBS_Import_Unsafe".Translate();
                    return null;
                }

                var theme = new SavedCustomTheme();
                Scribe.loader.InitLoading(path);
                try
                {
                    theme.ExposeData();
                }
                finally
                {
                    Scribe.loader.FinalizeLoading();
                }
                theme.customTheme ??= new CustomThemeData();
                theme.baseTheme ??= BarConfig.VanillaAppearance;
                if (theme.formatVersion > SavedCustomTheme.CurrentFormatVersion)
                {
                    error = "MBBS_Import_TooNew".Translate(theme.formatVersion, SavedCustomTheme.CurrentFormatVersion);
                    return null;
                }
                theme.folder = folder;
                return theme;
            }
            catch (Exception e)
            {
                error = "MBBS_Import_Failed".Translate(e.Message);
                return null;
            }
        }

        /// <summary>
        /// The saved look, ready to copy into the settings (spec 4.17: the save no longer needs the file): its PNGs are
        /// copied into the Images folder, a file of the same name with other content is kept and the copy renamed
        /// ("Name (2).png"), and the references follow. PNGs missing from the theme's folder stay as they are; the
        /// theme's own image is used for them (warned once).
        /// </summary>
        public static CustomThemeData Install(SavedCustomTheme theme)
        {
            var data = theme.customTheme.Clone();
            var renamed = new Dictionary<string, string>();
            foreach (var reference in data.ImageRefs().Where(ImageRef.IsFile).Distinct().ToList())
            {
                string file = ImageRef.Parse(reference).Value;
                if (file == null) continue;
                string source = Path.Combine(theme.folder, file);
                if (!File.Exists(source)) continue;
                Directory.CreateDirectory(ModPaths.ImagesFolder);
                string target = ImageRef.UniqueFileName(file, n => File.Exists(Path.Combine(ModPaths.ImagesFolder, n)) && !SameFile(source, Path.Combine(ModPaths.ImagesFolder, n)));
                string targetPath = Path.Combine(ModPaths.ImagesFolder, target);
                if (!File.Exists(targetPath)) File.Copy(source, targetPath);
                if (target != file) renamed[reference] = ImageRef.ForFile(target);
            }
            if (renamed.Count > 0) data.MapImageRefs(r => renamed.TryGetValue(r, out var to) ? to : r);
            CustomThemes.RefreshImages();
            return data;
        }

        private static bool SameFile(string a, string b)
        {
            try
            {
                var fa = new FileInfo(a);
                var fb = new FileInfo(b);
                return fa.Length == fb.Length && File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b));
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static void OpenFolder()
        {
            Directory.CreateDirectory(ModPaths.ThemesFolder);
            FolderUtil.Open(ModPaths.ThemesFolder);
        }
    }
}
