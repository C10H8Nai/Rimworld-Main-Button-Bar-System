using System.Collections.Generic;
using MainButtonBarSystem.Core;
using Xunit;

namespace MainButtonBarSystem.Tests
{
    public class ButtonOrderTests
    {
        [Fact]
        public void Merge_NoSavedOrder_ReturnsCurrent()
        {
            Assert.Equal(new[] { "a", "b", "Menu" }, ButtonOrder.Merge(null, new[] { "a", "b", "Menu" }, "Menu"));
        }

        [Fact]
        public void Merge_KeepsSavedOrder()
        {
            Assert.Equal(new[] { "b", "Menu", "a" }, ButtonOrder.Merge(new[] { "b", "Menu", "a" }, new[] { "a", "b", "Menu" }, "Menu"));
        }

        [Fact]
        public void Merge_NewButtonsGoBeforeMenu_InCurrentOrder()
        {
            var merged = ButtonOrder.Merge(new[] { "b", "a", "Menu" }, new[] { "a", "new1", "b", "new2", "Menu" }, "Menu");

            Assert.Equal(new[] { "b", "a", "new1", "new2", "Menu" }, merged);
        }

        [Fact]
        public void Merge_NewButtonsAppendedWhenAnchorMissing()
        {
            Assert.Equal(new[] { "b", "a", "new" }, ButtonOrder.Merge(new[] { "b", "a" }, new[] { "a", "b", "new" }, "Menu"));
        }

        [Fact]
        public void Merge_DropsRemovedAndDuplicateAndNullEntries()
        {
            var merged = ButtonOrder.Merge(new[] { "gone", "b", null, "b", "a" }, new[] { "a", "b" }, "Menu");

            Assert.Equal(new[] { "b", "a" }, merged);
        }

        [Fact]
        public void Effective_PartitionsStably()
        {
            var pins = new Dictionary<string, PinPosition> { ["Menu"] = PinPosition.Right, ["x"] = PinPosition.Left, ["y"] = PinPosition.Left };
            var result = ButtonOrder.Effective(new[] { "a", "Menu", "y", "b", "x" }, k => pins.TryGetValue(k, out var p) ? p : PinPosition.None);

            Assert.Equal(new[] { "y", "x", "a", "b", "Menu" }, result);
        }

        [Theory]
        [InlineData(0, 3, new[] { "b", "c", "a", "d" })] // drag a down to the gap before d
        [InlineData(3, 0, new[] { "d", "a", "b", "c" })] // drag d to the top
        [InlineData(1, 4, new[] { "a", "c", "d", "b" })] // drag b to the end
        [InlineData(1, 1, new[] { "a", "b", "c", "d" })] // dropped on itself
        [InlineData(1, 2, new[] { "a", "b", "c", "d" })] // dropped right after itself
        [InlineData(2, 99, new[] { "a", "b", "d", "c" })] // out of range clamps to the end
        public void Reorder_FollowsReorderableWidgetConvention(int from, int to, string[] expected)
        {
            var list = new List<string> { "a", "b", "c", "d" };
            ButtonOrder.Reorder(list, from, to);

            Assert.Equal(expected, list);
        }

        [Fact]
        public void Step_SwapsNeighboursAndStopsAtEnds()
        {
            var list = new List<string> { "a", "b", "c" };

            Assert.True(ButtonOrder.Step(list, 1, -1));
            Assert.Equal(new[] { "b", "a", "c" }, list);
            Assert.False(ButtonOrder.Step(list, 0, -1));
            Assert.False(ButtonOrder.Step(list, 2, 1));
        }
    }
}
