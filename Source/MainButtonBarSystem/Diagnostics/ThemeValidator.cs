using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Development-mode checks of every theme (built-in and from theme mods): textures that do not load, atlases that
    /// are not square or larger than the useful size, oversized decorations and pictures, button pictures for buttons
    /// that are not loaded, button pictures with no button atlas to frame them. Each finding is one warning naming the
    /// theme and the field (all button pictures for buttons that are not loaded are one finding), reported once per
    /// session. Runs at startup and after "Reload theme textures".
    /// </summary>
    public static class ThemeValidator
    {
        /// <summary>Checks every theme; returns the number of findings.</summary>
        public static int ValidateAll()
        {
            int findings = 0;
            foreach (var def in BarModel.Appearances) findings += Validate(def);
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
            findings += ValidateButtonImages(def);
            findings += ValidateButtonImagesHaveFrame(def);
            return findings;
        }

        /// <summary>
        /// Button pictures are drawn under the button atlas. With no <see cref="BarAppearanceDef.button"/> atlas the game's
        /// own button background is drawn over them, and it is opaque, so the pictures do not show at all. Only the def's
        /// fields are known here: a theme with its own worker may supply the atlas in code, so it is not checked.
        /// </summary>
        private static int ValidateButtonImagesHaveFrame(BarAppearanceDef def)
        {
            if (!def.HasButtonImages || !string.IsNullOrEmpty(def.button) || def.workerClass != typeof(BarAppearanceWorker)) return 0;
            return Warn(def, nameof(BarAppearanceDef.button),
                "the theme has button pictures but no button atlas, so the game's opaque button background covers them and they do not show; add a button atlas with a transparent middle (make_theme.py: \"hollow\": true or \"frame\": \"vanilla\" in the palette)");
        }

        /// <summary>
        /// buttonImages entries naming a button no loaded mod has (a typo, or a mod that is not active) and buttons
        /// listed twice. Hints only: a theme may well name buttons of mods the player does not use, so the buttons that
        /// are not loaded are listed in one warning per theme, however many there are (a theme covering a hundred
        /// buttons of other mods would otherwise fill the log).
        /// </summary>
        private static int ValidateButtonImages(BarAppearanceDef def)
        {
            if (def.buttonImages == null) return 0;
            int findings = 0;
            var seen = new System.Collections.Generic.HashSet<string>();
            var notLoaded = new System.Collections.Generic.List<string>();
            for (int i = 0; i < def.buttonImages.Count; i++)
            {
                var button = def.buttonImages[i]?.button;
                if (string.IsNullOrEmpty(button)) continue;   // a config error already
                if (!seen.Add(button))
                {
                    string field = $"{nameof(BarAppearanceDef.buttonImages)}[{i}].{nameof(BarButtonImage.button)}";
                    findings += Warn(def, field, $"'{button}' is listed again; this later entry is used");
                }
                else if (DefDatabase<RimWorld.MainButtonDef>.GetNamedSilentFail(button) == null)
                {
                    notLoaded.Add(button);
                }
            }
            if (notLoaded.Count > 0)
                findings += Warn(def, nameof(BarAppearanceDef.buttonImages),
                    $"{notLoaded.Count} picture(s) name main buttons that are not loaded (a typo, or a mod that is not active), so they are unused: {string.Join(", ", notLoaded)}");
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
            string mod = def.modContentPack?.Name ?? (def.IsFromInactiveProject ? "theme project " + System.IO.Path.GetFileName(def.projectFolder) : "?");
            Log.WarningOnce($"{MainButtonBarSystemMod.LogPrefix}Theme {def.defName} ({mod}), {field}: {message}", (def.defName + "|" + field + "|" + message).GetHashCode());
            return 1;
        }
    }
}
