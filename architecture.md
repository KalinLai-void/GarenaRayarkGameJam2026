# 專案架構紀錄

## Phase 0.1 移動底座

本階段目標是完成一個可直接遊玩的 2D 素材 + 假 2.5D 俯視移動底座。範圍只包含玩家移動、地圖碰撞、基本遮擋、攝影機跟隨與暫用美術，不包含戰鬥、對話、技能、升級、敵人、UI、音效或地圖切換。

## 場景

- 主要場景：`Assets/Scenes/ForestMapScene.unity`
- 玩家物件：`Player`
- 地圖根物件：`ForestMap`
- 攝影機：`Main Camera` + `CM_Phase01_PlayerFollow`

## 輸入與移動

- 使用 Unity 舊版原生輸入。
- 玩家移動腳本：`Assets/01_Project/Scripts/Gameplay/PlayerMovement.cs`
- 操作鍵：`WASD` 與方向鍵。

`PlayerMovement` 只負責透過 `Input.GetKey` 讀取按住狀態、正規化方向、透過 `Rigidbody2D.MovePosition` 移動，以及依左右方向翻轉角色 Sprite。斜向移動會被正規化，避免速度變快。移動需要連續按住，所以不用 `Input.GetKeyDown` 作為主要移動讀取；`GetKeyDown` 後續保留給互動、確認或攻擊這類一次性按鍵。

## 碰撞與邊界

- Player 使用 `Rigidbody2D`，重力為 0，凍結旋轉，並使用 `CircleCollider2D` 作為主要身體碰撞。
- Phase 0.1 障礙物使用 `BoxCollider2D`。
- 地圖邊界由 `Phase01_MapBounds` 底下四個牆面物件限制，避免玩家離開可走區域。

## 2.5D 視覺與遮擋

- 使用 Orthographic Camera 維持 2D 俯視操作。
- 使用 `SpriteYSorter` 依世界座標 Y 值更新 `SpriteRenderer.sortingOrder`。
- 規則：畫面越下方的物件排序越前，營造基本前後遮擋。

## 攝影機

- 已安裝 `com.unity.cinemachine`。
- `Main Camera` 掛載 `CinemachineBrain`。
- `CM_Phase01_PlayerFollow` 跟隨並看向 `Player`，優先度設定為 20。

## 暫用美術

暫用素材由 imagegen 產生，來源圖與切圖位於：

- `Assets/01_Project/Sprites/Generated/phase01_generated_sheet_source.png`
- `Assets/01_Project/Sprites/Generated/player_lamb_temp.png`
- `Assets/01_Project/Sprites/Generated/mushroom_tree_temp.png`
- `Assets/01_Project/Sprites/Generated/mossy_rock_temp.png`
- `Assets/01_Project/Sprites/Generated/boundary_shrine_temp.png`

這些素材是 Prototype 用，可在不改程式邏輯的前提下替換成正式圖。

## 編輯器輔助

`Assets/01_Project/Scripts/System/Editor/Phase01SceneBuilder.cs` 提供選單：

`GarenaRayark/Phase 0.1/建置移動底座`

用途是快速重建 Phase 0.1 場景配置，包含 Sprite 匯入設定、舊版原生輸入、Rigidbody2D、障礙碰撞、地圖邊界、Cinemachine 攝影機與 Y 軸排序。這是 Game Jam 快速落地用工具，不依賴 asmdef，也不使用 TDD。
