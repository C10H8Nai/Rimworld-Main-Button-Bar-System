# 主題範本要怎麼發佈：放進本模組，還是另開工作坊項目

日期：2026-11-04。要給作者決定的是最後一節；目前 `make_release.py` 預設採用方案 A。

## 要發佈的東西

- `docs/examples/ExampleTheme/`：能直接複製改名的完整主題模組（About、Defs、全部貼圖、三種語言的 DefInjected、Preview.png），約 20 個檔案、60 KB。
- `tools/make_theme.py`＋`tools/themes/ExampleTheme.json`：從 JSON 配色產生整個主題模組的工具（需要 Python 3 和 Pillow）。
- `docs/API.md`（已經隨發佈檔附上）和 `docs/API.zh-TW.md`。

## 方案 A：放進本模組的發佈資料夾（目前預設）

`make_release.py` 把它們複製到發佈資料夾的 `ThemeTemplate/`：

```
<發佈資料夾>/
  About/ 1.7/ Defs/ Languages/ Textures/ LoadFolders.xml   （和以前一樣）
  API.md  API.zh-TW.md
  ThemeTemplate/
    ExampleTheme/        完整範例模組（About、Defs、Textures、Languages）
    tools/make_theme.py
    tools/ExampleTheme.json
```

遊戲只讀模組根目錄和 `LoadFolders.xml` 列出的資料夾底下的 `About`、`Defs`、`Textures`、`Languages`、`Assemblies`、`Patches`、`Sounds`，`ThemeTemplate/` 裡的 `Defs`、`Textures` 在第二層以下，不會被載入；`make_release.py` 會檢查這一點。

優點：
- 主題作者已經訂閱本模組，範本和工具就在同一個資料夾裡，`API.md` 也在旁邊，不用再找第二個項目。
- 版本永遠同步：範本用的欄位和 DLL 認得的欄位來自同一次發佈。
- 只要維護一個工作坊頁面、一個說明。

缺點：
- 發佈檔大約多 60 KB（主要是 PNG），每個玩家都會下載到，雖然用不到。
- 想做主題的人要知道去模組資料夾找（工作坊說明要寫一行）。
- `make_theme.py` 從 `ThemeTemplate/tools/` 找齒輪來源圖的路徑是 `../../Textures/MainButtonBarSystem/UI/Gear.png`，工具已經處理這個位置；如果有人把 `ThemeTemplate/` 整個搬走，要在 JSON 加 `gearSource`。

## 方案 B：另開「主題範本」工作坊項目

把 `ThemeTemplate/` 的內容做成獨立的工作坊項目（本身也是一個可啟用的主題模組，就是 ExampleTheme，順便示範效果）。

優點：
- 玩家的下載量不變。
- 工作坊搜尋得到「theme template」，對不認識本模組的作者比較顯眼；項目頁可以直接放教學。
- 訂閱後它會在遊戲裡顯示成一款可選的主題，作者馬上能看到範例效果。

缺點：
- 兩個項目要一起更新：欄位加了新東西，範本項目也要重發，否則範本落後；`make_theme.py` 寫出的 Defs 也可能用到舊版 DLL 沒有的欄位（舊主題永遠能載入，但反過來不行）。
- 多一個頁面要維護、多一份說明文字、多一次上傳流程。
- 玩家可能誤訂閱範本當成功能（它只是示範主題）。

## 建議

先用方案 A（已實作）：本模組還在 0.x，欄位可能再變，一份發佈最不容易出錯；主題作者本來就得訂閱本模組。等欄位穩定（1.0）、有別人真的開始做主題時，再考慮把 `ThemeTemplate/` 另外上傳成方案 B 的項目，到時候 `make_release.py --template-only <資料夾>` 之類的開關就夠了（還沒做）。

要切換成 B 的話：`make_release.py` 加 `--no-template` 就不會複製 `ThemeTemplate/`；範本項目的資料夾直接用 `docs/examples/ExampleTheme`（加上 `tools/make_theme.py` 和 JSON）上傳。

## 作者要決定的

- [x] 維持方案 A（預設）／改方案 B／兩者都做。→ **維持方案 A**（作者 2026-11-07）。
- [x] 工作坊說明的「For modders」那段要不要提 `ThemeTemplate/`（`docs/workshop/description.txt` 已加一句，可以刪）。→ 保留那一句，並在「新增主題」那條加上教學檔名。

## 2026-11-07 補充

- 「新增主題」（主題專案）做出來之後，給畫圖的人的入口是它；`ThemeTemplate/` 的 ExampleTheme 當完整範例、`make_theme.py` 當選用的進階工具（教學裡寫了 Python＋Pillow 的安裝步驟）。
- 逐步教學 `docs/ThemeGuide.md`（英文）、`.zh-TW.md`、`.zh-CN.md`、`.ja.md` 和示意圖 `ThemeGuide_NineSlice.png`，`make_release.py` 複製到發佈資料夾根目錄，和 `API.md` 放一起。
