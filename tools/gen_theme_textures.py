#!/usr/bin/env python3
"""Draws the built-in themes' textures and the mod icon (needs Pillow).

Usage (from the repository root):
    python3 tools/gen_theme_textures.py

Writes Textures/MainButtonBarSystem/Themes/<Theme>/*.png and About/ModIcon.png. Everything is drawn from the
palettes below, so tweak a colour and run again. Nothing is taken from the game's or any DLC's art: the colours
were measured from the expansions' logos and art and the game's data (docs/research/dlc-visuals-2026-11-04.md),
and the shapes are simple corner ornaments.

Atlases follow the game's nine-slice rules (docs/API.md section 4): square 64x64, so corners are 16px on screen,
with all border art inside the outer 16px.
"""
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Textures", "MainButtonBarSystem", "Themes")
N = 64          # atlas size
Q = N // 4      # outer quarter: border art must stay inside this

# Palettes measured from the official logos and art and from the game's data (docs/research/dlc-visuals-2026-11-04.md).
# base: button fill; edge: main border; outline: dark line outside the border (or None); rule: thin inner line;
# accent: hover/active underline; icon: arrows and gear (defaults to edge); tile: gene-tile corner colour (defaults
# to rule); rivet: hull rivet colour (defaults to a lighter edge); ornament: corner style (corners only: the game
# stretches the middle of each edge).
THEMES = {
    # Royalty: navy body, gold double hairline (logo gold), ivory labels, psy-teal underline, gold finials.
    "Imperial": dict(base=(36, 42, 48), edge=(226, 208, 109), outline=None, rule=(150, 136, 70), accent=(189, 247, 204),
                     active=(48, 56, 66), ornament="finial", double=True),
    # Ideology: deep teal body, heavy black outline with coral border (logo coral), teal inner rule, gold roundels.
    "Creed": dict(base=(32, 64, 70), edge=(224, 100, 98), outline=(8, 8, 10), rule=(57, 111, 115), accent=(218, 162, 33),
                  active=(44, 84, 90), ornament="roundel"),
    # Biotech: teal-blue body, teal border (#66B0B5, from the expansion's preview art), deep red gene-tile corners
    # (#A84457) and a deep red rule on the open button.
    "GeneLab": dict(base=(24, 56, 68), edge=(102, 176, 181), outline=None, rule=(60, 112, 120), accent=(150, 220, 224),
                    active=(30, 70, 84), ornament="genetab", tile=(168, 68, 87), active_rule=(168, 68, 87)),
    # Anomaly: void black body, sage border, arrows and gear (#AEBC7E, the expansion's main colour); red only as a
    # thin inset line, glowing and underlined under the mouse.
    "Void": dict(base=(17, 19, 17), edge=(174, 188, 126), outline=None, rule=(150, 44, 44), accent=(196, 58, 54),
                 active=(30, 34, 26), ornament="shard", glow_on_hover=True),
    # Odyssey: deep space-purple body, purple hull border, arrows and gear (#9074BB, from the expansion's preview art),
    # cyan rivets and thruster underline as the second colour.
    "Starfarer": dict(base=(30, 26, 44), edge=(144, 116, 187), outline=None, rule=(84, 68, 112), accent=(80, 230, 236),
                      active=(44, 36, 66), ornament="hull", icon=(176, 150, 222), rivet=(90, 210, 220)),
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


def corners():
    for cx, cy in ((0, 0), (N - 1, 0), (0, N - 1), (N - 1, N - 1)):
        yield cx, cy, (1 if cx < N / 2 else -1), (1 if cy < N / 2 else -1)


def cut_corners(img, size):
    """Transparent 45-degree cuts in the corners (hull plates, shards)."""
    px = img.load()
    for cx, cy, sx, sy in corners():
        for i in range(size):
            for j in range(size - i):
                px[cx + sx * i, cy + sy * j] = (0, 0, 0, 0)


def ornament(d, theme, color):
    """Corner decoration, always inside the outer quarter."""
    style = theme["ornament"]
    for cx, cy, sx, sy in corners():
        if style == "finial":
            x = cx + 6 * sx
            if sy == 1:  # crown point and finial dot on the top corners
                d.polygon([(x - 3, cy + 9), (x, cy + 3), (x + 3, cy + 9)], fill=color)
                d.ellipse([x - 1.5, cy + 1, x + 1.5, cy + 4], fill=color)
            else:
                d.ellipse([x - 1.5, cy - 6, x + 1.5, cy - 3], fill=color)
        elif style == "roundel":
            x, y = cx + 7 * sx, cy + 7 * sy
            d.ellipse([x - 4, y - 4, x + 4, y + 4], fill=(8, 8, 10, 255))
            d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=color)
            d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=theme["accent"] + (255,))
        elif style == "genetab":
            x, y = cx + 4 * sx, cy + 4 * sy
            d.rectangle([min(x, x + 5 * sx), min(y, y + 5 * sy), max(x, x + 5 * sx), max(y, y + 5 * sy)],
                        fill=theme.get("tile", theme["rule"]) + (255,))
        elif style == "shard":
            d.line([(cx + 7 * sx, cy), (cx, cy + 7 * sy)], fill=theme["rule"] + (255,), width=1)
            d.line([(cx + 11 * sx, cy + 2 * sy), (cx + 2 * sx, cy + 11 * sy)], fill=shade(theme["edge"], 20) + (255,), width=1)
        elif style == "hull":
            rivet = theme.get("rivet", shade(theme["edge"], 50)) + (255,)
            x, y = cx + 9 * sx, cy + 4 * sy
            d.ellipse([x - 1.5, y - 1.5, x + 1.5, y + 1.5], fill=rivet)
            x, y = cx + 4 * sx, cy + 9 * sy
            d.ellipse([x - 1.5, y - 1.5, x + 1.5, y + 1.5], fill=rivet)


def hover_glow(img, theme, inner, state):
    """A red glow that is plain to see at the bar's size: a bright inset line, its blurred halo, and red haze rising
    from the bottom edge. Everything stays inside the outer quarter, which the game draws at 1:1 on a 36px button."""
    hot = shade(theme["rule"], 70 if state == "active" else 55)
    halo = Image.new("RGBA", (N, N))
    ImageDraw.Draw(halo).rectangle([inner, inner, N - 1 - inner, N - 1 - inner], outline=hot + (255,), width=3)
    halo = halo.filter(ImageFilter.GaussianBlur(2.5))
    img = Image.alpha_composite(img, halo)
    img = Image.alpha_composite(img, halo)
    haze = Image.new("RGBA", (N, N))
    hd = ImageDraw.Draw(haze)
    bottom = N - 1 - inner
    rows = Q - inner - 2
    peak = 150 if state == "active" else 120
    for i in range(rows):  # strongest at the bottom line, fading upwards
        hd.line([(inner, bottom - i), (N - 1 - inner, bottom - i)], fill=hot + (int(peak * (1 - i / rows) ** 1.6),))
    img = Image.alpha_composite(img, haze)
    d = ImageDraw.Draw(img)
    d.rectangle([inner + 1, inner + 1, N - 2 - inner, N - 2 - inner], outline=hot + (255,), width=1)
    d.line([(inner + 1, bottom - 1), (N - 2 - inner, bottom - 1)], fill=shade(theme["accent"], 40) + (255,), width=3)
    return img


def button(theme, fill, edge, state="normal"):
    img = Image.new("RGBA", (N, N))
    gradient_fill(img, shade(fill, 10), shade(fill, -10))
    d = ImageDraw.Draw(img)
    style = theme["ornament"]
    o = 0
    if theme.get("outline"):
        d.rectangle([0, 0, N - 1, N - 1], outline=theme["outline"] + (255,), width=1)
        o = 1
    width = 3 if state == "active" else 2
    if theme.get("double"):
        d.rectangle([o, o, N - 1 - o, N - 1 - o], outline=edge + (255,), width=1)
        d.rectangle([o + 2, o + 2, N - 3 - o, N - 3 - o], outline=edge + (255,), width=1 if state != "active" else 2)
        inner = o + 5
    else:
        d.rectangle([o, o, N - 1 - o, N - 1 - o], outline=edge + (255,), width=width)
        inner = o + width + 1
    if style in ("roundel", "shard"):
        d.rectangle([inner + 1, inner + 1, N - 2 - inner, N - 2 - inner], outline=theme["rule"] + (255,), width=1)
    if state == "active" and theme.get("active_rule"):
        d.rectangle([inner + 1, inner + 1, N - 2 - inner, N - 2 - inner], outline=theme["active_rule"] + (255,), width=1)
    if state in ("mouseover", "active"):
        # Accent underline: psy teal, gold, gene blue, red glow or thruster cyan.
        y = N - 1 - inner - 1
        d.line([(inner + 1, y), (N - 2 - inner, y)], fill=theme["accent"] + (255,), width=2)
    glow_states = ("mouseover", "active") if theme.get("glow_on_hover") else ("normal", "mouseover", "active")
    if style == "shard" and state in glow_states:
        if theme.get("glow_on_hover"):
            img = hover_glow(img, theme, inner, state)
        else:
            glow = Image.new("RGBA", (N, N))
            ImageDraw.Draw(glow).rectangle([inner, inner, N - 1 - inner, N - 1 - inner], outline=theme["rule"] + (180 if state == "normal" else 255,), width=2)
            img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(1.5)))
        d = ImageDraw.Draw(img)
    ornament(d, theme, (edge if state != "disabled" else grey(edge)) + (255,))
    if style in ("shard", "hull"):
        cut_corners(img, 4 if style == "hull" else 3)
        d = ImageDraw.Draw(img)
        for cx, cy, sx, sy in corners():  # edge along the cut
            n = 4 if style == "hull" else 3
            d.line([(cx + n * sx, cy), (cx, cy + n * sy)], fill=edge + (255,), width=2)
    return img


def bar(theme):
    img = Image.new("RGBA", (N, N))
    gradient_fill(img, shade(theme["base"], -6), shade(theme["base"], -18))
    d = ImageDraw.Draw(img)
    d.line([(0, 0), (N - 1, 0)], fill=theme["edge"] + (255,), width=2)
    d.line([(0, 3), (N - 1, 3)], fill=theme["rule"] + (255,), width=1)
    return img


def drawer(theme):
    img = Image.new("RGBA", (N, N))
    gradient_fill(img, shade(theme["base"], -2), shade(theme["base"], -16))
    d = ImageDraw.Draw(img)
    if theme.get("outline"):
        d.rectangle([0, 0, N - 1, N - 1], outline=theme["outline"] + (255,), width=1)
    d.rectangle([1, 1, N - 2, N - 2], outline=theme["edge"] + (255,), width=2)
    d.rectangle([5, 5, N - 6, N - 6], outline=theme["rule"] + (255,), width=1)
    ornament(d, theme, theme["edge"] + (255,))
    return img


def arrow(theme, left):
    S = 4
    img = Image.new("RGBA", (32 * S, 32 * S))
    d = ImageDraw.Draw(img)
    pts = [(20, 7), (10, 16), (20, 25)] if left else [(12, 7), (22, 16), (12, 25)]
    pts = [(x * S, y * S) for x, y in pts]
    d.line(pts, fill=(8, 8, 10, 230), width=7 * S, joint="curve")
    d.line(pts, fill=theme.get("icon", theme["edge"]) + (255,), width=4 * S, joint="curve")
    return img.resize((32, 32), Image.LANCZOS)


def tinted_gear(theme):
    gear = Image.open(os.path.join(ROOT, "Textures", "MainButtonBarSystem", "UI", "Gear.png")).convert("RGBA")
    accent = theme.get("icon", theme["edge"])
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
            "ButtonMouseover": button(t, shade(t["base"], 14), shade(t["edge"], 25), "mouseover"),
            "ButtonActive": button(t, t["active"], shade(t["edge"], 35), "active"),
            "ButtonDisabled": button(t, grey(shade(t["base"], -4)), grey(shade(t["edge"], -60)), "disabled"),
            "Bar": bar(t),
            "More": button(t, mix(t["base"], t["active"], 0.6), t["edge"]),
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
