# 主題附屬模組（theme-addons）F、H 節 實機測試結果

- 分支：`claude/round-12-fixes` @ `e890556`（含 main `0f8e673` 的萘伊像素主題 #17）
- 測試日期：2026-11-06（Claude 用 computer use 操作，本機），遊戲 1.7.5000 rev1275，開發模式，2560×1440，簡中
- 清單：`docs/testing/theme-addons.md` 的 F（萘伊主題）、H（按鈕背景圖）。A～E、G 這次沒做
- 排序：`ModsConfig_20261106_1039_mbbs_r12fix_main.xml`（82 個，含 VTE、TooManyButtons、ExampleTheme），存檔 `MBBS_r12_nai`（萘伊）、`MBBS_r12_imperial`（在裡面改選示例钢铁，沒存回去）
- ExampleTheme 是 repo `docs/examples/ExampleTheme` 原樣複製到 `Mods\ExampleTheme`。H 節要改 Def 或檔名的項目分三批改 Mods 那份、每批重開，測完從 repo 原檔還原、模組移走

## 總結

| 節 | 結果 | 說明 |
|---|---|---|
| F1～F6 萘伊主題 | ✅（附觀察） | 圖、文字顏色、框、預覽、背景不透明度、窄按鈕都照清單。⚠️ 按鈕寬度普遍不夠時畫面很擠，使用者看過後決定維持現狀（見 F 節後的「觀察」） |
| H1～H10、H12～H14 | ✅ | 自己的圖、預設圖、錨點、inset、缺圖只記一次、Fit／Stretch、錯字和設定錯誤的提示都照清單 |
| H4 停用狀態 | 沒做 | 要組商隊進入選路線，這次沒操作 |
| H11 只有按鈕圖的主題 | ⚠️ | 圖有畫，但被遊戲的按鈕底圖完全蓋住，像素和沒圖的按鈕一模一樣，不是清單寫的「看起來很暗」 |

## F. 萘伊主題

| # | 結果 |
|---|---|
| F1 | ✅ 有圖示的按鈕是萘伊在左邊拿著道具（研究＝燒瓶、醫療＝醫療箱、工作＝工具箱…），沒圖示的模組按鈕是萘伊從黑板後面探頭，沒有立繪和裝飾圖（`ta_f2f3_nai_states.png`、`ta_f6_narrow_nai.png`） |
| F2 | ✅ 文字一般柔白、滑鼠移上淡紫、分頁開著接近白（`ta_f2f3_nai_states.png` 的「方案」三列） |
| F3 | ✅ 原版風格框；滑鼠移上邊框變亮、蒙一層淡白；分頁開著邊框最亮。抽屜深藍、齒輪和捲動箭頭灰白 |
| F4 | ✅ 外觀分頁的萘伊卡片是畫出來的預覽，有底條、按鈕和按鈕裡的圖，四種狀態都有（`ta_f4_cards.png`） |
| F5 | ✅ 「插图与背景」對萘伊只顯示「背景不透明度」。調成 30% 時，原版風格的中空框透出地圖，按鈕裡的萘伊圖不受影響（`ta_f5_opacity100_vs30.png`，上 100%、下 30%） |
| F6 | ✅ 約 70 px 的半格按鈕只剩萘伊和道具（靠左）；黑板版只剩黑板。被擠到 12 px 的按鈕只畫圖（見 round-12-fixes 的 F1）。開發模式黃字每款主題一條 |

### 觀察（使用者已決定，2026-11-06）

- 82 個模組的排序平分時每顆約 90 px：萘伊約佔左邊 40 px、原版圖示在中間，勉強放得下。
- VTE 改成「圖示＋文字」的按鈕（建築規劃、方案）：圖示蓋在萘伊臉上，文字壓在頭上。
- 半格約 70 px：牛奶瓶圖示疊在萘伊肩上（`ta_f6_narrow_nai.png`）。
- 這兩個存檔的文字設成靠左，黑板版的字壓在萘伊頭上，沒有落在黑板上。
- 使用者決定**維持現狀**，在主題說明寫清楚適合折疊／捲動模式、按鈕寬 120 px 以上；另外下一輪做「主題建議版面」＋「半格按鈕也用完整寬度」開關。

## H. 按鈕背景圖（ExampleTheme）

| # | 結果 |
|---|---|
| H1 | ✅ 建築棕底（看不到 ARCHITECT 字，正常）、研究藍綠底帶 RESEARCH、其他是 BUTTON PICTURE；文字和圖示畫在圖上（`ta_h1_example_bar.png`） |
| H2 | ✅ 刪掉 `<buttonImage>` 重開：只有建築、研究有圖，其他只剩框，透出底部列背景（`ta_h2_h5_h6_h12fit_bar.png`） |
| H3 | ✅（部分）抽屜格子用預設圖。建築、研究沒有收進抽屜，所以「格子裡用自己的圖」沒看到（`ta_h3_example_drawer.png`，DRAWER、HOVER 徽章也在） |
| H4 | ✅（部分）滑鼠移上、分頁開著：效果只在框上，圖沒被染色（`ta_h4_example_states.png`）。停用狀態沒做 |
| H5 | ✅ 建築單顆寬度 120：看得到佔位圖上半部的對角線；研究看得到左緣（`ta_h5_architect_w120.png`）。`verticalAnchor` 改 Bottom 重開後，看得到的對角線換成往下收的那段（佔位圖線條很細，只能大致分辨） |
| H6 | ✅ 建築 `inset` 8：圖明顯內縮一圈（`ta_h2_h5_h6_h12fit_bar.png`、`ta_h9_drawn_preview.png`） |
| H7 | ✅ `ButtonResearch.png` 改名重開：主選單一條 `Theme ExampleTheme_Steel (…), buttonImages[1].texture: texture 'ExampleTheme/ButtonResearch' not found (the game's look is used for it)`；滑鼠移過研究後仍只有 1 條；研究按鈕只剩框，沒改用預設圖；沒有紅字（`ta_h7_research_missing.png`） |
| H8 | ✅ 外觀分頁的原版、帝國、信條、基因實驗室、虛空、星航卡片沒有按鈕圖（`ta_f4_cards.png`）。沒有和改版前逐像素比對 |
| H9 | ✅ 有 `<preview>` 時卡片用自己的預覽圖；刪掉 `<preview>` 重開改成畫出來的預覽：Architect 是自己的棕色圖（含 inset），其他格只有框（這批同時刪了預設圖），停用格也只有框（`ta_h9_drawn_preview.png`） |
| H10 | ✅ 建築寬度 120 時只顯示圖示，圖示畫在圖上；寬的時候文字＋圖示都在圖上 |
| H11 | ⚠️ 刪掉四個按鈕 atlas 和三個文字顏色重開：底部列看起來就是原版按鈕。取樣按鈕中間的像素，建築、研究、工作都是 (28, 38, 44)，完全相同，看不出圖。程式（`MainButtonsRootPatches.cs` 第 179～183 行）是在遊戲畫按鈕 atlas 之前先畫圖，所以圖有畫，只是被原版按鈕完全蓋住（`ta_h11_h12stretch_bar.png`）。清單寫的「看起來很暗」不符 |
| H12 | ✅ `Fit`：整張 RESEARCH 圖都看得到（按鈕比例剛好接近 4:1，右邊幾乎沒有留空）；`Stretch`：拉伸填滿（`ta_h12_stretch.png`）。第一次和 H11 同一批改，被 H11 蓋住看不到，另外單獨重開測 |
| H13 | ✅ 加一個 `button` 寫成 `Achitect` 的項目：一條黃字 `Theme ExampleTheme_Steel (…), buttonImages: 1 picture(s) name main buttons that are not loaded (…): Achitect`，沒有紅字。round-12-fixes 把格式改成每款主題一條，所以不是清單寫的 `buttonImages[2].button: no main button 'Achitect' is loaded` |
| H14 | ✅ 加一個沒寫 `button`、`opacity` 2 的項目：`Config error in ExampleTheme_Steel: buttonImages[2]: button is empty (the defName of a MainButtonDef)`、`Config error in ExampleTheme_Steel: buttonImages[2]: opacity must be between 0 and 1` |

## 要改清單的地方

- H11 的預期改成「圖會被遊戲的按鈕完全蓋住、看不到」，或改程式／啟動檢查提醒主題作者（要不要改由使用者決定，記在 PLAN）。
- H13 的黃字原文改成 round-12-fixes 之後的合併格式。

## 測完的還原

`Mods\ExampleTheme` 每批改完都從 repo 原檔還原，最後整個移走。存檔沒有存回去。
