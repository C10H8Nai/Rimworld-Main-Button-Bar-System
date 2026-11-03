# 第三階段修正：實機測試結果

- 測試分支：`claude/stage-3-fixes`（commit `94e500c`）
- 環境：RimWorld 1.7.4995 rev309（Waylith，.NET 10），啟用 2,201 個模組，本模組和上次一樣已啟用（Harmony 之後）。
- TooManyButtons、Custom Bottom Bar：沒有啟用。

## 1. 靜態檢查

- `verify_dll.py`（`1.7/Assemblies/MainButtonBarSystem.dll`）：**問題 0**。
- 組件參考和上一版相同（System.Runtime 10.0.0.0、Assembly-CSharp 1.7.9800.9270、UnityStubs 1.0.0.0、0Harmony 3.0.0.0、System.Collections／Xml.ReaderWriter／Linq／Threading／Text.Encoding.Extensions／Text.RegularExpressions 10.0.0.0），版本和遊戲一致。
- 對 UnityStubs 的 55 個成員引用：問題 0。
- `check_order.py`：沒有和本模組有關的問題（只有原本就有的 2 個缺前置、25 組同名 DLL）。
- `xml_check.py`（含新的 `Defs/MainButtonBarSystem/Tuning.xml`）：問題 0。
- DefInjected 資料夾已改成 `MainButtonBarSystem.BarDisplayModeDef`、`MainButtonBarSystem.BarAppearanceDef`（簡中、繁中都有）。

## 2. 原版行為確認（從遊戲的 Assembly-CSharp.dll 反組譯 IL）

### `MainButtonWorker.DoButton(Rect rect)`

依序做這些事：

1. `Text.Font = GameFont.Small`。
2. **決定文字**：先取 `def.LabelCap` 和 `def.LabelCapWidth`。如果寬度 `> rect.width - 2`，改用 `def.ShortenedLabelCap` 和 `def.ShortenedLabelCapWidth`。
3. **停用中**（`Disabled` 為真）：只用 `Widgets.DrawAtlas(rect, Widgets.ButtonSubtleAtlas)` 畫背景。滑鼠在按鈕上按下（MouseDown）時把事件 `Use()` 掉，然後**直接 return**。所以停用的按鈕沒有文字、沒有圖示、沒有提示框。
4. **左邊距**：`textLeftMargin = (文字寬 > rect.width × 0.85 − 1) ? 2f : -1f`。文字快塞滿按鈕時只留 2px，否則傳 -1，交給 `ButtonTextSubtle` 自己算（見下）。
5. **畫背景和文字**：呼叫
   `Widgets.ButtonTextSubtle(rect, 文字, ButtonBarPercent, textLeftMargin, SoundDefOf.Mouseover_Category, functionalSizeOffset: Vector2.zero, labelColor: null, highlight: false)`。
   - **有圖示時傳的文字是空字串 `""`**，所以有圖示的按鈕完全不畫文字，只畫圖示。
   - 回傳 true（被點擊）時呼叫 `InterfaceTryActivate()`。
6. **圖示**（`def.Icon != null`）：
   - 中心點是 `rect.center`。滑鼠在按鈕上時，中心點往右 2px、往上 2px（和文字的位移一樣，看起來像按下去）。
   - 畫成 **32×32**：`GUI.DrawTexture(new Rect(中心.x − 16, 中心.y − 16, 32, 32), def.Icon)`，預設的拉伸模式（StretchToFill），不依按鈕高度縮放。
   - 這時 `GUI.color` 已經被 `ButtonTextSubtle` 設回白色，所以圖示不會被滑鼠移上去的顏色染到。
   - `def.Icon` 是延遲載入：第一次讀取時用 `ContentFinder<Texture2D>.Get(iconPath, reportFailure: true)` 載入，存在 `icon` 欄位。`iconPath` 是 null 就一直是 null。
7. **教學提示**：目前打開的分頁不是這顆、而且沒有開著的非即時對話框時，呼叫 `UIHighlighter.HighlightOpportunity(rect, def.cachedHighlightTagClosed)`。
8. **滑鼠提示**：滑鼠在按鈕上，而且 `def.description` 不是空的時候，`TooltipHandler.TipRegion(rect, 黃色的完整 LabelCap + "\n\n" + description)`。提示框裡用的是完整名稱，不是縮短後的。

截短的方式（`TaggedString.Shorten` → `GenText.Shorten`）：

- 字串是空的或只有 4 個字元以下時，原樣回傳。
- 否則用空格切成單字。長度 3 以上的單字，保留第一個和最後一個字元，中間刪掉小寫母音（`aeiouy`）。長度 1～2 的單字會被丟掉，只剩下分隔的空格。
- **不是加刪節號的截斷**。中文沒有空格也沒有母音，所以中文名稱縮短後和原本一樣。太長的中文名稱不會被縮短，會直接超出按鈕（`WordWrap = false`）。

`ButtonBarPercent`：基底類別回傳 0。原版只有 `MainButtonWorker_ToggleResearchTab` 覆寫，回傳目前研究計畫的 `ProgressPercent`（沒有研究時是 0），所以研究按鈕裡會有進度條。原版的子類別沒有一個覆寫 `DoButton`。

### `Widgets.ButtonTextSubtle(rect, label, barPercent, textLeftMargin, mouseoverSound, functionalSizeOffset, labelColor, highlight)`

1. 判定範圍 `rect2` = `rect` 的寬、高各加上 `functionalSizeOffset`（DoButton 傳 0，所以和 `rect` 一樣）。
2. 滑鼠在 `rect2` 上時，記下「滑鼠在上面」，並把 `GUI.color` 設成 `GenUI.MouseoverColor`。
3. `mouseoverSound` 不是 null 時呼叫 `MouseoverSounds.DoRegion(rect2, mouseoverSound)`。
4. 背景：`Widgets.DrawAtlas(rect, Widgets.ButtonSubtleAtlas)`，畫的時候套用上面的滑鼠顏色。
5. `highlight` 為真時，用灰色畫 2px 的框（`Widgets.DrawBox(rect, 2)`）。
6. `GUI.color = Color.white`。
7. `barPercent > 0.001` 時，在 `rect.ContractedBy(1)` 裡畫進度條（`Widgets.FillableBar`，用 `Widgets.ButtonBarTex`）。
8. **文字範圍**：
   - 從 `rect` 開始。
   - `textLeftMargin < 0` 時改成 `rect.width × 0.15`。
   - `x += textLeftMargin`，**寬度不變**，所以文字範圍的右緣會超出按鈕 `textLeftMargin` 那麼多。
   - 滑鼠在上面時，文字範圍再往右 2px、往上 2px。
9. **`Text.Anchor = TextAnchor.MiddleLeft`**、`Text.WordWrap = false`、`Text.Font = GameFont.Small`。
10. `GUI.color = labelColor ?? Color.white`，然後 `Widgets.Label(文字範圍, label)`。
11. 還原：`Text.Anchor = UpperLeft`、`Text.WordWrap = true`、`GUI.color = white`。字型**沒有**還原，留在 Small。
12. 回傳 `Widgets.ButtonInvisible(rect2, doMouseoverSound: false)`。

### 和雲端做法對照

雲端攔截 `ButtonTextSubtle`：Prefix 把 `label` 換成空字串，Postfix 用 `ButtonPainter.DrawAlignedLabel` 補畫。前提成立：原版 `DoButton` 的文字確實是由 `ButtonTextSubtle` 畫的，而有圖示的按鈕傳的本來就是空字串，所以只有圖示的按鈕不受影響。

和原版不同、可能要注意的地方：

- **滑鼠移上去時的位移**：原版文字在滑鼠移上去時會往右上移 2px（圖示也一樣）。`DrawAlignedLabel` 沒有這個位移，所以選了對齊之後，滑鼠移上去時背景和圖示有「按下」的感覺，文字沒有。
- **靠左的邊距**：原版是 2px（文字快塞滿時）或按鈕寬的 15%。雲端的「靠左」用 `labelSidePadding`，和原版的「照遊戲原本的」不會一樣，這是預期的。
- **置中、靠右**：原版的文字範圍右緣會超出按鈕。雲端用按鈕本身的範圍左右各扣 `labelSidePadding`，比原版合理，不用照抄原版的範圍。
- **字型**：`DrawAlignedLabel` 沒有設定字型，靠 `ButtonTextSubtle` 剛把字型設成 Small 而且沒有還原。目前沒問題；如果別的模組在中間改了字型，就會受影響。

給下一階段「文字／圖示／兩者」和自訂圖示的資訊：

- 原版是「有圖示就只畫圖示，沒有就只畫文字」，兩者不會同時出現。要做「兩者」，必須自己畫，或在 `ButtonTextSubtle` 的文字範圍和圖示位置上動手。
- 圖示固定 32×32、置中、滑鼠移上去時往右上移 2px。圖示讀的是 `def.Icon`。換自訂圖示可以改 `MainButtonDef.icon` 欄位（`Icon` getter 只在欄位是 null 時才從 `iconPath` 載入），或者自己畫。
- VTE 用 transpiler 改 `DoButton`、TD Enhancement Pack 在 `DoButton` 的 Postfix 畫研究指示（見上次的 Patches 診斷）。如果自己畫而不呼叫 `DoButton`，這些就不會生效。

## 3. Player.log

### 完整模組清單（2,201 個，2026-11-03 22:37～23:34，開發模式）

- 第 2889 行有 `[Main Button Bar System] Loaded.`。
- **本模組的紅字、黃字：0 筆。** 掃描方式和上一份報告一樣。
- 和主按鈕相關、但沒點名本模組的只有 1 筆，和上次相同：載入 Def 時的 `Tried to use an uninitialized DefOf of type KeyBindingDefOf`。它夾在一串別的模組的 DefOf 警告中間，沒有堆疊。
- `Tuning.xml`（`MainButtonBarTuningDef`）沒有任何 Config error。
- 翻譯：DefInjected 改名後，顯示方式名稱已經是中文（截圖 `screenshots/fixes_01_hotkey_conflict_false_positive.png` 左上：「折叠＋滚动」）。
- 開發模式的診斷：
  - 「Diagnostics (dev mode)」在 22:57:14，內容和上一份報告的 Stage 1 diagnostics 完全一樣：84 顆按鈕、13 顆有 KeyBindingDef、圖示 54/55 反查得到。
  - 「Patches on main button methods (dev mode)」在 23:16:08，和上次相同。
  - 診斷的方法清單沒有列 `Widgets.ButtonTextSubtle`。文字對齊現在補丁了這個方法，建議加進清單，才看得出有沒有別的模組也補丁它。
- 整份 log 的 ERROR 有 720 筆，都是別的模組。

### 18 個模組的測試排序（2026-11-03 23:37～23:43，英文介面，開發模式）

排序：Harmony、本體、5 個 DLC、本模組，再加 10 個會新增主按鈕的模組：Simple Warrants、Stuff List (Continued)、Dub's Paint Shop、Hospital、Rules of Engagement、故人归来、Animal Tab、Medical Tab - Continued、Mech Work Tab、Dubs Mint Menus。沒有 VTE、Bradson 的圖示包。使用者回報：**沒有問題**。

- **本模組的紅字、黃字：0 筆。** 整份 log 沒有任何 ERROR。
- 「null texture passed to GUI.DrawTexture」9,999 筆，洗到 log 上限。堆疊全部來自 Rules of Engagement 的 `AsuRim.Armoury.Patch_PlaySettings_GuardMarkers.Postfix`（右下角遊戲設定列的按鈕貼圖是 null），和本模組無關。
- `Key binding conflict: MainTab_MedicalTab and Misc7 are both bound to M`：原版 `KeyPrefsData.ErrorCheck` 在啟動時印出，是 Medical Tab 的 M 和原版 `Misc7` 撞鍵。這是原版認定的真衝突，可以拿來驗證本模組的撞鍵警告：選「醫療」時應該要顯示這一條，而建筑规划的 Tab 不應該顯示（見下面的誤報）。
- 截圖（英文介面）：
  - `screenshots/fixes_04_bar_en_buttons18.webp`：沒有捲動時的整條列。
  - `screenshots/fixes_05_bar_en_scroll_more.webp`：捲動模式，兩端有 `<` `>`，「More (7)」完整顯示，右箭頭和「More」之間沒有重疊。
  - `screenshots/fixes_06_drawer_en.png`：抽屜格子，文字和圖示都置中，下方是「Main button bar settings」。

## 4. 測試清單逐項結果

使用者在完整清單（2,201 個）和 18 個模組的排序各測了一輪，只測了一部分。「通過」只限於有截圖或使用者明確回報的部分。

| 步驟 | 項目 | 結果 | 依據／看到的情況 |
|---|---|---|---|
| 1 | 顯示方式名稱是中文 | 通過 | 截圖 fixes_01、fixes_03：「原版／折叠／滚动／折叠＋滚动」 |
| 2 | 捲動模式出現箭頭 | 通過 | 截圖 fixes_05 |
| 2 | 被箭頭蓋住的按鈕不反白、不跳提示、點不到；露出的部分能點 | 未測 | |
| 3 | 「更多（N）」完整顯示 | 通過 | 截圖 fixes_03「更多（7）」、fixes_05「More (7)」 |
| 3 | 「更多」寬度調小後按「預設值」回到自動 | 未測 | |
| 4 | 每個選項旁有「預設值」按鈕 | 部分通過 | 截圖 fixes_01、fixes_02（單顆按鈕）、fixes_03（整條列）都有。已經是預設值時沒有變灰（見下面的問題） |
| 5 | 「抽屜」欄按住拖過去連續勾選 | **不通過** | 整個視窗被拖走（見下面的問題） |
| 6 | 空白處能拖動視窗 | 可以拖，但**需求改了** | 使用者要的是只有標題列能拖（見下面的問題） |
| 7 | 文字對齊 | 未測 | 截圖 fixes_06 的抽屜格子是置中，看不出設定值 |
| 8 | 「按钮间距」滑桿沒有「0.000」 | 未測 | 進階的整條列面板沒有截圖 |
| — | 只有原版＋本模組的排序 | **不通過**（一項） | 抽屜只有 1 顆按鈕時，設定按鈕的文字塞不下（見下面的問題）；log 0 筆錯誤 |
| — | 18 個模組的排序 | 通過 | 使用者回報沒有問題；log 本模組 0 筆錯誤 |

### 只有原版＋本模組的排序（2026-11-03 23:49～23:50，8 個模組，英文介面）

- log：**0 筆 ERROR**，本模組沒有任何 WARN。其餘黃字都是啟動時掃描已安裝模組的 About 產生的（缺 downloadUrl 之類），和這份排序無關。
- **抽屜只有一欄時，設定按鈕的文字塞不下**（截圖 `screenshots/fixes_07_vanilla_drawer_settings_truncated.png`）：原版只有「文化」（Ideos）一顆收進抽屜，抽屜只有一欄寬，下方「Main button bar settings」兩邊都被切掉。原因是抽屜寬度只看格子數，沒有考慮設定按鈕的文字。英文比中文長，更容易發生。
- **使用者提議：設定按鈕改成齒輪圖示。** 建議：
  - 做成方形的齒輪圖示按鈕，滑鼠移上去顯示「主按钮栏设置」提示。這樣和語言、抽屜寬度都無關。
  - 齒輪貼圖照「不寫死」原則放在 Tuning，主題也可以換。
  - 如果還要保留文字版，抽屜的最小寬度至少要放得下設定按鈕的文字。

- **英文介面下，清單的「Drawer」欄標題沒對齊而且被切掉**（使用者回報，只有對話裡的截圖，沒有檔案）：
  - 欄標題顯示成「Drawe」，位置比旁邊的「From mod」高。
  - 原因推定：欄標題用 `TextAnchor.MiddleCenter` 畫在固定寬度的抽屜欄裡（`DrawList`），`Text.WordWrap` 還是預設的 true。英文「Drawer」比欄寬，被折成兩行，「r」跑到第二行被裁掉，第一行就往上偏。
  - 建議：欄寬至少取欄標題文字寬度（`Text.CalcSize`）和勾選框寬度的較大值；畫欄標題時 `WordWrap = false`。或者欄標題改成圖示，滑鼠移上去顯示文字。其他欄標題（Visible、Icon、Hotkey）在別的語言下也可能有同樣的問題。

- **看不太出來目前是「簡單」還是「進階」**（使用者回報，截圖 `screenshots/fixes_08_simple_advanced_toggle.png`）：兩顆按鈕長得幾乎一樣，選中的只有邊框稍微亮一點。建議改用原版視窗的分頁樣式（`TabDrawer.DrawTabs`／`TabRecord`，選中的分頁和下面的內容區連在一起，像研究、信仰視窗那樣）。如果維持按鈕，選中的要有明顯差異，例如填色、文字變亮，或未選中的變暗。之後加「外觀」分頁時也用同一套樣式。

### 給雲端的新需求：外觀分頁和主題（使用者 2026-11-03）

- 設定視窗最上面（「简单」「进阶」旁）新增「外觀」分頁，列出所有主題，附預覽，讓玩家挑。
- 主題可以由其他模組提供：以本模組為前置的主題模組，訂閱後就出現在外觀分頁。例如種族模組的作者可以照種族特徵做一套主題。
- **使用者（NAI）會自己提供幾款內建主題**，風格對應原版的幾個 DLC，讓其他作者有範例可以照著做。系統和範本由雲端做，圖由使用者提供。
- **只用 XML＋圖片就能做主題，不用寫 C#**：
  - 目前的 `BarAppearanceDef` 一定要有 `workerClass`。建議內建一個預設的繪製類別，直接讀 Def 上的貼圖欄位；`workerClass` 留給要做特效的進階作者。
  - 主題 Def 的欄位都可以不填，不填就用原版的。建議的欄位：
    - 整條列的背景
    - 按鈕三種狀態的背景（一般、滑鼠移上去、按下或已開啟）
    - 文字顏色、滑鼠移上去時的文字顏色
    - 「更多」按鈕和捲動箭頭的貼圖
    - 抽屜視窗的背景
    - 外觀分頁用的預覽圖
  - 按鈕背景要和原版 `Widgets.ButtonSubtleAtlas` 一樣用九宮格（`Widgets.DrawAtlas`），任何寬度都不會讓四角變形。`docs/API.md` 要附一份主題模組的範本（About、Defs、Textures 資料夾結構，貼圖尺寸和九宮格的切法）。
- 實作要注意：
  - 原版按鈕背景寫死在兩個地方：`Widgets.ButtonTextSubtle` 裡的 `DrawAtlas(rect, ButtonSubtleAtlas)`，以及 `MainButtonWorker.DoButton` 停用按鈕那段也直接畫 `ButtonSubtleAtlas`。要換主題就得兩處都攔截，例如用 transpiler 把讀 `Widgets.ButtonSubtleAtlas` 的地方換成讀目前主題的貼圖。
  - VTE 的 `DoButton` transpiler 和 TD Enhancement Pack 的 Postfix 是畫在背景上面的，不受影響。
  - 選了非原版主題，就等於「改過設定」，主按鈕列改由本模組畫。
- 還沒決定：主題跟著存檔走（和其他設定一樣），還是所有存檔共用。

### 測試中記下的問題

- **快捷鍵衝突警告是誤報，而且沒有說明**（使用者回報，截圖 `screenshots/fixes_01_hotkey_conflict_false_positive.png`）：
  - 選「建筑规划」（Architect，快捷鍵 Tab），右側出現紅字「和这些按键冲突：更改已选择的调试选项」。那是原版的 `Dev_ChangeSelectedDebugAction`，分類是 `DebugMenu`，`devModeOnly`，預設鍵也是 Tab。
  - **原版不把這兩個當成衝突**。`KeyPrefsData.ConflictingBindings(keyDef, code)` 對每個其他的 KeyBindingDef：
    - 跳過自己，也跳過 `keyDef.ignoreConflictsWith` 裡的。
    - 只有下面任一條件成立時才算同一個衝突範圍：
      - 同一分類，而且分類的 `selfConflicting` 為真。
      - `keyDef.category.checkForConflicts` 包含對方的分類（只看這一個方向）。
      - 兩邊的 `extraConflictTags` 有交集。
    - 然後才比對按鍵。
  - `MainTabs` 的 `checkForConflicts` 只有 Development、Game、GameMapCamera、SelectionMisc，沒有 DebugMenu，所以 Architect 的 Tab 不會和除錯選單的 Tab 衝突。原版的鍵盤設定畫面也不會標紅。
  - 本模組的 `Hotkeys.Conflicts` 拿 `KeyPrefsData.keyPrefs` 裡**所有**綁定比對，沒看分類，所以會誤報。建議：
    - 遊戲有註冊的快捷鍵，直接用 `KeyPrefs.KeyPrefsData.ConflictingBindings(def.hotKey, key)`。
    - 本模組自存的快捷鍵，當成 `MainTabs` 分類，套用同樣的規則。
  - **警告沒有說明**（使用者回報）。只寫「和这些按键冲突：X」，看不出 X 是什麼、在哪裡設定、撞了會怎樣、要怎麼解決。建議寫出對方所在的分類或模組（例如「除錯選單：更改已选择的调试选项」），說明撞鍵時哪一個會先觸發，並提示到「选项 → 键盘设置」改，或直接在這裡換一個鍵。
- **連續勾選沒有作用，按住左鍵拖過「抽屉」欄時整個視窗被拖走**（使用者回報，截圖 `screenshots/fixes_02_drag_only_title_bar.png`）。
  - **使用者要的是：只有最上面的標題列能拖動視窗**，就是截圖紅框那一條（標題「主按钮栏设置（当前存档）」和「简单／进阶」那一列）。清單、右側面板、底部按鈕之間都不要能拖動視窗，這樣才能按住左鍵往下拖，把很多顆按鈕一次收進抽屜。
  - 更正：上一份報告（`results-stage-1-3.md`）把「只有清單上方那一區能拖動視窗」和「不能拖過去連續勾選」寫成兩個獨立問題，是我理解錯了。使用者原本的意思是同一件事：要能拖過去連續勾選，而且視窗只從標題列拖。這一版照錯誤的寫法把可拖的範圍放大，反而讓連續勾選更做不到。
  - 建議：視窗設 `draggable = false`，只在標題列的範圍呼叫 `GUI.DragWindow(標題列 rect)`。
- **捲動模式下按鈕放得下時沒有箭頭，使用者以為捲動沒啟用**（18 個模組的測試排序，截圖 `screenshots/fixes_03_buttons_set_no_scroll.jpg`）：
  - 使用者問「是要超過一定數量才會出現捲動嗎」。照 `BarLayout.LayoutNatural`，不是看數量，是看寬度。每顆的自然寬度是：單顆寬度；沒設單顆寬度就用預設寬度，半格按鈕減半；至少要有最小寬度。全部加上間距後，如果超過可用寬度（畫面寬扣掉「更多」、菜单、固定在兩端的按鈕），才會出現箭頭。放得下而且開著「填滿畫面」時，按鈕會被拉寬鋪滿。
  - 這次畫面寬 2000px，列上約 13 顆，預設寬度 100、最小寬度 80，放得下，所以沒有箭頭。這是照設計的行為。
  - 建議在「捲動」「摺疊＋捲動」的選項說明，或「最小寬度」滑桿旁加一句「按鈕放得下時不會出現箭頭」。簡單模式只有最小寬度可調，玩家不容易聯想到。
  - 同一張截圖：「更多（7）」完整顯示，括號沒有被截斷。
- **「默认值」按鈕好像沒有變灰**（同一張截圖，待確認）：建筑规划的各項都還是預設值（顯示、不收抽屜、名稱空白、寬度跟随整条栏、不固定、不強制顯示），但每個「默认值」按鈕的顏色看起來都和「设置」「清除」一樣。清單寫「已經是預設值時按鈕是灰的」，截圖看不出灰色。
