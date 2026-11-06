using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class LayoutSuggestionTests
    {
        private static LayoutValues Now(string mode = "Vanilla", int defaultWidth = 100, int minWidth = 80, bool full = false) =>
            new LayoutValues { Mode = mode, DefaultWidth = defaultWidth, MinWidth = minWidth, MinimizedFullWidth = full };

        [Fact]
        public void ModeNotListed_SwitchesToTheFirst()
        {
            var after = LayoutSuggestion.Apply(Now("Vanilla"), new[] { "Scroll", "CollapseScroll" }, 0, false);
            Assert.Equal("Scroll", after.Mode);
        }

        [Fact]
        public void ModeListed_IsKept()
        {
            var after = LayoutSuggestion.Apply(Now("CollapseScroll"), new[] { "Scroll", "CollapseScroll" }, 0, false);
            Assert.Equal("CollapseScroll", after.Mode);
        }

        [Fact]
        public void NoModes_KeepsTheMode()
        {
            Assert.Equal("Collapse", LayoutSuggestion.Apply(Now("Collapse"), null, 0, false).Mode);
            Assert.Equal("Collapse", LayoutSuggestion.Apply(Now("Collapse"), new string[0], 0, false).Mode);
        }

        [Fact]
        public void Width_RaisesButNeverLowers()
        {
            var raised = LayoutSuggestion.Apply(Now(defaultWidth: 100, minWidth: 80), null, 120, false);
            Assert.Equal(120, raised.DefaultWidth);
            Assert.Equal(120, raised.MinWidth);

            var kept = LayoutSuggestion.Apply(Now(defaultWidth: 150, minWidth: 130), null, 120, false);
            Assert.Equal(150, kept.DefaultWidth);
            Assert.Equal(130, kept.MinWidth);
        }

        [Fact]
        public void ZeroWidth_LeavesWidthsAlone()
        {
            var after = LayoutSuggestion.Apply(Now(defaultWidth: 60, minWidth: 40), null, 0, false);
            Assert.Equal(60, after.DefaultWidth);
            Assert.Equal(40, after.MinWidth);
        }

        [Fact]
        public void HalfButtons_TurnedOnButNeverOff()
        {
            Assert.True(LayoutSuggestion.Apply(Now(full: false), null, 0, true).MinimizedFullWidth);
            Assert.True(LayoutSuggestion.Apply(Now(full: true), null, 0, false).MinimizedFullWidth);
            Assert.False(LayoutSuggestion.Apply(Now(full: false), null, 0, false).MinimizedFullWidth);
        }

        [Fact]
        public void SameAs_ComparesEveryField()
        {
            var now = Now("Scroll", 120, 120, true);
            Assert.True(now.SameAs(LayoutSuggestion.Apply(now, new[] { "Scroll" }, 120, true)));
            Assert.False(now.SameAs(Now("Scroll", 120, 120, false)));
            Assert.False(now.SameAs(Now("Collapse", 120, 120, true)));
            Assert.False(now.SameAs(Now("Scroll", 121, 120, true)));
            Assert.False(now.SameAs(Now("Scroll", 120, 119, true)));
        }
    }
}
