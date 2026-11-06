using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// The game's colour picker (design spec 2026-11-05 §4.10) for a custom look's label colour: colour wheel,
    /// palette (<c>settingsWindow.lookColorPalette</c> in Tuning.xml) and RGB / HSV boxes. "Default" in it is the colour
    /// the layer below gives now.
    /// </summary>
    public class Dialog_LookColorPicker : Dialog_ColorPickerBase
    {
        private readonly Color fallback;
        private readonly Action<Color> onAccept;

        public Dialog_LookColorPicker(Color current, Color fallback, Action<Color> onAccept)
            : base(Widgets.ColorComponents.All, Widgets.ColorComponents.All)
        {
            color = current;
            oldColor = current;
            this.fallback = fallback;
            this.onAccept = onAccept;
            forcePause = false;
        }

        protected override bool ShowDarklight => false;

        protected override Color DefaultColor => fallback;

        protected override List<Color> PickableColors => MainButtonBarTuningDef.Current.settingsWindow.lookColorPalette ?? new List<Color>();

        /// <summary>Negative: the value (brightness) is not forced.</summary>
        protected override float ForcedColorValue => -1f;

        protected override bool ShowColorTemperatureBar => false;

        protected override void SaveColor(Color picked) => onAccept(new Color(picked.r, picked.g, picked.b, 1f));
    }
}
