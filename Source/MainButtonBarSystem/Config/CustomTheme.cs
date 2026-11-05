using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MainButtonBarSystem.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>Which half of the Appearance tab is in effect (design spec 2026-11-05).</summary>
    public enum AppearanceMode
    {
        /// <summary>A pre-designed theme as it is.</summary>
        Preset,

        /// <summary>The chosen theme with the player's custom look on top.</summary>
        Custom,
    }

    /// <summary>Label font of a custom look; Default keeps the game's.</summary>
    public enum LookFont
    {
        Default,
        Tiny,
        Small,
        Medium,
    }

    /// <summary>
    /// One layer of a custom look: "all buttons" (<see cref="defName"/> null) or one button. Every field has a "not
    /// changed" value and then follows the layer below: this button → all buttons → the chosen theme → the game.
    /// </summary>
    public class ButtonLook : IExposable
    {
        public static readonly Color UnsetColor = new Color(0f, 0f, 0f, -1f);

        public string defName;

        public LabelAlign align;
        public LookFont font;
        public Color labelColor = UnsetColor;
        public Color labelMouseoverColor = UnsetColor;
        public Color labelActiveColor = UnsetColor;

        /// <summary>Pixels; -1 not changed.</summary>
        public int iconSize = BarConfig.Unset;

        public IconAnchor iconAnchor;

        /// <summary>Pixels, may be negative; <see cref="BarConfig.UnsetOffset"/> not changed.</summary>
        public int iconOffsetX = BarConfig.UnsetOffset;
        public int iconOffsetY = BarConfig.UnsetOffset;

        /// <summary>Button backgrounds per state, as <see cref="CustomThemes.AtlasRef"/>; null not changed.</summary>
        public string atlas;
        public string atlasMouseover;
        public string atlasActive;
        public string atlasDisabled;

        public static bool IsSet(Color c) => c.a >= 0f;

        public bool IsEmpty =>
            align == LabelAlign.Default && font == LookFont.Default && !IsSet(labelColor) && !IsSet(labelMouseoverColor) && !IsSet(labelActiveColor) &&
            iconSize < 0 && iconAnchor == IconAnchor.Default && iconOffsetX == BarConfig.UnsetOffset && iconOffsetY == BarConfig.UnsetOffset &&
            atlas.NullOrEmpty() && atlasMouseover.NullOrEmpty() && atlasActive.NullOrEmpty() && atlasDisabled.NullOrEmpty();

        public bool HasIconLook => iconSize >= 0 || iconAnchor != IconAnchor.Default || iconOffsetX != BarConfig.UnsetOffset || iconOffsetY != BarConfig.UnsetOffset;

        public ButtonLook Clone() => (ButtonLook)MemberwiseClone();

        public string AtlasOf(ButtonVisualState state) => state switch
        {
            ButtonVisualState.Mouseover => atlasMouseover,
            ButtonVisualState.Active => atlasActive,
            ButtonVisualState.Disabled => atlasDisabled,
            _ => atlas,
        };

        public void SetAtlas(ButtonVisualState state, string value)
        {
            value = value.NullOrEmpty() ? null : value;
            switch (state)
            {
                case ButtonVisualState.Mouseover: atlasMouseover = value; break;
                case ButtonVisualState.Active: atlasActive = value; break;
                case ButtonVisualState.Disabled: atlasDisabled = value; break;
                default: atlas = value; break;
            }
        }

        /// <summary>Label colour per state: 0 normal, 1 mouse-over, 2 open.</summary>
        public Color ColorOf(int which) => which switch
        {
            1 => labelMouseoverColor,
            2 => labelActiveColor,
            _ => labelColor,
        };

        public void SetColor(int which, Color value)
        {
            switch (which)
            {
                case 1: labelMouseoverColor = value; break;
                case 2: labelActiveColor = value; break;
                default: labelColor = value; break;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref align, "align", LabelAlign.Default);
            Scribe_Values.Look(ref font, "font", LookFont.Default);
            Scribe_Values.Look(ref labelColor, "labelColor", UnsetColor);
            Scribe_Values.Look(ref labelMouseoverColor, "labelMouseoverColor", UnsetColor);
            Scribe_Values.Look(ref labelActiveColor, "labelActiveColor", UnsetColor);
            Scribe_Values.Look(ref iconSize, "iconSize", BarConfig.Unset);
            Scribe_Values.Look(ref iconAnchor, "iconAnchor", IconAnchor.Default);
            Scribe_Values.Look(ref iconOffsetX, "iconOffsetX", BarConfig.UnsetOffset);
            Scribe_Values.Look(ref iconOffsetY, "iconOffsetY", BarConfig.UnsetOffset);
            Scribe_Values.Look(ref atlas, "atlas");
            Scribe_Values.Look(ref atlasMouseover, "atlasMouseover");
            Scribe_Values.Look(ref atlasActive, "atlasActive");
            Scribe_Values.Look(ref atlasDisabled, "atlasDisabled");
        }
    }

    /// <summary>The player's custom look: an "all buttons" layer and per-button layers, on top of the chosen theme.</summary>
    public class CustomThemeData : IExposable
    {
        public ButtonLook all = new ButtonLook();
        public List<ButtonLook> buttons = new List<ButtonLook>();

        [Unsaved] private Dictionary<string, ButtonLook> index;

        /// <summary>The layer of one button, or "all buttons" for null; null when the button has none.</summary>
        public ButtonLook Layer(string defName)
        {
            if (defName == null) return all;
            EnsureIndex();
            return index.TryGetValue(defName, out var look) ? look : null;
        }

        public ButtonLook GetOrCreate(string defName)
        {
            if (defName == null) return all;
            EnsureIndex();
            if (!index.TryGetValue(defName, out var look))
            {
                look = new ButtonLook { defName = defName };
                buttons.Add(look);
                index[defName] = look;
            }
            return look;
        }

        /// <summary>Back to following the layer below: one button, or "all buttons" for null.</summary>
        public void Reset(string defName)
        {
            if (defName == null)
            {
                all = new ButtonLook();
                return;
            }
            EnsureIndex();
            if (index.TryGetValue(defName, out var look))
            {
                buttons.Remove(look);
                index.Remove(defName);
            }
        }

        public void Prune()
        {
            if (buttons.RemoveAll(b => b == null || b.defName.NullOrEmpty() || b.IsEmpty) > 0) index = null;
        }

        public bool IsEmpty
        {
            get
            {
                if (!all.IsEmpty) return false;
                foreach (var b in buttons)
                    if (b != null && !b.IsEmpty) return false;
                return true;
            }
        }

        public CustomThemeData Clone()
        {
            var copy = new CustomThemeData { all = all.Clone() };
            foreach (var b in buttons)
                if (b != null) copy.buttons.Add(b.Clone());
            return copy;
        }

        private void EnsureIndex()
        {
            if (index != null) return;
            index = new Dictionary<string, ButtonLook>();
            foreach (var b in buttons)
                if (b?.defName != null) index[b.defName] = b;
        }

        // ------------------------------------------------ resolved values (this button, then all buttons); no allocation

        public LabelAlign AlignFor(string defName)
        {
            var b = Layer(defName);
            return b != null && b.align != LabelAlign.Default ? b.align : all.align;
        }

        public LookFont FontFor(string defName)
        {
            var b = Layer(defName);
            return b != null && b.font != LookFont.Default ? b.font : all.font;
        }

        /// <summary>
        /// Label colour for a state (design spec 4.5): this button's colour for the state, else all buttons', else the
        /// theme's own for that state; only then the next state down (open → mouse-over → normal). Null keeps the game's.
        /// </summary>
        public Color? LabelColorFor(string defName, ButtonVisualState state, BarAppearanceDef theme)
        {
            var b = Layer(defName);
            if (state == ButtonVisualState.Active && Exact(b, 2, theme, out var c)) return c;
            if ((state == ButtonVisualState.Active || state == ButtonVisualState.Mouseover) && Exact(b, 1, theme, out c)) return c;
            return Exact(b, 0, theme, out c) ? c : (Color?)null;
        }

        private bool Exact(ButtonLook b, int which, BarAppearanceDef theme, out Color color)
        {
            if (b != null && ButtonLook.IsSet(b.ColorOf(which))) color = b.ColorOf(which);
            else if (ButtonLook.IsSet(all.ColorOf(which))) color = all.ColorOf(which);
            else
            {
                color = theme == null ? default : which switch
                {
                    2 => theme.labelActiveColor,
                    1 => theme.labelMouseoverColor,
                    _ => theme.labelColor,
                };
                return color.a > 0f;
            }
            return true;
        }

        /// <summary>True when any icon field is changed for this button (or all buttons); unchanged fields come back as the game's.</summary>
        public bool IconFor(string defName, int defaultSize, out int size, out IconAnchor anchor, out Vector2 offset)
        {
            var b = Layer(defName);
            bool any = all.HasIconLook || (b != null && b.HasIconLook);
            size = b != null && b.iconSize >= 0 ? b.iconSize : all.iconSize >= 0 ? all.iconSize : defaultSize;
            anchor = b != null && b.iconAnchor != IconAnchor.Default ? b.iconAnchor : all.iconAnchor;
            int x = b != null && b.iconOffsetX != BarConfig.UnsetOffset ? b.iconOffsetX : all.iconOffsetX;
            int y = b != null && b.iconOffsetY != BarConfig.UnsetOffset ? b.iconOffsetY : all.iconOffsetY;
            offset = new Vector2(x == BarConfig.UnsetOffset ? 0 : x, y == BarConfig.UnsetOffset ? 0 : y);
            return any;
        }

        /// <summary>
        /// The background for a state (design spec 4.5): this button's for the state, else all buttons', else the
        /// theme's own for that state; only then the next state down (open → mouse-over → normal, disabled → normal).
        /// A texture found for a state other than normal replaces the game's tint, like themes do. Null keeps the game's.
        /// </summary>
        public Texture2D AtlasFor(string defName, ButtonVisualState state, BarAppearanceDef theme, out bool replacesTint)
        {
            var b = Layer(defName);
            var s = state;
            while (true)
            {
                var tex = ExactAtlas(b, s, theme);
                if (tex != null)
                {
                    replacesTint = s != ButtonVisualState.Normal;
                    return tex;
                }
                if (s == ButtonVisualState.Normal)
                {
                    replacesTint = false;
                    return null;
                }
                s = s == ButtonVisualState.Active ? ButtonVisualState.Mouseover : ButtonVisualState.Normal;
            }
        }

        private Texture2D ExactAtlas(ButtonLook b, ButtonVisualState state, BarAppearanceDef theme)
        {
            string r = b?.AtlasOf(state);
            if (r.NullOrEmpty()) r = all.AtlasOf(state);
            if (!r.NullOrEmpty()) return CustomThemes.AtlasTexture(r);
            if (theme == null) return null;
            return theme.Tex(state switch
            {
                ButtonVisualState.Mouseover => theme.buttonMouseover,
                ButtonVisualState.Active => theme.buttonActive,
                ButtonVisualState.Disabled => theme.buttonDisabled,
                _ => theme.button,
            });
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref all, "all");
            Scribe_Collections.Look(ref buttons, "buttons", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                all ??= new ButtonLook();
                buttons ??= new List<ButtonLook>();
                buttons.RemoveAll(b => b == null || b.defName.NullOrEmpty());
                index = null;
            }
        }
    }

    /// <summary>Workers and texture lookups for custom looks.</summary>
    public static class CustomThemes
    {
        private static readonly ConditionalWeakTable<CustomThemeData, BarAppearanceWorker_Custom> workers = new ConditionalWeakTable<CustomThemeData, BarAppearanceWorker_Custom>();
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        /// <summary>The worker drawing <paramref name="data"/> over <paramref name="baseDef"/>; kept per data object.</summary>
        public static BarAppearanceWorker WorkerFor(CustomThemeData data, BarAppearanceDef baseDef)
        {
            if (data == null || baseDef == null) return baseDef?.Worker;
            if (!workers.TryGetValue(data, out var worker) || worker.def != baseDef)
            {
                worker = new BarAppearanceWorker_Custom(baseDef, data);
                workers.AddOrUpdate(data, worker);
            }
            return worker;
        }

        /// <summary>The theme fields a custom look can take a button background from.</summary>
        public static readonly string[] AtlasFields = { "button", "buttonMouseover", "buttonActive", "buttonDisabled" };

        /// <summary>Reference to one theme's background for one state: "defName|field".</summary>
        public static string AtlasRef(BarAppearanceDef theme, string field) => theme.defName + "|" + field;

        public static Texture2D AtlasTexture(string reference)
        {
            if (textures.TryGetValue(reference, out var tex)) return tex;
            tex = null;
            int bar = reference.IndexOf('|');
            if (bar > 0)
            {
                var theme = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(reference.Substring(0, bar));
                tex = theme == null ? null : theme.Tex(FieldValue(theme, reference.Substring(bar + 1)));
            }
            textures[reference] = tex;
            return tex;
        }

        public static string FieldValue(BarAppearanceDef theme, string field) => field switch
        {
            "buttonMouseover" => theme.buttonMouseover,
            "buttonActive" => theme.buttonActive,
            "buttonDisabled" => theme.buttonDisabled,
            "button" => theme.button,
            _ => null,
        };

        /// <summary>Themes changed (reload, play data): look textures up again.</summary>
        internal static void ResetCache() => textures.Clear();
    }

    /// <summary>
    /// Draws the chosen theme with a custom look on top: button backgrounds, label colours, alignment, font and icon
    /// placement per button. Everything else (bar and drawer backgrounds, More, arrows, decorations) is the theme's.
    /// Uses the default drawing for the theme's def, so a theme's own worker class effects do not apply here.
    /// </summary>
    public sealed class BarAppearanceWorker_Custom : BarAppearanceWorker
    {
        public readonly CustomThemeData data;

        public BarAppearanceWorker_Custom(BarAppearanceDef baseDef, CustomThemeData data)
        {
            def = baseDef;
            this.data = data;
        }

        public override bool ChangesButtons => true;

        public override Texture2D ButtonAtlas(ButtonVisualState state, out bool replacesTint)
        {
            var button = ButtonPainter.ActiveButton;
            if (button == null) return base.ButtonAtlas(state, out replacesTint);
            return data.AtlasFor(button.defName, state, def, out replacesTint);
        }

        public override Color? LabelColor(ButtonVisualState state)
        {
            var button = ButtonPainter.ActiveButton;
            return button != null ? data.LabelColorFor(button.defName, state, def) : base.LabelColor(state);
        }

        public override LabelAlign LabelAlignFor(MainButtonDef button) => data.AlignFor(button?.defName);

        public override GameFont? LabelFontFor(MainButtonDef button) => data.FontFor(button?.defName) switch
        {
            LookFont.Tiny => GameFont.Tiny,
            LookFont.Small => GameFont.Small,
            LookFont.Medium => GameFont.Medium,
            _ => (GameFont?)null,
        };

        public override bool IconLookFor(MainButtonDef button, out float size, out IconAnchor anchor, out Vector2 offset)
        {
            bool any = data.IconFor(button?.defName, Mathf.RoundToInt(MainButtonBarTuningDef.Current.behaviour.iconSize), out int s, out anchor, out offset);
            size = s;
            return any;
        }
    }
}
