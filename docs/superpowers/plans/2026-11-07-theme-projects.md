# 主題專案（新增／刪除主題）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> 2026-11-07 夜間由 Claude 自己照這份做（Native：逐項實作，最後一次整體 review）；作者已同意 openspec change `theme-projects` 的排程。

**Goal:** 外觀分頁可以在遊戲裡新增一個空白主題模組（主題專案）、不重開就選到、改 PNG／XML 後重新載入或自動更新，並只能刪除（搬走）自己新增的主題專案。

**Architecture:** 主題專案＝`Mods\<資料夾>` 底下的完整主題模組＋根目錄標記檔 `MBBS_ThemeProject.xml`。未啟用時本模組用遊戲的 `DirectXmlToObject` 解析它的 `Defs\*.xml`，加進 `DefDatabase<BarAppearanceDef>`，貼圖由 def 上的 `[Unsaved]` 專案資料夾欄位直接讀 PNG；已啟用（packageId 在 `RunningMods`）就交給遊戲。純邏輯（名稱、標記檔、能不能刪、修改時間比對、刪除目的地）放 `Core/` 並有單元測試；空白素材由 `tools/make_theme_kit.py` 產生到 repo 根目錄 `ThemeKit/`。

**Tech Stack:** C# net10.0（Krafs 1.7 參考組件、Harmony 不需要新補丁）、xunit、Python 3.12＋Pillow 13（`RimClaude\tools\.venv_vector`）。

**Spec:** `docs/superpowers/specs/2026-11-07-theme-projects-design.md`、`openspec/specs/theme-projects/spec.md`；1.7 API 的結論在 PR 說明「技術筆記」（本機副本 `_資料與測試\MainButtonBarSystem\theme-projects\技術筆記.md`）。

## Global Constraints

- 數值放 `Tuning.xml`（C# 欄位不給初值，`TuningXmlTests` 會檢查 XML 有寫）：`behaviour.themeProjectPollSeconds` 1、`behaviour.themeProjectDisplayOrder` 1000。
- 新介面文字 29 種語言：改 `tools/i18n/English.json` → 子代理翻其他 28 種 → `python -I -X utf8 tools/gen_translations.py`。
- 不能有紅字；警告每個只記一次；自動偵測不能每幀讀檔或配置記憶體。
- 不改既有預設值、按鈕分頁；沒有主題專案時外觀分頁只多一顆「新增主題…」。
- 只動有 `MBBS_ThemeProject.xml` 的資料夾；內建、工作坊、手動做的主題模組不能被刪或改。
- 刪除＝搬到 `ModPaths.DataFolder\DeletedThemes\<資料夾名>_<yyyyMMdd_HHmmss>`；跨磁碟改成複製後刪除，失敗就還原、什麼都不改。
- 只讀 `.png`。packageId 預設 `<作者>.mbbstheme.<名稱>`；`<Id>` 由 packageId 衍生（去掉 `mbbstheme` 段，其餘 PascalCase 相接），defName `<Id>_Theme`，貼圖資料夾 `Textures/<Id>/`。
- About 的前置項要有 `downloadUrl`／`steamWorkshopUrl`（和 `ArrangementIO` 共用同一段程式）。

## Review Focus

1. 作者存檔途中（XML 打到一半、PNG 寫到一半）被自動偵測讀到 → 不紅字、保留舊圖、下一輪再試。測：`FileSnapshotTests.ChangedWhenTimeChanges`＋實機 theme-projects.md 的「檔案正在寫入」。
2. 作者把專案資料夾整個複製一份 → 第二份 defName 撞名略過、一行黃字（不是 `Adding duplicate` 紅字）。實機測。
3. 存檔選了主題專案，下次資料夾不見 → `BarConfig.AppearanceDef` 已退回 Vanilla（現有行為），不用改；實機測。
4. 同一 session 新增兩個同名主題 → 資料夾名、packageId 都要不同（`ModLister` 還不知道新資料夾）。測：`ThemeProjectNamesTests.UniquePackageIdSkipsTaken`；UI 端把本 session 建立的 packageId 也當成已佔用。
5. 已在模組清單勾選但還沒重開的主題專案 → 仍由本模組載入（看 `RunningMods`），但刪除鈕是灰的（看 `ModsConfig`）。測：`ThemeProjectRulesTests.CanDelete*`。

---

### Task 1: 空白素材 ThemeKit（tasks 2.1～2.3）

**Files:**
- Create: `tools/make_theme_kit.py`、`ThemeKit/`（產物：`Textures/*.png` 12 張、`About/Preview.png`、`_Guide.png`、`Defs/Theme.xml.template`）
- Modify: `tools/make_release.py:23`（`FOLDERS` 加 `"ThemeKit"`）、`RimClaude\模組開發\環境\deploy.ps1:41`（`$folders` 加 `"ThemeKit"`，repo 外）

**Interfaces:**
- Produces: `ThemeKit/Textures/{Button,ButtonMouseover,ButtonActive,ButtonDisabled,More,Drawer,Bar,ButtonPicture,ArrowLeft,ArrowRight,Gear,Preview}.png`；`ThemeKit/Defs/Theme.xml.template`，佔位字串 `{{ID}}`、`{{LABEL}}`、`{{DESCRIPTION}}`、`{{AUTHOR}}`、`{{DISPLAY_ORDER}}`（C# 端替換前做 XML escape）。貼圖路徑在範本裡寫成 `{{ID}}/Button` 這種形式。

- [ ] **Step 1:** 寫 `make_theme_kit.py`：尺寸照設計 5.1（按鈕四張、More、Drawer 64×64 上色九宮格、切線在 16px、中間 alpha 約 0.35；四狀態色調藍／綠／橘／灰，More／Drawer 紫；Bar 256×36 格線＋兩端接縫記號；ButtonPicture 240×72 準心＋X＋外框＋寬 240／160／80 的三個置中框並標 120／80／40；箭頭 32×32、齒輪 64×64 輪廓；Preview 600×36 由前面幾張拼；About/Preview 640×360；`_Guide.png` 標出每塊的檔名、尺寸、九宮格切法、裁切框）。Def 範本照設計 5.1 的欄位，每個欄位上一行英文註解，最後註解指向 API.md「Adding a theme in the game」節。
- [ ] **Step 2:** 執行 `RimClaude\tools\.venv_vector\Scripts\python.exe -I -X utf8 tools/make_theme_kit.py`；用 Pillow 讀回每張檢查尺寸＝設計表。
- [ ] **Step 3:** 腳本另輸出一張模擬底部列（四種狀態、三種按鈕寬的插圖裁切）到 `_資料與測試\MainButtonBarSystem\theme-projects\kit_preview.png`，用 Read 目視確認三點（狀態分得出、切線看得見、裁切框看得見）。
- [ ] **Step 4:** 改 `make_release.py`、`deploy.ps1`；`python -I -X utf8 tools/make_release.py <scratch>` 輸出含 `ThemeKit/`、沒有 unexpected。
- [ ] **Step 5:** Commit（`ThemeKit/`、`tools/make_theme_kit.py`、`tools/make_release.py`）。

### Task 2: Core 純邏輯（tasks 3.1～3.5）

**Files:**
- Create: `Source/MainButtonBarSystem/Core/ThemeProjectNames.cs`、`Core/ThemeProjectMarker.cs`、`Core/ThemeProjectRules.cs`、`Core/FileSnapshot.cs`
- Test: `Source/MainButtonBarSystem.Tests/ThemeProjectNamesTests.cs`、`ThemeProjectMarkerTests.cs`、`ThemeProjectRulesTests.cs`、`FileSnapshotTests.cs`

**Interfaces（Produces）:**
- `static class ThemeProjectNames`：`string PackageId(string author, string name, string nameFallback)`（＝`SharingNames.Identifier(author,"Player") + ".mbbstheme." + SharingNames.Identifier(name, nameFallback)`）；`string UniquePackageId(string baseId, Func<string,bool> taken)`（taken 時依序試 `baseId2`、`baseId3`…）；`string IdOf(string packageId)`（各段 `Identifier` 後相接、略過不分大小寫等於 `mbbstheme` 的段，空就 `"Theme"`，最長 40）；`string DefNameOf(string id)`＝`id + "_Theme"`。
- `sealed class ThemeProjectMarker`：欄位 `int formatVersion; string packageId; string defName; string created; string modVersion`；`const int CurrentFormat = 1`；`const string FileName = "MBBS_ThemeProject.xml"`；`static ThemeProjectMarker Parse(string xml)`（壞 XML 或沒有 packageId → null）；`string ToXml()`；`bool IsNewerThanSupported => formatVersion > CurrentFormat`。
- `static class ThemeProjectRules`：`enum DeleteState { Allowed, NotProject, Running, ActiveInModsConfig }`；`DeleteState CanDelete(bool hasMarker, bool running, bool activeInModsConfig)`；`bool SamePackageId(string a, string b)`（不分大小寫，去掉結尾 `_steam`）；`string DeletedFolderName(string folderName, DateTime time)`＝`<folderName>_<yyyyMMdd_HHmmss>`。
- `sealed class FileSnapshot`：`static FileSnapshot Of(IEnumerable<(string path, long ticks)> files)`；`bool SameAs(FileSnapshot other)`（路徑不分大小寫、順序無關；任一為 null → false）。

- [ ] **Step 1:** 寫測試：`PackageIdShape`（`("小明 Nai","Steel bar")` → `"Nai.mbbstheme.SteelBar"`、`IsValidPackageId` 為真）、`UniquePackageIdSkipsTaken`（taken＝{a.mbbstheme.X, a.mbbstheme.X2} → `a.mbbstheme.X3`）、`IdOfDropsMarkerPart`（`"Nai.mbbstheme.Steel"` → `"NaiSteel"`）、`MarkerRoundTrip`、`MarkerBadXmlIsNull`、`MarkerNewerFlag`（formatVersion 2）、`CanDeleteMatrix`（四種情況）、`SamePackageIdIgnoresSteamSuffix`、`DeletedFolderNameFormat`（2026-11-07 23:05:09 → `X_20261107_230509`）、`SnapshotSameAs`／`ChangedWhenTimeChanges`／`ChangedWhenFileAdded`。
- [ ] **Step 2:** `dotnet test` 看到這些測試編譯失敗。
- [ ] **Step 3:** 實作四個檔。
- [ ] **Step 4:** `dotnet test` 全過。
- [ ] **Step 5:** Commit。

### Task 3: 執行中載入＋專案貼圖（tasks 5.1～5.4）

**Files:**
- Modify: `API/BarAppearanceDef.cs`（`[Unsaved] internal string projectFolder;`、`Tex`／`TexQuiet`／`ReloadTextures` 走專案資料夾、`internal void ClearCaches()`、`internal void CopyFrom(BarAppearanceDef fresh)`）
- Create: `Config/ThemeProjectLoader.cs`（解析、欄位檢查、加入／移除 DefDatabase）、`Config/ThemeProjects.cs`（登錄表、掃描、狀態）
- Modify: `ModStartup.cs`（`ThemeProjects.ScanAtStartup()` 在 `ButtonBaseline.Capture()` 之後、讀範本之前）、`Rendering/BarModel.cs:162`（`Appearances` 過濾 `ThemeProjects.IsHidden`）、`Config/CustomTheme.cs`（`ThemeImages` 同樣過濾）、`Diagnostics/ThemeValidator.cs:129`（來源名稱）、`UI/Dialog_MainButtonBar.cs:453`（卡片來源文字）

**Interfaces:**
- Consumes: Task 2 全部。
- Produces:
  - `sealed class ThemeProject { string Folder; ThemeProjectMarker Marker; bool LoadedByGame; List<BarAppearanceDef> Themes; FileSnapshot Snapshot; }`
  - `static class ThemeProjects`：`IReadOnlyList<ThemeProject> All`；`void ScanAtStartup()`；`void Rescan()`（只加新資料夾）；`ThemeProject ProjectOf(BarAppearanceDef def)`；`bool IsHidden(BarAppearanceDef def)`；`IEnumerable<string> KnownPackageIds`。
  - `static class ThemeProjectLoader`：`List<BarAppearanceDef> ParseFolder(string folder, string sourceName)`（`Defs\*.xml` 裡每個 `MainButtonBarSystem.BarAppearanceDef` 節點；`Abstract="True"` 略過、`ParentName` 略過＋黃字、不認得的欄位拿掉＋黃字、XML 讀不了→黃字回傳 null 表示「這輪讀失敗」）；`bool TryAdd(BarAppearanceDef def)`（撞名→黃字回 false；給 shortHash；`ResolveReferences`；`ConfigErrors` 用黃字）；`void Remove(BarAppearanceDef def)`（反射呼叫 private `DefDatabase<T>.Remove`，失敗就標隱藏）。
- 專案貼圖：`<projectFolder>\Textures\<path>.png`，用 `LoadImage`；讀不到檔→null＋警告一次；IO 失敗（檔案被鎖）→保留舊圖並回報「這輪要重試」。

- [ ] **Step 1:** `BarAppearanceDef` 改動（上面列的）＋`ThemeProjectLoader`＋`ThemeProjects`。
- [ ] **Step 2:** 卡片來源：`ProjectOf(theme)` 不為 null 且 `!LoadedByGame` → `"MBBS_ThemeProject_Source".Translate()`；`ThemeValidator.Warn` 的 mod 名同樣處理。
- [ ] **Step 3:** `verify.ps1 -SkipTests` 0 警告。
- [ ] **Step 4:** Commit。

### Task 4: 建立（tasks 4.1～4.3）

**Files:**
- Modify: `Config/ArrangementIO.cs`（`AboutXml` 改成 `internal static string AboutXml(string name, string author, string packageId, string description, string usage)`，`ExportModFolder` 改用它）
- Create: `Config/ThemeProjectWriter.cs`（`static string Create(string name, string author, string description, string packageId, out string error)`：檢查 `ThemeKit` 存在→在 `GenFilePaths.ModsFolderPath` 建 `SharingNames.Unique(FileName(name,"Theme"))`→複製 ThemeKit（Textures 放 `Textures/<Id>/`、`_Guide.png`、`About/Preview.png`）→寫 About、Def（範本替換）、標記檔；任何例外→刪掉半成品、`error` 寫原因＋路徑、回傳 null）
- Modify: `UI/Dialog_Sharing.cs`（`ExportTarget.ThemeProject`；`NeedsPackageId` 含它；packageId 預設用 `ThemeProjectNames.PackageId`＋`UniquePackageId`；`Validate` 另擋 `ThemeProjects.KnownPackageIds`）
- Create: `UI/Dialog_MainButtonBar.ThemeProjects.cs`（partial：`DrawNewThemeButton(Listing_Standard)`、`CreateThemeProject()`）
- Modify: `UI/Dialog_MainButtonBar.cs:332`（`DrawSavedThemes` 之後呼叫 `DrawNewThemeButton`）

- [ ] **Step 1:** 實作上面四項；建立成功→`ThemeProjects.Rescan()`→`working.appearance = defName`→`Messages` 顯示路徑。
- [ ] **Step 2:** `verify.ps1 -SkipTests`。
- [ ] **Step 3:** Commit。

### Task 5: 重新載入、自動偵測、刪除（tasks 6.1～7.2）

**Files:**
- Modify: `Config/ThemeProjects.cs`（`ReloadResult Reload(ThemeProject p)`、`bool Delete(ThemeProject p, out string error, out string movedTo)`、`bool PollChanged(ThemeProject p)`）、`UI/Dialog_MainButtonBar.ThemeProjects.cs`（`DrawProjectButtons(Listing_Standard, BarAppearanceDef)`、`PollSelectedProject()`）、`UI/Dialog_MainButtonBar.cs:329`（選中且是專案時呼叫 `DrawProjectButtons`；`DrawAppearanceTab` 開頭呼叫 `PollSelectedProject`）
- Modify: `API/MainButtonBarTuningDef.cs`（`Behaviour.themeProjectPollSeconds`、`themeProjectDisplayOrder`＋`ConfigErrors`）、`Defs/MainButtonBarSystem/Tuning.xml`

**Interfaces:**
- `ReloadResult { int files; bool retry; bool active; }`。
- 未啟用：重新 `ParseFolder`（null＝讀失敗→`retry`）；同 defName 的用 `CopyFrom` 就地覆蓋＋`ClearCaches`；新的 `TryAdd`；消失的 `Remove`，正選著它→`working.appearance = Vanilla`；最後 `ThemeValidator.Validate`、`CustomThemes.ResetCache()`。
- 已啟用：每款 `ReloadTextures()`。
- 輪詢：`Time.realtimeSinceStartup` 到了間隔才列 `Textures\**\*.png`、`Defs\*.xml` 的修改時間；和上次快照不同→`Reload`；`retry` 時不更新快照。
- 刪除：`CanDelete`（`ModsConfig.IsActive`、`RunningMods`）不是 Allowed→按鈕灰、tip 說原因；確認視窗（`Dialog_MessageBox.CreateConfirmation`）→`Directory.Move`，失敗且是跨磁碟→複製後刪除，複製失敗→刪掉複製品、原狀；成功→專案所有主題 `Remove`、從 `All` 拿掉；選著的回 Vanilla。

- [ ] **Step 1:** 實作。
- [ ] **Step 2:** `verify.ps1`（含 `TuningXmlTests`）全過。
- [ ] **Step 3:** Commit。

### Task 6: 翻譯與文件（tasks 8.1～8.4）

**Files:**
- Modify: `tools/i18n/*.json`、`Languages/`（產生）、`docs/API.md`（新節 “Adding a theme in the game”）、`docs/API.zh-TW.md`（節錄）
- Create: `docs/testing/theme-projects.md`（spec 每個 Scenario 一節，寫實機步驟和預期）

- [ ] **Step 1:** English.json 加新鍵；子代理（Sonnet）翻 28 種；`gen_translations.py`，`--check` 過。
- [ ] **Step 2:** 文件。
- [ ] **Step 3:** `verify.ps1` 全過；Commit。

### Task 7: 檢查與 PR（tasks 9.1～9.3）

- [ ] **Step 1:** `superpowers:requesting-code-review`（Sonnet），修完再 `verify.ps1`。
- [ ] **Step 2:** push `claude/theme-projects`，`gh pr create --base claude/openspec-setup`，說明含技術筆記、verify 結果、實機待測。
