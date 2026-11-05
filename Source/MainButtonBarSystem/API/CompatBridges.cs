using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>Access to the compatibility bridges for the rest of the mod.</summary>
    public static class CompatBridges
    {
        private static bool applied;

        /// <summary>Priority fixed on the first apply of the session, by def name; outlives reloaded defs and workers.</summary>
        private static readonly Dictionary<string, CompatPriority> sessionPriorities = new Dictionary<string, CompatPriority>();

        internal static CompatPriority? SessionPriorityOf(CompatBridgeDef def) =>
            def != null && sessionPriorities.TryGetValue(def.defName, out var priority) ? priority : (CompatPriority?)null;

        /// <summary>Records <paramref name="chosen"/> the first time; later calls return the recorded priority.</summary>
        internal static CompatPriority FixSessionPriority(CompatBridgeDef def, CompatPriority chosen)
        {
            if (!sessionPriorities.TryGetValue(def.defName, out var priority))
                sessionPriorities[def.defName] = priority = chosen;
            return priority;
        }

        public static IEnumerable<CompatBridgeDef> All => DefDatabase<CompatBridgeDef>.AllDefs;

        /// <summary>
        /// Reloaded defs have fresh workers; apply again with the priorities fixed earlier this session (repeating a
        /// takeover is harmless: the other mod's patches are already gone).
        /// </summary>
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
        /// At startup, before any settings are read: fixes this session's priority of every detected bridge (changes
        /// apply after a restart anyway) and lets bridges with this mod first set their runtime button defaults.
        /// </summary>
        internal static void PrepareAtStartup()
        {
            foreach (var def in All)
            {
                try
                {
                    var worker = def.Worker;
                    if (!worker.Detected) continue;
                    if (FixSessionPriority(def, worker.Priority) == CompatPriority.Ours) worker.RegisterRuntimeDefaults();
                }
                catch (System.Exception e)
                {
                    Log.Error($"{MainButtonBarSystemMod.LogPrefix}Compatibility with {def.ModNameOrLabel} failed to prepare: {e}");
                }
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
