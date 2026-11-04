using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainButtonBarSystem.Core
{
    /// <summary>Safe names for files, folders, packageIds and defNames made from what a player types.</summary>
    public static class SharingNames
    {
        /// <summary>A file or folder name: letters, digits, spaces, '-', '_' and '.'; never empty or a reserved name.</summary>
        public static string FileName(string text, string fallback)
        {
            var sb = new StringBuilder();
            foreach (char c in (text ?? "").Trim())
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '.') sb.Append(c);
                else sb.Append('_');
            }
            string name = sb.ToString().Trim(' ', '.');
            if (name.Length > 60) name = name.Substring(0, 60).TrimEnd(' ', '.');
            if (name.Length == 0 || IsReserved(name)) return fallback;
            return name;
        }

        /// <summary>
        /// The ASCII part of a name as an identifier: letters and digits, words joined in PascalCase. Used for
        /// packageIds, defNames and texture folders, which the game wants in plain ASCII.
        /// </summary>
        public static string Identifier(string text, string fallback)
        {
            var sb = new StringBuilder();
            bool upper = true;
            foreach (char c in text ?? "")
            {
                if (c < 128 && char.IsLetterOrDigit(c))
                {
                    sb.Append(upper ? char.ToUpperInvariant(c) : c);
                    upper = false;
                }
                else
                {
                    upper = true;
                }
            }
            string id = sb.ToString();
            if (id.Length == 0) return fallback;
            if (char.IsDigit(id[0])) id = fallback + id;
            return id.Length > 40 ? id.Substring(0, 40) : id;
        }

        /// <summary>A packageId the game accepts: "author.name", ASCII letters and digits, each part starting with a letter.</summary>
        public static string PackageId(string author, string name, string nameFallback = "Arrangement") =>
            Identifier(author, "Player") + "." + Identifier(name, nameFallback);

        /// <summary>True when the text has the shape the game requires of a packageId.</summary>
        public static bool IsValidPackageId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var parts = id.Split('.');
            if (parts.Length < 2) return false;
            foreach (var part in parts)
            {
                if (part.Length == 0 || !char.IsLetter(part[0]) || part[0] > 127) return false;
                if (part.Any(c => c > 127 || !(char.IsLetterOrDigit(c) || c == '_'))) return false;
            }
            return true;
        }

        /// <summary>A path that does not exist yet: the given one, else "name (2)", "name (3)", ...</summary>
        public static string Unique(string path, Func<string, bool> exists, string extension = "")
        {
            if (!exists(path + extension)) return path + extension;
            for (int i = 2; ; i++)
            {
                string candidate = path + " (" + i + ")" + extension;
                if (!exists(candidate)) return candidate;
            }
        }

        private static readonly HashSet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        private static bool IsReserved(string name)
        {
            int dot = name.IndexOf('.');
            return Reserved.Contains(dot >= 0 ? name.Substring(0, dot) : name);
        }
    }

    /// <summary>
    /// How the buttons of a shared arrangement line up with the buttons of this game (spec 3.9). "Theirs" are the
    /// buttons of the game that exported it: the list stored with the export, else its saved order, else only the
    /// buttons it mentions.
    /// </summary>
    public sealed class ButtonMatch
    {
        /// <summary>Buttons both games have.</summary>
        public List<string> Matched { get; } = new List<string>();

        /// <summary>Buttons the arrangement sets or orders that this game does not have; their settings are skipped.</summary>
        public List<string> MissingHere { get; } = new List<string>();

        /// <summary>Buttons only this game has; empty when the exporter's full list is unknown.</summary>
        public List<string> OnlyHere { get; } = new List<string>();

        /// <summary>True when the exporter's full button list is known (stored list or saved order).</summary>
        public bool TheirListKnown { get; private set; }

        private HashSet<string> theirs;

        /// <summary>
        /// Whether the arrangement has an opinion about this button: it was in the exporter's game. Settings of other
        /// buttons are kept when importing.
        /// </summary>
        public bool KnownToThem(string defName) => defName != null && theirs.Contains(defName);

        /// <param name="mine">Buttons of this game.</param>
        /// <param name="mentioned">Buttons the arrangement orders or changes.</param>
        /// <param name="theirAll">Every button of the exporter's game, or null when not stored.</param>
        /// <param name="theirOrder">The arrangement's saved order, or null for the game's order.</param>
        public static ButtonMatch Compute(IEnumerable<string> mine, IEnumerable<string> mentioned, IEnumerable<string> theirAll, IEnumerable<string> theirOrder)
        {
            var result = new ButtonMatch();
            var mineSet = new HashSet<string>(mine.Where(n => n != null));
            var mentionedSet = new HashSet<string>(mentioned?.Where(n => n != null) ?? Enumerable.Empty<string>());
            var full = theirAll ?? theirOrder;
            result.TheirListKnown = full != null;
            result.theirs = new HashSet<string>(full?.Where(n => n != null) ?? Enumerable.Empty<string>());
            result.theirs.UnionWith(mentionedSet);

            foreach (var name in mineSet.OrderBy(n => n, StringComparer.Ordinal))
            {
                if (result.theirs.Contains(name)) result.Matched.Add(name);
                else if (result.TheirListKnown) result.OnlyHere.Add(name);
            }
            var referenced = new HashSet<string>(mentionedSet);
            if (theirOrder != null) referenced.UnionWith(theirOrder.Where(n => n != null));
            foreach (var name in referenced.OrderBy(n => n, StringComparer.Ordinal))
            {
                if (!mineSet.Contains(name)) result.MissingHere.Add(name);
            }
            return result;
        }
    }
}
