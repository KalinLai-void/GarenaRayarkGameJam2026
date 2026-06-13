---
name: garena-rayark-game-jam-2026
description: Use when working in GarenaRayarkGameJam2026 Unity Game Jam project, especially Assets folder organization, Inspector Chinese LabelAttribute, Traditional Chinese Unity docs/UI, 2.5D survivor prototype logic, quick jam production, architecture.md maintenance, imagegen temporary art, UIUX feedback, or release checks.
---

# Garena Rayark Game Jam 2026 開發守則

## 核心原則

這是短時程 Unity Game Jam 專案。先做出可玩、可展示、可快速迭代的垂直切片，再補完整架構。所有文件、Inspector 可見欄位、UI 文字、tooltip、設定說明與交付紀錄都使用繁體中文。

使用者已明確表示舊有內容多數有問題並已刪除；不要依賴舊資料夾、舊 prefab、舊場景或舊架構假設。接手時以本 SKILL 為基準重整。

## Assets 根目錄規格

Unity 的 `Assets/` 根目錄只建立並保留三個頂層資料夾：

```text
Assets/
├── 01_Project/
├── 02_Plugins/
└── 03_AssetStores/
```

| 資料夾 | 面板中文標籤 | 用途 |
| --- | --- | --- |
| `01_Project/` | 【我們自己做的】 | 全隊自己寫的程式、自己做的 UI、自己切的圖片、場景、prefab、設定與整理後的遊戲資產。加 `01` 是為了永遠排在最上方。 |
| `02_Plugins/` | 【外來工具插件】 | DOTween、TextMeshPro、輸入系統或其他第三方 Unity 工具。 |
| `03_AssetStores/` | 【下載的美術音效素材包】 | Asset Store 或外部下載的原始美術、音效、字型、shader、素材包。 |

不要在 `Assets/` 根目錄新增其他資料夾。若 Unity 或插件匯入後產生根目錄資料夾，整理到上述分類後再提交。

## `01_Project/` 主要開發區

`Assets/01_Project/` 內直接建立以下七個資料夾。全隊所有自己寫的程式、自己拉的 UI、自己切的圖片，只能放在這裡。

```text
Assets/01_Project/
├── _Scenes/
├── Scripts/
├── Prefabs/
├── Sprites/
├── Audio/
├── Settings/
└── Animations/
```

| 資料夾 | 面板中文標籤 | 負責存放的內容 |
| --- | --- | --- |
| `_Scenes/` | 【遊戲場景】 | 只放一個 `Main` 遊戲場景。 |
| `Scripts/` | 【所有程式碼】 | 所有自寫 C#。內部分為 `System`（大腦/UI/工具）與 `Gameplay`（玩家/技能/怪物）。 |
| `Prefabs/` | 【遊戲預製物】 | 已綁好程式碼與美術的物件，例如主角、火球子彈、怪物、掉落物、UI prefab。 |
| `Sprites/` | 【美術 2D 圖片】 | 自己畫的圖片、外來素材解構後的角色、UI 切片、背景圖。 |
| `Audio/` | 【遊戲音效剪輯】 | 剪好的 BGM，以及跳躍、受傷、按鈕、技能等 SFX 短音效。 |
| `Settings/` | 【系統設定檔】 | 技能 ScriptableObject、角色數值設定、輸入設定、URP 或其他專案設定資源。 |
| `Animations/` | 【動畫控制器】 | 玩家與怪物的 Animator Controller、Animation Clip、UI 動畫 clip。 |

`Scripts/System/` 放遊戲流程、GameManager、UI、輸入、音訊、資料讀取、Editor/Inspector 工具。`Scripts/Gameplay/` 放玩家、怪物、技能、投射物、碰撞、掉落與戰鬥規則。

## Inspector 中文 Label 工具

專案需要一個 Custom PropertyAttribute，讓欄位可用 `[Label("中文欄位名")]` 取代 Inspector 上的英文變數名稱。未來實作時請放在：

```text
Assets/01_Project/Scripts/System/Inspector/LabelAttribute.cs
Assets/01_Project/Scripts/System/Editor/LabelAttributeDrawer.cs
```

必要規格：

- `LabelAttribute` 繼承 `PropertyAttribute`，保存繁體中文欄位名稱。
- `LabelAttributeDrawer` 繼承 `PropertyDrawer`，只在 Unity Editor 編譯。
- `OnGUI` 使用新的 `GUIContent` 顯示中文 label，並呼叫 `EditorGUI.PropertyField(position, property, label, true)`。
- `GetPropertyHeight` 使用 `EditorGUI.GetPropertyHeight(property, label, true)`，避免陣列、struct、foldout 或巢狀序列化欄位高度錯誤。
- 中文 label 為空字串時，保留 Unity 原本欄位名稱。
- C# 檔案使用 UTF-8，避免中文註解或字串亂碼。

建議 namespace：

```csharp
namespace GarenaRayarkGameJam2026.SystemTools
```

使用範例：

```csharp
using GarenaRayarkGameJam2026.SystemTools;
using UnityEngine;

public sealed class PlayerTuning : MonoBehaviour
{
    [SerializeField, Label("移動速度")]
    private float moveSpeed = 6f;

    [SerializeField, Label("最大生命值")]
    private int maxHealth = 100;
}
```

## Game Jam 快速開發策略

- 先完成 1 分鐘可玩的核心循環：移動、攻擊、敵人生成、受傷、擊殺、升級或取得技能、結算。
- 每個玩法數值都先暴露成 `[SerializeField]`、`ScriptableObject` 或設定檔，不要硬寫在深層方法裡。
- 程式邏輯要清楚命名、單一職責、小步提交；Game Jam 可以簡化架構，但不能讓狀態流向看不懂。
- 優先做少量但完整的系統：一位玩家、一種基本敵人、一種投射物、一種升級選項、一個結算畫面。
- UI 只先做必要資訊：血量、時間、擊殺數或分數、升級選項、暫停與遊戲結束。
- UIUX 回饋要早做：按鈕 hover/click、受傷閃爍、擊中震動、技能冷卻、升級彈窗、死亡/勝利轉場。
- 美術可先用 imagegen 或暫用素材，但要記錄 prompt、用途與替換位置；不要把未整理的素材散落在根目錄。
- 2.5D 方向以 dark-cute、風格化、俯視或斜俯視的倖存者玩法為主；可借鑑氛圍與鏡頭語言，不直接複製既有 IP。

## 邏輯設計建議

- `GameManager` 只管理遊戲狀態：開始、遊玩中、升級中、暫停、結算。
- `PlayerController` 只處理玩家移動與輸入轉換，不直接管理敵人或 UI。
- `Health` 統一生命、受傷、死亡事件，玩家與敵人共用概念。
- `EnemySpawner` 負責波次與生成節奏，敵人 AI 只負責追蹤與攻擊。
- `SkillDefinition` 或相近 ScriptableObject 描述技能顯示名稱、說明、圖示、數值、冷卻與 prefab。
- 可展示名稱一律保留繁體中文欄位，例如 `displayNameZh`、`descriptionZh`、`tooltipZh`。
- 優先使用事件或清楚的公開方法串接 UI，不要讓 UI 每幀搜尋場景物件。

## `architecture.md` 維護

若任務涉及架構、系統切分或功能完成，直接更新同一份 `architecture.md`，不要另開平行版本。至少記錄：

- 專案範圍與目前可玩目標。
- Unity 版本與主要套件。
- `Assets/` 資料夾規格。
- 場景流程與 `Main` 場景責任。
- 2.5D 鏡頭與座標策略。
- 系統責任分工：System、Gameplay、UI、Audio、Settings。
- ScriptableObject/config 清單。
- UI/input flow。
- imagegen 暫用美術流程與 prompt 紀錄位置。
- 效能策略、測試方式、交付檢查與已知限制。

## 交付前檢查

- `Assets/` 根目錄只有 `01_Project/`、`02_Plugins/`、`03_AssetStores/`。
- `Assets/01_Project/` 只有 `_Scenes/`、`Scripts/`、`Prefabs/`、`Sprites/`、`Audio/`、`Settings/`、`Animations/` 這七個主要資料夾。
- 自寫程式都在 `Assets/01_Project/Scripts/`。
- 主要遊戲場景是 `Assets/01_Project/_Scenes/Main.unity`。
- Inspector 需要展示給企劃或隊友看的欄位有繁體中文 label。
- UI、文件、tooltip、設定描述都使用繁體中文。
- Unity Console 沒有新的 compile error。
- 從 `Main` 按 Play 可以開始、死亡或結算後可以重新開始。
- WebGL 或目標平台 build 前，確認沒有遺失 reference、沒有根目錄散落素材、沒有未替換的明顯測試文字。
