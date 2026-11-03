using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Applies custom button names by changing <c>MainButtonDef.label</c> and clearing the cached label text.
    /// Only buttons the user renamed are touched; a name is restored to the baseline when its override goes away.
    /// </summary>
    public static class LabelOverrides
    {
        private static readonly AccessTools.FieldRef<Def, TaggedString> CachedLabelCap =
            AccessTools.FieldRefAccess<Def, TaggedString>("cachedLabelCap");
        private static readonly AccessTools.FieldRef<MainButtonDef, string> CachedShortenedLabelCap =
            AccessTools.FieldRefAccess<MainButtonDef, string>("cachedShortenedLabelCap");
        private static readonly AccessTools.FieldRef<MainButtonDef, float> CachedLabelCapWidth =
            AccessTools.FieldRefAccess<MainButtonDef, float>("cachedLabelCapWidth");
        private static readonly AccessTools.FieldRef<MainButtonDef, float> CachedShortenedLabelCapWidth =
            AccessTools.FieldRefAccess<MainButtonDef, float>("cachedShortenedLabelCapWidth");

        /// <summary>defName -> label this class last wrote.</summary>
        private static readonly Dictionary<string, string> applied = new Dictionary<string, string>();

        public static void Sync(BarConfig config)
        {
            foreach (var o in config.overrides)
            {
                if (o.label.NullOrEmpty()) continue;
                if (applied.TryGetValue(o.defName, out var current) && current == o.label) continue;

                var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(o.defName);
                if (def == null) continue;
                SetLabel(def, o.label);
                applied[o.defName] = o.label;
            }

            if (applied.Count == 0) return;
            foreach (var defName in applied.Keys.ToList())
            {
                if (!config.Get(defName).label.NullOrEmpty()) continue;
                var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(defName);
                var baseline = ButtonBaseline.Get(defName);
                if (def != null && baseline != null) SetLabel(def, baseline.Label);
                applied.Remove(defName);
            }
        }

        private static void SetLabel(MainButtonDef def, string label)
        {
            def.label = label;
            CachedLabelCap(def) = default;
            CachedShortenedLabelCap(def) = null;
            CachedLabelCapWidth(def) = -1f;
            CachedShortenedLabelCapWidth(def) = -1f;
        }
    }
}
