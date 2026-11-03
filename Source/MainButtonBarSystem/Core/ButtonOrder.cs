using System;
using System.Collections.Generic;

namespace MainButtonBarSystem.Core
{
    /// <summary>Ordering rules for the button list (spec 3.7, 4.3, 3.9).</summary>
    public static class ButtonOrder
    {
        /// <summary>
        /// Combines a saved order with the buttons that exist now.
        /// Saved buttons keep their saved order; buttons the save does not know about (a newly added mod) are
        /// inserted just before <paramref name="insertBefore"/> when it is present, otherwise appended, in their
        /// current relative order. Saved buttons that no longer exist are dropped from the result only; callers
        /// keep their settings so they come back if the mod returns.
        /// </summary>
        public static List<string> Merge(IReadOnlyList<string> saved, IReadOnlyList<string> current, string insertBefore)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));

            var result = new List<string>(current.Count);
            if (saved == null)
            {
                result.AddRange(current);
                return result;
            }

            var exists = new HashSet<string>(current);
            var seen = new HashSet<string>();
            foreach (var key in saved)
            {
                if (key != null && exists.Contains(key) && seen.Add(key))
                    result.Add(key);
            }

            var added = new List<string>();
            foreach (var key in current)
            {
                if (!seen.Contains(key)) added.Add(key);
            }
            if (added.Count == 0) return result;

            int at = insertBefore == null ? -1 : result.IndexOf(insertBefore);
            if (at < 0) result.AddRange(added);
            else result.InsertRange(at, added);
            return result;
        }

        /// <summary>
        /// Stable partition into left-pinned, unpinned, right-pinned. This is the order buttons appear on screen,
        /// and the order the settings list shows from top to bottom.
        /// </summary>
        public static List<string> Effective(IReadOnlyList<string> order, Func<string, PinPosition> pinOf)
        {
            var left = new List<string>();
            var center = new List<string>();
            var right = new List<string>();
            foreach (var key in order)
            {
                switch (pinOf(key))
                {
                    case PinPosition.Left: left.Add(key); break;
                    case PinPosition.Right: right.Add(key); break;
                    default: center.Add(key); break;
                }
            }
            left.AddRange(center);
            left.AddRange(right);
            return left;
        }

        /// <summary>
        /// Applies a drag result using RimWorld's ReorderableWidget convention: <paramref name="to"/> is the gap
        /// index in the list before the item is removed.
        /// </summary>
        public static void Reorder<T>(IList<T> list, int from, int to)
        {
            if (from < 0 || from >= list.Count) return;
            to = Math.Max(0, Math.Min(to, list.Count));
            if (to == from || to == from + 1) return;

            var item = list[from];
            list.RemoveAt(from);
            if (to > from) to--;
            list.Insert(to, item);
        }

        /// <summary>Moves one item up (-1) or down (+1), skipping nothing. Returns false at the ends.</summary>
        public static bool Step<T>(IList<T> list, int index, int delta)
        {
            int target = index + delta;
            if (index < 0 || index >= list.Count || target < 0 || target >= list.Count) return false;
            (list[index], list[target]) = (list[target], list[index]);
            return true;
        }
    }
}
