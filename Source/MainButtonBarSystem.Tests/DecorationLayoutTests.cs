using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class DecorationLayoutTests
    {
        // The bar on a 1920x1000 screen: full width, 36 tall, top 964.
        private const float BarX = 0f, BarY = 964f, BarW = 1920f, BarH = 36f;

        private static DecorationBox Place(DecorationAnchor anchor, DecorationVerticalAnchor vertical, float sizeX, float sizeY,
            int texW, int texH, float offsetX = 0f, float offsetY = 0f, float scale = 1f, float playerX = 0f, float playerY = 0f) =>
            DecorationLayout.Place(BarX, BarY, BarW, BarH, anchor, vertical, sizeX, sizeY, texW, texH, offsetX, offsetY, scale, playerX, playerY);

        [Fact]
        public void Place_RightBottomTouchesCorner()
        {
            var box = Place(DecorationAnchor.Right, DecorationVerticalAnchor.Bottom, 200f, 280f, 64, 64);
            Assert.Equal(1720f, box.X);
            Assert.Equal(720f, box.Y);
            Assert.Equal(200f, box.Width);
            Assert.Equal(280f, box.Height);
        }

        [Fact]
        public void Place_LeftBottomGrowsUpwards()
        {
            var box = Place(DecorationAnchor.Left, DecorationVerticalAnchor.Bottom, 0f, 0f, 200, 280);
            Assert.Equal(0f, box.X);
            Assert.Equal(BarY + BarH - 280f, box.Y);
        }

        [Fact]
        public void Place_ZeroSizeUsesTexture()
        {
            var box = Place(DecorationAnchor.Center, DecorationVerticalAnchor.Center, 0f, 0f, 48, 24);
            Assert.Equal(48f, box.Width);
            Assert.Equal(24f, box.Height);
            Assert.Equal(BarX + (BarW - 48f) / 2f, box.X);
            Assert.Equal(BarY + (BarH - 24f) / 2f, box.Y);
        }

        [Fact]
        public void Place_OneSideZeroKeepsAspect()
        {
            var wide = Place(DecorationAnchor.Left, DecorationVerticalAnchor.Top, 100f, 0f, 200, 400);
            Assert.Equal(100f, wide.Width);
            Assert.Equal(200f, wide.Height);

            var tall = Place(DecorationAnchor.Left, DecorationVerticalAnchor.Top, 0f, 100f, 200, 400);
            Assert.Equal(50f, tall.Width);
            Assert.Equal(100f, tall.Height);
        }

        [Fact]
        public void Place_ZeroTextureReturnsEmpty()
        {
            Assert.True(Place(DecorationAnchor.Left, DecorationVerticalAnchor.Bottom, 0f, 0f, 0, 0).IsEmpty);
            Assert.True(Place(DecorationAnchor.Left, DecorationVerticalAnchor.Bottom, 100f, 0f, 0, 0).IsEmpty);
        }

        [Fact]
        public void Place_PlayerScaleKeepsAnchor()
        {
            var normal = Place(DecorationAnchor.Right, DecorationVerticalAnchor.Bottom, 200f, 280f, 64, 64);
            var doubled = Place(DecorationAnchor.Right, DecorationVerticalAnchor.Bottom, 200f, 280f, 64, 64, scale: 2f);
            Assert.Equal(400f, doubled.Width);
            Assert.Equal(560f, doubled.Height);
            Assert.Equal(normal.X + normal.Width, doubled.X + doubled.Width);
            Assert.Equal(normal.Y + normal.Height, doubled.Y + doubled.Height);
        }

        [Fact]
        public void Place_NonPositiveScaleMeansOne()
        {
            var box = Place(DecorationAnchor.Left, DecorationVerticalAnchor.Top, 0f, 0f, 48, 48, scale: 0f);
            Assert.Equal(48f, box.Width);
        }

        [Fact]
        public void Place_OffsetsAdd()
        {
            var plain = Place(DecorationAnchor.Right, DecorationVerticalAnchor.Bottom, 0f, 0f, 48, 48);
            var moved = Place(DecorationAnchor.Right, DecorationVerticalAnchor.Bottom, 0f, 0f, 48, 48, offsetX: -8f, offsetY: -40f, playerX: 10f, playerY: 5f);
            Assert.Equal(plain.X + 2f, moved.X);
            Assert.Equal(plain.Y - 35f, moved.Y);
        }
    }
}
