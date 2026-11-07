# theme-projects Specification

## Purpose
外觀分頁的「新增主題…」：在遊戲裡一鍵做出完整的空白主題模組（主題專案），作者只要改 PNG 就能在真正的底部列上看到結果，做完的資料夾就是可以啟用、上傳工作坊的主題模組；只能刪除自己用這個功能做的主題。設計見 `docs/superpowers/specs/2026-11-07-theme-projects-design.md`，2026-11-07 實作（PR #23，經 PR #25 進 main），歸檔在 `openspec/changes/archive/2026-11-07-theme-projects/`。

## Requirements
### Requirement: 新增主題專案
外觀分頁的主題清單最後（內建主題卡片、自訂主題存檔之後）SHALL 有「新增主題…」按鈕。按下後的對話框 SHALL 讓玩家填名稱（必填）、作者、說明，並顯示可修改的 packageId（預設由作者和名稱產生、和已安裝模組撞名時加後綴）。建立 SHALL 在遊戲的 `Mods\` 產生一個完整的主題模組資料夾：`About/About.xml`（前置項帶 `downloadUrl` 或 `steamWorkshopUrl`）、`About/Preview.png`、`Defs/<Id>_Theme.xml`（一個 `BarAppearanceDef`，含按鈕四種狀態、三種文字顏色、列背景、抽屜、「更多」、左右捲動鍵、齒輪、預覽、共用插圖）、`Textures/<Id>/` 的空白 PNG、根目錄的說明書 `_Guide_<語言>.png`（英文、繁中、簡中、日文各一張，給第一次做主題的人看得懂的一圖流）和標記檔 `MBBS_ThemeProject.xml`。空白主題 MUST NOT 附裝飾圖或逐顆按鈕插圖。

#### Scenario: 建立成功
- **WHEN** 玩家填好名稱並按「建立」，且 `Mods\` 可以寫入
- **THEN** `Mods\<資料夾名>\` 出現上述全部檔案，清單多一張這個主題的卡片並被選上，底部列馬上用空白主題畫出，訊息列顯示資料夾路徑

#### Scenario: 名稱空白或 packageId 不合法
- **WHEN** 名稱空白，或 packageId 不合法、和已安裝的模組重複
- **THEN** 「建立」是灰的，滑鼠提示說明原因

#### Scenario: Mods 寫不進去
- **WHEN** 建立時 `Mods\` 無法寫入
- **THEN** 訊息說明原因和路徑，不建立任何檔案或資料夾，沒有紅字

#### Scenario: 產生的模組啟用後沒有黃字
- **WHEN** 玩家在模組清單啟用剛建立的主題專案並重開遊戲
- **THEN** 它照一般主題模組載入，log 沒有 `needs to have <downloadUrl>` 或本模組的主題檢查警告

### Requirement: 空白主題的圖
空白 PNG SHALL 讓作者在實際的底部列上分辨各零件：四種按鈕狀態（一般、滑鼠移上、開啟中、停用）各用一個色調；所有九宮格圖（按鈕四張、「更多」、抽屜，64×64）SHALL 看得出 1/4 處的切線和角／邊／中間；按鈕圖的中間 MUST 是半透明，讓插圖看得到；共用插圖（240×72）SHALL 標出準心和按鈕寬 120／80／40 時 Cover 裁切後的可見範圍；列背景（256×36，Tile）SHALL 有格線和接縫記號。每張 `_Guide_<語言>.png` SHALL 是完整的一圖流說明書（流程、檔案、九宮格、插圖、文字顏色、上傳），SHALL 標出每個檔名對應底部列的哪一塊和它的尺寸。

#### Scenario: 分辨狀態
- **WHEN** 選著空白主題，滑鼠移到某顆按鈕上、再打開某個分頁
- **THEN** 滑鼠下的按鈕、開啟中的按鈕、一般按鈕顏色各不相同

#### Scenario: 插圖看得到
- **WHEN** 選著空白主題
- **THEN** 每顆按鈕的框裡看得到插圖的準心或裁切框

### Requirement: 未啟用的主題專案由本模組載入
遊戲啟動完和每次新增後，本模組 SHALL 掃描 `Mods\` 底下有 `MBBS_ThemeProject.xml` 的資料夾。標記檔的 packageId 不在 `LoadedModManager.RunningMods` 裡時，本模組 SHALL 用遊戲的 Def 解析讀入該資料夾 `Defs\*.xml` 的所有 `BarAppearanceDef`（支援所有合法欄位），加進 `DefDatabase<BarAppearanceDef>`，貼圖從 `<資料夾>\Textures\<path>.png` 讀。packageId 在 `RunningMods` 裡時，本模組 MUST NOT 再載入它。一個專案有哪些主題 SHALL 以 `Defs\*.xml` 為準，不以標記檔記的 defName 為準。

#### Scenario: 不用重開就能選
- **WHEN** 剛建立主題專案，或主題專案在 `Mods\` 但沒在模組清單啟用，遊戲啟動
- **THEN** 外觀分頁有它的卡片（來源顯示「主題專案（未啟用）」），可以選、可以存讀檔、自訂主題的選圖清單也有它的零件

#### Scenario: 已啟用的不重複載入
- **WHEN** 主題專案已在模組清單啟用並重開遊戲
- **THEN** 清單裡它只出現一次，來源顯示模組名稱，log 沒有重複 defName 的錯誤

#### Scenario: defName 撞名
- **WHEN** 主題專案裡某款主題的 defName 和已存在的主題相同
- **THEN** 那一款略過，log 一行黃字，其他主題照常

#### Scenario: 貼圖讀不到
- **WHEN** 主題專案的 Def 指向不存在或壞掉的 PNG
- **THEN** 那個零件用遊戲的外觀，警告一次，沒有紅字

#### Scenario: 主題專案不見了
- **WHEN** 存檔選的是某個主題專案，下次啟動時資料夾已經不在
- **THEN** 退回原版外觀，沒有紅字

### Requirement: 重新載入與自動偵測
主題專案的卡片（選中時）SHALL 有「重新載入」「打開資料夾」「刪除」按鈕，不需要開發模式。對本模組載入的主題專案，「重新載入」SHALL 重讀 Def XML 和全部 PNG，Def 的所有欄位都 MUST 生效，並清掉跟著主題的快取；XML 裡新增或刪除的主題 SHALL 加進或移出清單。對已啟用的主題專案，「重新載入」SHALL 只重讀 PNG，並提示改了 Defs 要重開遊戲。設定視窗開著、外觀分頁顯示、選中的是主題專案時，本模組 SHALL 每隔 `Tuning.xml` 的間隔（預設 1 秒）比對它 `Textures\` 下 `.png` 和 `Defs\` 下 `.xml` 的修改時間，有變就重新載入一次；MUST NOT 每幀讀檔或配置記憶體。

#### Scenario: 改圖後自動更新
- **WHEN** 設定視窗開在外觀分頁、選著主題專案，作者用繪圖軟體改了 `Button.png` 並存檔
- **THEN** 幾秒內底部列和卡片預覽換成新的圖，沒有紅字

#### Scenario: 改 XML 後重新載入
- **WHEN** 作者在未啟用的主題專案的 Def 改了文字顏色、加了一筆 `buttonImages`，然後按「重新載入」
- **THEN** 顏色和那顆按鈕的插圖都照新的設定畫出

#### Scenario: 檔案正在寫入
- **WHEN** 自動偵測時 PNG 正被繪圖軟體寫入、讀取失敗
- **THEN** 不報錯，下一輪再試

#### Scenario: 打開資料夾
- **WHEN** 按「打開資料夾」
- **THEN** 用系統檔案總管開啟該主題專案的資料夾

### Requirement: 只能刪除主題專案
只有資料夾裡有 `MBBS_ThemeProject.xml` 的主題 SHALL 有「刪除」；內建主題、工作坊主題、手動做的主題模組 MUST NOT 能被刪除或修改。packageId 在 `LoadedModManager.RunningMods` 或 `ModsConfig` 的啟用清單裡時，「刪除」SHALL 是灰的並提示先停用、重開遊戲。刪除 SHALL 先跳確認視窗，再把整個資料夾搬到 `<存檔資料夾>\MainButtonBarSystem\DeletedThemes\<資料夾名>_<yyyyMMdd_HHmmss>`，不是真的刪除。

#### Scenario: 刪除未啟用的主題專案
- **WHEN** 對未啟用的主題專案按「刪除」並確認
- **THEN** 資料夾搬到 `DeletedThemes\`，它的所有主題從清單消失；如果正選著它，外觀回到原版

#### Scenario: 不能刪的主題
- **WHEN** 看內建主題、工作坊主題或手動做的主題模組的卡片
- **THEN** 沒有「刪除」按鈕

#### Scenario: 已啟用的不能刪
- **WHEN** 主題專案已在模組清單勾選（不論有沒有重開）
- **THEN** 「刪除」是灰的，滑鼠提示要先停用並重開遊戲

#### Scenario: 搬移失敗
- **WHEN** 刪除時資料夾裡有檔案被其他程式鎖住
- **THEN** 訊息說明原因，資料夾和清單都維持原狀，沒有紅字

### Requirement: 不打擾只整理按鈕的玩家
這個功能 MUST NOT 改變任何既有預設值、按鈕分頁、或沒有主題專案時的外觀分頁內容（除了清單最後多一顆「新增主題…」），也 MUST NOT 跳出任何主動提示。新增的介面文字 SHALL 有 29 種語言。

#### Scenario: 沒用這個功能的玩家
- **WHEN** 玩家從沒按過「新增主題…」
- **THEN** 外觀分頁只多一顆按鈕，其他畫面、設定、log 都和這個功能加入前一樣

