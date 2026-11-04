using System.Collections.Generic;
using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class SharingTests
    {
        [Theory]
        [InlineData("My Colony", "My Colony")]
        [InlineData("  a/b\\c:d  ", "a_b_c_d")]
        [InlineData("整齊的列", "整齊的列")]
        [InlineData("...", "fallback")]
        [InlineData("CON", "fallback")]
        [InlineData("nul.txt", "fallback")]
        [InlineData(null, "fallback")]
        public void FileName_IsSafe(string input, string expected)
        {
            Assert.Equal(expected, SharingNames.FileName(input, "fallback"));
        }

        [Fact]
        public void FileName_IsShortened()
        {
            Assert.Equal(60, SharingNames.FileName(new string('a', 100), "x").Length);
        }

        [Theory]
        [InlineData("tidy colony", "TidyColony")]
        [InlineData("NAI", "NAI")]
        [InlineData("整齊", "X")]
        [InlineData("2nd try", "X2ndTry")]
        [InlineData("my-bar_v2", "MyBarV2")]
        public void Identifier_IsAscii(string input, string expected)
        {
            Assert.Equal(expected, SharingNames.Identifier(input, "X"));
        }

        [Fact]
        public void PackageId_IsValid()
        {
            string id = SharingNames.PackageId("小明", "整齊的列");
            Assert.Equal("Player.Arrangement", id);
            Assert.True(SharingNames.IsValidPackageId(id));
            Assert.True(SharingNames.IsValidPackageId(SharingNames.PackageId("Nai", "Tidy colony 2")));
        }

        [Theory]
        [InlineData("NAI.MainButtonBarSystem", true)]
        [InlineData("a.b.c", true)]
        [InlineData("noDot", false)]
        [InlineData("a..b", false)]
        [InlineData("1a.b", false)]
        [InlineData("a.b c", false)]
        [InlineData("名.b", false)]
        [InlineData("", false)]
        public void IsValidPackageId(string id, bool valid)
        {
            Assert.Equal(valid, SharingNames.IsValidPackageId(id));
        }

        [Fact]
        public void Unique_AddsNumber()
        {
            var taken = new HashSet<string> { "a.xml", "a (2).xml" };
            Assert.Equal("a (3).xml", SharingNames.Unique("a", taken.Contains, ".xml"));
            Assert.Equal("b.xml", SharingNames.Unique("b", taken.Contains, ".xml"));
        }

        [Fact]
        public void Match_WithFullList()
        {
            var m = ButtonMatch.Compute(
                mine: new[] { "Work", "Research", "MyRace" },
                mentioned: new[] { "Research", "TheirMod" },
                theirAll: new[] { "Work", "Research", "TheirMod", "Unchanged" },
                theirOrder: null);

            Assert.True(m.TheirListKnown);
            Assert.Equal(new[] { "Research", "Work" }, m.Matched);
            Assert.Equal(new[] { "TheirMod" }, m.MissingHere);
            Assert.Equal(new[] { "MyRace" }, m.OnlyHere);
            Assert.True(m.KnownToThem("Work"));
            Assert.False(m.KnownToThem("MyRace"));
        }

        [Fact]
        public void Match_OrderCountsAsList()
        {
            var m = ButtonMatch.Compute(new[] { "a", "b", "c" }, new string[0], null, new[] { "b", "a", "x" });

            Assert.True(m.TheirListKnown);
            Assert.Equal(new[] { "a", "b" }, m.Matched);
            Assert.Equal(new[] { "x" }, m.MissingHere);
            Assert.Equal(new[] { "c" }, m.OnlyHere);
        }

        [Fact]
        public void Match_OldFileWithoutList_OnlyMentionedAreKnown()
        {
            var m = ButtonMatch.Compute(new[] { "a", "b" }, new[] { "a", "z" }, null, null);

            Assert.False(m.TheirListKnown);
            Assert.Equal(new[] { "a" }, m.Matched);
            Assert.Equal(new[] { "z" }, m.MissingHere);
            Assert.Empty(m.OnlyHere);
            Assert.True(m.KnownToThem("a"));
            Assert.False(m.KnownToThem("b"));
        }
    }
}
