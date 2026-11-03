# Main Button Bar System

RimWorld 1.7 模組。自訂遊戲畫面底部的主按鈕列：排序、隱藏、改名、收進「更多」抽屜，支援摺疊與捲動顯示，並相容其他會新增或修改主按鈕的模組。

作者：NAI

## 安裝（測試用）

1. 需要先訂閱或安裝 [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)。
2. 在本頁按 **Code → Download ZIP**。
3. 解壓縮到 RimWorld 的 `Mods` 資料夾，例如：
   `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\MainButtonBarSystem`
4. 在遊戲的模組清單啟用，並放在 Harmony 之後。

## 資料夾結構

| 路徑 | 內容 |
|---|---|
| `About/` | 模組名稱、作者、支援版本 |
| `1.7/Assemblies/` | 編譯好的 `MainButtonBarSystem.dll` |
| `Source/` | C# 原始碼 |

## 編譯

需要 .NET SDK 8 以上：

```bash
dotnet build Source/MainButtonBarSystem/MainButtonBarSystem.csproj -c Release
```

輸出會直接放到 `1.7/Assemblies/`。
