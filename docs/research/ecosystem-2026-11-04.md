# RimWorld 模組生態調查：讓別人容易相容本模組（2026-11-04）

雲端子代理實際 clone 各專案的 GitHub repo 讀程式碼後整理。rimworldwiki、Steam 工作坊、ludeon.com、rimsort.github.io 在雲端連不到，靠這些來源的內容標「未驗證」。

## 1. 框架與函式庫

| 名稱 | 網址 | 1.6／1.7 狀況 | 對本模組 |
|---|---|---|---|
| Harmony（模組） | https://github.com/pardeike/HarmonyRimWorld | 支援 1.2～1.7；1.7 版是 .NET 10 上的 Harmony 3 | **高**：本模組的前置 |
| Harmony 文件 | https://github.com/pardeike/Harmony/tree/master/Documentation/articles | 最新 | **高**：補丁優先級、`HarmonyBefore/After`、prefix 回傳 false 對其他 prefix 的影響 |
| Prepatcher | https://github.com/Zetrith/Prepatcher | 只到 1.6；README 說它綁 Unity 的 Mono，1.7 不能用 | 低：不要依賴 |
| HugsLib | https://github.com/UnlimitedHugs/RimworldHugsLib | 最後更新 2025-07（1.6），沒有 1.7 | 低 |
| Vanilla Expanded Framework | https://github.com/Vanilla-Expanded/VanillaExpandedFramework | 支援到 1.7 | 低：程式裡完全沒碰主按鈕 |
| XML Extensions | https://github.com/15adhami/XmlExtensions | 只到 1.6 | 低：玩家可能拿它改 Tuning |
| Fluffy 的模組（WorkTab、AnimalTab、ResearchTree、ModManager） | https://github.com/fluffy-mods | 停在 1.2～1.3 | 中：只當「分頁模組怎麼接進遊戲」的例子 |
| Multiplayer＋API | https://github.com/rwmt/Multiplayer 、https://github.com/rwmt/MultiplayerAPI | 只支援 1.6；API 版本 "0.6" | 中：它自己也補丁 `DoButtons`，還會同步模組設定檔 |
| RimSort＋社群規則 | https://github.com/RimSort/RimSort 、https://github.com/RimSort/Community-Rules-Database | 活躍；規則檔 `communityRules.json`（CC0） | 中 |

## 2. 會碰主按鈕列或分頁的模組

| 模組 | 怎麼接進遊戲 | 和本模組並存 |
|---|---|---|
| Dubs Mint Menus（https://github.com/Dubwise56/Dubs-Mint-Menus） | repo 公開、支援 1.7，但**只有 DLL 沒有原始碼**。定義 3 顆隱藏按鈕、2 顆半格按鈕；DLL 裡有 `MainButtonsOnGUI`、`HandleLowPriorityShortcuts` 的補丁（內容未驗證） | 前幾輪實測同時啟用沒有問題 |
| Multiplayer | 觀戰者時 `DoButtons` 的 prefix 回傳 false，只畫選單和世界；postfix **每次都**照 `def.order` 重排 `allButtonsInOrder`（連單人遊戲也是） | 本模組不讀 `allButtonsInOrder` 的順序，所以不受重排影響 |
| Tab-sorting（https://github.com/emipa606/TabSorting） | 改 `MainButtonDef.order`，用反射重寫 `allButtonsInOrder` | 它的 `IgnoreSettingDef`（讓別的模組宣告「別動我」）是好的參考設計；之後可以做「從它匯入排序」 |
| DragReorder（https://github.com/ifchen0/DragReorder） | 建構子 postfix 重排清單；`MainButtonsOnGUI` prefix 用**原版按鈕位置**判斷拖曳 | 本模組畫列時，它的拖曳會落在錯的位置。**本輪已加相容橋接** |
| UI Not Included（https://github.com/GonDragon/UINotIncluded） | transpiler 把 `MainButtonsOnGUI` 裡呼叫 `DoButtons` 的地方換成自己的列 | 它啟用時本模組的補丁**根本不會執行**。**本輪已加相容橋接**（本模組優先時移除它這個 transpiler） |
| RimHUD（https://github.com/Jaxe-Dev/RimHUD） | 補丁 `MainTabWindow_Inspect.PaneTopY`，假設主按鈕列高 35 | 附屬模組如果做出比一列高的顯示方式，檢視面板會被蓋住（原版也有同樣的假設） |
| Work Tab、Animal Tab、Numbers | 換 `tabWindowClass`、用同名 Def 覆蓋、純 XML 新增按鈕 | 本模組用 defName 認按鈕、照樣呼叫 `DoButton`，三者都安全 |
| 舊的列模組：Reorderer、Vanilla UI Expanded、ModButtons | 建構子 postfix、`DoButtons` prefix 等 | 只到 1.3／1.4，僅供參考 |
| TooManyButtons、Custom Bottom Bar | 找不到公開的 GitHub repo（未驗證） | 已有相容橋接 |

## 3. 建議與處理狀態

| # | 建議 | 狀態 |
|---|---|---|
| 1 | **`DoButtons` 的觀察要放在 postfix**。Harmony 3：前面的 prefix 回傳 false 後，後面所有「會影響原方法」的 prefix（回傳 bool 或有 ref 參數）都會被跳過，所以本模組 prefix 裡「別人已經接管」的判斷永遠不會執行。已讀 Harmony 原始碼確認（`MethodCreatorTools.AffectsOriginal`、`MethodCreator` 第 223 行）。 | **已修**：觀察改到 postfix |
| 2 | 軟相依的寫法寫進 API.md：`MayRequire`、`PatchOperationFindMod`（比對模組**名稱**）、`LoadFolders.xml` 的 `IfModActive` | **已寫**。`MayRequire` 用在 `modExtensions` 的 `Class=` 項目上是否有效**未驗證**，要實機測 |
| 3 | `About.xml` 加 `modVersion`；加 `LoadFolders.xml` | **已加**（`modVersion` 0.6.0）。`url` 等工作坊頁面建立後再加 |
| 4 | 公開 API 加版本號，列出哪些是穩定的、移除前先標 `[Obsolete]` | **已加** `MainButtonBarAPI.ApiVersion`（屬性，不是 const，呼叫端才讀得到實際安裝的版本）和 API.md「API stability」 |
| 5 | API.md 公開 Harmony id、補丁清單和優先級；不跳過原方法的補丁要維持不跳過 | **已寫**「Harmony patches and ordering」表 |
| 6 | Multiplayer：向 Multiplayer 開 PR，把本模組加進 `SyncConfigs.cs` 的 `ignoredConfigsModIds`（Dubs Mint Menus、RimHUD 都在名單上）；多人連線時每個玩家的排法改存在存檔外 | **待辦**。Multiplayer 還沒有 1.7 版，不急。本模組不呼叫 `Rand`，存檔裡的排法不會造成不同步；但加入的玩家會拿到主機的排法 |
| 7 | 偵測 UI Not Included、DragReorder；Tab-sorting、DragReorder 的排序可以當匯入來源 | 偵測**已做**（相容橋接）；匯入是**待辦** |
| 8 | API.md 寫明限制：比一列高的列不被原版和 RimHUD 尊重；改名會改到整個遊戲的 `label` | **已寫**「Limits to know」 |
| 9 | 翻譯慣例 | 已符合（`MBBS_` 前綴、DefInjected 帶命名空間），不用改 |

## 4. 1.7（Waylith）相關

- GitHub 上可確認的：
  - Harmony 3 跑在 .NET 10 上。
  - RimRef 現在只附遊戲本身的組件，模組改用一般的 .NET SDK 編譯（https://github.com/krafs/RimRef/discussions/108）。
- 只看到搜尋摘要、未驗證的（ludeon.com 連不到）：
  - 大部分 1.6 模組不用改就能用。
  - 存檔雙向相容。
  - 用 Prepatcher 的模組會壞。
  - 1.7 還在 unstable 分支。
- Multiplayer、HugsLib、XML Extensions、RimHUD、Tab-sorting、DragReorder、UI Not Included 在 GitHub 上**都還沒有 1.7 版**，發佈時和它們的並存情況等於沒測過。
