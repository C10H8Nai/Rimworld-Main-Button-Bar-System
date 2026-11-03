using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Stage 1 probes for the risks listed in the design spec (section 8). Everything here is temporary
    /// and is removed before release. Probe failures are reported as plain messages, never as errors,
    /// because a failed probe is an expected outcome, not a bug.
    /// </summary>
    public static class StartupDiagnostics
    {
        // 4x4 opaque orange PNG.
        private const string ProbePngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAYAAACp8Z5+AAAAEklEQVR4nGP438DwHxkzkC4AAFBPJ+HrAPilAAAAAElFTkSuQmCC";

        private static bool patchReportDone;

        private static string DataFolder => ModPaths.DataFolder;
        private static string IconsFolder => ModPaths.IconsFolder;

        public static void RunAtStartup()
        {
            var report = new StringBuilder();
            report.AppendLine(MainButtonBarSystemMod.LogPrefix + "Stage 1 diagnostics (temporary)");

            Section(report, "Buttons", ReportButtons);
            Section(report, "Hotkeys", ReportHotkeys);
            Section(report, "Icon sources", ReportIconSources);
            Section(report, "LoadImage", ReportLoadImage);
            Section(report, "Button table", WriteButtonTable);

            Log.Message(report.ToString().TrimEnd());
        }

        /// <summary>Called on the first DoButtons, when every mod has had the chance to patch.</summary>
        public static void ReportPatchesOnce()
        {
            if (patchReportDone) return;
            patchReportDone = true;

            var report = new StringBuilder();
            report.AppendLine(MainButtonBarSystemMod.LogPrefix + "Patches on main button methods (temporary)");
            Section(report, "Patches", sb =>
            {
                Describe(sb, AccessTools.Method(typeof(MainButtonsRoot), "DoButtons"));
                Describe(sb, AccessTools.Method(typeof(MainButtonsRoot), nameof(MainButtonsRoot.MainButtonsOnGUI)));
                Describe(sb, AccessTools.Method(typeof(MainButtonsRoot), nameof(MainButtonsRoot.HandleLowPriorityShortcuts)));
                Describe(sb, AccessTools.Method(typeof(MainButtonWorker), nameof(MainButtonWorker.DoButton)));
                Describe(sb, AccessTools.Method(typeof(CameraDriver), nameof(CameraDriver.CameraDriverOnGUI)));
                Describe(sb, AccessTools.Method(typeof(KeyBindingDefGenerator), nameof(KeyBindingDefGenerator.ImpliedKeyBindingDefs)));
            });
            Log.Message(report.ToString().TrimEnd());
        }

        private static void Section(StringBuilder report, string name, Action<StringBuilder> body)
        {
            try
            {
                body(report);
            }
            catch (Exception e)
            {
                report.AppendLine($"  {name}: probe failed: {e.GetType().Name}: {e.Message}");
            }
        }

        private static void ReportButtons(StringBuilder sb)
        {
            var all = ButtonBaseline.All;
            sb.AppendLine($"  Buttons: {all.Count} MainButtonDefs, {all.Count(b => b.ButtonVisible)} buttonVisible, {all.Count(b => b.Minimized)} minimized, {all.Count(b => !string.IsNullOrEmpty(b.IconPath))} with iconPath");
            var menu = ButtonBaseline.Get("Menu");
            sb.AppendLine(menu == null
                ? "  Menu def: NOT FOUND (defName 'Menu')"
                : $"  Menu def: order {menu.Order}, last in vanilla order: {ButtonBaseline.All.Last().DefName == "Menu"}");
        }

        private static void ReportHotkeys(StringBuilder sb)
        {
            var all = ButtonBaseline.All;
            var with = all.Where(b => b.HotKeyDefName != null).ToList();
            var withoutButDefault = all.Where(b => b.HotKeyDefName == null && b.DefaultHotKey != KeyCode.None).ToList();
            sb.AppendLine($"  Hotkeys: {with.Count} buttons have a KeyBindingDef, {all.Count - with.Count} do not");
            sb.AppendLine($"  Hotkeys: buttons with defaultHotKey but no KeyBindingDef: {withoutButDefault.Count}");
            var namedLikeGenerator = with.Count(b => b.HotKeyDefName == "MainTab_" + b.DefName);
            sb.AppendLine($"  Hotkeys: KeyBindingDefs named MainTab_<defName>: {namedLikeGenerator}/{with.Count}");

            var implied = DefDatabase<KeyBindingDef>.AllDefs.Count(k => k.defName.StartsWith("MainTab_", StringComparison.Ordinal));
            sb.AppendLine($"  Hotkeys: KeyBindingDefs starting with MainTab_ in database: {implied}");

            var withoutSample = all.Where(b => b.HotKeyDefName == null).Take(8).Select(b => b.DefName);
            sb.AppendLine($"  Hotkeys: sample without KeyBindingDef: {string.Join(", ", withoutSample)}");

            var data = KeyPrefs.KeyPrefsData;
            if (data != null)
            {
                int bound = 0;
                foreach (var b in with)
                {
                    var keyDef = DefDatabase<KeyBindingDef>.GetNamedSilentFail(b.HotKeyDefName);
                    if (keyDef != null && data.GetBoundKeyCode(keyDef, KeyPrefs.BindingSlot.A) != KeyCode.None) bound++;
                }
                sb.AppendLine($"  Hotkeys: with a key bound in slot A: {bound}");
            }
        }

        private static void ReportIconSources(StringBuilder sb)
        {
            var withIcon = ButtonBaseline.All.Where(b => !string.IsNullOrEmpty(b.IconPath)).ToList();
            var resolved = withIcon.Count(b => b.IconSourceModName != null);
            sb.AppendLine($"  Icon sources: resolved {resolved}/{withIcon.Count} through mod content holders");
            foreach (var group in withIcon.Where(b => b.IconSourceModName != null).GroupBy(b => b.IconSourceModName).OrderByDescending(g => g.Count()).Take(6))
                sb.AppendLine($"    {group.Key}: {group.Count()}");

            var unresolved = withIcon.Where(b => b.IconSourceModName == null).ToList();
            if (unresolved.Count > 0)
            {
                var loadable = unresolved.Count(b => ContentFinder<Texture2D>.Get(b.IconPath, false) != null);
                sb.AppendLine($"  Icon sources: unresolved {unresolved.Count}, of which ContentFinder still loads {loadable}; sample: {string.Join(", ", unresolved.Take(5).Select(b => b.DefName + "=" + b.IconPath))}");
            }

            var holderCounts = LoadedModManager.RunningModsListForReading.Sum(m => m.GetContentHolder<Texture2D>()?.contentList?.Count ?? 0);
            sb.AppendLine($"  Icon sources: textures registered across all mod content holders: {holderCounts}");
        }

        private static void ReportLoadImage(StringBuilder sb)
        {
            var bytes = Convert.FromBase64String(ProbePngBase64);
            var tex = new Texture2D(2, 2);
            bool ok = tex.LoadImage(bytes);
            sb.AppendLine($"  LoadImage: embedded 4x4 PNG -> returned {ok}, texture {tex.width}x{tex.height}");

            Directory.CreateDirectory(IconsFolder);
            var files = Directory.GetFiles(IconsFolder, "*.png");
            sb.AppendLine($"  LoadImage: custom icon folder {IconsFolder} has {files.Length} PNG file(s)");
            foreach (var file in files.Take(5))
            {
                var t = new Texture2D(2, 2);
                bool loaded = t.LoadImage(File.ReadAllBytes(file));
                sb.AppendLine($"    {Path.GetFileName(file)} -> {loaded}, {t.width}x{t.height}");
            }
        }

        private static void WriteButtonTable(StringBuilder sb)
        {
            var folder = Path.Combine(DataFolder, "Diagnostics");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "buttons.tsv");

            var lines = new List<string>
            {
                "defName\tlabel\torder\tbuttonVisible\tminimized\tvalidWithoutMap\tworker\ttabWindow\tdefaultHotKey\thotKeyDef\tsourceMod\tsourcePackageId\ticonPath\ticonSourceMod",
            };
            foreach (var b in ButtonBaseline.All)
            {
                lines.Add(string.Join("\t", b.DefName, b.Label, b.Order, b.ButtonVisible, b.Minimized, b.ValidWithoutMap, b.WorkerClass, b.TabWindowClass,
                    b.DefaultHotKey, b.HotKeyDefName, b.SourceModName, b.SourcePackageId, b.IconPath, b.IconSourceModName));
            }
            File.WriteAllLines(path, lines, new UTF8Encoding(false));
            sb.AppendLine($"  Button table: {path}");
        }

        private static void Describe(StringBuilder sb, MethodBase method)
        {
            if (method == null)
            {
                sb.AppendLine("  (method not found)");
                return;
            }

            var info = Harmony.GetPatchInfo(method);
            string name = method.DeclaringType?.Name + "." + method.Name;
            if (info == null)
            {
                sb.AppendLine($"  {name}: no patches");
                return;
            }

            sb.AppendLine($"  {name}:");
            Line(sb, "prefix", info.Prefixes);
            Line(sb, "postfix", info.Postfixes);
            Line(sb, "transpiler", info.Transpilers);
            Line(sb, "finalizer", info.Finalizers);
        }

        private static void Line(StringBuilder sb, string kind, IEnumerable<Patch> patches)
        {
            foreach (var p in patches)
                sb.AppendLine($"    {kind}: {p.owner} (priority {p.priority}) {p.PatchMethod?.DeclaringType?.FullName}.{p.PatchMethod?.Name}");
        }
    }
}
