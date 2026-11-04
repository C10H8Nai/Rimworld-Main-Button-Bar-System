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
}
