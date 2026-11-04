using System.Collections.Generic;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// An icon pack: textures a mod ships in its own Textures folder, offered in the icon picker's built-in list
    /// (XML only). Players' own PNGs go to the icons folder outside every mod folder instead, so Steam updates never
    /// delete them.
    /// <code>
    /// &lt;MainButtonBarSystem.MainButtonIconSetDef&gt;
    ///   &lt;defName&gt;MyIcons&lt;/defName&gt;
    ///   &lt;label&gt;my icons&lt;/label&gt;
    ///   &lt;icons&gt;
    ///     &lt;li&gt;MyIcons/Factions&lt;/li&gt;
    ///   &lt;/icons&gt;
    /// &lt;/MainButtonBarSystem.MainButtonIconSetDef&gt;
    /// </code>
    /// </summary>
    public class MainButtonIconSetDef : Def
    {
        /// <summary>Texture paths relative to Textures/, without the extension.</summary>
        public List<string> icons = new List<string>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (icons.NullOrEmpty()) yield return "icons is empty";
        }
    }
}
