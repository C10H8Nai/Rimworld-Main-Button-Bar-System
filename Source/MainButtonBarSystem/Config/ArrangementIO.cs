using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Export and import of whole arrangements (spec 3.9): files, clipboard text, and the def XML of an
    /// arrangement mod. All three share one format: the same XML the template file uses (spec 4.9, 4.15).
    /// </summary>
    public static class ArrangementIO
    {
        public const string RootNode = "MainButtonBarSystem";

        /// <summary>Readable XML of an arrangement, identical to an exported file.</summary>
        public static string ToText(BarConfig config)
        {
            var copy = Prepared(config);
            Directory.CreateDirectory(ModPaths.DataFolder);
            BarConfigManager.ConfigFile.Write(ModPaths.TempFile, copy);
            try
            {
                return File.ReadAllText(ModPaths.TempFile, Encoding.UTF8);
            }
            finally
            {
                TryDelete(ModPaths.TempFile);
            }
        }

        /// <summary>Parses exported text. Returns null and an error message when it is not an arrangement.</summary>
        public static BarConfig FromText(string text, out string error)
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
            var configNode = doc.DocumentElement?.Name == RootNode
                ? doc.DocumentElement["config"]
                : doc.SelectSingleNode("//config") as XmlElement;
            if (configNode == null)
            {
                error = "MBBS_Import_NoConfig".Translate();
                return null;
            }

            // Text from the clipboard is untrusted: never let it pick classes to instantiate.
            if (configNode.SelectSingleNode("descendant-or-self::*[@Class]") != null)
            {
                error = "MBBS_Import_Unsafe".Translate();
                return null;
            }

            var wrapped = new XmlDocument();
            var root = wrapped.CreateElement(RootNode);
            wrapped.AppendChild(wrapped.CreateXmlDeclaration("1.0", "utf-8", null));
            wrapped.AppendChild(root);
            root.AppendChild(wrapped.ImportNode(configNode, true));

            Directory.CreateDirectory(ModPaths.DataFolder);
            try
            {
                wrapped.Save(ModPaths.TempFile);
                var config = BarConfigManager.ConfigFile.Read(ModPaths.TempFile);
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
                return config;
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

        public static string SaveToFile(BarConfig config)
        {
            Directory.CreateDirectory(ModPaths.ExportsFolder);
            var path = Path.Combine(ModPaths.ExportsFolder, "Arrangement_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xml");
            File.WriteAllText(path, ToText(config), new UTF8Encoding(false));
            return path;
        }

        public static IEnumerable<FileInfo> ExportedFiles()
        {
            var dir = new DirectoryInfo(ModPaths.ExportsFolder);
            if (!dir.Exists) return Enumerable.Empty<FileInfo>();
            return dir.GetFiles("*.xml").OrderByDescending(f => f.LastWriteTimeUtc);
        }

        public static BarConfig FromFile(string path, out string error) => FromText(File.ReadAllText(path, Encoding.UTF8), out error);

        /// <summary>A complete Defs file for an arrangement mod, with the same config content as an export.</summary>
        public static string ToModXml(BarConfig config, string defName, string label, string author)
        {
            var exported = new XmlDocument();
            exported.LoadXml(ToText(config));
            var configNode = exported.DocumentElement["config"];

            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            var defs = doc.CreateElement("Defs");
            doc.AppendChild(defs);
            var def = doc.CreateElement("MainButtonBarSystem.MainButtonArrangementDef");
            defs.AppendChild(def);
            Append(doc, def, "defName", defName);
            Append(doc, def, "label", label);
            Append(doc, def, "author", author);
            def.AppendChild(doc.ImportNode(configNode, true));

            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, IndentChars = "  ", OmitXmlDeclaration = false }))
                doc.Save(writer);
            return sb.ToString();
        }

        /// <summary>
        /// Import rules (spec 3.9): the whole arrangement replaces the working copy. Buttons missing here are
        /// skipped by the order merge; buttons only present here go before the menu; missing custom images fall
        /// back to the original icon when drawn. Nothing is saved until the player presses Save.
        /// </summary>
        public static BarConfig ForImport(BarConfig imported)
        {
            var copy = imported.Clone();
            copy.formatVersion = BarConfig.CurrentFormatVersion;
            return copy;
        }

        private static BarConfig Prepared(BarConfig config)
        {
            var copy = config.Clone();
            copy.Prune();
            copy.formatVersion = BarConfig.CurrentFormatVersion;
            return copy;
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
