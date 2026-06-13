using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 玩家技能管理器，負責記錄技能等級與冷卻。
    /// </summary>
    public sealed class PlayerSkillSystem : MonoBehaviour
    {
        public static PlayerSkillSystem Instance { get; private set; }

        // 紀錄技能的等級 (技能ID -> 等級)
        private readonly Dictionary<string, int> skillLevels = new Dictionary<string, int>();
        
        // 紀錄是否已取得 SSR (天使金光菇)
        private bool hasAcquiredSSR = false;

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

            if (skill.rarity == Rarity.SSR)
            {
                hasAcquiredSSR = true;
                // SSR 恆定為最高等級 (LV.5)
                skillLevels[skill.skillID] = skill.maxLevel;
                Debug.Log($"【技能系統】獲得 SSR 天使金光菇，直接強行提升至最大等級 LV.{skill.maxLevel}，整局僅此一次！");
            }

            // TODO: 同步與更新左上角 HUD 狀態 (SkillUIManager)
        }
    }
}
