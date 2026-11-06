# 第十三輪實機測試結果

- 分支：`claude/round-13` @ `a26bc98`（PR #20），用 `deploy.ps1` 部署（repo 裡的 DLL）。
- 環境：RimWorld 1.7.5000 rev1275，開發模式開，2560×1440，介面簡體中文（A.3 切日文）。
- 測試前改的設定：模組設定的「總是由本模組繪製」原本是勾的，A.4 測試時取消。
- 測試日期：2026-11-06 23:24～23:45，Claude 用 computer use 操作。

## 總結

| 節 | 結果 | 說明 |
|---|---|---|
| A 補測發現的 4 項 | ✅ | 4 項都通過 |
| B 來源模組篩選 | ✅ | B.4（換存檔回到全部）沒測 |
| C 字級、半格、建議版面 | ✅ | **Waylith 吃 `GUIStyle.fontSize`**（24px 生效）；C.2 沒測 |
| D 自訂主題：worker、預覽、選色、統計 | ⚠️ | 選色視窗出 1 筆紅字 `Rect height was too small by 7`，最下面一列被切掉 |
| E 選圖、背景插圖、複製貼上 | ✅ | E.3 沒測 |
| F 自訂主題存檔、載入 | ✅ | F.1（灰掉）、F.6、F.7 沒測 |
| G 分享排法帶圖 | ✅ | 只測 G.1；G.2、G.3 要重開遊戲、另一個環境，沒測 |
| H 回歸 | ✅（部分） | 取消設定後列回到原樣；六款內建主題沒有逐一比對 |
| 其他 | ⚠️ | 日文「スタイルを貼り付け」按鈕超出右邊 |

## 環境

- 排序：`ModsConfig_20261106_2324_mbbs_r13_main.xml`（51 個：上一輪 `r89a_main` 拿掉範例主題、加測試模組 `local.mbbs.r10throw`）。check_order 只有已知的 0MultiplayerAPI 同名 DLL；匯入後 `same order: True`。
- 存檔：`MBBS_r89a_test`（「Mod 不匹配」按仍然載入），沒有存檔。
- log：本機 `_資料與測試\MainButtonBarSystem\round-13\logs\main_zh\`、`logs\ja\`。

## 細節

### A. 補測發現的 4 項

| 項目 | 結果 | 截圖 |
|---|---|---|
| A.1 `DoButton` 一律丟例外的按鈕 | 進地圖後一筆 `R10_ThrowOnDraw … threw while being drawn`；之後在它上面點擊、移動滑鼠，沒有第二筆（上一輪會多一筆 handling input） | — |
| A.2 做成排序模組的 About | 前置項有 `<downloadUrl>https://github.com/C10H8Nai/Rimworld-Main-Button-Bar-System</downloadUrl>`（本機部署不是工作坊版，所以沒有 steamWorkshopUrl） | — |
| A.3 日文作者名 | 預設主題清單的自訂主題卡片顯示「作者: boblin00215」，小寫保留。匯入選單裡這次沒有帶作者的排序檔，沒看到 | r13_a3_ja_saved_card.webp |
| A.4 測試用選項 | 開發模式開：看得到；開發模式關（選項已取消勾選）：看不到 | r13_a4_devoff_hidden.webp |

### B. 來源模組篩選

| 步驟 | 結果 | 截圖 |
|---|---|---|
| 下拉選單 | 「全部 (30)」，接著依名稱排序、帶按鈕數（[TEST]…(2)、Animal Tab (1)…核心 (12)、生物技术 (1)） | r13_b1_filter_menu.webp |
| 選「核心」 | 只剩 11 顆核心按鈕＋「原本不顯示 (1)」 | r13_b2_filter_core.webp |
| 篩選中「下移」工作 | 列變成 建築規劃、醫療、管制、工作：工作移到下一顆核心按鈕（管制）後面，夾在中間的醫療（Medical Tab）位置不變 | r13_b3_filtered_move.webp |

### C. 字級、半格、建議版面

| 步驟 | 結果 | 截圖 |
|---|---|---|
| C.1 所有按鈕「自訂大小」 | 預設 14px（遊戲一般字）；改 24 後列上的字和預覽都變大。**Waylith 支援 `GUIStyle.fontSize`** | r13_c1_fontsize24.webp |
| C.3 半格按鈕用完整寬度 | 開了之後薄荷清單菜單、薄荷裝備菜單、內容和其他按鈕一樣寬 | r13_c3_halffull_on.webp |
| C.4 萘伊建議版面 | 折疊模式時卡片下出現黃字「按钮至少 120px 宽时，萘伊 的效果最好…」和「应用建议布局」；滑鼠提示列出「显示方式：滚动」「滚动时每个按钮的最小宽度：120px」（預設寬 150、半格已開，所以沒列）。按下後變捲動模式、提示消失 | r13_c4_suggest_hint.webp、r13_c4_applied.webp |

### D. 自訂主題

| 步驟 | 結果 | 截圖 |
|---|---|---|
| D.1 切到自訂、不改 | 列和預設主題模式一樣 | r13_d1_custom_nochange.webp |
| D.3 色碼框 | 打 `#FF8000` 立刻生效（列上字變橘色，RGB 255/128/0） | r13_d3_hex.webp |
| D.3 選色… | 開遊戲的選色視窗（色輪、`lookColorPalette` 色票、RGB／HSV），選色後按接受生效。**問題**：開視窗時一筆紅字 `Rect height was too small by 7 for the requested row height.`（沒有堆疊），最下面「更早颜色」那列被切掉一半。推定：遊戲的 `Dialog_ColorPickerBase` 預設 600×450 放不下（中文字高），要覆寫 `InitialSize` 加高（數值放 Tuning） | r13_d3_picker.webp |
| D.4 統計 | 改一顆後「所有按钮（已修改）（1 个有自己的设置）」，有「全部 1 个改回跟随所有按钮」，按了之後清掉 | r13_d4_owncount.webp |

### E. 選圖、背景插圖、複製貼上

| 步驟 | 結果 | 截圖 |
|---|---|---|
| E.1 選圖視窗 | 「主题里的图」列出各主題的框和萘伊的按鈕插圖；「导入的」列出 `Images/` 的 PNG，有開啟資料夾、重新整理 | r13_e1_picker_themes.webp |
| E.2 背景插圖 | 所有按鈕選匯入的 PNG：列上每顆都畫；改「完整放入」、不透明度 50、內縮 4 都生效；位置沒改時跟萘伊自己插圖的錨點（靠左） | r13_e2_picture_all.webp、r13_e2_fit_opacity_inset.webp |
| E.4 刪掉用到的 PNG | 開關一次選圖視窗（重讀 `Images/`）後，列改用萘伊自己的圖，log 一行黃字（只一次）。也驗證了 review 修的「重讀後快取拿到已銷毀的貼圖」 | r13_e4_missing_fallback.webp |
| E.5 複製／貼上 | 沒複製時「粘贴样式」是灰的；複製「所有按钮」貼到工作後工作標「已修改」 | r13_d4_owncount.webp |

### F. 自訂主題存檔、載入

| 步驟 | 結果 | 截圖 |
|---|---|---|
| F.2 存成自訂主題 | 匯出選單第一項「保存为自定义主题…」；沒填名稱時「请填写名称」、儲存是灰的；存成「R13测试主题」→ `Themes/R13测试主题/theme.xml`＋`R13Test.png`，內容有 baseTheme、customTheme（字級 24、顏色、插圖設定） | r13_f1_export_menu.webp、r13_f2_saved.webp |
| F.3 清單 | 預設主題最後一段「你保存的自定义主题」，卡片寫作者和起點（萘伊），提示說明會複製進設定 | r13_f3_saved_card.webp |
| F.4、F.5 載入、同名不同內容 | 目前有自訂外觀，先跳確認；確定後 `Images/` 多一個 `R13Test (2).png`（原本同名的是另一張圖），外觀用的是主題資料夾那張 | r13_f4_confirm.webp、r13_f5_applied_renamed.webp |

### G. 分享排法帶圖

| 步驟 | 結果 |
|---|---|
| G.1 做成排序模組 | `Textures/Boblin00215R13/Images/R13Test2.png`，排序 Def 裡是 `<image>tex:Boblin00215R13/Images/R13Test2</image>`；說明提示也寫了「自定义主题图片」 |

### H. 回歸

- 測完按「取消」，列回到測試前的樣子（原版外觀、折疊、更多 (3)）。r13_h1_after_cancel.webp
- 本模組在整段 log 裡：1 筆刻意製造的紅字（A.1）、1 筆萘伊的開發模式黃字（已知）、1 筆刻意的缺圖黃字（E.4），加上選色視窗的 1 筆紅字（不是本模組印的，但由本模組開的視窗造成）。

## 其他發現

- 日文：自訂主題「すべてのボタン」那列的三顆按鈕（この見た目をリセット／スタイルをコピー／スタイルを貼り付け）超過寬度，第三顆超出右邊被切掉。要放不下就換行。
- 存成自訂主題的 `theme.xml` 的 `meta` 帶了整份按鈕清單（沿用排序匯出的 meta），主題用不到，可以拿掉（不影響功能）。

## 沒測

B.4、C.2、D.2（預覽細節只順便看）、E.3、F.1、F.6、F.7、G.2、G.3、H 的六款主題逐一比對、A.3 的匯入選單和匯入報告（這次沒有帶作者的排序檔）。

## 還原

- Config 四個檔從備份放回，雜湊一致；`Autosave-*.rws` 10 個從備份放回、雜湊一致。
- 移走 `Mods\local_mbbs_r10throw`、做出來的排序模組 `Mods\R13测试主题`、`MainButtonBarSystem\Themes`、`Images`（都移到本機 `round-13\test_mods\created\`）。
- `Mods\MainButtonBarSystem` 留著 `claude/round-13` @ `a26bc98`。ModsConfig 是測試前的 `r89a_main`（51 個）。
