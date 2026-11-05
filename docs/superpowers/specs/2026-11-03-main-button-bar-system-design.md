# Main Button Bar System 設計文件

- 日期：2026-11-03 定稿（設計討論在本機 Claude Code session；實作在雲端 session）
- 設計過程的紀錄：使用者本機的討論筆記（不在 repo）
- Repo：https://github.com/C10H8Nai/Rimworld-Main-Button-Bar-System
- 模組名稱：Main Button Bar System；作者：NAI；packageId：`NAI.MainButtonBarSystem`
- 目標版本：RimWorld 1.7（Waylith 引擎：.NET 10 CoreCLR＋Vulkan）。往下相容以後再說。
- 標記說明：**【使用者】** 是使用者親口決定的；**【判斷】** 是設計者（Claude）提出、使用者沒反對或交給我們決定的，實作者可以在不改變使用者意圖的前提下調整；**【實作者決定】** 是明確交給實作者的。

---

## 1. 為什麼要做

使用者啟用約 2,200 個模組，下方主按鈕列（MainButtons）全部出現時有 74 顆（44 顆全寬、30 顆半格）。原版 `MainButtonsRoot.DoButtons` 把螢幕寬度平均分給所有可見按鈕，1920 寬的螢幕上一顆全寬按鈕只剩約 33px，根本看不清楚。

工作坊上有兩個相關模組，但都不夠：
- 「自定义底部栏 - Custom Bottom Bar」（3830268640）：可調寬度、間距、對齊、順序、名稱、文字或圖示、隱藏、強制顯示、自訂圖示，溢出時能拖曳或滾輪捲動，但沒有抽屜、沒有捲軸。
- 「TooManyButtons」（3717973899）：可隱藏、排序、把少用的按鈕收進彈窗抽屜，但不能調外觀；留言說 1.7 排序失效、讀檔後隱藏的按鈕跑到最後。

這個模組把兩者的功能整合起來，再加上捲動。

**定位【使用者】**：它是一個「轉接層」。使用者可能只有原版主按鈕列，也可能裝得比作者更多，所以相容性要夠好；別人要能在它上面做附屬模組，所以要像積木一樣有接口。

## 2. 目標、非目標、反目標

**目標**
- 使用者在遊戲裡就能替主按鈕列排序、調整每顆按鈕（寬度、間距、對齊、改名、文字或圖示、匯入自訂圖示、隱藏、強制顯示、快捷鍵）。
- 顯示方式四選一：原版（平分寬度，第一次安裝的預設）、摺疊（少用的收進「更多」抽屜）、捲動（按鈕維持最小寬度，整條列可左右捲動）、摺疊＋捲動。
- 和其他會改主按鈕的模組共存：讀它們改完的結果，不把它們的效果洗掉。
- 有接口，別人可以做外觀、顯示方式、相容橋接、按鈕預設值、排序分享等附屬模組。
- 使用者可以在遊戲內用自己的 Steam 帳號上傳工作坊：指用遊戲內建的上傳功能上傳**模組本身**，本模組不整合 Steam 工作坊 API。

**非目標【使用者】**
- 不做建築選單（Architect）裡分類的排序（TooManyButtons 有這功能）。
- 不碰主按鈕「內部」的東西（按下去打開的視窗內容）。
- 不做 1.6 以下版本。

**反目標**（技術上能動，但算失敗）
- 剛安裝、沒碰設定的使用者，主按鈕列外觀和原版（加上其他模組改過的樣子）不一樣（預設是「原版」顯示方式，見 3.3）。
- 其他模組加的圖示、換的視窗、新增的按鈕被我們蓋掉或消失。
- 開一個空接口卻沒有任何內建功能用它（接口沒被測過）。
- 載入或遊戲中出現紅字。

## 3. 使用者已決定的行為

### 3.1 設定畫面【使用者】

示意（以文字描述，遊戲原版視窗風格：深灰底、原版按鈕和勾選框）：

```
┌ 主按鈕列設定（目前存檔／預設範本）   [簡單][進階]           ✕ ┐
│ ┌ 左：按鈕清單 ──────────────────┐ ┌ 右上：所選項目的設定 ───┐ │
│ │ ★ 整條列（預設值）       固定第一列 │ │                          │ │
│ │ ≡ ☑ [圖] 建筑   核心    [B]  ☐   │ │                          │ │
│ │ ≡ ☑ [圖] 工作   核心    [F]  ☐   │ │                          │ │
│ │ ≡ ☑      模組按鈕 某模組        ☑   │ └──────────────────────┘ │
│ │ 🔒 ☑ [圖] 菜单   核心   [Esc]  —   │ ┌ 右下 ──────────────────┐ │
│ └──────────────── 可上下捲動 ───┘ │ [全部重設] [取消] [儲存] │ │
│ [上移] [下移]        上→下＝左→右      └──────────────────────┘ │
└──────────────────────────────────────────────┘
```

- 左側是主按鈕清單，欄位依序：拖曳把手、顯示（勾選框）、圖示、名稱、來源模組、快捷鍵（只在進階出現）、抽屜（勾選框）。欄位要對齊；**沒有圖示的按鈕圖示欄留空**。清單可上下捲動。
- 排序：拖把手，或選取後按「上移」「下移」。**清單由上到下＝遊戲畫面由左到右。**
- 清單最上面固定一列「整條列（預設值）」：不能拖、不能隱藏、開視窗時預設選中。選它時右上顯示整條列的設定（見 3.3）。**顯示方式切換放在這裡**（使用者提議把它和整條列合併）。
- 右上：所選按鈕（或整條列）的設定。按鈕的資訊區顯示名稱、**按鈕來源模組、圖示來源模組**、defName。
- 右下：全部重設、取消、儲存。
- 分「簡單」「進階」兩個分頁，欄位分配見 3.2。

### 3.2 簡單／進階的欄位分配

| 項目 | 簡單 | 進階 | 來源 |
|---|---|---|---|
| 顯示／隱藏、收進抽屜、自訂名稱 | ✔ | ✔ | 【使用者】（示意圖確認） |
| 排序（拖曳、上移下移） | ✔ | ✔ | 【使用者】 |
| 整條列：顯示方式、捲動時每顆最小寬度 | ✔ | ✔ | 【使用者】 |
| 整條列：預設寬度、按鈕間距、文字對齊、填滿畫面寬度 |  | ✔ | 【使用者】（示意圖 v5 確認） |
| 單顆：寬度（預設「跟整條列」）、文字／圖示／兩者、選內建圖示或匯入圖片、強制顯示 |  | ✔ | 【判斷】 |
| 單顆：固定位置（不固定／左端／右端） |  | ✔ | 【判斷】 |
| 快捷鍵：清單欄位、右側「快捷鍵：[鍵] [設定] [清除]」 |  | ✔ | 【使用者】（清單欄位）／【判斷】（右側欄位） |
| 遊戲選單（菜单）鎖在最右邊 | 鎖定 | 可解鎖 | 【使用者】（簡單鎖定）／【判斷】（進階可解鎖） |
| 衝突模組：一行提示 | ✔ | ✔ | 【使用者】 |
| 衝突模組：優先級、對方設定值參考、匯入對方設定 |  | ✔ | 【使用者】 |

### 3.3 整條列與顯示方式【使用者】

顯示方式有四種：原版、摺疊、捲動、摺疊＋捲動。

- **原版**【使用者 2026-11-03 追加】：和原版一樣把畫面寬度平分給可見按鈕，不捲動、沒有抽屜（「收進抽屜」沒有作用）。排序、隱藏、改名、圖示等其他設定照樣生效。**第一次安裝、還沒存過預設範本時，預設就是這個模式**，所以只有原版按鈕列的人裝了不會有變化；使用者換成其他模式後按「存成預設範本」，之後的新存檔就用自己的排法。「對方優先」時也等於讓出繪製。
- **摺疊**：標了「收進抽屜」的按鈕不出現在列上，改放進「更多」按鈕的彈出抽屜。其餘按鈕平分寬度（填滿畫面寬度開啟時）。
- **捲動**：所有可見按鈕維持寬度（單顆寬度 → 整條列預設寬度 → 不得小於「最小寬度」），整條列超出畫面時可左右捲動：滑鼠滾輪、兩端左右箭頭。**列的下方不放捲軸**【使用者 2026-11-05 修改】。捲動模式下「收進抽屜」沒有作用（清單那欄顯示「—」）。
- **摺疊＋捲動**：抽屜照常，剩下的按鈕可捲動。
- 列的右側順序：捲動區 →「更多」→ 固定在右端的按鈕（預設只有菜单）。固定在左端的按鈕在最左邊、不跟著捲動。固定的按鈕不能收抽屜。
- 「更多」按鈕：點了往上彈出格狀抽屜；**抽屜下方有自己的設定按鈕**，打開設定畫面。**抽屜沒有按鈕時「更多」藏起來**（這時遊戲中只能從選項進設定）。
- 遊戲選單（菜单）：**簡單模式下固定在最右邊，不能隱藏、移動、收抽屜，只能改名**。

### 3.4 設定存放【使用者】

- **設定每個存檔分開**，存在存檔裡。
- **預設範本**：`Config` 另存一份共用範本。新遊戲、第一次載入的舊存檔，都從範本開始。設定畫面有「存成預設範本」「套用預設範本」。還沒存過範本時，用原版排序加上其他模組改完的結果（也就是「沒改過」的狀態）。
- 從某個存檔移除本模組時，讀檔可能出一次找不到類別的紀錄，要寫進工作坊說明。

### 3.5 設定入口【使用者】

1. 選項 → 模組設定：在遊戲中打開，改目前存檔；在主選單打開，改預設範本。視窗標題寫明是哪一個。
2. 遊戲中點「更多」，抽屜裡的設定按鈕。
- 主按鈕列上不做右鍵入口。

### 3.6 快捷鍵【使用者】＋【判斷】

- 【使用者】設定畫面可以編輯每顆按鈕的快捷鍵；進階模式清單多一欄顯示快捷鍵。被隱藏、收進抽屜的按鈕也保留快捷鍵（不像 TooManyButtons 那樣一隱藏就失效）。
- 【判斷】快捷鍵所有存檔共用：直接改原版鍵盤設定（`KeyPrefs.xml`），「選項 → 鍵盤設定」看到的一樣。原本沒有快捷鍵設定的模組按鈕，由本模組另存一份全域設定。撞鍵時顯示警告。

### 3.7 相容【使用者】

- **只讀其他模組改完的結果**：圖示（Bradson's Main Button Icons、Vanilla Textures Expanded）、換掉的視窗（Modern History Menu）、新增的按鈕都照常生效。只有使用者在設定裡改過的欄位才蓋過去。
- 新裝模組多出來的按鈕，自動加到清單最後（菜单左邊）【判斷】；模組拿掉後，那顆按鈕的設定保留但不顯示【判斷】。
- **會接管整條列繪製的模組**（目前已知 TooManyButtons、Custom Bottom Bar）：
  - 簡單模式：上方一行提示，例如「偵測到 TooManyButtons，可能和本模組衝突，詳細請到進階」。
  - 進階模式：每個偵測到的模組各一個優先級選項——「本模組優先」（整條列由本模組畫，對方對主按鈕列的效果被關掉，它的設定只當參考值）或「對方優先」（本模組讓出主按鈕列，只保留設定畫面）。優先級以模組為單位，不逐欄位決定。受影響的欄位旁顯示對方的設定值（例如清單「工作」旁標「TooManyButtons：隱藏」，寬度欄下寫「Custom Bottom Bar：120px」）。一顆「匯入它的設定」把對方的排序、隱藏、寬度、改名抄過來。

### 3.8 接口（積木）【使用者同意的方案】

核心只負責：主按鈕清單（含其他模組改完的結果）、排序、每顆按鈕的覆寫設定、存檔與範本。其他都透過五種插槽接上，**內建功能也走同一套插槽**，第一版每個接口至少被自己用過一次：

| 插槽 | 用途 | 內建積木 | 附屬模組例子 |
|---|---|---|---|
| 外觀 | 按鈕和整條列怎麼畫（背景、圖示大小、滑鼠移上的效果） | 原版風格 | 半透明、深色主題 |
| 顯示方式 | 按鈕怎麼排 | 原版、摺疊、捲動（＋組合） | 兩排、直立側邊 |
| 相容橋接 | 偵測其他主按鈕列模組、讀設定、匯入、讓它停手 | TooManyButtons、Custom Bottom Bar | 其他類似模組 |
| 按鈕預設值 | 其他模組作者在自己的 MainButtonDef 上用 XML 宣告預設（預設收抽屜、預設圖示、預設固定位置等），不用寫程式 | （無） | 圖示包、大型模組 |
| 查詢 | 其他模組查詢目前哪些按鈕在列上、哪些在抽屜、整條列高度與位置 | （無） | 要在主按鈕列上方放東西的模組 |

### 3.9 排序分享【使用者】

兩種都做：
1. 設定畫面「匯出」「匯入」：存成檔案，或複製成一段文字（貼到 Discord、LINE）。
2. 做成模組：作者把排序寫成一段 XML、包成工作坊模組（排序積木），訂閱者在匯入選單直接看到、套用。

匯入規則【判斷】：匯入整份排法（順序、隱藏、抽屜、固定位置、改名、寬度、顯示方式）；本機沒有的按鈕略過；本機有、排序裡沒有的按鈕接在最後（菜单左邊）；缺少的自訂圖片改用原圖示；只蓋目前存檔，要當範本再按「存成預設範本」。

---

## 4. 設計者的判斷（使用者可檢查）

以下是我（Claude）的決定，使用者沒反對或交給我們決定：

1. 進階模式下菜单可解鎖；任何按鈕可固定在左端或右端。
2. 單顆按鈕的進階欄位：寬度、文字／圖示／兩者、選內建圖示或匯入圖片、強制顯示（原本有條件才出現的按鈕也顯示）。
3. 新按鈕加在最後（菜单左邊）；拿掉的模組設定保留。
4. 自訂圖示放在模組資料夾外（例如 `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\MainButtonBarSystem\Icons`），避免 Steam 更新洗掉（Custom Bottom Bar 放在自己模組資料夾，更新時會被洗掉）。
5. 快捷鍵全域共用，見 3.6。
6. 匯入規則，見 3.9。
7. 設定畫面開著時，改動直接反映在遊戲畫面下方真正的主按鈕列上（即時預覽）；「取消」還原到打開視窗前；「儲存」寫入存檔設定。
8. 「全部重設」＝回到「沒改過」的狀態（原版＋其他模組的結果），不是套用範本。
9. 匯出文字用可讀的 XML（和檔案內容相同），不另外壓縮。
10. 滾輪在主按鈕列上時只捲列，不讓地圖跟著縮放。
11. 清單範圍與「顯示」的語意：
    - 清單列出所有 MainButtonDef。
    - 「顯示」勾選＝使用者有沒有隱藏它；預設勾選，意思是「照原本的條件出現」。
    - 「強制顯示」（進階）＝原本條件不成立（`Worker.Visible` 為 false）或 `buttonVisible=false` 時也顯示。
    - 有條件才出現的按鈕，清單上標「有條件」。
    - `buttonVisible=false` 的按鈕（例如原版 Inspect、Dubs Mint Menus 的 3 個）放在清單底部的收合群組「原本不顯示」，只有開了強制顯示才會出現在列上。
12. 半格（`minimized`）按鈕在每種模式都維持「一般按鈕的一半寬」；單顆設了寬度就以設定為準。
13. 「沒改過」的基準線：定義載入完成後（含所有 XML 補丁）、任何接管型模組在執行期動手之前，拍下每顆按鈕的原始狀態。「全部重設」「沒改過時用原本的」都以這份為準。
14. 和接管型模組並存：
    - 「對方優先」時，設定畫面照樣能開、能編輯，上方標示「目前由 X 控制，改動要等改成本模組優先才會生效」。
    - 「本模組優先」時，TooManyButtons 自己新增的那顆按鈕照一般按鈕列在清單，預設隱藏，因為它的抽屜功能已經被取代。
15. 匯出的檔案或文字，和「排序積木」用同一份格式（含格式版本號）。排序積木是一個 Def，內容就是匯出的那一段。
16. 簡單／進階只是兩種檢視，資料是同一份。「菜单鎖在最右」是一個設定，預設開啟，只能在進階改；在進階解鎖後切回簡單，簡單照實顯示，不會自動改回去。

## 5. 交給實作者決定

- 插槽的具體型別設計（Def＋workerClass、介面、事件等）、命名空間、API 文件形式。建議用 Def 登記插槽，讓附屬模組只靠 XML＋自己的 DLL 就能接上，不用處理載入順序。
- 每個存檔的設定用 GameComponent 或 WorldComponent。
- 「選項 → 模組設定」的原版設定視窗空間有限，可以在裡面放一顆按鈕打開本模組自己的視窗，或直接開自己的視窗。
- 「本模組優先」怎麼讓對方停手（Custom Bottom Bar 有 `enabled` 總開關；TooManyButtons 直接改 `MainButtonDef.buttonVisible`／`order` 和 `allButtonsInOrder`，可能要撤掉它的 Harmony 補丁或在繪製前忽略它的改動）。
- 「圖示來源模組」怎麼查（例如用 `iconPath` 反查哪個模組的 Textures 有這張圖）。查不到就留空。
- 捲動的實作細節（箭頭每次捲多少）。
- 預覽、抽屜的動畫與排版細節。

## 6. 技術限制與環境

- **編譯**：這台沒有 dotnet SDK；有 Visual Studio Build Tools 18 的 Roslyn：`C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe`（C# 14）。雲端實作端請自行準備環境。
- **1.7 組件是 net10.0**。參考遊戲根目錄的 `System.Runtime.dll`、`System.Private.CoreLib.dll`、`Assembly-CSharp.dll`、`UnityStubs.dll`（UnityEngine 型別在這裡；Custom Bottom Bar 另外引用 `Waylith.dll`），Harmony 用工作坊 2009463077 的 `1.7\Assemblies\0Harmony.dll`。參考的 csproj：TooManyButtons `Source\TooManyButtons\TooManyButtons.csproj`（net10.0）、Custom Bottom Bar `Source\BottomBarSpacing.csproj`。
- **前置**：只有 Harmony（`brrainz.harmony`，工作坊 2009463077），寫在 About.xml 的 `modDependencies` 和 `loadAfter`。相容的模組都是執行時偵測，不是前置；NuGet 套件只在編譯時用，不隨模組發佈。
- **雲端編譯與測試流程【判斷，使用者沒意見】**：
  - 遊戲的 DLL 不放進 repo（版權）。雲端改用 NuGet：`Krafs.Rimworld.Ref` 有 `1.7.4995-unstable`，和使用者的遊戲版本 1.7.4995 相同；Harmony 用 `Lib.Harmony` 3.0.0-rc（工作坊 Harmony 1.7 版的組件版本是 3.0.0.0），編譯時只當參考、不隨模組發佈。這兩個套件涵蓋哪些組件（例如 `UnityStubs.dll`、net10 的 `System.Runtime`）沒驗證，雲端第一步要先確認編得過。
  - 雲端需要 .NET 10 SDK。編好的 DLL 提交進 repo 的 `1.7/Assemblies/`，讓 repo 本身就是模組資料夾。
  - 本機：把 repo clone 到 `D:\Steam\steamapps\common\RimWorld\Mods\` 底下，之後每次 `git pull` 就拿到新版；本機 session 跑 `verify_dll.py`、`check_order.py`；使用者在 RimCrow 啟用、進遊戲測，測完由本機 session 看 `Player.log`，把結果帶回雲端。
  - 實作要分階段交付，高風險的部分先做最小版本讓使用者測：`DoButtons` 補丁和其他模組（Performance Optimizer、TooManyButtons、Custom Bottom Bar）的互動、`Texture2D.LoadImage` 匯入圖片、快捷鍵。
- **Harmony 3 型別檢查比較嚴**：補丁參數型別要和原方法完全一致（例如 `__instance` 型別錯會整個 PatchAll 中斷）。
- **原版 1.7 相關成員**（從 Assembly-CSharp 讀出）：
  - `MainButtonDef`：`workerClass`、`tabWindowClass`、`buttonVisible`、`order`、`defaultHotKey`、`hotKey`（KeyBindingDef）、`validWithoutMap`、`minimized`（半格）、`iconPath`、`closesWorldView`、`validWithClassicIdeo`、`Icon`、`ShortenedLabelCap`、`LabelCapWidth`。
  - `MainButtonsRoot`：欄位 `tabs`、`allButtonsInOrder`；方法 `MainButtonsOnGUI`、`HandleLowPriorityShortcuts`、`DoButtons`、`VisibleButtonsCount`。
  - `MainButtonWorker`：`DoButton(Rect)`（有圖示時只畫圖示）、`Visible`、`Disabled`、`Activate`、`InterfaceTryActivate`、`ButtonBarPercent`。
  - 原版 `DoButtons` 把畫面寬度平分給可見按鈕，`minimized` 算半格。
- **自訂圖片載入**：Custom Bottom Bar 在 1.7 用 `new Texture2D(2,2)` ＋ `LoadImage(bytes)`，應該可用，但我們沒在 Waylith 驗證過。
- **專案規則**（本機）：不要直接改遊戲的 `Config\ModsConfig.xml`，啟用由使用者在 RimCrow 做；啟用後跑 `RimClaude\tools\_草稿_1.7工具\check_order.py`；DLL 用 `verify_dll.py` 檢查對遊戲本體的引用。遊戲載入約 27 分鐘，使用者會配合測，測完看 `Player.log`。
- **翻譯**：English、简体中文、繁體中文。使用者的遊戲語言是简体中文。

## 7. 調查結果

### 7.1 兩個參考模組

- 兩個都**沒寫授權**、都附原始碼。只能參考做法，**不抄程式碼和貼圖**。
- **TooManyButtons**（`East.TooManyButtons`，作者 East）
  - `MainButtonsRoot.DoButtons` Prefix（不跳過原版）：只在 `MainButtonsRoot` 換實例時（讀檔、回主選單再進）改 `def.buttonVisible`、`def.order`，並用 Traverse 換掉 `allButtonsInOrder`；`Game.FinalizeInit` Postfix 再套一次；另有 `MainTabWindow_Architect.CacheDesPanels` Prefix（建築分類排序）。
  - 自己新增一個 MainButtonDef（TMB 按鈕），點開是抽屜彈窗。
  - 設定 `TMBSettings`（ModSettings）：`mainTabSlots`（每顆 MainTab／TMBWindow／Hidden）、`mainTabOrder`、`tmbWindowOrder`、`hiddenTabOrder`、`architectOrder`、`vanillaMainTabOrder`、`vanillaArchitectOrder`。Harmony id `East.TooManyButtons`。
  - 因為隱藏是設 `buttonVisible=false` 並從清單移除，被隱藏的按鈕快捷鍵也失效。
- **Custom Bottom Bar**（`dogebm.custombuttons`，作者 DOGEBM，DLL `BottomBarSpacing.dll`）
  - `MainButtonsRoot.DoButtons` Prefix **回傳 false**，整條列自己畫（溢出時可拖曳、滾輪捲動）；`MainButtonWorker.DoButton(Rect)` Prefix 處理對齊和圖示；另補 `ModMetaData.Name` getter（改自己的顯示名稱）。用反射呼叫 Harmony。
  - 設定 `BottomBarSpacingSettings`（ModSettings）：`enabled`（總開關）、`buttonWidth`、`labelPadding`、`gap`、`labelAlign`、`hideIcon`、`unifyLong`、`fillScreen`、`buttonOrder`、`overrides`（`ButtonOverride` 清單：`defName`、`customWidth`／`width`、`padding`、`customGap`／`gap`、`customAlign`／`labelAlign`、`customHideIcon`／`hideIcon`、`hidden`、`forceShow`、`customIcon`、`customName`）。
  - 自訂圖示放在它模組資料夾的 `Textures/icons`。

### 7.2 使用者環境裡的主按鈕（2026-11-03 掃描，2,200 個啟用模組）

- 87 個 MainButtonDef 定義（含同名覆蓋，例如 Nice Research Tab 蓋掉原版 Research、Animal Tab — Unofficial Update 蓋掉 Animals）。很多由 `workerClass` 的 `Visible` 決定是否出現（例如有機械體才出現）。部分 `buttonVisible=false`（Dubs Mint Menus 的 3 個、Milira 的 Milian_Work 等），不應該被當成使用者隱藏。
- 用 XML Patch 改 MainButtonDef 的模組 9 個：Bradson's Main Button Icons (Forked + Expanded)（173 處，加 `iconPath`）、Vanilla Textures Expanded（18 處）、Modern History Menu（換 History 的 `tabWindowClass`）、WVC - Work Modes、Mech Work Tab、More Faction Interaction (Continued)、Prepare Landing (Continued)、Rules of Engagement、SS Lightning Rod (Continued)。
- DLL 引用 `MainButtonsRoot` 的啟用模組 15 個，多數只是用 `Find.MainButtonsRoot` 開分頁。**真的補丁 `DoButtons` 的**：Performance Optimizer（只有 1.6 版，`Optimization_MainButtonsRoot_DoButtons` 有 Prefix，行為沒查）；WOWGAG's Performance Patch（GUI 效能分段計時，補 `MainButtonsOnGUI` 一帶）。其他引用者：Character Editor、Adaptive Storage Framework、[LTO] Colony Groups、Dubs Mint Menus、Dubs Mint Minimap、Layered Atmosphere and Orbit、PA's God Hands、Prisoner Good Treat Voluntary Join、Dragonian Prime Race2.0 (Unofficial)、Ratkin Knights+、Tab-sorting、Weapon Station、边缘股市（RimStock）。
- 掃描原始資料：`docs/research/mainbuttons_20261103.json`。

## 8. 風險與未驗證

- Performance Optimizer 的 `DoButtons` Prefix 會不會跳過原版繪製，進而跳過我們的繪製：沒查。
- 多個模組在 `DoButtons` 上都有 Prefix 時的執行順序和「回傳 false」的互動，要依 Harmony 3 的實際行為設計（Custom Bottom Bar 回傳 false）。
- `Texture2D.LoadImage` 在 Waylith 能不能用：只知道 Custom Bottom Bar 1.7 版在用。
- 原版是否替每個 MainButtonDef 都產生 KeyBindingDef（`KeyBindingDefGenerator`），影響「沒有快捷鍵的按鈕」怎麼新增快捷鍵：沒查。
- 「圖示來源模組」反查是否可行：沒查。

## 9. 完成的定義

- 模組在 1.7 能載入，`Player.log` 沒有本模組的紅字。
- 設定畫面能排序、調寬度、隱藏、換圖示（含匯入圖片）、編輯快捷鍵。
- 原版、摺疊、捲動、摺疊＋捲動四種模式都能用；第一次安裝預設是原版。
- 設定存在存檔裡：存檔、重開遊戲、讀檔後保留；預設範本在新遊戲生效。
- 和 Bradson's Main Button Icons、Vanilla Textures Expanded、Modern History Menu 同時啟用時，它們的效果照常。
- 偵測到 TooManyButtons、Custom Bottom Bar 時，簡單模式有提示、進階模式有優先級和匯入。
- 每個插槽至少有一個內建積木用它；附上給附屬模組作者的英文說明（README 或 docs）。
- 排序可匯出／匯入（檔案、剪貼簿），也能從 XML 排序積木匯入。
- 英文、简体中文、繁體中文翻譯齊全；About.xml（supportedVersions 1.7、modDependencies Harmony）、Preview.png 備妥。
- 使用者實際進遊戲測過；工作坊上傳由使用者在遊戲內用自己的帳號做，實作端只準備檔案和說明文字。
