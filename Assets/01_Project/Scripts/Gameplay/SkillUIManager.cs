using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gameplay
{
    /// <summary>
    /// UI 總管腳本，管理左上生命值與能力階級（第一格為主動技能，其餘為被動能力）、右上持有道具欄（從右上往左排）。
    /// </summary>
    public sealed class SkillUIManager : MonoBehaviour
    {
        [Header("--- 左上角：角色狀態與生命值 (Player Status HUD) ---")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("--- 左上角：技能與能力格子 (第一格為主動技能，2~4為被動) ---")]
        [SerializeField] private TextMeshProUGUI[] skillLevelTexts = new TextMeshProUGUI[4]; // 0:主動, 1~3:被動

        [Header("--- 主動技能冷卻 (對應第一格) ---")]
        [SerializeField] private Image activeCooldownMask;
        [SerializeField] private TextMeshProUGUI activeCooldownText;
        
        private float activeCooldownDuration;
        private float activeCooldownTimer;
        private bool isActiveOnCooldown;

        [Header("--- 右上角：持有道具欄 (Inventory Quick-View) ---")]
        [SerializeField] private Transform inventoryIconContainer; // 掛有 Horizontal Layout Group 的容器
        [SerializeField] private GameObject miniIconPrefab;       // 小圖示 Prefab

        private void Start()
        {
            // 初始化：隱藏冷卻遮罩與文字
            if (activeCooldownMask != null)
            {
                activeCooldownMask.gameObject.SetActive(false);
            }
            if (activeCooldownText != null)
            {
                activeCooldownText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // 處理主動技能冷卻倒數與 FillAmount 更新
            if (isActiveOnCooldown)
            {
                activeCooldownTimer -= Time.deltaTime;
                
                if (activeCooldownTimer <= 0f)
                {
                    // 冷卻結束
                    isActiveOnCooldown = false;
                    activeCooldownTimer = 0f;
                    
                    if (activeCooldownMask != null)
                    {
                        activeCooldownMask.fillAmount = 0f;
                        activeCooldownMask.gameObject.SetActive(false);
                    }
                    if (activeCooldownText != null)
                    {
                        activeCooldownText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // 更新冷卻遮罩與文字
                    if (activeCooldownMask != null)
                    {
                        activeCooldownMask.fillAmount = activeCooldownTimer / activeCooldownDuration;
                    }
                    if (activeCooldownText != null)
                    {
                        activeCooldownText.text = Mathf.CeilToInt(activeCooldownTimer).ToString();
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
        /// 升級對應 Index 的能力文字 (0:主動技能, 1~3:被動技能)
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
        /// 觸發主動技能冷卻 (不論傳入何 slotIndex，均對應左上第一格)
        /// </summary>
        public void TriggerSkillCooldown(int slotIndex, float duration)
        {
            activeCooldownDuration = duration;
            activeCooldownTimer = duration;
            isActiveOnCooldown = true;

            if (activeCooldownMask != null)
            {
                activeCooldownMask.gameObject.SetActive(true);
                activeCooldownMask.fillAmount = 1f;
            }
            if (activeCooldownText != null)
            {
                activeCooldownText.gameObject.SetActive(true);
                activeCooldownText.text = Mathf.CeilToInt(duration).ToString();
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
