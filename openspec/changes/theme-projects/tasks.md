## 1. 準備

- [x] 1.1 repo 停在 `claude/openspec-setup`：commit openspec 設定（`openspec/`、`.claude/`）、本 change、`docs/superpowers/specs/2026-11-07-theme-projects-design.md`，push，對 main 開 PR
- [x] 1.2 從 `claude/openspec-setup` 開 `claude/theme-projects`（疊在上面的 PR，base 設 `claude/openspec-setup`）
- [x] 1.3 讀 `docs/HANDOFF.md` §4、§5、設計文件、`docs/API.md` §4；用 ilspycmd 確認 1.7 的 `Verse.DirectXmlToObject`、`Verse.DirectXmlLoader`、`DefDatabase<T>` 的加入／移除、shortHash、`LoadedModManager.RunningMods` 的用法，結論記在 PR 說明的「技術筆記」
- [x] 1.4 用 `superpowers:writing-plans` 把 2～8 展開成 `docs/superpowers/plans/2026-11-07-theme-projects.md`（要改哪些檔、每步怎麼驗證），再照它開工

## 2. 空白素材

- [x] 2.1 寫 `tools/make_theme_kit.py`（Pillow，用 `RimClaude\tools\.venv_vector` 的 python）：產生 `ThemeKit/`，內容照設計文件 5.1 的空白圖表（12 張 Textures、About 的 `Preview.png`、`_Guide.png`）加 Def 和 About 的範本 XML（作者名、名稱等用佔位字串，建立時替換）
- [x] 2.2 執行並 commit 產物；檢查尺寸和設計表一致；拼一張模擬底部列的預覽圖存到 `_資料與測試\MainButtonBarSystem\theme-projects\`，目視確認四種狀態分得出、九宮格切線看得見、插圖裁切框看得見
- [x] 2.3 `tools/make_release.py` 的 `FOLDERS` 加 `ThemeKit`；`RimClaude\模組開發\環境\deploy.ps1` 的 `$folders` 加 `ThemeKit`（環境資料夾不在 repo，改完在 PR 說明註記）

## 3. Core 純邏輯＋單元測試

- [x] 3.1 由名稱、作者產生 packageId、資料夾名、`<Id>`、defName，處理撞名（沿用 `SharingNames`）
- [x] 3.2 標記檔 `MBBS_ThemeProject.xml` 的模型：讀寫、`formatVersion`、比本模組新的略過
- [x] 3.3 「能不能刪」的判斷：有標記檔、packageId 不在 `RunningMods`、不在 `ModsConfig` 啟用清單
- [x] 3.4 修改時間快照比對（輸入一組路徑和時間，回傳有沒有變）
- [x] 3.5 `DeletedThemes\<資料夾名>_<yyyyMMdd_HHmmss>` 的目的地名稱（撞名加後綴）

## 4. 建立

- [x] 4.1 建立主題專案：複製 `ThemeKit`、填 About（和 `ArrangementIO.ExportModFolder` 共用 About 與前置項網址的程式，必要時抽成共用方法）、填 Def（defName、label、description、author、`displayOrder` 用 Tuning 值）、寫標記檔；`Mods\` 寫不進去或中途失敗 → 回傳錯誤並清掉半成品
- [x] 4.2 建立對話框：名稱、作者（預設上次匯出用的作者名）、說明、packageId；不能建立時按鈕灰掉並提示原因
- [x] 4.3 外觀分頁主題清單最後（`DrawSavedThemes` 之後）加「新增主題…」；建立成功 → 載入、選上、訊息列顯示路徑

## 5. 執行中載入

- [x] 5.1 `BarAppearanceDef` 加 `[Unsaved]` 的專案資料夾欄位；有值時 `Tex`／`TexQuiet`／`ReloadTextures` 從 `<資料夾>\Textures\<path>.png` 讀
- [x] 5.2 掃描 `Mods\` 底下的標記檔；packageId 在 `RunningMods` 就略過；否則解析 `Defs\*.xml` 的所有 `BarAppearanceDef`、`ResolveReferences`、補 shortHash、加進 `DefDatabase`；defName 撞名 → 那一款略過並出一行黃字
- [x] 5.3 掃描時機：Def 載入完、讀存檔之前（確認時點，存檔選了主題專案也要讀得到）；新增後再掃一次
- [x] 5.4 `modContentPack` 為 null 的處理：卡片來源文字「主題專案（未啟用）」、`ThemeValidator`、`LoadFromModFolder`

## 6. 重新載入與自動偵測

- [x] 6.1 重新載入（未啟用的）：重新解析 XML、就地覆蓋同一個 def 物件的欄位、清掉所有跟著 def 的快取（`textures`、`ownTextures`、`buttonImageIndex`、`workerInt`、繪製端的參考，全部找出來）；XML 新增的 def 加入、消失的移除，目前選著消失的 → 回原版；最後 `ThemeValidator.Validate`、`CustomThemes.ResetCache`
- [x] 6.2 重新載入（已啟用的）：只重讀 PNG，提示改了 Defs 要重開遊戲
- [x] 6.3 主題專案卡片（選中時）的按鈕列：重新載入、打開資料夾、刪除；不需要開發模式
- [x] 6.4 自動偵測：設定視窗開著、外觀分頁、選中主題專案時，每隔 Tuning 的間隔比對 `Textures\*.png`、`Defs\*.xml` 的修改時間；只在間隔到時查檔案；讀失敗保留舊圖、下一輪再試

## 7. 刪除

- [x] 7.1 刪除鈕只給主題專案；已啟用（`RunningMods` 或 `ModsConfig`）時灰掉並提示
- [x] 7.2 確認視窗 → 搬到 `DeletedThemes\`（跨磁碟改成複製後刪除，失敗就還原、什麼都不改）→ 這個專案的主題全部移出清單；選著它的回原版；自訂主題引用到的照現有「找不到主題」處理

## 8. Tuning、翻譯、文件

- [x] 8.1 `Tuning.xml` 新增數值（自動偵測間隔、主題專案的 `displayOrder` 等），每項有預設值
- [x] 8.2 新字串加進 `tools/i18n/English.json`，其他 28 種語言交給子代理翻，跑 `gen_translations.py` `2026-11-07`
- [x] 8.3 `docs/API.md` 新增「在遊戲裡新增主題」一節（流程、檔案清單、手動加 `buttonImages`／`decorations`、複製專案要改 packageId 和 defName、上傳工作坊的注意事項、做好後要併進自己的模組（例如種族模組）時把 Defs 和 Textures 搬進 `IfModActive` 資料夾，連到同檔講其他模組附主題的那節，約第 615 行的 `IfModActive` 範例），`docs/API.zh-TW.md` 節錄
- [x] 8.4 `docs/testing/theme-projects.md`：照 `specs/theme-projects/spec.md` 的每個 Scenario 寫實機步驟

## 9. 檢查與 PR

- [x] 9.1 `verify.ps1 -Mod MainButtonBarSystem` 全過（0 警告、單元測試、翻譯檢查） `2026-11-07`
- [x] 9.2 用 `superpowers:requesting-code-review`（Sonnet）review，修完再跑一次 `verify.ps1` `2026-11-07`
- [x] 9.3 push `claude/theme-projects`，開 PR（base `claude/openspec-setup`），說明寫改了什麼、技術筆記、verify 結果、實機測了什麼沒測什麼 `2026-11-07`

## 10. 實機

- [x] 10.1 關遊戲、部署 `claude/theme-projects`，照 `標準測試流程.md` 換小排序（帶優化模組），照 `docs/testing/theme-projects.md` 全部測一遍；測試建立的主題專案資料夾測完搬到 `_資料與測試\MainButtonBarSystem\theme-projects\` `2026-11-07`
- [x] 10.2 結果推 `test/theme-projects-results`，PR 說明補實機結果；發現的問題修在 `claude/theme-projects` 並重測 `2026-11-07`
