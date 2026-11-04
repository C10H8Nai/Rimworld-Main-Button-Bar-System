#!/usr/bin/env python3
## 部署路徑：tools/gen_theme_textures.py
##
## 說明：畫內建主題的貼圖和模組圖示（需要 Pillow）。每款主題的配色在 tools/themes/<主題>.json，畫圖的程式碼在
##   tools/make_theme.py（主題作者也用同一支工具）。改一個顏色再跑一次就好。沒有用到遊戲或任何 DLC 的美術：
##   顏色是從擴充的 logo、美術和遊戲資料量出來的（docs/research/dlc-visuals-2026-11-04.md），形狀是簡單的角落裝飾。
##
## 用法（在 repo 根目錄）：
##   python -I -X utf8 tools/gen_theme_textures.py
##
## 寫出 Textures/MainButtonBarSystem/Themes/<主題>/*.png 和 About/ModIcon.png。
## 注意：PNG 的位元組會隨 Pillow 版本不同（編碼器差異），像素不會；比對用像素。
##
## 架構：
##   main() — 逐款主題呼叫 make_theme.theme_write_textures，再寫模組圖示
##
## 修改日誌
## V1.0：2026-11-04  畫圖程式碼移到 make_theme.py，本檔只剩呼叫；主題清單加 Nai
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_theme                                                       # noqa: E402（要先加 sys.path）

_GEN_ROOT        = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
_GEN_THEMES_DIR  = os.path.join(_GEN_ROOT, "tools", "themes")
_GEN_OUT         = os.path.join(_GEN_ROOT, "Textures", "MainButtonBarSystem", "Themes")
_GEN_GEAR        = os.path.join(_GEN_ROOT, "Textures", "MainButtonBarSystem", "UI", "Gear.png")
_GEN_BUILTIN     = ("Imperial", "Creed", "GeneLab", "Void", "Starfarer", "Nai")   # 內建主題，顯示順序


############################################################################################################
#   main — 逐款主題寫貼圖，最後寫 About/ModIcon.png
############################################################################################################
def main():
    for name in _GEN_BUILTIN:
        theme = make_theme.theme_load(os.path.join(_GEN_THEMES_DIR, name + ".json"))
        textures = make_theme.theme_write_textures(theme, os.path.join(_GEN_OUT, name), _GEN_GEAR)
        print(f"{name}: {len(textures)} textures")
    make_theme.theme_icon_build_mod(_GEN_GEAR).save(os.path.join(_GEN_ROOT, "About", "ModIcon.png"), optimize=True)
    print("About/ModIcon.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
