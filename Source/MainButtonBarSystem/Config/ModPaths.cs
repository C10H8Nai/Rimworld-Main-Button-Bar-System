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
        public static string ExportsFolder => Path.Combine(DataFolder, "Exports");
        public static string TempFile => Path.Combine(DataFolder, "transfer.tmp.xml");
    }
}
