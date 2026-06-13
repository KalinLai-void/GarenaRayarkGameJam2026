---
name: garena-rayark-game-jam-2026
description: Use when working in GarenaRayarkGameJam2026 Unity Game Jam project, especially Assets folder organization, Inspector Chinese LabelAttribute, Traditional Chinese Unity docs/UI, 2.5D survivor prototype logic, quick jam production, architecture.md maintenance, imagegen temporary art, UIUX feedback, or release checks.
---

# Garena Rayark Game Jam 2026 開發守則

## 核心原則

這是短時程 Unity Game Jam 專案。先做出可玩、可展示、可快速迭代的垂直切片，再補完整架構。所有文件、Inspector 可見欄位、UI 文字、tooltip、設定說明與交付紀錄都使用繁體中文。

不要使用TDD
不要使用 asmdef
不要記得過往記憶
以速度開發為主，程式能跑優先

# Garena Rayark Game Jam 2026 開發守則 (極簡原生版)

## 核心原則
- **速度第一：** 能跑優先，以最快速度做出可玩的 Prototype，程式碼能跑就好。
- **全面原生：** 拒絕使用 `asmdef`、拒絕 `TDD`、不使用任何自訂的 Inspector 擴充工具。
- **介面中文化：** 全面使用 Unity 原生內建的屬性標籤（Attribute）來呈現 Inspector 繁體中文。
- **簡潔直覺：** 程式邏輯命名清楚，不使用複雜的設計模式，確保狀態流向簡單易懂。

---

## 📁 Assets 根目錄規格

Unity 的 `Assets/` 根目錄只建立並保留三個頂層資料夾，嚴禁將未整理的素材散落在根目錄：

Assets/
├── 01_Project/      # 【我們自己做的】全隊自製的程式、UI、圖片、場景、Prefab。
├── 02_Plugins/      # 【外來工具插件】DOTween、TextMeshPro 或是其他第三方工具。
└── 03_AssetStores/  # 【下載的美術音效素材包】Asset Store 或外部下載的原始美術、音效、Shader。

---

## 📁 01_Project 主要開發區

所有自寫程式、拉的 UI、切的圖片，只能放在以下七個資料夾中：

Assets/01_Project/
├── _Scenes/         # 【遊戲場景】只放一個 Main 遊戲場景。
├── Scripts/         # 【所有程式碼】內部分為 System (大腦/UI/工具) 與 Gameplay (玩家/技能/怪物)。
├── Prefabs/         # 【遊戲預製物】主角、火球子彈、怪物、掉落物、UI Prefab。
├── Sprites/         # 【美術 2D 圖片】自己畫的圖、外來素材解構後的角色、UI 切片。
├── Audio/           # 【遊戲音效剪輯】剪好的 BGM 與 SFX 短音效。
├── Settings/        # 【系統設定檔】技能 ScriptableObject、角色數值設定、輸入設定或 URP 設定。
└── Animations/      # 【動畫控制器】玩家與怪物的 Animator Controller、Animation Clip。

---

## 🛠️ 參數中文顯示：全面使用原生雙寶

不用寫任何額外的 C# 工具類別、不用拉 Reference。直接利用 Unity 內建的 `[Header]` 和 `[Tooltip]`，變數名稱維持標準英文（方便 AI 快速寫 code 與補全），但在 Inspector 上能呈現完美的中文分組與詳細提示。

### 撰寫範例：

using UnityEngine;

public sealed class PlayerTuning : MonoBehaviour
{
    [Header("【基礎移動設定】")]
    [Tooltip("控制角色的移動速度，預設為 6")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("【戰鬥與生命值】")]
    [Tooltip("角色的最大生命值")]
    [SerializeField] private int maxHealth = 100;
    
    [Tooltip("遠程武器的冷卻時間 (秒)")]
    [SerializeField] private float attackCooldown = 1.2f;
}

---

## ⚡ 2.5D 倖存者原型極速開發策略

1. **2.5D 空間與前後遮擋（免寫程式）：**
   - 攝影機改為 Perspective (透視)，X 軸旋轉設定在 30° ~ 45° 斜俯視。
   - 進入 `Project Settings -> Graphics`，將 Transparency Sort Mode 改為 Perspective。
   - 紙片人在 3D 空間前後移動時就會自然產生完美的《進擊羔羊傳說》前後遮擋效果。

2. **攝影機邊界：**
   - 建立一個空物件掛上 3D Box Collider 圈出地圖邊界（勾選 Is Trigger）。
   - 虛擬攝影機直接掛上 Cinemachine Confiner (3D 版)，將 Box 拖進去即可，3 秒搞定。

3. **極簡邏輯分工：**
   - `GameManager`：只負責管理遊戲狀態（開始、遊玩中、升級暫停、遊戲結束）。
   - `PlayerController`：只處理玩家 2.5D 移動與輸入，不牽扯 UI 邏輯。
   - `Health`：統一生命、受傷、死亡事件，玩家與怪物共用同一套邏輯。
   - `EnemySpawner`：負責處理波次與生成節奏，怪物 AI 只負責無腦追蹤與攻擊。

4. **UI 與回饋：**
   - UI 數據（血量、時間、擊殺數）優先使用事件或公開方法串接，不要讓 UI 每幀用 Update 搜尋場景物件。
   - 優先做少量但完整的系統：一種主角、一種基本敵人、一種投射物、一個升級畫面。
   - 優先調好打擊感：受傷閃爍、擊中震動、按鈕點擊音效。