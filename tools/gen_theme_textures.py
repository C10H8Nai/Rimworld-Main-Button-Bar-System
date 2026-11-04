#!/usr/bin/env python3
"""Draws the built-in themes' textures and the mod icon (needs Pillow).

Usage (from the repository root):
    python3 tools/gen_theme_textures.py

Writes Textures/MainButtonBarSystem/Themes/<Theme>/*.png and About/ModIcon.png. Everything is drawn from the
palettes below, so tweak a colour and run again. Nothing is taken from the game's or any DLC's art; the themes
only borrow each expansion's general colour mood.

Atlases follow the game's nine-slice rules (docs/API.md section 4): square 64x64, so corners are 16px on screen,
with all border art inside the outer 16px.
"""
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Textures", "MainButtonBarSystem", "Themes")
N = 64          # atlas size
Q = N // 4      # outer quarter: border art must stay inside this

# base: button fill; edge: border/accent; active: fill of an open/pressed button; ornament: corner style.
THEMES = {
    "Imperial": dict(base=(54, 32, 78), edge=(204, 166, 78), active=(96, 60, 124), ornament="diamond"),
    "Creed": dict(base=(60, 45, 33), edge=(166, 116, 62), active=(96, 68, 42), ornament="dot"),
    "GeneLab": dict(base=(20, 50, 48), edge=(92, 214, 174), active=(30, 84, 76), ornament="bracket"),
    "Void": dict(base=(24, 17, 20), edge=(156, 34, 44), active=(64, 20, 27), ornament="glow"),
    "Starfarer": dict(base=(19, 28, 50), edge=(112, 154, 206), active=(32, 54, 96), ornament="chamfer"),
}


def shade(c, d):
    return tuple(max(0, min(255, v + d)) for v in c[:3])


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def grey(c, t=0.6):
    g = int(sum(c[:3]) / 3)
    return mix(c, (g, g, g), t)


def gradient_fill(img, top, bottom):
    d = ImageDraw.Draw(img)
    for y in range(N):
        d.line([(0, y), (N - 1, y)], fill=mix(top, bottom, y / (N - 1)) + (255,))


def ornament(d, style, color, inset):
    """Corner decoration, kept inside the outer quarter."""
    for cx, cy in ((inset, inset), (N - 1 - inset, inset), (inset, N - 1 - inset), (N - 1 - inset, N - 1 - inset)):
        sx = 1 if cx < N / 2 else -1
        sy = 1 if cy < N / 2 else -1
        if style == "diamond":
            x, y = cx + 4 * sx, cy + 4 * sy
            d.polygon([(x, y - 3), (x + 3, y), (x, y + 3), (x - 3, y)], fill=color)
        elif style == "dot":
            x, y = cx + 4 * sx, cy + 4 * sy
            d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=color)
        elif style == "bracket":
            d.line([(cx, cy), (cx + 8 * sx, cy)], fill=color, width=2)
            d.line([(cx, cy), (cx, cy + 8 * sy)], fill=color, width=2)
        elif style == "chamfer":
            d.line([(cx, cy + 6 * sy), (cx + 6 * sx, cy)], fill=color, width=2)


def button(theme, fill, edge, border=2, inner=None):
    img = Image.new("RGBA", (N, N))
    gradient_fill(img, shade(fill, 10), shade(fill, -10))
    d = ImageDraw.Draw(img)
    style = theme["ornament"]
    if style == "chamfer":
        # Cut corners: transparent triangles, then the edge along the cut.
        for i in range(5):
            for (x, y) in ((i, 0), (0, i), (N - 1 - i, 0), (N - 1, i), (i, N - 1), (0, N - 1 - i), (N - 1 - i, N - 1), (N - 1, N - 1 - i)):
                for k in range(5 - i):
                    px = x + (k if x < N / 2 else -k) if y in (0, N - 1) else x
                    py = y + (k if y < N / 2 else -k) if x in (0, N - 1) else y
                    img.putpixel((px, py), (0, 0, 0, 0))
    d.rectangle([0, 0, N - 1, N - 1], outline=edge + (255,), width=border)
    if inner:
        d.rectangle([border + 2, border + 2, N - 3 - border, N - 3 - border], outline=inner + (255,), width=1)
    if style == "glow":
        glow = Image.new("RGBA", (N, N))
        gd = ImageDraw.Draw(glow)
        gd.rectangle([1, 1, N - 2, N - 2], outline=edge + (200,), width=3)
        glow = glow.filter(ImageFilter.GaussianBlur(2))
        img = Image.alpha_composite(img, glow)
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, N - 1, N - 1], outline=edge + (255,), width=1)
    else:
        ornament(d, style, edge + (255,), border + 1)
    return img


def bar(theme):
    img = Image.new("RGBA", (N, N))
    gradient_fill(img, shade(theme["base"], -8), shade(theme["base"], -22))
    d = ImageDraw.Draw(img)
    d.line([(0, 0), (N - 1, 0)], fill=theme["edge"] + (255,), width=2)
    d.line([(0, 3), (N - 1, 3)], fill=shade(theme["edge"], -70) + (255,), width=1)
    return img


def drawer(theme):
    img = Image.new("RGBA", (N, N))
    gradient_fill(img, shade(theme["base"], -4), shade(theme["base"], -20))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, N - 1, N - 1], outline=theme["edge"] + (255,), width=2)
    d.rectangle([4, 4, N - 5, N - 5], outline=shade(theme["edge"], -80) + (255,), width=1)
    if theme["ornament"] != "glow":
        ornament(d, theme["ornament"], theme["edge"] + (255,), 3)
    return img


def arrow(theme, left):
    S = 4
    img = Image.new("RGBA", (32 * S, 32 * S))
    d = ImageDraw.Draw(img)
    pts = [(20, 7), (10, 16), (20, 25)] if left else [(12, 7), (22, 16), (12, 25)]
    pts = [(x * S, y * S) for x, y in pts]
    d.line(pts, fill=(10, 10, 10, 220), width=7 * S, joint="curve")
    d.line(pts, fill=shade(theme["edge"], 30) + (255,), width=4 * S, joint="curve")
    return img.resize((32, 32), Image.LANCZOS)


def tinted_gear(theme):
    gear = Image.open(os.path.join(ROOT, "Textures", "MainButtonBarSystem", "UI", "Gear.png")).convert("RGBA")
    accent = shade(theme["edge"], 30)
    px = gear.load()
    for y in range(gear.height):
        for x in range(gear.width):
            r, g, b, a = px[x, y]
            if a and r > 128:  # the light body; keep the dark outline
                k = r / 255
                px[x, y] = (int(accent[0] * k), int(accent[1] * k), int(accent[2] * k), a)
    return gear


def mod_icon():
    S = 4
    size = 128
    img = Image.new("RGBA", (size * S, size * S))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, size * S - 1, size * S - 1], radius=22 * S, fill=(34, 38, 46, 255))
    # A small bar of buttons, one highlighted, and the More drawer above it.
    y0 = 82 * S
    colors = [(64, 72, 84), (76, 110, 150), (64, 72, 84)]
    for i, c in enumerate(colors):
        x0 = (12 + i * 36) * S
        d.rectangle([x0, y0, x0 + 32 * S, y0 + 22 * S], fill=c + (255,), outline=(110, 118, 128, 255), width=2 * S)
    d.rectangle([60 * S, 30 * S, 116 * S, 76 * S], fill=(46, 52, 62, 255), outline=(120, 128, 138, 255), width=2 * S)
    for r in range(2):
        for c in range(2):
            x0, y = (64 + c * 26) * S, (34 + r * 20) * S
            d.rectangle([x0, y, x0 + 22 * S, y + 16 * S], fill=(64, 72, 84, 255))
    img = img.resize((size, size), Image.LANCZOS)
    gear = Image.open(os.path.join(ROOT, "Textures", "MainButtonBarSystem", "UI", "Gear.png")).convert("RGBA").resize((46, 46), Image.LANCZOS)
    img.alpha_composite(gear, (10, 22))
    return img


def main():
    for name, t in THEMES.items():
        folder = os.path.join(OUT, name)
        os.makedirs(folder, exist_ok=True)
        files = {
            "Button": button(t, t["base"], t["edge"]),
            "ButtonMouseover": button(t, shade(t["base"], 18), shade(t["edge"], 35)),
            "ButtonActive": button(t, t["active"], shade(t["edge"], 45), border=3, inner=shade(t["edge"], -20)),
            "ButtonDisabled": button(t, grey(shade(t["base"], -6)), grey(shade(t["edge"], -60))),
            "Bar": bar(t),
            "More": button(t, mix(t["base"], t["active"], 0.5), t["edge"], inner=shade(t["edge"], -40)),
            "Drawer": drawer(t),
            "ArrowLeft": arrow(t, True),
            "ArrowRight": arrow(t, False),
            "Gear": tinted_gear(t),
        }
        for file, img in files.items():
            img.save(os.path.join(folder, file + ".png"), optimize=True)
        print(f"{name}: {len(files)} textures")
    mod_icon().save(os.path.join(ROOT, "About", "ModIcon.png"), optimize=True)
    print("About/ModIcon.png")


if __name__ == "__main__":
    main()
