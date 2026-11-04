using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>Access to the compatibility bridges for the rest of the mod.</summary>
    public static class CompatBridges
    {
        private static bool applied;

        public static IEnumerable<CompatBridgeDef> All => DefDatabase<CompatBridgeDef>.AllDefs;

        /// <summary>Reloaded defs have fresh workers; apply the priorities to them again (repeating a takeover is harmless).</summary>
        internal static void ResetCache() => applied = false;

        public static IEnumerable<CompatBridgeDef> Detected => All.Where(d => d.Worker.Detected);

        /// <summary>True when a detected mod keeps the bar this session, so this mod must not draw it.</summary>
        public static bool AnyControlsBar
        {
            get
            {
                foreach (var def in All)
                    if (def.Worker.ControlsBar) return true;
                return false;
            }
        }

        /// <summary>
        /// Applies the chosen priorities once per session. Called from a game component's OnGUI, outside the
        /// methods that may get unpatched, after every mod has finished patching.
        /// </summary>
        public static void ApplyOnce()
        {
            if (applied) return;
            applied = true;
            foreach (var def in All)
            {
                try
                {
                    def.Worker.ApplyOnce();
                }
                catch (System.Exception e)
                {
                    Log.Error($"{MainButtonBarSystemMod.LogPrefix}Compatibility with {def.ModNameOrLabel} failed to apply: {e}");
                }
            }
        }
    }
}
