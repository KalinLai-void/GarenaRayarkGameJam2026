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

            // 監聽空白鍵觸發主動技能
            // ⚠️ 若 Tinder UI 正開著，Space 是 Submit 鍵會觸發 Like 按鈕，此時不觸發技能
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !IsTinderUIOpen())
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
            return cachedTinderManager != null && cachedTinderManager.gameObject.activeInHierarchy;
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
                // 天使金光菇特殊邏輯：
                // 取得除了自身以外，所有等級等於 LV.1 的技能
                List<string> lv1SkillIDs = new List<string>();
                foreach (var kvp in skillLevels)
                {
                    if (kvp.Key != "SSR_Angel" && kvp.Value == 1)
                    {
                        lv1SkillIDs.Add(kvp.Key);
                    }
                }

                if (lv1SkillIDs.Count > 0)
                {
                    foreach (var id in lv1SkillIDs)
                    {
                        skillLevels[id] = 2;
                        Debug.Log($"【技能系統】天使金光菇：強行將技能 {id} 等級提升至 LV.2");
                        SyncSkillLevelUI(id, 2);
                    }
                }
                else
                {
                    // 若無任何 LV.1 技能，則回血 50%
                    PlayerHealth hp = GetComponent<PlayerHealth>();
                    if (hp == null) hp = Object.FindFirstObjectByType<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.Heal(50);
                        Debug.Log("【技能系統】天使金光菇：玩家技能均已 >= LV.2，觸發治癒回血 50%！");
                    }
                }
            }
            else
            {
                // 其他主動技能（黑木耳、珊瑚菇、猴頭菇）：持續時間 10 秒
                isActiveSkillEffectRunning = true;
                activeSkillEffectDurationTimer = 10f;
                Debug.Log($"【技能系統】{activeSkill.skillName} 效果啟動，持續 10 秒。");
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
                    activeSkill = skill;
                    Debug.Log($"【技能系統】主動技能已設定為：{skill.skillName}");
                }

                if (ui != null)
                {
                    Sprite icon = skill.avatarSprite != null ? skill.avatarSprite : skill.hudIcon;
                    Debug.Log($"【技能系統】設定主動技能 icon，sprite: {(icon != null ? icon.name : "null")}");
                    ui.SetSkillIcon(0, icon);
                    ui.UpgradeSkillLevel(0, nextLevel);
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
                        Debug.LogWarning("【技能系統】被動技能欄位已滿（3/3）！無法獲得新被動技能。");
                    }
                }
            }
        }
    }
}
