using System;
using System.Collections.Generic;
using System.Linq;
using MainButtonBarSystem.Core;
using RimWorld;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>One button of the game that exported an arrangement, and the mod it came from.</summary>
    public class ArrangementButtonSource : IExposable
    {
        public string defName;
        public string mod;
        public string packageId;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref mod, "mod");
            Scribe_Values.Look(ref packageId, "packageId");
        }
    }

    /// <summary>
    /// Who made a shared arrangement and with which game, stored next to the config in exports and arrangement defs.
    /// Everything is optional: files from older versions have none of it, and older versions ignore it (they only
    /// read <c>config</c>). Same field names for files (Scribe) and defs (XML), so both read alike.
    /// </summary>
    public class ArrangementMeta : IExposable
    {
        public string name;
        public string author;
        public string description;
        public string gameVersion;
        public string modVersion;

        /// <summary>When it was exported, ISO 8601 (local time with offset).</summary>
        public string exported;

        /// <summary>Every main button of the exporting game; tells "not changed" apart from "not installed".</summary>
        public List<ArrangementButtonSource> buttons = new List<ArrangementButtonSource>();

        /// <summary>Mod of the chosen theme, so a missing theme can be named.</summary>
        public string appearanceMod;

        public string displayModeMod;

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref author, "author");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref gameVersion, "gameVersion");
            Scribe_Values.Look(ref modVersion, "modVersion");
            Scribe_Values.Look(ref exported, "exported");
            Scribe_Collections.Look(ref buttons, "buttons", LookMode.Deep);
            Scribe_Values.Look(ref appearanceMod, "appearanceMod");
            Scribe_Values.Look(ref displayModeMod, "displayModeMod");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                buttons ??= new List<ArrangementButtonSource>();
                buttons.RemoveAll(b => b == null || b.defName.NullOrEmpty());
            }
        }

        public bool HasButtonList => buttons != null && buttons.Count > 0;

        /// <summary>Mod name of a button of the exporting game, or null.</summary>
        public string ModOf(string defName) => buttons?.FirstOrDefault(b => b.defName == defName)?.mod;

        /// <summary>Describes this game for an export of <paramref name="config"/>.</summary>
        public static ArrangementMeta ForExport(BarConfig config, string name, string author, string description)
        {
            var meta = new ArrangementMeta
            {
                name = name.NullOrEmpty() ? null : name,
                author = author.NullOrEmpty() ? null : author,
                description = description.NullOrEmpty() ? null : description,
                gameVersion = VersionControl.CurrentVersionStringWithRev,
                modVersion = MainButtonBarSystemMod.Instance?.Content?.ModMetaData?.ModVersion,
                exported = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                appearanceMod = config.AppearanceDef?.modContentPack?.Name,
                displayModeMod = config.DisplayModeDef?.modContentPack?.Name,
            };
            foreach (var b in ButtonBaseline.All)
                meta.buttons.Add(new ArrangementButtonSource { defName = b.DefName, mod = b.SourceModName, packageId = b.SourcePackageId });
            return meta;
        }

        public ArrangementMeta Clone()
        {
            var copy = (ArrangementMeta)MemberwiseClone();
            copy.buttons = buttons?.Select(b => new ArrangementButtonSource { defName = b.defName, mod = b.mod, packageId = b.packageId }).ToList()
                           ?? new List<ArrangementButtonSource>();
            return copy;
        }
    }

    /// <summary>An arrangement read from a file, the clipboard or a def, ready to be checked and imported.</summary>
    public class SharedArrangement
    {
        public BarConfig config;
        public ArrangementMeta meta;

        /// <summary>Where it came from, for the import window: a file name, "clipboard" or a mod name.</summary>
        public string source;

        public string Title => !meta?.name.NullOrEmpty() == true ? meta.name : source;

        public ButtonMatch Match()
        {
            var mentioned = config.overrides.Where(o => o?.defName != null).Select(o => o.defName);
            var theirAll = meta != null && meta.HasButtonList ? meta.buttons.Select(b => b.defName) : null;
            return ButtonMatch.Compute(ButtonBaseline.All.Select(b => b.DefName), mentioned, theirAll, config.order);
        }
    }
}
