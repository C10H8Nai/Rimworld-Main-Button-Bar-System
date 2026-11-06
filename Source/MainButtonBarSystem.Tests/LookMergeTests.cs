using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class LookMergeTests
    {
        private static int Mask(params (LookLayer layer, LookState state)[] set)
        {
            int mask = 0;
            foreach (var (layer, state) in set) mask |= LookMerge.Bit(layer, state);
            return mask;
        }

        [Fact]
        public void SameState_ButtonBeatsAllBeatsTheme()
        {
            int mask = Mask((LookLayer.Button, LookState.Normal), (LookLayer.AllButtons, LookState.Normal), (LookLayer.Theme, LookState.Normal));
            Assert.True(LookMerge.Find(LookState.Normal, mask, out var layer, out var found));
            Assert.Equal(LookLayer.Button, layer);
            Assert.Equal(LookState.Normal, found);

            mask = Mask((LookLayer.AllButtons, LookState.Normal), (LookLayer.Theme, LookState.Normal));
            LookMerge.Find(LookState.Normal, mask, out layer, out _);
            Assert.Equal(LookLayer.AllButtons, layer);
        }

        [Fact]
        public void ThemeForTheState_BeatsAButtonValueForALowerState()
        {
            // Spec 4.5: every layer for "open" first, only then mouse-over.
            int mask = Mask((LookLayer.Button, LookState.Mouseover), (LookLayer.Theme, LookState.Active));
            Assert.True(LookMerge.Find(LookState.Active, mask, out var layer, out var found));
            Assert.Equal(LookLayer.Theme, layer);
            Assert.Equal(LookState.Active, found);
        }

        [Fact]
        public void Open_FallsToMouseoverThenNormal()
        {
            int mask = Mask((LookLayer.AllButtons, LookState.Mouseover), (LookLayer.Button, LookState.Normal));
            LookMerge.Find(LookState.Active, mask, out var layer, out var found);
            Assert.Equal(LookLayer.AllButtons, layer);
            Assert.Equal(LookState.Mouseover, found);

            mask = Mask((LookLayer.Button, LookState.Normal));
            LookMerge.Find(LookState.Active, mask, out layer, out found);
            Assert.Equal(LookLayer.Button, layer);
            Assert.Equal(LookState.Normal, found);
        }

        [Fact]
        public void Disabled_FallsToNormal_NotToMouseover()
        {
            int mask = Mask((LookLayer.Button, LookState.Mouseover), (LookLayer.Theme, LookState.Normal));
            LookMerge.Find(LookState.Disabled, mask, out var layer, out var found);
            Assert.Equal(LookLayer.Theme, layer);
            Assert.Equal(LookState.Normal, found);
        }

        [Fact]
        public void Normal_DoesNotLookAtOtherStates()
        {
            int mask = Mask((LookLayer.Button, LookState.Mouseover), (LookLayer.Button, LookState.Active), (LookLayer.Button, LookState.Disabled));
            Assert.False(LookMerge.Find(LookState.Normal, mask, out _, out _));
            Assert.False(LookMerge.Find(LookState.Active, 0, out _, out _));
        }

        [Fact]
        public void Pick_ButtonThenAllThenFallback()
        {
            Assert.Equal(1, LookMerge.Pick(true, 1, true, 2, 3));
            Assert.Equal(2, LookMerge.Pick(false, 1, true, 2, 3));
            Assert.Equal(3, LookMerge.Pick(false, 1, false, 2, 3));
        }

        [Theory]
        [InlineData("#FF8000", 255, 128, 0)]
        [InlineData("ff8000", 255, 128, 0)]
        [InlineData("  #0a0B0c ", 10, 11, 12)]
        public void ColorHex_Parses(string text, int r, int g, int b)
        {
            Assert.True(ColorHex.TryParse(text, out int pr, out int pg, out int pb));
            Assert.Equal((r, g, b), (pr, pg, pb));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("#FFF")]
        [InlineData("#GG0000")]
        [InlineData("#FF00000")]
        public void ColorHex_RejectsOthers(string text)
        {
            Assert.False(ColorHex.TryParse(text, out _, out _, out _));
        }

        [Fact]
        public void ColorHex_FormatsAndClamps()
        {
            Assert.Equal("#FF800A", ColorHex.Format(255, 128, 10));
            Assert.Equal("#00FF00", ColorHex.Format(-5, 300, 0));
        }
    }
}
