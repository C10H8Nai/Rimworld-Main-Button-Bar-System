using System.Collections.Generic;
using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class ImageRefTests
    {
        [Fact]
        public void File()
        {
            var r = ImageRef.Parse("file:Frame 1.png");
            Assert.Equal(ImageRefKind.File, r.Kind);
            Assert.Equal("Frame 1.png", r.Value);
            Assert.Null(r.Theme);
            Assert.Equal("file:Frame 1.png", ImageRef.ForFile("Frame 1.png"));
        }

        [Theory]
        [InlineData("file:")]
        [InlineData("file:../x.png")]
        [InlineData("file:sub/x.png")]
        [InlineData("file:sub\\x.png")]
        [InlineData("file: x.png")]
        [InlineData("file:x.jpg")]
        [InlineData("file:.png")]
        [InlineData("file:theme.xml")]
        [InlineData("file:CON.png")]
        [InlineData("file:nul.PNG")]
        public void File_RejectsPaths(string reference)
        {
            Assert.Equal(ImageRefKind.None, ImageRef.Parse(reference).Kind);
        }

        [Fact]
        public void ThemeField()
        {
            var r = ImageRef.Parse("MBBS_Nai|buttonMouseover");
            Assert.Equal(ImageRefKind.ThemeField, r.Kind);
            Assert.Equal("MBBS_Nai", r.Theme);
            Assert.Equal("buttonMouseover", r.Value);
            Assert.Equal("MBBS_Nai|buttonMouseover", ImageRef.ForThemeField("MBBS_Nai", "buttonMouseover"));
        }

        [Fact]
        public void ThemeTexture()
        {
            var r = ImageRef.Parse("MBBS_Nai|image:MainButtonBarSystem/Themes/Nai/Btn_board_plain");
            Assert.Equal(ImageRefKind.ThemeTexture, r.Kind);
            Assert.Equal("MBBS_Nai", r.Theme);
            Assert.Equal("MainButtonBarSystem/Themes/Nai/Btn_board_plain", r.Value);
            Assert.Equal("A|image:P/Q", ImageRef.ForThemeTexture("A", "P/Q"));
        }

        [Fact]
        public void GameTexture()
        {
            var r = ImageRef.Parse("tex:MyArrangement/Images/Frame");
            Assert.Equal(ImageRefKind.GameTexture, r.Kind);
            Assert.Null(r.Theme);
            Assert.Equal("MyArrangement/Images/Frame", r.Value);
            Assert.Equal("tex:A/B", ImageRef.ForGameTexture("A/B"));
            Assert.Equal(ImageRefKind.None, ImageRef.Parse("tex:").Kind);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("|button")]
        [InlineData("Theme|")]
        [InlineData("Theme|image:")]
        [InlineData("plain/path")]
        public void Others_AreNone(string reference)
        {
            Assert.Equal(ImageRefKind.None, ImageRef.Parse(reference).Kind);
        }

        [Fact]
        public void UniqueFileName_NumbersOnCollision()
        {
            var taken = new HashSet<string> { "a.png", "a (2).png" };
            Assert.Equal("b.png", ImageRef.UniqueFileName("b.png", taken.Contains));
            Assert.Equal("a (3).png", ImageRef.UniqueFileName("a.png", taken.Contains));
        }
    }
}
