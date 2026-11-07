using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using MainButtonBarSystem.Core;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// "New theme" (design spec 2026-11-07 §5.1): writes a complete blank theme mod into the game's Mods folder from
    /// this mod's <c>ThemeKit</c> folder: About (depending on this mod, with links), the def, the blank textures, the
    /// one-page guides (one per language, <see cref="ThemeProjectNames.GuideFiles"/>) and the marker file that makes
    /// it a theme project.
    /// </summary>
    internal static class ThemeProjectWriter
    {
        /// <summary>The blank theme files shipped with this mod (not loaded by the game).</summary>
        internal static string KitFolder => Path.Combine(MainButtonBarSystemMod.Instance.Content.RootDir, "ThemeKit");

        /// <summary>
        /// Writes the project; returns its folder, or null with <paramref name="error"/> saying why and where. A failed
        /// attempt leaves nothing behind: the new folder is removed again.
        /// </summary>
        internal static string Create(string name, string author, string description, string packageId, out string error)
        {
            error = null;
            string kit = KitFolder;
            if (!Directory.Exists(Path.Combine(kit, "Textures")) || !File.Exists(Path.Combine(kit, "Defs", "Theme.xml.template")))
            {
                error = "MBBS_ThemeProject_NoKit".TranslateKeepCase(kit);
                return null;
            }

            string id = ThemeProjectNames.IdOf(packageId);
            string defName = ThemeProjectNames.DefNameOf(id);
            string mods = GenFilePaths.ModsFolderPath;
            string root = null;
            try
            {
                root = SharingNames.Unique(Path.Combine(mods, SharingNames.FileName(name, "Theme")), Directory.Exists);
                Directory.CreateDirectory(root);

                string textures = Path.Combine(root, "Textures", id);
                Directory.CreateDirectory(textures);
                foreach (var file in Directory.GetFiles(Path.Combine(kit, "Textures"), "*.png"))
                    File.Copy(file, Path.Combine(textures, Path.GetFileName(file)));

                string about = Path.Combine(root, "About");
                Directory.CreateDirectory(about);
                CopyIfExists(Path.Combine(kit, "About", "Preview.png"), Path.Combine(about, "Preview.png"));
                File.WriteAllText(Path.Combine(about, "About.xml"),
                    ArrangementIO.AboutXml(name, author, packageId, description, "MBBS_ThemeProject_Usage".Translate()), new UTF8Encoding(false));

                string defs = Path.Combine(root, "Defs");
                Directory.CreateDirectory(defs);
                string template = File.ReadAllText(Path.Combine(kit, "Defs", "Theme.xml.template"), Encoding.UTF8);
                File.WriteAllText(Path.Combine(defs, defName + ".xml"), FillTemplate(template, id, name, author, description), new UTF8Encoding(false));

                foreach (var guide in ThemeProjectNames.GuideFiles)
                    CopyIfExists(Path.Combine(kit, guide), Path.Combine(root, guide));

                // Last: the marker is what makes the folder a theme project.
                var marker = new ThemeProjectMarker
                {
                    packageId = packageId,
                    defName = defName,
                    created = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    modVersion = MainButtonBarSystemMod.Instance.Content.ModMetaData?.ModVersion ?? "",
                };
                File.WriteAllText(Path.Combine(root, ThemeProjectMarker.FileName), marker.ToXml(), new UTF8Encoding(false));
                return root;
            }
            catch (Exception e)
            {
                error = "MBBS_ThemeProject_CreateFailed".TranslateKeepCase(root ?? mods, e.Message);
                if (root != null)
                {
                    try
                    {
                        if (Directory.Exists(root)) Directory.Delete(root, true);
                    }
                    catch (Exception)
                    {
                        // Nothing more to do: the message names the folder.
                    }
                }
                return null;
            }
        }

        private static string FillTemplate(string template, string id, string name, string author, string description)
        {
            // No description: no <description> line (the game reports an empty one as a config error).
            if (description.NullOrEmpty())
                template = string.Join("\n", template.Split('\n').Where(line => !line.Contains("{{DESCRIPTION}}")));
            return template.Replace("{{ID}}", id)
                .Replace("{{LABEL}}", Escape(name))
                .Replace("{{DESCRIPTION}}", Escape(description ?? ""))
                .Replace("{{AUTHOR}}", Escape(author ?? ""))
                .Replace("{{DISPLAY_ORDER}}", MainButtonBarTuningDef.Current.behaviour.themeProjectDisplayOrder.ToString());
        }

        private static string Escape(string text) => SecurityElement.Escape(text) ?? "";

        private static void CopyIfExists(string source, string target)
        {
            if (File.Exists(source)) File.Copy(source, target);
        }
    }
}
