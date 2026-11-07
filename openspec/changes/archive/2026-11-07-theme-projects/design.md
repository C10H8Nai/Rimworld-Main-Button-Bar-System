完整設計（含作者流程、檔案清單、空白圖規格、決定紀錄）在 `docs/superpowers/specs/2026-11-07-theme-projects-design.md`，這裡只整理技術決定和風險。

## Context

- 主題是 `BarAppearanceDef`（`API/BarAppearanceDef.cs`），全模組都用 `DefDatabase<BarAppearanceDef>.GetNamedSilentFail` 找主題，`BarModel.Appearances` 是 `DefDatabase` 依 `displayOrder` 排序。
- 貼圖由 `Tex`／`TexQuiet` 經 `ContentFinder` 讀並快取在 `textures`；開發模式的 `ReloadTextures` 從 `modContentPack.foldersToLoadDescendingOrder` 用 `LoadImage` 重讀 PNG。
- 「做成排序模組」（`Config/ArrangementIO.ExportModFolder`）已經會寫模組到 `GenFilePaths.ModsFolderPath`、產生 About 和前置項網址；`Core/Sharing.cs` 的 `SharingNames` 處理識別字和撞名。
- 自訂主題存檔在 `ModPaths.ThemesFolder`（存檔資料夾的 `MainButtonBarSystem\Themes`），外觀分頁用 `DrawSavedThemes` 列在內建主題後面。

## Goals / Non-Goals

**Goals：** 遊戲內新增主題專案、未啟用時本模組自己載入、不開開發模式也能重新載入並自動偵測、只能刪主題專案且可復原。

**Non-Goals：** 從現有主題複製；本模組內上傳工作坊；逐顆插圖與裝飾圖的編輯介面；PNG 以外的格式；刪除自訂主題存檔；後備寫入位置；改 `make_theme.py`／`ExampleTheme`／範本發佈方式；UI 縮放 2 倍時的九宮格解析度。

## Decisions

1. **主題專案就是一個放在 `Mods\` 的完整主題模組，加一個根目錄標記檔。** 做完不用任何轉換就能啟用、上傳。替代方案「放存檔資料夾、另外匯出」多一步，作者容易卡住；不採用。
2. **未啟用時加進 `DefDatabase`，而不是另做一份主題清單。** 選主題、存讀檔、自訂主題選圖、`ThemeValidator` 都直接吃 `DefDatabase`，加進去就全部能用。代價是要自己補 shortHash、`ResolveReferences`，刪除或重新載入時要處理移除。
3. **用遊戲的 Def 解析讀 XML（`DirectXmlToObject` 這條路）。** 作者會照 `API.md` 手動加 `buttonImages`、`decorations`，自己寫的欄位解析一定會漏。實作者先確認 1.7 的 API 名稱和用法（ilspycmd 反編譯 `Verse.DirectXmlToObject`、`Verse.DirectXmlLoader`）。
4. **「遊戲已經載入」只看 packageId 是否在 `LoadedModManager.RunningMods`。** `ModsConfig` 在玩家勾選後就變了，但遊戲要重開才會真的載入，用它判斷會出現「兩邊都沒載」的空窗。刪除另外擋 `ModsConfig` 已勾選的，免得下次啟動缺模組。
5. **專案資料夾記在 `BarAppearanceDef` 的 `[Unsaved]` 欄位，`Tex`／`TexQuiet` 看到有資料夾就從檔案讀。** `modContentPack` 是 null，卡片來源文字、`ThemeValidator`、`LoadFromModFolder` 都要處理 null。
6. **重新載入＝重新解析後就地覆蓋同一個 def 物件的欄位，再清掉跟著 def 的快取**（`textures`、`ownTextures`、`buttonImageIndex`、`workerInt`，以及繪製端留的參考）。就地覆蓋讓已經拿著這個 def 的地方（設定、自訂主題）不用重新找。XML 裡新增的 def 加進 `DefDatabase`，消失的移除。
7. **自動偵測用間隔輪詢修改時間，不用 `FileSystemWatcher`。** 輪詢簡單、只在設定視窗開著時跑；Waylith 上 `FileSystemWatcher` 的行為沒驗證過。
8. **空白素材隨模組附在根目錄 `ThemeKit/`，由 `tools/make_theme_kit.py`（Pillow）產生並 commit。** 執行中用 C# 畫圖要處理文字和反鋸齒，不如預先做好；放 `Textures/` 會被遊戲當貼圖載入、白佔顯存。`make_release.py` 和 `deploy.ps1` 的資料夾清單要加 `ThemeKit`。
9. **刪除是搬到 `DeletedThemes\<資料夾名>_<時間>`。** 誤刪可以復原；`Directory.Move` 跨磁碟會失敗，實作者處理（例如複製後刪除，失敗就整個還原）。

## Risks / Trade-offs

- [執行中加進 `DefDatabase` 的 def 沒有經過遊戲的完整載入流程（例如 `PostLoad`、翻譯注入）] → 主題專案只有作者自己的語言，不做 DefInjected；需要的後處理照 1.7 的 `DefDatabase.AddAllInMods` 補齊，實機測確認。
- [從 `DefDatabase` 移除 def 的 API 可能不是公開的] → 用反射叫遊戲自己的移除方法，或改成標記隱藏並在 `BarModel.Appearances`、選圖清單過濾；選哪個寫在 PR 說明。
- [自動偵測讀到寫了一半的 PNG] → `LoadImage` 失敗就保留舊圖、下一輪再試，不報錯。
- [作者把主題專案資料夾整個複製一份] → 兩份 packageId、defName 相同，第二份的主題因 defName 撞名略過並出一行黃字；文件寫要改 packageId 和 defName。
- [`Mods\` 在某些安裝方式下唯讀] → 報錯不建立；文件寫原因。

## Migration Plan

純新增功能，不改存檔格式（存檔只記 defName）。舊存檔不受影響；選了主題專案的存檔在沒有那個專案的電腦上照現有規則退回原版。

## Open Questions

（無。實作中自己決定的細節寫在 PR 說明的「技術筆記」。）
