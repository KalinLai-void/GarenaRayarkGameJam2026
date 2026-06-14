using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    /// <summary>
    /// 玩家技能管理器，負責記錄技能等級與冷卻。
    /// ⚠️ 此腳本必須掛在場景的 Player（或 UIManager）GameObject 上，否則 Instance 為 null！
    /// </summary>
    public sealed class PlayerSkillSystem : MonoBehaviour
    {
        public static PlayerSkillSystem Instance { get; private set; }

        [Header("【主動與被動技能記錄】")]
        [SerializeField] private SkillData activeSkill;
        [SerializeField] private List<SkillData> passiveSkills = new List<SkillData>();

        [Header("--- Input Action (綁定空白鍵，可在 Inspector 自行更換為 F 等按鍵) ---")]
        [SerializeField] private InputAction skillAction = new InputAction("UseSkill", binding: "<Keyboard>/space");

        // 紀錄技能的等級 (技能ID -> 等級)
        private readonly Dictionary<string, int> skillLevels = new Dictionary<string, int>();
        
        // 紀錄是否已取得 SSR (天使金光菇)
        private bool hasAcquiredSSR = false;

        // 冷卻與持續時間狀態
        private float activeCDTimer = 0f;
        private float activeCDDuration = 0f;
        private float activeSkillEffectDurationTimer = 0f;
        private bool isActiveSkillEffectRunning = false;

        public SkillData ActiveSkill => activeSkill;
        public List<SkillData> PassiveSkills => passiveSkills;

        public float ActiveCDTimer => activeCDTimer;
        public float ActiveCDDuration => activeCDDuration;
        public bool IsActiveSkillEffectRunning => isActiveSkillEffectRunning;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log($"【技能系統】PlayerSkillSystem 初始化成功！掛載於：{gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"【技能系統】已有 PlayerSkillSystem 實體 ({Instance.gameObject.name})，銷毀重複實體。");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            if (skillAction != null)
            {
                skillAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (skillAction != null)
            {
                skillAction.Disable();
            }
        }

        private void Update()
        {
            // 更新冷卻時間
            if (activeCDTimer > 0f)
            {
                activeCDTimer -= Time.deltaTime;
            }

            // 更新主動技能持續時間（如 10 秒）
            if (isActiveSkillEffectRunning && activeSkillEffectDurationTimer > 0f)
            {
                activeSkillEffectDurationTimer -= Time.deltaTime;
                if (activeSkillEffectDurationTimer <= 0f)
                {
                    isActiveSkillEffectRunning = false;
                    Debug.Log($"【技能系統】主動技能 {activeSkill?.skillName} 效果持續時間已結束。");
                }
            }

            // 監聽空白鍵觸發主動技能 (支援 InputAction 與 Keyboard.current 直讀，防止焦點丟失)
            bool spacePressed = false;
            if (skillAction != null && skillAction.triggered)
            {
                spacePressed = true;
            }
            else if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                spacePressed = true;
            }

            if (spacePressed)
            {
                // 加入詳細診斷日誌，讓玩家與工程師能明確知道按鍵是否被偵測，以及為何沒有觸發
                Debug.Log($"【技能系統】空白鍵被按下！TinderUI 是否開啟: {IsTinderUIOpen()} | 當前主動技能: {(activeSkill != null ? activeSkill.skillName : "無")} | 當前冷卻計時器: {activeCDTimer:F2}s");

                if (IsTinderUIOpen())
                {
                    Debug.Log("【技能系統】空白鍵按下，但因 Tinder 升級介面開啟中而忽略觸發技能。");
                }
                else
                {
                    if (activeSkill == null)
                    {
                        Debug.Log("【技能系統】Space 按下，但尚未獲得主動技能。");
                    }
                    else if (activeCDTimer > 0f)
                    {
                        Debug.Log($"【技能系統】Space 按下，{activeSkill.skillName} 還在冷卻中（剩餘 {activeCDTimer:F1}s）。");
                    }
                    else
                    {
                        TriggerActiveSkill();
                    }
                }
            }
        }

        private TinderSwipeManager cachedTinderManager;

        /// <summary>
        /// 檢查 Tinder (LevelUp) UI 是否正在開啟中
        /// </summary>
        private bool IsTinderUIOpen()
        {
            if (cachedTinderManager == null)
            {
                // 包含非活動狀態的物件一起搜尋，因為 TinderSwipeManager 預設是 inactive
                cachedTinderManager = Object.FindFirstObjectByType<TinderSwipeManager>(FindObjectsInactive.Include);
            }
            return cachedTinderManager != null && cachedTinderManager.gameObject.activeInHierarchy && !cachedTinderManager.IsClosing;
        }

        /// <summary>
        /// 釋放主動技能
        /// </summary>
        private void TriggerActiveSkill()
        {
            if (activeSkill == null) return;

            // 啟動冷卻時間
            activeCDDuration = activeSkill.cooldown;
            activeCDTimer = activeCDDuration;

            // 同步 HUD 轉圈
            SkillUIManager ui = Object.FindFirstObjectByType<SkillUIManager>();
            if (ui != null)
            {
                ui.TriggerSkillCooldown(0, activeCDDuration);
            }

            Debug.Log($"【技能系統】✅ 釋放主動技能：{activeSkill.skillName}（ID: {activeSkill.skillID}）！");

            if (activeSkill.skillID == "SSR_Angel")
            {
                // 天使金光菇主動：回復最大血量 50%
                PlayerHealth hp = GetComponent<PlayerHealth>();
                if (hp == null) hp = Object.FindFirstObjectByType<PlayerHealth>();
                if (hp != null)
                {
                    int healAmt = Mathf.RoundToInt(hp.MaxHealth * 0.5f);
                    hp.Heal(healAmt);
                    Debug.Log($"【技能系統】天使金光菇：觸發主動，回復最大生命值 50%（共 {healAmt} 點）！");
                }
            }
            else
            {
                // 其他主動技能（黑木耳、珊瑚菇、猴頭菇）：持續時間
                isActiveSkillEffectRunning = true;
                float duration = activeSkill != null ? activeSkill.activeDuration : 10f;
                activeSkillEffectDurationTimer = duration;
                Debug.Log($"【技能系統】{activeSkill.skillName} 效果啟動，持續 {duration} 秒。");
            }
        }

        private void SyncSkillLevelUI(string skillID, int level)
        {
            SkillUIManager ui = Object.FindFirstObjectByType<SkillUIManager>();
            if (ui == null) return;

            // 檢查主動
            if (activeSkill != null && activeSkill.skillID == skillID)
            {
                ui.UpgradeSkillLevel(0, level);
                return;
            }

            // 檢查被動
            for (int i = 0; i < passiveSkills.Count; i++)
            {
                if (passiveSkills[i].skillID == skillID)
                {
                    ui.UpgradeSkillLevel(i + 1, level);
                    return;
                }
            }
        }

        /// <summary>
        /// 取得已獲得的技能資料物件
        /// </summary>
        public SkillData GetSkillData(string skillID)
        {
            if (activeSkill != null && activeSkill.skillID == skillID)
            {
                return activeSkill;
            }
            foreach (var passive in passiveSkills)
            {
                if (passive != null && passive.skillID == skillID)
                {
                    return passive;
                }
            }
            return null;
        }

        /// <summary>
        /// 取得技能等級
        /// </summary>
        public int GetSkillLevel(string skillID)
        {
            return skillLevels.TryGetValue(skillID, out int lvl) ? lvl : 0;
        }

        /// <summary>
        /// 檢查技能是否已達滿級
        /// </summary>
        public bool IsSkillAtMaxLevel(SkillData skill)
        {
            if (skill == null) return true;
            int currentLevel = GetSkillLevel(skill.skillID);
            return currentLevel >= skill.maxLevel;
        }

        /// <summary>
        /// 檢查是否已取得過 SSR (天使金光菇)
        /// </summary>
        public bool HasAcquiredSSR()
        {
            return hasAcquiredSSR;
        }

        /// <summary>
        /// 取得或升級技能 (由 TinderSwipeManager 右滑 Like 時呼叫)
        /// </summary>
        public void AcquireOrUpgradeSkill(SkillData skill)
        {
            if (skill == null)
            {
                Debug.LogError("【技能系統】AcquireOrUpgradeSkill 傳入的 skill 為 null！");
                return;
            }

            int currentLevel = GetSkillLevel(skill.skillID);
            if (currentLevel >= skill.maxLevel)
            {
                Debug.LogWarning($"【技能系統】技能 {skill.skillName} 已達最高等級 LV.{skill.maxLevel}！");
                return;
            }

            int nextLevel = currentLevel + 1;
            skillLevels[skill.skillID] = nextLevel;
            Debug.Log($"【技能系統】✅ 獲得/升級技能：{skill.skillName} (ID: {skill.skillID}) | LV {currentLevel} → {nextLevel} | 主動:{skill.isActive} | 稀有度:{skill.rarity}");

            SkillUIManager ui = Object.FindFirstObjectByType<SkillUIManager>();
            if (ui == null)
            {
                Debug.LogWarning("【技能系統】找不到 SkillUIManager！icon 無法更新。");
            }

            // ── 判斷是主動還是被動技能 ──
            bool isActiveskill = skill.isActive || skill.rarity == Rarity.SSR;

            if (isActiveskill)
            {
                if (skill.rarity == Rarity.SSR)
                {
                    hasAcquiredSSR = true;
                    nextLevel = skill.maxLevel;
                    skillLevels[skill.skillID] = nextLevel;
                    Debug.Log($"【技能系統】獲得 SSR {skill.skillName}，直接提升至最大等級 LV.{skill.maxLevel}！");
                }

                // 放入主動技能槽 (Slot 0)
                if (activeSkill == null || activeSkill.skillID != skill.skillID)
                {
                    if (activeSkill != null)
                    {
                        skillLevels.Remove(activeSkill.skillID);
                        Debug.Log($"【技能系統】移除舊主動技能等級記錄：{activeSkill.skillID}");
                    }
                    activeSkill = skill;
                    Debug.Log($"【技能系統】主動技能已設定為：{skill.skillName}");
                }

                // 再次選取主動技能，冷卻時間會重置
                activeCDTimer = 0f;
                Debug.Log($"【技能系統】主動技能冷卻重置為 0f！");

                if (ui != null)
                {
                    Sprite icon = skill.avatarSprite != null ? skill.avatarSprite : skill.hudIcon;
                    Debug.Log($"【技能系統】設定主動技能 icon，sprite: {(icon != null ? icon.name : "null")}");
                    ui.SetSkillIcon(0, icon);
                    ui.UpgradeSkillLevel(0, nextLevel);
                    ui.TriggerSkillCooldown(0, 0f);
                }
            }
            else
            {
                // 處理被動技能，放入 Slot 1 ~ 3
                int passiveIndex = -1;
                for (int i = 0; i < passiveSkills.Count; i++)
                {
                    if (passiveSkills[i].skillID == skill.skillID)
                    {
                        passiveIndex = i;
                        break;
                    }
                }

                if (passiveIndex != -1)
                {
                    // 已持有，升級
                    Debug.Log($"【技能系統】升級被動技能 {skill.skillName}，Slot {passiveIndex + 1}");
                    if (ui != null)
                    {
                        ui.UpgradeSkillLevel(passiveIndex + 1, nextLevel);
                    }
                }
                else
                {
                    // 新獲得
                    if (passiveSkills.Count < 3)
                    {
                        passiveSkills.Add(skill);
                        passiveIndex = passiveSkills.Count - 1;
                        Debug.Log($"【技能系統】新增被動技能 {skill.skillName}，放入 Slot {passiveIndex + 1}");

                        if (ui != null)
                        {
                            Sprite icon = skill.avatarSprite != null ? skill.avatarSprite : skill.hudIcon;
                            Debug.Log($"【技能系統】設定被動技能 icon，sprite: {(icon != null ? icon.name : "null")}");
                            ui.SetSkillIcon(passiveIndex + 1, icon);
                            ui.UpgradeSkillLevel(passiveIndex + 1, nextLevel);
                        }
                    }
                    else
                    {
                        // 被動技能欄位已滿 (3/3)，隨機取代一個被動技能
                        int replacedIndex = Random.Range(0, 3);
                        SkillData replacedSkill = passiveSkills[replacedIndex];
                        Debug.Log($"【技能系統】被動技能欄位已滿（3/3）！隨機取代 Slot {replacedIndex + 1} 的被動技能 {replacedSkill.skillName} 為新被動技能 {skill.skillName}");

                        // 清除舊技能的等級記錄
                        skillLevels.Remove(replacedSkill.skillID);

                        // 替換被動技能
                        passiveSkills[replacedIndex] = skill;
                        passiveIndex = replacedIndex;

                        if (ui != null)
                        {
                            Sprite icon = skill.avatarSprite != null ? skill.avatarSprite : skill.hudIcon;
                            Debug.Log($"【技能系統】設定被動技能 icon，sprite: {(icon != null ? icon.name : "null")}");
                            ui.SetSkillIcon(passiveIndex + 1, icon);
                            ui.UpgradeSkillLevel(passiveIndex + 1, nextLevel);
                        }
                    }
                }
            }
        }
    }
}
