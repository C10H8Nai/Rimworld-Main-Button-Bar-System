using System;
using System.Collections.Generic;
using System.IO;
using MainButtonBarSystem.Core;
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
    /// <para>Button textures are nine-slice atlases, drawn like the game's <c>Widgets.ButtonSubtleAtlas</c>. The bar
    /// and drawer backgrounds can instead be whole images (<see cref="barBackgroundFit"/>), and <see cref="decorations"/>
    /// places any number of images, such as a character portrait, at the corners of the bar or the drawer.</para>
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

        /// <summary>Icon of the drawer's settings button. Empty: the gear from the tuning def.</summary>
        public string drawerSettingsIcon;

        /// <summary>Image in the Appearance tab. Empty: a preview drawn from the textures above.</summary>
        public string preview;

        /// <summary>How <see cref="barBackground"/> fills the bar: Atlas (nine-slice, the default), Tile, Stretch, Fit or Cover.</summary>
        public BackgroundFit barBackgroundFit = BackgroundFit.Atlas;

        /// <summary>Tile only: the image is repeated at its pixel size times this.</summary>
        public float barBackgroundTileScale = 1f;

        /// <summary>How <see cref="drawerBackground"/> fills the drawer window; see <see cref="barBackgroundFit"/>.</summary>
        public BackgroundFit drawerBackgroundFit = BackgroundFit.Atlas;

        public float drawerBackgroundTileScale = 1f;

        /// <summary>Label colour while pressed or while its tab is open. Fully transparent uses <see cref="labelMouseoverColor"/>.</summary>
        public Color labelActiveColor;

        /// <summary>
        /// Images placed at anchors of the bar or the drawer, below or above the buttons (a portrait growing out of
        /// the bar, a badge that appears under the mouse). Empty: none. See <see cref="BarDecoration"/>.
        /// </summary>
        public List<BarDecoration> decorations;

        /// <summary>
        /// Picture inside every main button, under the button atlas (a frame with a transparent middle lets it show).
        /// Empty: none. Buttons listed in <see cref="buttonImages"/> use their own instead.
        /// </summary>
        public BarButtonImage buttonImage;

        /// <summary>Pictures for named buttons (<see cref="BarButtonImage.button"/>); override <see cref="buttonImage"/>. Buttons not listed keep the default.</summary>
        public List<BarButtonImage> buttonImages;

        [Unsaved] private BarAppearanceWorker workerInt;

        /// <summary>buttonImages by button defName, built on first use (a later entry for the same button wins).</summary>
        [Unsaved] private Dictionary<string, BarButtonImage> buttonImageIndex;
        [Unsaved] private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        /// <summary>Paths already reported as missing (by <see cref="Tex"/> or the dev-mode validator), so each is reported once.</summary>
        [Unsaved] private readonly HashSet<string> missingReported = new HashSet<string>();

        /// <summary>Textures <see cref="ReloadTextures"/> read from disk; released at the next reload.</summary>
        [Unsaved] private readonly List<Texture2D> ownTextures = new List<Texture2D>();

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

        /// <summary>Loads a texture once; null when the path is empty or missing (a missing one is reported once, unless the dev-mode checks already did).</summary>
        public Texture2D Tex(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!textures.TryGetValue(path, out var tex))
            {
                tex = ContentFinder<Texture2D>.Get(path, false);
                if (tex == null && missingReported.Add(path))
                    Log.WarningOnce($"{MainButtonBarSystemMod.LogPrefix}Theme {defName}: texture '{path}' not found; using the game's look for it.", (defName + path).GetHashCode());
                textures[path] = tex;
            }
            return tex;
        }

        /// <summary>Like <see cref="Tex"/>, but a missing texture is left for the caller to report (the validator names the field); it is then not reported again.</summary>
        internal Texture2D TexQuiet(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!textures.TryGetValue(path, out var tex))
            {
                tex = ContentFinder<Texture2D>.Get(path, false);
                textures[path] = tex;
            }
            if (tex == null) missingReported.Add(path);
            return tex;
        }

        public bool HasLabelColor => labelColor.a > 0f;

        public bool HasDecorations => decorations != null && decorations.Count > 0;

        /// <summary>True when any button gets a picture (<see cref="buttonImage"/> or an entry of <see cref="buttonImages"/>).</summary>
        public bool HasButtonImages => IsUsable(buttonImage) || ButtonImageIndex.Count > 0;

        /// <summary>The picture for a button: its own entry in <see cref="buttonImages"/>, else <see cref="buttonImage"/>, else null.</summary>
        public BarButtonImage ButtonImageFor(string buttonDefName)
        {
            if (buttonDefName != null && ButtonImageIndex.TryGetValue(buttonDefName, out var own)) return own;
            return IsUsable(buttonImage) ? buttonImage : null;
        }

        private Dictionary<string, BarButtonImage> ButtonImageIndex
        {
            get
            {
                if (buttonImageIndex == null)
                {
                    buttonImageIndex = new Dictionary<string, BarButtonImage>();
                    if (buttonImages != null)
                    {
                        foreach (var image in buttonImages)
                            if (IsUsable(image) && !string.IsNullOrEmpty(image.button)) buttonImageIndex[image.button] = image;
                    }
                }
                return buttonImageIndex;
            }
        }

        private static bool IsUsable(BarButtonImage image) => image != null && !string.IsNullOrEmpty(image.texture);

        /// <summary>True when the theme replaces anything inside the buttons themselves.</summary>
        public bool ChangesButtons =>
            !string.IsNullOrEmpty(button) || !string.IsNullOrEmpty(buttonMouseover) || !string.IsNullOrEmpty(buttonActive) ||
            !string.IsNullOrEmpty(buttonDisabled) || HasLabelColor || labelMouseoverColor.a > 0f || labelActiveColor.a > 0f ||
            HasButtonImages;

        /// <summary>Every texture field of the def with its path (empty paths included), for checks and reloading.</summary>
        public IEnumerable<(string field, string path)> TexturePaths()
        {
            yield return (nameof(button), button);
            yield return (nameof(buttonMouseover), buttonMouseover);
            yield return (nameof(buttonActive), buttonActive);
            yield return (nameof(buttonDisabled), buttonDisabled);
            yield return (nameof(moreButton), moreButton);
            yield return (nameof(arrowLeft), arrowLeft);
            yield return (nameof(arrowRight), arrowRight);
            yield return (nameof(barBackground), barBackground);
            yield return (nameof(drawerBackground), drawerBackground);
            yield return (nameof(drawerSettingsIcon), drawerSettingsIcon);
            yield return (nameof(preview), preview);
            if (buttonImage != null)
                yield return ($"{nameof(buttonImage)}.{nameof(BarButtonImage.texture)}", buttonImage.texture);
            if (buttonImages != null)
            {
                for (int i = 0; i < buttonImages.Count; i++)
                    yield return ($"{nameof(buttonImages)}[{i}].{nameof(BarButtonImage.texture)}", buttonImages[i]?.texture);
            }
            if (decorations == null) yield break;
            for (int i = 0; i < decorations.Count; i++)
                yield return ($"{nameof(decorations)}[{i}].{nameof(BarDecoration.texture)}", decorations[i]?.texture);
        }

        /// <summary>
        /// Development mode: reads this theme's PNG files again from its own mod folder and uses them instead of the
        /// game's cached textures, so an edited image shows without a restart. Paths with no file in the mod folder go
        /// back to the game's texture. The textures read by the previous reload are destroyed first (nothing holds them
        /// but this cache). Returns how many files were read.
        /// </summary>
        public int ReloadTextures()
        {
            foreach (var old in ownTextures)
                if (old != null) UnityEngine.Object.Destroy(old);
            ownTextures.Clear();
            textures.Clear();
            missingReported.Clear();
            int count = 0;
            foreach (var (_, path) in TexturePaths())
            {
                if (string.IsNullOrEmpty(path) || textures.ContainsKey(path)) continue;
                var tex = LoadFromModFolder(path);
                if (tex == null) continue;
                textures[path] = tex;
                ownTextures.Add(tex);
                count++;
            }
            return count;
        }

        private Texture2D LoadFromModFolder(string path)
        {
            var pack = modContentPack;
            if (pack?.foldersToLoadDescendingOrder == null) return null;
            string relative = Path.Combine("Textures", path.Replace('/', Path.DirectorySeparatorChar) + ".png");
            foreach (var folder in pack.foldersToLoadDescendingOrder)
            {
                string file = Path.Combine(folder, relative);
                if (!File.Exists(file)) continue;
                try
                {
                    var tex = new Texture2D(2, 2);
                    if (!tex.LoadImage(File.ReadAllBytes(file))) return null;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.name = "MBBS_Reloaded_" + path;
                    return tex;
                }
                catch (Exception e)
                {
                    Log.Warning($"{MainButtonBarSystemMod.LogPrefix}Theme {defName}: could not reload {file}: {e.Message}");
                    return null;
                }
            }
            return null;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (workerClass == null || !typeof(BarAppearanceWorker).IsAssignableFrom(workerClass))
                yield return "workerClass must derive from " + nameof(BarAppearanceWorker);
            if (barBackgroundTileScale <= 0f) yield return nameof(barBackgroundTileScale) + " must be positive";
            if (drawerBackgroundTileScale <= 0f) yield return nameof(drawerBackgroundTileScale) + " must be positive";
            if (buttonImage != null)
            {
                foreach (var e in buttonImage.ConfigErrors(-1)) yield return e;
            }
            if (buttonImages != null)
            {
                for (int i = 0; i < buttonImages.Count; i++)
                {
                    if (buttonImages[i] == null)
                    {
                        yield return $"{nameof(buttonImages)}[{i}] is empty";
                        continue;
                    }
                    foreach (var e in buttonImages[i].ConfigErrors(i)) yield return e;
                }
            }
            if (decorations == null) yield break;
            for (int i = 0; i < decorations.Count; i++)
            {
                if (decorations[i] == null)
                {
                    yield return $"{nameof(decorations)}[{i}] is empty";
                    continue;
                }
                foreach (var e in decorations[i].ConfigErrors(i)) yield return e;
            }
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

        /// <summary>Whether this look changes main buttons at all (else they are drawn exactly as the game does).</summary>
        public virtual bool ChangesButtons => def != null && def.ChangesButtons;

        /// <summary>Label alignment for one button; Default uses the bar's setting.</summary>
        public virtual LabelAlign LabelAlignFor(MainButtonDef button) => LabelAlign.Default;

        /// <summary>Label font for one button; null keeps the game's.</summary>
        public virtual GameFont? LabelFontFor(MainButtonDef button) => null;

        /// <summary>
        /// Icon size and placement for one button. True when changed: this mod then draws that button's icon itself
        /// (as with "icon only"), so mods that move icons in DoButton do not apply to it.
        /// </summary>
        public virtual bool IconLookFor(MainButtonDef button, out float size, out Core.IconAnchor anchor, out Vector2 offset)
        {
            size = 0f;
            anchor = Core.IconAnchor.Default;
            offset = Vector2.zero;
            return false;
        }

        /// <summary>Drawn once per event before any button: the background image, then the decorations below the buttons.</summary>
        public virtual void DrawBarBackground(Rect barRect)
        {
            DrawBarBackgroundImage(barRect);
            DrawDecorations(DecorationTarget.Bar, DecorationLayer.BelowButtons, barRect);
        }

        /// <summary>Only the bar's background image, drawn the way <see cref="BarAppearanceDef.barBackgroundFit"/> says, at the player's background opacity.</summary>
        public virtual void DrawBarBackgroundImage(Rect barRect) => DrawBarBackgroundImage(barRect, BarConfigManager.Active.BackgroundOpacity);

        /// <summary>The bar's background image at a given opacity (the Appearance tab's preview passes 1 so every card looks the same).</summary>
        public virtual void DrawBarBackgroundImage(Rect barRect, float opacity)
        {
            var tex = def.Tex(def.barBackground);
            if (tex != null) TexturePainter.Draw(barRect, tex, def.barBackgroundFit, def.barBackgroundTileScale, opacity);
        }

        /// <summary>Drawn once per event after every button and the More button: the decorations above the buttons.</summary>
        public virtual void DrawBarOverlay(Rect barRect) => DrawDecorations(DecorationTarget.Bar, DecorationLayer.AboveButtons, barRect);

        public virtual void DrawButton(BarButton button, Rect rect)
        {
            ButtonPainter.Draw(button, rect, this);
        }

        /// <summary>Draws one cell of the "More" drawer.</summary>
        public virtual void DrawDrawerButton(BarButton button, Rect rect)
        {
            ButtonPainter.Draw(button, rect, this);
        }

        /// <summary>Icon of the drawer's settings button, or null for the tuning def's gear.</summary>
        public virtual Texture2D DrawerSettingsIcon() => def.Tex(def.drawerSettingsIcon);

        /// <summary>Background of the drawer window, covering the whole window, then the decorations below its buttons.</summary>
        public virtual void DrawDrawerBackground(Rect windowRect)
        {
            DrawDrawerBackgroundImage(windowRect);
            DrawDecorations(DecorationTarget.Drawer, DecorationLayer.BelowButtons, windowRect);
        }

        /// <summary>Only the drawer's background image (the game's window background when the theme has none).</summary>
        public virtual void DrawDrawerBackgroundImage(Rect windowRect)
        {
            var tex = def.Tex(def.drawerBackground);
            if (tex != null) TexturePainter.Draw(windowRect, tex, def.drawerBackgroundFit, def.drawerBackgroundTileScale, BarConfigManager.Active.BackgroundOpacity);
            else Widgets.DrawWindowBackground(windowRect);
        }

        /// <summary>Drawn after the drawer's buttons and its settings gear: the decorations above them. The window clips anything outside it.</summary>
        public virtual void DrawDrawerOverlay(Rect windowRect) => DrawDecorations(DecorationTarget.Drawer, DecorationLayer.AboveButtons, windowRect);

        /// <summary>
        /// Draws the def's decorations for one target and layer, placed by <see cref="DecorationLayout.Place"/> with the
        /// player's scale, offset and opacity settings applied. Decorations whose texture is missing are skipped.
        /// </summary>
        public virtual void DrawDecorations(DecorationTarget target, DecorationLayer layer, Rect area)
        {
            if (!def.HasDecorations) return;
            var config = BarConfigManager.Active;
            if (!config.DecorationsShown) return;

            bool mouseover = Mouse.IsOver(area);
            bool drawerOpen = target == DecorationTarget.Bar && Window_ButtonDrawer.IsShowing;
            float scale = config.DecorationScale;
            var shift = config.DecorationOffset;
            float opacity = config.DecorationOpacity;
            var saved = GUI.color;
            try
            {
                foreach (var decoration in def.decorations)
                {
                    if (decoration == null || decoration.target != target || decoration.layer != layer || !decoration.ShowsIn(mouseover, drawerOpen)) continue;
                    var tex = def.Tex(decoration.texture);
                    if (tex == null) continue;
                    var box = DecorationLayout.Place(area.x, area.y, area.width, area.height, decoration.anchor, decoration.verticalAnchor,
                        decoration.size.x, decoration.size.y, tex.width, tex.height, decoration.offset.x, decoration.offset.y, scale, shift.x, shift.y);
                    if (box.IsEmpty) continue;
                    GUI.color = new Color(saved.r, saved.g, saved.b, saved.a * Mathf.Clamp01(decoration.opacity) * opacity);
                    GUI.DrawTexture(new Rect(Mathf.Round(box.X), Mathf.Round(box.Y), Mathf.Round(box.Width), Mathf.Round(box.Height)), tex);
                }
            }
            finally
            {
                GUI.color = saved;
            }
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

        /// <summary>The picture to draw inside this main button (<see cref="BarAppearanceDef.ButtonImageFor"/>), or null.</summary>
        public virtual BarButtonImage ButtonImage(MainButtonDef button) => button == null ? null : def.ButtonImageFor(button.defName);

        /// <summary>
        /// Draws a button picture inside <paramref name="rect"/> (minus its inset), before the button atlas. Only the
        /// alpha of the current GUI colour is kept, so the game's mouse-over tint stays on the frame and does not tint
        /// the picture. A missing texture draws nothing (and is reported once).
        /// </summary>
        public virtual void DrawButtonImage(Rect rect, BarButtonImage image)
        {
            if (image == null) return;
            var tex = def.Tex(image.texture);
            if (tex == null) return;
            // Whole pixels, like the atlas drawn over it (DrawAtlas rounds its rect), so no sliver shows past the frame.
            var rounded = rect.Rounded();
            var area = image.inset > 0f ? rounded.ContractedBy(image.inset) : rounded;
            if (area.width <= 0f || area.height <= 0f) return;
            var saved = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, saved.a);
            try
            {
                TexturePainter.DrawAnchored(area, tex, image.EffectiveFit, 1f, image.opacity, image.anchor, image.verticalAnchor);
            }
            finally
            {
                GUI.color = saved;
            }
        }

        /// <summary>Label colour for a state, or null to keep the game's. Active: labelActiveColor, else labelMouseoverColor, else labelColor.</summary>
        public virtual Color? LabelColor(ButtonVisualState state)
        {
            if (state == ButtonVisualState.Active && def.labelActiveColor.a > 0f) return def.labelActiveColor;
            if (state == ButtonVisualState.Mouseover || state == ButtonVisualState.Active)
            {
                if (def.labelMouseoverColor.a > 0f) return def.labelMouseoverColor;
            }
            return def.HasLabelColor ? def.labelColor : (Color?)null;
        }

        private int moreLabelCount = -1;
        private LoadedLanguage moreLabelLanguage;
        private string moreLabel;

        /// <summary>"More (N)" in the current language; translated again only when N or the language changes.</summary>
        public virtual string MoreButtonLabel(int drawerCount)
        {
            var language = LanguageDatabase.activeLanguage;
            if (moreLabel == null || drawerCount != moreLabelCount || language != moreLabelLanguage)
            {
                moreLabel = "MBBS_More".Translate(drawerCount);
                moreLabelCount = drawerCount;
                moreLabelLanguage = language;
            }
            return moreLabel;
        }

        private string measuredLabel;
        private GameFont measuredFont;
        private float measuredWidth;

        /// <summary>Width of the More button: the player's setting, or the label's width when that is 0.</summary>
        public virtual float MoreButtonWidth(BarConfig config, int drawerCount)
        {
            if (config.MoreButtonWidth > 0) return config.MoreButtonWidth;
            var b = MainButtonBarTuningDef.Current.behaviour;
            string label = MoreButtonLabel(drawerCount);
            if (!ReferenceEquals(label, measuredLabel) || b.labelFont != measuredFont)
            {
                var font = Text.Font;
                Text.Font = b.labelFont;
                measuredWidth = Text.CalcSize(label).x;
                Text.Font = font;
                measuredLabel = label;
                measuredFont = b.labelFont;
            }
            return measuredWidth + b.moreButtonLabelPadding;
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
    // The attribute tells the game the static texture field is intended (it only holds the icon being drawn).
    [StaticConstructorOnStartup]
    public static class ButtonPainter
    {
        /// <summary>Alignment requested for the button being drawn; Default outside this mod's drawing.</summary>
        public static LabelAlign ActiveAlign { get; private set; }

        /// <summary>Theme applied to the button being drawn; null outside this mod's drawing.</summary>
        public static BarAppearanceWorker ActiveTheme { get; private set; }

        /// <summary>
        /// The main button being drawn while <see cref="ActiveTheme"/> is set, for its picture
        /// (<see cref="BarAppearanceWorker.ButtonImage"/>); null for the More button, the arrows and outside this mod's drawing.
        /// </summary>
        public static MainButtonDef ActiveButton { get; private set; }

        /// <summary>
        /// Set once <see cref="ActiveButton"/>'s picture is drawn: only the first subtle atlas of a button gets it (the
        /// game draws one; a mod drawing sub-buttons or badges inside the same button does not get extra copies).
        /// </summary>
        internal static bool ActiveButtonImageDrawn { get; set; }

        /// <summary>Whether the button being drawn is pressed, open or disabled; mouse-over is decided per rect.</summary>
        public static ButtonVisualState ActiveBaseState { get; private set; }

        /// <summary>
        /// Icon this mod draws itself for the button being drawn: left of the label ("icon and text") or centred
        /// ("icon only", see <see cref="ActiveIconOnly"/>); null when the game draws the button's icon.
        /// </summary>
        public static Texture2D ActiveIcon { get; private set; }

        /// <summary>True when <see cref="ActiveIcon"/> is drawn alone, with no label.</summary>
        public static bool ActiveIconOnly { get; private set; }

        /// <summary>
        /// <see cref="ActiveIcon"/> replaces an icon the game draws invisibly: alone when the game gives the button no
        /// label, beside the label when it (or another mod) does. Decided by <see cref="NoteGameLabel"/>.
        /// </summary>
        internal static bool ActiveIconFollowsGame { get; private set; }

        /// <summary>Called by the label patch with the label the game passes for the button being drawn.</summary>
        internal static void NoteGameLabel(string label)
        {
            if (ActiveIconFollowsGame) ActiveIconOnly = string.IsNullOrEmpty(label);
        }

        /// <summary>
        /// The button being drawn is too narrow for its icon or label (<c>contentMinButtonWidth</c>): only its
        /// background is drawn.
        /// </summary>
        public static bool ActiveBlank { get; private set; }

        /// <summary>Label font of the button being drawn; null for the tuning def's.</summary>
        internal static GameFont? ActiveFont { get; private set; }

        /// <summary>Icon size of a custom look for the button being drawn; negative for the tuning def's.</summary>
        internal static float ActiveIconSize { get; private set; } = -1f;

        internal static Core.IconAnchor ActiveIconAnchor { get; private set; }
        internal static Vector2 ActiveIconOffset { get; private set; }

        private static float IconSizeNow => ActiveIconSize >= 0f ? ActiveIconSize : B.iconSize;

        private static MainButtonBarTuningDef.Behaviour B => MainButtonBarTuningDef.Current.behaviour;

        // Null when a game update renamed the field: buttons are then drawn by the game as they are, without icon settings.
        private static readonly HarmonyLib.AccessTools.FieldRef<MainButtonDef, Texture2D> IconField =
            SafeAccess.FieldRef<MainButtonDef, Texture2D>("icon");

        /// <summary>
        /// Draws through the game's DoButton. Icon settings work by swapping the def's icon only for this call (the
        /// game draws the icon when MainButtonDef.Icon is set and the label otherwise), then restoring it. For "icon
        /// and text" and "icon only" the game gets no icon and draws the label; the label patch then draws the icon
        /// itself, so mods that change how DoButton places icons and labels (Vanilla Textures Expanded shows both when
        /// the button is wide enough) cannot add the label back.
        /// </summary>
        public static void Draw(BarButton button, Rect rect, BarAppearanceWorker theme)
        {
            var def = button.Def;
            var config = BarConfigManager.Active;
            var align = config.labelAlign;
            bool themed = theme != null && theme.ChangesButtons;
            var settings = button.Settings;
            var custom = CustomIcons.Resolve(settings.icon);
            var mode = config.IconModeFor(settings);

            // A custom look can align, size and place per button (design spec 2026-11-05 §4.8, 4.9).
            GameFont? font = null;
            bool iconLook = false;
            float iconSize = 0f;
            var anchor = Core.IconAnchor.Default;
            var offset = Vector2.zero;
            if (themed)
            {
                var ownAlign = theme.LabelAlignFor(def);
                if (ownAlign != LabelAlign.Default) align = ownAlign;
                font = theme.LabelFontFor(def);
                iconLook = theme.IconLookFor(def, out iconSize, out anchor, out offset);
            }
            // Squeezed by other buttons' widths (equal split): an icon or label would spill over the neighbours.
            bool blank = B.contentMinButtonWidth > 0 && rect.width < B.contentMinButtonWidth;
            // With only the font changed, align stays Default and the label patch draws it where the game would.
            if (IconField == null || (!blank && align == LabelAlign.Default && !themed && custom == null && mode == IconMode.Default))
            {
                def.Worker.DoButton(rect);
                return;
            }

            // Load the button's own icon before remembering the field: restoring a field saved before the first load
            // would put null back, and the game would look the icon up again through every mod on each draw.
            Texture2D own = SafeIcon(def);
            var savedIcon = IconField(def);
            var savedPath = def.iconPath;
            Texture2D effective = custom ?? own;

            ActiveAlign = align;
            ActiveFont = font;
            ActiveIconSize = iconLook ? iconSize : -1f;
            ActiveIconAnchor = anchor;
            ActiveIconOffset = iconLook ? offset : Vector2.zero;
            ActiveTheme = themed ? theme : null;
            ActiveButton = themed ? def : null;
            ActiveButtonImageDrawn = false;
            ActiveBaseState = BaseState(def, rect);
            try
            {
                if (blank)
                {
                    // The game then draws the label instead, which the label patch drops.
                    HideIcon(def);
                    ActiveBlank = true;
                }
                else switch (mode)
                {
                    case IconMode.Text:
                        HideIcon(def);
                        break;
                    case IconMode.Both:
                    case IconMode.Icon:
                        // Without an icon both fall back to the label, like the game.
                        if (effective != null)
                        {
                            HideIcon(def);
                            ActiveIcon = effective;
                            ActiveIconOnly = mode == IconMode.Icon;
                        }
                        break;
                    default: // Default: the game (and mods changing DoButton) decide, with the custom icon if set.
                        if (iconLook && effective != null)
                        {
                            // A custom icon size or position (spec 4.9): the game still decides whether the label
                            // shows (it does with Vanilla Textures Expanded), but draws an invisible icon; the label
                            // patch draws the real one where the look puts it.
                            IconField(def) = BaseContent.ClearTex;
                            ActiveIcon = effective;
                            ActiveIconFollowsGame = true;
                        }
                        else if (custom != null) IconField(def) = custom;
                        break;
                }
                def.Worker.DoButton(rect);
            }
            finally
            {
                IconField(def) = savedIcon;
                def.iconPath = savedPath;
                ActiveAlign = LabelAlign.Default;
                ActiveTheme = null;
                ActiveButton = null;
                ActiveButtonImageDrawn = false;
                ActiveBaseState = ButtonVisualState.Normal;
                ActiveIcon = null;
                ActiveIconOnly = false;
                ActiveIconFollowsGame = false;
                ActiveBlank = false;
                ActiveFont = null;
                ActiveIconSize = -1f;
                ActiveIconAnchor = Core.IconAnchor.Default;
                ActiveIconOffset = Vector2.zero;
            }
        }

        private static void HideIcon(MainButtonDef def)
        {
            IconField(def) = null;
            def.iconPath = null;
        }

        /// <summary>The button's own icon (after other mods' patches), loading it if needed.</summary>
        public static Texture2D SafeIcon(MainButtonDef def)
        {
            if (def == null) return null;
            if (string.IsNullOrEmpty(def.iconPath)) return IconField != null ? IconField(def) : def.Icon;
            try
            {
                return def.Icon;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>The icon a button shows with its settings: the custom one, else its own.</summary>
        public static Texture2D EffectiveIcon(MainButtonDef def, in ButtonSettings settings) =>
            CustomIcons.Resolve(settings.icon) ?? SafeIcon(def);

        /// <summary>"Icon and text": icon at the left, label in the rest, both shifted under the mouse like the game's.</summary>
        internal static void DrawIconAndLabel(Rect rect, string label, Color? labelColor)
        {
            bool over = Mouse.IsOver(rect);
            var box = Core.IconPlacement.Beside(rect.x, rect.y, rect.width, rect.height, IconSizeNow, ActiveIconAnchor, B.labelSidePadding, B.iconTextGap,
                ActiveIconOffset.x, ActiveIconOffset.y, out float textLeft, out float textRight);
            var icon = new Rect(box.X, box.Y, box.Width, box.Height);
            if (over) icon.position += B.hoverLabelOffset;
            var color = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(icon, ActiveIcon);
            GUI.color = color;

            var textRect = new Rect(textLeft, rect.y, Mathf.Max(0f, textRight - textLeft), rect.height);
            var anchor = ActiveAlign switch
            {
                LabelAlign.Center => TextAnchor.MiddleCenter,
                LabelAlign.Right => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleLeft,
            };
            DrawLabel(textRect, label, labelColor ?? Color.white, anchor, over);
        }

        /// <summary>"Icon only": the icon centred, shifted under the mouse like the game's own icons.</summary>
        internal static void DrawIconOnly(Rect rect)
        {
            var box = Core.IconPlacement.Alone(rect.x, rect.y, rect.width, rect.height, IconSizeNow, ActiveIconAnchor, B.labelSidePadding,
                ActiveIconOffset.x, ActiveIconOffset.y);
            var icon = new Rect(box.X, box.Y, box.Width, box.Height);
            if (Mouse.IsOver(rect)) icon.position += B.hoverLabelOffset;
            var color = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(icon, ActiveIcon);
            GUI.color = color;
        }

        /// <summary>A subtle button (More, arrows) drawn with the theme's atlas and label colour.</summary>
        internal static bool ThemedSubtleButton(BarAppearanceWorker theme, Rect rect, string label, bool active, TextAnchor? anchor = null)
        {
            bool themed = theme != null && theme.ChangesButtons;
            var previousTheme = ActiveTheme;
            var previousButton = ActiveButton;
            var previousState = ActiveBaseState;
            var previousAlign = ActiveAlign;
            ActiveTheme = themed ? theme : null;
            ActiveButton = null;   // More and the arrows are not main buttons: no picture
            ActiveBaseState = active ? ButtonVisualState.Active : ButtonVisualState.Normal;
            if (anchor == TextAnchor.MiddleCenter) ActiveAlign = LabelAlign.Center;
            try
            {
                return Widgets.ButtonTextSubtle(rect, label, 0f, -1f, SoundDefOf.Mouseover_Category, default, null, active);
            }
            finally
            {
                ActiveTheme = previousTheme;
                ActiveButton = previousButton;
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

        /// <summary>
        /// Draws a button label at the active alignment, with the game's 2px "pressed" shift under the mouse.
        /// <paramref name="textLeftMargin"/> is the margin the caller gave the game's subtle button: room it keeps left
        /// of the label (Vanilla Textures Expanded's icon) stays free at every alignment.
        /// </summary>
        internal static void DrawAlignedLabel(Rect rect, string label, Color? labelColor, float textLeftMargin)
        {
            bool followGame = ActiveAlign == LabelAlign.Default;
            float inset = Core.LabelPlacement.LeftInset(followGame, textLeftMargin, rect.width, B.gameLabelLeftMarginFraction, B.labelSidePadding);
            var area = new Rect(rect.x + inset, rect.y, rect.width - inset, rect.height);
            var anchor = ActiveAlign switch
            {
                LabelAlign.Default or LabelAlign.Left => TextAnchor.MiddleLeft,
                LabelAlign.Right => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleCenter,
            };
            DrawLabel(area, label, labelColor ?? Color.white, anchor, Mouse.IsOver(rect));
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
                Text.Font = ActiveFont ?? B.labelFont;
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
