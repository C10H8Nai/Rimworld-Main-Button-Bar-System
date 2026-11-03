using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Slot "appearance" (a theme): how the bar and each button are painted (spec 3.8). A theme needs only XML and
    /// images: every texture and colour below is optional and falls back to the game's look. Authors who want
    /// effects can still set <see cref="workerClass"/>.
    /// <para>Button textures are nine-slice atlases, drawn like the game's <c>Widgets.ButtonSubtleAtlas</c>.</para>
    /// </summary>
    public class BarAppearanceDef : Def
    {
        public Type workerClass = typeof(BarAppearanceWorker);
        public int displayOrder;

        /// <summary>Shown in the Appearance tab, e.g. the theme mod's author.</summary>
        public string author;

        /// <summary>Atlas behind the whole bar. Empty: nothing (vanilla).</summary>
        public string barBackground;

        /// <summary>Button atlas in its normal state. Empty: the game's subtle button atlas.</summary>
        public string button;

        /// <summary>Button atlas under the mouse. Empty: the normal atlas with the game's mouse-over tint.</summary>
        public string buttonMouseover;

        /// <summary>Button atlas while pressed, or while its tab is open. Empty: as normal / mouse-over.</summary>
        public string buttonActive;

        /// <summary>Button atlas while the button is disabled (no map, etc.). Empty: the normal atlas.</summary>
        public string buttonDisabled;

        /// <summary>Label colour. Fully transparent (the default) keeps the game's white.</summary>
        public Color labelColor;

        /// <summary>Label colour under the mouse. Fully transparent uses <see cref="labelColor"/>.</summary>
        public Color labelMouseoverColor;

        /// <summary>Atlas of the "More" button. Empty: drawn like the other buttons.</summary>
        public string moreButton;

        /// <summary>Icons on the scroll arrows. Empty: the characters &lt; and &gt;.</summary>
        public string arrowLeft;
        public string arrowRight;

        /// <summary>Atlas behind the "More" drawer. Empty: the game's window background.</summary>
        public string drawerBackground;

        /// <summary>Image in the Appearance tab. Empty: a preview drawn from the textures above.</summary>
        public string preview;

        [Unsaved] private BarAppearanceWorker workerInt;
        [Unsaved] private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

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

        /// <summary>Loads a texture once; null when the path is empty or missing (a missing one is reported once).</summary>
        public Texture2D Tex(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!textures.TryGetValue(path, out var tex))
            {
                tex = ContentFinder<Texture2D>.Get(path, false);
                if (tex == null)
                    Log.WarningOnce($"{MainButtonBarSystemMod.LogPrefix}Theme {defName}: texture '{path}' not found; using the game's look for it.", (defName + path).GetHashCode());
                textures[path] = tex;
            }
            return tex;
        }

        public bool HasLabelColor => labelColor.a > 0f;

        /// <summary>True when the theme replaces anything inside the buttons themselves.</summary>
        public bool ChangesButtons =>
            !string.IsNullOrEmpty(button) || !string.IsNullOrEmpty(buttonMouseover) || !string.IsNullOrEmpty(buttonActive) ||
            !string.IsNullOrEmpty(buttonDisabled) || HasLabelColor || labelMouseoverColor.a > 0f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (workerClass == null || !typeof(BarAppearanceWorker).IsAssignableFrom(workerClass))
                yield return "workerClass must derive from " + nameof(BarAppearanceWorker);
        }
    }

    public enum ButtonVisualState
    {
        Normal,
        Mouseover,
        Active,
        Disabled,
    }

    /// <summary>
    /// Paints a theme from its def's fields. Every method can be overridden. Buttons still go through the
    /// game's MainButtonWorker.DoButton (so other mods' patches on it keep working); the theme only swaps the
    /// background atlas and label colour the game uses while this mod draws.
    /// </summary>
    public class BarAppearanceWorker
    {
        public BarAppearanceDef def;

        /// <summary>Drawn once per event before any button.</summary>
        public virtual void DrawBarBackground(Rect barRect)
        {
            var tex = def.Tex(def.barBackground);
            if (tex != null) Widgets.DrawAtlas(barRect, tex);
        }

        public virtual void DrawButton(BarButton button, Rect rect)
        {
            ButtonPainter.Draw(button, rect, this);
        }

        /// <summary>Draws one cell of the "More" drawer.</summary>
        public virtual void DrawDrawerButton(BarButton button, Rect rect)
        {
            ButtonPainter.Draw(button, rect, this);
        }

        /// <summary>Background of the drawer window, covering the whole window.</summary>
        public virtual void DrawDrawerBackground(Rect windowRect)
        {
            var tex = def.Tex(def.drawerBackground);
            if (tex != null) Widgets.DrawAtlas(windowRect, tex);
            else Widgets.DrawWindowBackground(windowRect);
        }

        /// <summary>
        /// The atlas to use instead of the game's subtle button atlas, or null to keep it. <paramref name="replacesTint"/>
        /// is true when the texture is made for that state, so the game's mouse-over tint should not be added.
        /// </summary>
        public virtual Texture2D ButtonAtlas(ButtonVisualState state, out bool replacesTint)
        {
            replacesTint = false;
            Texture2D tex = null;
            switch (state)
            {
                case ButtonVisualState.Disabled: tex = def.Tex(def.buttonDisabled); break;
                case ButtonVisualState.Active: tex = def.Tex(def.buttonActive); break;
                case ButtonVisualState.Mouseover: tex = def.Tex(def.buttonMouseover); break;
            }
            if (tex != null)
            {
                replacesTint = true;
                return tex;
            }
            if (state == ButtonVisualState.Active)
            {
                tex = def.Tex(def.buttonMouseover);
                if (tex != null)
                {
                    replacesTint = true;
                    return tex;
                }
            }
            return def.Tex(def.button);
        }

        /// <summary>Label colour for a state, or null to keep the game's.</summary>
        public virtual Color? LabelColor(ButtonVisualState state)
        {
            if (state == ButtonVisualState.Mouseover || state == ButtonVisualState.Active)
            {
                if (def.labelMouseoverColor.a > 0f) return def.labelMouseoverColor;
            }
            return def.HasLabelColor ? def.labelColor : (Color?)null;
        }

        public virtual string MoreButtonLabel(int drawerCount) => "MBBS_More".Translate(drawerCount);

        /// <summary>Width of the More button: the player's setting, or the label's width when that is 0.</summary>
        public virtual float MoreButtonWidth(BarConfig config, int drawerCount)
        {
            if (config.MoreButtonWidth > 0) return config.MoreButtonWidth;
            var font = Text.Font;
            Text.Font = MainButtonBarTuningDef.Current.behaviour.labelFont;
            float width = Text.CalcSize(MoreButtonLabel(drawerCount)).x;
            Text.Font = font;
            return width + MainButtonBarTuningDef.Current.behaviour.moreButtonLabelPadding;
        }

        /// <summary>Returns true when clicked.</summary>
        public virtual bool DrawMoreButton(Rect rect, int drawerCount, bool drawerOpen)
        {
            bool clicked;
            var more = def.Tex(def.moreButton);
            if (more != null)
            {
                var state = drawerOpen ? ButtonVisualState.Active : Mouse.IsOver(rect) ? ButtonVisualState.Mouseover : ButtonVisualState.Normal;
                Widgets.DrawAtlas(rect, more);
                if (state != ButtonVisualState.Normal) Widgets.DrawHighlight(rect);
                ButtonPainter.DrawLabel(rect, MoreButtonLabel(drawerCount), LabelColor(state) ?? Color.white, TextAnchor.MiddleCenter, state != ButtonVisualState.Normal);
                MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Category);
                clicked = Widgets.ButtonInvisible(rect, false);
            }
            else
            {
                clicked = ButtonPainter.ThemedSubtleButton(this, rect, MoreButtonLabel(drawerCount), drawerOpen);
            }
            TooltipHandler.TipRegion(rect, "MBBS_MoreTip".Translate());
            return clicked;
        }

        /// <summary>Returns true when clicked.</summary>
        public virtual bool DrawScrollArrow(Rect rect, bool left)
        {
            var icon = def.Tex(left ? def.arrowLeft : def.arrowRight);
            if (icon == null) return ButtonPainter.ThemedSubtleButton(this, rect, left ? "<" : ">", false, TextAnchor.MiddleCenter);

            bool clicked = ButtonPainter.ThemedSubtleButton(this, rect, "", false);
            float size = Mathf.Min(rect.width, rect.height);
            Widgets.DrawTextureFitted(new Rect(rect.center.x - size / 2f, rect.center.y - size / 2f, size, size), icon, 1f);
            return clicked;
        }
    }

    /// <summary>
    /// Draws a button the way the game does, through MainButtonWorker.DoButton, while the active theme and label
    /// alignment are applied by patches on Widgets.DrawAtlas and Widgets.ButtonTextSubtle. Nothing changes for any
    /// widget the game draws outside these calls.
    /// </summary>
    public static class ButtonPainter
    {
        /// <summary>Alignment requested for the button being drawn; Default outside this mod's drawing.</summary>
        public static LabelAlign ActiveAlign { get; private set; }

        /// <summary>Theme applied to the button being drawn; null outside this mod's drawing.</summary>
        public static BarAppearanceWorker ActiveTheme { get; private set; }

        /// <summary>Whether the button being drawn is pressed, open or disabled; mouse-over is decided per rect.</summary>
        public static ButtonVisualState ActiveBaseState { get; private set; }

        private static MainButtonBarTuningDef.Behaviour B => MainButtonBarTuningDef.Current.behaviour;

        public static void Draw(BarButton button, Rect rect, BarAppearanceWorker theme)
        {
            var def = button.Def;
            var align = BarConfigManager.Active.labelAlign;
            bool themed = theme != null && theme.def.ChangesButtons;
            if (align == LabelAlign.Default && !themed)
            {
                def.Worker.DoButton(rect);
                return;
            }

            ActiveAlign = align;
            ActiveTheme = themed ? theme : null;
            ActiveBaseState = BaseState(def, rect);
            try
            {
                def.Worker.DoButton(rect);
            }
            finally
            {
                ActiveAlign = LabelAlign.Default;
                ActiveTheme = null;
                ActiveBaseState = ButtonVisualState.Normal;
            }
        }

        /// <summary>A subtle button (More, arrows) drawn with the theme's atlas and label colour.</summary>
        internal static bool ThemedSubtleButton(BarAppearanceWorker theme, Rect rect, string label, bool active, TextAnchor? anchor = null)
        {
            bool themed = theme != null && theme.def.ChangesButtons;
            var previousTheme = ActiveTheme;
            var previousState = ActiveBaseState;
            var previousAlign = ActiveAlign;
            ActiveTheme = themed ? theme : null;
            ActiveBaseState = active ? ButtonVisualState.Active : ButtonVisualState.Normal;
            if (anchor == TextAnchor.MiddleCenter) ActiveAlign = LabelAlign.Center;
            try
            {
                return Widgets.ButtonTextSubtle(rect, label, 0f, -1f, SoundDefOf.Mouseover_Category, default, null, active);
            }
            finally
            {
                ActiveTheme = previousTheme;
                ActiveBaseState = previousState;
                ActiveAlign = previousAlign;
            }
        }

        private static ButtonVisualState BaseState(MainButtonDef def, Rect rect)
        {
            try
            {
                if (def.Worker.Disabled) return ButtonVisualState.Disabled;
                if (Find.MainTabsRoot?.OpenTab == def) return ButtonVisualState.Active;
                if (Input.GetMouseButton(0) && Mouse.IsOver(rect)) return ButtonVisualState.Active;
            }
            catch
            {
                // Workers that need a game or map: treat as normal.
            }
            return ButtonVisualState.Normal;
        }

        /// <summary>The state for a rect being drawn now: the base state, or mouse-over.</summary>
        internal static ButtonVisualState StateFor(Rect rect) =>
            ActiveBaseState != ButtonVisualState.Normal ? ActiveBaseState : Mouse.IsOver(rect) ? ButtonVisualState.Mouseover : ButtonVisualState.Normal;

        /// <summary>Draws a button label at the active alignment, with the game's 2px "pressed" shift under the mouse.</summary>
        internal static void DrawAlignedLabel(Rect rect, string label, Color? labelColor)
        {
            var anchor = ActiveAlign switch
            {
                LabelAlign.Left => TextAnchor.MiddleLeft,
                LabelAlign.Right => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleCenter,
            };
            DrawLabel(rect, label, labelColor ?? Color.white, anchor, Mouse.IsOver(rect));
        }

        internal static void DrawLabel(Rect rect, string label, Color color, TextAnchor anchor, bool shifted)
        {
            var oldAnchor = Text.Anchor;
            var oldColor = GUI.color;
            var oldFont = Text.Font;
            bool oldWrap = Text.WordWrap;
            try
            {
                Text.Anchor = anchor;
                Text.WordWrap = false;
                Text.Font = B.labelFont;
                GUI.color = color;
                float pad = B.labelSidePadding;
                var r = new Rect(rect.x + pad, rect.y, Mathf.Max(0f, rect.width - pad * 2f), rect.height);
                if (shifted) r.position += B.hoverLabelOffset;
                Widgets.Label(r, label);
            }
            finally
            {
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.WordWrap = oldWrap;
            }
        }
    }
}
