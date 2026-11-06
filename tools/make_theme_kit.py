#!/usr/bin/env python3
## 檔案路徑：tools/make_theme_kit.py
##
## 說明：產生「新增主題」用的空白主題素材 ThemeKit/（repo 根目錄，遊戲不會載入；隨模組發佈）。
##   外觀分頁按「新增主題…」時，本模組把這裡的圖複製到新主題模組的 Textures/<Id>/，Def 範本替換佔位字串後寫進 Defs/。
##   圖的規格見 docs/superpowers/specs/2026-11-07-theme-projects-design.md §5.1：
##   - 按鈕四種狀態、More、Drawer：64×64 上色九宮格（四角／四邊／中間各一種深淺，切線在 1/4＝16px），中間半透明。
##   - Bar：256×36，Tile 鋪滿，格線＋左右接縫記號。
##   - ButtonPicture：240×72，準心、X、外框，加上按鈕寬 120／80／40 時 Cover 裁切後的可見範圍。
##   - ArrowLeft／ArrowRight 32×32、Gear 64×64：輪廓。
##   - Preview（Textures 內，主題卡片用）600×36；About/Preview.png 640×360；_Guide.png 說明圖。
##
## 用法（在 repo 根目錄執行；需要 Pillow）：
##   RimClaude\tools\.venv_vector\Scripts\python.exe -I -X utf8 tools/make_theme_kit.py [--preview <png>]
##   --preview 另外輸出一張模擬底部列的預覽圖（不進 repo，給人目視檢查）。
##
## 版本：V1.0（2026-11-07）

import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
KIT = os.path.join(ROOT, "ThemeKit")

ATLAS = 64          # 九宮格貼圖邊長
CUT = ATLAS // 4    # 角＝貼圖寬 1/4
BAR_H = 36          # 底部列高（Tuning behaviour.buttonRectHeight）

FRAME = 3           # 九宮格最外圈的實色外框（px）
CORNER_ALPHA = 150  # 角、邊、中間的不透明度：由深到淺，插圖從框裡透出來
EDGE_ALPHA = 95

# 四種狀態各一色調：(角, 邊, 中間 RGB, 中間 alpha, 角上的字)
STATES = {
    "Button": ((34, 78, 178), (66, 122, 222), (150, 188, 250), 45, "N"),
    "ButtonMouseover": ((26, 132, 62), (56, 180, 96), (150, 228, 170), 45, "H"),
    "ButtonActive": ((196, 98, 16), (238, 146, 46), (255, 204, 140), 45, "A"),
    "ButtonDisabled": ((78, 78, 78), (122, 122, 122), (186, 186, 186), 45, "X"),
    "More": ((104, 42, 156), (150, 86, 204), (206, 166, 240), 120, "+"),
    "Drawer": ((104, 42, 156), (150, 86, 204), (60, 40, 80), 215, "W"),
}

STATE_NAMES = {
    "Button": "normal", "ButtonMouseover": "mouse over", "ButtonActive": "open / pressed",
    "ButtonDisabled": "disabled", "More": "More button", "Drawer": "drawer window",
}


def font(size):
    return ImageFont.load_default(size=size)


def text_center(draw, box, text, size, fill):
    f = font(size)
    l, t, r, b = draw.textbbox((0, 0), text, font=f)
    x = box[0] + (box[2] - box[0] - (r - l)) / 2 - l
    y = box[1] + (box[3] - box[1] - (b - t)) / 2 - t
    draw.text((x, y), text, font=f, fill=fill)


def dashed_line(draw, xy0, xy1, fill, dash=3):
    (x0, y0), (x1, y1) = xy0, xy1
    if x0 == x1:
        for y in range(y0, y1 + 1, dash * 2):
            draw.line([(x0, y), (x0, min(y + dash - 1, y1))], fill=fill)
    else:
        for x in range(x0, x1 + 1, dash * 2):
            draw.line([(x, y0), (min(x + dash - 1, x1), y0)], fill=fill)


def nine_slice_atlas(corner, edge, center, center_alpha, letter):
    """
    上色九宮格：角、邊、中間三種深淺，切線（虛線）畫在 1/4 處，左上角寫狀態字母。
    36px 高的按鈕上，上下兩條邊各佔 16px、中間只剩 4px，所以角和邊也要半透明（只有最外 3px 是實色外框），
    插圖才看得到。
    """
    img = Image.new("RGBA", (ATLAS, ATLAS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    hi = ATLAS - CUT
    d.rectangle([CUT, CUT, hi - 1, hi - 1], fill=center + (center_alpha,))
    for box in ([CUT, 0, hi - 1, CUT - 1], [CUT, hi, hi - 1, ATLAS - 1], [0, CUT, CUT - 1, hi - 1], [hi, CUT, ATLAS - 1, hi - 1]):
        d.rectangle(box, fill=edge + (EDGE_ALPHA,))
    for box in ([0, 0, CUT - 1, CUT - 1], [hi, 0, ATLAS - 1, CUT - 1], [0, hi, CUT - 1, ATLAS - 1], [hi, hi, ATLAS - 1, ATLAS - 1]):
        d.rectangle(box, fill=corner + (CORNER_ALPHA,))
    # 實色外框
    for i in range(FRAME):
        d.rectangle([i, i, ATLAS - 1 - i, ATLAS - 1 - i], outline=corner + (255,))
    # 切線：角和邊、邊和中間的界線
    line = (255, 255, 255, 210)
    for x in (CUT, hi - 1):
        dashed_line(d, (x, 0), (x, ATLAS - 1), line)
    for y in (CUT, hi - 1):
        dashed_line(d, (0, y), (ATLAS - 1, y), line)
    d.rectangle([0, 0, ATLAS - 1, ATLAS - 1], outline=(10, 10, 10, 255))
    text_center(d, (0, 0, CUT, CUT), letter, 12, (255, 255, 255, 255))
    return img


def bar_tile():
    w = 256
    img = Image.new("RGBA", (w, BAR_H), (28, 32, 40, 210))
    d = ImageDraw.Draw(img)
    for x in range(0, w, 16):
        d.line([(x, 0), (x, BAR_H - 1)], fill=(64, 72, 86, 255))
    for y in range(0, BAR_H, 12):
        d.line([(0, y), (w - 1, y)], fill=(64, 72, 86, 255))
    # 接縫記號：左右兩端的黃色三角，鋪開後相鄰兩塊在接縫處拼成菱形
    seam = (240, 200, 40, 255)
    for y0 in (0, BAR_H - 8):
        d.polygon([(0, y0), (5, y0 + 4), (0, y0 + 8)], fill=seam)
        d.polygon([(w - 1, y0), (w - 6, y0 + 4), (w - 1, y0 + 8)], fill=seam)
    d.text((22, 2), "Bar.png 256x36 Tile", font=font(10), fill=(150, 160, 176, 255))
    return img


def button_picture():
    w, h = 240, 72
    img = Image.new("RGBA", (w, h), (52, 56, 68, 255))
    d = ImageDraw.Draw(img)
    d.line([(0, 0), (w - 1, h - 1)], fill=(120, 126, 140, 255))
    d.line([(0, h - 1), (w - 1, 0)], fill=(120, 126, 140, 255))
    # 按鈕寬 120／80／40（高 36）時 Cover 縮成 0.5 倍，看得到的是中間寬 240／160／80 的部分
    for width, label, color in ((240, "120", (240, 200, 40)), (160, "80", (60, 210, 230)), (80, "40", (240, 90, 200))):
        x0 = (w - width) // 2
        d.rectangle([x0, 0, x0 + width - 1, h - 1], outline=color + (255,), width=2)
        d.text((x0 + 4, h - 16), label, font=font(12), fill=color + (255,))
    cx, cy = w // 2, h // 2
    red = (235, 60, 50, 255)
    d.line([(cx - 14, cy), (cx + 14, cy)], fill=red, width=2)
    d.line([(cx, cy - 14), (cx, cy + 14)], fill=red, width=2)
    d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], outline=red, width=2)
    d.text((6, 3), "ButtonPicture.png 240x72", font=font(10), fill=(170, 176, 190, 255))
    return img


def arrow(left):
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pts = [(22, 6), (8, 16), (22, 26)] if left else [(10, 6), (24, 16), (10, 26)]
    d.polygon(pts, fill=(255, 255, 255, 60), outline=(255, 255, 255, 255))
    d.line(pts + [pts[0]], fill=(255, 255, 255, 255), width=2)
    return img


def gear():
    import math
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = cy = 32
    pts = []
    teeth = 8
    for i in range(teeth * 4):
        a = 2 * math.pi * i / (teeth * 4)
        r = 26 if (i % 4) in (1, 2) else 19
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(pts, fill=(255, 255, 255, 50), outline=(255, 255, 255, 255))
    d.line(pts + [pts[0]], fill=(255, 255, 255, 255), width=2)
    d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], outline=(255, 255, 255, 255), width=2)
    return img


def draw_nine_slice(dst, src, box):
    """像遊戲的 Widgets.DrawAtlas：角＝貼圖寬 1/4，但不超過目標高、寬的一半；邊和中間拉伸。"""
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    c = int(min(src.width / 4, w / 2, h / 2))
    s = src.width // 4
    n = src.width
    pieces = [
        ((0, 0, s, s), (x0, y0, c, c)), ((n - s, 0, n, s), (x1 - c, y0, c, c)),
        ((0, n - s, s, n), (x0, y1 - c, c, c)), ((n - s, n - s, n, n), (x1 - c, y1 - c, c, c)),
        ((s, 0, n - s, s), (x0 + c, y0, w - 2 * c, c)), ((s, n - s, n - s, n), (x0 + c, y1 - c, w - 2 * c, c)),
        ((0, s, s, n - s), (x0, y0 + c, c, h - 2 * c)), ((n - s, s, n, n - s), (x1 - c, y0 + c, c, h - 2 * c)),
        ((s, s, n - s, n - s), (x0 + c, y0 + c, w - 2 * c, h - 2 * c)),
    ]
    for crop, (px, py, pw, ph) in pieces:
        if pw <= 0 or ph <= 0:
            continue
        part = src.crop(crop).resize((pw, ph), Image.BILINEAR)
        dst.alpha_composite(part, (px, py))


def draw_cover(dst, src, box):
    """Cover、置中：縮放到蓋滿，裁掉超出的部分（按鈕插圖的預設）。"""
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    scale = max(w / src.width, h / src.height)
    rw, rh = max(1, round(src.width * scale)), max(1, round(src.height * scale))
    resized = src.resize((rw, rh), Image.BILINEAR)
    left, top = (rw - w) // 2, (rh - h) // 2
    dst.alpha_composite(resized.crop((left, top, left + w, top + h)), (x0, y0))


def tile(dst, src, box):
    x0, y0, x1, y1 = box
    for x in range(x0, x1, src.width):
        part = src.crop((0, 0, min(src.width, x1 - x), min(src.height, y1 - y0)))
        dst.alpha_composite(part, (x, y0))


def label(dst, box, text, color=(255, 255, 255, 255), size=12):
    text_center(ImageDraw.Draw(dst), box, text, size, color)


def bar_mock(tex, width, buttons):
    """底部列示意：buttons 是 (狀態檔名, 寬, 文字, 有沒有插圖) 的清單，前後加左右箭頭。"""
    img = Image.new("RGBA", (width, BAR_H), (0, 0, 0, 0))
    tile(img, tex["Bar"], (0, 0, width, BAR_H))
    x = 0
    seq = [("Button", 24, None, False, "ArrowLeft")] + [b + (None,) for b in buttons] + [("Button", 24, None, False, "ArrowRight")]
    for state, w, text, picture, icon in seq:
        box = (x, 0, x + w, BAR_H)
        if picture:
            draw_cover(img, tex["ButtonPicture"], box)
        draw_nine_slice(img, tex[state], box)
        if icon:
            ic = tex[icon].resize((24, 24), Image.BILINEAR)
            img.alpha_composite(ic, (x + (w - 24) // 2, 6))
        elif text and state != "ButtonDisabled":
            color = {"ButtonMouseover": (255, 255, 153, 255), "ButtonActive": (255, 217, 128, 255)}.get(state, (255, 255, 255, 255))
            label(img, box, text, color)
        x += w
    return img


def guide(tex):
    W, H = 1280, 900
    img = Image.new("RGBA", (W, H), (22, 24, 30, 255))
    d = ImageDraw.Draw(img)
    white, grey, yellow = (240, 240, 240, 255), (170, 176, 190, 255), (240, 200, 40, 255)
    d.text((24, 16), "Blank theme: which file paints which part of the bar", font=font(26), fill=white)
    d.text((24, 52), "Edit the PNGs in Textures/<Id>/ with any image editor and save. With the Appearance tab open, the bar updates "
                     "within a second (or press Reload on the theme card).", font=font(14), fill=grey)

    # 底部列示意（2 倍）
    mock = bar_mock(tex, 600, [("Button", 120, "Normal", True), ("ButtonMouseover", 120, "Mouse over", True),
                               ("ButtonActive", 120, "Open", True), ("ButtonDisabled", 80, "", True), ("More", 88, "More (3)", False)])
    big = mock.resize((1200, 72), Image.NEAREST)
    img.alpha_composite(big, (40, 110))
    callouts = [
        (40 + 24 * 2, "ArrowLeft.png 32x32"), (40 + 48 + 120, "Button.png (normal)"), (40 + 48 + 360, "ButtonMouseover.png"),
        (40 + 48 + 600, "ButtonActive.png"), (40 + 48 + 840 - 40, "ButtonDisabled.png"), (40 + 48 + 1000 + 40, "More.png"),
    ]
    for i, (x, text) in enumerate(callouts):
        y = 190 + (i % 2) * 22
        d.line([(x, 182), (x, y)], fill=yellow)
        d.text((x - 20, y + 2), text, font=font(13), fill=yellow)
    d.text((40, 236), "Behind everything: Bar.png (256x36, tiled across the screen).  Under each button's frame: ButtonPicture.png (Cover, centred).",
           font=font(13), fill=grey)
    d.text((40, 256), "Drawer.png: the window that opens from More.  Gear.png: the drawer's settings button.  ArrowRight.png: the right scroll arrow.",
           font=font(13), fill=grey)

    # 九宮格說明（Button.png 放大 4 倍）
    y0 = 290
    d.text((40, y0), "Button.png, ButtonMouseover.png, ButtonActive.png, ButtonDisabled.png, More.png, Drawer.png: 64x64 nine-slice",
           font=font(16), fill=white)
    big_atlas = tex["Button"].resize((256, 256), Image.NEAREST)
    img.alpha_composite(big_atlas, (40, y0 + 32))
    notes = [
        "Corners (the outer 16x16 px): drawn as they are, never stretched.",
        "Edges: stretched along the button's length or height.",
        "Middle (32x32): stretched over the rest of the button.",
        "A bar button is only 36 px tall: the top and bottom edges take 16 px each,",
        "  so draw the frame thin and keep corners, edges and middle see-through",
        "  where the ButtonPicture under them should show.",
        "Keep frame art inside the outer quarter; 64x64 is the right size",
        "  (larger only gets scaled down on a 36 px tall button). Must be square.",
        "The letter in the corner tells the states apart: N normal, H mouse over,",
        "  A open / pressed, X disabled, + More, W drawer window.",
    ]
    for i, n in enumerate(notes):
        d.text((320, y0 + 40 + i * 24), n, font=font(14), fill=grey)

    # 插圖說明
    y1 = y0 + 310
    d.text((40, y1), "ButtonPicture.png: 240x72 (2x the bar height), drawn with Cover in every button", font=font(16), fill=white)
    img.alpha_composite(tex["ButtonPicture"].resize((480, 144), Image.NEAREST), (40, y1 + 32))
    pnotes = [
        "Cover fills the button and crops the rest. The framed areas stay visible",
        "when a button is 120 / 80 / 40 px wide; keep the important part in the middle.",
        "The label is drawn on top of the picture.",
        "To give one button its own picture, add buttonImages to the Def",
        "  (see API.md, \"Pictures inside buttons\").",
    ]
    for i, n in enumerate(pnotes):
        d.text((540, y1 + 40 + i * 24), n, font=font(14), fill=grey)

    d.text((40, H - 70), "Label colours are in Defs/<Id>_Theme.xml: labelColor, labelMouseoverColor, labelActiveColor (r, g, b, a from 0 to 1).",
           font=font(14), fill=grey)
    d.text((40, H - 44), "This file (_Guide.png) and MBBS_ThemeProject.xml are not loaded by the game. Delete MBBS_ThemeProject.xml only if "
                         "you no longer want Main Button Bar System to manage this folder.", font=font(14), fill=grey)
    return img


def about_preview(tex):
    img = Image.new("RGBA", (640, 360), (26, 30, 38, 255))
    d = ImageDraw.Draw(img)
    text_center(d, (0, 40, 640, 110), "New theme", 44, (240, 240, 240, 255))
    text_center(d, (0, 110, 640, 150), "for Main Button Bar System", 20, (170, 176, 190, 255))
    mock = bar_mock(tex, 600, [("Button", 150, "Normal", True), ("ButtonMouseover", 150, "Mouse over", True),
                               ("ButtonActive", 150, "Open", True), ("More", 102, "More", False)])
    img.alpha_composite(mock, (20, 220))
    text_center(d, (0, 290, 640, 330), "Replace this picture with your own (About/Preview.png).", 14, (130, 136, 150, 255))
    return img


DEF_TEMPLATE = """<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- Made with Main Button Bar System's "New theme". Texture paths are relative to Textures/, without ".png". -->
  <!-- Pictures for named buttons (buttonImages) and decorations: see API.md, "Adding a theme in the game". -->
  <MainButtonBarSystem.BarAppearanceDef>
    <defName>{{ID}}_Theme</defName>
    <label>{{LABEL}}</label>
    <description>{{DESCRIPTION}}</description>
    <author>{{AUTHOR}}</author>
    <!-- Position in the Appearance tab's list (the built-in themes use 0 to 200). -->
    <displayOrder>{{DISPLAY_ORDER}}</displayOrder>

    <!-- Main buttons, nine-slice 64x64: normal, under the mouse, open or pressed, disabled. -->
    <button>{{ID}}/Button</button>
    <buttonMouseover>{{ID}}/ButtonMouseover</buttonMouseover>
    <buttonActive>{{ID}}/ButtonActive</buttonActive>
    <buttonDisabled>{{ID}}/ButtonDisabled</buttonDisabled>

    <!-- Label colours (r, g, b, a from 0 to 1): normal, under the mouse, open or pressed. -->
    <labelColor>(1, 1, 1, 1)</labelColor>
    <labelMouseoverColor>(1, 1, 0.6, 1)</labelMouseoverColor>
    <labelActiveColor>(1, 0.85, 0.5, 1)</labelActiveColor>

    <!-- Behind the whole bar: Tile repeats the image; Atlas / Stretch / Fit / Cover are the other choices. -->
    <barBackground>{{ID}}/Bar</barBackground>
    <barBackgroundFit>Tile</barBackgroundFit>

    <!-- The window that opens from the More button (nine-slice). -->
    <drawerBackground>{{ID}}/Drawer</drawerBackground>
    <drawerBackgroundFit>Atlas</drawerBackgroundFit>

    <!-- The More button (nine-slice). -->
    <moreButton>{{ID}}/More</moreButton>

    <!-- Icons on the scroll arrows (scrolling display modes). -->
    <arrowLeft>{{ID}}/ArrowLeft</arrowLeft>
    <arrowRight>{{ID}}/ArrowRight</arrowRight>

    <!-- Icon of the drawer's settings button. -->
    <drawerSettingsIcon>{{ID}}/Gear</drawerSettingsIcon>

    <!-- Picture on the theme's card in the Appearance tab. -->
    <preview>{{ID}}/Preview</preview>

    <!-- Picture inside every main button, under its frame (keep the frame's middle semi-transparent). -->
    <buttonImage>
      <texture>{{ID}}/ButtonPicture</texture>
      <fit>Cover</fit>
      <anchor>Center</anchor>
      <verticalAnchor>Center</verticalAnchor>
    </buttonImage>
  </MainButtonBarSystem.BarAppearanceDef>
</Defs>
"""


def build():
    tex = {}
    for name, (corner, edge, center, alpha, letter) in STATES.items():
        tex[name] = nine_slice_atlas(corner, edge, center, alpha, letter)
    tex["Bar"] = bar_tile()
    tex["ButtonPicture"] = button_picture()
    tex["ArrowLeft"] = arrow(True)
    tex["ArrowRight"] = arrow(False)
    tex["Gear"] = gear()
    tex["Preview"] = bar_mock(tex, 600, [("Button", 140, "Normal", True), ("ButtonMouseover", 140, "Mouse over", True),
                                         ("ButtonActive", 140, "Open", True), ("More", 132, "More", False)])
    return tex


EXPECTED = {
    "Button": (64, 64), "ButtonMouseover": (64, 64), "ButtonActive": (64, 64), "ButtonDisabled": (64, 64),
    "More": (64, 64), "Drawer": (64, 64), "Bar": (256, 36), "ButtonPicture": (240, 72),
    "ArrowLeft": (32, 32), "ArrowRight": (32, 32), "Gear": (64, 64), "Preview": (600, 36),
}


def main():
    parser = argparse.ArgumentParser(description="Generate ThemeKit/ (blank theme files for \"New theme\").")
    parser.add_argument("--preview", help="also write a mock bar for checking by eye (not part of the kit)")
    args = parser.parse_args()

    tex = build()
    os.makedirs(os.path.join(KIT, "Textures"), exist_ok=True)
    os.makedirs(os.path.join(KIT, "About"), exist_ok=True)
    os.makedirs(os.path.join(KIT, "Defs"), exist_ok=True)
    for name, image in tex.items():
        image.save(os.path.join(KIT, "Textures", name + ".png"), optimize=True)
    about_preview(tex).convert("RGB").save(os.path.join(KIT, "About", "Preview.png"), optimize=True)
    guide(tex).convert("RGB").save(os.path.join(KIT, "_Guide.png"), optimize=True)
    with open(os.path.join(KIT, "Defs", "Theme.xml.template"), "w", encoding="utf-8", newline="\n") as f:
        f.write(DEF_TEMPLATE)

    # 讀回檢查尺寸
    bad = []
    for name, size in EXPECTED.items():
        with Image.open(os.path.join(KIT, "Textures", name + ".png")) as im:
            if im.size != size:
                bad.append(f"{name}: {im.size} != {size}")
    with Image.open(os.path.join(KIT, "About", "Preview.png")) as im:
        if im.size != (640, 360):
            bad.append(f"About/Preview: {im.size}")
    if bad:
        print("Size mismatch:\n  " + "\n  ".join(bad))
        return 1
    print(f"ThemeKit written: {len(EXPECTED)} textures, About/Preview.png, _Guide.png, Defs/Theme.xml.template")

    if args.preview:
        rows = []
        for widths in ((120, 120, 120, 120), (80, 80, 80, 80), (40, 40, 40, 40)):
            rows.append(bar_mock(tex, 600, [("Button", widths[0], "Normal", True), ("ButtonMouseover", widths[1], "Hover", True),
                                            ("ButtonActive", widths[2], "Open", True), ("ButtonDisabled", widths[3], "", True),
                                            ("More", 70, "More", False)]))
        sheet = Image.new("RGBA", (600 * 2, (BAR_H * 2 + 12) * len(rows)), (60, 60, 60, 255))
        for i, row in enumerate(rows):
            sheet.alpha_composite(row.resize((1200, 72), Image.NEAREST), (0, i * (BAR_H * 2 + 12)))
        sheet.convert("RGB").save(args.preview)
        print("Preview: " + args.preview)
    return 0


if __name__ == "__main__":
    sys.exit(main())
