using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    /// <summary>
    /// The built-in Nai theme is kept in two places: tools/themes/Nai.json (its textures are generated from it) and the
    /// MBBS_Nai def in Defs/MainButtonBarSystem/BarAppearances.xml (written by hand). This checks that they agree.
    /// </summary>
    public class NaiThemeConsistencyTests
    {
        private const string TexturePrefix = "MainButtonBarSystem/Themes/Nai/";

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Defs", "MainButtonBarSystem", "BarAppearances.xml")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return dir.FullName;
        }

        private static JsonElement Json()
        {
            string text = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "themes", "Nai.json"));
            return JsonDocument.Parse(text).RootElement;
        }

        private static XElement Def()
        {
            var doc = XDocument.Load(Path.Combine(RepoRoot(), "Defs", "MainButtonBarSystem", "BarAppearances.xml"));
            var def = doc.Root.Elements("MainButtonBarSystem.BarAppearanceDef").FirstOrDefault(e => (string)e.Element("defName") == "MBBS_Nai");
            Assert.NotNull(def);
            return def;
        }

        private static float[] XmlColor(string text) =>
            text.Trim().TrimStart('(').TrimEnd(')').Split(',').Select(p => float.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();

        [Fact]
        public void Header_Agrees()
        {
            var json = Json();
            var def = Def();
            Assert.Equal(json.GetProperty("label").GetString(), (string)def.Element("label"));
            Assert.Equal(json.GetProperty("description").GetString(), (string)def.Element("description"));
            Assert.Equal(json.GetProperty("author").GetString(), (string)def.Element("author"));
            Assert.Equal(json.GetProperty("displayOrder").GetInt32(), (int)def.Element("displayOrder"));
        }

        [Theory]
        [InlineData("labelColor")]
        [InlineData("labelMouseoverColor")]
        [InlineData("labelActiveColor")]
        public void LabelColors_Agree(string field)
        {
            var fromJson = Json().GetProperty(field).EnumerateArray().Select(v => v.GetSingle()).ToArray();
            var fromXml = XmlColor((string)Def().Element(field));
            Assert.Equal(fromJson.Length, fromXml.Length);
            for (int i = 0; i < fromJson.Length; i++) Assert.Equal(fromJson[i], fromXml[i], 3);
        }

        [Fact]
        public void DefaultPicture_Agrees()
        {
            var image = Json().GetProperty("buttonImage");
            Assert.Equal(TexturePrefix + image.GetProperty("file").GetString(), (string)Def().Element("buttonImage")?.Element("texture"));
        }

        [Fact]
        public void ButtonPictures_AgreeInOrder()
        {
            var fromJson = Json().GetProperty("buttonImages").EnumerateArray().ToList();
            var fromXml = Def().Element("buttonImages")?.Elements("li").ToList() ?? new List<XElement>();
            Assert.Equal(fromJson.Count, fromXml.Count);
            for (int i = 0; i < fromJson.Count; i++)
            {
                var j = fromJson[i];
                var x = fromXml[i];
                string button = j.GetProperty("button").GetString();
                Assert.True(button == (string)x.Element("button"), $"buttonImages[{i}]: {button} vs {(string)x.Element("button")}");
                Assert.Equal(TexturePrefix + j.GetProperty("file").GetString(), (string)x.Element("texture"));
                Assert.Equal(Text(j, "fit", "Cover"), (string)x.Element("fit") ?? "Cover");
                Assert.Equal(Text(j, "anchor", "Center"), (string)x.Element("anchor") ?? "Center");
                Assert.Equal(Text(j, "verticalAnchor", "Center"), (string)x.Element("verticalAnchor") ?? "Center");
                Assert.Equal(Number(j, "opacity", 1f), x.Element("opacity") == null ? 1f : (float)x.Element("opacity"), 3);
                Assert.Equal(Number(j, "inset", 0f), x.Element("inset") == null ? 0f : (float)x.Element("inset"), 3);
            }
        }

        [Fact]
        public void SuggestedLayout_Agrees()
        {
            var json = Json().GetProperty("suggestedLayout");
            var xml = Def().Element("suggestedLayout");
            Assert.NotNull(xml);
            Assert.Equal(json.GetProperty("displayModes").EnumerateArray().Select(m => m.GetString()), xml.Element("displayModes").Elements("li").Select(e => (string)e));
            Assert.Equal(json.GetProperty("buttonWidth").GetInt32(), (int)xml.Element("buttonWidth"));
            Assert.Equal(json.GetProperty("halfButtonsFullWidth").GetBoolean(), (bool)xml.Element("halfButtonsFullWidth"));
        }

        private static string Text(JsonElement e, string name, string fallback) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : fallback;

        private static float Number(JsonElement e, string name, float fallback) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : fallback;
    }
}
