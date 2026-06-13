技能系統與卡片 UI 設計規格文件
本文件統整了《GarenaRayarkGameJam2026》技能系統的整體架構、Tinder 卡片 UI 階層調整規格、戰鬥視覺反饋，以及首批（優先度 ⭐3 以上）技能的實作規格。

🛠️ 1. 卡片 UI 節點階層規格 (Tinder Card UI Hierarchy)
為了解決角色去背圖與卡片背景/外框的拉伸問題，我們將重新整理 UI_TinderCard Prefab 的節點關係。

📌 節點階層結構

UI_TinderCard (Prefab 根節點，掛載 UI_TinderCard 腳本與 Drag Handler)
 ├── Character_Illustration (原節點，重新整理為 AvatarBackground，負責顯示背景/外框)
 │    └── Avatar (新建子節點，Image 元件，負責顯示獨立的人物去背圖，可自由調整位置與縮放)
 └── Dialog_Bubble (對話框容器)
      ├── Skill_Name_Text (TextMeshProUGUI，顯示技能名稱)
      └── Skill_Desc_Text (TextMeshProUGUI，顯示技能對白與效果描述)
🎨 稀有度視覺規劃 (Rarity Visuals)
SSR：金色外框。
SR：紫色外框。
R：標準木質或銀色外框。 (注意：以上皆為單純圖片外框，不需要任何動態粒子/流光特效)
⚙️ 2. 程式架構設計 (Code Architecture)
技能系統採用 資料驅動 (Data-Driven) 與 狀態管理器 的分離架構。

Mermaid diagram
🔹 關鍵模組與 HUD 同步職責
SkillData.cs (ScriptableObject)：
儲存單一技能的所有靜態設定（包含用於左上角 HUD 的 skillIcon 與用於卡片的 cardAvatar）。
UI_TinderCard.cs (卡片控制器)：
負責將 SkillData 的資料渲染至卡片 UI（包含 AvatarBackground 背景與子物件 Avatar 去背圖）。
PlayerSkillSystem.cs (玩家技能管理器 - 掛載於 Player)：
紀錄玩家持有的技能與當前等級。
處理主動技能的冷卻（CD）計時，並與畫面的 SkillUIManager 同步。
處理玩家按下 空白鍵 (Space) 釋放主動技能的按鍵監聽。
HUD 同步規則：
當獲得新技能時，若為主動則放入 Slot 0；若為被動則依序放入 Slot 1 ~ 3。
將 SkillData.skillIcon 指派給該 Slot 的 UI Image，並顯示對應的等級（如 LV 1）。
🍄 3. 首批實作技能規格 (⭐3 以上優先)
稀有度	蘑菇名稱 (暫定)	技能類型	一般冷卻	戰鬥效果	升級效果	專屬對白
SSR	天使金光菇	主動技能	60 秒	釋放時回復 50% 生命值；
若玩家有低於 LV.2 的技能，則強制提升最低等技能至 LV.2（若符合此條件則不回血）。	恆定為 LV.5，
整局只會抽到並 Like 一次。	「已購買，是個人都愛!」
SR	黑木耳	主動技能	15 秒	使用後 10 秒內，子彈擊中敵人或牆壁時在撞擊點留下一個持續 2 秒的黑色黏著方塊。方塊會緩速經過的怪物 10% 並造成每秒 5% 子彈傷害。	每次升級：
緩速強度 +5%
燃燒/每秒傷害 +5%	「被打中就別起來了~」
R	白精靈菇	被動技能	無 (被動)	玩家射擊時，同時發射 2 發子彈（呈扇形擴散）。	每次升級：
子彈數量 +1 發	「一次只能射一發，去跟小孩一桌」
R	洋菇	被動技能	無 (被動)	子彈擊中敵人時，降低其移動速度。	每次升級：
緩速效果提升 3%
(LV.1 = 5%, LV.2 = 8%...)	「修但幾累!」
R	香菇	被動技能	無 (被動)	子彈擊中敵人時施加燃燒效果（持續 3 秒），使其每秒受到 5% 子彈傷害 的 DoT 傷害。	每次升級：
每秒燃燒傷害提升 3%
(LV.1 = 5%, LV.2 = 8%...)	「又燒又痛，這波叫雙贏!」
R	金針菇	被動技能	無 (被動)	玩家射速提升 15%，且子彈飛行速度提升 10%。	每次升級：
射速額外 +5%	「今天超度你，看你怎麼明天見?」
🎨 4. 戰鬥視覺反饋與顏色特效規格 (Combat FX & Color Tint Specs)
IMPORTANT

視覺與數字規範：

所有效果皆不需要粒子特效，亦不要出現任何傷害飄字數字。
只使用簡單的顏色變更 (Color Tint) 來作為狀態回饋。
🔵 A. 緩速狀態 (洋菇 / 黑木耳方塊)
視覺反饋：怪物被緩速時，將怪物 SpriteRenderer 顏色變更為藍色（Tint Blue）。
規範：無粒子特效，無水滴/冰晶圖示。
物理機制：EnemyMovement 的速度乘以 (1 - 當前最大緩速值)。多個緩速來源同時存在時，取效果最強者（不套用乘法疊加，防止速度變為 0）。
🔴 B. 灼燒狀態 (香菇 / 黑木耳方塊)
視覺反饋：怪物受燃燒傷害時，將怪物 SpriteRenderer 顏色變更為紅色（Tint Red）。
規範：無火焰粒子特效，無任何傷害數字飄字。
傷害機制：每秒觸發一次扣血。
🌀 C. 散彈擴散 (白精靈菇)
視覺反饋：複數子彈以滑鼠指向的角度為中心，呈扇形發射。
規範：無任何射擊煙霧或槍口閃光等粒子特效。
🌟 D. 天使治癒 (天使金光菇)
視覺反饋：釋放時玩家血量條回復，並同步更新 UI。
規範：無金色光柱，無羽毛粒子特效，無流光效果，無治療飄字數字。
🛠️ 5. 系統串接規範
TinderSwipeManager 改動：
將字串陣列卡池改為讀取 List<SkillData>。
右滑 Like 時，呼叫 PlayerSkillSystem.Instance.AcquireOrUpgradeSkill(selectedSkill)。
WeaponController 改動：
讀取「白精靈菇」與「金針菇」的等級來決定子彈射擊發數、射速加成與子彈速度。
將玩家當前的「洋菇（緩速值）」與「香菇（灼燒值）」附加在射出的 Bullet 實例上。
Bullet 改動：
攜帶緩速、灼燒、黑木耳技能參數。
擊中怪物時，在撞擊點呼叫 Instantiate 生成黑木耳方塊（簡潔無特效），並對怪物施加狀態。