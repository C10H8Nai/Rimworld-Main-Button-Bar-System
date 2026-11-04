#!/usr/bin/env python3
"""Builds About/Preview.png from two in-game screenshots (needs Pillow).

Top half: the problem (the vanilla bar squeezed by many mods). Bottom half: this mod (for example scrolling plus
the "More" drawer). Each half shows a cropped part of its screenshot, scaled to fit, with a caption.

Usage (from the repository root):
    python3 tools/make_preview.py BEFORE.png AFTER.png
    python3 tools/make_preview.py BEFORE.png AFTER.png --before-crop 0,1044,1920,36 --after-crop 960,780,960,300
    python3 tools/make_preview.py BEFORE.png AFTER.png --size 1280x720 --out About/Preview.png

Crops are x,y,width,height in screenshot pixels. Without one, the bottom strip of the screenshot is used: 48 px
for BEFORE (the bar) and 320 px for AFTER (the bar and an open drawer above it), at full width.

Use only screenshots of the game and of this mod's own themes; no expansion art. The result must stay under 1 MB
(Steam's limit); the script reduces colours if needed.
"""
import argparse
import io
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BACKGROUND = (30, 33, 38)
CAPTION = (225, 225, 215)
ACCENT = (226, 208, 109)
MAX_BYTES = 1_000_000
FONTS = [
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    "C:/Windows/Fonts/segoeuib.ttf",
    "C:/Windows/Fonts/arialbd.ttf",
    "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
]


def font(size):
    for path in FONTS:
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def parse_crop(text):
    parts = [int(p) for p in text.split(",")]
    if len(parts) != 4 or parts[2] <= 0 or parts[3] <= 0:
        raise argparse.ArgumentTypeError("crop must be x,y,width,height with positive width and height")
    return parts


def parse_size(text):
    w, h = (int(p) for p in text.lower().split("x"))
    return w, h


def crop(img, box, default_height):
    if box is None:
        h = min(default_height, img.height)
        box = [0, img.height - h, img.width, h]
    x, y, w, h = box
    return img.crop((x, y, x + w, y + h))


def fit(img, w, h):
    scale = min(w / img.width, h / img.height)
    size = (max(1, round(img.width * scale)), max(1, round(img.height * scale)))
    return img.resize(size, Image.LANCZOS)


def panel(canvas, img, caption, top, height, pad):
    d = ImageDraw.Draw(canvas)
    size = max(12, canvas.height // 20)
    f = font(size)
    while size > 10 and d.textlength(caption, font=f) > canvas.width - 2 * pad:  # shrink long captions to fit
        size -= 1
        f = font(size)
    d.text((pad, top + pad // 2), caption, font=f, fill=CAPTION)
    caption_h = f.getbbox(caption)[3] + pad
    area_w, area_h = canvas.width - 2 * pad, height - caption_h - pad
    shot = fit(img, area_w, area_h)
    x = (canvas.width - shot.width) // 2
    y = top + caption_h + (area_h - shot.height) // 2
    canvas.paste(shot, (x, y))
    d.rectangle([x - 1, y - 1, x + shot.width, y + shot.height], outline=ACCENT)


def save_small(img, path):
    for colours in (None, 256, 128, 64):
        out = img if colours is None else img.quantize(colours, method=Image.Quantize.MEDIANCUT)
        buffer = io.BytesIO()
        out.save(buffer, "PNG", optimize=True)
        if buffer.tell() < MAX_BYTES:
            with open(path, "wb") as f:
                f.write(buffer.getvalue())
            return buffer.tell(), colours
    raise SystemExit("Could not get the preview under 1 MB; use a smaller --size or tighter crops.")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("before", help="screenshot of the vanilla bar with many buttons")
    parser.add_argument("after", help="screenshot of the bar with this mod")
    parser.add_argument("--before-crop", type=parse_crop)
    parser.add_argument("--after-crop", type=parse_crop)
    parser.add_argument("--before-caption", default="Vanilla: dozens of buttons squeezed")
    parser.add_argument("--after-caption", default="Main Button Bar System: scroll, More drawer, your order")
    parser.add_argument("--size", type=parse_size, default=(640, 360), help="WxH, keep 16:9 (default 640x360)")
    parser.add_argument("--split", type=float, default=0.38, help="share of the height for the top half")
    parser.add_argument("--out", default=os.path.join(ROOT, "About", "Preview.png"))
    args = parser.parse_args()

    before = crop(Image.open(args.before).convert("RGB"), args.before_crop, 48)
    after = crop(Image.open(args.after).convert("RGB"), args.after_crop, 320)

    w, h = args.size
    pad = max(6, h // 40)
    canvas = Image.new("RGB", (w, h), BACKGROUND)
    top_h = round(h * args.split)
    panel(canvas, before, args.before_caption, 0, top_h, pad)
    panel(canvas, after, args.after_caption, top_h, h - top_h, pad)

    size, colours = save_small(canvas, args.out)
    note = "" if colours is None else f", reduced to {colours} colours"
    print(f"{args.out}: {w}x{h}, {size / 1024:.0f} KB{note}")


if __name__ == "__main__":
    sys.exit(main())
