using System;
using System.Collections.Generic;

namespace MainButtonBarSystem.Core
{
    /// <summary>
    /// Names of a theme project ("New theme", design spec 2026-11-07 §5.1): its packageId, and the identifier
    /// (<c>&lt;Id&gt;</c>) derived from it that names the def and the texture folder.
    /// </summary>
    public static class ThemeProjectNames
    {
        /// <summary>The middle part of every default packageId, so theme projects are easy to tell apart in a mod list.</summary>
        public const string MarkerPart = "mbbstheme";

        /// <summary>
        /// The one-page guides "New theme" copies from ThemeKit to the top of the new folder, one per language (the game
        /// does not load them). Made by tools/make_theme_kit.py.
        /// </summary>
        public static readonly IReadOnlyList<string> GuideFiles = new[]
        {
            "_Guide_English.png", "_Guide_ChineseTraditional.png", "_Guide_ChineseSimplified.png", "_Guide_Japanese.png",
        };

        /// <summary>"&lt;author&gt;.mbbstheme.&lt;name&gt;", from the ASCII parts of what the player typed.</summary>
        public static string PackageId(string author, string name, string nameFallback) =>
            SharingNames.Identifier(author, "Player") + "." + MarkerPart + "." + SharingNames.Identifier(name, nameFallback);

        /// <summary><paramref name="baseId"/>, or it with 2, 3, … appended until <paramref name="taken"/> says no.</summary>
        public static string UniquePackageId(string baseId, Func<string, bool> taken)
        {
            if (!taken(baseId)) return baseId;
            for (int i = 2; ; i++)
            {
                string candidate = baseId + i;
                if (!taken(candidate)) return candidate;
            }
        }

        /// <summary>
        /// The identifier a packageId gives the theme: its parts without "mbbstheme", joined in PascalCase
        /// ("Nai.mbbstheme.Steel" → "NaiSteel"); "Theme" when nothing is left.
        /// </summary>
        public static string IdOf(string packageId)
        {
            var parts = new List<string>();
            foreach (var part in (packageId ?? "").Split('.'))
                if (!string.Equals(part, MarkerPart, StringComparison.OrdinalIgnoreCase)) parts.Add(part);
            return SharingNames.Identifier(string.Join(" ", parts), "Theme");
        }

        /// <summary>defName of the theme a project starts with.</summary>
        public static string DefNameOf(string id) => id + "_Theme";
    }
}
