using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Decides which <see cref="BarConfig"/> is in effect: the open settings window's working copy while it
    /// previews, else the current save's config in a game, else the shared template (spec 3.4, 4.7).
    /// </summary>
    public static class BarConfigManager
    {
        private const string RootNode = "MainButtonBarSystem";

        private static BarConfig template;
        private static bool templateLoaded;
        private static readonly BarConfig Untouched = new BarConfig();

        /// <summary>Working copy of the settings window; drawn live until the window closes.</summary>
        public static BarConfig Preview { get; set; }

        public static string TemplatePath => Path.Combine(GenFilePaths.ConfigFolderPath, "MainButtonBarSystem_Template.xml");

        public static bool InGame => Current.ProgramState == ProgramState.Playing && Current.Game != null;

        public static BarConfig Active => Preview ?? Saved;

        /// <summary>The stored config the window edits: this save's, or the template at the main menu.</summary>
        public static BarConfig Saved
        {
            get
            {
                if (InGame)
                {
                    var component = Component;
                    if (component?.config != null) return component.config;
                }
                else if (componentList != null)
                {
                    // Out of a game: let the old game's components go before the next one loads.
                    componentList = null;
                    component = null;
                }
                return Template ?? Untouched;
            }
        }

        private static List<GameComponent> componentList;
        private static int componentCount;
        private static MainButtonBarGameComponent component;

        /// <summary>
        /// This game's component, looked up again only when the game's component list changes (a new or loaded game).
        /// The settings are read several times per button on every GUI event, and the list can be long with many mods.
        /// </summary>
        private static MainButtonBarGameComponent Component
        {
            get
            {
                var list = Current.Game?.components;
                if (list == null) return null;
                if (component == null || !ReferenceEquals(list, componentList) || list.Count != componentCount)
                {
                    componentList = list;
                    componentCount = list.Count;
                    component = Current.Game.GetComponent<MainButtonBarGameComponent>();
                }
                return component;
            }
        }

        public static BarConfig Template
        {
            get
            {
                if (!templateLoaded) LoadTemplate();
                return template;
            }
        }

        public static bool HasTemplate => Template != null;

        /// <summary>Config for a new game or for a save that has never had one.</summary>
        public static BarConfig NewFromTemplate() => Template?.Clone() ?? new BarConfig();

        /// <summary>Replaces the config being edited (this save, or the template at the main menu).</summary>
        public static void Store(BarConfig config)
        {
            config.Prune();
            if (InGame)
            {
                var component = Component;
                if (component != null)
                {
                    component.config = config;
                    return;
                }
            }
            SaveTemplate(config);
        }

        public static void SaveTemplate(BarConfig config)
        {
            var copy = config.Clone();
            copy.Prune();
            copy.formatVersion = BarConfig.CurrentFormatVersion;
            Directory.CreateDirectory(Path.GetDirectoryName(TemplatePath));
            ConfigFile.Write(TemplatePath, copy);
            template = copy;
            templateLoaded = true;
        }

        private static void LoadTemplate()
        {
            templateLoaded = true;
            template = null;
            if (!File.Exists(TemplatePath)) return;

            try
            {
                template = ConfigFile.Read(TemplatePath);
            }
            catch (Exception e)
            {
                Log.Warning(MainButtonBarSystemMod.LogPrefix + "Could not read the default template " + TemplatePath + "; using the untouched bar. " + e.Message);
            }
        }

        /// <summary>Reads and writes a config as a standalone XML file through the game's Scribe system.</summary>
        public static class ConfigFile
        {
            public static void Write(string path, BarConfig config)
            {
                Scribe.saver.InitSaving(path, RootNode);
                try
                {
                    Scribe_Deep.Look(ref config, "config");
                }
                finally
                {
                    Scribe.saver.FinalizeSaving();
                }
            }

            public static BarConfig Read(string path)
            {
                BarConfig config = null;
                Scribe.loader.InitLoading(path);
                try
                {
                    Scribe_Deep.Look(ref config, "config");
                }
                finally
                {
                    Scribe.loader.FinalizeLoading();
                }
                return config;
            }
        }
    }
}
