using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Development-mode checks of every theme (built-in and from theme mods): textures that do not load, atlases that
    /// are not square or larger than the useful size, oversized decorations. Each finding is one warning naming the
    /// theme and the field, reported once per session. Runs at startup and after "Reload theme textures".
    /// </summary>
    public static class ThemeValidator
    {
        /// <summary>Checks every theme; returns the number of findings.</summary>
        public static int ValidateAll()
        {
            int findings = 0;
            foreach (var def in DefDatabase<BarAppearanceDef>.AllDefs) findings += Validate(def);
            return findings;
        }

        /// <summary>One line for the startup diagnostics report.</summary>
        public static void Report(StringBuilder sb)
        {
            int themes = DefDatabase<BarAppearanceDef>.AllDefs.Count();
            int findings = ValidateAll();
            sb.AppendLine($"  Themes: {themes} theme(s), {findings} warning(s)" + (findings > 0 ? " (see the warnings above)" : ""));
        }

        public static int Validate(BarAppearanceDef def)
        {
            int findings = 0;
            var behaviour = MainButtonBarTuningDef.Current.behaviour;
            foreach (var (field, path) in def.TexturePaths())
            {
                if (string.IsNullOrEmpty(path)) continue;
                var texture = def.TexQuiet(path);   // reported here with the field name, and then not again by Tex
                if (texture == null)
                {
                    findings += Warn(def, field, $"texture '{path}' not found (the game's look is used for it)");
                    continue;
                }
                if (IsAtlas(def, field))
                {
                    if (texture.width != texture.height)
                        findings += Warn(def, field, $"atlas '{path}' is {texture.width}x{texture.height}, not square: its corners will be squashed");
                    if (texture.width > behaviour.themeAtlasMaxSize)
                        findings += Warn(def, field, $"atlas '{path}' is {texture.width}x{texture.height}; above {behaviour.themeAtlasMaxSize}x{behaviour.themeAtlasMaxSize} it is only scaled down on a bar button");
                }
                else if (texture.width > behaviour.themeDecorationMaxSize || texture.height > behaviour.themeDecorationMaxSize)
                {
                    findings += Warn(def, field, $"image '{path}' is {texture.width}x{texture.height}, larger than {behaviour.themeDecorationMaxSize} on a side");
                }
            }
            return findings;
        }

        /// <summary>Fields drawn with the game's nine-slice method, where the texture must be square.</summary>
        private static bool IsAtlas(BarAppearanceDef def, string field)
        {
            switch (field)
            {
                case nameof(BarAppearanceDef.button):
                case nameof(BarAppearanceDef.buttonMouseover):
                case nameof(BarAppearanceDef.buttonActive):
                case nameof(BarAppearanceDef.buttonDisabled):
                case nameof(BarAppearanceDef.moreButton):
                    return true;
                case nameof(BarAppearanceDef.barBackground):
                    return def.barBackgroundFit == BackgroundFit.Atlas;
                case nameof(BarAppearanceDef.drawerBackground):
                    return def.drawerBackgroundFit == BackgroundFit.Atlas;
                default:
                    return false;
            }
        }

        private static int Warn(BarAppearanceDef def, string field, string message)
        {
            string mod = def.modContentPack?.Name ?? "?";
            Log.WarningOnce($"{MainButtonBarSystemMod.LogPrefix}Theme {def.defName} ({mod}), {field}: {message}", (def.defName + "|" + field + "|" + message).GetHashCode());
            return 1;
        }
    }
}
