using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    /// <summary>Label font of a custom look; Default keeps the game's, Custom is the normal font at <see cref="ButtonLook.fontSize"/>.</summary>
    public enum LookFont
    {
        Default,
        Tiny,
        Small,
        Medium,
        Custom,
    }

    /// <summary>How a custom look's background picture fills the button; Default follows the layer below.</summary>
    public enum LookImageFit
    {
        Default,
        Cover,
        Fit,
        Stretch,
    }

    public enum LookImageAnchor
    {
        Default,
        Left,
        Center,
        Right,
    }

    public enum LookImageVerticalAnchor
    {
        Default,
        Top,
        Center,
        Bottom,
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

        /// <summary>Pixels when <see cref="font"/> is Custom; -1 the tuning def's default size.</summary>
        public int fontSize = BarConfig.Unset;

        public Color labelColor = UnsetColor;
        public Color labelMouseoverColor = UnsetColor;
        public Color labelActiveColor = UnsetColor;

        /// <summary>Pixels; -1 not changed.</summary>
        public int iconSize = BarConfig.Unset;

        public IconAnchor iconAnchor;

        /// <summary>Pixels, may be negative; <see cref="BarConfig.UnsetOffset"/> not changed.</summary>
        public int iconOffsetX = BarConfig.UnsetOffset;
        public int iconOffsetY = BarConfig.UnsetOffset;

        /// <summary>Button backgrounds per state, as image references (<see cref="ImageRef"/>); null not changed.</summary>
        public string atlas;
        public string atlasMouseover;
        public string atlasActive;
        public string atlasDisabled;

        /// <summary>Background picture inside the button, under its frame, as an image reference; null not changed.</summary>
        public string image;

        public LookImageFit imageFit;
        public LookImageAnchor imageAnchor;
        public LookImageVerticalAnchor imageVerticalAnchor;

        /// <summary>Percent; -1 not changed.</summary>
        public int imageOpacity = BarConfig.Unset;

        /// <summary>Pixels kept free inside the button's edge; -1 not changed.</summary>
        public int imageInset = BarConfig.Unset;

        public static bool IsSet(Color c) => c.a >= 0f;

        public bool HasAtlas => !atlas.NullOrEmpty() || !atlasMouseover.NullOrEmpty() || !atlasActive.NullOrEmpty() || !atlasDisabled.NullOrEmpty();

        public bool HasColor => IsSet(labelColor) || IsSet(labelMouseoverColor) || IsSet(labelActiveColor);

        public bool HasImageLook =>
            !image.NullOrEmpty() || imageFit != LookImageFit.Default || imageAnchor != LookImageAnchor.Default ||
            imageVerticalAnchor != LookImageVerticalAnchor.Default || imageOpacity >= 0 || imageInset >= 0;

        public bool IsEmpty =>
            align == LabelAlign.Default && font == LookFont.Default && !HasColor && !HasIconLook && !HasAtlas && !HasImageLook;

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

        /// <summary>Every image reference this layer stores (backgrounds and the picture), empty ones left out.</summary>
        public IEnumerable<string> ImageRefs()
        {
            if (!atlas.NullOrEmpty()) yield return atlas;
            if (!atlasMouseover.NullOrEmpty()) yield return atlasMouseover;
            if (!atlasActive.NullOrEmpty()) yield return atlasActive;
            if (!atlasDisabled.NullOrEmpty()) yield return atlasDisabled;
            if (!image.NullOrEmpty()) yield return image;
        }

        /// <summary>Replaces every image reference through <paramref name="map"/> (renamed imported PNGs).</summary>
        public void MapImageRefs(Func<string, string> map)
        {
            if (!atlas.NullOrEmpty()) atlas = map(atlas);
            if (!atlasMouseover.NullOrEmpty()) atlasMouseover = map(atlasMouseover);
            if (!atlasActive.NullOrEmpty()) atlasActive = map(atlasActive);
            if (!atlasDisabled.NullOrEmpty()) atlasDisabled = map(atlasDisabled);
            if (!image.NullOrEmpty()) image = map(image);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref align, "align", LabelAlign.Default);
            Scribe_Values.Look(ref font, "font", LookFont.Default);
            Scribe_Values.Look(ref fontSize, "fontSize", BarConfig.Unset);
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
            Scribe_Values.Look(ref image, "image");
            Scribe_Values.Look(ref imageFit, "imageFit", LookImageFit.Default);
            Scribe_Values.Look(ref imageAnchor, "imageAnchor", LookImageAnchor.Default);
            Scribe_Values.Look(ref imageVerticalAnchor, "imageVerticalAnchor", LookImageVerticalAnchor.Default);
            Scribe_Values.Look(ref imageOpacity, "imageOpacity", BarConfig.Unset);
            Scribe_Values.Look(ref imageInset, "imageInset", BarConfig.Unset);
        }
    }

    /// <summary>A button picture a custom look resolved, with its texture (an imported PNG has no texture path).</summary>
    public sealed class ResolvedButtonImage : BarButtonImage
    {
        public Texture2D Texture;
    }

    /// <summary>The player's custom look: an "all buttons" layer and per-button layers, on top of the chosen theme.</summary>
    public class CustomThemeData : IExposable
    {
        public ButtonLook all = new ButtonLook();
        public List<ButtonLook> buttons = new List<ButtonLook>();

        [Unsaved] private Dictionary<string, ButtonLook> index;

        /// <summary>Raised on every change, so cached results (button pictures) are rebuilt; see <see cref="Touch"/>.</summary>
        [Unsaved] public int Version;

        // Merged button pictures by defName ("" for all buttons), valid for one Version and one theme worker.
        [Unsaved] private readonly Dictionary<string, ResolvedButtonImage> images = new Dictionary<string, ResolvedButtonImage>();
        [Unsaved] private int imagesVersion = -1;
        [Unsaved] private int imagesGeneration = -1;
        [Unsaved] private BarAppearanceWorker imagesTheme;

        /// <summary>Marks the data as changed. Editing code that writes fields directly calls this.</summary>
        public void Touch() => Version++;

        /// <summary>The layer of one button, or "all buttons" for null; null when the button has none.</summary>
        public ButtonLook Layer(string defName)
        {
            if (defName == null) return all;
            EnsureIndex();
            return index.TryGetValue(defName, out var look) ? look : null;
        }

        public ButtonLook GetOrCreate(string defName)
        {
            Touch();
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

        /// <summary>Puts a copy of <paramref name="look"/> in place of a layer ("all buttons" for null): paste style.</summary>
        public void SetLayer(string defName, ButtonLook look)
        {
            Touch();
            var copy = look.Clone();
            copy.defName = defName;
            if (defName == null)
            {
                all = copy;
                return;
            }
            Reset(defName);
            buttons.Add(copy);
            index = null;
        }

        /// <summary>Back to following the layer below: one button, or "all buttons" for null.</summary>
        public void Reset(string defName)
        {
            Touch();
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

        /// <summary>Every button back to following "all buttons" (spec 4.6).</summary>
        public void ResetAllButtons()
        {
            Touch();
            buttons.Clear();
            index = null;
        }

        /// <summary>Buttons with settings of their own (spec 4.6).</summary>
        public int OwnButtonCount
        {
            get
            {
                int n = 0;
                foreach (var b in buttons)
                    if (b != null && !b.IsEmpty) n++;
                return n;
            }
        }

        public void Prune()
        {
            if (buttons.RemoveAll(b => b == null || b.defName.NullOrEmpty() || b.IsEmpty) > 0)
            {
                index = null;
                Touch();
            }
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

        /// <summary>Every image reference of every layer.</summary>
        public IEnumerable<string> ImageRefs() => new[] { all }.Concat(buttons).Where(b => b != null).SelectMany(b => b.ImageRefs());

        /// <summary>Replaces every image reference through <paramref name="map"/>.</summary>
        public void MapImageRefs(Func<string, string> map)
        {
            all.MapImageRefs(map);
            foreach (var b in buttons) b?.MapImageRefs(map);
            Touch();
        }

        private void EnsureIndex()
        {
            if (index != null) return;
            index = new Dictionary<string, ButtonLook>();
            foreach (var b in buttons)
                if (b?.defName != null) index[b.defName] = b;
        }

        // ------------------------------------------------ resolved values (this button, then all buttons); no allocation

        public bool HasAtlas(string defName) => all.HasAtlas || (Layer(defName)?.HasAtlas ?? false);
        public bool HasColor(string defName) => all.HasColor || (Layer(defName)?.HasColor ?? false);
        public bool HasImageLook(string defName) => all.HasImageLook || (Layer(defName)?.HasImageLook ?? false);

        public LabelAlign AlignFor(string defName)
        {
            var b = Layer(defName);
            return b != null && b.align != LabelAlign.Default ? b.align : all.align;
        }

        public LookFont FontFor(string defName) => FontFor(defName, out _);

        /// <summary>The label font; for Custom also its size in pixels, from the same layer (-1: the default size).</summary>
        public LookFont FontFor(string defName, out int size)
        {
            var b = Layer(defName);
            var layer = b != null && b.font != LookFont.Default ? b : all;
            size = layer.font == LookFont.Custom ? layer.fontSize : BarConfig.Unset;
            return layer.font;
        }

        /// <summary>
        /// Label colour for a state (design spec 4.5, <see cref="LookMerge"/>): this button's colour for the state,
        /// else all buttons', else the theme's own for that state; only then the next state down (open → mouse-over →
        /// normal). Null keeps the game's.
        /// </summary>
        public Color? LabelColorFor(string defName, ButtonVisualState state, BarAppearanceDef theme)
        {
            var b = Layer(defName);
            int mask = 0;
            for (int which = 0; which < 3; which++)
            {
                var s = (LookState)which;
                if (b != null && ButtonLook.IsSet(b.ColorOf(which))) mask |= LookMerge.Bit(LookLayer.Button, s);
                if (ButtonLook.IsSet(all.ColorOf(which))) mask |= LookMerge.Bit(LookLayer.AllButtons, s);
                if (ThemeColor(theme, which).a > 0f) mask |= LookMerge.Bit(LookLayer.Theme, s);
            }
            if (!LookMerge.Find((LookState)state, mask, out var layer, out var found)) return null;
            int at = (int)found;
            return layer switch
            {
                LookLayer.Button => b.ColorOf(at),
                LookLayer.AllButtons => all.ColorOf(at),
                _ => ThemeColor(theme, at),
            };
        }

        private static Color ThemeColor(BarAppearanceDef theme, int which) => theme == null ? default : which switch
        {
            2 => theme.labelActiveColor,
            1 => theme.labelMouseoverColor,
            _ => theme.labelColor,
        };

        /// <summary>True when any icon field is changed for this button (or all buttons); unchanged fields come back as the game's.</summary>
        public bool IconFor(string defName, int defaultSize, out int size, out IconAnchor anchor, out Vector2 offset)
        {
            var b = Layer(defName);
            bool any = all.HasIconLook || (b != null && b.HasIconLook);
            size = LookMerge.Pick(b != null && b.iconSize >= 0, b?.iconSize ?? 0, all.iconSize >= 0, all.iconSize, defaultSize);
            anchor = LookMerge.Pick(b != null && b.iconAnchor != IconAnchor.Default, b?.iconAnchor ?? IconAnchor.Default, true, all.iconAnchor, IconAnchor.Default);
            int x = LookMerge.Pick(b != null && b.iconOffsetX != BarConfig.UnsetOffset, b?.iconOffsetX ?? 0, all.iconOffsetX != BarConfig.UnsetOffset, all.iconOffsetX, 0);
            int y = LookMerge.Pick(b != null && b.iconOffsetY != BarConfig.UnsetOffset, b?.iconOffsetY ?? 0, all.iconOffsetY != BarConfig.UnsetOffset, all.iconOffsetY, 0);
            offset = new Vector2(x, y);
            return any;
        }

        /// <summary>
        /// The background for a state (design spec 4.5, <see cref="LookMerge"/>): this button's for the state, else all
        /// buttons', else the theme's own for that state; only then the next state down (open → mouse-over → normal,
        /// disabled → normal). An imported PNG that is missing counts as not set. A texture found for a state other than
        /// normal replaces the game's tint, like themes do. Null keeps the game's.
        /// </summary>
        public Texture2D AtlasFor(string defName, ButtonVisualState state, BarAppearanceDef theme, out bool replacesTint)
        {
            var b = Layer(defName);
            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                var s = (ButtonVisualState)i;
                if (b != null && CustomThemes.Texture(b.AtlasOf(s)) != null) mask |= LookMerge.Bit(LookLayer.Button, (LookState)i);
                if (CustomThemes.Texture(all.AtlasOf(s)) != null) mask |= LookMerge.Bit(LookLayer.AllButtons, (LookState)i);
                if (ThemeAtlas(theme, s) != null) mask |= LookMerge.Bit(LookLayer.Theme, (LookState)i);
            }
            if (!LookMerge.Find((LookState)state, mask, out var layer, out var found))
            {
                replacesTint = false;
                return null;
            }
            var at = (ButtonVisualState)found;
            replacesTint = at != ButtonVisualState.Normal;
            return layer switch
            {
                LookLayer.Button => CustomThemes.Texture(b.AtlasOf(at)),
                LookLayer.AllButtons => CustomThemes.Texture(all.AtlasOf(at)),
                _ => ThemeAtlas(theme, at),
            };
        }

        private static Texture2D ThemeAtlas(BarAppearanceDef theme, ButtonVisualState state) => theme?.Tex(state switch
        {
            ButtonVisualState.Mouseover => theme.buttonMouseover,
            ButtonVisualState.Active => theme.buttonActive,
            ButtonVisualState.Disabled => theme.buttonDisabled,
            _ => theme.button,
        });

        /// <summary>
        /// The background picture of a button ("all buttons" for null), every field merged on its own: this button,
        /// else all buttons, else the theme's own picture for it (<paramref name="themeImage"/>), else the theme
        /// picture defaults. A missing imported PNG falls back to the theme's picture. Null when there is no picture.
        /// Cached per button until the data changes (<see cref="Version"/>), another theme is used, or the imported
        /// images are read again (<see cref="CustomThemes.ImagesGeneration"/>).
        /// </summary>
        public ResolvedButtonImage ImageFor(string defName, BarAppearanceWorker theme, BarButtonImage themeImage)
        {
            if (imagesVersion != Version || imagesTheme != theme || imagesGeneration != CustomThemes.ImagesGeneration)
            {
                images.Clear();
                imagesVersion = Version;
                imagesTheme = theme;
                imagesGeneration = CustomThemes.ImagesGeneration;
            }
            string key = defName ?? "";
            if (images.TryGetValue(key, out var cached)) return cached;

            var b = Layer(defName);
            string reference = LookMerge.Pick(b != null && !b.image.NullOrEmpty(), b?.image, !all.image.NullOrEmpty(), all.image, null);
            var tex = CustomThemes.Texture(reference);
            if (tex == null && themeImage != null) tex = theme?.def?.Tex(themeImage.texture);
            ResolvedButtonImage result = null;
            if (tex != null)
            {
                var fallback = themeImage ?? Defaults;
                result = new ResolvedButtonImage
                {
                    Texture = tex,
                    texture = themeImage?.texture,
                    fit = FitOf(LookMerge.Pick(b != null && b.imageFit != LookImageFit.Default, b?.imageFit ?? LookImageFit.Default,
                        all.imageFit != LookImageFit.Default, all.imageFit, LookImageFit.Default), fallback.EffectiveFit),
                    anchor = AnchorOf(LookMerge.Pick(b != null && b.imageAnchor != LookImageAnchor.Default, b?.imageAnchor ?? LookImageAnchor.Default,
                        all.imageAnchor != LookImageAnchor.Default, all.imageAnchor, LookImageAnchor.Default), fallback.anchor),
                    verticalAnchor = VerticalOf(LookMerge.Pick(b != null && b.imageVerticalAnchor != LookImageVerticalAnchor.Default,
                        b?.imageVerticalAnchor ?? LookImageVerticalAnchor.Default, all.imageVerticalAnchor != LookImageVerticalAnchor.Default,
                        all.imageVerticalAnchor, LookImageVerticalAnchor.Default), fallback.verticalAnchor),
                    opacity = LookMerge.Pick(b != null && b.imageOpacity >= 0, (b?.imageOpacity ?? 0) / 100f, all.imageOpacity >= 0, all.imageOpacity / 100f, fallback.opacity),
                    inset = LookMerge.Pick(b != null && b.imageInset >= 0, b?.imageInset ?? 0, all.imageInset >= 0, all.imageInset, fallback.inset),
                };
            }
            images[key] = result;
            return result;
        }

        /// <summary>A theme picture's own defaults, for a custom picture on a theme that has none.</summary>
        private static readonly BarButtonImage Defaults = new BarButtonImage();

        public static BackgroundFit FitOf(LookImageFit fit, BackgroundFit fallback) => fit switch
        {
            LookImageFit.Cover => BackgroundFit.Cover,
            LookImageFit.Fit => BackgroundFit.Fit,
            LookImageFit.Stretch => BackgroundFit.Stretch,
            _ => fallback,
        };

        public static DecorationAnchor AnchorOf(LookImageAnchor anchor, DecorationAnchor fallback) => anchor switch
        {
            LookImageAnchor.Left => DecorationAnchor.Left,
            LookImageAnchor.Center => DecorationAnchor.Center,
            LookImageAnchor.Right => DecorationAnchor.Right,
            _ => fallback,
        };

        public static DecorationVerticalAnchor VerticalOf(LookImageVerticalAnchor anchor, DecorationVerticalAnchor fallback) => anchor switch
        {
            LookImageVerticalAnchor.Top => DecorationVerticalAnchor.Top,
            LookImageVerticalAnchor.Center => DecorationVerticalAnchor.Center,
            LookImageVerticalAnchor.Bottom => DecorationVerticalAnchor.Bottom,
            _ => fallback,
        };

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
                Touch();
            }
        }
    }

    /// <summary>Workers, image references and imported PNGs of custom looks.</summary>
    public static class CustomThemes
    {
        private static readonly ConditionalWeakTable<CustomThemeData, BarAppearanceWorker_Custom> workers = new ConditionalWeakTable<CustomThemeData, BarAppearanceWorker_Custom>();
        /// <summary>Textures by image reference (null once looked up and not found); read on every frame without parsing.</summary>
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Raised whenever imported PNGs are read again (their old textures are destroyed then), so anything that kept
        /// one (<see cref="CustomThemeData.ImageFor"/>) looks it up again.
        /// </summary>
        public static int ImagesGeneration { get; private set; }

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
        public static string AtlasRef(BarAppearanceDef theme, string field) => ImageRef.ForThemeField(theme.defName, field);

        /// <summary>
        /// The texture an image reference points at (<see cref="ImageRef"/>): an installed theme's background or
        /// picture, or an imported PNG. Null when empty, not understood, or not found (a missing PNG is warned about once).
        /// </summary>
        public static Texture2D Texture(string reference)
        {
            if (reference.NullOrEmpty()) return null;
            if (textures.TryGetValue(reference, out var tex)) return tex;
            var r = ImageRef.Parse(reference);
            switch (r.Kind)
            {
                case ImageRefKind.File:
                    tex = FileTexture(r.Value);
                    break;
                case ImageRefKind.ThemeField:
                case ImageRefKind.ThemeTexture:
                    var theme = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(r.Theme);
                    tex = theme == null ? null : theme.Tex(r.Kind == ImageRefKind.ThemeField ? FieldValue(theme, r.Value) : r.Value);
                    break;
                case ImageRefKind.GameTexture:
                    tex = ContentFinder<Texture2D>.Get(r.Value, false);
                    break;
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

        /// <summary>Themes changed (reload, play data): look theme and mod textures up again. Imported PNGs are this mod's own and stay.</summary>
        internal static void ResetCache()
        {
            foreach (var key in textures.Keys.Where(k => !ImageRef.IsFile(k)).ToList())
                textures.Remove(key);
            ImagesGeneration++;
        }

        // ------------------------------------------------ imported PNGs (design spec 4.16)

        /// <summary>An imported PNG from <see cref="ModPaths.ImagesFolder"/> (the caller caches it); a missing one is warned about once.</summary>
        private static Texture2D FileTexture(string fileName)
        {
            var tex = LoadPng(Path.Combine(ModPaths.ImagesFolder, fileName), fileName);
            if (tex == null)
                Log.WarningOnce(MainButtonBarSystemMod.LogPrefix + "The custom theme image " + fileName + " is not in " + ModPaths.ImagesFolder +
                                "; the theme's own image is used instead.", ("MBBS_Image_" + fileName).GetHashCode());
            return tex;
        }

        private static Texture2D LoadPng(string path, string name)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var tex = new Texture2D(2, 2);
                if (!tex.LoadImage(File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.name = "MBBS_Image_" + name;
                return tex;
            }
            catch (Exception e)
            {
                Log.WarningOnce(MainButtonBarSystemMod.LogPrefix + "Could not read the image " + path + ": " + e.Message, path.GetHashCode());
                return null;
            }
        }

        /// <summary>True when <paramref name="reference"/> is an imported PNG that cannot be found.</summary>
        public static bool IsMissingFile(string reference) =>
            ImageRef.IsFile(reference) && !File.Exists(Path.Combine(ModPaths.ImagesFolder, ImageRef.Parse(reference).Value ?? ""));

        /// <summary>True when the image a reference points at is not here (no warning is logged; for import reports).</summary>
        public static bool IsMissing(string reference)
        {
            if (reference.NullOrEmpty()) return false;
            return ImageRef.IsFile(reference) ? IsMissingFile(reference) : Texture(reference) == null;
        }

        /// <summary>The image a reference points at, as a short name for reports: the file name or the texture path.</summary>
        public static string ShortName(string reference)
        {
            var r = ImageRef.Parse(reference);
            return r.Kind == ImageRefKind.None ? reference : r.Value;
        }

        /// <summary>
        /// Reads the Images folder again: the loaded PNG textures are destroyed (this mod made them; anything that kept
        /// one is told through <see cref="ImagesGeneration"/>), and missing files are looked for again.
        /// </summary>
        public static void RefreshImages()
        {
            foreach (var key in textures.Keys.Where(ImageRef.IsFile).ToList())
            {
                if (textures[key] != null) UnityEngine.Object.Destroy(textures[key]);
                textures.Remove(key);
            }
            ImagesGeneration++;
        }

        /// <summary>PNG files in the Images folder, as image references.</summary>
        public static List<string> ImportedImages()
        {
            var dir = new DirectoryInfo(ModPaths.ImagesFolder);
            if (!dir.Exists) return new List<string>();
            return dir.GetFiles("*.png").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).Select(f => ImageRef.ForFile(f.Name)).ToList();
        }

        public static void OpenImagesFolder()
        {
            Directory.CreateDirectory(ModPaths.ImagesFolder);
            Application.OpenURL("file:///" + ModPaths.ImagesFolder.Replace('\\', '/'));
        }

        /// <summary>Every theme image a custom look can use: each installed theme's backgrounds, then its button pictures.</summary>
        public static List<(BarAppearanceDef theme, string reference)> ThemeImages()
        {
            var result = new List<(BarAppearanceDef, string)>();
            foreach (var theme in BarModel.Appearances)
            {
                foreach (var field in AtlasFields)
                    if (!FieldValue(theme, field).NullOrEmpty()) result.Add((theme, AtlasRef(theme, field)));
                var seen = new HashSet<string>();
                if (theme.buttonImage != null && !theme.buttonImage.texture.NullOrEmpty() && seen.Add(theme.buttonImage.texture))
                    result.Add((theme, ImageRef.ForThemeTexture(theme.defName, theme.buttonImage.texture)));
                if (theme.buttonImages == null) continue;
                foreach (var image in theme.buttonImages)
                    if (image != null && !image.texture.NullOrEmpty() && seen.Add(image.texture))
                        result.Add((theme, ImageRef.ForThemeTexture(theme.defName, image.texture)));
            }
            return result;
        }

        /// <summary>Short text for an image reference: "Theme: normal background", "Theme: Btn_board", "file.png".</summary>
        public static string Describe(string reference)
        {
            var r = ImageRef.Parse(reference);
            switch (r.Kind)
            {
                case ImageRefKind.File:
                    return IsMissingFile(reference) ? "MBBS_IconMissing".Translate(r.Value).ToString() : r.Value;
                case ImageRefKind.ThemeField:
                case ImageRefKind.ThemeTexture:
                    var theme = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(r.Theme);
                    if (theme == null) return "MBBS_Look_AtlasMissing".Translate(reference);
                    string what = r.Kind == ImageRefKind.ThemeField ? ("MBBS_Look_Field_" + r.Value).Translate().ToString() : Path.GetFileName(r.Value);
                    return "MBBS_Look_AtlasFrom".Translate(theme.LabelCap, what);
                case ImageRefKind.GameTexture:
                    return Texture(reference) == null ? "MBBS_Look_AtlasMissing".Translate(reference).ToString() : Path.GetFileName(r.Value);
                default:
                    return reference ?? "";
            }
        }

        /// <summary>The game font a look's font is drawn with; Custom resizes the normal font.</summary>
        public static GameFont? GameFontOf(LookFont font) => font switch
        {
            LookFont.Tiny => GameFont.Tiny,
            LookFont.Small or LookFont.Custom => GameFont.Small,
            LookFont.Medium => GameFont.Medium,
            _ => (GameFont?)null,
        };

        /// <summary>
        /// Pixel size of a Custom font: the stored size, else the tuning def's default, else (default 0) the size of
        /// the game's normal font.
        /// </summary>
        public static int FontSize(int stored)
        {
            // Clamped to the slider's range: a shared theme or arrangement may carry any number.
            if (stored > 0) return Mathf.Clamp(stored, MainButtonBarTuningDef.Current.ranges.lookFontSize.min, MainButtonBarTuningDef.Current.ranges.lookFontSize.max);
            int fallback = MainButtonBarTuningDef.Current.barDefaults.lookFontSize;
            if (fallback > 0) return fallback;
            var normal = Text.fontStyles[(int)GameFont.Small].font;
            return normal != null ? normal.fontSize : 0;
        }
    }

    /// <summary>
    /// Draws the chosen theme with a custom look on top: button backgrounds, label colours, alignment, font, icon
    /// placement and background picture per button. Everything the custom look does not change goes to the theme's own
    /// worker (<see cref="inner"/>), so a theme mod's worker class keeps working: the bar and drawer backgrounds, More,
    /// the arrows, decorations, and a button's look wherever no layer of the custom look changes it.
    /// </summary>
    public sealed class BarAppearanceWorker_Custom : BarAppearanceWorker
    {
        public readonly CustomThemeData data;

        /// <summary>The chosen theme's own worker.</summary>
        public readonly BarAppearanceWorker inner;

        public BarAppearanceWorker_Custom(BarAppearanceDef baseDef, CustomThemeData data)
        {
            def = baseDef;
            this.data = data;
            inner = baseDef.Worker ?? new BarAppearanceWorker { def = baseDef };
        }

        public override bool ChangesButtons => true;

        public override Texture2D ButtonAtlas(ButtonVisualState state, out bool replacesTint)
        {
            var button = ButtonPainter.ActiveButton;
            if (button == null || !data.HasAtlas(button.defName)) return inner.ButtonAtlas(state, out replacesTint);
            return data.AtlasFor(button.defName, state, def, out replacesTint);
        }

        public override Color? LabelColor(ButtonVisualState state)
        {
            var button = ButtonPainter.ActiveButton;
            if (button == null || !data.HasColor(button.defName)) return inner.LabelColor(state);
            return data.LabelColorFor(button.defName, state, def);
        }

        public override LabelAlign LabelAlignFor(MainButtonDef button)
        {
            var align = data.AlignFor(button?.defName);
            return align != LabelAlign.Default ? align : inner.LabelAlignFor(button);
        }

        public override GameFont? LabelFontFor(MainButtonDef button)
        {
            var font = data.FontFor(button?.defName);
            return font != LookFont.Default ? CustomThemes.GameFontOf(font) : inner.LabelFontFor(button);
        }

        public override int LabelFontSizeFor(MainButtonDef button)
        {
            var font = data.FontFor(button?.defName, out int size);
            if (font == LookFont.Default) return inner.LabelFontSizeFor(button);
            return font == LookFont.Custom ? CustomThemes.FontSize(size) : 0;
        }

        public override bool IconLookFor(MainButtonDef button, out float size, out IconAnchor anchor, out Vector2 offset)
        {
            if (!data.IconFor(button?.defName, Mathf.RoundToInt(MainButtonBarTuningDef.Current.behaviour.iconSize), out int s, out anchor, out offset))
                return inner.IconLookFor(button, out size, out anchor, out offset);
            size = s;
            return true;
        }

        public override BarButtonImage ButtonImage(MainButtonDef button)
        {
            var themeImage = inner.ButtonImage(button);
            if (!data.HasImageLook(button?.defName)) return themeImage;
            return data.ImageFor(button?.defName, inner, themeImage);
        }

        /// <summary>The picture "all buttons" shows in the Appearance tab's preview (the theme's default picture under it).</summary>
        public BarButtonImage AllButtonsImage()
        {
            var themeImage = def.ButtonImageFor(null);
            if (!data.all.HasImageLook) return themeImage;
            return data.ImageFor(null, inner, themeImage);
        }

        public override void DrawButtonImage(Rect rect, BarButtonImage image)
        {
            if (image is ResolvedButtonImage resolved) DrawButtonImageTexture(rect, resolved, resolved.Texture);
            else inner.DrawButtonImage(rect, image);
        }

        // DrawButton and DrawDrawerButton stay the base ones on purpose: they paint through ButtonPainter with this worker,
        // so the custom look applies; a theme worker's own DrawButton would paint with itself and skip it.

        // Everything outside the buttons is the theme's.
        public override void DrawBarBackground(Rect barRect) => inner.DrawBarBackground(barRect);
        public override void DrawBarBackgroundImage(Rect barRect) => inner.DrawBarBackgroundImage(barRect);
        public override void DrawBarBackgroundImage(Rect barRect, float opacity) => inner.DrawBarBackgroundImage(barRect, opacity);
        public override void DrawBarOverlay(Rect barRect) => inner.DrawBarOverlay(barRect);
        public override Texture2D DrawerSettingsIcon() => inner.DrawerSettingsIcon();
        public override void DrawDrawerBackground(Rect windowRect) => inner.DrawDrawerBackground(windowRect);
        public override void DrawDrawerBackgroundImage(Rect windowRect) => inner.DrawDrawerBackgroundImage(windowRect);
        public override void DrawDrawerOverlay(Rect windowRect) => inner.DrawDrawerOverlay(windowRect);
        public override void DrawDecorations(DecorationTarget target, DecorationLayer layer, Rect area) => inner.DrawDecorations(target, layer, area);
        public override string MoreButtonLabel(int drawerCount) => inner.MoreButtonLabel(drawerCount);
        public override float MoreButtonWidth(BarConfig config, int drawerCount) => inner.MoreButtonWidth(config, drawerCount);
        public override bool DrawMoreButton(Rect rect, int drawerCount, bool drawerOpen) => inner.DrawMoreButton(rect, drawerCount, drawerOpen);
        public override bool DrawScrollArrow(Rect rect, bool left) => inner.DrawScrollArrow(rect, left);
    }
}
