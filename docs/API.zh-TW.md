# Main Button Bar System — 附屬模組作者指南（繁體中文節錄）

完整版是英文的 [API.md](API.md)。這份只翻譯主題（外觀）那一節，以及「軟性依賴」「API 穩定性」兩段；其他接口（按鈕預設值、排序分享、圖示包、顯示方式、相容橋接、查詢 API）請看英文版。

## 4. 主題（外觀）：只要 XML 和圖片

主題改變底部列和按鈕的外觀。玩家在設定視窗的**外觀**分頁選主題，那裡會列出所有已載入模組的 `BarAppearanceDef`，附預覽。主題不需要 C#：每個欄位都是選填，沒填的部分維持遊戲原本的樣子。

主題不會取代遊戲畫按鈕的程式：按鈕仍由 `MainButtonWorker.DoButton` 畫，所以其他模組對它的修改（圖示、研究進度、教學提示）都還有效。本模組畫底部列的按鈕時，只是把遊戲的按鈕 atlas 換成主題該狀態的 atlas，並設定文字顏色。

### 主題模組的檔案配置

```
MyTheme/
  About/About.xml
  Defs/MyTheme_Themes.xml
  Textures/MyTheme/
    Button.png            一般狀態的按鈕（九宮格 atlas）
    ButtonMouseover.png   滑鼠移上
    ButtonActive.png      按下，或它的分頁開著
    ButtonDisabled.png    停用（沒有地圖等）
    Bar.png               整條列的背景（atlas，或整張圖，見下面「插圖」）
    More.png              「更多」按鈕（atlas）
    ArrowLeft.png         左捲動箭頭的圖示
    ArrowRight.png        右捲動箭頭的圖示
    Drawer.png            「更多」抽屜的背景（atlas，或整張圖）
    Gear.png              抽屜的設定按鈕（圖示）
    Preview.png           外觀分頁的預覽
```

`About/About.xml`：

```xml
<ModMetaData>
  <name>My Theme</name>
  <author>Me</author>
  <packageId>me.mytheme</packageId>
  <supportedVersions><li>1.7</li></supportedVersions>
  <modDependencies>
    <li>
      <packageId>NAI.MainButtonBarSystem</packageId>
      <displayName>Main Button Bar System</displayName>
    </li>
  </modDependencies>
  <loadAfter><li>NAI.MainButtonBarSystem</li></loadAfter>
</ModMetaData>
```

`Defs/MyTheme_Themes.xml`（貼圖路徑相對於 `Textures/`，不含副檔名）：

```xml
<Defs>
  <MainButtonBarSystem.BarAppearanceDef>
    <defName>MyTheme_Steel</defName>
    <label>steel</label>
    <description>Brushed steel buttons with amber labels.</description>
    <author>Me</author>
    <displayOrder>100</displayOrder>

    <button>MyTheme/Button</button>
    <buttonMouseover>MyTheme/ButtonMouseover</buttonMouseover>
    <buttonActive>MyTheme/ButtonActive</buttonActive>
    <buttonDisabled>MyTheme/ButtonDisabled</buttonDisabled>
    <labelColor>(1, 0.8, 0.4, 1)</labelColor>
    <labelMouseoverColor>(1, 0.95, 0.7, 1)</labelMouseoverColor>

    <barBackground>MyTheme/Bar</barBackground>
    <moreButton>MyTheme/More</moreButton>
    <arrowLeft>MyTheme/ArrowLeft</arrowLeft>
    <arrowRight>MyTheme/ArrowRight</arrowRight>
    <drawerBackground>MyTheme/Drawer</drawerBackground>
    <drawerSettingsIcon>MyTheme/Gear</drawerSettingsIcon>
    <preview>MyTheme/Preview</preview>
  </MainButtonBarSystem.BarAppearanceDef>
</Defs>
```

| 欄位 | 省略時 |
|---|---|
| `button` | 遊戲的按鈕 atlas |
| `buttonMouseover` | 一般 atlas 加上遊戲的滑鼠移上色調 |
| `buttonActive` | `buttonMouseover`，再不然一般 atlas |
| `buttonDisabled` | 一般 atlas |
| `labelColor` | 白色。顏色寫成 `(r, g, b, a)`，0～1；alpha 為 0 也算「省略」 |
| `labelMouseoverColor` | `labelColor` |
| `barBackground` | 底部列後面什麼都不畫（原版） |
| `moreButton` | 和其他按鈕畫法相同 |
| `arrowLeft`、`arrowRight` | 按鈕上的 `<` 和 `>` 字元 |
| `drawerBackground` | 遊戲的視窗背景 |
| `drawerSettingsIcon` | 本模組的齒輪（`MainButtonBarSystem/UI/Gear`，在 tuning def 裡設定） |
| `preview` | 用上面的貼圖畫出來的預覽 |
| `labelActiveColor`（1.1） | 按下或分頁開著時用 `labelMouseoverColor` |
| `barBackgroundFit`、`drawerBackgroundFit`（1.1） | `Atlas`：背景是九宮格 atlas。見下面「插圖」 |
| `decorations`（1.1） | 沒有額外的圖。見下面「插圖」 |

**Atlas** 用遊戲的 `Widgets.DrawAtlas` 畫，也就是原版按鈕的九宮格方式，一張貼圖適用任何按鈕大小：

- 貼圖切成四等分：每個角是外側 25%×25%，每條邊是外側 25% 乘中段 50%，中央是中段 50%×50%。邊和中央是**拉伸**，不是平鋪。
- 畫面上每個角是**貼圖寬度 ÷ 4** 像素（UI 單位），但不會超過矩形高或寬的一半。只看寬度，所以**貼圖必須是正方形**，否則角會被垂直壓扁。
- 所以貼圖大小決定邊框粗細：64×64 的貼圖給 16 像素的角。底部列的按鈕高 36 像素，角最多長到 18 像素：超過 **72×72** 的貼圖只會被縮小。
- 邊框和角的圖案都要畫在每邊外側四分之一以內；中段的東西會被拉伸。
- 圖怎麼畫就怎麼顯示（上面是上面）。

找不到的貼圖會在 log 報一次，那個部分退回遊戲原本的樣子。

文字顏色、滑鼠移上和按下狀態也用在「更多」按鈕、捲動箭頭和抽屜上。`label` 和 `description` 的翻譯放在 `Languages/<語言>/DefInjected/MainButtonBarSystem.BarAppearanceDef/`。

### 插圖：整張圖與裝飾圖（API 1.1）

Atlas 會拉伸中段，所以一張畫不能當 atlas 用。三個選填的新功能讓主題不用 C# 就能放真正的插圖；沒用到的主題外觀和以前完全一樣。

**背景的繪製方式。** `barBackgroundFit` 和 `drawerBackgroundFit` 決定 `barBackground`／`drawerBackground` 怎麼填滿區域：

| 值 | 畫法 |
|---|---|
| `Atlas` | 九宮格，同上（預設） |
| `Tile` | 從左上角以貼圖原尺寸乘 `barBackgroundTileScale`／`drawerBackgroundTileScale`（預設 1）平鋪，超出的裁掉 |
| `Stretch` | 拉伸填滿，不管比例：給底部列畫一張寬圖時通常選這個 |
| `Fit` | 保持比例縮放到完全放得進區域，置中，剩下的留透明 |
| `Cover` | 保持比例縮放到蓋滿區域，置中，超出的裁掉 |

底部列是螢幕寬 × 36 像素（tuning def 的 `behaviour.buttonRectHeight`），所以給 `Stretch` 用的圖應該是寬的、36 像素高。抽屜的大小看內容而定。

**裝飾圖。** `decorations` 是一串放在底部列或抽屜某個錨點上的圖：

```xml
<decorations>
  <li>
    <texture>MyTheme/Portrait</texture>       <!-- 必填；相對於 Textures/，不含副檔名 -->
    <target>Bar</target>                      <!-- Bar（預設）／Drawer -->
    <anchor>Left</anchor>                     <!-- Left（預設）／Center／Right -->
    <verticalAnchor>Bottom</verticalAnchor>   <!-- Bottom（預設）／Center／Top -->
    <offset>(0, 0)</offset>                   <!-- 像素；x 往右、y 往下 -->
    <size>(0, 0)</size>                       <!-- 像素；(0, 0)：貼圖原尺寸；只有一邊是 0：另一邊依比例 -->
    <opacity>1</opacity>                      <!-- 0～1 -->
    <layer>BelowButtons</layer>               <!-- BelowButtons（預設）／AboveButtons -->
    <showWhen><li>Mouseover</li></showWhen>   <!-- 省略或空：永遠顯示；Normal／Mouseover／DrawerOpen，任一成立就顯示 -->
  </li>
</decorations>
```

擺放：先依 `size` 決定方框大小，再把方框的錨點角貼到目標區域對應的邊（`Right`＋`Bottom` 就是右下角對右下角），最後加上 `offset`。底部列只有 36 像素高，所以錨在底邊的立繪會往上長出列外，這就是設計用途。底部列的裝飾在底部列自己的繪製階段畫，長出列外的部分會蓋在地圖上、壓在視窗下面。抽屜是視窗，超出視窗的部分會被裁掉。

狀態：`Mouseover` 是滑鼠在目標區域本身上（底部列的 36 像素矩形，不含裝飾圖長出去的部分；抽屜視窗）。`DrawerOpen` 是「更多」抽屜開著；抽屜的裝飾圖永遠不會成立（它們本來就只在抽屜開著時畫），所以抽屜裝飾用 `Mouseover`、`Normal` 或不填。`Normal` 是另外兩個都不成立。

玩家可以在外觀分頁調整裝飾圖，疊加在主題自己的值上：全部顯示或隱藏、不透明度、大小（以同一個角為基準）、X／Y 位移，以及背景圖的不透明度。每一列都有「預設值」按鈕；預設和滑桿範圍在 tuning def（`barDefaults`、`ranges`）。

**按下時的文字顏色。** `labelActiveColor` 用在按鈕按下或分頁開著時；省略時照舊用滑鼠移上顏色，再不然 `labelColor`。

外觀分頁自己畫的預覽只有背景圖和按鈕，不畫裝飾圖（會長出卡片外），所以有裝飾圖的主題請附 `preview` 圖。

### 檢查與重新載入（開發者模式）

開著開發者模式時，本模組啟動時會檢查每個主題，每個問題記一筆警告，寫明主題和欄位：讀不到的貼圖、不是正方形的 atlas、超過 72×72 的 atlas（`behaviour.themeAtlasMaxSize`）、單邊超過 2048 的裝飾圖或背景圖（`behaviour.themeDecorationMaxSize`）。關掉開發者模式什麼都不會記；缺貼圖只會退回遊戲原本的樣子。

開發者模式下外觀分頁多一顆**重新載入主題貼圖**按鈕：從各主題自己的模組資料夾重新讀 PNG，改過的圖不用重開遊戲就會顯示，然後再檢查一次。重載過的貼圖不會釋放，所以同一次遊戲重載很多次會多佔一點記憶體。

### 做一個主題模組

- `docs/examples/ExampleTheme/`（發佈版在 `ThemeTemplate/ExampleTheme/`）是用到全部欄位的完整主題模組，圖是佔位圖：複製、改名、換圖就能用。
- `tools/make_theme.py`（發佈版在 `ThemeTemplate/tools/`）從一份 JSON 配色畫出十張標準貼圖，並寫出整個主題模組（含佔位圖）：`python -I -X utf8 make_theme.py MyTheme.json <輸出資料夾>`。範例的 JSON `ExampleTheme.json` 列出每個設定。需要 Python 3 和 Pillow。
- 上工作坊時，把 **Main Button Bar System** 加進項目的 *Required items*：About.xml 的 `modDependencies` 只有遊戲看得到，Steam 不會。`loadAfter` 照範例寫。

### 相容承諾

主題欄位只會新增，不會移除或改變意義：照 API 1.0 寫的主題在之後的每個 1.x 都能載入、外觀相同。本模組不再讀的欄位會保留一個版本，並在文件標明。新增欄位會提高 `MainButtonBarAPI.ApiVersion` 的次版本號（1.1 加了插圖欄位）。

### 內建主題就是範例

本模組內建六款照這個方式做的主題，Def 在 `Defs/MainButtonBarSystem/BarAppearances.xml`，貼圖在 `Textures/MainButtonBarSystem/Themes/`：`MBBS_Imperial`、`MBBS_Creed`、`MBBS_GeneLab`、`MBBS_Void`、`MBBS_Starfarer` 只用 atlas；`MBBS_Nai` 是插圖欄位的範例（拉伸的底部列背景、按鈕後面的立繪、滑鼠移上才出現的標記、抽屜裡的徽章、`labelActiveColor`）。貼圖由 `tools/gen_theme_textures.py` 從 `tools/themes/<主題>.json` 產生，用的就是主題模組用的那支 `make_theme.py`。

### 貼圖以外的效果

把 `workerClass` 設成繼承 `BarAppearanceWorker` 的類別，覆寫需要的方法：`DrawBarBackground`、`DrawBarBackgroundImage`、`DrawBarOverlay`、`DrawButton`、`DrawDrawerButton`、`DrawDrawerBackground`、`DrawDrawerBackgroundImage`、`DrawDrawerOverlay`、`DrawDecorations`、`DrawerSettingsIcon`、`ButtonAtlas`、`LabelColor`、`DrawMoreButton`、`MoreButtonWidth`、`DrawScrollArrow`。`DrawBarBackground` 是背景圖加按鈕下層的裝飾；`DrawBarOverlay` 在每種顯示方式畫完之後呼叫，所以按鈕上層的裝飾對任何顯示方式都有效。畫按鈕請繼續走 `ButtonPainter.Draw`（或遊戲的 `DoButton`），其他模組對按鈕的修改才會保留。

## 軟性依賴本模組

大部分附屬模組不管有沒有 Main Button Bar System 都應該能用。常見做法：

- **XML 的清單項目**：`MayRequire` 會在本模組不在時略過那一項。
  ```xml
  <modExtensions>
    <li Class="MainButtonBarSystem.MainButtonBarDefaults" MayRequire="NAI.MainButtonBarSystem">
      <inDrawer>true</inDrawer>
    </li>
  </modExtensions>
  ```
  （`modExtensions` 裡帶 `Class=` 的項目在 1.7 還沒驗證過；如果報錯，改用下一個做法。）
- **XML 補丁**：把 `PatchOperationAddModExtension` 包在 `PatchOperationFindMod` 裡。注意 FindMod 比對的是模組**名稱** `Main Button Bar System`，不是 packageId。
- **C#**：把會碰本模組的程式放在另一個組件，用 `LoadFolders.xml` 只在本模組啟用時載入：`<li IfModActive="NAI.MainButtonBarSystem">MBBS/</li>`。或者呼叫前先檢查 `ModsConfig.IsActive("NAI.MainButtonBarSystem")`。
- `About.xml` 裡，硬性依賴寫在 `modDependencies` 加 `loadAfter`。RimSort 把 `*ByVersion` 標籤當成取代、不是補充，所以用了它們的話要重複寫一份。

主題模組一律是硬性依賴（沒有本模組就沒有人讀 `BarAppearanceDef`），照上面 About.xml 的寫法即可。

## API 穩定性

`MainButtonBarAPI.ApiVersion`（屬性，目前 `1.1`）涵蓋英文文件裡的所有東西：五個接口的 Def 欄位和 worker、`MainButtonBarDefaults`、`MainButtonArrangementDef`、`BarDrawContext`、`BarButton`、`BarDecoration`、`DecorationLayout`、`ButtonPainter.Draw` 和 `MainButtonBarAPI`。新增提高次版本號；移除或改變意義提高主版本號，而且被移除的成員會先標 `[Obsolete]` 保留一個版本。排序檔有自己的 `formatVersion`。沒列出來的都是內部實作，可能會變。
