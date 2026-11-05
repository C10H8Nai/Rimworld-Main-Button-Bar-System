# Main Button Bar System — guide for add-on authors

Main Button Bar System is an adapter layer for RimWorld's bottom bar. Its own features are built on five
extension points ("slots"); add-ons use the same ones. Everything lives in the `MainButtonBarSystem` namespace.

Add-ons only need XML for the "button defaults" and "arrangement" slots. The other slots take a small DLL that
references `MainButtonBarSystem.dll`; add `NAI.MainButtonBarSystem` to your `modDependencies` and `loadAfter`.

What you can make, from the bar's top to its bottom:

| You make | How | Needs a DLL | Section |
|---|---|---|---|
| A mod with main buttons (race, faction, UI mod) | Say how your buttons should start; ship a theme or icons too, optionally | No | 1, "Shipping content inside another mod" |
| A mod that draws the whole bar itself | A compatibility bridge, so players choose who draws | Yes | 5 |
| A new way to lay out the bar | A display mode | Yes | 3 |
| A look | A theme | No | 4 |
| Icons | An icon pack | No | 2b |
| A ready-made arrangement | An arrangement def, or let the game write the whole mod for you | No | 2 |
| A tool that reads the bar | The query API | Yes | "Query API" |

Working examples of the XML-only kinds are in the repository's `examples/` folder.

## 1. Button defaults (XML only)

Declare how *your own* button should start out. Players can still change every value; these only replace
"not changed".

```xml
<MainButtonDef>
  <defName>MyTab</defName>
  <!-- ... -->
  <modExtensions>
    <li Class="MainButtonBarSystem.MainButtonBarDefaults">
      <inDrawer>true</inDrawer>      <!-- start in the "More" drawer (collapse modes) -->
      <pin>None</pin>                <!-- None / Left / Right -->
      <hidden>false</hidden>
      <width>0</width>               <!-- pixels; 0 follows the bar -->
      <iconMode>Default</iconMode>   <!-- Default / Text / Icon / Both -->
      <icon></icon>                  <!-- texture path, like MainButtonDef.iconPath -->
    </li>
  </modExtensions>
</MainButtonDef>
```

To set defaults on another mod's button, use a `PatchOperationAddModExtension` patch.

## 2. Arrangements (XML only)

Ship a ready-made arrangement. Players see it under **Import** in the settings window.

The easiest way: arrange the bar in game, then **Export → Create an arrangement mod**. It asks for a name,
author, description and packageId and writes a complete mod into the game's `Mods` folder: `About.xml` (depending
on this mod), the arrangement def, and the PNG icons the arrangement uses (copied into its `Textures` folder, so
subscribers get them). Restart, enable it, and upload it from the mod list.

To put an arrangement into a mod you already have, use **Export → Copy as mod XML** and save it as a `.xml` file in
your `Defs` folder (see "Shipping content inside another mod" if that mod must also work without this one).

Players can also share plain files: **Export → Save as a file** writes `<name>.xml` to
`…/RimWorld by Ludeon Studios/MainButtonBarSystem/Exports/`, and files placed there appear under **Import**.
**Copy as text** is the same file as text, for Discord and the like.

Every export carries a `meta` node beside `config`: name, author, description, game and mod version, and every
main button of the exporting game with its mod. On import, players see a report first (buttons both games have,
buttons from mods they lack grouped by mod, a missing theme or icons) and pick which parts to take: order and
placement, sizes and alignment, names, icons, theme. Buttons the exporting game did not have keep the player's own
settings. `meta` is optional; files without it import as before, and older versions of this mod ignore it.

```xml
<Defs>
  <MainButtonBarSystem.MainButtonArrangementDef>
    <defName>MyArrangement</defName>
    <label>tidy colony</label>
    <author>Me</author>
    <config>
      <formatVersion>1</formatVersion>
      <displayMode>Collapse</displayMode>
      <order>
        <li>Architect</li>
        <li>Work</li>
        <!-- ... -->
      </order>
      <overrides>
        <li>
          <defName>Factions</defName>
          <inDrawer>True</inDrawer>
        </li>
      </overrides>
    </config>
  </MainButtonBarSystem.MainButtonArrangementDef>
</Defs>
```

On import, buttons the player does not have are skipped, and buttons the arrangement does not mention go just
before the game menu button.

`config` fields: `formatVersion` (currently 2), `displayMode` (a `BarDisplayModeDef` defName), `appearance` (a
`BarAppearanceDef` defName), `order`, `defaultWidth`, `minWidth`, `gap`, `fillScreen`, `labelAlign`
(`Default` / `Left` / `Center` / `Right`), `menuLocked`, `moreButtonWidth`, `arrowWidth`, `wheelScrollPixels`,
`drawerColumns`, `drawerCellWidth`, and `overrides` (each with `defName`, `hidden`, `inDrawer`, `label`, `width`,
`pin`, `forceShow`, `iconMode`, `icon`).

Leave out any field you do not want to set. Number fields left out (or `-1`) and switches left out (or `Default`)
follow the player's tuning defaults; see below.

`meta` fields (all optional, in a def beside `config`): `name`, `author`, `description`, `gameVersion`, `modVersion`,
`exported`, `buttons` (each `li` with `defName`, `mod`, `packageId`: every button of the game it was made in, which
tells "left unchanged" apart from "not installed"), `appearanceMod`, `displayModeMod`. Without `buttons`, only the
buttons the arrangement orders or changes count as known to it.

## 2b. Icon packs (XML only)

Players can give any button another icon. The picker's **Built-in** tab lists every icon the game's buttons use,
plus the textures of every `MainButtonIconSetDef`. Ship the PNGs in your own `Textures/` folder:

```xml
<Defs>
  <MainButtonBarSystem.MainButtonIconSetDef>
    <defName>MyIcons</defName>
    <label>my icons</label>
    <icons>
      <li>MyIcons/Factions</li>   <!-- Textures/MyIcons/Factions.png -->
      <li>MyIcons/Research</li>
    </icons>
  </MainButtonBarSystem.MainButtonIconSetDef>
</Defs>
```

To set an icon as a button's default, use `<icon>` in `MainButtonBarDefaults` (section 1). Players' own PNGs are
not part of any mod: they live in `…/RimWorld by Ludeon Studios/MainButtonBarSystem/Icons/`, so Steam updates
never delete them. In configs they are stored as `file:Name.png`.

## 3. Display modes

How buttons are arranged. The built-in modes (`Vanilla`, `Collapse`, `Scroll`, `CollapseScroll`) are defs of
this type.

```xml
<MainButtonBarSystem.BarDisplayModeDef>
  <defName>MyMod_TwoRows</defName>
  <label>two rows</label>
  <description>Buttons wrap into two rows.</description>
  <workerClass>MyMod.DisplayModeWorker_TwoRows</workerClass>
  <usesDrawer>false</usesDrawer>     <!-- show the Drawer column in settings -->
  <usesMinWidth>true</usesMinWidth>  <!-- show the minimum-width slider -->
  <displayOrder>100</displayOrder>
</MainButtonBarSystem.BarDisplayModeDef>
```

```csharp
public class DisplayModeWorker_TwoRows : BarDisplayModeWorker
{
    public override void DrawBar(BarDrawContext ctx)
    {
        // ctx.Buttons: visible buttons, left to right, user settings already resolved
        //   (Def, Settings, Pin, InDrawer, Minimized, Width)
        // ctx.BarRect: the normal one-row bar area; ctx.Config: the whole-bar settings
        var rowHeight = ctx.BarRect.height;
        var top = new Rect(ctx.BarRect.x, ctx.BarRect.y - rowHeight, ctx.BarRect.width, rowHeight * 2f);
        ctx.ReportBarRect(top);              // tell queries the bar is taller
        // ... compute rects, then for each button:
        // ctx.DrawButton(button, rect);     // draws through the active appearance
        // ctx.ReportInDrawer(button);       // if you keep some buttons elsewhere
        // ctx.CaptureScrollWheel(area);     // if you scroll with the wheel
    }
}
```

`DrawBar` runs for every GUI event, like the game's own bar.

## 4. Themes (appearance) — XML and images only

A theme changes how the bar and its buttons look. Players pick one in the settings window's **Appearance** tab,
which lists every `BarAppearanceDef` from every loaded mod, with a preview. A theme needs no C#: every field is
optional, and anything left out keeps the game's look.

The theme never replaces the game's button code: buttons are still drawn by `MainButtonWorker.DoButton`, so
other mods' changes to it (icons, research progress, tutorial highlights) keep working. While this mod draws a
bar button, the game's subtle button atlas is swapped for the theme's atlas of the current state, and the label
colour is set.

### Theme mod layout

```
MyTheme/
  About/About.xml
  Defs/MyTheme_Themes.xml
  Textures/MyTheme/
    Button.png            normal button (nine-slice atlas)
    ButtonMouseover.png   under the mouse
    ButtonActive.png      pressed, or its tab is open
    ButtonDisabled.png    disabled (no map, etc.)
    Bar.png               behind the whole bar (atlas)
    More.png              the "More" button (atlas)
    ArrowLeft.png         icon on the left scroll arrow
    ArrowRight.png        icon on the right scroll arrow
    Drawer.png            behind the "More" drawer (atlas)
    Gear.png              the drawer's settings button (icon)
    Preview.png           shown in the Appearance tab
```

`About/About.xml`:

```xml
<ModMetaData>
  <name>My Theme</name>
  <author>Me</author>
  <packageId>me.mytheme</packageId>
  <supportedVersions><li>1.7</li></supportedVersions>
  <modDependencies>
    <li>
      <packageId>NAI.MainButtonBarSystem</packageId>
      <displayName>Main Button Bar System</displayName>
    </li>
  </modDependencies>
  <loadAfter><li>NAI.MainButtonBarSystem</li></loadAfter>
</ModMetaData>
```

`Defs/MyTheme_Themes.xml` (texture paths are relative to `Textures/`, without the extension):

```xml
<Defs>
  <MainButtonBarSystem.BarAppearanceDef>
    <defName>MyTheme_Steel</defName>
    <label>steel</label>
    <description>Brushed steel buttons with amber labels.</description>
    <author>Me</author>
    <displayOrder>100</displayOrder>

    <button>MyTheme/Button</button>
    <buttonMouseover>MyTheme/ButtonMouseover</buttonMouseover>
    <buttonActive>MyTheme/ButtonActive</buttonActive>
    <buttonDisabled>MyTheme/ButtonDisabled</buttonDisabled>
    <labelColor>(1, 0.8, 0.4, 1)</labelColor>
    <labelMouseoverColor>(1, 0.95, 0.7, 1)</labelMouseoverColor>

    <barBackground>MyTheme/Bar</barBackground>
    <moreButton>MyTheme/More</moreButton>
    <arrowLeft>MyTheme/ArrowLeft</arrowLeft>
    <arrowRight>MyTheme/ArrowRight</arrowRight>
    <drawerBackground>MyTheme/Drawer</drawerBackground>
    <drawerSettingsIcon>MyTheme/Gear</drawerSettingsIcon>
    <preview>MyTheme/Preview</preview>
  </MainButtonBarSystem.BarAppearanceDef>
</Defs>
```

| Field | Left out |
|---|---|
| `button` | The game's subtle button atlas |
| `buttonMouseover` | The normal atlas with the game's mouse-over tint |
| `buttonActive` | `buttonMouseover`, else the normal atlas |
| `buttonDisabled` | The normal atlas |
| `labelColor` | White. Colours are `(r, g, b, a)` from 0 to 1; alpha 0 also means "left out" |
| `labelMouseoverColor` | `labelColor` |
| `barBackground` | Nothing behind the bar (vanilla) |
| `moreButton` | Drawn like the other buttons |
| `arrowLeft`, `arrowRight` | The characters `<` and `>` on a button |
| `drawerBackground` | The game's window background |
| `drawerSettingsIcon` | This mod's gear (`MainButtonBarSystem/UI/Gear`, set in the tuning def) |
| `preview` | A preview drawn from the textures above |
| `labelActiveColor` (1.1) | `labelMouseoverColor` while pressed or the tab is open |
| `barBackgroundFit`, `drawerBackgroundFit` (1.1) | `Atlas`: the background is a nine-slice atlas. See *Illustrations* below |
| `decorations` (1.1) | No extra images. See *Illustrations* below |
| `buttonImage`, `buttonImages` (1.1) | No pictures inside the buttons. See *Pictures inside buttons* below |

**Atlases** are drawn with the game's `Widgets.DrawAtlas`, the nine-slice method of the vanilla buttons, so one
texture fits any button size:

- The texture is cut into quarters: each corner is the outer 25% × 25%, each edge the outer 25% by the middle
  50%, and the centre the middle 50% × 50%. Edges and centre are **stretched**, not tiled.
- On screen each corner is **texture width ÷ 4** pixels (UI units), but never more than half the rect's height or
  width. Only the width counts, so **the texture must be square**, or the corners are squashed vertically.
- So the texture size sets the border thickness: a 64×64 texture gives 16-pixel corners. Bar buttons are 36
  pixels tall, so corners stop growing at 18 pixels: textures larger than **72×72** only get scaled down.
- Keep all border and corner art inside the outer quarter on each side; anything in the middle half stretches.
- The image is drawn the way it looks in the PNG (top is top).

Textures that are missing are reported once in the log, and that part falls back to the game's look.

Label colours, mouse-over and active states also apply to the "More" button, the scroll arrows and the drawer.
Translate `label` and `description` in `Languages/<Language>/DefInjected/MainButtonBarSystem.BarAppearanceDef/`.

### Illustrations: whole images and decorations (API 1.1)

An atlas stretches its middle, so a picture cannot be one. Four optional additions put real illustrations on the
bar with no C#; a theme that leaves them out looks exactly as before.

**Background fit.** `barBackgroundFit` and `drawerBackgroundFit` say how `barBackground` / `drawerBackground` fill
their area:

| Value | Drawing |
|---|---|
| `Atlas` | Nine-slice, as above (default) |
| `Tile` | Repeated at its pixel size times `barBackgroundTileScale` / `drawerBackgroundTileScale` (default 1) from the top-left corner; the rest is clipped |
| `Stretch` | Scaled to fill the area, ignoring the aspect ratio: the usual choice for a wide image drawn for the bar |
| `Fit` | Scaled to fit inside the area keeping the aspect ratio, centred; the rest stays transparent |
| `Cover` | Scaled to cover the area keeping the aspect ratio, centred; what sticks out is cropped |

The bar is the screen width × 36 pixels (tuning def `behaviour.buttonRectHeight`), so an image for `Stretch` should
be wide and 36 pixels tall. The drawer's size depends on its contents.

**Decorations.** `decorations` is a list of images placed at an anchor of the bar or the drawer:

```xml
<decorations>
  <li>
    <texture>MyTheme/Portrait</texture>       <!-- required; relative to Textures/, no extension -->
    <target>Bar</target>                      <!-- Bar (default) / Drawer -->
    <anchor>Left</anchor>                     <!-- Left (default) / Center / Right -->
    <verticalAnchor>Bottom</verticalAnchor>   <!-- Bottom (default) / Center / Top -->
    <offset>(0, 0)</offset>                   <!-- pixels; x to the right, y downwards -->
    <size>(0, 0)</size>                       <!-- pixels; (0, 0): the texture's size; one axis 0: keep the aspect ratio -->
    <opacity>1</opacity>                      <!-- 0 to 1 -->
    <layer>BelowButtons</layer>               <!-- BelowButtons (default) / AboveButtons -->
    <showWhen><li>Mouseover</li></showWhen>   <!-- missing or empty: always; Normal / Mouseover / DrawerOpen, any of them -->
  </li>
</decorations>
```

Placement: the box takes its size, its anchor corner is put on the matching edge of the target (`Right` + `Bottom`
is the bottom-right corner), then `offset` is added. The bar is only 36 pixels tall, so a portrait anchored at the
bottom grows upwards out of it; that is the intended use. Bar decorations are drawn in the bar's own pass, so the
part above the bar is painted over the map and under windows. The drawer is a window and clips anything outside it.

States: `Mouseover` holds while the mouse is over the target area itself (the bar's 36-pixel rect, not the part of a
decoration that sticks out; the drawer window). `DrawerOpen` holds while the "More" drawer is open; for drawer
decorations it never holds (they are only drawn while the drawer is open), so use `Mouseover`, `Normal` or no list.
`Normal` holds when neither of the others does.

Players can adjust decorations in the Appearance tab, on top of the theme's values: show or hide them all, their
opacity, their size (anchored at the same corner), an X / Y offset, and the opacity of the background images. Each
row has a Default button; the defaults and slider ranges are in the tuning def (`barDefaults`, `ranges`).

**Label colour when active.** `labelActiveColor` applies while a button is pressed or its tab is open; left out,
the mouse-over colour, then `labelColor`, are used as before.

**Pictures inside buttons.** `buttonImage` puts a picture inside every main button; `buttonImages` gives named
buttons their own ("this button is this character, that one is that character"). Both are optional, and a button
with no picture of its own uses `buttonImage`, or none:

```xml
<buttonImage>                                 <!-- every main button; leave it out for none -->
  <texture>MyTheme/Buttons/Default</texture>  <!-- required; any size -->
</buttonImage>
<buttonImages>
  <li>
    <button>Architect</button>                <!-- required: the MainButtonDef's defName (Architect, Work, Research, a mod's button) -->
    <texture>MyTheme/Buttons/Alice</texture>  <!-- required -->
    <fit>Cover</fit>                          <!-- Cover (default) / Fit / Stretch; Atlas and Tile are drawn as Stretch -->
    <anchor>Center</anchor>                   <!-- Left / Center (default) / Right -->
    <verticalAnchor>Top</verticalAnchor>      <!-- Top / Center (default) / Bottom -->
    <opacity>1</opacity>                      <!-- 0 to 1 -->
    <inset>2</inset>                          <!-- pixels kept free inside the button's edge; default 0 -->
  </li>
</buttonImages>
```

`Cover` scales the picture to fill the button keeping its aspect ratio and crops what sticks out; the anchors say
which part is kept (`verticalAnchor` `Top` keeps the top edge, for a face). `Fit` shows the whole picture and places
it at the anchors; `Stretch` fills the button, distorting it. A later entry for the same button wins.

Drawing order, for every button on the bar and every cell of the "More" drawer, in every state (disabled too): the
picture, then the button atlas, then the label and the icon. So the atlas frames the picture: draw your button
atlases as a frame with a transparent middle (`make_theme.py`: `"hollow": true` in the palette) or the picture is
covered. The mouse-over and pressed looks stay on the frame; the picture is never tinted. With no button atlas of
its own, the theme's pictures sit under the game's (mostly opaque) button background, which shows little of them.
The More button and the scroll arrows never get a picture.

In practice:

- A bar button is 36 pixels tall and usually 33 to 120 pixels wide (wider with few buttons or a wide width
  setting). A full portrait shows only a strip: crop it first to a wide banner with the face in the middle, or use
  `verticalAnchor` `Top` (or `Center`) with `Cover`.
- The label is drawn over the picture. Keep the important part away from the middle, choose label colours that
  stay readable (`labelColor`), or tell players to pick "icon only" for those buttons.
- Use `inset` to keep the picture inside the frame's border.
- A missing picture texture draws nothing (and is reported once, see below); the button still has its frame. In
  development mode the checks also point out `buttonImages` entries for buttons that are not loaded (a typo, or a
  mod that is not active) and buttons listed twice.
- The player's decoration settings (show, opacity, size, offset) and background opacity do not affect button
  pictures.
- With `make_theme.py`'s `hollow`, the scroll arrows are hollow too (they use the `button` atlas) and show the bar
  background; they never get a picture. The More button keeps its fill.

The preview the Appearance tab draws shows the background image, the buttons and their pictures, but no decorations
(they would grow out of the card), so a theme with decorations should ship a `preview` image.

### Checks and reloading (development mode)

With development mode on, the mod checks every theme at startup and logs one warning per finding, naming the theme
and the field: textures that do not load, atlases that are not square, atlases larger than 72×72
(`behaviour.themeAtlasMaxSize`), decoration, background or button pictures with a side over 2048
(`behaviour.themeDecorationMaxSize`). A missing texture is reported once per session in total: by the check in
development mode, otherwise by the one-line warning the theme logs when it first draws (as before); it then falls
back to the game's look. Invalid values in the XML (`opacity` outside 0 to 1, a negative `size` or `inset`, a tile scale
of 0, an empty `texture`, a `buttonImages` entry with no `button`) are config errors, shown like any other def error. A `Tile` background that would need more
than `behaviour.themeTileMaxCount` tiles (4096) is stretched instead.

In development mode the Appearance tab also has a **Reload theme textures** button: it reads every theme's PNG files
again from the theme's own mod folder, so an edited image shows without restarting the game, then runs the checks
again. The textures read by the previous reload are released at the next one.

### Making a theme mod

- `docs/examples/ExampleTheme/` (in the released mod: `ThemeTemplate/ExampleTheme/`) is a complete theme mod that
  uses every field, with placeholder images: copy it, rename it, replace the images.
- `tools/make_theme.py` (released: `ThemeTemplate/tools/`) draws the ten standard textures from a JSON palette and
  writes a whole theme mod, placeholders included: `python -I -X utf8 make_theme.py MyTheme.json <output folder>`.
  The example's JSON, `ExampleTheme.json`, shows every setting. Needs Python 3 and Pillow.
- On the Workshop, add **Main Button Bar System** to the item's *Required items*: `modDependencies` in About.xml
  only tells the game, not Steam. Keep `loadAfter` as in the example.

### Compatibility promise

Theme fields are only added, never removed or given another meaning: a theme written for API 1.0 loads and looks
the same under every later 1.x. A field this mod stops reading stays declared for one release, marked in this
document. Additions raise the minor number of `MainButtonBarAPI.ApiVersion` (1.1 added the illustration fields).

### Built-in themes as examples

This mod ships six themes made exactly this way, in `Defs/MainButtonBarSystem/BarAppearances.xml` with textures in
`Textures/MainButtonBarSystem/Themes/`: `MBBS_Imperial`, `MBBS_Creed`, `MBBS_GeneLab`, `MBBS_Void` and
`MBBS_Starfarer` use atlases only; `MBBS_Nai` adds a semi-transparent portrait behind the buttons (a decoration)
and `labelActiveColor`. `docs/examples/ExampleTheme` shows every other illustration field: a stretched bar
background, decorations that appear under the mouse or in the drawer, and button pictures with hollow frames. Their textures are drawn by `tools/gen_theme_textures.py` from `tools/themes/<theme>.json`
through the same `make_theme.py` a theme mod uses.

### Effects beyond textures

Set `workerClass` to a class deriving from `BarAppearanceWorker` and override what you need:
`DrawBarBackground`, `DrawBarBackgroundImage(Rect, float opacity)`, `DrawBarOverlay`, `DrawButton`, `DrawDrawerButton`,
`DrawDrawerBackground`, `DrawDrawerBackgroundImage`, `DrawDrawerOverlay`, `DrawDecorations`, `DrawerSettingsIcon`,
`ButtonAtlas`, `ButtonImage`, `DrawButtonImage`, `LabelColor`, `DrawMoreButton`, `MoreButtonWidth`, `DrawScrollArrow`.
The theme is applied inside buttons (atlases, label colours, pictures) only when the def sets at least one button
field (a button atlas, a label colour, `buttonImage` or `buttonImages`), so a worker that chooses pictures in code
still needs one of them in its XML. Override the two-parameter
`DrawBarBackgroundImage`: the one-parameter form only forwards to it with the player's opacity, and the Appearance
tab's preview calls the two-parameter form with 1. `DrawBarBackground` is the image
plus the decorations below the buttons, and a display mode is expected to call it with `BarDrawContext.BarRect`
(the standard modes do); `DrawBarOverlay` is called with that same rect after every display mode's drawing, so
decorations above the buttons work with any display mode, and a mode that never calls `DrawBarBackground` shows
only the upper layer. Keep drawing buttons through `ButtonPainter.Draw` (or the game's `DoButton`) so other mods'
changes to buttons keep working.

## 5. Compatibility bridges

For another mod that also takes over the whole bar. A bridge detects it, lets the player choose who has
priority, and can import its settings.

```xml
<MainButtonBarSystem.CompatBridgeDef>
  <defName>MyMod_OtherBar</defName>
  <modName>Other Bar Mod</modName>
  <workerClass>MainButtonBarSystem.CompatBridgeWorker</workerClass>
  <packageIds><li>someone.otherbar</li></packageIds>
  <harmonyIds><li>someone.otherbar</li></harmonyIds>          <!-- patches to remove when this mod has priority -->
  <assemblyNames><li>OtherBar</li></assemblyNames>            <!-- for patches made without a known Harmony id -->
  <defaultPriority>Theirs</defaultPriority>
</MainButtonBarSystem.CompatBridgeDef>
```

The default worker removes the other mod's Harmony patches on `MainButtonsRoot.DoButtons`,
`MainButtonsRoot.MainButtonsOnGUI` and `MainButtonWorker.DoButton` when this mod has priority. Override for more:

| Member | Purpose |
|---|---|
| `TakeOver()` | Switch the other mod off on the bar (called once per session when this mod has priority) |
| `ReferenceValues(defName)` | Lines describing what the other mod set for one button, shown in the settings window |
| `BarReferenceValues()` | The same for the whole bar |
| `CanImport`, `ImportInto(BarConfig)` | Copy the other mod's arrangement into the player's working copy |

`OtherModSettings` reads another mod's `ModSettings` by field name without failing (missing fields give null), and
`ButtonDefaults.SetRuntimeDefault` sets a button default while the game runs. Priority changes apply after a
restart.

## Shipping content inside another mod

Themes, icon packs, arrangements and button defaults are defs and textures, and the game reads defs and textures
from every active mod. So any mod can carry them: a race mod can ship its own bar theme, and the Appearance tab
lists it with the race mod's name. Load order does not matter for them.

The one rule: if your mod must also work **without** Main Button Bar System, nothing of this mod's may load then,
or the game logs "Could not find type MainButtonBarSystem...". Pick one:

1. **A folder read only when this mod is active** (recommended for themes, since their textures come along):
   ```xml
   <!-- LoadFolders.xml in your mod's root -->
   <loadFolders>
     <v1.7>
       <li>/</li>
       <li IfModActive="NAI.MainButtonBarSystem">Compat/MainButtonBar</li>
     </v1.7>
   </loadFolders>
   ```
   Put `Defs/` and `Textures/` for this mod under `Compat/MainButtonBar/`. Without a `LoadFolders.xml` the game
   reads only your root folder, so adding one means listing `/` (and any version folders you use) yourself.
2. **`MayRequire` on a whole def**: `<MainButtonBarSystem.BarAppearanceDef MayRequire="NAI.MainButtonBarSystem">`.
   Textures it points to may sit in your normal `Textures/` folder; they are simply unused without this mod.
3. **`MayRequire` on a list item**, for button defaults on your own `MainButtonDef` (section 1).

Use texture paths under a folder of your own (`Textures/MyRaceMod/MBBS/...`): when two mods have the same path,
the one loaded last wins. If a player picks your theme and later removes your mod, the bar falls back to the
vanilla look; nothing breaks.

`examples/MBBS_Example_SoftRace` in the repository uses all three ways; `examples/MBBS_Example_ThemePack` is a
theme pack that simply requires this mod.

## Depending on this mod softly

Most add-ons should work with or without Main Button Bar System. Common ways:

- **XML on a list item**: `MayRequire` skips the item when the mod is missing.
  ```xml
  <modExtensions>
    <li Class="MainButtonBarSystem.MainButtonBarDefaults" MayRequire="NAI.MainButtonBarSystem">
      <inDrawer>true</inDrawer>
    </li>
  </modExtensions>
  ```
  (Unverified on 1.7 for a `Class=` item inside `modExtensions`; if it errors, use the next option.)
- **XML patch**: wrap `PatchOperationAddModExtension` in `PatchOperationFindMod`. Note that FindMod matches the
  mod's **name**, `Main Button Bar System`, not its packageId.
- **C#**: put the code that touches this mod in a separate assembly loaded only when it is active, through
  `LoadFolders.xml`: `<li IfModActive="NAI.MainButtonBarSystem">MBBS/</li>`. Or check
  `ModsConfig.IsActive("NAI.MainButtonBarSystem")` before calling into it.
- In `About.xml`, a hard dependency goes in `modDependencies` plus `loadAfter`. RimSort treats the `*ByVersion`
  tags as replacing, not adding to, the plain ones, so repeat entries if you use them.

## API stability

`MainButtonBarAPI.ApiVersion` (a property; currently `1.1`) covers everything in this document: the five
slots' def fields and workers, `MainButtonBarDefaults`, `MainButtonArrangementDef`, `BarDrawContext`,
`BarButton`, `BarDecoration`, `BarButtonImage`, `DecorationLayout`, `ImageFit`, `ButtonPainter.Draw` and `MainButtonBarAPI`. Additions raise the minor number; removals or changed
meanings raise the major number, and removed members stay for one release marked `[Obsolete]`. Arrangements carry
their own `formatVersion`. Anything not listed here is internal and may change.

## Harmony patches and ordering

Harmony id: `NAI.MainButtonBarSystem`. Order your patches around it with `[HarmonyBefore]` / `[HarmonyAfter]`.

| Method | Patch | Notes |
|---|---|---|
| `MainButtonsRoot.DoButtons` | prefix, `Priority.Last` | Returns false only when this mod draws the bar. If an earlier prefix of yours returns false, Harmony skips this one and the bar is yours. |
| `MainButtonsRoot.DoButtons` | postfix | Observes who drew, for `MainButtonBarAPI`; never changes anything |
| `MainButtonsRoot.HandleLowPriorityShortcuts` | postfix | Hotkeys kept by this mod, after the game's and after windows |
| `CameraDriver.CameraDriverOnGUI` | prefix | Skips only for a scroll-wheel event over a scrolling bar, so the map does not zoom |
| `Widgets.ButtonTextSubtle` | prefix + postfix | Acts only while this mod draws a bar button (label alignment, theme colour); never skips |
| `Widgets.DrawAtlas(Rect, Texture2D, bool)` | prefix + postfix | Acts only while this mod draws a bar button: draws the button's picture first (once per button), then swaps in the theme atlas; never skips |
| `PlayDataLoader.LoadAllPlayData` | postfix | After a reload (for example a language change), refreshes this mod's cached defs once loading finishes |

Compatibility bridges remove other mods' patches only when the player gives this mod priority, and log each
removal.

## Limits to know

- Buttons are always drawn through the game's `MainButtonWorker.DoButton`, so changes you make there apply.
- Exception: with "icon only" or "icon and text" chosen, the game gets the button without its icon and this mod
  draws the icon itself (inside `Widgets.ButtonTextSubtle`), so patches that move the icon or add the label back
  (Vanilla Textures Expanded's icon-and-label mode) only affect buttons left on "default".
- While the player has not set an order, the bar follows the game's `MainButtonsRoot.allButtonsInOrder`, so mods
  that reorder or add buttons at runtime are followed (at the main menu, the order recorded at startup is shown).
  Once the player sets an order, theirs wins; buttons it does not list are inserted before the menu button.
- A display mode may report a bar taller than one row (`ReportBarRect`), but the game and mods such as RimHUD
  place the inspect pane assuming the vanilla height.
- Renaming a button changes `MainButtonDef.label` for the whole game, so other mods see the new name too.
- Per-save arrangements are stored in a `GameComponent`. In Multiplayer, joining players receive the host's
  arrangement (Multiplayer has no 1.7 build yet; see docs/research).

## Tuning def

Every tunable number lives in `Defs/MainButtonBarSystem/Tuning.xml`
(`MainButtonBarSystem.MainButtonBarTuningDef`, defName `MainButtonBarTuning`); the code has no built-in values.
Sections:

| Section | Contents |
|---|---|
| `barDefaults` | Defaults of the settings players can change (widths, gap, fill, menu lock, More width, arrows, wheel speed, drawer columns and cell width) |
| `ranges` | Slider ranges in the settings window |
| `behaviour` | Fixed behaviour: bar position and height (vanilla: top 35px above the screen bottom, 36px tall), drawer padding, label padding |
| `settingsWindow` | Layout of the settings window |

Change values with an ordinary XML patch, for example:

```xml
<Operation Class="PatchOperationReplace">
  <xpath>Defs/MainButtonBarSystem.MainButtonBarTuningDef[defName="MainButtonBarTuning"]/barDefaults/minWidth</xpath>
  <value><minWidth>100</minWidth></value>
</Operation>
```

## Query API

`MainButtonBarAPI` describes the most recent bar draw, whoever drew it:

| Member | Meaning |
|---|---|
| `ButtonsOnBar` | Buttons on the bar, left to right, including ones scrolled out of view |
| `ButtonsInDrawer` | Buttons reachable only through the "More" drawer |
| `BarRect`, `BarHeight` | Bar area in GUI coordinates |
| `DrawnByThisMod` | False while the game or another mod draws the bar |
| `LayoutChanged` | Event raised after a draw in which any of the above changed |
| `OpenSettings()` | Opens the settings window |

## Save data

Each save stores its arrangement in a `GameComponent`; the default template is
`Config/MainButtonBarSystem_Template.xml`. Exported files go to
`…/RimWorld by Ludeon Studios/MainButtonBarSystem/Exports/`. The format carries `formatVersion`; this mod refuses
arrangements with a newer version than it understands.
