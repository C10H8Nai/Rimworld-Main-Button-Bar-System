# 主題附屬模組與萘伊主題 設計文件

- 日期：2026-11-04
- 分支：`claude/theme-addons`（從 main `7cf90c7` 開出，第八輪已合併）
- 來源：作者（萘伊）透過上游 session 交付的工作指示；本文件把指示整理成可以直接實作的規格。
- 標記：**【作者】** 是指示裡明寫的；**【判斷】** 是我（實作者）補上的決定，作者可以推翻；**【保留】** 是先不做、等作者決定的。

---

## 1. 為什麼要做

目前的 `BarAppearanceDef` 每個貼圖欄位都是九宮格 atlas，整張插圖會被切成四角拉伸，做不出「整條列放一張圖、角落貼一張立繪」這種常見的二次元 UI 主題。作者想做一款自己 OC（萘伊）的二次元風主題，也想讓別人能在工作坊做主題附屬模組，所以要：

1. 讓純 XML 主題放得了插圖（不用寫 C#）。
2. 給主題作者一份能直接複製改名的範本模組，和一支從 JSON 配色產生整個主題模組的工具。
3. 給作者自己用的開發功能：載入時檢查第三方主題、改 PNG 不用重開遊戲。
4. 文件（英文 `API.md` 第 4 節、繁中版）。

## 2. 目標、非目標、反目標

**目標【作者】**
- XML 主題可以：整條列／抽屜背景用 atlas／tile／stretch／fit 畫；加任意張裝飾圖（錨點、位移、大小、圖層、顯示狀態）；各狀態的文字顏色。
- 舊主題不加新欄位時行為**完全不變**；`ApiVersion` 升一個小版號（1.0 → 1.1）。
- 所有固定數值（位移、大小、透明度）在模組設定裡可調，每項有「預設值」按鈕。
- 內建 5 款主題改用新工具＋各自的 JSON 產生，輸出和現在逐檔一致。
- 萘伊主題（佔位圖）和 `ExampleTheme` 都能被遊戲載入。
- `docs/testing/theme-addons.md` 的測試清單照「外部主題模組只放在 `Mods`、不改本模組」的情境寫。

**非目標**
- 插圖本身：作者之後用 ComfyUI 生成再提供，現在一律佔位圖（色塊加文字）【作者】。
- 不做動畫、不做按鈕本身的插圖（按鈕仍是 atlas）【判斷】。
- 不改按鈕的繪製流程（仍走遊戲的 `MainButtonWorker.DoButton`）【判斷】。

**反目標**
- 任何既有主題（內建 5 款、別人已做的）外觀改變。
- 沒有裝飾圖的主題多出任何效能成本。
- 開發模式的檢查在正式遊玩時冒紅字、黃字。

## 3. 指示裡已定的行為

### 3.1 背景繪製方式【作者】

`barBackground`、`drawerBackground` 各加一個繪製方式欄位，預設 `Atlas`（和現在一樣）：

| 值 | 意義 |
|---|---|
| `Atlas` | 九宮格（`Widgets.DrawAtlas`），預設 |
| `Tile` | 以貼圖原尺寸從左上角平鋪，超出的部分裁掉 |
| `Stretch` | 拉伸填滿整個區域，不保持比例 |
| `Fit` | 保持比例縮放到完全放得進區域，置中，留白透明 |
| `Cover` | 保持比例縮放到填滿區域，超出的部分置中裁掉【判斷：指示沒列，但「整條列放一張圖」最常用的就是它】 |

欄位：`barBackgroundFit`、`drawerBackgroundFit`（列舉 `BackgroundFit`）；`Tile` 另有 `barBackgroundTileScale`、`drawerBackgroundTileScale`（預設 1）。

### 3.2 裝飾圖清單【作者】

`decorations` 是 `<li>` 清單，每項：

| 欄位 | 預設 | 意義 |
|---|---|---|
| `texture` | 必填 | 貼圖路徑（和其他欄位一樣相對於 `Textures/`） |
| `target` | `Bar` | `Bar`（整條列）／`Drawer`（抽屜視窗） |
| `anchor` | `Left` | 水平錨點 `Left`／`Center`／`Right` |
| `verticalAnchor` | `Bottom` | 垂直錨點 `Bottom`／`Center`／`Top`【判斷：指示只提水平，立繪要從列的底邊往上長，必須有垂直錨點】 |
| `offset` | `(0,0)` | 位移，px，x 往右、y 往下 |
| `size` | `(0,0)` | 大小，px；`(0,0)` 用貼圖原尺寸；只有一邊為 0 時依比例算另一邊 |
| `opacity` | `1` | 透明度 0～1 |
| `layer` | `BelowButtons` | `BelowButtons`（按鈕下）／`AboveButtons`（按鈕上） |
| `showWhen` | 空 | 顯示條件清單，空＝永遠顯示；`Normal`／`Mouseover`／`DrawerOpen`，**任一成立就顯示** |

錨點的算法：裝飾圖的矩形先依 `size` 決定寬高，再把它的對應邊貼齊目標區域的對應邊（`Right`＋`Bottom` 就是右下角對右下角），最後加上 `offset`。列高只有 36px，立繪會往上長出列的範圍，這是刻意的。

狀態的定義：`Mouseover` 是滑鼠在目標區域內（列：列的矩形，不含長出去的裝飾圖；抽屜：抽屜視窗）；`DrawerOpen` 是「更多」抽屜開著；`Normal` 是兩者都不成立（抽屜裝飾只看滑鼠）。

### 3.3 各狀態文字顏色【作者】

現有 `labelColor`、`labelMouseoverColor`；新增 `labelActiveColor`（按下或分頁開著）。沒設時沿用現在的規則：Active → `labelMouseoverColor` → `labelColor`。停用狀態的按鈕遊戲本來就不畫文字，所以不加停用顏色【判斷】。

### 3.4 玩家可調的值【作者：任何固定數值都要可調、每項有預設值按鈕】

存在 `BarConfig`（跟著存檔，主選單時存進範本），外觀分頁的主題清單下面一節「插圖與背景」：

| 設定 | 預設（`Tuning.xml` `barDefaults`） | 範圍（`ranges`） | 作用 |
|---|---|---|---|
| 顯示裝飾圖 | 開 | — | 關掉就不畫任何裝飾圖 |
| 裝飾圖透明度 | 100% | 0～100，步進 5 | 乘上每張圖自己的 `opacity` |
| 裝飾圖縮放 | 100% | 25～300，步進 5 | 乘上每張圖的大小（以錨點為基準縮放） |
| 裝飾圖位移 X／Y | 0px | −400～400，步進 1 | 加在每張圖的 `offset` 上 |
| 背景透明度 | 100% | 0～100，步進 5 | 列和抽屜背景（所有繪製方式） |

每項都走既有的 `SectionHeader`／`IntSetting` 樣式，右邊有「預設值」按鈕；「全部重設」也會重設它們。這些設定對「原版」主題沒有作用，所以不影響「沒碰設定就交給遊戲畫」的判斷。

### 3.5 範本與工具【作者】

- `docs/examples/ExampleTheme/`：完整主題模組（`About`、`Defs`、`Textures` 全部欄位、`Languages` 的 DefInjected、`Preview.png`），`About` 的 `modDependencies` 和 `loadAfter` 都寫好本模組。它是 `tools/make_theme.py` 從 `tools/themes/ExampleTheme.json` 產生的，可以重產。
- `tools/make_theme.py`：讀一份 JSON，輸出到指定資料夾，產生上面那種完整主題模組；`--textures-only` 只輸出貼圖（內建主題用）。
- 內建 5 款：`tools/themes/Imperial.json` 等；`gen_theme_textures.py` 改成呼叫 `make_theme.py` 的函式逐款產生貼圖和模組圖示。畫圖的程式碼原樣搬過去，不改任何像素。
- 佔位圖：純色塊＋外框＋文字（用英文，避免字型問題）【判斷】。

### 3.6 作者好用的功能【作者】

- 開發模式載入時（`StartupDiagnostics`）檢查每個 `BarAppearanceDef`：貼圖找不到、atlas 不是正方形、atlas 邊長超過上限（`Tuning.xml` `behaviour.themeAtlasMaxSize`，預設 72）、裝飾圖貼圖找不到、`opacity` 超出 0～1、`size` 為負。每個問題用 `Log.WarningOnce` 報一次，寫明主題 defName 和欄位。
- 開發模式的設定視窗外觀分頁加「重新載入主題貼圖」按鈕：對每個主題，把它自己模組資料夾裡的 PNG 直接從磁碟重讀（`Texture2D.LoadImage`），換掉快取；找不到檔案的沿用原本的貼圖。重讀後再跑一次檢查。只在 `Prefs.DevMode` 顯示。

### 3.7 文件【作者】

- `docs/API.md` 第 4 節：新欄位表、工作坊上架要在 Required Items 加本模組、版本相容承諾（新增欄位只升小版號、舊主題永遠能載入）。
- `docs/API.zh-TW.md`：主題那一節的繁中版（含上架和相容承諾）。
- `make_release.py` 要不要把範本和工具一起放進發佈檔、還是另外做「主題範本」工作坊項目：評估寫在 `docs/workshop/theme-template-release.md`，給作者決定；預設先採用其中一種。

## 4. 實作者的判斷（作者可檢查）

1. **繪製的接縫**：`BarAppearanceWorker` 加三個 virtual：`DrawBarBackgroundImage(Rect)`（只畫背景圖，依繪製方式）、`DrawBarOverlay(Rect)`（按鈕上層的裝飾）、`DrawDrawerOverlay(Rect)`；既有的 `DrawBarBackground`／`DrawDrawerBackground` 意義不變（＝背景圖＋按鈕下層裝飾），所以覆寫過它們的第三方 worker 不受影響。`BarRenderer.Draw` 在顯示方式畫完後呼叫 `DrawBarOverlay(ctx.ReportedBarRect)`，所以第三方顯示方式不用改就有上層裝飾；抽屜在畫完格子和齒輪後呼叫 `DrawDrawerOverlay`。
2. **外觀分頁的預覽**不畫裝飾圖（會長出卡片），只畫背景圖＋按鈕；有裝飾圖的主題建議提供 `preview`。
3. **抽屜視窗會裁掉超出視窗的部分**（遊戲視窗用 GUI group），抽屜裝飾不能長出視窗；文件寫明。
4. **Tile 自己迴圈畫**，不依賴貼圖的 wrapMode（遊戲載入的貼圖 wrapMode 不確定）；`Fit`／`Cover` 自己算矩形和 UV，用 `GUI.DrawTexture`／`GUI.DrawTextureWithTexCoords`，已確認 Waylith 的 `UnityStubs.dll` 有實作。
5. **裝飾圖的擺放算法**放在不依賴遊戲的 `Core/DecorationLayout.cs`（純數學），有單元測試。
6. **位移設定的「未設定」用 `int.MinValue`** 當哨兵（既有欄位用 −1，但位移可以是負數）。
7. **內建主題的 Def（`BarAppearances.xml`）和翻譯（`tools/i18n`）維持手寫**，JSON 只負責貼圖；`make_theme.py` 只在產生獨立模組時寫 Def 和翻譯。理由：翻譯已有自己的流程，不要兩套。
8. **萘伊主題做成內建第 6 款**（`MBBS_Nai`，displayOrder 60），貼圖在 `Textures/MainButtonBarSystem/Themes/Nai/`，JSON 在 `tools/themes/Nai.json`，標籤和說明翻成 29 種語言。配色照三視圖色票：按鈕深海軍藍 `#2d354f`，邊框灰白 `#dad4d8`，內線石墨灰 `#5b585d`，強調色用暗紫瞳 `#5a4a6e` 提亮，文字柔白 `#f6f2f1`。佔位裝飾：左下角立繪（`BelowButtons`，200×280，往上長出列）、右端一個滑鼠移上才出現的小標記（`AboveButtons`）、抽屜右上角一個標記。背景用 `Stretch`。
9. **重新載入只處理走 `BarAppearanceDef.Tex()` 的貼圖**，也就是主題自己的欄位；不碰遊戲的 `ContentFinder` 快取。自己讀進來的貼圖在下次重載時 `Destroy`。
10. **發佈方式預設**：範本模組和 `make_theme.py` 一起放進發佈資料夾的 `ThemeTemplate/`（遊戲不讀這個資料夾），不另開工作坊項目。理由寫在評估文件。

## 5. 技術限制

- 遊戲 1.7（Waylith：.NET 10 CoreCLR、Vulkan、`UnityStubs.dll` 當 Unity 相容層）。
- 貼圖經 `ContentFinder` 載入，大小以像素＝UI 單位（UI 縮放由遊戲套用）。
- 這台開發機沒有 .NET SDK，編譯要另外安排；XML／貼圖／文件可以先做。
- `Pillow` 在 `RimClaude\tools\.venv_vector`（13.0.0）。既有貼圖是用哪個版本的 Pillow 產生的不確定，逐檔比對先比位元組，位元組不同時再比像素。

## 6. 完成的定義【作者】

- 分支 `claude/theme-addons` 上 commit 完整。
- 內建 5 款主題用新工具重產後逐檔一致。
- 萘伊主題（佔位圖）和 `ExampleTheme` 能被遊戲載入。
- `docs/testing/theme-addons.md` 的測試清單照「外部主題模組」情境寫好。
- 程式改完做過 code review（`superpowers:requesting-code-review`）。

## 7. 保留給作者決定的【保留】

- 萘伊主題的正式插圖（ComfyUI 生成後換掉佔位圖；JSON 的 `placeholder` 改成 `file` 指向成品即可）。
- 範本要不要另外做成工作坊項目（見評估文件）。
- 萘伊主題的說明文字（我先寫一版，29 種語言）。
