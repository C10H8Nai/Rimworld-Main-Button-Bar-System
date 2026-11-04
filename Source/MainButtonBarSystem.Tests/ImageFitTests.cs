using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class ImageFitTests
    {
        // A main button: 100 wide, 36 tall, at (10, 964). A portrait 200x400 and a banner 400x100.
        private const float X = 10f, Y = 964f, W = 100f, H = 36f;

        private static void Near(float expected, float actual) => Assert.Equal(expected, actual, 4);

        [Fact]
        public void Cover_CentreCropsEvenly()
        {
            // Portrait is scaled to 100 wide (200 tall): 36/200 of its height is shown, centred.
            var uv = ImageFit.CoverCoords(W, H, 200, 400, DecorationAnchor.Center, DecorationVerticalAnchor.Center);
            Near(0f, uv.X);
            Near(1f, uv.Width);
            Near(0.18f, uv.Height);
            Near((1f - 0.18f) / 2f, uv.Y);
        }

        [Fact]
        public void Cover_TopKeepsTheTopEdge()
        {
            // Unity's texture coordinates start at the bottom: the top strip starts at v = 1 - height.
            var uv = ImageFit.CoverCoords(W, H, 200, 400, DecorationAnchor.Center, DecorationVerticalAnchor.Top);
            Near(1f - 0.18f, uv.Y);
            Near(0.18f, uv.Height);
        }

        [Fact]
        public void Cover_BottomKeepsTheBottomEdge()
        {
            var uv = ImageFit.CoverCoords(W, H, 200, 400, DecorationAnchor.Center, DecorationVerticalAnchor.Bottom);
            Near(0f, uv.Y);
        }

        [Fact]
        public void Cover_HorizontalAnchorsOnAWideImage()
        {
            // Banner is scaled to 36 tall (144 wide): 100/144 of its width is shown.
            float shown = 100f / 144f;
            var left = ImageFit.CoverCoords(W, H, 400, 100, DecorationAnchor.Left, DecorationVerticalAnchor.Center);
            var right = ImageFit.CoverCoords(W, H, 400, 100, DecorationAnchor.Right, DecorationVerticalAnchor.Center);
            var centre = ImageFit.CoverCoords(W, H, 400, 100, DecorationAnchor.Center, DecorationVerticalAnchor.Center);
            Near(0f, left.X);
            Near(1f - shown, right.X);
            Near((1f - shown) / 2f, centre.X);
            Near(shown, centre.Width);
            Near(0f, centre.Y);
            Near(1f, centre.Height);
        }

        [Fact]
        public void Cover_WithoutSizeShowsEverything()
        {
            var uv = ImageFit.CoverCoords(W, H, 0, 0, DecorationAnchor.Left, DecorationVerticalAnchor.Top);
            Near(0f, uv.X);
            Near(0f, uv.Y);
            Near(1f, uv.Width);
            Near(1f, uv.Height);
        }

        [Fact]
        public void Fit_CentresByDefault()
        {
            // Banner fits by width: 100 x 25, centred vertically.
            var box = ImageFit.FitBox(X, Y, W, H, 400, 100, DecorationAnchor.Center, DecorationVerticalAnchor.Center);
            Near(X, box.X);
            Near(100f, box.Width);
            Near(25f, box.Height);
            Near(Y + (H - 25f) / 2f, box.Y);
        }

        [Fact]
        public void Fit_AnchorsPlaceTheImage()
        {
            // Portrait fits by height: 18 x 36.
            var left = ImageFit.FitBox(X, Y, W, H, 200, 400, DecorationAnchor.Left, DecorationVerticalAnchor.Center);
            var right = ImageFit.FitBox(X, Y, W, H, 200, 400, DecorationAnchor.Right, DecorationVerticalAnchor.Center);
            Near(X, left.X);
            Near(X + W - 18f, right.X);
            Near(18f, right.Width);

            var top = ImageFit.FitBox(X, Y, W, H, 400, 100, DecorationAnchor.Center, DecorationVerticalAnchor.Top);
            var bottom = ImageFit.FitBox(X, Y, W, H, 400, 100, DecorationAnchor.Center, DecorationVerticalAnchor.Bottom);
            Near(Y, top.Y);
            Near(Y + H - 25f, bottom.Y);
        }
    }
}
