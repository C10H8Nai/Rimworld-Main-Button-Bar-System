# Main Button Bar System — 附属模组作者指南（简体中文节选）

完整版是英文的 [API.md](API.md)。这份只翻译主题（外观）那一节，以及“软性依赖”“API 稳定性”两段；其他接口（按钮默认值、排序分享、图标包、显示方式、兼容桥接、查询 API）请看英文版。

## 4. 主题（外观）：只要 XML 和图片

主题改变底部栏和按钮的外观。玩家在设置窗口的**外观**标签页选主题，那里会列出所有已加载模组的 `BarAppearanceDef`，附预览。主题不需要 C#：每个字段都是选填，没填的部分保持游戏原本的样子。

主题不会取代游戏绘制按钮的代码：按钮仍由 `MainButtonWorker.DoButton` 绘制，所以其他模组对它的修改（图标、研究进度、教学提示）都还有效。本模组绘制底部栏的按钮时，只是把游戏的按钮 atlas 换成主题该状态的 atlas，并设置文字颜色。

### 主题模组的文件布局

```
MyTheme/
  About/About.xml
  Defs/MyTheme_Themes.xml
  Textures/MyTheme/
    Button.png            一般状态的按钮（九宫格 atlas）
    ButtonMouseover.png   鼠标移上
    ButtonActive.png      按下，或它的标签页打开着
    ButtonDisabled.png    停用（没有地图等）
    Bar.png               整条栏的背景（atlas，或整张图，见下面“插图”）
    More.png              “更多”按钮（atlas）
    ArrowLeft.png         左滚动箭头的图标
    ArrowRight.png        右滚动箭头的图标
    Drawer.png            “更多”抽屉的背景（atlas，或整张图）
    Gear.png              抽屉的设置按钮（图标）
    Preview.png           外观标签页的预览
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

`Defs/MyTheme_Themes.xml`（贴图路径相对于 `Textures/`，不含扩展名）：

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

| 字段 | 省略时 |
|---|---|
| `button` | 游戏的按钮 atlas |
| `buttonMouseover` | 一般 atlas 加上游戏的鼠标移上色调 |
| `buttonActive` | `buttonMouseover`，再不然一般 atlas |
| `buttonDisabled` | 一般 atlas |
| `labelColor` | 白色。颜色写成 `(r, g, b, a)`，0～1；alpha 为 0 也算“省略” |
| `labelMouseoverColor` | `labelColor` |
| `barBackground` | 底部栏后面什么都不画（原版） |
| `moreButton` | 和其他按钮画法相同 |
| `arrowLeft`、`arrowRight` | 按钮上的 `<` 和 `>` 字符 |
| `drawerBackground` | 游戏的窗口背景 |
| `drawerSettingsIcon` | 本模组的齿轮（`MainButtonBarSystem/UI/Gear`，在 tuning def 里设置） |
| `preview` | 用上面的贴图画出来的预览 |
| `labelActiveColor`（1.1） | 按下或标签页打开着时用 `labelMouseoverColor` |
| `barBackgroundFit`、`drawerBackgroundFit`（1.1） | `Atlas`：背景是九宫格 atlas。见下面“插图” |
| `decorations`（1.1） | 没有额外的图。见下面“插图” |
| `buttonImage`、`buttonImages`（1.1） | 按钮里没有图。见下面“按钮里的图” |

**Atlas** 用游戏的 `Widgets.DrawAtlas` 绘制，也就是原版按钮的九宫格方式，一张贴图适用任何按钮大小：

- 贴图切成四等分：每个角是外侧 25%×25%，每条边是外侧 25% 乘中段 50%，中央是中段 50%×50%。边和中央是**拉伸**，不是平铺。
- 屏幕上每个角是**贴图宽度 ÷ 4** 像素（UI 单位），但不会超过矩形高或宽的一半。只看宽度，所以**贴图必须是正方形**，否则角会被垂直压扁。
- 所以贴图大小决定边框粗细：64×64 的贴图给 16 像素的角。底部栏的按钮高 36 像素，角最多长到 18 像素：超过 **72×72** 的贴图只会被缩小。
- 边框和角的图案都要画在每边外侧四分之一以内；中段的东西会被拉伸。
- 图怎么画就怎么显示（上面是上面）。

找不到的贴图会在日志里报一次，那个部分回退到游戏原本的样子。

文字颜色、鼠标移上和按下状态也用在“更多”按钮、滚动箭头和抽屉上。`label` 和 `description` 的翻译放在 `Languages/<语言>/DefInjected/MainButtonBarSystem.BarAppearanceDef/`。

### 插图：整张图与装饰图（API 1.1）

Atlas 会拉伸中段，所以一幅画不能当 atlas 用。四个选填的新功能让主题不用 C# 就能放真正的插图；没用到的主题外观和以前完全一样。

**背景的绘制方式。** `barBackgroundFit` 和 `drawerBackgroundFit` 决定 `barBackground`／`drawerBackground` 怎么填满区域：

| 值 | 画法 |
|---|---|
| `Atlas` | 九宫格，同上（默认） |
| `Tile` | 从左上角以贴图原尺寸乘 `barBackgroundTileScale`／`drawerBackgroundTileScale`（默认 1）平铺，超出的裁掉 |
| `Stretch` | 拉伸填满，不管比例：给底部栏画一张宽图时通常选这个 |
| `Fit` | 保持比例缩放到完全放得进区域，居中，剩下的留透明 |
| `Cover` | 保持比例缩放到盖满区域，居中，超出的裁掉 |

底部栏是屏幕宽 × 36 像素（tuning def 的 `behaviour.buttonRectHeight`），所以给 `Stretch` 用的图应该是宽的、36 像素高。抽屉的大小看内容而定。

**装饰图。** `decorations` 是一串放在底部栏或抽屉某个锚点上的图：

```xml
<decorations>
  <li>
    <texture>MyTheme/Portrait</texture>       <!-- 必填；相对于 Textures/，不含扩展名 -->
    <target>Bar</target>                      <!-- Bar（默认）／Drawer -->
    <anchor>Left</anchor>                     <!-- Left（默认）／Center／Right -->
    <verticalAnchor>Bottom</verticalAnchor>   <!-- Bottom（默认）／Center／Top -->
    <offset>(0, 0)</offset>                   <!-- 像素；x 往右、y 往下 -->
    <size>(0, 0)</size>                       <!-- 像素；(0, 0)：贴图原尺寸；只有一边是 0：另一边按比例 -->
    <opacity>1</opacity>                      <!-- 0～1 -->
    <layer>BelowButtons</layer>               <!-- BelowButtons（默认）／AboveButtons -->
    <showWhen><li>Mouseover</li></showWhen>   <!-- 省略或空：永远显示；Normal／Mouseover／DrawerOpen，任一成立就显示 -->
  </li>
</decorations>
```

摆放：先按 `size` 决定方框大小，再把方框的锚点角贴到目标区域对应的边（`Right`＋`Bottom` 就是右下角对右下角），最后加上 `offset`。底部栏只有 36 像素高，所以锚在底边的立绘会往上长出栏外，这就是设计用途。底部栏的装饰在底部栏自己的绘制阶段绘制，长出栏外的部分会盖在地图上、压在窗口下面。抽屉是窗口，超出窗口的部分会被裁掉。

状态：`Mouseover` 是鼠标在目标区域本身上（底部栏的 36 像素矩形，不含装饰图长出去的部分；抽屉窗口）。`DrawerOpen` 是“更多”抽屉打开着；抽屉的装饰图永远不会成立（它们本来就只在抽屉打开时绘制），所以抽屉装饰用 `Mouseover`、`Normal` 或不填。`Normal` 是另外两个都不成立。

玩家可以在外观标签页调整装饰图，叠加在主题自己的值上：全部显示或隐藏、不透明度、大小（以同一个角为基准）、X／Y 偏移，以及背景图的不透明度。每一行都有“默认值”按钮；默认值和滑块范围在 tuning def（`barDefaults`、`ranges`）。

**按下时的文字颜色。** `labelActiveColor` 用在按钮按下或标签页打开着时；省略时照旧用鼠标移上颜色，再不然 `labelColor`。

**按钮里的图。** `buttonImage` 在每个主按钮里放一张图；`buttonImages` 给指定的按钮各自的图（“这个按钮是这个人物、那个按钮是那个人物”）。两个都是选填，没有自己的图的按钮用 `buttonImage`，再不然就没有图：

```xml
<buttonImage>                                 <!-- 每个主按钮；省略就没有 -->
  <texture>MyTheme/Buttons/Default</texture>  <!-- 必填；任意分辨率 -->
</buttonImage>
<buttonImages>
  <li>
    <button>Architect</button>                <!-- 必填：MainButtonDef 的 defName（Architect、Work、Research、模组的按钮） -->
    <texture>MyTheme/Buttons/Alice</texture>  <!-- 必填 -->
    <fit>Cover</fit>                          <!-- Cover（默认）／Fit／Stretch；Atlas、Tile 视同 Stretch -->
    <anchor>Center</anchor>                   <!-- Left／Center（默认）／Right -->
    <verticalAnchor>Top</verticalAnchor>      <!-- Top／Center（默认）／Bottom -->
    <opacity>1</opacity>                      <!-- 0～1 -->
    <inset>2</inset>                          <!-- 按钮边缘往内留几像素；默认 0 -->
  </li>
</buttonImages>
```

`Cover` 保持比例缩放到盖满按钮，超出的裁掉；锚点决定保留哪一侧（`verticalAnchor` 设 `Top` 保留上缘，脸就不会被裁掉）。`Fit` 显示整张图，放在锚点那一侧；`Stretch` 拉伸填满、会变形。同一个按钮写两次时，后面的赢。

绘制顺序（底部栏的每个按钮、“更多”抽屉的每一格，所有状态都一样，停用也画）：先画图，再画按钮 atlas，最后画文字和图标。所以 atlas 是图的框：按钮 atlas 要画成中间透明的框（`make_theme.py` 的 palette 设 `"hollow": true`；或设 `"frame": "vanilla"`，画原版风格的中空框，原版的按钮底色改由底部栏背景提供），不然图会被盖住。鼠标移上和按下的效果留在框上，图本身不会被染色。主题没有自己的按钮 atlas（`button`）时，图会压在游戏的按钮底下；游戏的按钮底是不透明的，主题没给 atlas 的状态下图完全看不到，开发人员模式的检查会为这种主题报一条黄字。“更多”按钮和滚动箭头不会有图。

实用建议：

- 底部栏的按钮高 36 像素，宽通常 33～120 像素（按钮少或调宽时更宽）。完整的立绘只会露出一条：先裁成脸在中间的横幅，或用 `Cover` 加 `verticalAnchor` 设 `Top`（或 `Center`）。
- 文字会压在图上。重要的部分避开正中间、选看得清楚的文字颜色（`labelColor`），或请玩家把那些按钮设成“只显示图标”。
- 用 `inset` 让图留在框的边线以内。
- 找不到的按钮图什么都不画（只报一次，见下节），按钮仍然有框。开发人员模式的检查也会指出 `buttonImages` 里写了没加载的按钮（打错字，或那个模组没启用），以及同一个按钮写了两次。
- 玩家的装饰图设置（显示、不透明度、大小、偏移）和背景不透明度不影响按钮图。
- 用 `make_theme.py` 的 `hollow` 或 `"frame": "vanilla"` 时，滚动箭头也是中空的（它们用 `button` atlas），会透出底部栏背景，而且不会有图；“更多”按钮照旧有底色。

外观标签页自己画的预览会画背景图、按钮和按钮里的图，不画装饰图（会长出卡片外），所以有装饰图的主题请附 `preview` 图。

### 检查与重新加载（开发人员模式）

开着开发人员模式时，本模组启动时会检查每个主题，每个问题记一条警告，写明主题和字段：读不到的贴图、不是正方形的 atlas、超过 72×72 的 atlas（`behaviour.themeAtlasMaxSize`）、单边超过 2048 的装饰图、背景图或按钮图（`behaviour.themeDecorationMaxSize`）。缺贴图整个游戏过程只会记一次：开发人员模式由这个检查记，否则由主题第一次画到它时的那一行警告记（和以前一样），然后回退到游戏原本的样子。XML 里不合法的值（`opacity` 不在 0～1、`size` 或 `inset` 为负、tile scale 为 0、`texture` 空白、`buttonImages` 的条目没写 `button`）是配置错误，和其他 def 错误一样显示。`Tile` 背景需要的格数超过 `behaviour.themeTileMaxCount`（4096）时改成拉伸。

开发人员模式下外观标签页多一个**重新加载主题贴图**按钮：从各主题自己的模组文件夹重新读 PNG，改过的图不用重启游戏就会显示，然后再检查一次。上一次重新加载读进来的贴图会在下一次重新加载时释放。

### 做一个主题模组

- `docs/examples/ExampleTheme/`（发布版在 `ThemeTemplate/ExampleTheme/`）是用到全部字段的完整主题模组，图是占位图：复制、改名、换图就能用。
- `tools/make_theme.py`（发布版在 `ThemeTemplate/tools/`）从一份 JSON 配色画出十张标准贴图，并写出整个主题模组（含占位图）：`python -I -X utf8 make_theme.py MyTheme.json <输出文件夹>`。示例的 JSON `ExampleTheme.json` 列出每个设置。需要 Python 3 和 Pillow。
- 上传创意工坊时，把 **Main Button Bar System** 加进条目的 *Required items*：About.xml 的 `modDependencies` 只有游戏看得到，Steam 不会。`loadAfter` 照示例写。

### 在游戏里新建主题

不用任何工具的做法：外观标签页的主题列表最后有**新建主题…**。给画图的人的分步教程（附九宫格示意图）在同文件夹的 `ThemeGuide.zh-CN.md`（另有英文 `ThemeGuide.md`、繁中 `ThemeGuide.zh-TW.md`、日文 `ThemeGuide.ja.md`）。

1. 填名称（必填）、作者、说明。packageId 由它们生成（`<作者>.mbbstheme.<名称>`，撞到已有的模组会加数字），可以改。
2. 本模组在游戏的 `Mods` 文件夹写出一个完整的主题模组并选中它，不用重启：底部栏变成空白主题（上色九宫格框、栏上的格线、每个按钮里的准心插图）。
3. 选中它的卡片后按**打开文件夹**，用任何绘图软件改 PNG 保存。外观标签页打开着时每秒（`behaviour.themeProjectPollSeconds`）检查一次文件，有变就重新读；**重新加载**是马上读。还在写入的文件会保留上一个能用的版本。
4. 做好后在模组列表启用它，就和一般主题模组一样（也可以从那里上传创意工坊）。启用后由游戏加载，重新加载只会重读 PNG，改 Defs 要重启游戏。
5. **删除**是把文件夹移到游戏存档文件夹的 `MainButtonBarSystem/DeletedThemes/<文件夹>_<日期>`（移回 `Mods` 就恢复）。只有用“新建主题”做的主题能删，而且要在模组列表没勾选时。

`Textures/<Id>/` 的空白图：按钮四种状态、More、Drawer（64×64 九宫格）、Bar（256×36，Tile）、ButtonPicture（240×72）、左右箭头（32×32）、Gear（64×64）、Preview（600×36）；根目录的 `_Guide_<语言>.png`（英、繁中、简中、日各一张）是给第一次做主题的人的一图流说明书，也标出每个文件画在栏的哪里。36px 高的按钮上，64×64 atlas 的上下边各占 16px，所以空白框除了外圈细线都是半透明的。

还没启用时本模组用游戏自己的 Def 解析读它，所以本节的所有字段都能用，包括自己手动加的 `buttonImages`、`decorations`（图放在 `Textures/<Id>/`），也可以在 Defs 里多加几款 `BarAppearanceDef`。启用前的限制：只读 `.png`、不支持 `ParentName`、没有翻译（`Languages/`）、不认识的字段以警告略过。

- **复制项目**：副本要改自己的 packageId（About.xml 和 `MBBS_ThemeProject.xml`）和 defName，不然那款主题会因 defName 撞名被略过（一行警告）。
- **上传创意工坊**：照“做一个主题模组”把本模组加进 *Required items*。`_Guide_*.png`、`MBBS_ThemeProject.xml` 可以留着，游戏不读；不想让本模组再提供重新加载和删除，就删掉 `MBBS_ThemeProject.xml`。
- **并进其他模组**（例如种族模组自带主题）：把 `Defs/`、`Textures/` 复制到那个模组只在本模组启用时才读的文件夹（`IfModActive`，见英文版〈Shipping content inside another mod〉），再删掉这个项目。

### 兼容承诺

主题字段只会新增，不会移除或改变含义：照 API 1.0 写的主题在之后的每个 1.x 都能加载、外观相同。本模组不再读的字段会保留一个版本，并在文档标明。新增字段会提高 `MainButtonBarAPI.ApiVersion` 的次版本号（1.1 加了插图字段）。

### 内置主题就是示例

本模组内置六款照这个方式做的主题，Def 在 `Defs/MainButtonBarSystem/BarAppearances.xml`，贴图在 `Textures/MainButtonBarSystem/Themes/`：`MBBS_Imperial`、`MBBS_Creed`、`MBBS_GeneLab`、`MBBS_Void`、`MBBS_Starfarer` 只用 atlas；`MBBS_Nai` 在每个主按钮里放一张像素图（共用的 `buttonImage` 加 77 条 `buttonImages`，图在 `tools/themes/Nai/Buttons/`），配原版风格的中空框，另外设了 `labelActiveColor`。其他插图字段看 `docs/examples/ExampleTheme`：拉伸的底部栏背景、鼠标移上或抽屉里才出现的装饰图、配中空框的按钮图。贴图由 `tools/gen_theme_textures.py` 从 `tools/themes/<主题>.json` 生成，用的就是主题模组用的那支 `make_theme.py`。

### 贴图以外的效果

把 `workerClass` 设成继承 `BarAppearanceWorker` 的类，重写需要的方法：`DrawBarBackground`、`DrawBarBackgroundImage(Rect, float opacity)`、`DrawBarOverlay`、`DrawButton`、`DrawDrawerButton`、`DrawDrawerBackground`、`DrawDrawerBackgroundImage`、`DrawDrawerOverlay`、`DrawDecorations`、`DrawerSettingsIcon`、`ButtonAtlas`、`ButtonImage`、`DrawButtonImage`、`LabelColor`、`DrawMoreButton`、`MoreButtonWidth`、`DrawScrollArrow`。主题只有在 Def 至少设了一个按钮字段（按钮 atlas、文字颜色、`buttonImage` 或 `buttonImages`）时才会套用到按钮里（atlas、文字颜色、按钮图），所以在代码里决定按钮图的 worker，XML 里仍要有其中一个字段。要重写的是双参数的 `DrawBarBackgroundImage`：单参数版只是带着玩家的不透明度转调用它，外观标签页的预览则用 1 调用双参数版。`DrawBarBackground` 是背景图加按钮下层的装饰，显示方式应该用 `BarDrawContext.BarRect` 调用它（内置的显示方式都是）；`DrawBarOverlay` 在每种显示方式画完之后用同一个矩形调用，所以按钮上层的装饰对任何显示方式都有效，没调用 `DrawBarBackground` 的显示方式只会有上层。画按钮请继续走 `ButtonPainter.Draw`（或游戏的 `DoButton`），其他模组对按钮的修改才会保留。

## 软性依赖本模组

大部分附属模组不管有没有 Main Button Bar System 都应该能用。常见做法：

- **XML 的列表项**：`MayRequire` 会在本模组不在时略过那一项。
  ```xml
  <modExtensions>
    <li Class="MainButtonBarSystem.MainButtonBarDefaults" MayRequire="NAI.MainButtonBarSystem">
      <inDrawer>true</inDrawer>
    </li>
  </modExtensions>
  ```
  （`modExtensions` 里带 `Class=` 的项在 1.7 还没验证过；如果报错，改用下一个做法。）
- **XML 补丁**：把 `PatchOperationAddModExtension` 包在 `PatchOperationFindMod` 里。注意 FindMod 比对的是模组**名称** `Main Button Bar System`，不是 packageId。
- **C#**：把会碰本模组的代码放在另一个程序集，用 `LoadFolders.xml` 只在本模组启用时加载：`<li IfModActive="NAI.MainButtonBarSystem">MBBS/</li>`。或者调用前先检查 `ModsConfig.IsActive("NAI.MainButtonBarSystem")`。
- `About.xml` 里，硬性依赖写在 `modDependencies` 加 `loadAfter`。RimSort 把 `*ByVersion` 标签当成替换、不是补充，所以用了它们的话要重复写一份。

主题模组一律是硬性依赖（没有本模组就没有人读 `BarAppearanceDef`），照上面 About.xml 的写法即可。

## API 稳定性

`MainButtonBarAPI.ApiVersion`（属性，目前 `1.1`）涵盖英文文档里的所有东西：五个接口的 Def 字段和 worker、`CompatNoticeDef`、`MainButtonBarDefaults`、`MainButtonArrangementDef`、`BarDrawContext`、`BarButton`、`BarDecoration`、`BarButtonImage`、`DecorationLayout`、`ImageFit`、`ButtonPainter.Draw` 和 `MainButtonBarAPI`。新增提高次版本号；移除或改变含义提高主版本号，而且被移除的成员会先标 `[Obsolete]` 保留一个版本。排序文件有自己的 `formatVersion`。没列出来的都是内部实现，可能会变。
