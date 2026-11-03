using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// The "untouched" state of one main button: after every def and XML patch has loaded, before any
    /// mod changes buttons at runtime. "Reset all" and "not customised" both mean this state (spec 4.13).
    /// </summary>
    public sealed class ButtonBaselineEntry
    {
        public string DefName;
        public string Label;
        public int Order;
        public bool ButtonVisible;
        public bool Minimized;
        public bool ValidWithoutMap;
        public string IconPath;
        public string WorkerClass;
        public string TabWindowClass;
        public KeyCode DefaultHotKey;
        public string HotKeyDefName;

        /// <summary>Mod whose XML defines the button (last definition wins when a mod overrides another's def).</summary>
        public string SourceModName;
        public string SourcePackageId;

        /// <summary>Mod whose Textures folder supplies the icon; null when unresolved or no icon.</summary>
        public string IconSourceModName;

        /// <summary>No mod folder has the icon, but the game loads it from its own asset bundles.</summary>
        public bool IconFromGameAssets;
    }

    public static class ButtonBaseline
    {
        private static readonly Dictionary<string, ButtonBaselineEntry> byDefName = new Dictionary<string, ButtonBaselineEntry>();
        private static readonly List<ButtonBaselineEntry> inVanillaOrder = new List<ButtonBaselineEntry>();

        public static bool Captured { get; private set; }

        public static IReadOnlyList<ButtonBaselineEntry> All => inVanillaOrder;

        public static ButtonBaselineEntry Get(string defName) =>
            defName != null && byDefName.TryGetValue(defName, out var entry) ? entry : null;

        public static void Capture()
        {
            byDefName.Clear();
            inVanillaOrder.Clear();

            var iconSources = new IconSourceResolver();
            foreach (var def in DefDatabase<MainButtonDef>.AllDefs.OrderBy(d => d.order))
            {
                var entry = new ButtonBaselineEntry
                {
                    DefName = def.defName,
                    Label = def.label,
                    Order = def.order,
                    ButtonVisible = def.buttonVisible,
                    Minimized = def.minimized,
                    ValidWithoutMap = def.validWithoutMap,
                    IconPath = def.iconPath,
                    WorkerClass = def.workerClass?.FullName,
                    TabWindowClass = def.tabWindowClass?.FullName,
                    DefaultHotKey = def.defaultHotKey,
                    HotKeyDefName = def.hotKey?.defName,
                    SourceModName = def.modContentPack?.Name,
                    SourcePackageId = def.modContentPack?.PackageId,
                    IconSourceModName = string.IsNullOrEmpty(def.iconPath) ? null : iconSources.Resolve(def.iconPath),
                };
                if (!string.IsNullOrEmpty(def.iconPath) && entry.IconSourceModName == null)
                    entry.IconFromGameAssets = ContentFinder<Texture2D>.Get(def.iconPath, false) != null;
                byDefName[def.defName] = entry;
                inVanillaOrder.Add(entry);
            }

            Captured = true;
        }

        /// <summary>
        /// Finds which mod supplies a texture path the same way the game resolves it: the last loaded mod that has it wins.
        /// </summary>
        private sealed class IconSourceResolver
        {
            private readonly List<ModContentPack> modsLastFirst;

            public IconSourceResolver()
            {
                modsLastFirst = LoadedModManager.RunningModsListForReading.AsEnumerable().Reverse().ToList();
            }

            public string Resolve(string texturePath)
            {
                foreach (var mod in modsLastFirst)
                {
                    try
                    {
                        var holder = mod.GetContentHolder<Texture2D>();
                        if (holder?.contentList != null && holder.contentList.ContainsKey(texturePath))
                            return mod.Name;
                    }
                    catch (Exception)
                    {
                        // A mod with broken content must not stop the lookup for the others.
                    }
                }
                return null;
            }
        }
    }
}
