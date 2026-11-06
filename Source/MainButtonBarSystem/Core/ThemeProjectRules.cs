using System;

namespace MainButtonBarSystem.Core
{
    /// <summary>What may be done with a theme project (design spec 2026-11-07 §5.2, §5.4).</summary>
    public static class ThemeProjectRules
    {
        public enum DeleteState
        {
            Allowed,

            /// <summary>No marker file: a built-in, Workshop or hand-made theme; never deleted.</summary>
            NotProject,

            /// <summary>The game loaded it as a mod this session.</summary>
            Running,

            /// <summary>Ticked in the mod list (the game loads it after a restart).</summary>
            ActiveInModsConfig,
        }

        public static DeleteState CanDelete(bool hasMarker, bool running, bool activeInModsConfig)
        {
            if (!hasMarker) return DeleteState.NotProject;
            if (running) return DeleteState.Running;
            if (activeInModsConfig) return DeleteState.ActiveInModsConfig;
            return DeleteState.Allowed;
        }

        private const string SteamSuffix = "_steam";

        /// <summary>
        /// The same mod: packageIds compare without case, and without the "_steam" the game adds to a Workshop copy
        /// that shares its packageId with a local one.
        /// </summary>
        public static bool SamePackageId(string a, string b)
        {
            if (a == null || b == null) return false;
            return string.Equals(WithoutSteamSuffix(a), WithoutSteamSuffix(b), StringComparison.OrdinalIgnoreCase);
        }

        private static string WithoutSteamSuffix(string id) =>
            id.EndsWith(SteamSuffix, StringComparison.OrdinalIgnoreCase) ? id.Substring(0, id.Length - SteamSuffix.Length) : id;

        /// <summary>Folder name a deleted project is moved to: "&lt;folder&gt;_yyyyMMdd_HHmmss".</summary>
        public static string DeletedFolderName(string folderName, DateTime time) => folderName + "_" + time.ToString("yyyyMMdd_HHmmss");
    }
}
