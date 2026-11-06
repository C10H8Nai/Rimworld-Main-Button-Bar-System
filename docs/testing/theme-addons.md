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
- [ ] D4 重載：遊戲開著、進地圖、選 ExampleTheme，用小畫家把 `Portrait.png` 整張塗成紅色存檔，外觀分頁按「重新載入主題貼圖」：畫面上立繪立刻變紅，左下角出現訊息「主題貼圖已重新載入：重新讀取了 N 個檔案」（N ≥ 17：ExampleTheme 的 17 張，含 3 張按鈕圖；內建主題在本模組資料夾裡也會被重讀）。沒有紅字。
- [ ] D5 重載後再按一次、再按第三次：仍然正常，沒有例外、畫面沒有破圖（上一次重載的貼圖在這次被釋放，正在畫的是新的一批）。
- [ ] D6 重載按鈕只在開發者模式顯示：關掉開發者模式後外觀分頁沒有這顆按鈕。

## E. 舊主題與舊存檔不變

- [ ] E1 帝國、信條、基因實驗室、虛空、星航五款各選一次：底部列、抽屜、預覽和第八輪一樣（它們的 Def 沒有新欄位，走 `Atlas` 路徑）。「插圖與背景」一節對它們只顯示「背景不透明度」（有 `barBackground`）。
- [ ] E2 原版主題：底部列由遊戲畫（`IsUntouched`），「插圖與背景」只有灰字。
- [ ] E3 讀第八輪留下的存檔（`MBBS_round7_test`）：外觀和當時相同，log 沒有 `decorationsShown`／`backgroundOpacity` 相關的錯誤（舊存檔沒有這些欄位，讀成「未設定」）。
- [ ] E4 存檔後看存檔 XML：沒改過的新欄位**不會**寫進存檔（`Scribe_Values.Look` 的預設值）；改過的（例如 `decorationScale`）才出現。

## F. 萘伊主題（內建）

- [ ] F1 選萘伊：每顆主按鈕裡有一張萘伊像素圖，沒有立繪、沒有裝飾圖。有圖示的按鈕是萘伊雙手拿著道具、人在按鈕左邊（建築＝攤開的藍圖、研究＝燒瓶、健康＝醫療箱、動物＝灰貓、歷史＝日誌⋯，對照在 Def 的 `buttonImages`）；沒圖示的模組按鈕是萘伊從黑板後面探頭、文字落在黑板上；沒列到的按鈕只有黑板。
- [ ] F2 文字：一般柔白、滑鼠移上淡紫、分頁開著接近白。
- [ ] F3 按鈕框是原版風格（灰邊＋一條暗內線、中間透出圖），底部列背景是原版按鈕的深色；滑鼠移上時邊框變亮、蒙一層很淡的白，分頁開著時邊框最亮；More 是實心的原版樣式；抽屜、捲動箭頭、齒輪是萘伊配色（灰白）。
- [ ] F4 外觀分頁的萘伊卡片用畫出來的預覽（沒有 `preview` 圖）：預覽裡是底條＋按鈕＋按鈕裡的圖。
- [ ] F5 「插圖與背景」一節對萘伊顯示哪些項目，記下來（萘伊已經沒有裝飾圖）；背景不透明度不影響按鈕圖。
- [ ] F6 按鈕窄到 80 以下時，有圖示的按鈕只剩萘伊和道具（靠左裁切），黑板版只剩黑板中段（置中裁切）；比 20 窄時只畫圖。開發者模式下，`buttonImages` 指到沒載入的按鈕會出黃字（main 每顆一條，`claude/round-12-fixes` 合併後每款主題一條），不是紅字。

## G. 工具與範本

- [ ] G1 在 repo 根目錄：`python -I -X utf8 tools/make_theme.py tools/themes/ExampleTheme.json docs/examples/ExampleTheme` 之後 `git status` 乾淨（同一台機器、同一個 Pillow 時）。
- [ ] G2 `python -I -X utf8 tools/gen_theme_textures.py` 之後，五款舊主題的 PNG 像素和提交版相同（位元組可能因 Pillow 版本不同而不同；用 Pillow 比像素）。
- [ ] G3 `python -I -X utf8 tools/make_release.py <repo 外的資料夾>`：輸出有 `ThemeTemplate/ExampleTheme`、`ThemeTemplate/tools/make_theme.py`、`API.zh-TW.md`；在 `ThemeTemplate/tools/` 裡執行 `make_theme.py ExampleTheme.json <資料夾> --textures-only` 能找到齒輪來源圖。
- [ ] G4 把發佈資料夾放進 `Mods/`（暫時停用 repo 那份）進遊戲：`ThemeTemplate/` 裡的 Defs 和 Textures **沒有**被載入（外觀分頁沒有第二個 Example 卡片、log 沒有重複 defName 的錯誤）。

## H. 主按鈕背景圖（`buttonImage`／`buttonImages`）

ExampleTheme 的設定：預設圖 `ButtonPicture`（240×72「BUTTON PICTURE (PLACEHOLDER)」）給每顆按鈕；「建築」（Architect）用 200×300 的棕色「ARCHITECT」（`verticalAnchor` Top、`inset` 2）；「研究」（Research）用 256×64 的藍綠「RESEARCH」（`anchor` Left、`opacity` 0.85、`inset` 2）。按鈕 atlas 是 `hollow`（中間透明的框）。

- [ ] H1 指定的按鈕有自己的圖：建築是**棕色**底（ARCHITECT 佔位圖是 200×300、用 Cover＋Top，按鈕只露出上緣一小段，看得到佔位圖的上框線和對角線，**看不到「ARCHITECT」字樣**是正常的）；研究是藍綠底、看得到「RESEARCH」的左半；其他按鈕是 BUTTON PICTURE。圖在框裡面，文字和圖示畫在圖上面。
- [ ] H2 沒指定的按鈕用預設，沒有預設時只剩框：把 `Mods/ExampleTheme/Defs/ExampleTheme_Themes.xml` 的 `<buttonImage>…</buttonImage>` 整段刪掉重開：只有建築、研究有圖，其他按鈕只剩框（透出底部列背景）。改回。
- [ ] H3 抽屜格子也有：摺疊模式把建築、研究和另一顆收進抽屜，格子裡的圖和底部列上一樣（建築、研究是自己的圖，另一顆是預設圖）。
- [ ] H4 四種狀態都有圖：一般、滑鼠移上、分頁開著、停用（遊戲裡「組成商隊」→「選擇路線」，路線規劃期間「世界」等按鈕停用）。滑鼠移上和按下的效果只在框上，**圖不會被染藍**。
- [ ] H5 Cover 的錨點：把建築的單顆寬度調到 120：ARCHITECT 佔位圖看得到**上緣**（佔位圖的外框上邊在、下邊被裁掉）；研究看得到**左緣**。把 Defs 裡建築的 `verticalAnchor` 改成 `Bottom` 重開，換成看到下緣。改回。
- [ ] H6 inset：建築、研究的圖離按鈕邊緣 2px，預設圖貼齊邊緣。把建築的 `inset` 改成 8 重開，圖明顯內縮一圈。改回。
- [ ] H7 缺圖只記一次：開發者模式，把 `ButtonResearch.png` 改名重開：主選單 log 只有一筆黃字 `… buttonImages[1].texture: texture 'ExampleTheme/ButtonResearch' not found …`；進地圖、滑鼠移過研究按鈕後不再多記；研究按鈕只剩框（指定了圖但缺檔時不改用預設圖）；沒有紅字。改回。
- [ ] H8 舊主題外觀不變：原版、帝國、信條、基因實驗室、虛空、星航、萘伊各選一次，和這次改版前相同（它們沒有按鈕圖）。
- [ ] H9 預覽：外觀分頁的 Example 卡片用它自己的 `Preview` 圖（make_theme.py 產的預覽條，Architect 那格是 ARCHITECT 圖、其他格是預設圖）。把 Defs 的 `<preview>` 刪掉重開，改用畫出來的預覽：樣本按鈕（原版排序前四顆）裡 Architect 是自己的棕色圖、其他是預設圖，停用的那格也有圖。改回。
- [ ] H10 「只顯示圖示」：把建築設成只顯示圖示，圖示畫在圖上、沒有文字；「圖示＋文字」時圖示和文字都在圖上。
- [ ] H11 只有按鈕圖的主題：把 ExampleTheme 的 Defs 裡 `<button>`、`<buttonMouseover>`、`<buttonActive>`、`<buttonDisabled>` 四行和三個文字顏色（`labelColor` 等）刪掉重開：按鈕圖畫在遊戲原本的按鈕底下，遊戲的按鈕底不透明，所以**完全看不到**（2026-11-06 實機：像素和沒圖的按鈕相同）。開發模式下主選單 log 有一筆黃字 `Theme ExampleTheme_Steel (…), button: the theme has button pictures but no button atlas, …`，沒有紅字。改回。
- [ ] H12 Fit 與 Stretch：把研究的 `fit` 改成 `Fit`：整張 RESEARCH 圖都看得到，靠左（`anchor` Left），右邊留空；改成 `Stretch`：拉伸填滿、文字變形。改回。
- [ ] H13 開發者模式的提示：在 `buttonImages` 加一個 `button` 寫成 `Achitect` 的項目重開：一筆黃字 `Theme ExampleTheme_Steel (…), buttonImages: 1 picture(s) name main buttons that are not loaded (a typo, or a mod that is not active), so they are unused: Achitect`（沒載入的按鈕每款主題合併成一條），沒有紅字。改回。
- [ ] H14 設定錯誤（選做）：在 Defs 加一個沒寫 `<button>` 的 `buttonImages` 項目重開，log 有 `buttonImages[2]: button is empty` 的設定錯誤；`opacity` 寫 2 有 `opacity must be between 0 and 1`。改回。

## 要帶回的資料

- 各項通過／不通過；不通過的寫實際看到什麼，最好附截圖。
- B2、B3、B4、C4 的截圖（立繪位置、HOVER 出現、抽屜徽章、縮放後錨點不動）。
- H1、H3、H4（滑鼠移上與停用）、H5 的截圖。
- D1 的那行黃字原文。
- `Player.log` 裡所有 `[Main Button Bar System]` 開頭的行。
