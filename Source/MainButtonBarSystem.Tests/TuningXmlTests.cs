using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    /// <summary>
    /// The tuning def's C# fields have no built-in values, so every one must be set in Defs/MainButtonBarSystem/Tuning.xml.
    /// This compares the fields declared in MainButtonBarTuningDef.cs with the XML, section by section.
    /// </summary>
    public class TuningXmlTests
    {
        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Defs", "MainButtonBarSystem")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return dir.FullName;
        }

        [Theory]
        [InlineData("BarDefaults", "barDefaults")]
        [InlineData("SettingRanges", "ranges")]
        [InlineData("Behaviour", "behaviour")]
        [InlineData("SettingsWindowLayout", "settingsWindow")]
        public void EveryFieldIsSetInXml(string className, string xmlSection)
        {
            var root = RepoRoot();
            var source = File.ReadAllText(Path.Combine(root, "Source", "MainButtonBarSystem", "API", "MainButtonBarTuningDef.cs"));
            var body = Regex.Match(source, @"public class " + className + @"\s*\{(?<body>.*?)\n        \}", RegexOptions.Singleline);
            Assert.True(body.Success, "class " + className + " not found");

            var fields = Regex.Matches(body.Groups["body"].Value, @"public (?:int|float|bool|IntRange|FloatRange|Vector2|GameFont) (\w+);")
                .Select(m => m.Groups[1].Value)
                .ToList();
            Assert.NotEmpty(fields);

            var xml = XDocument.Load(Path.Combine(root, "Defs", "MainButtonBarSystem", "Tuning.xml"));
            var section = xml.Descendants(xmlSection).Single();
            var missing = fields.Where(f => section.Element(f) == null || string.IsNullOrWhiteSpace(section.Element(f).Value)).ToList();

            Assert.True(missing.Count == 0, $"Tuning.xml <{xmlSection}> is missing: {string.Join(", ", missing)}");
        }
    }
}
