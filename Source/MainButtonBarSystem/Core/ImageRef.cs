using System;
using System.IO;

namespace MainButtonBarSystem.Core
{
    /// <summary>What an image reference of a custom look points at.</summary>
    public enum ImageRefKind
    {
        /// <summary>Empty or not understood.</summary>
        None,

        /// <summary>A PNG the player imported: "file:Name.png", in the mod's Images folder.</summary>
        File,

        /// <summary>One of an installed theme's button backgrounds: "ThemeDefName|button" (or buttonMouseover, buttonActive, buttonDisabled).</summary>
        ThemeField,

        /// <summary>Any texture an installed theme uses, by path: "ThemeDefName|image:Path/Under/Textures" (its button pictures).</summary>
        ThemeTexture,

        /// <summary>A texture some mod ships, by path: "tex:Path/Under/Textures" (PNGs an arrangement mod brings along).</summary>
        GameTexture,
    }

    /// <summary>A parsed image reference (design spec 2026-11-05 §5). Strings are stored; this is only read.</summary>
    public readonly struct ImageRef
    {
        public readonly ImageRefKind Kind;

        /// <summary>The theme's defName (theme kinds), else null.</summary>
        public readonly string Theme;

        /// <summary>The file name (File), the field name (ThemeField) or the texture path (ThemeTexture, GameTexture).</summary>
        public readonly string Value;

        public ImageRef(ImageRefKind kind, string theme, string value)
        {
            Kind = kind;
            Theme = theme;
            Value = value;
        }

        public const string FilePrefix = "file:";
        public const string TexturePrefix = "image:";
        public const string GameTexturePrefix = "tex:";

        public static string ForFile(string fileName) => FilePrefix + fileName;
        public static string ForThemeField(string theme, string field) => theme + "|" + field;
        public static string ForThemeTexture(string theme, string path) => theme + "|" + TexturePrefix + path;
        public static string ForGameTexture(string path) => GameTexturePrefix + path;

        public static bool IsFile(string reference) => reference != null && reference.StartsWith(FilePrefix, StringComparison.Ordinal);

        public static ImageRef Parse(string reference)
        {
            if (string.IsNullOrEmpty(reference)) return default;
            if (IsFile(reference))
            {
                string name = reference.Substring(FilePrefix.Length);
                return IsValidFileName(name) ? new ImageRef(ImageRefKind.File, null, name) : default;
            }
            if (reference.StartsWith(GameTexturePrefix, StringComparison.Ordinal))
            {
                string path = reference.Substring(GameTexturePrefix.Length);
                return path.Length == 0 ? default : new ImageRef(ImageRefKind.GameTexture, null, path);
            }
            int bar = reference.IndexOf('|');
            if (bar <= 0 || bar == reference.Length - 1) return default;
            string theme = reference.Substring(0, bar);
            string rest = reference.Substring(bar + 1);
            if (rest.StartsWith(TexturePrefix, StringComparison.Ordinal))
            {
                string path = rest.Substring(TexturePrefix.Length);
                return path.Length == 0 ? default : new ImageRef(ImageRefKind.ThemeTexture, theme, path);
            }
            return new ImageRef(ImageRefKind.ThemeField, theme, rest);
        }

        /// <summary>
        /// A plain PNG file name in the Images folder: ends in ".png", no folders, no characters a file name cannot have,
        /// not a Windows device name.
        /// </summary>
        public static bool IsValidFileName(string name) =>
            !string.IsNullOrEmpty(name) && name.Trim() == name && name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && name.Length > 4 &&
            name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && name.IndexOf('/') < 0 && name.IndexOf('\\') < 0 && !SharingNames.IsReserved(name);

        /// <summary>
        /// <paramref name="name"/>, or "Name (2).png", "Name (3).png"… — the first that <paramref name="taken"/> says is
        /// free. Used when an imported custom theme brings a PNG whose name is already in the Images folder.
        /// </summary>
        public static string UniqueFileName(string name, Func<string, bool> taken)
        {
            if (!taken(name)) return name;
            string stem = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);
            for (int i = 2; ; i++)
            {
                string candidate = stem + " (" + i + ")" + extension;
                if (!taken(candidate)) return candidate;
            }
        }
    }
}
