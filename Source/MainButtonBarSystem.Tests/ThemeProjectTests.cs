using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    /// <summary>Names, marker file, delete rules and file snapshots of theme projects ("New theme", design spec 2026-11-07).</summary>
    public class ThemeProjectTests
    {
        // ------------------------------------------------ names

        [Fact]
        public void PackageId_HasThemeMarkerPartAndIsValid()
        {
            string id = ThemeProjectNames.PackageId("小明 Nai", "Steel bar", "Theme");
            Assert.Equal("Nai.mbbstheme.SteelBar", id);
            Assert.True(SharingNames.IsValidPackageId(id));
        }

        [Fact]
        public void PackageId_NonAsciiNameUsesFallback()
        {
            Assert.Equal("Player.mbbstheme.Theme1107", ThemeProjectNames.PackageId("", "鋼鐵", "Theme1107"));
        }

        [Fact]
        public void UniquePackageId_SkipsTaken()
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a.mbbstheme.X", "A.MBBSTHEME.X2" };
            Assert.Equal("a.mbbstheme.X3", ThemeProjectNames.UniquePackageId("a.mbbstheme.X", taken.Contains));
            Assert.Equal("b.mbbstheme.Y", ThemeProjectNames.UniquePackageId("b.mbbstheme.Y", taken.Contains));
        }

        [Theory]
        [InlineData("Nai.mbbstheme.Steel", "NaiSteel")]
        [InlineData("nai.MBBSTheme.steel_bar", "NaiSteelBar")]
        [InlineData("me.mytheme", "MeMytheme")]
        [InlineData("x.mbbstheme.2fast", "X2fast")]
        [InlineData("", "Theme")]
        public void IdOf_DropsMarkerPart(string packageId, string expected)
        {
            Assert.Equal(expected, ThemeProjectNames.IdOf(packageId));
        }

        [Fact]
        public void DefNameOf_AddsSuffix()
        {
            Assert.Equal("NaiSteel_Theme", ThemeProjectNames.DefNameOf("NaiSteel"));
        }

        // ------------------------------------------------ marker

        [Fact]
        public void Marker_RoundTrip()
        {
            var marker = new ThemeProjectMarker
            {
                formatVersion = ThemeProjectMarker.CurrentFormat,
                packageId = "Nai.mbbstheme.Steel",
                defName = "NaiSteel_Theme",
                created = "2026-11-07T23:05:09",
                modVersion = "0.9.0",
            };
            var back = ThemeProjectMarker.Parse(marker.ToXml());
            Assert.NotNull(back);
            Assert.Equal(marker.formatVersion, back.formatVersion);
            Assert.Equal(marker.packageId, back.packageId);
            Assert.Equal(marker.defName, back.defName);
            Assert.Equal(marker.created, back.created);
            Assert.Equal(marker.modVersion, back.modVersion);
            Assert.False(back.IsNewerThanSupported);
        }

        [Theory]
        [InlineData("")]
        [InlineData("<not xml")]
        [InlineData("<MBBS_ThemeProject><formatVersion>1</formatVersion></MBBS_ThemeProject>")]
        [InlineData("<Other><packageId>a.b</packageId></Other>")]
        public void Marker_BadXmlIsNull(string xml)
        {
            Assert.Null(ThemeProjectMarker.Parse(xml));
        }

        [Fact]
        public void Marker_NewerFormatIsFlagged()
        {
            var back = ThemeProjectMarker.Parse("<MBBS_ThemeProject><formatVersion>2</formatVersion><packageId>a.mbbstheme.b</packageId></MBBS_ThemeProject>");
            Assert.NotNull(back);
            Assert.True(back.IsNewerThanSupported);
        }

        // ------------------------------------------------ rules

        [Theory]
        [InlineData(false, false, false, ThemeProjectRules.DeleteState.NotProject)]
        [InlineData(true, true, false, ThemeProjectRules.DeleteState.Running)]
        [InlineData(true, false, true, ThemeProjectRules.DeleteState.ActiveInModsConfig)]
        [InlineData(true, true, true, ThemeProjectRules.DeleteState.Running)]
        [InlineData(true, false, false, ThemeProjectRules.DeleteState.Allowed)]
        public void CanDelete_Matrix(bool hasMarker, bool running, bool active, ThemeProjectRules.DeleteState expected)
        {
            Assert.Equal(expected, ThemeProjectRules.CanDelete(hasMarker, running, active));
        }

        [Theory]
        [InlineData("Nai.mbbstheme.Steel", "nai.mbbstheme.steel", true)]
        [InlineData("Nai.mbbstheme.Steel", "nai.mbbstheme.steel_steam", true)]
        [InlineData("Nai.mbbstheme.Steel_steam", "nai.mbbstheme.steel", true)]
        [InlineData("Nai.mbbstheme.Steel", "nai.mbbstheme.steel2", false)]
        [InlineData(null, "a.b", false)]
        public void SamePackageId_IgnoresCaseAndSteamSuffix(string a, string b, bool same)
        {
            Assert.Equal(same, ThemeProjectRules.SamePackageId(a, b));
        }

        [Fact]
        public void DeletedFolderName_HasTimestamp()
        {
            Assert.Equal("Steel_20261107_230509", ThemeProjectRules.DeletedFolderName("Steel", new DateTime(2026, 11, 7, 23, 5, 9)));
        }

        // ------------------------------------------------ snapshots

        [Fact]
        public void Snapshot_SameFilesInAnyOrderAreSame()
        {
            var a = FileSnapshot.Of(new[] { ("C:/x/A.png", 10L), ("C:/x/B.png", 20L) });
            var b = FileSnapshot.Of(new[] { ("c:/x/b.png", 20L), ("c:/x/a.png", 10L) });
            Assert.True(a.SameAs(b));
        }

        [Fact]
        public void Snapshot_ChangedWhenTimeChanges()
        {
            var a = FileSnapshot.Of(new[] { ("A.png", 10L) });
            var b = FileSnapshot.Of(new[] { ("A.png", 11L) });
            Assert.False(a.SameAs(b));
        }

        [Fact]
        public void Snapshot_CountsByExtension()
        {
            var s = FileSnapshot.Of(new[] { ("Defs/A.xml", 1L), ("Defs/B.XML", 1L), ("Textures/A.png", 1L) });
            Assert.Equal(2, s.CountWithExtension(".xml"));
            Assert.Equal(1, s.CountWithExtension(".png"));
        }

        [Fact]
        public void Snapshot_ChangedWhenFileAddedOrRemoved()
        {
            var a = FileSnapshot.Of(new[] { ("A.png", 10L) });
            var b = FileSnapshot.Of(new[] { ("A.png", 10L), ("B.png", 10L) });
            Assert.False(a.SameAs(b));
            Assert.False(b.SameAs(a));
            Assert.False(a.SameAs(null));
        }

        // ------------------------------------------------ guide pictures

        [Fact]
        public void GuideFiles_OnePerLanguage_AllPngsInTheKit()
        {
            Assert.Equal(new[] { "_Guide_English.png", "_Guide_ChineseTraditional.png", "_Guide_ChineseSimplified.png", "_Guide_Japanese.png" },
                ThemeProjectNames.GuideFiles);
            string kit = Path.Combine(RepoRoot(), "ThemeKit");
            byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            foreach (var file in ThemeProjectNames.GuideFiles)
            {
                string path = Path.Combine(kit, file);
                Assert.True(File.Exists(path), file + " is missing from ThemeKit");
                var head = new byte[png.Length];
                using (var stream = File.OpenRead(path)) Assert.Equal(png.Length, stream.Read(head, 0, head.Length));
                Assert.Equal(png, head);
            }
            Assert.False(File.Exists(Path.Combine(kit, "_Guide.png")), "the English-only _Guide.png is replaced by the four guides");
            var inKit = Directory.GetFiles(kit, "_Guide*.png").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal);
            Assert.Equal(ThemeProjectNames.GuideFiles.OrderBy(n => n, StringComparer.Ordinal), inKit);
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "ThemeKit")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}
