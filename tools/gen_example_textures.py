#!/usr/bin/env python3
"""Draws the textures of the example mods in examples/ (needs Pillow), with the same shapes as the built-in themes.

Usage (from the repository root):
    python3 tools/gen_example_textures.py
"""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_theme_textures as g  # noqa: E402

ROOT = g.ROOT
EXAMPLES = os.path.join(ROOT, "examples")

# A race mod's own theme (soft dependency) and an add-on theme pack (hard dependency).
THEMES = {
    os.path.join(EXAMPLES, "MBBS_Example_SoftRace", "Compat", "MainButtonBar", "Textures", "MBBSExample", "SoftRace", "Theme"):
        dict(base=(28, 46, 30), edge=(176, 140, 76), outline=(10, 12, 8), rule=(70, 104, 64), accent=(214, 186, 96),
             active=(38, 62, 40), ornament="roundel"),
    os.path.join(EXAMPLES, "MBBS_Example_ThemePack", "Textures", "MBBSExample", "ThemePack", "Ember"):
        dict(base=(34, 30, 30), edge=(232, 128, 52), outline=None, rule=(120, 64, 36), accent=(255, 196, 92),
             active=(52, 40, 36), ornament="finial", double=True),
}


def textures(t):
    return {
        "Button": g.button(t, t["base"], t["edge"]),
        "ButtonMouseover": g.button(t, g.shade(t["base"], 14), g.shade(t["edge"], 25), "mouseover"),
        "ButtonActive": g.button(t, t["active"], g.shade(t["edge"], 35), "active"),
        "ButtonDisabled": g.button(t, g.grey(g.shade(t["base"], -4)), g.grey(g.shade(t["edge"], -60)), "disabled"),
        "Bar": g.bar(t),
        "More": g.button(t, g.mix(t["base"], t["active"], 0.6), t["edge"]),
        "Drawer": g.drawer(t),
        "ArrowLeft": g.arrow(t, True),
        "ArrowRight": g.arrow(t, False),
        "Gear": g.tinted_gear(t),
    }


def leaf_icon(color):
    """The example race button's icon: a simple leaf, 64x64 white-on-transparent like the game's icons."""
    S = 4
    img = Image.new("RGBA", (64 * S, 64 * S))
    d = ImageDraw.Draw(img)
    d.ellipse([14 * S, 8 * S, 50 * S, 56 * S], fill=(8, 8, 10, 230))
    d.ellipse([17 * S, 11 * S, 47 * S, 53 * S], fill=color + (255,))
    d.line([(32 * S, 14 * S), (32 * S, 60 * S)], fill=(8, 8, 10, 255), width=3 * S)
    return img.resize((64, 64), Image.LANCZOS)


def main():
    for folder, t in THEMES.items():
        os.makedirs(folder, exist_ok=True)
        for name, img in textures(t).items():
            img.save(os.path.join(folder, name + ".png"), optimize=True)
        print(os.path.relpath(folder, ROOT))
    # The race button's icon is a normal texture (always loaded); the extra icon pack lives in the compat folder.
    plain = os.path.join(EXAMPLES, "MBBS_Example_SoftRace", "Textures", "MBBSExample", "SoftRace")
    os.makedirs(plain, exist_ok=True)
    leaf_icon((230, 230, 230)).save(os.path.join(plain, "RaceTab.png"), optimize=True)
    pack = os.path.join(EXAMPLES, "MBBS_Example_SoftRace", "Compat", "MainButtonBar", "Textures", "MBBSExample", "SoftRace", "Icons")
    os.makedirs(pack, exist_ok=True)
    leaf_icon((176, 140, 76)).save(os.path.join(pack, "LeafGold.png"), optimize=True)
    leaf_icon((120, 190, 110)).save(os.path.join(pack, "LeafGreen.png"), optimize=True)
    print("icons")


if __name__ == "__main__":
    main()
