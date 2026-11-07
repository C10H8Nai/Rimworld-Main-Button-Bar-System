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
##   - Preview（Textures 內，主題卡片用）600×36；About/Preview.png 640×360。
##   - _Guide_<語言>.png：給第一次做主題的人的一圖流說明書，英文、繁中、簡中、日文各一張（字型用 Windows 內建字型）。
##
## 用法（在 repo 根目錄執行；需要 Pillow）：
##   RimClaude\tools\.venv_vector\Scripts\python.exe -I -X utf8 tools/make_theme_kit.py [--preview <png>]
##   --preview 另外輸出一張模擬底部列的預覽圖（不進 repo，給人目視檢查）。
##
## 版本：V1.1（2026-11-07）：說明圖改成四種語言的一圖流說明書（_Guide_<語言>.png）

import argparse
import os
import re
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


def font(size):
    return ImageFont.load_default(size=size)


def text_center(draw, box, text, size, fill, f=None):
    f = f or font(size)
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


# ---------------------------------------------------------------- 一圖流說明書（_Guide_<語言>.png）
#
# 「新增主題」把這四張圖放進新資料夾的最上層，給第一次做主題的人：一張圖看懂流程、哪張圖畫哪裡、九宮格、插圖、
# 文字顏色、資料夾內容、上傳之後。檔名要和 Core/ThemeProjectNames.GuideFiles 一致（單元測試會檢查）。
# 字型用 Windows 內建字型（GUIDE_FONTS），所以這支要在 Windows 上跑。

FONT_DIR = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts")
GUIDE_FONTS = {   # 語言: (內文, 粗體)
    "English": ("segoeui.ttf", "segoeuib.ttf"),
    "ChineseTraditional": ("msjh.ttc", "msjhbd.ttc"),
    "ChineseSimplified": ("msyh.ttc", "msyhbd.ttc"),
    "Japanese": ("YuGothM.ttc", "YuGothB.ttc"),
}
MONO_FONT = "consola.ttf"
GUIDE_LANGS = ["English", "ChineseTraditional", "ChineseSimplified", "Japanese"]   # 順序同 ThemeProjectNames.GuideFiles

G_W = 1600                 # 圖寬
G_M = 48                   # 左右邊界
G_BG = (22, 24, 30, 255)
G_PANEL = (33, 36, 46, 255)
G_WHITE = (242, 243, 246, 255)
G_BODY = (214, 218, 228, 255)
G_DIM = (150, 157, 172, 255)
G_ACCENT = (240, 200, 40, 255)
G_FILE = (110, 205, 235, 255)

GUIDE_TEXT = {
    "ChineseTraditional": {
        "title": "主題說明書：三步做出你的主題",
        "subtitle": "這個資料夾就是你的主題，也是一個模組。改裡面的圖片、存檔，遊戲底部的按鈕列就會跟著變。不用寫程式，也不用重開遊戲。",
        "steps": [
            ("改圖", "用繪圖軟體打開 Textures 裡那個資料夾的 PNG，改完存檔。檔名不要改。要畫半透明，請用支援透明的軟體（Krita、GIMP 等；小畫家不行）。"),
            ("馬上看到", "Main Button Bar System 的設定視窗開在「外觀」分頁（上方選「預設主題」）、選著這個主題時，大約 1 秒就換成新圖；也可以按卡片下面的「重新載入」。記得按視窗下面的「儲存」，主題才會一直選著。"),
            ("啟用、分享", "滿意後在模組清單啟用它、重開遊戲，它就是一般的主題模組。可以用遊戲內建的上傳按鈕傳到 Steam 工作坊。"),
        ],
        "a_title": "哪張圖畫哪裡",
        "mock": ["一般", "滑鼠移上", "按下", "", "更多 (3)"],
        "callouts": ["左箭頭", "一般", "滑鼠移上去", "按下、分頁開著", "不能用", "「更多」按鈕", "右箭頭"],
        "frames_row": "按鈕框（九宮格，見下面）",
        "rows": ["整條列的背景，橫向重複鋪滿", "每顆按鈕框底下的插圖", "按「更多」打開的抽屜", "抽屜裡的設定按鈕",
                 "捲動用的左右箭頭", "「外觀」分頁卡片上的預覽", "工作坊的預覽圖"],
        "b_title": "按鈕框怎麼畫：九宮格",
        "b_caps": ["64×64 的原圖（放大）", "畫在 36 px 高的按鈕上（放大）"],
        "b_points": [
            "同一張 64×64 的圖會套在任何大小的按鈕上：圖被切成九塊。",
            "1 = 四個角（各 16×16）：照原樣畫，不會拉伸。",
            "2 = 四條邊：被拉長。　3 = 中間：被拉長、填滿剩下的地方。",
            "花紋、邊框、角飾都畫在外圈（1 和 2 的外側）；放在中間的花紋會被拉到變形。",
            "底部列的按鈕只有 36 px 高，上下兩條邊就各佔 16 px：框畫細，中間留透明，下面的插圖才看得到。",
            "圖要是正方形；比 72×72 大沒有用，只會被縮小。",
            "角落的字母只是標記（N 一般、H 滑鼠移上、A 按下、X 不能用、+ 更多、W 抽屜），直接蓋掉就好。",
        ],
        "c_title": "按鈕裡的插圖 ButtonPicture.png（240×72）",
        "c_points": [
            "鋪滿每顆按鈕，多出來的部分裁掉（置中）。按鈕的文字和圖示畫在插圖上面。",
            "框起來的範圍＝按鈕寬 120、80、40 px 時還看得到的部分：重點放中間。",
            "不想要插圖：刪掉 Defs 檔裡 <buttonImage> 到 </buttonImage> 那一段。",
        ],
        "d_title": "文字顏色",
        "d_intro": "在 Defs 資料夾的 .xml 檔裡（用記事本打開）：",
        "d_points": [
            "labelColor 一般、labelMouseoverColor 滑鼠移上、labelActiveColor 按下。",
            "括號裡是（紅, 綠, 藍, 不透明度），每個數字 0～1。例如改成 (1, 0.8, 0.4, 1) 就是琥珀色。",
            "存檔後一樣會自動讀取（啟用成模組之前）。",
        ],
        "e_title": "資料夾裡有什麼",
        "tree": ["要改的圖（PNG）", "設定：文字顏色、用哪些圖", "模組名稱和說明", "工作坊的預覽圖",
                 "這份說明書（遊戲不讀，可以留著）", "讓 Main Button Bar System 認得這個資料夾（遊戲不讀）"],
        "f_title": "上傳和之後",
        "f_points": [
            "上傳後，到工作坊頁面的「必要項目」（Required items）加上 Main Button Bar System：About.xml 寫的前置只有遊戲看得到。",
            "啟用成模組之後：改 PNG 照樣會自動讀取（或按「重新載入」）；改 Defs 要重開遊戲。",
            "不要了：卡片下面的「刪除」把資料夾搬到存檔資料夾的 MainButtonBarSystem\\DeletedThemes\\，搬回 Mods 就能找回。",
            "模組清單勾選著、或這次開遊戲已當成模組載入的主題不能刪：先取消勾選、重開遊戲。",
        ],
        "footer": "更詳細的說明：Main Button Bar System 模組資料夾裡的 ThemeGuide.zh-TW.md；每顆按鈕不同的圖、裝飾圖這些進階欄位見 API.md。",
    },
    "ChineseSimplified": {
        "title": "主题说明书：三步做出你的主题",
        "subtitle": "这个文件夹就是你的主题，也是一个模组。修改里面的图片并保存，游戏底部的按钮栏就会跟着变。不用写代码，也不用重启游戏。",
        "steps": [
            ("改图", "用绘图软件打开 Textures 里那个文件夹中的 PNG，改完保存。文件名不要改。要画半透明，请用支持透明的软件（Krita、GIMP 等；画图不行）。"),
            ("马上看到", "Main Button Bar System 的设置窗口停在「外观」标签页（上方选「预设主题」）、选中这个主题时，大约 1 秒就换成新图；也可以点卡片下面的「重新加载」。记得点窗口下面的「保存」，主题才会一直保持选中。"),
            ("启用、分享", "满意后在 模组列表中启用它并重启游戏，它就是普通的主题模组了。可以用游戏内置的上传按钮传到 Steam 创意工坊。"),
        ],
        "a_title": "哪张图画哪里",
        "mock": ["普通", "鼠标移上", "按下", "", "更多 (3)"],
        "callouts": ["左箭头", "普通", "鼠标移上去", "按下、标签页打开", "不能用", "「更多」按钮", "右箭头"],
        "frames_row": "按钮框（九宫格，见下面）",
        "rows": ["整条栏的背景，横向重复铺满", "每个按钮框下面的插图", "点「更多」打开的抽屉", "抽屉里的设置按钮",
                 "滚动用的左右箭头", "「外观」标签页卡片上的预览", "创意工坊的预览图"],
        "b_title": "按钮框怎么画：九宫格",
        "b_caps": ["64×64 的原图（放大）", "画在 36 px 高的按钮上（放大）"],
        "b_points": [
            "同一张 64×64 的图会套在任何大小的按钮上：图被切成九块。",
            "1 = 四个角（各 16×16）：按原样画，不会拉伸。",
            "2 = 四条边：被拉长。　3 = 中间：被拉长、填满剩下的地方。",
            "花纹、边框、角饰都画在外圈（1 和 2 的外侧）；放在中间的花纹会被拉到变形。",
            "底部栏的按钮只有 36 px 高，上下两条边就各占 16 px：框画细，中间留透明，下面的插图才看得到。",
            "图要是正方形；比 72×72 大没有用，只会被缩小。",
            "角上的字母只是标记（N 普通、H 鼠标移上、A 按下、X 不能用、+ 更多、W 抽屉），直接盖掉就好。",
        ],
        "c_title": "按钮里的插图 ButtonPicture.png（240×72）",
        "c_points": [
            "铺满每个按钮，多出来的部分裁掉（居中）。按钮的文字和图标画在插图上面。",
            "框起来的范围＝按钮宽 120、80、40 px 时还看得到的部分：重点放中间。",
            "不想要插图：删掉 Defs 文件里 <buttonImage> 到 </buttonImage> 那一段。",
        ],
        "d_title": "文字颜色",
        "d_intro": "在 Defs 文件夹的 .xml 文件里（用记事本打开）：",
        "d_points": [
            "labelColor 普通、labelMouseoverColor 鼠标移上、labelActiveColor 按下。",
            "括号里是（红, 绿, 蓝, 不透明度），每个数字 0～1。例如改成 (1, 0.8, 0.4, 1) 就是琥珀色。",
            "保存后同样会自动读取（启用成模组之前）。",
        ],
        "e_title": "文件夹里有什么",
        "tree": ["要改的图（PNG）", "设置：文字颜色、用哪些图", "模组名称和说明", "创意工坊的预览图",
                 "这份说明书（游戏不读，可以留着）", "让 Main Button Bar System 认出这个文件夹（游戏不读）"],
        "f_title": "上传和之后",
        "f_points": [
            "上传后，到创意工坊页面的「必要项目」（Required items）里加上 Main Button Bar System：About.xml 写的前置只有游戏看得到。",
            "启用成模组之后：改 PNG 照样会自动读取（或点「重新加载」）；改 Defs 要重启游戏。",
            "不要了：卡片下面的「删除」把文件夹移到存档文件夹的 MainButtonBarSystem\\DeletedThemes\\，移回 Mods 就能找回。",
            "模组列表里勾选着、或这次启动游戏已当成模组加载的主题不能删：先取消勾选、重启游戏。",
        ],
        "footer": "更详细的说明：Main Button Bar System 模组文件夹里的 ThemeGuide.zh-CN.md；每个按钮不同的图、装饰图这些进阶字段见 API.md。",
    },
    "Japanese": {
        "title": "テーマ説明書：3ステップでテーマを作る",
        "subtitle": "このフォルダがあなたのテーマで、そのまま1つの Mod でもあります。中の画像を編集して保存すると、ゲーム下部のボタンバーがそれに合わせて変わります。プログラミングも再起動も不要です。",
        "steps": [
            ("画像を編集", "Textures の中にあるフォルダの PNG を画像編集ソフトで開き、編集して保存します。ファイル名は変えないでください。半透明を描くなら透明対応のソフト（Krita、GIMP など。ペイントは不可）を使います。"),
            ("すぐに確認", "Main Button Bar System の設定ウィンドウで「外観」タブ（上で「プリセット」を選択）を開き、このテーマを選んでいると、約1秒で新しい画像に替わります。カード下の「再読み込み」でもすぐ反映できます。ウィンドウ下の「保存」を押すと、テーマが選ばれたままになります。"),
            ("有効化して公開", "気に入ったら MODリストで有効にしてゲームを再起動すれば、普通のテーマ Mod になります。ゲーム内蔵のアップロードボタンで Steam Workshop に公開できます。"),
        ],
        "a_title": "どの画像がどこに描かれるか",
        "mock": ["通常", "マウス", "押下", "", "その他(3)"],
        "callouts": ["左矢印", "通常", "マウスを乗せたとき", "押したとき・タブ表示中", "使えないとき", "「その他」ボタン", "右矢印"],
        "frames_row": "ボタン枠（9スライス、下を参照）",
        "rows": ["バー全体の背景（横に繰り返し）", "各ボタン枠の下に敷くイラスト", "「その他」で開くメニュー", "「その他」メニュー内の設定ボタン",
                 "スクロール用の左右の矢印", "「外観」タブのカードのプレビュー", "Workshop のプレビュー画像"],
        "b_title": "ボタン枠の描き方：9スライス",
        "b_caps": ["64×64 の元画像（拡大）", "高さ 36 px のボタンに描いたところ（拡大）"],
        "b_points": [
            "1枚の 64×64 の画像を、どんな大きさのボタンにも当てはめます。画像は9つに分けられます。",
            "1 = 四隅（各 16×16）：そのまま描かれ、引き伸ばされません。",
            "2 = 四辺：引き伸ばされます。　3 = 中央：残りを埋めるように引き伸ばされます。",
            "模様、枠線、角の飾りは外側の帯（1 と 2 の外側）に描きます。中央の模様は伸びて形が崩れます。",
            "バーのボタンは高さ 36 px しかなく、上下の辺がそれぞれ 16 px を占めます。枠は細く描き、中央を透明にすると下のイラストが見えます。",
            "画像は正方形にします。72×72 より大きくしても縮小されるだけです。",
            "隅の文字は目印です（N 通常、H マウス、A 押下、X 使用不可、+ その他、W 「その他」メニュー）。上から塗りつぶしてください。",
        ],
        "c_title": "ボタンの中のイラスト ButtonPicture.png（240×72）",
        "c_points": [
            "各ボタンいっぱいに中央合わせで敷かれ、はみ出した部分は切り取られます。ボタンの文字とアイコンはイラストの上に描かれます。",
            "枠で囲んだ範囲は、ボタン幅が 120、80、40 px のときに見える部分です。見せたいものは中央に置きます。",
            "イラストが不要なら、Defs のファイルの <buttonImage> から </buttonImage> までを削除します。",
        ],
        "d_title": "文字色",
        "d_intro": "Defs フォルダの .xml ファイルにあります（メモ帳で開けます）：",
        "d_points": [
            "labelColor 通常、labelMouseoverColor マウスを乗せたとき、labelActiveColor 押したとき。",
            "かっこの中は（赤, 緑, 青, 不透明度）で、それぞれ 0～1。たとえば (1, 0.8, 0.4, 1) にすると琥珀色です。",
            "保存すればこちらも自動で読み込まれます（Mod として有効にする前まで）。",
        ],
        "e_title": "フォルダの中身",
        "tree": ["編集する画像（PNG）", "設定：文字色、使う画像", "Mod の名前と説明", "Workshop のプレビュー画像",
                 "この説明書（ゲームは読み込まないので、残して構いません）", "Main Button Bar System がこのフォルダを見分けるためのファイル（ゲームは読み込みません）"],
        "f_title": "公開とその後",
        "f_points": [
            "アップロード後、Workshop ページの「必要アイテム」（Required items）に Main Button Bar System を追加します。About.xml の前提 Mod はゲームにしか伝わりません。",
            "MOD として有効にした後も、PNG の変更は自動で（または「再読み込み」で）反映されます。Defs の変更には再起動が必要です。",
            "不要になったら、カード下の「削除」でフォルダがセーブデータフォルダの MainButtonBarSystem\\DeletedThemes\\ に移動します。Mods に戻せば元に戻ります。",
            "MODリストでチェックが入っているテーマや、今回の起動で Mod として読み込まれたテーマは削除できません。チェックを外して再起動してください。",
        ],
        "footer": "詳しくは Main Button Bar System の Mod フォルダにある ThemeGuide.ja.md を、ボタンごとの画像や装飾画像などの上級者向け項目は API.md を見てください。",
    },
    "English": {
        "title": "Theme guide: your theme in three steps",
        "subtitle": "This folder is your theme, and it is also a mod. Edit the pictures in it and save, and the button bar at the bottom of the game changes with them. No programming, no restart.",
        "steps": [
            ("Edit the pictures", "Open the PNGs in the folder inside Textures with any image editor, change them and save. Keep the file names. For see-through parts use an editor with transparency (Krita, GIMP…; not Paint)."),
            ("See it at once", "While Main Button Bar System's settings window is on the Appearance tab (Preset themes at the top) with this theme selected, the bar shows the new picture within about a second, or press Reload under the card. Press Save at the bottom of the window to keep the theme selected."),
            ("Enable and share", "When you like it, enable it in the mod list and restart: it is an ordinary theme mod now. Upload it to the Steam Workshop with the game's own upload button."),
        ],
        "a_title": "Which picture paints which part",
        "mock": ["Normal", "Mouse over", "Pressed", "", "More (3)"],
        "callouts": ["left arrow", "normal", "under the mouse", "pressed, or tab open", "can't be used", "the More button", "right arrow"],
        "frames_row": "button frames (nine-slice, see below)",
        "rows": ["behind the whole bar, repeated across", "the picture under every button's frame", "the drawer that More opens",
                 "the drawer's settings button", "the scroll arrows", "the preview on the Appearance tab's card", "the Workshop picture"],
        "b_title": "Drawing a button frame: nine-slice",
        "b_caps": ["the 64×64 picture (enlarged)", "drawn on a 36 px tall button (enlarged)"],
        "b_points": [
            "One 64×64 picture fits a button of any size: it is cut into nine parts.",
            "1 = the four corners (16×16 each): drawn as they are, never stretched.",
            "2 = the four edges: stretched.   3 = the middle: stretched over the rest.",
            "Put patterns, borders and corner art in the outer ring (the outer side of 1 and 2); a pattern in the middle gets pulled out of shape.",
            "Bar buttons are only 36 px tall, so the top and bottom edges take 16 px each: draw the frame thin and keep the middle see-through so the picture under it shows.",
            "The picture must be square; larger than 72×72 does not help, it is only scaled down.",
            "The corner letter is only a marker (N normal, H mouse over, A pressed, X can't be used, + More, W drawer); paint over it.",
        ],
        "c_title": "The picture inside buttons: ButtonPicture.png (240×72)",
        "c_points": [
            "It fills every button, centred, and what does not fit is cut off. The button's label and icon are drawn on top.",
            "The frames show what is still visible on buttons 120, 80 and 40 px wide: keep the important part in the middle.",
            "No picture wanted: delete the part from <buttonImage> to </buttonImage> in the Defs file.",
        ],
        "d_title": "Text colours",
        "d_intro": "In the .xml file in the Defs folder (open it with Notepad):",
        "d_points": [
            "labelColor normal, labelMouseoverColor under the mouse, labelActiveColor pressed.",
            "The brackets hold (red, green, blue, opacity), each from 0 to 1; (1, 0.8, 0.4, 1), for example, is amber.",
            "Saving it is picked up the same way (until the theme is enabled as a mod).",
        ],
        "e_title": "What's in this folder",
        "tree": ["the pictures to edit (PNG)", "settings: text colours, which pictures", "the mod's name and description",
                 "the Workshop picture", "this guide (not read by the game; can stay)", "lets Main Button Bar System recognise the folder (not read by the game)"],
        "f_title": "Uploading, and later",
        "f_points": [
            "After uploading, add Main Button Bar System under Required items on the Workshop page: the dependency in About.xml is only seen by the game.",
            "Once it is an enabled mod, PNG changes are still picked up (or press Reload); changes to Defs need a restart.",
            "Don't want it any more: Delete under the card moves the folder to MainButtonBarSystem\\DeletedThemes\\ in the save data folder; move it back into Mods to restore it.",
            "A theme ticked in the mod list, or loaded as a mod in this session, can't be deleted: untick it and restart first.",
        ],
        "footer": "More detail: ThemeGuide.md in the Main Button Bar System mod folder; advanced fields (a picture per button, decorations) in API.md.",
    },
}

GUIDE_ROW_FILES = ["Bar.png", "ButtonPicture.png", "Drawer.png", "Gear.png", "ArrowLeft.png, ArrowRight.png",
                   "Preview.png", "About\\Preview.png"]
GUIDE_ROW_SIZES = ["256×36", "240×72", "64×64", "64×64", "32×32", "600×36", "640×360"]
GUIDE_TREE = ["Textures\\…\\", "Defs\\…_Theme.xml", "About\\About.xml", "About\\Preview.png", "_Guide_*.png",
              "MBBS_ThemeProject.xml"]

_FONT_CACHE = {}


def guide_font(name, size):
    key = (name, size)
    if key not in _FONT_CACHE:
        path = os.path.join(FONT_DIR, name)
        if not os.path.exists(path):
            raise SystemExit(f"Font not found: {path} (the guides use the Windows fonts in GUIDE_FONTS)")
        _FONT_CACHE[key] = ImageFont.truetype(path, size)
    return _FONT_CACHE[key]


# 英數字串整段斷行，其他字（漢字、假名、全形標點）逐字斷行；全形句讀不放在行首
_TOKEN = re.compile(r"[A-Za-z0-9_\-./\\<>()*:'’,%=+#\"…×]+ ?| +|.")
_NO_LINE_START = set("，。、）」』：；！？,.)】〕》ー;:!?%")
_NO_LINE_END = set("「『（【〔《(")


def wrap(draw, text, f, width):
    lines = []
    for para in text.split("\n"):
        line = ""
        for tok in _TOKEN.findall(para):
            trial = line + tok
            if line.strip() and draw.textlength(trial.rstrip(), font=f) > width and tok[0] not in _NO_LINE_START:
                carry = ""
                while line and line[-1] in _NO_LINE_END:   # 開括號跟著下一行
                    carry, line = line[-1] + carry, line[:-1]
                lines.append(line.rstrip())
                line = carry + tok.lstrip()
            else:
                line = trial
        lines.append(line.rstrip())
    return lines


class GuideSheet:
    """一張說明書：底層畫面板底色，內容層畫字和圖，最後疊起來、裁到內容高度。"""

    def __init__(self, lang):
        self.lang = lang
        self.text = GUIDE_TEXT[lang]
        self.body_font, self.bold_font = GUIDE_FONTS[lang]
        self.base = Image.new("RGBA", (G_W, 4000), G_BG)
        self.layer = Image.new("RGBA", (G_W, 4000), (0, 0, 0, 0))
        self.bd = ImageDraw.Draw(self.base)
        self.d = ImageDraw.Draw(self.layer)

    def f(self, size, bold=False):
        return guide_font(self.bold_font if bold else self.body_font, size)

    def mono(self, size):
        return guide_font(MONO_FONT, size)

    def para(self, x, y, width, text, size, fill=G_BODY, bold=False, gap=1.45):
        f = self.f(size, bold)
        for line in wrap(self.d, text, f, width):
            self.d.text((x, y), line, font=f, fill=fill)
            y += round(size * gap)
        return y

    def bullets(self, x, y, width, items, size=21):
        for item in items:
            self.d.ellipse([x + 2, y + size * 0.55 - 4, x + 10, y + size * 0.55 + 4], fill=G_ACCENT)
            y = self.para(x + 24, y, width - 24, item, size) + 8
        return y

    def badge(self, cx, cy, number, r=17):
        self.d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=G_ACCENT, outline=(20, 20, 20, 255), width=2)
        text_center(self.d, (cx - r, cy - r, cx + r, cy + r), number, 0, (20, 20, 20, 255), self.f(22, True))

    def panel(self, x, y, w, h):
        self.bd.rounded_rectangle([x, y, x + w, y + h], radius=14, fill=G_PANEL)

    def title(self, x, y, text, size=29):
        self.d.text((x, y), text, font=self.f(size, True), fill=G_WHITE)
        return y + round(size * 1.6)

    def paste(self, image, xy):
        self.layer.alpha_composite(image, (int(xy[0]), int(xy[1])))

    def finish(self, height):
        if height > self.base.height:
            raise SystemExit(f"Guide {self.lang} needs {height} px, more than the {self.base.height} px canvas")
        img = self.base.crop((0, 0, G_W, height))
        img.alpha_composite(self.layer.crop((0, 0, G_W, height)))
        return img.convert("RGB")


def guide_sheet(tex, lang):
    s = GuideSheet(lang)
    t = s.text
    cw = G_W - 2 * G_M          # 內容寬
    pad = 28                    # 面板內距

    # 標題
    y = 36
    s.d.text((G_M, y), t["title"], font=s.f(46, True), fill=G_WHITE)
    y = s.para(G_M, y + 74, cw, t["subtitle"], 23, G_BODY) + 22

    # 三個步驟
    gap = 24
    card_w = (cw - 2 * gap) // 3
    bottoms = []
    for i, (head, body) in enumerate(t["steps"]):
        x = G_M + i * (card_w + gap)
        s.badge(x + pad + 22, y + pad + 22, str(i + 1), r=24)
        s.d.text((x + pad + 60, y + pad + 4), head, font=s.f(30, True), fill=G_WHITE)
        bottoms.append(s.para(x + pad, y + pad + 66, card_w - 2 * pad, body, 21))
    h = max(bottoms) - y + pad - 8
    for i in range(3):
        s.panel(G_M + i * (card_w + gap), y, card_w, h)
    y += h + 30

    # A. 哪張圖畫哪裡
    top = y
    y = s.title(G_M + pad, y + pad, t["a_title"])
    widths = [140, 140, 140, 100, 110]
    states = ["Button", "ButtonMouseover", "ButtonActive", "ButtonDisabled", "More"]
    pictures = [True, True, True, True, False]
    mock = bar_mock(tex, 720, [(st, w, None, p) for st, w, p in zip(states, widths, pictures)])
    scale = 2
    mx = G_M + pad
    s.paste(mock.resize((720 * scale, BAR_H * scale), Image.NEAREST), (mx, y))
    xs, x = [], 24
    for w in widths:
        xs.append((x, x + w))
        x += w
    colors = {"ButtonMouseover": (255, 255, 153, 255), "ButtonActive": (255, 217, 128, 255)}
    for (x0, x1), st, word in zip(xs, states, t["mock"]):
        if word:
            text_center(s.d, (mx + x0 * scale, y, mx + x1 * scale, y + BAR_H * scale), word, 0,
                        colors.get(st, (255, 255, 255, 255)), s.f(22, True))
    centers = [12] + [(a + b) / 2 for a, b in xs] + [xs[-1][1] + 12]
    files = ["ArrowLeft.png"] + [st + ".png" for st in states] + ["ArrowRight.png"]
    bar_bottom = y + BAR_H * scale
    for i, (c, file, desc) in enumerate(zip(centers, files, t["callouts"])):
        cx = mx + c * scale
        ty = bar_bottom + 16 + (i % 2) * 62
        s.d.line([(cx, bar_bottom + 2), (cx, ty)], fill=G_ACCENT, width=2)
        f1, f2 = s.mono(19), s.f(19)
        w1, w2 = s.d.textlength(file, font=f1), s.d.textlength(desc, font=f2)
        left = min(max(cx - max(w1, w2) / 2, G_M + pad), G_M + cw - pad - max(w1, w2))
        s.d.text((left, ty + 2), file, font=f1, fill=G_FILE)
        s.d.text((left, ty + 28), desc, font=f2, fill=G_BODY)
    y = bar_bottom + 16 + 62 + 80

    # 檔案表
    col_file, col_desc = G_M + pad, G_M + pad + 520
    rows = [("Button / ButtonMouseover / ButtonActive / ButtonDisabled / More .png", t["frames_row"], "64×64")]
    rows += list(zip(GUIDE_ROW_FILES, t["rows"], GUIDE_ROW_SIZES))
    for i, (file, desc, size) in enumerate(rows):
        if i == 0:
            fy = s.para(col_file, y, 480, file, 19, G_FILE)
            s.d.text((col_desc, y), desc, font=s.f(20), fill=G_BODY)
        else:
            s.d.text((col_file, y), file, font=s.mono(19), fill=G_FILE)
            s.d.text((col_desc, y), desc, font=s.f(20), fill=G_BODY)
            fy = y + 30
        sw = s.d.textlength(size, font=s.mono(19))
        s.d.text((G_M + cw - pad - sw, y), size, font=s.mono(19), fill=G_DIM)
        y = max(fy, y + 30) + 6
        s.d.line([(col_file, y - 3), (G_M + cw - pad, y - 3)], fill=(52, 56, 68, 255))
    s.panel(G_M, top, cw, y - top + pad - 6)
    y += pad + 24

    # B. 九宮格
    top = y
    y = s.title(G_M + pad, y + pad, t["b_title"])
    ax = G_M + pad + 20
    atlas = tex["Button"].resize((256, 256), Image.NEAREST)
    s.paste(atlas, (ax, y))
    s.badge(ax + 224, y + 32, "1")
    s.badge(ax + 128, y + 32, "2")
    s.badge(ax + 128, y + 128, "3")
    s.para(ax - 10, y + 266, 300, t["b_caps"][0], 18, G_DIM)
    arrow_x = ax + 300
    s.d.polygon([(arrow_x, y + 120), (arrow_x + 50, y + 120), (arrow_x + 50, y + 106), (arrow_x + 80, y + 128),
                 (arrow_x + 50, y + 150), (arrow_x + 50, y + 136), (arrow_x, y + 136)], fill=G_DIM)
    demo = Image.new("RGBA", (220, BAR_H), (0, 0, 0, 0))
    tile(demo, tex["Bar"], (0, 0, 220, BAR_H))
    draw_cover(demo, tex["ButtonPicture"], (0, 0, 220, BAR_H))
    draw_nine_slice(demo, tex["Button"], (0, 0, 220, BAR_H))
    bx, by = arrow_x + 120, y + 74
    s.paste(demo.resize((660, BAR_H * 3), Image.NEAREST), (bx, by))
    s.badge(bx + 660 - 24, by + 24, "1")
    s.badge(bx + 330, by + 24, "2")
    s.d.line([(bx + 230, by + 54), (bx + 230, by + 150)], fill=G_ACCENT, width=2)
    s.badge(bx + 230, by + 166, "3")
    s.para(bx, by + BAR_H * 3 + 76, 640, t["b_caps"][1], 18, G_DIM)
    y += 310
    y = s.bullets(G_M + pad, y, cw - 2 * pad, t["b_points"])
    s.panel(G_M, top, cw, y - top + pad - 8)
    y += pad + 16

    # C. 插圖
    top = y
    y = s.title(G_M + pad, y + pad, t["c_title"])
    s.paste(tex["ButtonPicture"].resize((480, 144), Image.NEAREST), (G_M + pad, y))
    yb = s.bullets(G_M + pad + 520, y, cw - 2 * pad - 520, t["c_points"])
    y = max(y + 144, yb) + 4
    s.panel(G_M, top, cw, y - top + pad - 4)
    y += pad + 20

    # D、E 並排：文字顏色、資料夾內容
    top = y
    half = (cw - gap) // 2
    # D
    dx = G_M + pad
    yd = s.title(dx, y + pad, t["d_title"])
    yd = s.para(dx, yd, half - 2 * pad, t["d_intro"], 21)
    code = "<labelColor>(1, 1, 1, 1)</labelColor>"      # 和 DEF_TEMPLATE 裡的一樣，玩家打開檔案找得到
    s.d.rounded_rectangle([dx, yd + 4, dx + half - 2 * pad, yd + 52], radius=8, fill=(18, 20, 26, 255))
    s.d.text((dx + 14, yd + 14), code, font=s.mono(20), fill=G_BODY)
    sw_x = dx + 14 + s.d.textlength(code, font=s.mono(20)) + 18
    s.d.rectangle([sw_x, yd + 14, sw_x + 26, yd + 40], fill=(255, 255, 255, 255), outline=(10, 10, 10, 255))
    yd = s.bullets(dx, yd + 70, half - 2 * pad, t["d_points"])
    # E
    ex = G_M + half + gap + pad
    ye = s.title(ex, y + pad, t["e_title"])
    for name, desc in zip(GUIDE_TREE, t["tree"]):
        s.d.text((ex, ye), name, font=s.mono(19), fill=G_FILE)
        ye = s.para(ex + 24, ye + 28, half - 2 * pad - 24, desc, 19, G_BODY) + 6
    h = max(yd, ye) - top + pad - 8
    s.panel(G_M, top, half, h)
    s.panel(G_M + half + gap, top, half, h)
    y = top + h + 30

    # F. 上傳和之後
    top = y
    y = s.title(G_M + pad, y + pad, t["f_title"])
    y = s.bullets(G_M + pad, y, cw - 2 * pad, t["f_points"])
    s.panel(G_M, top, cw, y - top + pad - 8)
    y += pad + 20

    y = s.para(G_M, y, cw, t["footer"], 19, G_DIM)
    return s.finish(y + 30)


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
    for lang in GUIDE_LANGS:
        guide_sheet(tex, lang).save(os.path.join(KIT, f"_Guide_{lang}.png"), optimize=True)
    old_guide = os.path.join(KIT, "_Guide.png")   # V1.0 的英文說明圖，換成四種語言
    if os.path.exists(old_guide):
        os.remove(old_guide)
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
    print(f"ThemeKit written: {len(EXPECTED)} textures, About/Preview.png, {len(GUIDE_LANGS)} guides, Defs/Theme.xml.template")

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
