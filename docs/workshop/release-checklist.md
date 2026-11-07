# 發佈流程：Steam 工作坊＋GitHub Release

寫給第一次發佈的人。每一步都標了誰做：**【你】**是要用你的 Steam 帳號、在遊戲或網頁上按的；**【Claude】**是在電腦上跑指令的部分。上傳工作坊一定要你自己按，Claude 不能代替。

## 先認識幾個名詞

- **工作坊項目**：Steam 上這個模組的頁面。第一次上傳時建立，之後每次上傳都是更新同一頁。
- **`PublishedFileId.txt`**：第一次上傳後，遊戲在模組的 `About/` 寫下的檔案，裡面是工作坊項目的編號。之後就靠它更新同一頁，所以要存回 repo。
- **發佈資料夾**：`tools/make_release.py` 做出來的資料夾，只有遊戲需要的檔案、授權檔和教學，沒有原始碼和測試。上傳的就是它。
- **tag**：git 歷史上的版本標籤，例如 `v1.0.0`，記下「這一版是從這個 commit 做出來的」。
- **GitHub Release**：GitHub 專案頁右邊的 Releases，每個版本一頁，附說明和 zip 下載。內容和工作坊上的一樣。

## 一、準備發佈資料夾【Claude】

1. 確認 main 就是要發佈的版本：要進這一版的 PR 都合併了，`verify.ps1` 通過。
2. `About/About.xml` 的 `modVersion` 是這次的版本，`CHANGELOG.md` 有這一版的段落。
3. 關掉遊戲，在 repo 根目錄執行（`<RimWorld>` 是遊戲資料夾；zip 放在 repo 外面，例如工作區的 `_資料與測試\MainButtonBarSystem\releases`）：

   ```
   python -I -X utf8 tools/make_release.py "<RimWorld>\Mods\MainButtonBarSystem" --clean --zip "<放 zip 的資料夾>"
   ```

   - `Mods\MainButtonBarSystem` 會整個換成發佈資料夾：只留發佈的檔案，`About/PublishedFileId.txt` 會保留。遊戲上傳的就是 `Mods` 裡這一份。
   - `--zip` 另外做出 `MainButtonBarSystem-<版本>.zip`，以及從 `CHANGELOG.md` 取出的這版說明 `MainButtonBarSystem-<版本>-notes.md`。
   - 從這裡到第五步存回 `PublishedFileId.txt` 之前，**不要跑 `deploy.ps1`**：它會重建 `Mods\MainButtonBarSystem`，把發佈資料夾換掉。

## 二、上傳前在遊戲裡看一次【你，約 5 分鐘】

1. 照平常用 RimCrow 開遊戲，排序不用換。
2. 進一張地圖，打開本模組的設定、按幾個主按鈕。沒有紅色錯誤跳出來就可以。
3. 關遊戲後跟 Claude 說一聲，Claude 會再檢查 log 裡有沒有本模組的紅字。

## 三、上傳到工作坊【你】

遊戲語言是簡體中文時，按鈕的名稱如下（括號裡是繁中／英文介面的名稱）：

0. **先打開 Steam 並登入要擁有這個模組的帳號。** 遊戲要連上 Steam 才會出現上傳選項。照平常用 RimCrow 開遊戲就可以；如果第 4 步找不到上傳選項，改從 Steam 開遊戲再試一次。
1. 主選單 →「选项」→ 勾「开发人员模式」（開發者模式／Development mode）→ 關掉選項視窗。
2. 主選單 →「Mod 配置」（模組／Mods）。
3. 在左邊清單點一下 **Main Button Bar System**（模組很多的話，用清單上方的搜尋框打 Main Button）。只要選它就好，不要勾選或取消勾選任何模組；它有沒有啟用都可以上傳。
4. 右邊說明下方按「高级...」（進階...／Advanced...）→「上传到 Steam 创意工坊」（上傳至Steam創意工坊／Upload to Steam Workshop）。
   - 已經上傳過的話，這個選項會變成「更新到 Steam 创意工坊」。它要等遊戲向 Steam 問到作者是誰才會出現，選完模組後等幾秒；一直沒出現的話，可能是 Steam 沒連上，或登入的不是上傳時的帳號。
   - 如果按下去跳出的是 packageId、支援版本或 LoadFolders 有問題的訊息，截圖給 Claude（本模組目前不會有）。
5. 跳出「确定上传到Steam创意工坊吗？」，要你同意 Steam 創意工坊條款。視窗裡有三個勾選框，**全部維持原樣不要動**：
   - 「标记为翻译」不勾：勾了會被標成翻譯包。
   - 「构建资源包」勾著：上傳時把貼圖打包，玩家讀取比較快。
   - 「不要上传原纹理」不勾。
   
   然後按「确定」。
6. 跳出「你所上传的内容必须是你原创的。这是你原创的吗？」→ 按鈕要等 6 秒才能按 →「是」。
7. 等上傳視窗跑完。成功時畫面上會出現「已成功上传Main Button Bar System。」，Steam 會打開這個模組的工作坊頁面。
8. 按「返回」關掉 Mod 配置。沒有改過勾選，就不會要求重開遊戲。
9. 回「选项」把「开发人员模式」關掉（不關也沒關係，只是會多一些除錯按鈕）。
10. 跟 Claude 說「上傳好了」。如果中途跳出錯誤，截圖給 Claude。

補充（從遊戲程式確認的）：

- 遊戲會先把模組資料夾複製到存檔資料夾的 `WorkshopUpload\` 再上傳。1.7 預設還會把貼圖烘焙成資產包一起上傳，所以工作坊版可能比 zip 多出這些檔，模組內容是一樣的。
- 工作坊的標題來自模組名稱，預覽圖來自 `About/Preview.png`，標籤會帶上支援的版本（1.7）。
- 說明只有第一次上傳時會從 `About.xml` 帶過去，而且是很短的那段，所以要照第四步換掉。

## 四、在工作坊頁面上設定【你；第一次要做，之後更新通常只要寫變更紀錄】

在 Steam 打開的頁面上操作。如果沒打開，可以從 Steam →「社群」→「工作坊」→「你的檔案」找到它。右邊「擁有者控制」那一區都是給作者用的按鈕。下面寫的是 Steam 網頁上大概的按鈕名稱，沒辦法從遊戲確認，實際文字可能略有不同；找不到就截圖給 Claude。

1. **法律協議**：頁面上方如果有提示要你同意「Steam 工作坊法律協議」，先點進去同意，不然別人看不到這個模組。沒看到提示就跳過。（Claude 看不到你的帳號狀態，所以這一步要你自己看。）
2. **說明**：按「編輯標題與描述」，把描述欄整段換成 `docs/workshop/description.txt` 的內容（Claude 會先複製到剪貼簿，你直接貼上）→ 儲存。
   - 內容是 Steam 的 BBCode 格式，英文在前，簡中、繁中在後。
   - 如果 Steam 說太長或被截斷，跟 Claude 說。Claude 會改成英文一份，中文用編輯頁的語言選單另外各存一份。
3. **必要項目**：按「新增／移除必要項目」，搜尋 Harmony（作者 Andreas Pardeike／brrainz）→ 加入。訂閱的人就會被提醒要先裝 Harmony。
4. **可見度**：準備好就選「公開」。想先自己看過，可以先選「隱藏」或「僅限好友」。
5. **變更紀錄**：第一次上傳時遊戲會自動寫「[Auto-generated text]: Initial upload.」。到「變更紀錄」頁可以改成 `CHANGELOG.md` 裡這一版的內容。之後每次更新都這樣做。

## 五、存回 repo、發 GitHub Release【Claude】

1. 把 `<RimWorld>\Mods\MainButtonBarSystem\About\PublishedFileId.txt` 複製到 repo 的 `About/`，開 PR（【你】合併）。之後 `deploy.ps1` 和 `make_release.py` 都會帶著它，可以照常用 `deploy.ps1` 了。
2. 重跑第一步的指令，讓 zip 裡也有 `PublishedFileId.txt`，和工作坊上的一樣。
3. 在做發佈資料夾的那個 main commit 打上 tag 並推上 GitHub：

   ```
   git tag -a v1.0.0 -m "Main Button Bar System 1.0.0" <commit>
   git push origin v1.0.0
   ```

4. 建立 GitHub Release，附上 zip 和這版說明：

   ```
   gh release create v1.0.0 "<zip 路徑>" --title "1.0.0" --notes-file "<notes.md 路徑>"
   ```

5. 工作坊網址確定後，在 `README.md` 補上連結。

## 之後每次更新

1. 【Claude】改 `About.xml` 的 `modVersion`，在 `CHANGELOG.md` 最上面加這一版。
2. 照第一～三步做。第三步的選項會變成「更新到 Steam 创意工坊」。
3. 【你】在工作坊的「變更紀錄」貼上 `CHANGELOG.md` 這一版的內容。第四步的其他設定不用再做。
4. 【Claude】做第五步的 3、4（打 tag、建 Release）。`PublishedFileId.txt` 已經在 repo 裡，不用再存。

## 授權

- 程式碼和翻譯是 MIT（`LICENSE`）。
- 萘伊的圖是 CC BY-NC-ND 4.0。
- 做主題用的素材是 CC0。
- 哪些檔案屬於哪一種，寫在 `LICENSES.md`。`make_release.py` 會把這兩個檔放進發佈資料夾。
- 工作坊說明的「License／授权／授權」段落也寫了。

## 其他

- 公開 API 的版本在 `MainButtonBarAPI.ApiVersion`，規則寫在 `docs/API.md` 的「API stability」：
  - 新增東西時，升次版本號。
  - 移除或改變意義時，升主版本號，而且要先標 `[Obsolete]` 保留一個版本。
- Multiplayer 出 1.7 版以後，可以向它開 PR，把本模組加進不同步設定檔的名單（見 `docs/research/ecosystem-2026-11-04.md`）。
