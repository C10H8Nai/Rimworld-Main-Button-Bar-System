# 工作坊發佈清單

工作坊上傳由你在遊戲內用自己的 Steam 帳號完成（規格第 2 節），雲端只準備檔案和說明文字。

## 1. 準備乾淨的上傳資料夾

這個 repo 本身就是模組資料夾，裡面還有原始碼、測試、工具、設計文件、測試截圖，這些玩家都不需要。在 repo 根目錄執行：

```bash
python3 tools/make_release.py "D:/RimWorldUpload/MainButtonBarSystem"
```

這會只複製遊戲會讀的東西：`About/`、`1.7/`、`Defs/`、`Languages/`、`Textures/`、`LoadFolders.xml`，以及給附屬模組作者看的 `API.md`。

- 目標資料夾已經有 `About/PublishedFileId.txt` 的話會保留，之後上傳都會更新同一個工作坊項目。
- 目標資料夾不能在 repo 裡面。

## 2. 上傳前檢查

- [ ] `About/About.xml`：`modVersion` 要和這次的版本一致；`description` 是遊戲內模組清單看到的簡短說明。
- [ ] `About/Preview.png`：目前是佔位圖（640×360，用圖形畫的，沒有用遊戲美術）。要換的話保持 16:9，檔案小於 1 MB。
  用實機截圖換：`python3 tools/make_preview.py 原版截圖.png 本模組截圖.png --before-crop x,y,w,h --after-crop x,y,w,h`（上半原版擠成縮寫的列，下半捲動＋「更多」抽屜；只用遊戲畫面和本模組自己的主題，不放 DLC 美術；腳本會確保小於 1 MB）。
- [ ] 把上傳資料夾放進 `RimWorld/Mods/`。暫時停用 repo 那份，避免同一個 packageId 出現兩次。
- [ ] 進遊戲確認：
  - `[Main Button Bar System] Loaded.` 有出現；
  - 本模組沒有紅字；
  - 設定畫面、抽屜齒輪、翻譯都正常。

## 3. 上傳

1. 開發模式打開。
2. 到「模組」清單選本模組，按「上傳到 Steam 工作坊」。
3. 第一次上傳後，`About/PublishedFileId.txt` 會出現。把它複製回 repo 的 `About/`，之後的發佈資料夾都會帶著它。
4. 到工作坊頁面貼上 `docs/workshop/description.txt`。內容是 Steam 的 BBCode，英文在前，簡中、繁中在後。
5. 標籤：Mod、1.7。
6. 工作坊網址確定後，可以在 `About.xml` 加上 `<url>`。

## 4. 發佈後

- 更新時改 `modVersion`、重新執行 `make_release.py`、上傳，並在工作坊的「變更紀錄」寫這次改了什麼。
- 公開 API 的版本在 `MainButtonBarAPI.ApiVersion`，規則寫在 `docs/API.md`「API stability」：
  - 新增東西時升次版本號。
  - 移除或改變意義時升主版本號，而且要先標 `[Obsolete]` 保留一個版本。
- Multiplayer 出 1.7 版以後，可以向它開 PR，把本模組加進不同步設定檔的名單（見 `docs/research/ecosystem-2026-11-04.md`）。
