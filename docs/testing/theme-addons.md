# 主題附屬模組測試清單

分支：`claude/theme-addons`（從 main `7cf90c7` 開出）。設計：`docs/superpowers/specs/2026-11-04-theme-addons-design.md`。

這一輪的重點是「外部主題模組」：一個只放在 `Mods/`、完全不改本模組的主題模組，要能被載入、顯示、調整。範例主題 `docs/examples/ExampleTheme` 就是為這個情境產生的；萘伊主題（`MBBS_Nai`，內建）走同一套欄位，一起驗。

## 準備

1. 排序：用 `ModsConfig_20261104_1231_mbbs_selftest_cleanlog.xml`（23 個）那種小排序，確定本模組是這個分支編出來的 DLL（`1.7/Assemblies/MainButtonBarSystem.dll`）。
2. 外部主題模組：把 `docs/examples/ExampleTheme` **整個資料夾複製**到 `RimWorld/Mods/ExampleTheme`（不要用 symlink，不要改本模組）。在 RimCrow 啟用它，排在本模組後面（它的 About 有 `loadAfter`，自動排序應該會處理）。
3. 開發者模式開著（啟動檢查和重載按鈕都只在開發者模式出現）。
4. 玩完要看 log 的話，重開遊戲前先把 `Player.log` 複製出來。

## A. 載入

- [ ] A1 進主選單沒有本模組或 ExampleTheme 的紅字；log 有 `[Main Button Bar System] Loaded.`。
- [ ] A2 開發者模式的診斷報告（`Diagnostics (dev mode)`）多了一行 `Themes: 8 theme(s), 0 warning(s)`（原版 1 ＋內建 6 ＋ ExampleTheme 1）。
- [ ] A3 設定視窗 → 外觀分頁：卡片依序是 原版、帝國、信條、基因實驗室、虛空、星航、萘伊、Example Steel（`displayOrder` 100 排最後）。Example 那張的作者行是「Your name（Example Theme for Main Button Bar System）」，預覽是它自己的 `Preview` 圖（一段按鈕列）。
- [ ] A4 語言切到繁中：Example 卡片標籤「範例鋼鐵」、說明是中文；萘伊卡片「萘伊」。切回原本的語言後再開一次設定視窗，卡片正常（play data reload 後 def 是新物件）。

## B. 外部主題的外觀

選 ExampleTheme，存檔設定，進地圖：

- [ ] B1 底部列背景是一整張拉伸的圖（`Stretch`），能看到佔位圖的對角線橫跨整條列；不是九宮格那種角落放大的樣子。
- [ ] B2 左下角有一張 160×240 的「PORTRAIT (PLACEHOLDER)」色塊，底邊貼齊螢幕底、往上長出列，**按鈕畫在它上面**（`BelowButtons`）。
- [ ] B3 滑鼠移到底部列上（任何一顆按鈕或空白處）時，右端往上 40px 出現 48×48 的「HOVER」色塊（`AboveButtons`、`showWhen` Mouseover＋DrawerOpen）；滑鼠離開列就消失。滑鼠移到立繪長出去的部分（列上方）**不算**移上。
- [ ] B4 顯示方式切「摺疊」、按「更多」打開抽屜：HOVER 色塊在抽屜開著時持續顯示（DrawerOpen）；抽屜右上角有 64×64 的「DRAWER」色塊，在格子和齒輪上面；抽屜背景是九宮格 atlas（`Atlas`）。
- [ ] B5 文字三種顏色：一般琥珀 `(1, 0.8, 0.4)`、滑鼠移上淡黃 `(1, 0.95, 0.7)`、分頁開著時接近白 `(1, 1, 0.85)`（`labelActiveColor`）。開一個分頁（例如「工作」）看它的按鈕文字比滑鼠移上的更白。
- [ ] B6 捲動模式：箭頭、More 按鈕都是 ExampleTheme 的貼圖；長出列外的立繪不受捲動影響。

## C. 玩家可調的值（每一列都有「預設值」按鈕）

外觀分頁下方「插圖與背景」一節（選到有插圖的主題才會出現滑桿；選原版時只有一行灰字）：

- [ ] C1 背景不透明度拉到 30%：底部列背景變淡，按鈕不變；外觀分頁上每張主題卡的預覽**不變**（預覽固定用 100%）；按「預設值」回 100%。
- [ ] C2 「顯示裝飾圖」關掉：立繪、HOVER、DRAWER 全部消失；開回來全部回來；「預設值」按鈕在改過後才可按。
- [ ] C3 裝飾圖不透明度 50%：三張圖都半透明（HOVER 本身 0.9 × 0.5）。
- [ ] C4 裝飾圖大小 200%：立繪變 320×480，**左下角不動**（錨點不變）；50% 時 80×120。
- [ ] C5 裝飾圖位移 X 100、Y −50：三張圖一起往右 100、往上 50；滑桿可以拉到負值（−400～400），數字框可以直接輸入負數。
- [ ] C6 以上每項的「預設值」按鈕各按一次都回到預設；右下「全部重設」也會把這一節全部重設（和主題一起）。
- [ ] C7 設定視窗開著時改滑桿，底部列**即時**跟著變（預覽用 working copy）；按「取消」關閉後恢復存檔的值。
- [ ] C8 在主選單（沒進遊戲）改這些值並存成範本，開新局後套用。

## D. 開發者模式的檢查與重載

- [ ] D1 關遊戲，把 `Mods/ExampleTheme/Textures/ExampleTheme/HoverBadge.png` 改名成 `HoverBadge_x.png`，重開：主選單 log 有一筆黃字 `Theme ExampleTheme_Steel (Example Theme for Main Button Bar System), decorations[1].texture: texture 'ExampleTheme/HoverBadge' not found ...`；診斷報告的 Themes 那行是 `1 warning(s)`。進地圖、滑鼠移上底部列之後，log **沒有**再多一筆 `texture 'ExampleTheme/HoverBadge' not found; using the game's look for it`（同一個缺圖整場只記一次）。其他裝飾正常、HOVER 不顯示、沒有紅字。改回檔名。
- [ ] D2 把 `Button.png` 換成一張 64×48 的圖（不是正方形）：黃字 `... button: atlas 'ExampleTheme/Button' is 64x48, not square ...`。換回。
- [ ] D3 關掉開發者模式重開 D1 的情況：主選單 log 沒有 `Theme ExampleTheme_Steel ... decorations[1]` 那種帶欄位名的檢查警告；進地圖、滑鼠移上底部列後只有一筆 `[Main Button Bar System] Theme ExampleTheme_Steel: texture 'ExampleTheme/HoverBadge' not found; using the game's look for it.`（這是原本就有的行為），之後不再重複。開回開發者模式。
- [ ] D4 重載：遊戲開著、進地圖、選 ExampleTheme，用小畫家把 `Portrait.png` 整張塗成紅色存檔，外觀分頁按「重新載入主題貼圖」：畫面上立繪立刻變紅，左下角出現訊息「主題貼圖已重新載入：重新讀取了 N 個檔案」（N ≥ 14：ExampleTheme 的 14 張；內建主題在本模組資料夾裡也會被重讀）。沒有紅字。
- [ ] D5 重載後再按一次、再按第三次：仍然正常，沒有例外、畫面沒有破圖（上一次重載的貼圖在這次被釋放，正在畫的是新的一批）。
- [ ] D6 重載按鈕只在開發者模式顯示：關掉開發者模式後外觀分頁沒有這顆按鈕。

## E. 舊主題與舊存檔不變

- [ ] E1 帝國、信條、基因實驗室、虛空、星航五款各選一次：底部列、抽屜、預覽和第八輪一樣（它們的 Def 沒有新欄位，走 `Atlas` 路徑）。「插圖與背景」一節對它們只顯示「背景不透明度」（有 `barBackground`）。
- [ ] E2 原版主題：底部列由遊戲畫（`IsUntouched`），「插圖與背景」只有灰字。
- [ ] E3 讀第八輪留下的存檔（`MBBS_round7_test`）：外觀和當時相同，log 沒有 `decorationsShown`／`backgroundOpacity` 相關的錯誤（舊存檔沒有這些欄位，讀成「未設定」）。
- [ ] E4 存檔後看存檔 XML：沒改過的新欄位**不會**寫進存檔（`Scribe_Values.Look` 的預設值）；改過的（例如 `decorationScale`）才出現。

## F. 萘伊主題（內建）

- [ ] F1 選萘伊：左下角 300×420 的半透明立繪（暫代圖，從既有插圖裁的）在按鈕後面、往上長出列，透明度 45%，透過它看得到地圖；底部列背景是一般的 atlas 底條；沒有別的裝飾。
- [ ] F2 文字：一般柔白、滑鼠移上淡紫、分頁開著接近白。
- [ ] F3 按鈕角落的「petal」裝飾（小圓點＋短斜線）在四個角都看得到；按鈕、More、箭頭、齒輪都是灰白邊框。
- [ ] F4 外觀分頁的萘伊卡片用畫出來的預覽（沒有 `preview` 圖）：預覽裡是底條＋按鈕，**沒有**立繪長出卡片。
- [ ] F5 「插圖與背景」一節對萘伊只顯示背景不透明度和裝飾圖的五項；把裝飾圖不透明度拉到 100%，立繪變不透明；大小 150% 時左下角不動。

## G. 工具與範本

- [ ] G1 在 repo 根目錄：`python -I -X utf8 tools/make_theme.py tools/themes/ExampleTheme.json docs/examples/ExampleTheme` 之後 `git status` 乾淨（同一台機器、同一個 Pillow 時）。
- [ ] G2 `python -I -X utf8 tools/gen_theme_textures.py` 之後，五款舊主題的 PNG 像素和提交版相同（位元組可能因 Pillow 版本不同而不同；用 Pillow 比像素）。
- [ ] G3 `python -I -X utf8 tools/make_release.py <repo 外的資料夾>`：輸出有 `ThemeTemplate/ExampleTheme`、`ThemeTemplate/tools/make_theme.py`、`API.zh-TW.md`；在 `ThemeTemplate/tools/` 裡執行 `make_theme.py ExampleTheme.json <資料夾> --textures-only` 能找到齒輪來源圖。
- [ ] G4 把發佈資料夾放進 `Mods/`（暫時停用 repo 那份）進遊戲：`ThemeTemplate/` 裡的 Defs 和 Textures **沒有**被載入（外觀分頁沒有第二個 Example 卡片、log 沒有重複 defName 的錯誤）。

## 要帶回的資料

- 各項通過／不通過；不通過的寫實際看到什麼，最好附截圖。
- B2、B3、B4、C4 的截圖（立繪位置、HOVER 出現、抽屜徽章、縮放後錨點不動）。
- D1 的那行黃字原文。
- `Player.log` 裡所有 `[Main Button Bar System]` 開頭的行。
