using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 管理玩家等級、經驗值數據與升級邏輯的系統。
    /// </summary>
    public sealed class PlayerLevelSystem : MonoBehaviour
    {
        [Header("--- 玩家等級數據 ---")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;

        [Header("--- 升級經驗值曲線設定 (陣列法) ---")]
        [Tooltip("依序填入升級到下一等所需的經驗值。例如 Element 0 代表 LV1 升 LV2 需要多少")]
        [SerializeField] private int[] xpRequirements = new int[] { 10, 25, 50, 90, 140, 200 };
        
        [Tooltip("如果等級超出陣列，每等額外增加的經驗需求")]
        [SerializeField] private int xpGrowthPerLevelAfterArray = 100;

        [Header("--- 參考組件對接 ---")]
        [SerializeField] private SkillUIManager uiManager;
        [SerializeField] private GameObject tinderHUD;

        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;

        private void Start()
        {
            if (uiManager == null)
            {
                uiManager = Object.FindFirstObjectByType<SkillUIManager>();
            }
            
            if (tinderHUD == null)
            {
                // 尋找 Canvas 下的 LevelUp_Tinder_HUD (包括非活動狀態的物件)
                TinderSwipeManager[] managers = Resources.FindObjectsOfTypeAll<TinderSwipeManager>();
                foreach (var manager in managers)
                {
                    if (manager.gameObject.scene.name != null) // 確保是場景執行個體而非 Prefab
                    {
                        tinderHUD = manager.gameObject;
                        break;
                    }
                }
            }

            UpdateXPSystem();
        }

        [ContextMenu("Add 5 XP")]
        public void TestAddXP()
        {
            AddExperience(5);
        }

        /// <summary>
        /// 公開方法：供外部（例如敵人死亡或吃到經驗球）呼叫，給予玩家經驗值
        /// </summary>
        /// <param name="amount">獲得的經驗值量</param>
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;

            currentXP += amount;
            Debug.Log($"[獲得經驗] +{amount} XP，目前進度: {currentXP} / {GetMaxXPForCurrentLevel()}");

            // 檢查是否滿足升級條件 (用 while 確保暴加經驗時可以連續升級)
            while (currentXP >= GetMaxXPForCurrentLevel())
            {
                LevelUp();
            }

            UpdateXPSystem();
        }

        private void LevelUp()
        {
            currentXP -= GetMaxXPForCurrentLevel();
            currentLevel++;
            Debug.LogWarning($"【玩家升級！】目前等級提升至：LV {currentLevel}");

            // 觸發升級介面
            if (tinderHUD != null)
            {
                tinderHUD.SetActive(true);
            }

            GameManager.TriggerPhase1Start();
        }

        private void UpdateXPSystem()
        {
            if (uiManager != null)
            {
                uiManager.UpdateXPUI(currentXP, GetMaxXPForCurrentLevel());
            }
        }

        /// <summary>
        /// 計算當前等級升級所需的總經驗值
        /// </summary>
        public int GetMaxXPForCurrentLevel()
        {
            int index = currentLevel - 1;
            
            // 如果還在設定的陣列範圍內，直接抓陣列數值
            if (index < xpRequirements.Length)
            {
                return xpRequirements[index];
            }
            
            // 如果等級爆表超出陣列，自動套用公式往上加
            int lastConfiguredXP = xpRequirements[xpRequirements.Length - 1];
            int exceededLevels = currentLevel - xpRequirements.Length;
            return lastConfiguredXP + (exceededLevels * xpGrowthPerLevelAfterArray);
        }
    }
}
