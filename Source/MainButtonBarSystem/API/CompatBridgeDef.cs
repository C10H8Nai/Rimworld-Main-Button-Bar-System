using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MainButtonBarSystem
{
    public enum CompatPriority
    {
        /// <summary>This mod draws the bar; the other mod's effect on the bar is switched off.</summary>
        Ours,

        /// <summary>The other mod keeps the bar; this mod only keeps its settings window.</summary>
        Theirs,
    }

    /// <summary>
    /// Slot "compatibility bridge": detects another mod that takes over the whole bar, and decides who draws it
    /// (spec 3.7, 3.8). Built-in bridges cover TooManyButtons and Custom Bottom Bar.
    /// </summary>
    public class CompatBridgeDef : Def
    {
        public Type workerClass = typeof(CompatBridgeWorker);

        /// <summary>Package ids of the other mod; any active one counts as detected.</summary>
        public List<string> packageIds = new List<string>();

        /// <summary>Harmony ids the other mod patches with.</summary>
        public List<string> harmonyIds = new List<string>();

        /// <summary>Assembly names of the other mod, for patches applied without a known Harmony id.</summary>
        public List<string> assemblyNames = new List<string>();

        /// <summary>Name shown to the player.</summary>
        public string modName;

        public CompatPriority defaultPriority = CompatPriority.Theirs;

        [Unsaved] private CompatBridgeWorker workerInt;

        public CompatBridgeWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (CompatBridgeWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public string ModNameOrLabel => modName ?? LabelCap;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (workerClass == null || !typeof(CompatBridgeWorker).IsAssignableFrom(workerClass))
                yield return "workerClass must derive from " + nameof(CompatBridgeWorker);
            if (packageIds.NullOrEmpty())
                yield return "packageIds is empty";
        }
    }

    public class CompatBridgeWorker
    {
        public CompatBridgeDef def;

        private bool? detected;
        private bool tookOver;

        public bool Detected => detected ??= def.packageIds.Any(id => ModsConfig.IsActive(id) || ModsConfig.IsActive(id.ToLowerInvariant()));

        /// <summary>The priority chosen in settings, else the def's default.</summary>
        public CompatPriority Priority => MainButtonBarSystemMod.Settings.PriorityFor(def);

        /// <summary>The priority in force this session; changes apply after a restart.</summary>
        public CompatPriority SessionPriority { get; private set; } = CompatPriority.Theirs;

        /// <summary>True when the other mod keeps the bar and this mod must not draw it.</summary>
        public virtual bool ControlsBar => Detected && SessionPriority == CompatPriority.Theirs;

        /// <summary>
        /// Called once on the first bar draw, after every mod has patched. With "ours first" it switches the
        /// other mod off on the bar. Undoing that needs a restart, so the priority is fixed for the session.
        /// </summary>
        public void ApplyOnce()
        {
            if (tookOver || !Detected) return;
            tookOver = true;
            SessionPriority = Priority;
            if (SessionPriority == CompatPriority.Ours) TakeOver();
        }

        /// <summary>Removes the other mod's patches on the bar. Override to also undo changes it already made.</summary>
        protected virtual void TakeOver()
        {
            foreach (var method in BarMethods())
                RemovePatchesFromOtherMod(method);
        }

        /// <summary>The value the other mod has for a button setting, shown next to ours; null when it has none.</summary>
        public virtual string ReferenceValue(string defName, string field) => null;

        public virtual bool CanImport => false;

        /// <summary>Copies the other mod's order, hidden flags, widths and names into <paramref name="config"/>.</summary>
        public virtual void ImportInto(BarConfig config)
        {
        }

        protected static IEnumerable<MethodBase> BarMethods()
        {
            yield return AccessTools.Method(typeof(MainButtonsRoot), "DoButtons");
            yield return AccessTools.Method(typeof(MainButtonsRoot), nameof(MainButtonsRoot.MainButtonsOnGUI));
            yield return AccessTools.Method(typeof(MainButtonWorker), nameof(MainButtonWorker.DoButton));
        }

        protected void RemovePatchesFromOtherMod(MethodBase method)
        {
            if (method == null) return;
            var info = Harmony.GetPatchInfo(method);
            if (info == null) return;

            var harmony = new Harmony(MainButtonBarSystemMod.HarmonyId);
            foreach (var patch in info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers).ToList())
            {
                if (!IsFromOtherMod(patch)) continue;
                harmony.Unpatch(method, patch.PatchMethod);
                Log.Message($"{MainButtonBarSystemMod.LogPrefix}{def.ModNameOrLabel}: removed its patch {patch.PatchMethod.DeclaringType?.FullName}.{patch.PatchMethod.Name} on {method.DeclaringType?.Name}.{method.Name} (this mod has priority).");
            }
        }

        protected bool IsFromOtherMod(Patch patch)
        {
            if (patch.owner == MainButtonBarSystemMod.HarmonyId) return false;
            if (def.harmonyIds.Contains(patch.owner)) return true;
            var assembly = patch.PatchMethod?.DeclaringType?.Assembly.GetName().Name;
            return assembly != null && def.assemblyNames.Contains(assembly);
        }
    }

    /// <summary>TooManyButtons rewrites MainButtonDef.buttonVisible and order at runtime; taking over undoes that.</summary>
    public class CompatBridgeWorker_TooManyButtons : CompatBridgeWorker
    {
        protected override void TakeOver()
        {
            base.TakeOver();
            RemovePatchesFromOtherMod(AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)));
            RestoreBaselineDefFields();
        }

        public static void RestoreBaselineDefFields()
        {
            foreach (var def in DefDatabase<MainButtonDef>.AllDefs)
            {
                var b = ButtonBaseline.Get(def.defName);
                if (b == null) continue;
                def.buttonVisible = b.ButtonVisible;
                def.order = b.Order;
            }
        }
    }
}
