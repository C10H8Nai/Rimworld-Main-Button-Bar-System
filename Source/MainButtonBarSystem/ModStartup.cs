using System;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Runs once after all defs and XML patches are loaded and before a game starts, so the baseline
    /// is taken before any mod rearranges buttons at runtime.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ModStartup
    {
        static ModStartup()
        {
            try
            {
                ButtonBaseline.Capture();
            }
            catch (Exception e)
            {
                Log.Error(MainButtonBarSystemMod.LogPrefix + "Could not record the original main buttons: " + e);
                return;
            }

            // Read the template now, outside any game load, so later reads never nest inside another Scribe pass.
            _ = BarConfigManager.Template;

            StartupDiagnostics.RunAtStartup();
        }
    }
}
