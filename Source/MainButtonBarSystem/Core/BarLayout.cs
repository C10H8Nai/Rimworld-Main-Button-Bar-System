using System;
using System.Collections.Generic;

// Pure layout math for the main button bar. Nothing in Core may reference Verse or UnityEngine,
// so it can be unit-tested outside the game (see Source/MainButtonBarSystem.Tests).
namespace MainButtonBarSystem.Core
{
    public enum DisplayMode
    {
        Vanilla,
        Collapse,
        Scroll,
        CollapseScroll,
    }

    public enum PinPosition
    {
        None,
        Left,
        Right,
    }

    public sealed class LayoutItem
    {
        public string Key;

        /// <summary>Half-width button (MainButtonDef.minimized).</summary>
        public bool Minimized;

        /// <summary>Per-button width override in pixels; null follows the bar default.</summary>
        public int? Width;

        public PinPosition Pin;

        /// <summary>User wants this button in the "More" drawer. Ignored by modes without a drawer and by pinned buttons.</summary>
        public bool InDrawer;

        public LayoutItem(string key, bool minimized = false, int? width = null, PinPosition pin = PinPosition.None, bool inDrawer = false)
        {
            Key = key;
            Minimized = minimized;
            Width = width;
            Pin = pin;
            InDrawer = inDrawer;
        }
    }

    /// <summary>
    /// Inputs of the layout. There are no built-in values: callers pass everything (the mod reads them from its
    /// tuning def and the player's settings).
    /// </summary>
    public sealed class LayoutOptions
    {
        public DisplayMode Mode;
        public int BarWidth;

        /// <summary>Natural width of a full button when it has no override (natural-width layouts only).</summary>
        public int DefaultWidth;

        /// <summary>No full button is narrower than this in natural-width layouts; half buttons get half.</summary>
        public int MinWidth;

        /// <summary>Space between neighbouring buttons. Vanilla mode ignores it.</summary>
        public int Gap;

        /// <summary>Stretch buttons to fill the available width when they fit.</summary>
        public bool FillScreen;

        public int MoreButtonWidth;
        public int ArrowWidth;
    }

    public readonly struct PlacedItem
    {
        public readonly string Key;
        public readonly int X;
        public readonly int Width;

        public PlacedItem(string key, int x, int width)
        {
            Key = key;
            X = x;
            Width = width;
        }

        public override string ToString() => $"{Key}@{X}+{Width}";
    }

    public sealed class LayoutResult
    {
        /// <summary>Pinned-left buttons, X in bar coordinates.</summary>
        public readonly List<PlacedItem> Left = new List<PlacedItem>();

        /// <summary>Unpinned buttons, X relative to the viewport's content origin (subtract the scroll offset to draw).</summary>
        public readonly List<PlacedItem> Center = new List<PlacedItem>();

        /// <summary>Pinned-right buttons, X in bar coordinates.</summary>
        public readonly List<PlacedItem> Right = new List<PlacedItem>();

        /// <summary>The "More" button, X in bar coordinates; null when the drawer is empty.</summary>
        public PlacedItem? More;

        public readonly List<string> Drawer = new List<string>();

        public int ViewportX;
        public int ViewportWidth;
        public int ContentWidth;
        public bool NeedsScroll;

        /// <summary>Scroll arrows, X in bar coordinates. Only meaningful when NeedsScroll.</summary>
        public int LeftArrowX;
        public int RightArrowX;
        public int ArrowWidth;

        public int MaxScroll => Math.Max(0, ContentWidth - ViewportWidth);

        public int ClampScroll(float offset)
        {
            if (offset <= 0f) return 0;
            int max = MaxScroll;
            return offset >= max ? max : (int)offset;
        }
    }

    public static class BarLayout
    {
        public const string MoreKey = "<more>";

        public static bool HasDrawer(DisplayMode mode) => mode == DisplayMode.Collapse || mode == DisplayMode.CollapseScroll;

        public static bool Scrolls(DisplayMode mode) => mode == DisplayMode.Scroll || mode == DisplayMode.CollapseScroll;

        public static LayoutResult Compute(IReadOnlyList<LayoutItem> items, LayoutOptions options)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var left = new List<LayoutItem>();
            var center = new List<LayoutItem>();
            var right = new List<LayoutItem>();
            var result = new LayoutResult();
            bool drawer = HasDrawer(options.Mode);

            foreach (var item in items)
            {
                switch (item.Pin)
                {
                    case PinPosition.Left: left.Add(item); break;
                    case PinPosition.Right: right.Add(item); break;
                    default:
                        if (drawer && item.InDrawer) result.Drawer.Add(item.Key);
                        else center.Add(item);
                        break;
                }
            }

            bool hasMore = result.Drawer.Count > 0;
            int barWidth = Math.Max(0, options.BarWidth);

            if (Scrolls(options.Mode))
                LayoutNatural(left, center, right, hasMore, barWidth, options, result, allowScroll: true);
            else if (options.Mode == DisplayMode.Collapse && !options.FillScreen)
                LayoutNatural(left, center, right, hasMore, barWidth, options, result, allowScroll: false);
            else
                LayoutSplit(left, center, right, hasMore, barWidth, options.Mode == DisplayMode.Vanilla ? 0 : Math.Max(0, options.Gap), result);

            return result;
        }

        /// <summary>
        /// Vanilla-style equal split: every visible button gets the same integer width (half buttons get half of it),
        /// and the last button takes whatever is left so the bar always spans exactly the full width.
        /// </summary>
        private static void LayoutSplit(List<LayoutItem> left, List<LayoutItem> center, List<LayoutItem> right, bool hasMore, int barWidth, int gap, LayoutResult result)
        {
            var sequence = new List<(LayoutItem item, int region)>();
            foreach (var i in left) sequence.Add((i, 0));
            foreach (var i in center) sequence.Add((i, 1));
            if (hasMore) sequence.Add((null, 2));
            foreach (var i in right) sequence.Add((i, 3));

            result.ViewportX = 0;
            result.ViewportWidth = 0;
            if (sequence.Count == 0) return;

            float units = 0f;
            foreach (var (item, _) in sequence) units += item != null && item.Minimized ? 0.5f : 1f;

            int available = Math.Max(0, barWidth - gap * (sequence.Count - 1));
            int full = (int)(available / units);
            int half = full / 2;

            int x = 0;
            int centerStart = -1, centerEnd = 0;
            for (int n = 0; n < sequence.Count; n++)
            {
                var (item, region) = sequence[n];
                int width = item != null && item.Minimized ? half : full;
                if (n == sequence.Count - 1) width = Math.Max(0, barWidth - x);

                switch (region)
                {
                    case 0: result.Left.Add(new PlacedItem(item.Key, x, width)); break;
                    case 1:
                        if (centerStart < 0) centerStart = x;
                        result.Center.Add(new PlacedItem(item.Key, x - centerStart, width));
                        centerEnd = x + width;
                        break;
                    case 2: result.More = new PlacedItem(MoreKey, x, width); break;
                    default: result.Right.Add(new PlacedItem(item.Key, x, width)); break;
                }

                x += width + gap;
            }

            if (centerStart >= 0)
            {
                result.ViewportX = centerStart;
                result.ViewportWidth = centerEnd - centerStart;
                result.ContentWidth = result.ViewportWidth;
            }
            else
            {
                // No unpinned buttons: park the empty viewport just after the left block.
                result.ViewportX = result.Left.Count > 0 ? result.Left[result.Left.Count - 1].X + result.Left[result.Left.Count - 1].Width : 0;
            }
        }

        /// <summary>Natural width of one button: override, else bar default; never below the minimum.</summary>
        public static int NaturalWidth(LayoutItem item, LayoutOptions options)
        {
            int min = Math.Max(0, options.MinWidth);
            if (item.Minimized) min /= 2;
            int width = item.Width ?? (item.Minimized ? Math.Max(options.DefaultWidth, options.MinWidth) / 2 : options.DefaultWidth);
            return Math.Max(width, min);
        }

        private static void LayoutNatural(List<LayoutItem> left, List<LayoutItem> center, List<LayoutItem> right, bool hasMore, int barWidth, LayoutOptions options, LayoutResult result, bool allowScroll)
        {
            int gap = Math.Max(0, options.Gap);

            // Pinned-left block from the left edge.
            int x = 0;
            foreach (var item in left)
            {
                int w = NaturalWidth(item, options);
                result.Left.Add(new PlacedItem(item.Key, x, w));
                x += w + gap;
            }
            int viewportStart = x;

            // "More" and pinned-right block against the right edge, in order: More, then right-pinned buttons.
            var rightWidths = new List<int>();
            if (hasMore) rightWidths.Add(Math.Max(0, options.MoreButtonWidth));
            foreach (var item in right) rightWidths.Add(NaturalWidth(item, options));

            int rightBlock = 0;
            foreach (int w in rightWidths) rightBlock += w;
            if (rightWidths.Count > 0) rightBlock += gap * rightWidths.Count; // gap before each, separating it from the viewport

            int rx = barWidth - rightBlock + (rightWidths.Count > 0 ? gap : 0);
            int index = 0;
            if (hasMore)
            {
                result.More = new PlacedItem(MoreKey, rx, rightWidths[index]);
                rx += rightWidths[index++] + gap;
            }
            foreach (var item in right)
            {
                result.Right.Add(new PlacedItem(item.Key, rx, rightWidths[index]));
                rx += rightWidths[index++] + gap;
            }

            int viewportWidth = Math.Max(0, barWidth - rightBlock - viewportStart);

            var naturals = new int[center.Count];
            int content = 0;
            for (int i = 0; i < center.Count; i++)
            {
                naturals[i] = NaturalWidth(center[i], options);
                content += naturals[i];
            }
            if (center.Count > 1) content += gap * (center.Count - 1);

            result.ViewportX = viewportStart;
            result.ViewportWidth = viewportWidth;

            if (content <= viewportWidth || !allowScroll)
            {
                if (options.FillScreen && center.Count > 0 && content < viewportWidth)
                    Stretch(naturals, viewportWidth - gap * (center.Count - 1));

                int cx = 0;
                for (int i = 0; i < center.Count; i++)
                {
                    result.Center.Add(new PlacedItem(center[i].Key, cx, naturals[i]));
                    cx += naturals[i] + gap;
                }
                result.ContentWidth = center.Count == 0 ? 0 : cx - gap;
                return;
            }

            // Overflow: reserve arrows at both ends of the viewport and scroll between them.
            int arrow = Math.Max(0, Math.Min(options.ArrowWidth, viewportWidth / 2));
            result.NeedsScroll = true;
            result.ArrowWidth = arrow;
            result.LeftArrowX = viewportStart;
            result.RightArrowX = viewportStart + viewportWidth - arrow;
            result.ViewportX = viewportStart + arrow;
            result.ViewportWidth = Math.Max(0, viewportWidth - 2 * arrow);

            int sx = 0;
            for (int i = 0; i < center.Count; i++)
            {
                result.Center.Add(new PlacedItem(center[i].Key, sx, naturals[i]));
                sx += naturals[i] + gap;
            }
            result.ContentWidth = sx - gap;
        }

        /// <summary>Scales widths up proportionally so they sum to exactly <paramref name="target"/>; the last absorbs rounding.</summary>
        private static void Stretch(int[] widths, int target)
        {
            long total = 0;
            foreach (int w in widths) total += w;
            if (total <= 0 || target <= total) return;

            int assigned = 0;
            for (int i = 0; i < widths.Length - 1; i++)
            {
                widths[i] = (int)(widths[i] * (long)target / total);
                assigned += widths[i];
            }
            widths[widths.Length - 1] = target - assigned;
        }
    }
}
