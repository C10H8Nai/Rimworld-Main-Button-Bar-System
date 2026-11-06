using System.Collections.Generic;
using System.Linq;
using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class BarLayoutTests
    {
        /// <summary>Values every test shares unless it sets its own: fill on, 120/80 widths, 70px More, 24px arrows.</summary>
        private static LayoutOptions Opt(LayoutOptions o, bool fill = true)
        {
            o.FillScreen = fill;
            if (o.DefaultWidth == 0) o.DefaultWidth = 120;
            if (o.MinWidth == 0) o.MinWidth = 80;
            if (o.MoreButtonWidth == 0) o.MoreButtonWidth = 70;
            if (o.ArrowWidth == 0) o.ArrowWidth = 24;
            if (o.SplitMinWidth == 0) o.SplitMinWidth = 12;
            return o;
        }

        private static List<LayoutItem> Items(params string[] keys) => keys.Select(k => new LayoutItem(k)).ToList();

        private static IEnumerable<PlacedItem> AllInBarCoordinates(LayoutResult r) =>
            r.Left
                .Concat(r.Center.Select(c => new PlacedItem(c.Key, r.ViewportX + c.X, c.Width)))
                .Concat(r.More.HasValue ? new[] { r.More.Value } : new PlacedItem[0])
                .Concat(r.Right)
                .OrderBy(p => p.X);

        [Fact]
        public void Vanilla_SplitsEvenly_LastTakesRemainder()
        {
            var r = BarLayout.Compute(Items("a", "b", "c"), Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 1000 }));

            Assert.Equal(new[] { 333, 333, 334 }, r.Center.Select(c => c.Width));
            Assert.Equal(new[] { 0, 333, 666 }, r.Center.Select(c => c.X));
            Assert.False(r.NeedsScroll);
            Assert.Null(r.More);
        }

        [Fact]
        public void Vanilla_MinimizedGetsHalfOfFullWidth()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("half", minimized: true), new LayoutItem("c") };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 1000 }));

            // 2.5 units -> full = 400, half = 200, last takes 1000 - 600.
            Assert.Equal(new[] { 400, 200, 400 }, r.Center.Select(c => c.Width));
        }

        [Fact]
        public void Vanilla_IgnoresGapAndDrawer()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("b", inDrawer: true) };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 100, Gap = 10 }));

            Assert.Equal(new[] { "a", "b" }, r.Center.Select(c => c.Key));
            Assert.Equal(new[] { 50, 50 }, r.Center.Select(c => c.Width));
            Assert.Empty(r.Drawer);
        }

        [Fact]
        public void Vanilla_PinnedButtonsKeepEndsAndSpanFullWidth()
        {
            var items = new List<LayoutItem>
            {
                new LayoutItem("menu", pin: PinPosition.Right),
                new LayoutItem("a"),
                new LayoutItem("first", pin: PinPosition.Left),
                new LayoutItem("b"),
            };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 400 }));

            var placed = AllInBarCoordinates(r).ToList();
            Assert.Equal(new[] { "first", "a", "b", "menu" }, placed.Select(p => p.Key));
            Assert.Equal(0, placed[0].X);
            Assert.Equal(400, placed[3].X + placed[3].Width);
        }

        [Fact]
        public void Empty_ProducesEmptyResult()
        {
            var r = BarLayout.Compute(new List<LayoutItem>(), Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 500 }));

            Assert.Empty(r.Center);
            Assert.False(r.NeedsScroll);
            Assert.Equal(0, r.MaxScroll);
        }

        [Fact]
        public void Collapse_MovesDrawerItemsOutAndAddsMore()
        {
            var items = new List<LayoutItem>
            {
                new LayoutItem("a"),
                new LayoutItem("b", inDrawer: true),
                new LayoutItem("c"),
                new LayoutItem("menu", pin: PinPosition.Right, inDrawer: true),
            };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 400 }));

            Assert.Equal(new[] { "b" }, r.Drawer);
            Assert.Equal(new[] { "a", "c" }, r.Center.Select(c => c.Key));
            Assert.NotNull(r.More);
            Assert.Equal(new[] { "menu" }, r.Right.Select(c => c.Key));
            // Order on the bar: center, More, pinned right; four equal units filling 400px.
            Assert.Equal(new[] { "a", "c", BarLayout.MoreKey, "menu" }, AllInBarCoordinates(r).Select(p => p.Key));
            Assert.All(AllInBarCoordinates(r), p => Assert.Equal(100, p.Width));
        }

        [Fact]
        public void Vanilla_KeepsOwnWidthsAndSplitsTheRest()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("b", width: 200), new LayoutItem("c") };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 500 }));

            Assert.Equal(new[] { 150, 200, 150 }, r.Center.Select(c => c.Width));
            Assert.Equal(500, r.Center.Last().X + r.Center.Last().Width);
        }

        [Fact]
        public void Vanilla_OwnWidthWithHalfButtonAndRounding()
        {
            var items = new List<LayoutItem> { new LayoutItem("a", width: 100), new LayoutItem("b", minimized: true), new LayoutItem("c") };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 401 }));

            // 301 left for 1.5 units: 200 each, half for b, the last flexible takes the rounding.
            Assert.Equal(new[] { 100, 100, 201 }, r.Center.Select(c => c.Width));
            Assert.Equal(401, r.Center.Last().X + r.Center.Last().Width);
        }

        [Fact]
        public void Vanilla_OwnWidthsTooWideShrinkEverything()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("b", width: 290), new LayoutItem("c") };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 300, SplitMinWidth = 10 }));

            // 5px each would be left for a and c, below the 10px floor: 10 + 290 + 10 scaled down to 300.
            var widths = r.Center.Select(c => c.Width).ToArray();
            Assert.Equal(300, widths.Sum());
            Assert.True(widths[0] >= 9 && widths[2] >= 9);
            Assert.True(widths[1] < 290);
        }

        [Fact]
        public void CollapseFill_SqueezedSplitKeepsMoreAndMenuMinimums()
        {
            // Spec 2026-11-05 round 11 C.4: buttons with their own width squeeze the others; More and the menu stay usable.
            var items = new List<LayoutItem>
            {
                new LayoutItem("a", width: 400), new LayoutItem("b", width: 400), new LayoutItem("c"), new LayoutItem("d"),
                new LayoutItem("x", inDrawer: true), new LayoutItem("menu") { SplitMinWidth = 60 },
            };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 800, SplitMoreMinWidth = 50 }));

            var placed = AllInBarCoordinates(r).ToList();
            Assert.Equal(new[] { "a", "b", "c", "d", "menu", BarLayout.MoreKey }.OrderBy(k => k), placed.Select(p => p.Key).OrderBy(k => k));
            Assert.Equal(800, placed.Sum(p => p.Width));
            Assert.Equal(50, r.More.Value.Width);
            Assert.Equal(60, r.Center.Single(c => c.Key == "menu").Width);
            Assert.True(r.Center.Single(c => c.Key == "c").Width < 12);
            Assert.True(r.Center.Single(c => c.Key == "a").Width < 400);
        }

        [Fact]
        public void Vanilla_SplitMinimumsOnlyApplyWhenSqueezed()
        {
            // Without own widths the bar is split evenly like the game's, minimums or not.
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("b"), new LayoutItem("menu") { SplitMinWidth = 200 } };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 300 }));

            Assert.Equal(new[] { 100, 100, 100 }, r.Center.Select(c => c.Width));
        }

        [Fact]
        public void Vanilla_SplitMinimumsWiderThanTheBarShareIt()
        {
            var items = new List<LayoutItem> { new LayoutItem("a", width: 500), new LayoutItem("m1") { SplitMinWidth = 300 }, new LayoutItem("m2") { SplitMinWidth = 100 } };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 200 }));

            Assert.Equal(new[] { 0, 150, 50 }, r.Center.Select(c => c.Width));
        }

        [Fact]
        public void Vanilla_SplitMinimumsWithOthersSqueezedToZeroStillSpanTheBar()
        {
            // The first scaling leaves a and b at 0 px; the menu keeps its 40 and the bar is still filled exactly.
            var items = new List<LayoutItem>
            {
                new LayoutItem("big", width: 1000), new LayoutItem("a"), new LayoutItem("b"), new LayoutItem("menu") { SplitMinWidth = 40 },
            };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 50, SplitMinWidth = 1 }));

            var widths = r.Center.Select(c => c.Width).ToArray();
            Assert.Equal(50, widths.Sum());
            Assert.Equal(40, widths[3]);
        }

        [Fact]
        public void Vanilla_OnlyOwnWidthsThatFitAreKept()
        {
            var items = new List<LayoutItem> { new LayoutItem("a", width: 100), new LayoutItem("b", width: 150) };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 400 }));

            Assert.Equal(new[] { 100, 150 }, r.Center.Select(c => c.Width));
        }

        [Fact]
        public void Vanilla_AllOwnWidthsKeepPinnedRightAtTheEdge()
        {
            var items = new List<LayoutItem> { new LayoutItem("a", width: 100), new LayoutItem("menu", width: 80, pin: PinPosition.Right) };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Vanilla, BarWidth = 400 }));

            Assert.Equal(0, r.Center[0].X + r.ViewportX);
            Assert.Equal(320, r.Right[0].X);
            Assert.Equal(80, r.Right[0].Width);
        }

        [Fact]
        public void CollapseFill_KeepsOwnWidthsWithMoreAndGaps()
        {
            var items = new List<LayoutItem>
            {
                new LayoutItem("a"), new LayoutItem("b", width: 150), new LayoutItem("d", inDrawer: true),
                new LayoutItem("menu", pin: PinPosition.Right),
            };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 615, Gap = 5 }));

            // a, b, More, menu with three 5px gaps: 600 - 150 = 450 for three units.
            Assert.Equal(new[] { 150, 150 }, r.Center.Select(c => c.Width));
            Assert.Equal(150, r.More.Value.Width);
            Assert.Equal(615, r.Right.Last().X + r.Right.Last().Width);
        }

        [Fact]
        public void Collapse_WithoutDrawerItems_HasNoMoreButton()
        {
            var r = BarLayout.Compute(Items("a", "b"), Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 200 }));

            Assert.Null(r.More);
            Assert.Empty(r.Drawer);
        }

        [Fact]
        public void Collapse_WithoutFill_UsesNaturalWidthsWhenTheyFit()
        {
            var r = BarLayout.Compute(Items("a", "b", "c"), Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 400, DefaultWidth = 100, MinWidth = 50 }, fill: false));

            Assert.Equal(new[] { 100, 100, 100 }, r.Center.Select(c => c.Width));
            Assert.False(r.NeedsScroll);
        }

        [Fact]
        public void Collapse_WithoutFill_ShrinksToFitInsteadOfOverflowing()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("b", width: 200), new LayoutItem("c") };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 200, DefaultWidth = 100, MinWidth = 50 }, fill: false));

            Assert.False(r.NeedsScroll);
            Assert.Equal(new[] { 50, 100, 50 }, r.Center.Select(c => c.Width));
            Assert.Equal(200, r.ContentWidth);
            Assert.True(r.Center.All(c => c.X + c.Width <= r.ViewportWidth));
        }

        [Fact]
        public void Collapse_WithoutFill_ShrinksWithGapsAndHalfButtons()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("b", minimized: true), new LayoutItem("c") };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 210, DefaultWidth = 100, MinWidth = 50, Gap = 5 }, fill: false));

            // Naturals 100 + 50 + 100 plus two 5px gaps do not fit 210: the 200px left for buttons is shared in proportion.
            Assert.Equal(new[] { 80, 40, 80 }, r.Center.Select(c => c.Width));
            Assert.Equal(210, r.ContentWidth);
            Assert.False(r.NeedsScroll);
        }

        [Fact]
        public void Collapse_WithoutFill_ShrinksAroundMoreAndPinnedButtons()
        {
            var items = new List<LayoutItem>
            {
                new LayoutItem("a"), new LayoutItem("b"), new LayoutItem("c"), new LayoutItem("d", inDrawer: true),
                new LayoutItem("menu", pin: PinPosition.Right),
            };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Collapse, BarWidth = 300, DefaultWidth = 100, MinWidth = 50, MoreButtonWidth = 60 }, fill: false));

            var more = r.More.Value;
            Assert.Equal(300, r.Right.Last().X + r.Right.Last().Width);
            Assert.True(r.ViewportX + r.ContentWidth <= more.X);
            Assert.Equal(new[] { "a", "b", "c" }, r.Center.Select(c => c.Key));
        }

        [Fact]
        public void Scroll_DrawerFlagIgnored()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("b", inDrawer: true) };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 1000 }));

            Assert.Empty(r.Drawer);
            Assert.Null(r.More);
            Assert.Equal(2, r.Center.Count);
        }

        [Fact]
        public void Scroll_FitsAndFills()
        {
            var r = BarLayout.Compute(Items("a", "b", "c"), Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 900, DefaultWidth = 100, MinWidth = 80 }));

            Assert.False(r.NeedsScroll);
            Assert.Equal(new[] { 300, 300, 300 }, r.Center.Select(c => c.Width));
            Assert.Equal(900, r.ContentWidth);
        }

        [Fact]
        public void Scroll_FitsWithoutFill_KeepsNaturalWidth()
        {
            var r = BarLayout.Compute(Items("a", "b"), Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 900, DefaultWidth = 100, MinWidth = 80 }, fill: false));

            Assert.Equal(new[] { 100, 100 }, r.Center.Select(c => c.Width));
            Assert.Equal(200, r.ContentWidth);
        }

        [Fact]
        public void Scroll_OverflowReservesArrowsAndScrolls()
        {
            var keys = Enumerable.Range(0, 20).Select(i => "b" + i).ToArray();
            var r = BarLayout.Compute(Items(keys), Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 1000, DefaultWidth = 100, MinWidth = 80, ArrowWidth = 24 }));

            Assert.True(r.NeedsScroll);
            Assert.Equal(2000, r.ContentWidth);
            Assert.Equal(0, r.LeftArrowX);
            Assert.Equal(976, r.RightArrowX);
            Assert.Equal(24, r.ViewportX);
            Assert.Equal(952, r.ViewportWidth);
            Assert.Equal(2000 - 952, r.MaxScroll);
            Assert.All(r.Center, c => Assert.Equal(100, c.Width));
        }

        [Fact]
        public void Scroll_MinWidthWinsOverSmallerDefault()
        {
            var items = new List<LayoutItem> { new LayoutItem("a"), new LayoutItem("half", minimized: true), new LayoutItem("custom", width: 30) };
            var o = Opt(new LayoutOptions { Mode = DisplayMode.Scroll, DefaultWidth = 60, MinWidth = 100 });

            Assert.Equal(100, BarLayout.NaturalWidth(items[0], o));
            Assert.Equal(50, BarLayout.NaturalWidth(items[1], o));
            Assert.Equal(100, BarLayout.NaturalWidth(items[2], o));
        }

        [Fact]
        public void Scroll_ExplicitWidthAboveMinimumIsKept()
        {
            var o = Opt(new LayoutOptions { Mode = DisplayMode.Scroll, DefaultWidth = 120, MinWidth = 80 });

            Assert.Equal(200, BarLayout.NaturalWidth(new LayoutItem("w", width: 200), o));
            Assert.Equal(60, BarLayout.NaturalWidth(new LayoutItem("h", minimized: true), o));
        }

        [Fact]
        public void Scroll_PinnedButtonsStayOutsideViewport()
        {
            var keys = Enumerable.Range(0, 20).Select(i => "b" + i).ToList();
            var items = keys.Select(k => new LayoutItem(k)).ToList();
            items.Insert(0, new LayoutItem("left", pin: PinPosition.Left));
            items.Add(new LayoutItem("menu", pin: PinPosition.Right));
            items[5].InDrawer = true;

            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.CollapseScroll, BarWidth = 1000, DefaultWidth = 100, MinWidth = 80, MoreButtonWidth = 70, ArrowWidth = 24, Gap = 0 }));

            Assert.Equal(new PlacedItem("left", 0, 100).ToString(), r.Left.Single().ToString());
            Assert.Equal(new PlacedItem("menu", 900, 100).ToString(), r.Right.Single().ToString());
            Assert.Equal(new PlacedItem(BarLayout.MoreKey, 830, 70).ToString(), r.More.Value.ToString());
            Assert.True(r.NeedsScroll);
            Assert.Equal(100, r.LeftArrowX);
            Assert.Equal(830 - 24, r.RightArrowX);
            Assert.Equal(124, r.ViewportX);
            Assert.Equal(830 - 24 - 124, r.ViewportWidth);
            Assert.Equal(19, r.Center.Count);
        }

        [Fact]
        public void Gap_IsAppliedBetweenButtonsAndAroundBlocks()
        {
            var items = new List<LayoutItem> { new LayoutItem("left", pin: PinPosition.Left), new LayoutItem("a"), new LayoutItem("b"), new LayoutItem("menu", pin: PinPosition.Right) };
            var r = BarLayout.Compute(items, Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 1000, DefaultWidth = 100, MinWidth = 50, Gap = 10 }, fill: false));

            Assert.Equal(110, r.ViewportX);
            Assert.Equal(1000 - 110 - 110, r.ViewportWidth);
            Assert.Equal(new[] { 0, 110 }, r.Center.Select(c => c.X));
            Assert.Equal(900, r.Right.Single().X);
            Assert.Equal(210, r.ContentWidth);
        }

        [Fact]
        public void Gap_FillStillSpansViewportExactly()
        {
            var r = BarLayout.Compute(Items("a", "b", "c"), Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 1000, DefaultWidth = 100, MinWidth = 50, Gap = 10 }));

            var last = r.Center.Last();
            Assert.Equal(r.ViewportWidth, last.X + last.Width);
        }

        [Theory]
        [InlineData(-50f, 0)]
        [InlineData(10.7f, 10)]
        [InlineData(99999f, 1048)]
        public void ClampScroll_StaysInRange(float offset, int expected)
        {
            var keys = Enumerable.Range(0, 20).Select(i => "b" + i).ToArray();
            var r = BarLayout.Compute(Items(keys), Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 1000, DefaultWidth = 100, MinWidth = 80, ArrowWidth = 24 }));

            Assert.Equal(expected, r.ClampScroll(offset));
        }

        [Fact]
        public void TinyBar_DoesNotProduceNegativeSizes()
        {
            var r = BarLayout.Compute(Items("a", "b"), Opt(new LayoutOptions { Mode = DisplayMode.Scroll, BarWidth = 10, DefaultWidth = 100, MinWidth = 80, ArrowWidth = 24 }));

            Assert.True(r.ViewportWidth >= 0);
            Assert.True(r.ArrowWidth >= 0);
        }
    }
}
