using System.Collections.Generic;
using MainButtonBarSystem.Core;
using UnityEngine;

namespace MainButtonBarSystem
{
    /// <summary>How a theme's bar or drawer background image fills its area (docs/API.md section 4).</summary>
    public enum BackgroundFit
    {
        /// <summary>Nine-slice, like the game's buttons (the default and the pre-1.1 behaviour).</summary>
        Atlas,

        /// <summary>Repeated at its pixel size (times the tile scale) from the top-left corner; the rest is clipped.</summary>
        Tile,

        /// <summary>Scaled to fill the area, ignoring its aspect ratio.</summary>
        Stretch,

        /// <summary>Scaled to fit inside the area keeping its aspect ratio, centred; the rest stays transparent.</summary>
        Fit,

        /// <summary>Scaled to cover the area keeping its aspect ratio, centred; what sticks out is cropped.</summary>
        Cover,
    }

    /// <summary>What a decoration is attached to.</summary>
    public enum DecorationTarget
    {
        Bar,
        Drawer,
    }

    /// <summary>Whether a decoration is painted before the buttons (behind them) or after (over them).</summary>
    public enum DecorationLayer
    {
        BelowButtons,
        AboveButtons,
    }

    /// <summary>
    /// Conditions a decoration can be limited to. Mouseover: the mouse is over the target area (the bar's own rect,
    /// not the parts of a decoration that stick out; the drawer window). DrawerOpen: the "More" drawer is open (never
    /// true for drawer decorations, which are only drawn while it is open anyway). Normal: neither.
    /// </summary>
    public enum DecorationState
    {
        Normal,
        Mouseover,
        DrawerOpen,
    }

    /// <summary>
    /// One image a theme places on the bar or the drawer: a list item of <see cref="BarAppearanceDef.decorations"/>.
    /// Only <see cref="texture"/> is required; everything else has the defaults shown here. Placement follows
    /// <see cref="DecorationLayout.Place"/>; the player's scale, offset and opacity settings are applied on top.
    /// </summary>
    public class BarDecoration
    {
        /// <summary>Texture path relative to Textures/, without the extension. Drawn at its pixel size unless <see cref="size"/> is set.</summary>
        public string texture;

        public DecorationTarget target = DecorationTarget.Bar;

        public DecorationAnchor anchor = DecorationAnchor.Left;

        public DecorationVerticalAnchor verticalAnchor = DecorationVerticalAnchor.Bottom;

        /// <summary>Pixels; x to the right, y downwards (GUI coordinates). Added after anchoring.</summary>
        public Vector2 offset;

        /// <summary>Pixels. (0, 0): the texture's own size. One axis 0: that axis follows the texture's aspect ratio.</summary>
        public Vector2 size;

        /// <summary>0 to 1. Multiplied by the player's decoration opacity setting.</summary>
        public float opacity = 1f;

        public DecorationLayer layer = DecorationLayer.BelowButtons;

        /// <summary>Empty or missing: always shown. Otherwise shown while any listed state holds.</summary>
        public List<DecorationState> showWhen;

        /// <summary>Whether to draw now. <paramref name="drawerOpen"/> is false for drawer decorations.</summary>
        public bool ShowsIn(bool mouseover, bool drawerOpen)
        {
            if (showWhen == null || showWhen.Count == 0) return true;
            foreach (var state in showWhen)
            {
                switch (state)
                {
                    case DecorationState.Normal when !mouseover && !drawerOpen:
                    case DecorationState.Mouseover when mouseover:
                    case DecorationState.DrawerOpen when drawerOpen:
                        return true;
                }
            }
            return false;
        }

        /// <summary>Errors in the XML, reported by the def's ConfigErrors with the list index.</summary>
        public IEnumerable<string> ConfigErrors(int index)
        {
            string prefix = "decorations[" + index + "]: ";
            if (string.IsNullOrEmpty(texture)) yield return prefix + "texture is empty";
            if (opacity < 0f || opacity > 1f) yield return prefix + "opacity must be between 0 and 1";
            if (size.x < 0f || size.y < 0f) yield return prefix + "size must not be negative";
        }
    }

    /// <summary>
    /// A picture drawn inside main buttons, under the theme's button atlas (API 1.1): <see cref="BarAppearanceDef.buttonImage"/>
    /// for every button, <see cref="BarAppearanceDef.buttonImages"/> for named ones. Only <see cref="texture"/> (and, in
    /// the list, <see cref="button"/>) is required; everything else has the defaults shown here.
    /// </summary>
    public class BarButtonImage
    {
        /// <summary>defName of the MainButtonDef (Architect, Work, a mod's button...). Required in buttonImages; not used in buttonImage.</summary>
        public string button;

        /// <summary>Texture path relative to Textures/, without the extension. Any size.</summary>
        public string texture;

        /// <summary>Cover (the default) fills the button keeping the aspect ratio and crops; Fit shows the whole image; Stretch distorts. Atlas and Tile are drawn as Stretch.</summary>
        public BackgroundFit fit = BackgroundFit.Cover;

        /// <summary>Which side Cover keeps when cropping, and where Fit places the image.</summary>
        public DecorationAnchor anchor = DecorationAnchor.Center;

        public DecorationVerticalAnchor verticalAnchor = DecorationVerticalAnchor.Center;

        /// <summary>0 to 1.</summary>
        public float opacity = 1f;

        /// <summary>Pixels kept free inside the button's edge on every side (for example to stay inside the frame).</summary>
        public float inset;

        /// <summary>The fit actually drawn: Atlas and Tile make no sense inside one button and become Stretch.</summary>
        public BackgroundFit EffectiveFit => fit == BackgroundFit.Cover || fit == BackgroundFit.Fit ? fit : BackgroundFit.Stretch;

        /// <summary>Errors in the XML. <paramref name="index"/> is the position in buttonImages, or -1 for buttonImage.</summary>
        public IEnumerable<string> ConfigErrors(int index)
        {
            string prefix = index < 0 ? "buttonImage: " : "buttonImages[" + index + "]: ";
            if (string.IsNullOrEmpty(texture)) yield return prefix + "texture is empty";
            if (index >= 0 && string.IsNullOrEmpty(button)) yield return prefix + "button is empty (the defName of a MainButtonDef)";
            if (index < 0 && !string.IsNullOrEmpty(button)) yield return prefix + "button is only read in buttonImages; buttonImage applies to every button";
            if (opacity < 0f || opacity > 1f) yield return prefix + "opacity must be between 0 and 1";
            if (inset < 0f) yield return prefix + "inset must not be negative";
        }
    }
}
