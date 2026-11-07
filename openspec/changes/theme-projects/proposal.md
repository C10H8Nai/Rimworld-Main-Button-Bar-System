## Why

做主題的門檻卡在「做出第一版並看到效果」：要複製範例模組改 XML 或裝 Python 跑 `make_theme.py`，改完圖還得重開遊戲（開發模式的重新載入只讀已載入的模組）。只會畫圖的作者做不下去，所以內建外觀之外，玩家能訂閱的主題很難多起來。完整設計見 `docs/superpowers/specs/2026-11-07-theme-projects-design.md`。

## What Changes

- 外觀分頁的主題清單最後加「新增主題…」：在遊戲的 `Mods\` 產生一整套空白主題模組（About、Def、上色九宮格等空白 PNG、說明圖、標記檔），不用重開遊戲就能選到、在底部列上看到。
- 本模組在啟動時和新增後，把「還沒在模組清單啟用」的主題專案自己載入（加進 `DefDatabase<BarAppearanceDef>`，貼圖從資料夾讀 PNG）；啟用後交給遊戲載入，不重複。
- 主題專案的卡片加「重新載入」「打開資料夾」「刪除」，不開開發模式也有；設定視窗開著時會偵測檔案修改時間自動重新載入。
- 只有主題專案能刪除：搬到存檔資料夾的 `DeletedThemes\`，可以復原；已啟用的不能刪。
- 隨附空白主題素材 `ThemeKit/`（由 `tools/make_theme_kit.py` 產生），發佈和部署的資料夾清單加上它。
- 不做：從現有主題複製、本模組內上傳工作坊、逐顆插圖與裝飾圖的編輯介面、PNG 以外的格式、刪除自訂主題存檔、改動 `make_theme.py`／`ExampleTheme`。按鈕分頁和主題 Def 的欄位不變。

## Capabilities

### New Capabilities
- `theme-projects`：在遊戲內新增主題專案（空白主題模組）、未啟用時由本模組載入、重新載入與自動偵測、刪除（搬走）。

### Modified Capabilities
（無。`openspec/specs/` 目前沒有既有規格；外觀分頁、主題 Def 的既有行為不變。）

## Impact

- 程式：`UI/Dialog_MainButtonBar.cs`（外觀分頁卡片、按鈕）、新增建立對話框；`API/BarAppearanceDef.cs`（從資料夾讀貼圖、就地重讀 Def）；新增主題專案的建立／掃描／載入／刪除類別；`Core/` 新增純邏輯與單元測試；`Defs/MainButtonBarSystem/Tuning.xml` 新增數值。
- 共用既有：`ArrangementIO`（寫 About、前置項網址、寫入 `Mods` 的後備位置）、`SharingNames`、`ModPaths`、`ThemeValidator`、`CustomThemes.ResetCache`。
- 資源與工具：新增 `ThemeKit/`、`tools/make_theme_kit.py`；`tools/make_release.py` 的 `FOLDERS`、`RimClaude\模組開發\環境\deploy.ps1` 的 `$folders` 加 `ThemeKit`。
- 翻譯：新增介面文字 29 種語言（`tools/i18n/English.json` → `gen_translations.py`）。
- 文件：`docs/API.md`、`docs/API.zh-TW.md` 新增「在遊戲裡新增主題」；測試清單 `docs/testing/theme-projects.md`。
- 檔案系統：會在遊戲的 `Mods\` 建立主題專案資料夾，刪除時搬到存檔資料夾的 `MainButtonBarSystem\DeletedThemes\`。`Mods` 寫不進去就報錯，不建立任何東西。
