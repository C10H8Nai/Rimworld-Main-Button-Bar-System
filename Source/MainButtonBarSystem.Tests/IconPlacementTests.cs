using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class IconPlacementTests
    {
        [Fact]
        public void Alone_DefaultIsCentred()
        {
            var b = IconPlacement.Alone(0f, 0f, 100f, 36f, 32f, IconAnchor.Default, 6f, 0f, 0f);
            Assert.Equal(34f, b.X);
            Assert.Equal(2f, b.Y);
            Assert.Equal(32f, b.Width);
        }

        [Fact]
        public void Alone_CornersKeepThePadding()
        {
            var tl = IconPlacement.Alone(10f, 20f, 100f, 36f, 16f, IconAnchor.TopLeft, 4f, 0f, 0f);
            Assert.Equal(14f, tl.X);
            Assert.Equal(24f, tl.Y);

            var br = IconPlacement.Alone(10f, 20f, 100f, 36f, 16f, IconAnchor.BottomRight, 4f, 0f, 0f);
            Assert.Equal(90f, br.X);
            Assert.Equal(36f, br.Y);
        }

        [Fact]
        public void Alone_LargeIconStaysInsideAndInOrder()
        {
            // 32px in a 36px-high button with 6px padding: only 2px of room above and below.
            var top = IconPlacement.Alone(0f, 0f, 100f, 36f, 32f, IconAnchor.Top, 6f, 0f, 0f);
            var mid = IconPlacement.Alone(0f, 0f, 100f, 36f, 32f, IconAnchor.Center, 6f, 0f, 0f);
            var bottom = IconPlacement.Alone(0f, 0f, 100f, 36f, 32f, IconAnchor.Bottom, 6f, 0f, 0f);
            Assert.True(top.Y <= mid.Y && mid.Y <= bottom.Y);
            Assert.True(top.Y >= 0f && bottom.Y + bottom.Height <= 36f);
        }

        [Fact]
        public void Alone_OffsetMovesAndSizeNeverExceedsButton()
        {
            var b = IconPlacement.Alone(0f, 0f, 40f, 36f, 64f, IconAnchor.Center, 6f, 5f, -3f);
            Assert.Equal(36f, b.Width);
            Assert.Equal(2f + 5f, b.X);
            Assert.Equal(0f - 3f, b.Y);
        }

        [Fact]
        public void Beside_LeftMatchesTheGamesIconAndText()
        {
            var b = IconPlacement.Beside(0f, 0f, 120f, 36f, 32f, IconAnchor.Default, 6f, 4f, 0f, 0f, out float left, out float right);
            Assert.Equal(6f, b.X);
            Assert.Equal(30f, b.Width);       // capped at height - padding
            Assert.Equal(3f, b.Y);
            Assert.Equal(6f + 30f + 4f - 6f, left);
            Assert.Equal(120f, right);
        }

        [Fact]
        public void Beside_RightPutsTheIconAtTheEnd()
        {
            var b = IconPlacement.Beside(0f, 0f, 120f, 36f, 20f, IconAnchor.Right, 6f, 4f, 0f, 2f, out float left, out float right);
            Assert.Equal(94f, b.X);
            Assert.Equal(8f + 2f, b.Y);
            Assert.Equal(0f, left);
            Assert.Equal(94f - 4f + 6f, right);
        }
    }
}
