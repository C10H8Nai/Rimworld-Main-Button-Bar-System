using System;
using System.Collections.Generic;

namespace MainButtonBarSystem.Core
{
    /// <summary>
    /// Last-write times of a set of files, to tell whether any of them changed, appeared or disappeared since the
    /// last look (theme projects reload when their files change, design spec 2026-11-07 §5.3).
    /// </summary>
    public sealed class FileSnapshot
    {
        private readonly Dictionary<string, long> times;

        private FileSnapshot(Dictionary<string, long> times)
        {
            this.times = times;
        }

        public int Count => times.Count;

        public static FileSnapshot Of(IEnumerable<(string path, long ticks)> files)
        {
            var times = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var (path, ticks) in files)
                if (path != null) times[path.Replace('\\', '/')] = ticks;
            return new FileSnapshot(times);
        }

        /// <summary>True when both list the same files with the same times; false against null.</summary>
        public bool SameAs(FileSnapshot other)
        {
            if (other == null || other.times.Count != times.Count) return false;
            foreach (var pair in times)
                if (!other.times.TryGetValue(pair.Key, out long t) || t != pair.Value) return false;
            return true;
        }
    }
}
