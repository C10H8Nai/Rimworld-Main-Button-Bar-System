# 第十三輪補測、11-07 兩項修正、PR #21 翻譯 實機測試結果

- 分支：`claude/theme-projects`（`feddba7`／`6f32bb7`，包含 main `155a7fb` 的第十三輪功能、11-07 的兩項修正和 PR #21 的翻譯），用 `deploy.ps1` 部署。和主題專案的測試同一批（`docs/testing/results-theme-projects.md`，在 `test/theme-projects-results`）。
- 清單：`docs/testing/round-13.md` 裡上次沒測的項目；PLAN.md 的 11-07 兩項修正；PR #21 新增的語言。
- 環境：RimWorld 1.7.5000 rev1275（Waylith），開發模式開，2560×1440，介面簡體中文，語言測試切日文、愛沙尼亞文、丹麥文、挪威文、瑞典文。
- 測試日期：2026-11-07 03:16～03:53，Claude 用 computer use 操作。
- 排序：`ModsConfig_20261107_0248_mbbs_tp_main.xml`（50 個，帶優化模組；F.7 那次多了測試用主題專案 `Test Steel`）。

## 總結

| 項目 | 結果 | 說明 |
|---|---|---|
| 第十三輪 A.3（日文作者名） | ✅ | 匯入選單和匯入報告都保留小寫 `boblin00215` |
| 第十三輪 B.4（換存檔回到全部） | 沒測 | 要兩個模組組合不同的存檔 |
| 第十三輪 C.2（單顆「中」＋所有按鈕自訂 20） | ✅ | |
| 第十三輪 D.2（預覽四格） | 部分 | 「所有按鈕」的預覽有圖示；滑鼠移上那格的著色、文字靠左沒看（這次的模板是只顯示圖示） |
| 第十三輪 E.3（單顆只改插圖不透明度） | ✅ | |
| 第十三輪 F.1（沒有自訂外觀時存成自訂主題是灰的） | ✅ | |
| 第十三輪 F.6（壞的 baseTheme、formatVersion 99） | ✅ | |
| 第十三輪 F.7（存讀檔、刪掉 Themes/） | ✅ | |
| 第十三輪 G.2、G.3 | 沒測 | 要重開遊戲啟用排序模組、另一個沒有那張圖的環境 |
| 第十三輪 H.1（六款內建主題和這輪之前一樣） | ✅ | |
| 11-07 修正一（選色視窗 `Rect height` 紅字） | ✅ | 0 筆，「更早顏色」那列完整 |
| 11-07 修正二（日文「スタイルを貼り付け」超出） | ✅ | 換到第二行 |
| PR #21 翻譯（日文、愛沙尼亞文、丹麥文、挪威文、瑞典文） | ✅ | 按鈕分頁、外觀分頁都沒有字超出或缺翻譯 |
| release-1.0 的 theme.xml 不帶按鈕清單 | 沒測 | 今晚沒有部署 release 分支（Code review 確認讀取端不用按鈕清單） |

本模組 0 紅字。

## 細節

### 第十三輪

| 項目 | 結果 | 截圖 |
|---|---|---|
| A.3 | 日文介面：匯入選單裡帶作者的排序檔（今晚「存成檔案」做的）顯示 `boblin00215`；匯入報告也是小寫 | r13l_a3_ja_import_menu.webp、r13l_a3_ja_report.webp |
| C.2 | 所有按鈕「自訂大小」20、「故人旧影」單顆「中」：抽屜裡故人旧影是中字，RuntimeGC 是 20 | r13l_c2_font.webp、r13l_c2_drawer.webp |
| D.2 | 「所有按鈕」和單顆按鈕的預覽四格都有圖示；其他兩點沒看 | — |
| E.3 | 起點 Test Steel，「工作」只改插圖不透明度：只有它的插圖變淡，用的還是主題的圖 | r13l_e3_opacity.png |
| F.1 | 「匯出」→「存成自訂主題…」灰，提示「請先切換到自訂主題並改動任意一項」 | — |
| F.6 | `baseTheme` 改成不存在的 defName：載入時改用原版起點、提示一次、沒有紅字；`formatVersion` 99：不在清單，log 一行黃字 `Skipped the custom theme … TP v99` | r13l_f6_badbase.webp |
| F.7 | 遊戲中套用自訂主題「TP custom」、存檔 `MBBS_tp_f7`；把整個 `MainButtonBarSystem\Themes\` 搬走後讀檔：底部列和存檔前逐像素比對一樣（只差畫面上的動態），本模組沒有紅黃字 | r13l_f7_compare_left.png |
| H.1 | 六款內建主題逐一擷取底部列，和第十三輪之前的參考圖一樣；貼圖檔沒變，Def 只多萘伊的 `suggestedLayout`（不會自動套用） | r13l_h1_six_themes.png |

### 11-07 的兩項修正

- 修正一：自訂主題的「選色…」：log 0 筆 `Rect height was too small`，最下面「更早顏色」那列完整（r13l_picker.webp）。
- 修正二：日文的分層按鈕：「スタイルを貼り付け」換到第二行，沒有超出（r13l_ja_layer_buttons.webp）。

### PR #21 翻譯

| 語言 | 按鈕分頁 | 外觀分頁 | 截圖 |
|---|---|---|---|
| 日文 | ✅ 沒有字超出 | ✅（主題專案的按鈕、對話框也有） | pr21_ja_buttons.webp |
| 愛沙尼亞文 | ✅ | ✅ | pr21_et_buttons.webp、pr21_et_appearance.webp |
| 丹麥文 | ✅ | ✅ | pr21_da_buttons.webp、pr21_da_appearance.webp |
| 挪威文 | ✅ | ✅ | pr21_no_buttons.webp、pr21_no_appearance.webp |
| 瑞典文 | ✅ | ✅ | pr21_sv_buttons.webp、pr21_sv_appearance.webp |

丹麥文的「Cançél」是遊戲原版 `CancelButton` 在丹麥文缺翻譯時的開發模式標記，不是本模組。換語言時遊戲出的紅字（例如瑞典文 `Could not resolve symbol "1_labelShort"`）都是遊戲本身的翻譯。
