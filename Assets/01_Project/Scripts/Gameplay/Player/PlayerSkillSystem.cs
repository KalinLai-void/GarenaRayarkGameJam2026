using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    /// <summary>
    /// 玩家技能管理器，負責記錄技能等級與冷卻。
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
            }
            else
            {
                Destroy(gameObject);
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
                    Debug.Log($"【技能系統】主動技能 {activeSkill.skillName} 狀態效果已結束。");
                }
            }

            // 監聽空白鍵觸發主動技能
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (activeSkill != null && activeCDTimer <= 0f)
                {
                    TriggerActiveSkill();
                }
            }
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

            Debug.Log($"【技能系統】釋放主動技能：{activeSkill.skillName}！");

            if (activeSkill.skillID == "SSR_Angel")
            {
                // 天使金光菇特殊邏輯：
                // 1. 取得除了自身以外，所有等級等於 LV.1 的被動或主動技能
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
                    // 強行將等級最低的技能提升至 LV.2，此時不回血
                    foreach (var id in lv1SkillIDs)
                    {
                        skillLevels[id] = 2;
                        Debug.Log($"【技能系統】天使金光菇：強行將技能 {id} 等級提升至 LV.2");
                        
                        // 同步更新 UI 等級
                        SyncSkillLevelUI(id, 2);
                    }
                }
                else
                {
                    // 若無任何 LV.1 技能，則回血 50%
                    PlayerHealth hp = GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.Heal(50); // 預設最大血量 100，回血 50% 即 50 點
                        Debug.Log("【技能系統】天使金光菇：玩家技能均已 >= LV.2，觸發治癒回血 50%！");
                    }
                }
            }
            else
            {
                // 其他主動技能（黑木耳、珊瑚菇、猴頭菇）：持續時間 10 秒
                isActiveSkillEffectRunning = true;
                activeSkillEffectDurationTimer = 10f;
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
            if (skill == null) return;

            int currentLevel = GetSkillLevel(skill.skillID);
            if (currentLevel >= skill.maxLevel)
            {
                Debug.LogWarning($"【技能系統】技能 {skill.skillName} 已達最高等級！");
                return;
            }

            int nextLevel = currentLevel + 1;
            skillLevels[skill.skillID] = nextLevel;
            Debug.Log($"【技能系統】已選擇/升級技能：{skill.skillName} | 等級 {currentLevel} -> {nextLevel}");

            SkillUIManager ui = Object.FindFirstObjectByType<SkillUIManager>();

            // 處理主動技能
            if (skill.cooldown > 0f || skill.rarity == Rarity.SSR)
            {
                if (skill.rarity == Rarity.SSR)
                {
                    hasAcquiredSSR = true;
                    // SSR 恆定為最高等級 (LV.5)
                    nextLevel = skill.maxLevel;
                    skillLevels[skill.skillID] = nextLevel;
                    Debug.Log($"【技能系統】獲得 SSR 天使金光菇，直接強行提升至最大等級 LV.{skill.maxLevel}，整局僅此一次！");
                }

                // 若為主動，放入 Slot 0
                if (activeSkill == null || activeSkill.skillID != skill.skillID)
                {
                    activeSkill = skill;
                }

                if (ui != null)
                {
                    // 使用配對到的角色圖片做為 icon
                    ui.SetSkillIcon(0, skill.avatarSprite);
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

                        if (ui != null)
                        {
                            // 使用配對到的角色圖片做為 icon
                            ui.SetSkillIcon(passiveIndex + 1, skill.avatarSprite);
                            ui.UpgradeSkillLevel(passiveIndex + 1, nextLevel);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("【技能系統】被動技能欄位已滿！無法獲得新技能。");
                    }
                }
            }
        }
    }
}
