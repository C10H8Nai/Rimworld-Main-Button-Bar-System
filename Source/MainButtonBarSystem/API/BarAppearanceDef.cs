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

        /// <summary>Returns true when clicked.</summary>
        public virtual bool DrawMoreButton(Rect rect, int drawerCount, bool drawerOpen)
        {
            bool clicked = Widgets.ButtonTextSubtle(rect, "MBBS_More".Translate(drawerCount), 0f, -1f, SoundDefOf.Mouseover_Category, default, null, drawerOpen);
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

    /// <summary>Draws a button the way the game does, applying the per-button icon settings.</summary>
    public static class ButtonPainter
    {
        public static void Draw(BarButton button, Rect rect)
        {
            button.Def.Worker.DoButton(rect);
        }
    }
}
