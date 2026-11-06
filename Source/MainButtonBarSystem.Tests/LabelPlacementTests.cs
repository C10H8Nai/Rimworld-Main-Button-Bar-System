using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class LabelPlacementTests
    {
        private const float Fraction = 0.15f;
        private const float Pad = 6f;

        [Fact]
        public void FollowGame_DefaultMarginIsAShareOfTheWidth()
        {
            // The game puts the text at 15% of the width; the label area starts one padding earlier.
            Assert.Equal(100f * Fraction - Pad, LabelPlacement.LeftInset(true, -1f, 100f, Fraction, Pad), 3);
        }

        [Fact]
        public void FollowGame_ExplicitMarginIsUsedLikeTheGame()
        {
            Assert.Equal(2f - Pad, LabelPlacement.LeftInset(true, 2f, 100f, Fraction, Pad), 3);
            Assert.Equal(60f - Pad, LabelPlacement.LeftInset(true, 60f, 100f, Fraction, Pad), 3);
        }

        [Fact]
        public void Aligned_DefaultMarginUsesTheWholeButton()
        {
            Assert.Equal(0f, LabelPlacement.LeftInset(false, -1f, 100f, Fraction, Pad));
        }

        [Fact]
        public void Aligned_SmallMarginChangesNothing()
        {
            // The game's 2 for a long label is less than the padding: same place as before.
            Assert.Equal(0f, LabelPlacement.LeftInset(false, 2f, 100f, Fraction, Pad));
        }

        [Fact]
        public void Aligned_RoomForAnIconIsKept()
        {
            // Vanilla Textures Expanded: icon margin + 32 added to the game's margin; the text starts where VTE wants it.
            float margin = 10f + 32f - 1f;
            float inset = LabelPlacement.LeftInset(false, margin, 150f, Fraction, Pad);
            Assert.Equal(margin - Pad, inset, 3);
            Assert.Equal(margin, inset + Pad, 3);
        }
    }
}
