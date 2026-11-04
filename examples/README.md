# Example mods

Two small mods that show (and let you test) how other mods ship content for Main Button Bar System. They are
not part of the release (`tools/make_release.py` leaves this folder out). To try them, copy both folders into
RimWorld's `Mods` folder.

| Folder | Stands for | Depends on Main Button Bar System |
|---|---|---|
| `MBBS_Example_SoftRace` | A race (or any) mod that adds its own main button and *also* supports Main Button Bar System | No: works with or without it |
| `MBBS_Example_ThemePack` | An add-on that only makes sense with Main Button Bar System (a theme pack) | Yes: `modDependencies` |

`MBBS_Example_SoftRace` uses all three ways of depending softly (see `docs/API.md`, "Shipping content inside
another mod"):

1. `LoadFolders.xml` reads `Compat/MainButtonBar/` only when Main Button Bar System is active. The theme, its
   textures and the icon pack live there.
2. `MayRequire` on a list item: the button's `MainButtonBarDefaults` extension (`Defs/MainButtons.xml`).
3. `MayRequire` on a whole def: the arrangement (`Defs/Arrangement.xml`).

## What to check

With Main Button Bar System + both examples:
- No errors in the log.
- Appearance tab: "leaf folk (example)" and "ember (example)", each card naming its mod.
- Import menu: "leaf folk layout (example) (by NAI)"; importing it shows the report and switches to Collapse,
  the leaf folk theme and the gold leaf icon on the "leaf folk" button.
- Icon picker, Built-in tab: the two leaf icons.
- In a collapse mode, the "leaf folk" button starts in the More drawer and shows icon and text (its defaults).

With only `MBBS_Example_SoftRace` (Main Button Bar System disabled):
- No errors (no "Could not find type" or "MayRequire" messages); the "leaf folk" button is on the vanilla bar.

`MBBS_Example_ThemePack` without Main Button Bar System is flagged by the mod list as missing a dependency, as
intended.

The textures are drawn by `tools/gen_example_textures.py`.
