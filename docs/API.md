# Main Button Bar System — guide for add-on authors

Main Button Bar System is an adapter layer for RimWorld's bottom bar. Its own features are built on five
extension points ("slots"); add-ons use the same ones. Everything lives in the `MainButtonBarSystem` namespace.

Add-ons only need XML for the "button defaults" and "arrangement" slots. The other slots take a small DLL that
references `MainButtonBarSystem.dll`; add `NAI.MainButtonBarSystem` to your `modDependencies` and `loadAfter`.

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

The easiest way to write one: arrange the bar in game, then **Export → Copy as mod XML**, paste it into a file
under your mod's `Defs` folder and change `defName` and `label`.

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

### Built-in themes as examples

This mod ships five themes made exactly this way: `MBBS_Imperial`, `MBBS_Creed`, `MBBS_GeneLab`, `MBBS_Void` and
`MBBS_Starfarer` in `Defs/MainButtonBarSystem/BarAppearances.xml`, with textures in
`Textures/MainButtonBarSystem/Themes/`. Their textures are drawn by `tools/gen_theme_textures.py` (Pillow) from a
few colours per theme, which is a quick way to start a theme of your own.

### Effects beyond textures

Set `workerClass` to a class deriving from `BarAppearanceWorker` and override what you need:
`DrawBarBackground`, `DrawButton`, `DrawDrawerButton`, `DrawDrawerBackground`, `DrawerSettingsIcon`, `ButtonAtlas`,
`LabelColor`, `DrawMoreButton`, `MoreButtonWidth`, `DrawScrollArrow`. Keep drawing buttons through `ButtonPainter.Draw` (or
the game's `DoButton`) so other mods' changes to buttons keep working.

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

`MainButtonBarAPI.ApiVersion` (a property; currently `1.0`) covers everything in this document: the five
slots' def fields and workers, `MainButtonBarDefaults`, `MainButtonArrangementDef`, `BarDrawContext`,
`BarButton`, `ButtonPainter.Draw` and `MainButtonBarAPI`. Additions raise the minor number; removals or changed
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
| `Widgets.DrawAtlas(Rect, Texture2D, bool)` | prefix + postfix | Acts only while this mod draws a bar button (theme atlas); never skips |
| `PlayDataLoader.LoadAllPlayData` | postfix | After a reload (for example a language change), refreshes this mod's cached defs once loading finishes |

Compatibility bridges remove other mods' patches only when the player gives this mod priority, and log each
removal.

## Limits to know

- Buttons are always drawn through the game's `MainButtonWorker.DoButton`, so changes you make there apply.
- The order on screen comes from the player's settings and the game's defs, not from
  `MainButtonsRoot.allButtonsInOrder`; mods that rewrite that list do not change this mod's order (an import is
  planned instead).
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
