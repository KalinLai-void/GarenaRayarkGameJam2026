using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gameplay
{
    /// <summary>
    /// UI 總管腳本，管理左上生命值與能力階級、左下主動戰術技能冷卻、右上持有道具欄。
    /// </summary>
    public sealed class SkillUIManager : MonoBehaviour
    {
        [Header("--- 左上角：角色狀態與技能階級 (Player Status HUD) ---")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI[] skillLevelTexts = new TextMeshProUGUI[4];

        [System.Serializable]
        public struct ActiveSkillUI
        {
            public Image cooldownMask;
            public TextMeshProUGUI cooldownText;
            [HideInInspector] public float cooldownDuration;
            [HideInInspector] public float timer;
            [HideInInspector] public bool isOnCooldown;
        }

        [Header("--- 左下角：主動戰術技能 (Active Skills HUD) ---")]
        [SerializeField] private ActiveSkillUI[] activeSkills = new ActiveSkillUI[2];

        [Header("--- 右上角：持有道具欄 (Inventory Quick-View) ---")]
        [SerializeField] private Transform inventoryIconContainer; // 掛有 Horizontal Layout Group 的容器
        [SerializeField] private GameObject miniIconPrefab;       // 小圖示 Prefab

        private void Start()
        {
            // 初始化：隱藏冷卻遮罩與文字
            for (int i = 0; i < activeSkills.Length; i++)
            {
                if (activeSkills[i].cooldownMask != null)
                {
                    activeSkills[i].cooldownMask.gameObject.SetActive(false);
                }
                if (activeSkills[i].cooldownText != null)
                {
                    activeSkills[i].cooldownText.gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            // 處理技能冷卻倒數與 FillAmount 更新
            for (int i = 0; i < activeSkills.Length; i++)
            {
                if (activeSkills[i].isOnCooldown)
                {
                    activeSkills[i].timer -= Time.deltaTime;
                    
                    if (activeSkills[i].timer <= 0f)
                    {
                        // 冷卻結束
                        activeSkills[i].isOnCooldown = false;
                        activeSkills[i].timer = 0f;
                        
                        if (activeSkills[i].cooldownMask != null)
                        {
                            activeSkills[i].cooldownMask.fillAmount = 0f;
                            activeSkills[i].cooldownMask.gameObject.SetActive(false);
                        }
                        if (activeSkills[i].cooldownText != null)
                        {
                            activeSkills[i].cooldownText.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        // 更新冷卻遮罩與文字
                        if (activeSkills[i].cooldownMask != null)
                        {
                            activeSkills[i].cooldownMask.fillAmount = activeSkills[i].timer / activeSkills[i].cooldownDuration;
                        }
                        if (activeSkills[i].cooldownText != null)
                        {
                            activeSkills[i].cooldownText.text = Mathf.CeilToInt(activeSkills[i].timer).ToString();
                        }
                    }
                }
            }
        }

        // ─── 外部核心數據與事件對接接口 ───

        /// <summary>
        /// 接收玩家生命變更事件，動態更新 Slider 與文字
        /// </summary>
        public void UpdateHP(int current, int max)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = max;
                hpSlider.value = current;
            }
            if (hpText != null)
            {
                hpText.text = $"{current} / {max}";
            }
        }

        /// <summary>
        /// 升級對應 Index 的能力文字 (例如 0: 攻擊力, 1: 防禦力, 2: 多子彈, 3: 攻速)
        /// </summary>
        public void UpgradeSkillLevel(int index, int nextLevel)
        {
            if (index >= 0 && index < skillLevelTexts.Length)
            {
                if (skillLevelTexts[index] != null)
                {
                    skillLevelTexts[index].text = $"LV {nextLevel}";
                }
            }
        }

        /// <summary>
        /// 觸發指定欄位的主動技能冷卻
        /// </summary>
        public void TriggerSkillCooldown(int slotIndex, float duration)
        {
            if (slotIndex >= 0 && slotIndex < activeSkills.Length)
            {
                activeSkills[slotIndex].cooldownDuration = duration;
                activeSkills[slotIndex].timer = duration;
                activeSkills[slotIndex].isOnCooldown = true;

                if (activeSkills[slotIndex].cooldownMask != null)
                {
                    activeSkills[slotIndex].cooldownMask.gameObject.SetActive(true);
                    activeSkills[slotIndex].cooldownMask.fillAmount = 1f;
                }
                if (activeSkills[slotIndex].cooldownText != null)
                {
                    activeSkills[slotIndex].cooldownText.gameObject.SetActive(true);
                    activeSkills[slotIndex].cooldownText.text = Mathf.CeilToInt(duration).ToString();
                }
            }
        }

        /// <summary>
        /// 動態將撿到的道具小圖示加入右上角容器
        /// </summary>
        public void AddInventoryIcon(Sprite itemSprite)
        {
            if (inventoryIconContainer != null && miniIconPrefab != null)
            {
                GameObject newIcon = Instantiate(miniIconPrefab, inventoryIconContainer);
                Image img = newIcon.GetComponent<Image>();
                if (img != null && itemSprite != null)
                {
                    img.sprite = itemSprite;
                }
            }
        }
    }
}
