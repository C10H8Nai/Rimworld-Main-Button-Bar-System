using System;
using System.Text;
using System.Xml;

namespace MainButtonBarSystem.Core
{
    /// <summary>
    /// The marker file at the root of a theme project (<see cref="FileName"/>). Only folders with it are theme
    /// projects, which this mod may load while they are not active, reload and delete. The defName it records is
    /// information only: which themes a project has is read from its Defs folder.
    /// </summary>
    public sealed class ThemeProjectMarker
    {
        public const string FileName = "MBBS_ThemeProject.xml";
        public const string RootNode = "MBBS_ThemeProject";
        public const int CurrentFormat = 1;

        public int formatVersion = CurrentFormat;
        public string packageId;
        public string defName;

        /// <summary>When the project was made, ISO 8601 local time.</summary>
        public string created;

        /// <summary>Version of this mod that made it.</summary>
        public string modVersion;

        /// <summary>Written by a newer version of this mod: left alone.</summary>
        public bool IsNewerThanSupported => formatVersion > CurrentFormat;

        /// <summary>The marker read from XML text; null when it is not a marker or has no packageId.</summary>
        public static ThemeProjectMarker Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var root = doc.DocumentElement;
                if (root == null || root.Name != RootNode) return null;
                var marker = new ThemeProjectMarker
                {
                    packageId = root["packageId"]?.InnerText?.Trim(),
                    defName = root["defName"]?.InnerText?.Trim(),
                    created = root["created"]?.InnerText?.Trim(),
                    modVersion = root["modVersion"]?.InnerText?.Trim(),
                };
                if (string.IsNullOrEmpty(marker.packageId)) return null;
                marker.formatVersion = int.TryParse(root["formatVersion"]?.InnerText, out int v) ? v : CurrentFormat;
                return marker;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        public string ToXml()
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            doc.AppendChild(doc.CreateComment(" Made by Main Button Bar System's \"New theme\". Not loaded by the game. "));
            var root = doc.CreateElement(RootNode);
            doc.AppendChild(root);
            Add(doc, root, "formatVersion", formatVersion.ToString());
            Add(doc, root, "packageId", packageId);
            Add(doc, root, "defName", defName);
            Add(doc, root, "created", created);
            Add(doc, root, "modVersion", modVersion);
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "  " };
            using (var writer = XmlWriter.Create(sb, settings))
                doc.Save(writer);
            return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
        }

        private static void Add(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var e = doc.CreateElement(name);
            e.InnerText = value ?? "";
            parent.AppendChild(e);
        }
    }
}
