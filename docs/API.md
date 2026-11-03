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

## 4. Appearance

How the bar and each button are painted. Derive from `BarAppearanceWorker` and override what you need:
`DrawBarBackground`, `DrawButton`, `DrawMoreButton`, `DrawScrollArrow`, `DrawDrawerButton`. The defaults call
the game's `MainButtonWorker.DoButton`, so other mods' patches on it keep working; keep calling it unless you
really replace the whole button.

```xml
<MainButtonBarSystem.BarAppearanceDef>
  <defName>MyMod_Translucent</defName>
  <label>translucent</label>
  <workerClass>MyMod.AppearanceWorker_Translucent</workerClass>
</MainButtonBarSystem.BarAppearanceDef>
```

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
`MainButtonsRoot.MainButtonsOnGUI` and `MainButtonWorker.DoButton` when this mod has priority. Override
`TakeOver`, `ReferenceValue`, `CanImport` and `ImportInto` for more. Priority changes apply after a restart.

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
