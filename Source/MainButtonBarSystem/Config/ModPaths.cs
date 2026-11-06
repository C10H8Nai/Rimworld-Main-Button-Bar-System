using System.IO;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Folders outside the mod folder, so Steam updates never wipe them (spec 4.4):
    /// …/RimWorld by Ludeon Studios/MainButtonBarSystem/
    /// </summary>
    public static class ModPaths
    {
        public static string DataFolder => Path.Combine(GenFilePaths.SaveDataFolderPath, "MainButtonBarSystem");
        public static string IconsFolder => Path.Combine(DataFolder, "Icons");

        /// <summary>PNGs imported for custom looks: button backgrounds and pictures (design spec 2026-11-05 §4.16).</summary>
        public static string ImagesFolder => Path.Combine(DataFolder, "Images");

        /// <summary>Saved custom themes, one folder each with theme.xml and the PNGs it uses.</summary>
        public static string ThemesFolder => Path.Combine(DataFolder, "Themes");
        public static string ExportsFolder => Path.Combine(DataFolder, "Exports");

        /// <summary>Where "export as mod" writes when the game's Mods folder is not writable.</summary>
        public static string ModExportsFolder => Path.Combine(DataFolder, "ModExports");
        public static string TempFile => Path.Combine(DataFolder, "transfer.tmp.xml");
    }
}
