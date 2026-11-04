using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Icons a player picks for a button (spec 3.2, 4.4): a texture path the game already has, or a PNG imported into
    /// <see cref="ModPaths.IconsFolder"/> (outside the mod folder, so updates never delete it), stored as
    /// <c>file:Name.png</c>. A missing file falls back to the button's own icon (spec 3.9).
    /// </summary>
    public static class CustomIcons
    {
        public const string FilePrefix = "file:";

        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> missing = new HashSet<string>();

        public static bool IsFile(string icon) => icon != null && icon.StartsWith(FilePrefix, StringComparison.Ordinal);

        public static string FileName(string icon) => IsFile(icon) ? icon.Substring(FilePrefix.Length) : null;

        /// <summary>The texture for a stored icon value, or null when unset or not found.</summary>
        public static Texture2D Resolve(string icon)
        {
            if (string.IsNullOrEmpty(icon)) return null;
            if (cache.TryGetValue(icon, out var tex)) return tex;
            if (missing.Contains(icon)) return null;

            tex = IsFile(icon) ? LoadFile(FileName(icon)) : ContentFinder<Texture2D>.Get(icon, false);
            if (tex == null) missing.Add(icon);
            else cache[icon] = tex;
            return tex;
        }

        public static bool IsMissing(string icon) => !string.IsNullOrEmpty(icon) && Resolve(icon) == null;

        private static Texture2D LoadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
            var path = Path.Combine(ModPaths.IconsFolder, fileName);
            if (!File.Exists(path)) return null;
            try
            {
                var tex = new Texture2D(2, 2);
                if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.name = "MBBS_" + fileName;
                return tex;
            }
            catch (Exception e)
            {
                Log.WarningOnce($"{MainButtonBarSystemMod.LogPrefix}Could not load icon {path}: {e.Message}", path.GetHashCode());
                return null;
            }
        }

        /// <summary>
        /// Forget textures found through ContentFinder: reloading play data destroys them. Imported PNGs are this mod's
        /// own textures and stay valid.
        /// </summary>
        internal static void ForgetGameTextures()
        {
            foreach (var key in cache.Keys.Where(k => !IsFile(k)).ToList())
                cache.Remove(key);
            missing.Clear();
        }

        /// <summary>Forget loaded files so new or changed PNGs in the folder are picked up.</summary>
        public static void Refresh()
        {
            foreach (var key in cache.Keys.Where(IsFile).ToList())
                cache.Remove(key);
            missing.Clear();
        }

        /// <summary>PNG files in the icons folder, as stored icon values.</summary>
        public static List<string> ImportedIcons()
        {
            var dir = new DirectoryInfo(ModPaths.IconsFolder);
            if (!dir.Exists) return new List<string>();
            return dir.GetFiles("*.png").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).Select(f => FilePrefix + f.Name).ToList();
        }

        /// <summary>
        /// Texture paths offered as built-in icons: those the game's buttons already use (after other mods' patches),
        /// then every icon pack (<see cref="MainButtonIconSetDef"/>).
        /// </summary>
        public static List<string> BuiltInIcons()
        {
            var fromButtons = ButtonBaseline.All.Select(b => b.IconPath).Where(p => !string.IsNullOrEmpty(p))
                .Distinct().OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
            var fromPacks = DefDatabase<MainButtonIconSetDef>.AllDefs.SelectMany(d => d.icons ?? new List<string>())
                .Where(p => !string.IsNullOrEmpty(p));
            return fromButtons.Concat(fromPacks).Distinct().ToList();
        }

        public static void OpenFolder()
        {
            Directory.CreateDirectory(ModPaths.IconsFolder);
            Application.OpenURL("file:///" + ModPaths.IconsFolder.Replace('\\', '/'));
        }

        /// <summary>Short text for a stored icon value.</summary>
        public static string Describe(string icon)
        {
            if (string.IsNullOrEmpty(icon)) return "MBBS_IconOriginal".Translate();
            string name = IsFile(icon) ? FileName(icon) : icon;
            return IsMissing(icon) ? "MBBS_IconMissing".Translate(name) : (TaggedString)name;
        }
    }
}
