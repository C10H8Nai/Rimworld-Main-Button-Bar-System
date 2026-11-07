using HarmonyLib;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Switching language (and other play-data reloads) does not restart the game: it clears all play data and loads it
    /// again, so every Def becomes a new object and textures from mod folders are reloaded. [StaticConstructorOnStartup]
    /// only runs once per process, so everything this mod remembered about the old defs is refreshed here.
    /// </summary>
    [HarmonyPatch(typeof(PlayDataLoader), nameof(PlayDataLoader.LoadAllPlayData))]
    public static class PlayDataLoader_LoadAllPlayData_Patch
    {
        public static void Postfix()
        {
            // The first load is handled by ModStartup. Later loads finish their work in long events, so refresh once
            // they are done, on the main thread (ContentFinder needs it).
            if (!ButtonBaseline.Captured) return;
            LongEventHandler.ExecuteWhenFinished(PlayDataReload.Refresh);
        }
    }

    /// <summary>
    /// Before the game clears play data: theme projects' themes leave the def database first, because the game's clear
    /// does not reach this mod's def type (see <see cref="ThemeProjectLoader.TakeOutBeforeReload"/>).
    /// </summary>
    [HarmonyPatch(typeof(PlayDataLoader), nameof(PlayDataLoader.ClearAllPlayData))]
    public static class PlayDataLoader_ClearAllPlayData_Patch
    {
        public static void Prefix()
        {
            if (ButtonBaseline.Captured) ThemeProjects.BeforePlayDataClear();
        }
    }

    public static class PlayDataReload
    {
        public static void Refresh()
        {
            MainButtonBarTuningDef.ResetCache();
            BarModel.ResetCache();
            ButtonDefaults.ResetCache();
            LabelOverrides.ResetCache();
            CustomIcons.ForgetGameTextures();
            // The rebuilt def database no longer has the themes of theme projects this mod loaded: read them again.
            ThemeProjects.Reset();
            ThemeProjects.ScanAtStartup();
            CustomThemes.ResetCache();
            Window_ButtonDrawer.ResetCache();
            CompatBridges.ResetCache();
            ButtonBaseline.Capture();
            Log.Message(MainButtonBarSystemMod.LogPrefix + "Play data reloaded (for example a language change); refreshed the button list.");
        }
    }
}
