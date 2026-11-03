using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Slot "appearance": how the bar and each button are painted (spec 3.8). The built-in vanilla look is a def
    /// of this type. An add-on adds a def whose worker overrides some of the drawing methods.
    /// </summary>
    public class BarAppearanceDef : Def
    {
        public Type workerClass = typeof(BarAppearanceWorker);
        public int displayOrder;

        [Unsaved] private BarAppearanceWorker workerInt;

        public BarAppearanceWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (BarAppearanceWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (workerClass == null || !typeof(BarAppearanceWorker).IsAssignableFrom(workerClass))
                yield return "workerClass must derive from " + nameof(BarAppearanceWorker);
        }
    }

    /// <summary>
    /// The vanilla look. Every method can be overridden; the defaults keep the game's own drawing, so buttons
    /// still go through MainButtonWorker.DoButton and other mods' patches on it keep working.
    /// </summary>
    public class BarAppearanceWorker
    {
        public BarAppearanceDef def;

        /// <summary>Drawn once per event before any button. Vanilla draws nothing behind the buttons.</summary>
        public virtual void DrawBarBackground(Rect barRect)
        {
        }

        public virtual void DrawButton(BarButton button, Rect rect)
        {
            ButtonPainter.Draw(button, rect);
        }

        public virtual string MoreButtonLabel(int drawerCount) => "MBBS_More".Translate(drawerCount);

        /// <summary>Width of the More button: the player's setting, or the label's width when that is 0.</summary>
        public virtual float MoreButtonWidth(BarConfig config, int drawerCount)
        {
            if (config.MoreButtonWidth > 0) return config.MoreButtonWidth;
            var font = Text.Font;
            Text.Font = GameFont.Small;
            float width = Text.CalcSize(MoreButtonLabel(drawerCount)).x;
            Text.Font = font;
            return width + MainButtonBarTuningDef.Current.behaviour.moreButtonLabelPadding;
        }

        /// <summary>Returns true when clicked.</summary>
        public virtual bool DrawMoreButton(Rect rect, int drawerCount, bool drawerOpen)
        {
            Text.Font = GameFont.Small;
            bool clicked = Widgets.ButtonTextSubtle(rect, MoreButtonLabel(drawerCount), 0f, -1f, SoundDefOf.Mouseover_Category, default, null, drawerOpen);
            TooltipHandler.TipRegion(rect, "MBBS_MoreTip".Translate());
            return clicked;
        }

        /// <summary>Returns true when clicked.</summary>
        public virtual bool DrawScrollArrow(Rect rect, bool left) => Widgets.ButtonText(rect, left ? "<" : ">");

        /// <summary>Draws one cell of the "More" drawer.</summary>
        public virtual void DrawDrawerButton(BarButton button, Rect rect)
        {
            ButtonPainter.Draw(button, rect);
        }
    }

    /// <summary>
    /// Draws a button the way the game does. The game's MainButtonWorker.DoButton still runs (so other mods'
    /// patches on it keep working); only the label placement is changed when the player chose an alignment.
    /// </summary>
    public static class ButtonPainter
    {
        /// <summary>Alignment requested for the button being drawn; Default outside this mod's drawing.</summary>
        public static LabelAlign ActiveAlign { get; private set; }

        public static void Draw(BarButton button, Rect rect)
        {
            var align = BarConfigManager.Active.labelAlign;
            if (align == LabelAlign.Default)
            {
                button.Def.Worker.DoButton(rect);
                return;
            }

            ActiveAlign = align;
            try
            {
                button.Def.Worker.DoButton(rect);
            }
            finally
            {
                ActiveAlign = LabelAlign.Default;
            }
        }

        /// <summary>Draws a button label the game handed to Widgets.ButtonTextSubtle, at the chosen alignment.</summary>
        internal static void DrawAlignedLabel(Rect rect, string label, Color? labelColor)
        {
            var anchor = Text.Anchor;
            var color = GUI.color;
            bool wrap = Text.WordWrap;
            try
            {
                Text.Anchor = ActiveAlign switch
                {
                    LabelAlign.Left => TextAnchor.MiddleLeft,
                    LabelAlign.Right => TextAnchor.MiddleRight,
                    _ => TextAnchor.MiddleCenter,
                };
                Text.WordWrap = false;
                if (labelColor.HasValue) GUI.color = labelColor.Value;
                float pad = MainButtonBarTuningDef.Current.behaviour.labelSidePadding;
                Widgets.Label(new Rect(rect.x + pad, rect.y, Mathf.Max(0f, rect.width - pad * 2f), rect.height), label);
            }
            finally
            {
                Text.Anchor = anchor;
                GUI.color = color;
                Text.WordWrap = wrap;
            }
        }
    }
}
