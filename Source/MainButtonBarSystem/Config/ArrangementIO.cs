using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MainButtonBarSystem.Core;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Export and import of whole arrangements (spec 3.9): files, clipboard text, the def XML of an arrangement mod,
    /// and a complete arrangement mod folder. All share one format: the template file's XML (spec 4.9, 4.15), with an
    /// optional <c>meta</c> node beside <c>config</c> (name, author, the exporting game's buttons and their mods).
    /// Older versions read only <c>config</c>, so files stay compatible both ways.
    /// </summary>
    public static class ArrangementIO
    {
        public const string RootNode = "MainButtonBarSystem";
        public const string SelfPackageId = "NAI.MainButtonBarSystem";

        // ------------------------------------------------------------------ writing

        /// <summary>Readable XML of an arrangement, identical to an exported file.</summary>
        public static string ToText(BarConfig config, ArrangementMeta meta = null)
        {
            var copy = Prepared(config);
            Directory.CreateDirectory(ModPaths.DataFolder);
            WriteFile(ModPaths.TempFile, copy, meta);
            try
            {
                return File.ReadAllText(ModPaths.TempFile, Encoding.UTF8);
            }
            finally
            {
                TryDelete(ModPaths.TempFile);
            }
        }

        /// <summary>Saves to the exports folder as "&lt;name&gt;.xml" (numbered if taken); returns the path.</summary>
        public static string SaveToFile(BarConfig config, ArrangementMeta meta)
        {
            Directory.CreateDirectory(ModPaths.ExportsFolder);
            string name = SharingNames.FileName(meta?.name, "Arrangement_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            var path = SharingNames.Unique(Path.Combine(ModPaths.ExportsFolder, name), File.Exists, ".xml");
            File.WriteAllText(path, ToText(config, meta), new UTF8Encoding(false));
            return path;
        }

        /// <summary>A complete Defs file for an arrangement mod, with the same config content as an export.</summary>
        public static string ToModXml(BarConfig config, ArrangementMeta meta, string defName)
        {
            var exported = new XmlDocument();
            exported.LoadXml(ToText(config, meta));

            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            var defs = doc.CreateElement("Defs");
            doc.AppendChild(defs);
            var def = doc.CreateElement("MainButtonBarSystem.MainButtonArrangementDef");
            defs.AppendChild(def);
            Append(doc, def, "defName", defName);
            Append(doc, def, "label", meta?.name.NullOrEmpty() == false ? meta.name : "my arrangement");
            if (!meta?.description.NullOrEmpty() == true) Append(doc, def, "description", meta.description);
            Append(doc, def, "author", meta?.author ?? "");
            var metaNode = exported.DocumentElement["meta"];
            if (metaNode != null) def.AppendChild(doc.ImportNode(metaNode, true));
            def.AppendChild(doc.ImportNode(exported.DocumentElement["config"], true));
            return Pretty(doc);
        }

        /// <summary>
        /// Writes a ready-to-upload arrangement mod into the game's Mods folder (or the exports folder when that is
        /// not writable): About.xml depending on this mod, the arrangement def, and the player's own PNG icons it
        /// uses, copied into the mod's Textures folder so subscribers get them too. Returns the mod folder.
        /// </summary>
        public static string ExportModFolder(BarConfig config, ArrangementMeta meta, string packageId)
        {
            string folderName = SharingNames.FileName(meta.name, "Arrangement");
            string root;
            try
            {
                root = SharingNames.Unique(Path.Combine(GenFilePaths.ModsFolderPath, folderName), Directory.Exists);
                Directory.CreateDirectory(root);
            }
            catch (Exception)
            {
                root = SharingNames.Unique(Path.Combine(ModPaths.ModExportsFolder, folderName), Directory.Exists);
                Directory.CreateDirectory(root);
            }

            var copy = Prepared(config);
            string id = SharingNames.Identifier(packageId, "Arrangement");
            var bundled = BundleIcons(copy, Path.Combine(root, "Textures", id, "Icons"), id + "/Icons/");

            Directory.CreateDirectory(Path.Combine(root, "About"));
            Directory.CreateDirectory(Path.Combine(root, "Defs"));
            File.WriteAllText(Path.Combine(root, "About", "About.xml"), AboutXml(meta, packageId), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, "Defs", "MainButtonArrangement.xml"), ToModXml(copy, meta, id), new UTF8Encoding(false));
            if (bundled.Count > 0)
                File.WriteAllText(Path.Combine(root, "Defs", "MainButtonIcons.xml"), IconSetXml(id, meta.name, bundled), new UTF8Encoding(false));
            return root;
        }

        /// <summary>Copies the "file:" icons the arrangement uses and points the overrides at the copies.</summary>
        private static List<string> BundleIcons(BarConfig config, string folder, string texturePrefix)
        {
            var paths = new Dictionary<string, string>();
            foreach (var o in config.overrides)
            {
                if (!CustomIcons.IsFile(o.icon)) continue;
                if (!paths.TryGetValue(o.icon, out var texPath))
                {
                    var file = CustomIcons.FileName(o.icon);
                    var source = Path.Combine(ModPaths.IconsFolder, file ?? "");
                    if (file.NullOrEmpty() || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !File.Exists(source)) continue;
                    Directory.CreateDirectory(folder);
                    string name = SharingNames.Identifier(Path.GetFileNameWithoutExtension(file), "Icon");
                    string target = SharingNames.Unique(Path.Combine(folder, name), File.Exists, ".png");
                    File.Copy(source, target);
                    texPath = texturePrefix + Path.GetFileNameWithoutExtension(target);
                    paths[o.icon] = texPath;
                }
                o.icon = texPath;
            }
            return paths.Values.ToList();
        }

        private static string AboutXml(ArrangementMeta meta, string packageId)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            var root = doc.CreateElement("ModMetaData");
            doc.AppendChild(root);
            Append(doc, root, "name", meta.name);
            Append(doc, root, "author", meta.author ?? "");
            Append(doc, root, "packageId", packageId);
            var versions = doc.CreateElement("supportedVersions");
            root.AppendChild(versions);
            Append(doc, versions, "li", RimWorld.VersionControl.CurrentMajor + "." + RimWorld.VersionControl.CurrentMinor);
            var deps = doc.CreateElement("modDependencies");
            root.AppendChild(deps);
            var dep = doc.CreateElement("li");
            deps.AppendChild(dep);
            Append(doc, dep, "packageId", SelfPackageId);
            Append(doc, dep, "displayName", "Main Button Bar System");
            var after = doc.CreateElement("loadAfter");
            root.AppendChild(after);
            Append(doc, after, "li", SelfPackageId);
            string usage = "MBBS_ModFolder_Usage".Translate(meta.name);
            Append(doc, root, "description", meta.description.NullOrEmpty() ? usage : meta.description + "\n\n" + usage);
            return Pretty(doc);
        }

        private static string IconSetXml(string defName, string label, List<string> icons)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            var defs = doc.CreateElement("Defs");
            doc.AppendChild(defs);
            var def = doc.CreateElement("MainButtonBarSystem.MainButtonIconSetDef");
            defs.AppendChild(def);
            Append(doc, def, "defName", defName + "_Icons");
            Append(doc, def, "label", label);
            var list = doc.CreateElement("icons");
            def.AppendChild(list);
            foreach (var icon in icons) Append(doc, list, "li", icon);
            return Pretty(doc);
        }

        // ------------------------------------------------------------------ reading

        /// <summary>
        /// Parses exported text, a file or the def XML of an arrangement mod. Returns null and an error message when it
        /// is not an arrangement.
        /// </summary>
        public static SharedArrangement FromText(string text, string source, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "MBBS_Import_Empty".Translate();
                return null;
            }

            XmlDocument doc;
            try
            {
                doc = new XmlDocument();
                doc.LoadXml(text.Trim());
            }
            catch (Exception)
            {
                error = "MBBS_Import_NotXml".Translate();
                return null;
            }

            // Accept the exported file, or a def XML made with "Copy as mod XML".
            XmlElement configNode, metaNode;
            if (doc.DocumentElement?.Name == RootNode)
            {
                configNode = doc.DocumentElement["config"];
                metaNode = doc.DocumentElement["meta"];
            }
            else
            {
                configNode = doc.SelectSingleNode("//config") as XmlElement;
                metaNode = configNode?.ParentNode?["meta"];
            }
            if (configNode == null)
            {
                error = "MBBS_Import_NoConfig".Translate();
                return null;
            }

            // Text from the clipboard is untrusted: never let it pick classes to instantiate.
            if (configNode.SelectSingleNode("descendant-or-self::*[@Class]") != null ||
                metaNode?.SelectSingleNode("descendant-or-self::*[@Class]") != null)
            {
                error = "MBBS_Import_Unsafe".Translate();
                return null;
            }

            var wrapped = new XmlDocument();
            var root = wrapped.CreateElement(RootNode);
            wrapped.AppendChild(wrapped.CreateXmlDeclaration("1.0", "utf-8", null));
            wrapped.AppendChild(root);
            if (metaNode != null) root.AppendChild(wrapped.ImportNode(metaNode, true));
            root.AppendChild(wrapped.ImportNode(configNode, true));

            Directory.CreateDirectory(ModPaths.DataFolder);
            try
            {
                wrapped.Save(ModPaths.TempFile);
                ReadFile(ModPaths.TempFile, out var config, out var meta);
                if (config == null)
                {
                    error = "MBBS_Import_NoConfig".Translate();
                    return null;
                }
                if (config.formatVersion > BarConfig.CurrentFormatVersion)
                {
                    error = "MBBS_Import_TooNew".Translate(config.formatVersion, BarConfig.CurrentFormatVersion);
                    return null;
                }
                return new SharedArrangement { config = config, meta = meta, source = source };
            }
            catch (Exception e)
            {
                error = "MBBS_Import_Failed".Translate(e.Message);
                return null;
            }
            finally
            {
                TryDelete(ModPaths.TempFile);
            }
        }

        public static SharedArrangement FromFile(string path, out string error) =>
            FromText(File.ReadAllText(path, Encoding.UTF8), Path.GetFileName(path), out error);

        public static SharedArrangement FromDef(MainButtonArrangementDef def) => new SharedArrangement
        {
            config = def.config,
            meta = def.meta ?? new ArrangementMeta { name = def.label, author = def.author, description = def.description },
            source = def.modContentPack?.Name ?? def.defName,
        };

        /// <summary>Arrangement files in the exports folder, newest first.</summary>
        public static IEnumerable<FileInfo> ExportedFiles()
        {
            var dir = new DirectoryInfo(ModPaths.ExportsFolder);
            if (!dir.Exists) return Enumerable.Empty<FileInfo>();
            return dir.GetFiles("*.xml").OrderByDescending(f => f.LastWriteTimeUtc);
        }

        /// <summary>Name and author stored in a file, without loading it; nulls when there are none.</summary>
        public static (string name, string author) PeekTitle(string path)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(path);
                var meta = doc.DocumentElement?["meta"];
                return (meta?["name"]?.InnerText, meta?["author"]?.InnerText);
            }
            catch (Exception)
            {
                return (null, null);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static BarConfig Prepared(BarConfig config)
        {
            var copy = config.Clone();
            copy.Prune();
            copy.formatVersion = BarConfig.CurrentFormatVersion;
            return copy;
        }

        private static void WriteFile(string path, BarConfig config, ArrangementMeta meta)
        {
            Scribe.saver.InitSaving(path, RootNode);
            try
            {
                if (meta != null) Scribe_Deep.Look(ref meta, "meta");
                Scribe_Deep.Look(ref config, "config");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
        }

        private static void ReadFile(string path, out BarConfig config, out ArrangementMeta meta)
        {
            config = null;
            meta = null;
            Scribe.loader.InitLoading(path);
            try
            {
                Scribe_Deep.Look(ref meta, "meta");
                Scribe_Deep.Look(ref config, "config");
            }
            finally
            {
                Scribe.loader.FinalizeLoading();
            }
        }

        private static string Pretty(XmlDocument doc)
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", OmitXmlDeclaration = false, Encoding = new UTF8Encoding(false) };
            using (var writer = XmlWriter.Create(sb, settings))
                doc.Save(writer);
            // StringBuilder output declares utf-16; the files are written as UTF-8.
            return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
        }

        private static void Append(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var e = doc.CreateElement(name);
            e.InnerText = value ?? "";
            parent.AppendChild(e);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }
}
