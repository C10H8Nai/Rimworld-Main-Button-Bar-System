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

        // Ignoring the "_steam" suffix the game adds when a local and a Steam copy of the mod both exist.
        public bool Detected => detected ??= ModLister.AnyModActiveNoSuffix(def.packageIds);

        /// <summary>The priority chosen in settings, else the def's default.</summary>
        public CompatPriority Priority => MainButtonBarSystemMod.Settings.PriorityFor(def);

        /// <summary>
        /// The priority in force this session; changes apply after a restart. Kept by def name in
        /// <see cref="CompatBridges"/>, so a play-data reload (which creates new workers) does not change it.
        /// </summary>
        public CompatPriority SessionPriority => CompatBridges.SessionPriorityOf(def) ?? CompatPriority.Theirs;

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
            if (CompatBridges.FixSessionPriority(def, Priority) == CompatPriority.Ours) TakeOver();
        }

        /// <summary>
        /// Called once at startup, before any settings are read, when this mod has priority this session. Set button
        /// defaults here (<see cref="ButtonDefaults.SetRuntimeDefault"/>), so settings saved in older formats are read
        /// against the same defaults the game will use.
        /// </summary>
        public virtual void RegisterRuntimeDefaults()
        {
        }

        /// <summary>Removes the other mod's patches on the bar. Override to also undo changes it already made.</summary>
        protected virtual void TakeOver()
        {
            foreach (var method in BarMethods())
                RemovePatchesFromOtherMod(method);
        }

        /// <summary>What the other mod has set for one button, one line each, shown as reference (spec 3.7).</summary>
        public virtual IEnumerable<string> ReferenceValues(string defName) => Enumerable.Empty<string>();

        /// <summary>What the other mod has set for the whole bar, one line each.</summary>
        public virtual IEnumerable<string> BarReferenceValues() => Enumerable.Empty<string>();

        /// <summary>The other mod's settings object, or null when it cannot be read.</summary>
        protected ModSettings OtherSettings => Detected ? OtherModSettings.Of(def.packageIds) : null;

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

    /// <summary>
    /// TooManyButtons (East.TooManyButtons): hides buttons and moves others into its own pop-up, by rewriting
    /// MainButtonDef.buttonVisible / order and the root's button list. Field names come from its source as read on
    /// 2026-11-03 (design spec 7.1); anything missing is skipped.
    /// </summary>
    public class CompatBridgeWorker_TooManyButtons : CompatBridgeWorker
    {
        protected override void TakeOver()
        {
            base.TakeOver();
            RemovePatchesFromOtherMod(AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)));
            RestoreBaselineDefFields();
            // It already rewrote this game's button list; follow the restored order instead (not by changing the list).
            BarModel.IgnoreLiveOrderOf((Find.UIRoot as UIRoot_Play)?.mainButtonsRoot);
            RegisterRuntimeDefaults();
        }

        /// <summary>Spec 4.14: its own drawer button is listed like any other, starting hidden (this mod's drawer replaces it).</summary>
        public override void RegisterRuntimeDefaults()
        {
            foreach (var button in DefDatabase<MainButtonDef>.AllDefs)
            {
                var id = button.modContentPack?.PackageId;
                if (id != null && def.packageIds.Any(p => string.Equals(p, id, StringComparison.OrdinalIgnoreCase)))
                    ButtonDefaults.SetRuntimeDefault(new ButtonOverride(button.defName) { hidden = DefaultableBool.True });
            }
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

        private enum Slot
        {
            Bar,
            Popup,
            Hidden,
        }

        /// <summary>Where TooManyButtons puts each button: from mainTabSlots, else from its three order lists.</summary>
        private Dictionary<string, Slot> Slots(ModSettings settings)
        {
            var result = new Dictionary<string, Slot>();
            foreach (var pair in OtherModSettings.Map(settings, "mainTabSlots"))
            {
                string v = pair.Value;
                result[pair.Key] = v.IndexOf("Hidden", StringComparison.OrdinalIgnoreCase) >= 0 ? Slot.Hidden
                    : v.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0 || v.IndexOf("TMB", StringComparison.OrdinalIgnoreCase) >= 0 ? Slot.Popup
                    : Slot.Bar;
            }
            foreach (var n in OtherModSettings.DefNames(settings, "tmbWindowOrder")) result.TryAdd(n, Slot.Popup);
            foreach (var n in OtherModSettings.DefNames(settings, "hiddenTabOrder")) result.TryAdd(n, Slot.Hidden);
            foreach (var n in OtherModSettings.DefNames(settings, "mainTabOrder")) result.TryAdd(n, Slot.Bar);
            return result;
        }

        public override IEnumerable<string> ReferenceValues(string defName)
        {
            var settings = OtherSettings;
            if (settings == null || !Slots(settings).TryGetValue(defName, out var slot)) yield break;
            yield return ("MBBS_Ref_" + slot).Translate(def.ModNameOrLabel);
        }

        public override bool CanImport => OtherSettings != null;

        public override void ImportInto(BarConfig config)
        {
            var settings = OtherSettings;
            if (settings == null) return;
            var order = OtherModSettings.DefNames(settings, "mainTabOrder")
                .Concat(OtherModSettings.DefNames(settings, "tmbWindowOrder"))
                .Concat(OtherModSettings.DefNames(settings, "hiddenTabOrder"))
                .Distinct().ToList();
            if (order.Count > 0) config.order = order;

            bool anyInDrawer = false;
            foreach (var pair in Slots(settings))
            {
                if (DefDatabase<MainButtonDef>.GetNamedSilentFail(pair.Key) == null) continue;
                config.SetHidden(pair.Key, pair.Value == Slot.Hidden);
                config.SetInDrawer(pair.Key, pair.Value == Slot.Popup);
                anyInDrawer |= pair.Value == Slot.Popup;
            }
            if (anyInDrawer && !(config.DisplayModeDef?.usesDrawer ?? false)) config.displayMode = "Collapse";
        }
    }

    /// <summary>
    /// Custom Bottom Bar (dogebm.custombuttons): draws the whole bar itself (DoButtons prefix returning false) with its
    /// own widths, order and per-button overrides. Field names come from its source as read on 2026-11-03 (design
    /// spec 7.1); anything missing is skipped.
    /// </summary>
    public class CompatBridgeWorker_CustomBottomBar : CompatBridgeWorker
    {
        private object OverrideFor(ModSettings settings, string defName) =>
            OtherModSettings.Sequence(settings, "overrides").FirstOrDefault(o => OtherModSettings.String(o, "defName") == defName);

        /// <summary>A per-button value that counts only when its "custom..." switch is on (when the mod has one).</summary>
        private static object Custom(object o, string flag, string value)
        {
            var on = OtherModSettings.Bool(o, flag);
            return on == false ? null : OtherModSettings.Get(o, value);
        }

        public override IEnumerable<string> ReferenceValues(string defName)
        {
            var settings = OtherSettings;
            var o = settings == null ? null : OverrideFor(settings, defName);
            if (o == null) yield break;
            string mod = def.ModNameOrLabel;
            if (OtherModSettings.Bool(o, "hidden") == true) yield return "MBBS_Ref_Hidden".Translate(mod);
            if (OtherModSettings.Bool(o, "forceShow") == true) yield return "MBBS_Ref_ForceShow".Translate(mod);
            if (Custom(o, "customWidth", "width") is object w) yield return "MBBS_Ref_Width".Translate(mod, w.ToString());
            var name = OtherModSettings.String(o, "customName");
            if (!name.NullOrEmpty()) yield return "MBBS_Ref_Name".Translate(mod, name);
            if (Custom(o, "customHideIcon", "hideIcon") is bool hide && hide) yield return "MBBS_Ref_NoIcon".Translate(mod);
            var icon = OtherModSettings.String(o, "customIcon");
            if (!icon.NullOrEmpty()) yield return "MBBS_Ref_Icon".Translate(mod, icon);
        }

        public override IEnumerable<string> BarReferenceValues()
        {
            var settings = OtherSettings;
            if (settings == null) yield break;
            string mod = def.ModNameOrLabel;
            if (OtherModSettings.Number(settings, "buttonWidth") is float w) yield return "MBBS_Ref_Width".Translate(mod, w.ToString("0"));
            if (OtherModSettings.Number(settings, "gap") is float g) yield return "MBBS_Ref_Gap".Translate(mod, g.ToString("0"));
            if (OtherModSettings.Bool(settings, "fillScreen") is bool f) yield return "MBBS_Ref_Fill".Translate(mod, (f ? "Yes" : "No").Translate());
        }

        public override bool CanImport => OtherSettings != null;

        public override void ImportInto(BarConfig config)
        {
            var settings = OtherSettings;
            if (settings == null) return;

            var order = OtherModSettings.DefNames(settings, "buttonOrder");
            if (order.Count > 0) config.order = order;
            if (OtherModSettings.Number(settings, "buttonWidth") is float w && w > 0f) config.defaultWidth = UnityEngine.Mathf.RoundToInt(w);
            if (OtherModSettings.Number(settings, "gap") is float g && g >= 0f) config.gap = UnityEngine.Mathf.RoundToInt(g);
            if (OtherModSettings.Bool(settings, "fillScreen") is bool fill) config.fillScreen = fill ? DefaultableBool.True : DefaultableBool.False;
            config.labelAlign = AlignFrom(OtherModSettings.Get(settings, "labelAlign")) ?? config.labelAlign;
            // It scrolls when the buttons overflow; scrolling is the closest mode.
            if (config.displayMode == BarConfig.VanillaMode) config.displayMode = "Scroll";

            string iconDir = OtherModSettings.RootDirOf(def.packageIds);
            foreach (var o in OtherModSettings.Sequence(settings, "overrides"))
            {
                var name = OtherModSettings.String(o, "defName");
                if (name.NullOrEmpty() || DefDatabase<MainButtonDef>.GetNamedSilentFail(name) == null) continue;
                if (OtherModSettings.Bool(o, "hidden") is bool hidden) config.SetHidden(name, hidden);
                if (OtherModSettings.Bool(o, "forceShow") is bool force) config.SetForceShow(name, force);
                if (Custom(o, "customWidth", "width") is object width && float.TryParse(width.ToString(), out var px) && px > 0f) config.SetWidth(name, UnityEngine.Mathf.RoundToInt(px));
                var label = OtherModSettings.String(o, "customName");
                if (!label.NullOrEmpty()) config.SetLabel(name, label);
                if (Custom(o, "customHideIcon", "hideIcon") is bool hide && hide) config.SetIconMode(name, IconMode.Text);
                var icon = CopyIcon(iconDir, OtherModSettings.String(o, "customIcon"));
                if (icon != null) config.SetIcon(name, icon);
            }
        }

        private static LabelAlign? AlignFrom(object value)
        {
            var text = value?.ToString();
            if (text.NullOrEmpty()) return null;
            if (text.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0) return LabelAlign.Left;
            if (text.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0) return LabelAlign.Right;
            if (text.IndexOf("Center", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Middle", StringComparison.OrdinalIgnoreCase) >= 0) return LabelAlign.Center;
            return null;
        }

        /// <summary>
        /// Its custom icons live in its own mod folder (Textures/icons), which Steam updates can wipe; copy the file into
        /// this mod's icons folder and refer to the copy.
        /// </summary>
        private static string CopyIcon(string modRoot, string icon)
        {
            if (modRoot.NullOrEmpty() || icon.NullOrEmpty()) return null;
            try
            {
                string name = System.IO.Path.GetFileName(icon);
                if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) name += ".png";
                var source = new[] { System.IO.Path.Combine(modRoot, "Textures", "icons", name), System.IO.Path.Combine(modRoot, icon) }
                    .FirstOrDefault(System.IO.File.Exists);
                if (source == null) return null;
                System.IO.Directory.CreateDirectory(ModPaths.IconsFolder);
                var target = System.IO.Path.Combine(ModPaths.IconsFolder, name);
                if (!System.IO.File.Exists(target)) System.IO.File.Copy(source, target);
                CustomIcons.Refresh();
                return CustomIcons.FilePrefix + name;
            }
            catch (Exception e)
            {
                Log.WarningOnce(MainButtonBarSystemMod.LogPrefix + "Could not copy Custom Bottom Bar icon " + icon + ": " + e.Message, icon.GetHashCode());
                return null;
            }
        }
    }
}
