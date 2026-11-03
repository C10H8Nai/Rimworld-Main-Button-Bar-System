# Main Button Bar System

RimWorld 1.7 模組。自訂遊戲畫面底部的主按鈕列：排序、隱藏、改名、收進「更多」抽屜，有原版、摺疊、捲動、摺疊＋捲動四種顯示方式，並相容其他會新增或修改主按鈕的模組。

作者：NAI　packageId：`NAI.MainButtonBarSystem`

設計規格：[docs/superpowers/specs/2026-11-03-main-button-bar-system-design.md](docs/superpowers/specs/2026-11-03-main-button-bar-system-design.md)

## 安裝（測試用）

需要先訂閱或安裝 [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)。這個 repo 本身就是模組資料夾，以下兩種方式擇一：

- **git**：clone 到 RimWorld 的 `Mods` 資料夾，之後 `git pull` 就是新版。
  ```bash
  git clone https://github.com/C10H8Nai/Rimworld-Main-Button-Bar-System.git MainButtonBarSystem
  ```
- **ZIP**：本頁 **Code → Download ZIP**，解壓到 `Mods` 資料夾。

在模組清單啟用，放在 Harmony 之後。

## 資料夾結構

| 路徑 | 內容 |
|---|---|
| `About/` | 模組名稱、作者、支援版本、前置模組 |
| `1.7/Assemblies/` | 編譯好的 `MainButtonBarSystem.dll`（提交進 repo，讓 repo 直接可用） |
| `Defs/` | 內建的顯示方式、外觀、相容橋接（都是積木插槽的 Def） |
| `Languages/` | 英文、简体中文、繁體中文翻譯（Keyed 由 `tools/gen_translations.py` 產生） |
| `Source/MainButtonBarSystem/` | C# 原始碼；`Core/` 是不依賴遊戲的純邏輯（版面計算、排序合併） |
| `Source/MainButtonBarSystem.Tests/` | `Core/` 的單元測試，不需要遊戲就能跑 |
| `docs/` | 設計規格、調查資料、各階段測試清單（`docs/testing/`）、附屬模組作者說明（`docs/API.md`） |
| `tools/` | 開發用腳本 |

## 編譯

需要 .NET 10 SDK（RimWorld 1.7 跑在 .NET 10 CoreCLR 上）：

```bash
dotnet build Source/MainButtonBarSystem/MainButtonBarSystem.csproj -c Release
```

輸出直接放到 `1.7/Assemblies/`。遊戲本體和 Harmony 的組件來自 NuGet，只在編譯時當參考，不隨模組發佈：

| 套件 | 版本 | 說明 |
|---|---|---|
| `Krafs.Rimworld.Ref` | `1.7.4995-unstable` | RimWorld 1.7.4995（Steam unstable 分支）的參考組件 |
| `Lib.Harmony` | `3.0.0-rc.4` | 組件版本 0Harmony 3.0.0.0，和工作坊 Harmony 的 1.7 版相同 |

單元測試：

```bash
dotnet test Source/MainButtonBarSystem.Tests
```

Harmony 3 已將不帶參數的 `PatchAll()` 標為過時，要明確傳入組件：`PatchAll(typeof(X).Assembly)`。
