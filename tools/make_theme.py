#!/usr/bin/env python3
## 部署路徑：tools/make_theme.py（發佈檔裡在 ThemeTemplate/tools/make_theme.py）
##
## 說明：從一份 JSON 配色產生 Main Button Bar System 的主題。可以產生整個主題模組（About、Defs、Textures、
##   Languages、Preview.png），也可以只產生貼圖（內建主題用，由 gen_theme_textures.py 呼叫）。依賴 Pillow。
##   貼圖照遊戲的九宮格規則（docs/API.md 第 4 節）：正方形 64×64，四角在畫面上是 16px，邊框圖案都在外側 16px 內。
##   插圖欄位（背景繪製方式、裝飾圖）可以用佔位圖先佔位，之後把 JSON 的 placeholder 換成 source 指向成品即可。
##
## 用法（在 repo 根目錄或任何地方）：
##   python -I -X utf8 tools/make_theme.py <theme.json> <輸出資料夾>                 產生完整主題模組
##   python -I -X utf8 tools/make_theme.py <theme.json> <輸出資料夾> --textures-only  只產生貼圖到該資料夾
##
## JSON 結構（tools/themes/*.json 是範例）：
##   name            貼圖資料夾名和 Def 檔名用的短名（例 "Imperial"）
##   defName / label / description / author / displayOrder
##   labelColor / labelMouseoverColor / labelActiveColor   [r,g,b,a] 0～1，null 表示不設
##   palette         base, edge, outline, rule, accent, active：[r,g,b]；ornament：finial / roundel / genetab /
##                   shard / hull / petal；double, glow_on_hover, hollow：布林；icon, tile, rivet, active_rule：[r,g,b] 或 null
##                   hollow：true 時四種狀態的按鈕 atlas 不填底色（只留邊框、內線、底線、角落裝飾），按鈕圖才透得出來；
##                   More 的 atlas 照舊有底色（它不會有按鈕圖）；捲動箭頭用的是 Button，所以也是中空、透出底部列背景
##                   frame："theme"（預設，用上面的配色畫框）或 "vanilla"：四種狀態的按鈕、More、Bar 換成原版風格
##                   （灰邊＋一條暗內線、中間透明；原版的深色按鈕底改由 Bar 提供），等於一定是 hollow；
##                   抽屜、捲動箭頭、齒輪照舊用配色，所以 palette 的必填欄位不變
##   background      barFit / drawerFit：Atlas / Tile / Stretch / Fit / Cover；barTileScale / drawerTileScale；
##                   bar / drawer：null（內建畫法）、{"placeholder": {"size": [w,h], "color": [r,g,b], "text": "…"}}
##                   或 {"source": "相對於 JSON 的 PNG 路徑"}
##   decorations     清單，每項：file（貼圖檔名，不含副檔名）、placeholder 或 source、target（Bar / Drawer）、
##                   anchor（Left / Center / Right）、verticalAnchor（Bottom / Center / Top）、offset [x,y]、
##                   size [w,h]、opacity、layer（BelowButtons / AboveButtons）、showWhen（[]、Normal、Mouseover、DrawerOpen）
##   buttonImage     選填，每顆主按鈕的背景圖（畫在按鈕 atlas 底下）：file、placeholder 或 source、fit（Cover / Fit /
##                   Stretch；Atlas、Tile 視同 Stretch）、anchor、verticalAnchor（Top / Center / Bottom）、opacity、inset
##   buttonImages    選填清單，指定按鈕的背景圖，欄位同上再加 button（MainButtonDef 的 defName，例 "Architect"）
##   suggestedLayout 選填，主題建議的版面（外觀分頁提示，玩家按下才套用）：displayModes（顯示方式 defName 清單，
##                   目前的不在清單裡就換成第一個）、buttonWidth（預設和最小按鈕寬度至少這麼寬，0 不管）、
##                   halfButtonsFullWidth（半格按鈕也用完整寬度，布林）
##   mod             只有完整模組需要：packageId、name、texturesRoot（Textures/ 底下的資料夾）、supportedVersions
##   translations    {"ChineseTraditional": {"label": "…", "description": "…"}, …}
##   gearSource      選填，齒輪圖示的來源 PNG；省略時找本模組的 Textures/MainButtonBarSystem/UI/Gear.png
##
## 架構：
##   _theme_color_derive_shade()         — 顏色加減亮度
##   _theme_color_derive_mix()           — 兩色內插
##   _theme_color_derive_grey()          — 顏色去飽和（停用狀態）
##   _theme_atlas_list_corners()         — 列出 atlas 四角座標和方向
##   _theme_atlas_apply_gradient()       — 上下漸層填滿
##   _theme_atlas_apply_corner_cuts()    — 四角切成 45 度透明
##   _theme_atlas_apply_ornament()       — 角落裝飾（依 palette 的 ornament 樣式）
##   _theme_atlas_build_hover_glow()     — Void 風格的滑鼠移上紅光
##   _theme_atlas_build_button()         — 按鈕 atlas（normal / mouseover / active / disabled）
##   _theme_atlas_build_bar()            — 整條列背景 atlas
##   _theme_atlas_build_vanilla()        — palette.frame＝vanilla 時的原版風格按鈕框、More、Bar
##   _theme_atlas_build_drawer()         — 抽屜背景 atlas
##   _theme_icon_build_arrow()           — 捲動箭頭圖示
##   _theme_icon_build_gear()            — 用主題色染過的齒輪
##   _theme_image_build_placeholder()    — 佔位圖（色塊、外框、斜線、文字）
##   _theme_image_load_source()          — 讀 JSON 指定的現成 PNG
##   _theme_image_apply_nine_slice()     — 在畫布上用九宮格方式畫 atlas（預覽用）
##   _theme_image_build_bar_strip()      — 一段模擬的按鈕列（遊戲內預覽貼圖）
##   _theme_decoration_derive_box()      — 裝飾圖的擺放，和 C# DecorationLayout.Place 同一套規則（預覽用）
##   _theme_button_image_find()          — 某顆按鈕用的背景圖設定（指定優先，否則預設，否則 None）
##   _theme_button_image_apply()         — 把按鈕背景圖畫進格子，和 C# Core/ImageFit 同一套規則（預覽用）
##   _theme_image_build_mod_preview()    — About/Preview.png
##   _theme_json_validate_fields()       — JSON 的必填欄位、列舉值、檔名衝突檢查
##   _theme_locate_gear()                — 找齒輪來源 PNG
##   _theme_palette_normalize()          — JSON 的顏色清單轉成 tuple
##   _theme_def_serialize_color()        — [r,g,b,a] → "(r, g, b, a)"
##   _theme_def_serialize_vector()       — [x,y] → "(x, y)"
##   _theme_def_serialize_button_image() — 按鈕背景圖寫進 Defs 的欄位
##   _theme_button_image_list()          — buttonImage 加 buttonImages 的清單
##   _theme_mod_write_about()            — About/About.xml
##   _theme_mod_write_defs()             — Defs/<name>_Themes.xml
##   _theme_mod_write_languages()        — Languages/<語言>/DefInjected/…
##   theme_build_textures()              — 主題的所有貼圖（檔名 → Image）
##   theme_icon_build_mod()              — 本模組的 About/ModIcon.png
##   theme_load()                        — 讀 JSON 並整形
##   theme_write_mod()                   — 產生完整主題模組
##   theme_write_textures()              — 只寫貼圖
##   main()                              — 命令列入口
##
## 修改日誌
## V1.0：2026-11-04  從 gen_theme_textures.py 拆出；畫圖程式碼原樣搬入（像素不變），加上佔位圖、完整模組輸出
## V1.1：2026-11-04  review 後：JSON 列舉值與檔名衝突檢查、佔位圖文字依寬度縮字級、擺放算法提到模組層
## V1.2：2026-11-04  第二輪 review：檔名衝突不分大小寫；palette 不是物件時報 ValueError
## V1.3：2026-11-04  按鈕背景圖：JSON 的 buttonImage／buttonImages（寫進 Defs、產貼圖、驗證、預覽條先畫圖再畫框）；
##                   palette 的 hollow（按鈕 atlas 不填底色）
## V1.4：2026-11-05  palette 的 frame＝vanilla：原版風格的中空按鈕框（顏色量自原版按鈕列）
## V1.5：2026-11-06  suggestedLayout：主題建議的版面寫進 Defs，並驗證欄位
import argparse
import json
import os
import sys
from xml.sax.saxutils import escape

from PIL import Image, ImageDraw, ImageFilter, ImageFont

############################################################################################################
#   常數
############################################################################################################
_THEME_ATLAS_SIZE    = 64                                               # atlas 邊長
_THEME_ATLAS_QUARTER = _THEME_ATLAS_SIZE // 4                           # 外側四分之一：邊框圖案要在這裡面
_THEME_DEP_PACKAGE   = "NAI.MainButtonBarSystem"                        # 本模組的 packageId
_THEME_DEP_NAME      = "Main Button Bar System"
_THEME_DEP_URL       = "https://github.com/C10H8Nai/Rimworld-Main-Button-Bar-System"
_THEME_DEF_TYPE      = "MainButtonBarSystem.BarAppearanceDef"
_THEME_PREVIEW_SIZE  = (640, 360)                                       # About/Preview.png（Steam 要 16:9、< 1 MB）
_THEME_STRIP_SIZE    = (600, 36)                                        # 遊戲內預覽貼圖：一段按鈕列
_THEME_BAR_HEIGHT    = 36                                               # 遊戲的按鈕列高度
_THEME_TEXTURE_FILES = ("Button", "ButtonMouseover", "ButtonActive", "ButtonDisabled", "Bar", "More", "Drawer",
                        "ArrowLeft", "ArrowRight", "Gear")
_THEME_DECORATION_DEFAULTS = {"target": "Bar", "anchor": "Left", "verticalAnchor": "Bottom", "offset": [0, 0],
                              "size": [0, 0], "opacity": 1, "layer": "BelowButtons", "showWhen": []}
_THEME_BACKGROUND_DEFAULTS = {"barFit": "Atlas", "drawerFit": "Atlas", "barTileScale": 1, "drawerTileScale": 1,
                              "bar": None, "drawer": None}
_THEME_BUTTON_IMAGE_DEFAULTS = {"fit": "Cover", "anchor": "Center", "verticalAnchor": "Center", "opacity": 1, "inset": 0}
_THEME_PALETTE_COLOR_KEYS  = ("base", "edge", "outline", "rule", "accent", "active", "icon", "tile", "rivet", "active_rule")
_THEME_ORNAMENTS           = ("finial", "roundel", "genetab", "shard", "hull", "petal")   # 和 C# 無關，只有這支工具認得
_THEME_FITS                = ("Atlas", "Tile", "Stretch", "Fit", "Cover")                  # 以下六組要和 C# 的列舉同名
_THEME_TARGETS             = ("Bar", "Drawer")
_THEME_ANCHORS             = ("Left", "Center", "Right")
_THEME_VERTICAL_ANCHORS    = ("Bottom", "Center", "Top")
_THEME_LAYERS              = ("BelowButtons", "AboveButtons")
_THEME_FRAMES              = ("theme", "vanilla")                                           # palette.frame，只有這支工具認得
_THEME_VANILLA_FILL        = (25, 34, 41)                                                   # 原版按鈕底（量自原版按鈕列截圖）
_THEME_VANILLA_INNER       = (41, 46, 49)                                                   # 原版邊框內側的暗線
_THEME_VANILLA_STATES      = {"normal": ((100, 108, 112), 0), "mouseover": ((150, 158, 164), 22),   # 狀態 → (邊框色, 白色薄層的 alpha)
                              "active": ((196, 202, 206), 38), "disabled": ((70, 74, 78), 0)}
_THEME_STATES              = ("Normal", "Mouseover", "DrawerOpen")
_THEME_PREVIEW_LABELS      = ("Architect", "Work", "Schedule", "Assign")                 # 預覽條的按鈕，也是原版的 defName（找按鈕圖用）
_THEME_PREVIEW_BACKGROUND  = (30, 33, 38, 255)
_THEME_PLACEHOLDER_TEXT    = (246, 242, 241, 255)


############################################################################################################
#   _theme_color_derive_shade — 顏色每個分量加 delta，夾在 0～255
############################################################################################################
def _theme_color_derive_shade(color, delta):
    return tuple(max(0, min(255, channel + delta)) for channel in color[:3])


############################################################################################################
#   _theme_color_derive_mix — 從 color_a 往 color_b 內插 ratio
############################################################################################################
def _theme_color_derive_mix(color_a, color_b, ratio):
    return tuple(int(color_a[i] + (color_b[i] - color_a[i]) * ratio) for i in range(3))


############################################################################################################
#   _theme_color_derive_grey — 往灰色靠攏（停用狀態）
############################################################################################################
def _theme_color_derive_grey(color, ratio=0.6):
    grey_level = int(sum(color[:3]) / 3)
    return _theme_color_derive_mix(color, (grey_level, grey_level, grey_level), ratio)


############################################################################################################
#   _theme_atlas_list_corners — 四角的座標和往內的方向（step_x, step_y 是 ±1）
############################################################################################################
def _theme_atlas_list_corners():
    size = _THEME_ATLAS_SIZE
    for corner_x, corner_y in ((0, 0), (size - 1, 0), (0, size - 1), (size - 1, size - 1)):
        yield corner_x, corner_y, (1 if corner_x < size / 2 else -1), (1 if corner_y < size / 2 else -1)


############################################################################################################
#   _theme_atlas_apply_gradient — 從 top 色到 bottom 色逐列填滿
############################################################################################################
def _theme_atlas_apply_gradient(image, top, bottom):
    draw = ImageDraw.Draw(image)
    size = _THEME_ATLAS_SIZE
    for y in range(size):
        draw.line([(0, y), (size - 1, y)], fill=_theme_color_derive_mix(top, bottom, y / (size - 1)) + (255,))


############################################################################################################
#   _theme_atlas_apply_corner_cuts — 四角切成 45 度透明（船殼、碎片）
############################################################################################################
def _theme_atlas_apply_corner_cuts(image, cut_size):
    pixels = image.load()
    for corner_x, corner_y, step_x, step_y in _theme_atlas_list_corners():
        for i in range(cut_size):
            for j in range(cut_size - i):
                pixels[corner_x + step_x * i, corner_y + step_y * j] = (0, 0, 0, 0)


############################################################################################################
#   _theme_atlas_apply_ornament — 角落裝飾，一律在外側四分之一內（遊戲會拉伸每邊的中段）
############################################################################################################
def _theme_atlas_apply_ornament(draw, palette, color):
    style = palette["ornament"]
    for corner_x, corner_y, step_x, step_y in _theme_atlas_list_corners():
        if style == "finial":
            x = corner_x + 6 * step_x
            if step_y == 1:  # 上方兩角：皇冠尖角和頂飾圓點
                draw.polygon([(x - 3, corner_y + 9), (x, corner_y + 3), (x + 3, corner_y + 9)], fill=color)
                draw.ellipse([x - 1.5, corner_y + 1, x + 1.5, corner_y + 4], fill=color)
            else:
                draw.ellipse([x - 1.5, corner_y - 6, x + 1.5, corner_y - 3], fill=color)
        elif style == "roundel":
            x, y = corner_x + 7 * step_x, corner_y + 7 * step_y
            draw.ellipse([x - 4, y - 4, x + 4, y + 4], fill=(8, 8, 10, 255))
            draw.ellipse([x - 3, y - 3, x + 3, y + 3], fill=color)
            draw.ellipse([x - 1, y - 1, x + 1, y + 1], fill=palette["accent"] + (255,))
        elif style == "genetab":
            x, y = corner_x + 4 * step_x, corner_y + 4 * step_y
            draw.rectangle([min(x, x + 5 * step_x), min(y, y + 5 * step_y), max(x, x + 5 * step_x), max(y, y + 5 * step_y)],
                           fill=(palette.get("tile") or palette["rule"]) + (255,))
        elif style == "shard":
            draw.line([(corner_x + 7 * step_x, corner_y), (corner_x, corner_y + 7 * step_y)], fill=palette["rule"] + (255,), width=1)
            draw.line([(corner_x + 11 * step_x, corner_y + 2 * step_y), (corner_x + 2 * step_x, corner_y + 11 * step_y)],
                      fill=_theme_color_derive_shade(palette["edge"], 20) + (255,), width=1)
        elif style == "hull":
            rivet = (palette.get("rivet") or _theme_color_derive_shade(palette["edge"], 50)) + (255,)
            x, y = corner_x + 9 * step_x, corner_y + 4 * step_y
            draw.ellipse([x - 1.5, y - 1.5, x + 1.5, y + 1.5], fill=rivet)
            x, y = corner_x + 4 * step_x, corner_y + 9 * step_y
            draw.ellipse([x - 1.5, y - 1.5, x + 1.5, y + 1.5], fill=rivet)
        elif style == "petal":  # 萘伊：角落一顆小圓點，旁邊一道短斜線（像髮夾）
            x, y = corner_x + 6 * step_x, corner_y + 6 * step_y
            draw.ellipse([x - 2, y - 2, x + 2, y + 2], fill=color)
            draw.line([(corner_x + 11 * step_x, corner_y + 3 * step_y), (corner_x + 3 * step_x, corner_y + 11 * step_y)],
                      fill=palette["accent"] + (255,), width=1)


############################################################################################################
#   _theme_atlas_build_hover_glow — 滑鼠移上的紅光：亮內線、模糊光暈、底部往上的紅霧，都在外側四分之一內
############################################################################################################
def _theme_atlas_build_hover_glow(image, palette, inner, state):
    size = _THEME_ATLAS_SIZE
    hot = _theme_color_derive_shade(palette["rule"], 70 if state == "active" else 55)
    halo = Image.new("RGBA", (size, size))
    ImageDraw.Draw(halo).rectangle([inner, inner, size - 1 - inner, size - 1 - inner], outline=hot + (255,), width=3)
    halo = halo.filter(ImageFilter.GaussianBlur(2.5))
    image = Image.alpha_composite(image, halo)
    image = Image.alpha_composite(image, halo)
    haze = Image.new("RGBA", (size, size))
    haze_draw = ImageDraw.Draw(haze)
    bottom = size - 1 - inner
    rows = _THEME_ATLAS_QUARTER - inner - 2
    peak = 150 if state == "active" else 120
    for i in range(rows):  # 底線最強，往上淡出
        haze_draw.line([(inner, bottom - i), (size - 1 - inner, bottom - i)], fill=hot + (int(peak * (1 - i / rows) ** 1.6),))
    image = Image.alpha_composite(image, haze)
    draw = ImageDraw.Draw(image)
    draw.rectangle([inner + 1, inner + 1, size - 2 - inner, size - 2 - inner], outline=hot + (255,), width=1)
    draw.line([(inner + 1, bottom - 1), (size - 2 - inner, bottom - 1)], fill=_theme_color_derive_shade(palette["accent"], 40) + (255,), width=3)
    return image


############################################################################################################
#   _theme_atlas_build_button — 按鈕 atlas：漸層底、邊框、內線、狀態底線、角落裝飾
############################################################################################################
def _theme_atlas_build_button(palette, fill, edge, state="normal", hollow=False):
    size = _THEME_ATLAS_SIZE
    image = Image.new("RGBA", (size, size))
    if not hollow:  # hollow：不填底色，按鈕背景圖從中間透出來
        _theme_atlas_apply_gradient(image, _theme_color_derive_shade(fill, 10), _theme_color_derive_shade(fill, -10))
    draw = ImageDraw.Draw(image)
    style = palette["ornament"]
    outline_offset = 0
    if palette.get("outline"):
        draw.rectangle([0, 0, size - 1, size - 1], outline=palette["outline"] + (255,), width=1)
        outline_offset = 1
    width = 3 if state == "active" else 2
    if palette.get("double"):
        draw.rectangle([outline_offset, outline_offset, size - 1 - outline_offset, size - 1 - outline_offset], outline=edge + (255,), width=1)
        draw.rectangle([outline_offset + 2, outline_offset + 2, size - 3 - outline_offset, size - 3 - outline_offset],
                       outline=edge + (255,), width=1 if state != "active" else 2)
        inner = outline_offset + 5
    else:
        draw.rectangle([outline_offset, outline_offset, size - 1 - outline_offset, size - 1 - outline_offset], outline=edge + (255,), width=width)
        inner = outline_offset + width + 1
    if style in ("roundel", "shard", "petal"):
        draw.rectangle([inner + 1, inner + 1, size - 2 - inner, size - 2 - inner], outline=palette["rule"] + (255,), width=1)
    if state == "active" and palette.get("active_rule"):
        draw.rectangle([inner + 1, inner + 1, size - 2 - inner, size - 2 - inner], outline=palette["active_rule"] + (255,), width=1)
    if state in ("mouseover", "active"):
        # 強調色底線：靈能青、金、基因藍、紅光或推進器青
        y = size - 1 - inner - 1
        draw.line([(inner + 1, y), (size - 2 - inner, y)], fill=palette["accent"] + (255,), width=2)
    glow_states = ("mouseover", "active") if palette.get("glow_on_hover") else ("normal", "mouseover", "active")
    if style == "shard" and state in glow_states:
        if palette.get("glow_on_hover"):
            image = _theme_atlas_build_hover_glow(image, palette, inner, state)
        else:
            glow = Image.new("RGBA", (size, size))
            ImageDraw.Draw(glow).rectangle([inner, inner, size - 1 - inner, size - 1 - inner],
                                           outline=palette["rule"] + (180 if state == "normal" else 255,), width=2)
            image = Image.alpha_composite(image, glow.filter(ImageFilter.GaussianBlur(1.5)))
        draw = ImageDraw.Draw(image)
    _theme_atlas_apply_ornament(draw, palette, (edge if state != "disabled" else _theme_color_derive_grey(edge)) + (255,))
    if style in ("shard", "hull"):
        _theme_atlas_apply_corner_cuts(image, 4 if style == "hull" else 3)
        draw = ImageDraw.Draw(image)
        for corner_x, corner_y, step_x, step_y in _theme_atlas_list_corners():  # 沿著切口的邊線
            cut = 4 if style == "hull" else 3
            draw.line([(corner_x + cut * step_x, corner_y), (corner_x, corner_y + cut * step_y)], fill=edge + (255,), width=2)
    return image


############################################################################################################
#   _theme_atlas_build_bar — 整條列的背景 atlas：暗漸層，上緣一條邊框色和一條內線
############################################################################################################
def _theme_atlas_build_bar(palette):
    size = _THEME_ATLAS_SIZE
    image = Image.new("RGBA", (size, size))
    _theme_atlas_apply_gradient(image, _theme_color_derive_shade(palette["base"], -6), _theme_color_derive_shade(palette["base"], -18))
    draw = ImageDraw.Draw(image)
    draw.line([(0, 0), (size - 1, 0)], fill=palette["edge"] + (255,), width=2)
    draw.line([(0, 3), (size - 1, 3)], fill=palette["rule"] + (255,), width=1)
    return image


############################################################################################################
#   _theme_atlas_build_vanilla — 原版風格的 atlas（palette.frame＝vanilla）。part：Button 的四種狀態
#   （"normal"／"mouseover"／"active"／"disabled"）、"more" 或 "bar"。原版的按鈕底幾乎不透明，會蓋掉按鈕圖，
#   所以按鈕框中間透明，原版的深色底改由整條列的背景（bar）提供：按鈕圖 → 框 → 文字，看起來仍是原版按鈕。
#   原版滑鼠移上是整顆變亮，這裡改成邊框變亮加一層很淡的白，不會把按鈕圖洗白。More 沒有按鈕圖，維持實心
############################################################################################################
def _theme_atlas_build_vanilla(part):
    size = _THEME_ATLAS_SIZE
    fill = _THEME_VANILLA_FILL + (255,)
    if part == "bar":
        return Image.new("RGBA", (size, size), fill)
    if part == "more":
        image = Image.new("RGBA", (size, size), fill)
        ImageDraw.Draw(image).rectangle([0, 0, size - 1, size - 1], outline=_THEME_VANILLA_STATES["normal"][0] + (255,), width=2)
        return image
    edge, glow = _THEME_VANILLA_STATES[part]
    image = Image.new("RGBA", (size, size), (255, 255, 255, glow))
    draw = ImageDraw.Draw(image)
    draw.rectangle([0, 0, size - 1, size - 1], outline=edge + (255,), width=2)
    draw.rectangle([2, 2, size - 3, size - 3], outline=_THEME_VANILLA_INNER + (255,), width=1)
    return image


############################################################################################################
#   _theme_atlas_build_drawer — 抽屜背景 atlas：漸層、雙框、角落裝飾
############################################################################################################
def _theme_atlas_build_drawer(palette):
    size = _THEME_ATLAS_SIZE
    image = Image.new("RGBA", (size, size))
    _theme_atlas_apply_gradient(image, _theme_color_derive_shade(palette["base"], -2), _theme_color_derive_shade(palette["base"], -16))
    draw = ImageDraw.Draw(image)
    if palette.get("outline"):
        draw.rectangle([0, 0, size - 1, size - 1], outline=palette["outline"] + (255,), width=1)
    draw.rectangle([1, 1, size - 2, size - 2], outline=palette["edge"] + (255,), width=2)
    draw.rectangle([5, 5, size - 6, size - 6], outline=palette["rule"] + (255,), width=1)
    _theme_atlas_apply_ornament(draw, palette, palette["edge"] + (255,))
    return image


############################################################################################################
#   _theme_icon_build_arrow — 32×32 捲動箭頭，先畫 4 倍再縮小
############################################################################################################
def _theme_icon_build_arrow(palette, left):
    scale = 4
    image = Image.new("RGBA", (32 * scale, 32 * scale))
    draw = ImageDraw.Draw(image)
    points = [(20, 7), (10, 16), (20, 25)] if left else [(12, 7), (22, 16), (12, 25)]
    points = [(x * scale, y * scale) for x, y in points]
    draw.line(points, fill=(8, 8, 10, 230), width=7 * scale, joint="curve")
    draw.line(points, fill=(palette.get("icon") or palette["edge"]) + (255,), width=4 * scale, joint="curve")
    return image.resize((32, 32), Image.LANCZOS)


############################################################################################################
#   _theme_icon_build_gear — 把齒輪的亮面染成主題色，暗色輪廓保留
############################################################################################################
def _theme_icon_build_gear(palette, gear_source):
    gear = Image.open(gear_source).convert("RGBA")
    accent = palette.get("icon") or palette["edge"]
    pixels = gear.load()
    for y in range(gear.height):
        for x in range(gear.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha and red > 128:
                brightness = red / 255
                pixels[x, y] = (int(accent[0] * brightness), int(accent[1] * brightness), int(accent[2] * brightness), alpha)
    return gear


############################################################################################################
#   _theme_image_build_placeholder — 佔位圖：色塊、淺色外框、對角線、置中文字
############################################################################################################
def _theme_image_build_placeholder(spec):
    width, height = spec["size"]
    color = tuple(spec.get("color", (90, 90, 90)))
    image = Image.new("RGBA", (width, height), color + (255,))
    draw = ImageDraw.Draw(image)
    frame = _theme_color_derive_shade(color, 70) + (255,)
    draw.rectangle([0, 0, width - 1, height - 1], outline=frame, width=2)
    draw.line([(0, 0), (width - 1, height - 1)], fill=frame, width=1)
    draw.line([(0, height - 1), (width - 1, 0)], fill=frame, width=1)
    text = spec.get("text", "")
    if text:
        font_size = max(8, min(18, height // 3))
        while True:  # 字級從大往下試，直到一行放得進圖寬
            font = ImageFont.load_default(size=font_size)
            text_box = draw.textbbox((0, 0), text, font=font)
            text_width, text_height = text_box[2] - text_box[0], text_box[3] - text_box[1]
            if text_width <= width - 12 or font_size <= 8:
                break
            font_size -= 1
        origin = ((width - text_width) // 2 - text_box[0], (height - text_height) // 2 - text_box[1])
        draw.rectangle([origin[0] - 4, origin[1] - 2, origin[0] + text_width + 4, origin[1] + text_height + 2], fill=color + (255,))
        draw.text(origin, text, fill=_THEME_PLACEHOLDER_TEXT, font=font)
    return image


############################################################################################################
#   _theme_image_load_source — 讀 JSON 指定的現成 PNG（路徑相對於 JSON 所在資料夾）
############################################################################################################
def _theme_image_load_source(theme, relative_path):
    return Image.open(os.path.join(theme["_dir"], relative_path)).convert("RGBA")


############################################################################################################
#   _theme_image_apply_nine_slice — 照遊戲的九宮格規則把 atlas 畫進 box（x, y, w, h）
############################################################################################################
def _theme_image_apply_nine_slice(canvas, atlas, box):
    x, y, width, height = box
    corner = min(atlas.width // 4, width // 2, height // 2)
    source_corner = atlas.width // 4
    source_middle = atlas.width - 2 * source_corner
    middle_width, middle_height = width - 2 * corner, height - 2 * corner
    cells = (
        ((0, 0, source_corner, source_corner), (x, y, corner, corner)),
        ((source_corner, 0, source_middle, source_corner), (x + corner, y, middle_width, corner)),
        ((atlas.width - source_corner, 0, source_corner, source_corner), (x + width - corner, y, corner, corner)),
        ((0, source_corner, source_corner, source_middle), (x, y + corner, corner, middle_height)),
        ((source_corner, source_corner, source_middle, source_middle), (x + corner, y + corner, middle_width, middle_height)),
        ((atlas.width - source_corner, source_corner, source_corner, source_middle), (x + width - corner, y + corner, corner, middle_height)),
        ((0, atlas.height - source_corner, source_corner, source_corner), (x, y + height - corner, corner, corner)),
        ((source_corner, atlas.height - source_corner, source_middle, source_corner), (x + corner, y + height - corner, middle_width, corner)),
        ((atlas.width - source_corner, atlas.height - source_corner, source_corner, source_corner), (x + width - corner, y + height - corner, corner, corner)),
    )
    for (source_x, source_y, source_width, source_height), (target_x, target_y, target_width, target_height) in cells:
        if target_width <= 0 or target_height <= 0:
            continue
        piece = atlas.crop((source_x, source_y, source_x + source_width, source_y + source_height))
        piece = piece.resize((target_width, target_height), Image.Resampling.BILINEAR)
        canvas.alpha_composite(piece, (target_x, target_y))


############################################################################################################
#   _theme_image_build_bar_strip — 一段按鈕列：< [normal][mouseover][active][disabled] > [More]
############################################################################################################
def _theme_image_build_bar_strip(theme, textures, size):
    width, height = size
    strip = Image.new("RGBA", size, (0, 0, 0, 0))
    background = theme["background"]
    if background["barFit"] == "Atlas":
        _theme_image_apply_nine_slice(strip, textures["Bar"], (0, 0, width, height))
    else:
        strip.alpha_composite(textures["Bar"].resize(size, Image.Resampling.BILINEAR))
    font = ImageFont.load_default(size=13)
    arrow = 24
    count = len(_THEME_PREVIEW_LABELS) + 1
    cell = (width - arrow * 2) // count
    states = ("Button", "ButtonMouseover", "ButtonActive", "ButtonDisabled")
    labels = {"Button": theme["labelColor"], "ButtonMouseover": theme["labelMouseoverColor"] or theme["labelColor"],
              "ButtonActive": theme["labelActiveColor"] or theme["labelMouseoverColor"] or theme["labelColor"]}
    for index in range(count):
        is_more = index == count - 1
        x = arrow + cell * index + (arrow if is_more else 0)
        atlas_name = "More" if is_more else states[index]
        button_image = None if is_more else _theme_button_image_find(theme, _THEME_PREVIEW_LABELS[index])
        if button_image:  # 和遊戲一樣：先畫按鈕圖，再畫框
            _theme_button_image_apply(strip, textures[button_image["file"]], button_image, (x, 0, cell, height))
        _theme_image_apply_nine_slice(strip, textures[atlas_name], (x, 0, cell, height))
        if atlas_name == "ButtonDisabled":
            continue
        text = "More (3)" if is_more else _THEME_PREVIEW_LABELS[index]
        color = labels.get(atlas_name, theme["labelColor"]) or [1, 1, 1, 1]
        fill = tuple(int(channel * 255) for channel in color[:3]) + (255,)
        draw = ImageDraw.Draw(strip)
        text_box = draw.textbbox((0, 0), text, font=font)
        draw.text((x + (cell - (text_box[2] - text_box[0])) // 2, (height - (text_box[3] - text_box[1])) // 2 - text_box[1]), text, fill=fill, font=font)
    for left, x in ((True, 0), (False, arrow + cell * (count - 1))):
        _theme_image_apply_nine_slice(strip, textures["Button"], (x, 0, arrow, height))
        icon = textures["ArrowLeft" if left else "ArrowRight"].resize((arrow, arrow), Image.Resampling.LANCZOS)
        strip.alpha_composite(icon, (x, (height - arrow) // 2))
    return strip


############################################################################################################
#   _theme_decoration_derive_box — 裝飾圖的擺放（x, y, w, h），和 C# 的 Core/DecorationLayout.Place 同一套規則
############################################################################################################
def _theme_decoration_derive_box(decoration, image, target_box):
    size_x, size_y = decoration["size"]
    tex_w, tex_h = image.size
    if size_x <= 0 and size_y <= 0:
        box_w, box_h = tex_w, tex_h
    elif size_x <= 0:
        box_w, box_h = size_y * tex_w / tex_h, size_y
    elif size_y <= 0:
        box_w, box_h = size_x, size_x * tex_h / tex_w
    else:
        box_w, box_h = size_x, size_y
    target_x, target_y, target_w, target_h = target_box
    anchor, vertical = decoration["anchor"], decoration["verticalAnchor"]
    x = {"Left": target_x, "Center": target_x + (target_w - box_w) / 2, "Right": target_x + target_w - box_w}[anchor]
    y = {"Top": target_y, "Center": target_y + (target_h - box_h) / 2, "Bottom": target_y + target_h - box_h}[vertical]
    return int(round(x + decoration["offset"][0])), int(round(y + decoration["offset"][1])), int(round(box_w)), int(round(box_h))


############################################################################################################
#   _theme_button_image_list — buttonImage 加上 buttonImages，一份清單（產貼圖、寫檔用）
############################################################################################################
def _theme_button_image_list(theme):
    images = [theme["buttonImage"]] if theme.get("buttonImage") else []
    return images + list(theme.get("buttonImages") or [])


############################################################################################################
#   _theme_button_image_find — 某顆按鈕（defName）用的背景圖設定：buttonImages 裡指定的優先，否則 buttonImage，否則 None
############################################################################################################
def _theme_button_image_find(theme, button):
    for button_image in reversed(theme.get("buttonImages") or []):   # 驗證已擋掉重複；C# 遇到重複時後面的贏，這裡同規則
        if button_image["button"] == button:
            return button_image
    return theme.get("buttonImage")


############################################################################################################
#   _theme_button_image_apply — 把按鈕背景圖畫進格子 box（x, y, w, h）：扣掉 inset 後依 fit 與錨點擺放，
#   和 C# 的 Core/ImageFit 同一套規則（Cover 等比填滿、裁掉錨點另一側；Fit 等比放進去、放在錨點；其餘拉伸）
############################################################################################################
def _theme_button_image_apply(canvas, image, spec, box):
    x, y, width, height = box
    inset = int(round(spec["inset"]))
    x, y, width, height = x + inset, y + inset, width - 2 * inset, height - 2 * inset
    if width <= 0 or height <= 0:
        return
    image_w, image_h = image.size
    fit = spec["fit"] if spec["fit"] in ("Cover", "Fit") else "Stretch"
    horizontal = {"Left": 0.0, "Center": 0.5, "Right": 1.0}[spec["anchor"]]
    vertical = {"Top": 0.0, "Center": 0.5, "Bottom": 1.0}[spec["verticalAnchor"]]
    if fit == "Cover":
        scale = max(width / image_w, height / image_h)
        scaled = image.resize((max(width, round(image_w * scale)), max(height, round(image_h * scale))), Image.Resampling.BILINEAR)
        left = int(round((scaled.width - width) * horizontal))
        top = int(round((scaled.height - height) * vertical))
        piece, origin = scaled.crop((left, top, left + width, top + height)), (x, y)
    elif fit == "Fit":
        scale = min(width / image_w, height / image_h)
        piece = image.resize((max(1, round(image_w * scale)), max(1, round(image_h * scale))), Image.Resampling.BILINEAR)
        origin = (x + int(round((width - piece.width) * horizontal)), y + int(round((height - piece.height) * vertical)))
    else:
        piece, origin = image.resize((width, height), Image.Resampling.BILINEAR), (x, y)
    if spec["opacity"] < 1:
        piece.putalpha(piece.getchannel("A").point(lambda value: int(value * spec["opacity"])))
    canvas.alpha_composite(piece, origin)


############################################################################################################
#   _theme_image_build_mod_preview — About/Preview.png：標題、裝飾圖的位置、底部一段按鈕列
############################################################################################################
def _theme_image_build_mod_preview(theme, textures):
    width, height = _THEME_PREVIEW_SIZE
    canvas = Image.new("RGBA", _THEME_PREVIEW_SIZE, _THEME_PREVIEW_BACKGROUND)
    bar_box = (0, height - _THEME_BAR_HEIGHT, width, _THEME_BAR_HEIGHT)
    strip = _theme_image_build_bar_strip(theme, textures, (width, _THEME_BAR_HEIGHT))

    below = [decoration for decoration in theme["decorations"] if decoration["target"] == "Bar" and decoration["layer"] == "BelowButtons"]
    above = [decoration for decoration in theme["decorations"] if decoration["target"] == "Bar" and decoration["layer"] == "AboveButtons"]
    for group, after_strip in ((below, False), (above, True)):
        if after_strip:
            canvas.alpha_composite(strip, (bar_box[0], bar_box[1]))
        for decoration in group:
            image = textures[decoration["file"]]
            x, y, box_w, box_h = _theme_decoration_derive_box(decoration, image, bar_box)
            piece = image.resize((max(1, box_w), max(1, box_h)), Image.Resampling.BILINEAR)
            if decoration["opacity"] < 1:
                alpha = piece.getchannel("A").point(lambda value: int(value * decoration["opacity"]))
                piece.putalpha(alpha)
            canvas.alpha_composite(piece, (x, y))
    if not above:
        canvas.alpha_composite(strip, (bar_box[0], bar_box[1]))

    draw = ImageDraw.Draw(canvas)
    title_font = ImageFont.load_default(size=28)
    draw.text((24, 20), theme["mod"]["name"], fill=(225, 225, 215, 255), font=title_font)
    note_font = ImageFont.load_default(size=14)
    draw.text((24, 60), "Theme for " + _THEME_DEP_NAME, fill=(160, 160, 150, 255), font=note_font)
    return canvas


############################################################################################################
#   _theme_locate_gear — 齒輪來源：JSON 指定、本模組 repo、發佈檔的 ThemeTemplate/tools 位置
############################################################################################################
def _theme_locate_gear(theme, script_dir):
    candidates = []
    if theme.get("gearSource"):
        candidates.append(os.path.join(theme["_dir"], theme["gearSource"]))
    relative = os.path.join("Textures", "MainButtonBarSystem", "UI", "Gear.png")
    candidates.append(os.path.join(script_dir, "..", relative))
    candidates.append(os.path.join(script_dir, "..", "..", relative))
    for candidate in candidates:
        if os.path.exists(candidate):
            return candidate
    raise FileNotFoundError("Gear.png not found; set gearSource in the theme JSON. Tried: " + ", ".join(candidates))


############################################################################################################
#   _theme_palette_normalize — JSON 的 [r,g,b] 清單轉成 tuple（畫圖程式碼用 tuple 串接 alpha）
############################################################################################################
def _theme_palette_normalize(palette):
    normalized = dict(palette)
    for key in _THEME_PALETTE_COLOR_KEYS:
        value = palette.get(key)
        normalized[key] = tuple(value) if value else None
    return normalized


############################################################################################################
#   _theme_def_serialize_color — [r,g,b,a] → "(r, g, b, a)"，整數不帶小數點
############################################################################################################
def _theme_def_serialize_color(rgba):
    return "(" + ", ".join(f"{channel:g}" for channel in rgba) + ")"


############################################################################################################
#   _theme_def_serialize_vector — [x,y] → "(x, y)"
############################################################################################################
def _theme_def_serialize_vector(vector):
    return f"({vector[0]:g}, {vector[1]:g})"


############################################################################################################
#   _theme_def_serialize_button_image — 按鈕背景圖的欄位（texture、fit、錨點、opacity、inset），每行加縮排
############################################################################################################
def _theme_def_serialize_button_image(button_image, texture_prefix, indent):
    return [
        f"{indent}<texture>{texture_prefix}/{button_image['file']}</texture>",
        f"{indent}<fit>{button_image['fit']}</fit>",
        f"{indent}<anchor>{button_image['anchor']}</anchor>",
        f"{indent}<verticalAnchor>{button_image['verticalAnchor']}</verticalAnchor>",
        f"{indent}<opacity>{button_image['opacity']:g}</opacity>",
        f"{indent}<inset>{button_image['inset']:g}</inset>",
    ]


############################################################################################################
#   _theme_mod_write_about — About/About.xml，前置和載入順序都指向本模組
############################################################################################################
def _theme_mod_write_about(theme, out_dir):
    mod = theme["mod"]
    versions = "".join(f"    <li>{escape(version)}</li>\n" for version in mod.get("supportedVersions", ["1.7"]))
    description = mod.get("description") or f"A theme for {_THEME_DEP_NAME}: {theme['description']}"
    text = (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        "<ModMetaData>\n"
        f"  <name>{escape(mod['name'])}</name>\n"
        f"  <author>{escape(theme.get('author', ''))}</author>\n"
        f"  <packageId>{escape(mod['packageId'])}</packageId>\n"
        f"  <modVersion>{escape(mod.get('modVersion', '1.0.0'))}</modVersion>\n"
        "  <supportedVersions>\n" + versions + "  </supportedVersions>\n"
        "  <modDependencies>\n"
        "    <li>\n"
        f"      <packageId>{_THEME_DEP_PACKAGE}</packageId>\n"
        f"      <displayName>{_THEME_DEP_NAME}</displayName>\n"
        f"      <downloadUrl>{_THEME_DEP_URL}</downloadUrl>\n"
        "    </li>\n"
        "  </modDependencies>\n"
        "  <loadAfter>\n"
        f"    <li>{_THEME_DEP_PACKAGE}</li>\n"
        "  </loadAfter>\n"
        f"  <description>{escape(description)}</description>\n"
        "</ModMetaData>\n"
    )
    os.makedirs(os.path.join(out_dir, "About"), exist_ok=True)
    with open(os.path.join(out_dir, "About", "About.xml"), "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)


############################################################################################################
#   _theme_mod_write_defs — Defs/<name>_Themes.xml，寫出每個欄位（範本要讓人看得到全部欄位）
############################################################################################################
def _theme_mod_write_defs(theme, out_dir, texture_prefix):
    background = theme["background"]
    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "<Defs>",
        f"  <!-- Generated by make_theme.py from {os.path.basename(theme['_path'])}. Texture paths are relative to Textures/, without the extension. -->",
        f"  <{_THEME_DEF_TYPE}>",
        f"    <defName>{escape(theme['defName'])}</defName>",
        f"    <label>{escape(theme['label'])}</label>",
        f"    <description>{escape(theme['description'])}</description>",
        f"    <author>{escape(theme.get('author', ''))}</author>",
        f"    <displayOrder>{theme.get('displayOrder', 100)}</displayOrder>",
        "",
        "    <!-- Button atlases (nine-slice, square, 64x64 here) and label colours per state. -->",
        f"    <button>{texture_prefix}/Button</button>",
        f"    <buttonMouseover>{texture_prefix}/ButtonMouseover</buttonMouseover>",
        f"    <buttonActive>{texture_prefix}/ButtonActive</buttonActive>",
        f"    <buttonDisabled>{texture_prefix}/ButtonDisabled</buttonDisabled>",
    ]
    for field in ("labelColor", "labelMouseoverColor", "labelActiveColor"):
        if theme.get(field):
            lines.append(f"    <{field}>{_theme_def_serialize_color(theme[field])}</{field}>")
    lines += [
        "",
        "    <!-- Bar and drawer backgrounds. Fit: Atlas (nine-slice, default) / Tile / Stretch / Fit / Cover. -->",
        f"    <barBackground>{texture_prefix}/Bar</barBackground>",
        f"    <barBackgroundFit>{background['barFit']}</barBackgroundFit>",
        f"    <barBackgroundTileScale>{background['barTileScale']:g}</barBackgroundTileScale>",
        f"    <drawerBackground>{texture_prefix}/Drawer</drawerBackground>",
        f"    <drawerBackgroundFit>{background['drawerFit']}</drawerBackgroundFit>",
        f"    <drawerBackgroundTileScale>{background['drawerTileScale']:g}</drawerBackgroundTileScale>",
        "",
        f"    <moreButton>{texture_prefix}/More</moreButton>",
        f"    <arrowLeft>{texture_prefix}/ArrowLeft</arrowLeft>",
        f"    <arrowRight>{texture_prefix}/ArrowRight</arrowRight>",
        f"    <drawerSettingsIcon>{texture_prefix}/Gear</drawerSettingsIcon>",
        f"    <preview>{texture_prefix}/Preview</preview>",
    ]
    suggested = theme.get("suggestedLayout")
    if suggested:
        lines += ["", "    <!-- Layout the theme looks best with: offered in the Appearance tab, applied only when the player presses its button. -->",
                  "    <suggestedLayout>"]
        if suggested.get("displayModes"):
            lines.append("      <displayModes>" + "".join(f"<li>{escape(mode)}</li>" for mode in suggested["displayModes"]) + "</displayModes>")
        if suggested.get("buttonWidth"):
            lines.append(f"      <buttonWidth>{int(suggested['buttonWidth'])}</buttonWidth>")
        if suggested.get("halfButtonsFullWidth"):
            lines.append("      <halfButtonsFullWidth>true</halfButtonsFullWidth>")
        lines.append("    </suggestedLayout>")
    if theme["decorations"]:
        lines += ["", "    <!-- Decorations: any image at an anchor of the bar or the drawer, below or above the buttons. -->", "    <decorations>"]
        for decoration in theme["decorations"]:
            lines += [
                "      <li>",
                f"        <texture>{texture_prefix}/{decoration['file']}</texture>",
                f"        <target>{decoration['target']}</target>",
                f"        <anchor>{decoration['anchor']}</anchor>",
                f"        <verticalAnchor>{decoration['verticalAnchor']}</verticalAnchor>",
                f"        <offset>{_theme_def_serialize_vector(decoration['offset'])}</offset>",
                f"        <size>{_theme_def_serialize_vector(decoration['size'])}</size>",
                f"        <opacity>{decoration['opacity']:g}</opacity>",
                f"        <layer>{decoration['layer']}</layer>",
            ]
            if decoration["showWhen"]:
                lines.append("        <showWhen>" + "".join(f"<li>{state}</li>" for state in decoration["showWhen"]) + "</showWhen>")
            lines.append("      </li>")
        lines.append("    </decorations>")
    if theme.get("buttonImage") or theme.get("buttonImages"):
        lines += ["", "    <!-- Pictures inside the buttons, drawn under the button atlas: buttonImage for every button, buttonImages",
                  "         for named ones (button = the MainButtonDef's defName). Fit: Cover (default) / Fit / Stretch. -->"]
        if theme.get("buttonImage"):
            lines += ["    <buttonImage>"] + _theme_def_serialize_button_image(theme["buttonImage"], texture_prefix, "      ") + ["    </buttonImage>"]
        if theme.get("buttonImages"):
            lines.append("    <buttonImages>")
            for button_image in theme["buttonImages"]:
                lines += ["      <li>", f"        <button>{escape(button_image['button'])}</button>"]
                lines += _theme_def_serialize_button_image(button_image, texture_prefix, "        ") + ["      </li>"]
            lines.append("    </buttonImages>")
    lines += [f"  </{_THEME_DEF_TYPE}>", "</Defs>", ""]
    os.makedirs(os.path.join(out_dir, "Defs"), exist_ok=True)
    with open(os.path.join(out_dir, "Defs", theme["name"] + "_Themes.xml"), "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(lines))


############################################################################################################
#   _theme_mod_write_languages — 每種語言一個 DefInjected 檔
############################################################################################################
def _theme_mod_write_languages(theme, out_dir):
    for language, entry in sorted(theme.get("translations", {}).items()):
        folder = os.path.join(out_dir, "Languages", language, "DefInjected", _THEME_DEF_TYPE)
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, theme["name"] + ".xml"), "w", encoding="utf-8", newline="\n") as handle:
            handle.write('<?xml version="1.0" encoding="utf-8"?>\n<LanguageData>\n')
            for field in ("label", "description"):
                if entry.get(field):
                    handle.write(f"  <{theme['defName']}.{field}>{escape(entry[field])}</{theme['defName']}.{field}>\n")
            handle.write("</LanguageData>\n")


############################################################################################################
#   theme_build_textures — 主題的所有貼圖：10 張標準貼圖、背景覆蓋、裝飾圖；with_preview 時加遊戲內預覽
############################################################################################################
def theme_build_textures(theme, gear_source, with_preview=False):
    palette = theme["palette"]
    hollow = bool(palette.get("hollow"))   # 只影響四種狀態的按鈕（捲動箭頭用 Button，所以也是中空）；More 照舊有底色
    textures = {
        "Button": _theme_atlas_build_button(palette, palette["base"], palette["edge"], hollow=hollow),
        "ButtonMouseover": _theme_atlas_build_button(palette, _theme_color_derive_shade(palette["base"], 14), _theme_color_derive_shade(palette["edge"], 25), "mouseover", hollow),
        "ButtonActive": _theme_atlas_build_button(palette, palette["active"], _theme_color_derive_shade(palette["edge"], 35), "active", hollow),
        "ButtonDisabled": _theme_atlas_build_button(palette, _theme_color_derive_grey(_theme_color_derive_shade(palette["base"], -4)),
                                                    _theme_color_derive_grey(_theme_color_derive_shade(palette["edge"], -60)), "disabled", hollow),
        "Bar": _theme_atlas_build_bar(palette),
        "More": _theme_atlas_build_button(palette, _theme_color_derive_mix(palette["base"], palette["active"], 0.6), palette["edge"]),
        "Drawer": _theme_atlas_build_drawer(palette),
        "ArrowLeft": _theme_icon_build_arrow(palette, True),
        "ArrowRight": _theme_icon_build_arrow(palette, False),
        "Gear": _theme_icon_build_gear(palette, gear_source),
    }
    if palette.get("frame") == "vanilla":                               # 原版風格框：蓋掉四種狀態的按鈕、More、Bar
        for file_name, part in (("Button", "normal"), ("ButtonMouseover", "mouseover"), ("ButtonActive", "active"),
                                ("ButtonDisabled", "disabled"), ("More", "more"), ("Bar", "bar")):
            textures[file_name] = _theme_atlas_build_vanilla(part)
    background = theme["background"]
    for key, file_name in (("bar", "Bar"), ("drawer", "Drawer")):
        spec = background.get(key)
        if spec and spec.get("placeholder"):
            textures[file_name] = _theme_image_build_placeholder(spec["placeholder"])
        elif spec and spec.get("source"):
            textures[file_name] = _theme_image_load_source(theme, spec["source"])
    for decoration in theme["decorations"]:
        if decoration.get("placeholder"):
            textures[decoration["file"]] = _theme_image_build_placeholder(decoration["placeholder"])
        elif decoration.get("source"):
            textures[decoration["file"]] = _theme_image_load_source(theme, decoration["source"])
        else:
            raise ValueError(f"decoration {decoration['file']}: needs placeholder or source")
    for button_image in _theme_button_image_list(theme):
        if button_image.get("placeholder"):
            textures[button_image["file"]] = _theme_image_build_placeholder(button_image["placeholder"])
        elif button_image.get("source"):
            textures[button_image["file"]] = _theme_image_load_source(theme, button_image["source"])
        else:
            raise ValueError(f"button image {button_image['file']}: needs placeholder or source")
    if with_preview:
        textures["Preview"] = _theme_image_build_bar_strip(theme, textures, _THEME_STRIP_SIZE)
    return textures


############################################################################################################
#   theme_icon_build_mod — 本模組的圖示：一小段按鈕列、上方的抽屜、左邊一顆齒輪
############################################################################################################
def theme_icon_build_mod(gear_source):
    scale = 4
    size = 128
    image = Image.new("RGBA", (size * scale, size * scale))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle([0, 0, size * scale - 1, size * scale - 1], radius=22 * scale, fill=(34, 38, 46, 255))
    bar_top = 82 * scale
    colors = [(64, 72, 84), (76, 110, 150), (64, 72, 84)]
    for index, color in enumerate(colors):
        left = (12 + index * 36) * scale
        draw.rectangle([left, bar_top, left + 32 * scale, bar_top + 22 * scale], fill=color + (255,), outline=(110, 118, 128, 255), width=2 * scale)
    draw.rectangle([60 * scale, 30 * scale, 116 * scale, 76 * scale], fill=(46, 52, 62, 255), outline=(120, 128, 138, 255), width=2 * scale)
    for row in range(2):
        for column in range(2):
            left, top = (64 + column * 26) * scale, (34 + row * 20) * scale
            draw.rectangle([left, top, left + 22 * scale, top + 16 * scale], fill=(64, 72, 84, 255))
    image = image.resize((size, size), Image.LANCZOS)
    gear = Image.open(gear_source).convert("RGBA").resize((46, 46), Image.LANCZOS)
    image.alpha_composite(gear, (10, 22))
    return image


############################################################################################################
#   _theme_json_validate_fields — 列舉值、必填欄位、檔名衝突；不合法就拋 ValueError（才不會到畫圖時才用 KeyError 爆）
############################################################################################################
def _theme_json_validate_fields(theme, path):
    problems = []
    for required in ("name", "defName", "label", "description", "palette"):
        if required not in theme:
            problems.append(f"missing '{required}'")
    if problems:
        raise ValueError(f"{path}: " + "; ".join(problems))
    if not isinstance(theme["palette"], dict):
        raise ValueError(f"{path}: palette must be an object")
    for required in ("base", "edge", "rule", "accent", "active", "ornament"):
        if not theme["palette"].get(required):
            problems.append(f"palette.{required} is missing")
    if theme["palette"].get("ornament") and theme["palette"]["ornament"] not in _THEME_ORNAMENTS:
        problems.append(f"palette.ornament must be one of {', '.join(_THEME_ORNAMENTS)}")
    if "frame" in theme["palette"] and theme["palette"]["frame"] not in _THEME_FRAMES:
        problems.append(f"palette.frame must be one of {', '.join(_THEME_FRAMES)}")
    background = theme.get("background") or {}
    for key in ("barFit", "drawerFit"):
        if key in background and background[key] not in _THEME_FITS:
            problems.append(f"background.{key} must be one of {', '.join(_THEME_FITS)}")
    used_files = {name.lower() for name in _THEME_TEXTURE_FILES} | {"preview"}   # 檔案系統不分大小寫，比對也不分
    for index, decoration in enumerate(theme.get("decorations") or []):
        where = f"decorations[{index}]"
        file_name = decoration.get("file")
        if not file_name or not isinstance(file_name, str):
            problems.append(f"{where}: file is missing (a file name without extension)")
        elif file_name.lower() in used_files:
            problems.append(f"{where}: file '{file_name}' clashes with another texture of the theme")
        else:
            used_files.add(file_name.lower())
        for key, allowed in (("target", _THEME_TARGETS), ("anchor", _THEME_ANCHORS), ("verticalAnchor", _THEME_VERTICAL_ANCHORS), ("layer", _THEME_LAYERS)):
            if key in decoration and decoration[key] not in allowed:
                problems.append(f"{where}: {key} must be one of {', '.join(allowed)}")
        for state in decoration.get("showWhen") or []:
            if state not in _THEME_STATES:
                problems.append(f"{where}: showWhen value '{state}' must be one of {', '.join(_THEME_STATES)}")
        if not decoration.get("placeholder") and not decoration.get("source"):
            problems.append(f"{where}: needs placeholder or source")
    suggested = theme.get("suggestedLayout")
    if suggested is not None:
        if not isinstance(suggested, dict):
            problems.append("suggestedLayout must be an object")
        else:
            modes = suggested.get("displayModes", [])
            if not isinstance(modes, list) or not all(isinstance(mode, str) and mode for mode in modes):
                problems.append("suggestedLayout.displayModes must be a list of display mode defNames")
            width = suggested.get("buttonWidth", 0)
            if not isinstance(width, int) or isinstance(width, bool) or width < 0:
                problems.append("suggestedLayout.buttonWidth must be a whole number, 0 or more")
            if not isinstance(suggested.get("halfButtonsFullWidth", False), bool):
                problems.append("suggestedLayout.halfButtonsFullWidth must be true or false")
    button_images = []
    if theme.get("buttonImage") is not None:
        if not isinstance(theme["buttonImage"], dict):
            problems.append("buttonImage must be an object")
        else:
            button_images.append(("buttonImage", theme["buttonImage"]))
            if "button" in theme["buttonImage"]:
                problems.append("buttonImage: button is only used in buttonImages")
    if theme.get("buttonImages") is not None and not isinstance(theme["buttonImages"], list):
        problems.append("buttonImages must be a list")
    else:
        seen_buttons = set()
        for index, button_image in enumerate(theme.get("buttonImages") or []):
            where = f"buttonImages[{index}]"
            if not isinstance(button_image, dict):
                problems.append(f"{where} must be an object")
                continue
            button_images.append((where, button_image))
            button = button_image.get("button")
            if not button or not isinstance(button, str):
                problems.append(f"{where}: button is missing (the MainButtonDef's defName, e.g. Architect)")
            elif button in seen_buttons:
                problems.append(f"{where}: button '{button}' is listed twice")
            else:
                seen_buttons.add(button)
    button_files = {}   # 檔名（小寫）→ 來源；按鈕圖之間可以共用同一個檔（同一張圖給好幾顆按鈕），來源要相同
    for where, button_image in button_images:
        file_name = button_image.get("file")
        origin = json.dumps({"source": button_image.get("source"), "placeholder": button_image.get("placeholder")}, sort_keys=True)
        if not file_name or not isinstance(file_name, str):
            problems.append(f"{where}: file is missing (a file name without extension)")
        elif file_name.lower() in used_files:
            problems.append(f"{where}: file '{file_name}' clashes with another texture of the theme")
        elif file_name.lower() in button_files and button_files[file_name.lower()] != origin:
            problems.append(f"{where}: file '{file_name}' is used by another button picture with a different source or placeholder")
        else:
            button_files[file_name.lower()] = origin
        for key, allowed in (("fit", _THEME_FITS), ("anchor", _THEME_ANCHORS), ("verticalAnchor", _THEME_VERTICAL_ANCHORS)):
            if key in button_image and button_image[key] not in allowed:
                problems.append(f"{where}: {key} must be one of {', '.join(allowed)}")
        opacity = button_image.get("opacity", 1)
        if not isinstance(opacity, (int, float)) or not 0 <= opacity <= 1:
            problems.append(f"{where}: opacity must be a number from 0 to 1")
        inset = button_image.get("inset", 0)
        if not isinstance(inset, (int, float)) or inset < 0:
            problems.append(f"{where}: inset must be a number of pixels, 0 or more")
        if not button_image.get("placeholder") and not button_image.get("source"):
            problems.append(f"{where}: needs placeholder or source")
    if problems:
        raise ValueError(f"{path}: " + "; ".join(problems))


############################################################################################################
#   theme_load — 讀 JSON，檢查，補預設值，顏色轉 tuple，記住 JSON 的位置（source 路徑相對於它）
############################################################################################################
def theme_load(path):
    with open(path, encoding="utf-8") as handle:
        theme = json.load(handle)
    _theme_json_validate_fields(theme, path)
    theme["_path"] = os.path.abspath(path)
    theme["_dir"] = os.path.dirname(theme["_path"])
    theme["palette"] = _theme_palette_normalize(theme["palette"])
    theme["background"] = {**_THEME_BACKGROUND_DEFAULTS, **(theme.get("background") or {})}
    theme["decorations"] = [{**_THEME_DECORATION_DEFAULTS, **decoration} for decoration in theme.get("decorations") or []]
    theme["buttonImage"] = {**_THEME_BUTTON_IMAGE_DEFAULTS, **theme["buttonImage"]} if theme.get("buttonImage") else None
    theme["buttonImages"] = [{**_THEME_BUTTON_IMAGE_DEFAULTS, **image} for image in theme.get("buttonImages") or []]
    for field in ("labelColor", "labelMouseoverColor", "labelActiveColor"):
        theme.setdefault(field, None)
    return theme


############################################################################################################
#   theme_write_textures — 只寫貼圖（內建主題：gen_theme_textures.py 傳入 repo 的貼圖資料夾）
############################################################################################################
def theme_write_textures(theme, folder, gear_source, with_preview=False):
    textures = theme_build_textures(theme, gear_source, with_preview)
    os.makedirs(folder, exist_ok=True)
    for file_name, image in textures.items():
        image.save(os.path.join(folder, file_name + ".png"), optimize=True)
    return textures


############################################################################################################
#   theme_write_mod — 完整主題模組：About、Defs、Textures、Languages、About/Preview.png
############################################################################################################
def theme_write_mod(theme, out_dir, gear_source):
    if "mod" not in theme:
        raise ValueError(f"{theme['_path']}: a full theme mod needs the 'mod' section (packageId, name, texturesRoot)")
    textures_root = theme["mod"].get("texturesRoot") or theme["name"]
    texture_folder = os.path.join(out_dir, "Textures", textures_root)
    textures = theme_write_textures(theme, texture_folder, gear_source, with_preview=True)
    _theme_mod_write_about(theme, out_dir)
    _theme_mod_write_defs(theme, out_dir, textures_root)
    _theme_mod_write_languages(theme, out_dir)
    _theme_image_build_mod_preview(theme, textures).convert("RGB").save(os.path.join(out_dir, "About", "Preview.png"), optimize=True)
    return textures


############################################################################################################
#   main — 命令列入口
############################################################################################################
def main():
    parser = argparse.ArgumentParser(description="Make a Main Button Bar System theme from a JSON palette.")
    parser.add_argument("theme_json")
    parser.add_argument("out_dir")
    parser.add_argument("--textures-only", action="store_true", help="write only the PNG files into out_dir")
    args = parser.parse_args()

    theme = theme_load(args.theme_json)
    gear_source = _theme_locate_gear(theme, os.path.dirname(os.path.abspath(__file__)))
    if args.textures_only:
        textures = theme_write_textures(theme, args.out_dir, gear_source)
        print(f"{theme['name']}: {len(textures)} textures -> {args.out_dir}")
    else:
        textures = theme_write_mod(theme, args.out_dir, gear_source)
        print(f"{theme['name']}: theme mod with {len(textures)} textures -> {args.out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
