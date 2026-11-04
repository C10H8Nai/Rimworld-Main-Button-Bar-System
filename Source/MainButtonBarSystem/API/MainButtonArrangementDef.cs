using System.Collections.Generic;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// An arrangement shipped as a mod ("sort block", spec 3.9, 4.15). The <c>config</c> node has the same content
    /// as an exported arrangement, so "Copy as mod XML" in the settings window produces a ready-to-use def.
    /// Subscribers see it in the Import menu.
    /// </summary>
    public class MainButtonArrangementDef : Def
    {
        public string author;

        /// <summary>Optional: name, the exporting game's buttons and their mods (written by the settings window's export).</summary>
        public ArrangementMeta meta;

        public BarConfig config;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (config == null) yield return "config is missing";
            else if (config.formatVersion > BarConfig.CurrentFormatVersion)
                yield return $"config formatVersion {config.formatVersion} is newer than this version of Main Button Bar System understands ({BarConfig.CurrentFormatVersion})";
        }
    }
}
