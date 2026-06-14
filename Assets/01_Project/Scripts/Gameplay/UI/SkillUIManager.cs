using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gameplay
{
    /// <summary>
    /// UI 總管腳本，管理左上生命值與技能圖示（第一格為主動技能，其餘為被動能力）。
    /// 
    /// 【重要】在 Inspector 中，請將以下欄位指定完畢：
    ///   - Skill Icon Images[0]  → Slot_Active_Skill 下的 Skill_Icon（Image 元件）
    ///   - Skill Icon Images[1~3] → Slot_Passive_Skill 下的 Skill_Icon（Image 元件）
    ///   - Skill Level Texts[0~3] → 對應 LV_Text（TextMeshProUGUI 元件）
    ///   - Active Cooldown Mask  → Slot_Active_Skill 下的 CooldownMask（Image 元件）
    ///   - Active Cooldown Text  → Slot_Active_Skill 下的 CooldownText（TextMeshProUGUI 元件）
    /// </summary>
    public sealed class SkillUIManager : MonoBehaviour
    {
        [Header("--- 左上角：角色狀態與生命值 (Player Status HUD) ---")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("--- 技能格子 Icon（由 Inspector 直接指定，不用 GameObject.Find）---")]
        [Tooltip("Slot 0 = 主動技能 Skill_Icon，Slot 1~3 = 被動技能 Skill_Icon")]
        [SerializeField] private Image[] skillIconImages = new Image[4];

        [Header("--- 技能等級文字（由 Inspector 直接指定）---")]
        [Tooltip("Slot 0 = 主動技能 LV_Text，Slot 1~3 = 被動技能 LV_Text")]
        [SerializeField] private TextMeshProUGUI[] skillLevelTexts = new TextMeshProUGUI[4];

        [Header("--- 主動技能冷卻 (對應第一格) ---")]
        [SerializeField] private Image activeCooldownMask;
        [SerializeField] private TextMeshProUGUI activeCooldownText;
        
        private float activeCooldownDuration;
        private float activeCooldownTimer;
        private bool isActiveOnCooldown;

        [Header("--- 右上角：持有道具欄 (Inventory Quick-View) ---")]
        [SerializeField] private Transform inventoryIconContainer;
        [SerializeField] private GameObject miniIconPrefab;

        [Header("--- 下方：經驗值 UI 組件與外觀設計 ---")]
        [SerializeField] private Slider xpSlider;
        [SerializeField] private Color xpBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        [SerializeField] private Color xpFillColor = new Color(0f, 0.88f, 0.88f, 1f);
        [SerializeField] private float xpBarHeight = 15f;

        public Color XPBackgroundColor => xpBackgroundColor;
        public Color XPFillColor => xpFillColor;
        public float XPBarHeight => xpBarHeight;

        private void Awake()
        {
            // 初始化：將所有技能 icon 隱藏並清除 sprite，防止 prefab 預設圖片顯示
            for (int i = 0; i < skillIconImages.Length; i++)
            {
                if (skillIconImages[i] != null)
                {
                    skillIconImages[i].sprite = null;           // 清除預設 sprite
                    skillIconImages[i].color = Color.clear;    // 完全透明
                    skillIconImages[i].gameObject.SetActive(false);
                    Debug.Log($"【SkillUI】Slot {i} icon 初始化隱藏。物件: {skillIconImages[i].gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"【SkillUI】⚠️ Slot {i} 的 skillIconImages[{i}] 未在 Inspector 指定！請拖入對應的 Skill_Icon Image 元件。");
                }
            }
        }

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

            // 確認 PlayerSkillSystem 存在
            if (PlayerSkillSystem.Instance == null)
            {
                Debug.LogError("【SkillUI】❌ PlayerSkillSystem.Instance 為 null！請確認場景中有 GameObject 掛載 PlayerSkillSystem 腳本（通常掛在 Player 上）。");
            }
            else
            {
                Debug.Log($"【SkillUI】✅ PlayerSkillSystem 已連接，掛載於：{PlayerSkillSystem.Instance.gameObject.name}");
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
                    skillLevelTexts[index].text = $"{nextLevel}";
                    Debug.Log($"【SkillUI】Slot {index} 等級更新為 LV {nextLevel}");
                }
                else
                {
                    Debug.LogWarning($"【SkillUI】⚠️ skillLevelTexts[{index}] 未指定，無法更新等級文字。");
                }
            }
        }

        /// <summary>
        /// 觸發主動技能冷卻
        /// </summary>
        public void TriggerSkillCooldown(int slotIndex, float duration)
        {
            if (duration <= 0f)
            {
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
                return;
            }

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
                    img.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 公開方法：動態更新底部經驗值條
        /// </summary>
        public void UpdateXPUI(int currentXP, int maxXP)
        {
            if (xpSlider == null) return;
            
            xpSlider.maxValue = maxXP;
            xpSlider.value = currentXP;
        }

        /// <summary>
        /// 設定對應 Index 的技能圖示 (0:主動技能, 1~3:被動技能)
        /// icon 優先用 avatarSprite，未來有独立 icon 再改為 hudIcon
        /// </summary>
        public void SetSkillIcon(int index, Sprite icon)
        {
            if (index < 0 || index >= skillIconImages.Length)
            {
                Debug.LogError($"【SkillUI】SetSkillIcon index {index} 超出範圍！");
                return;
            }

            Image img = skillIconImages[index];
            if (img == null)
            {
                Debug.LogError($"【SkillUI】❌ skillIconImages[{index}] 為 null！請在 Inspector 中指定 Slot {index} 對應的 Skill_Icon Image 元件。");
                return;
            }

            if (icon != null)
            {
                img.sprite = icon;
                img.color = Color.white;   // 確保不是透明狀態
                img.gameObject.SetActive(true);
                img.enabled = true;
                Debug.Log($"【SkillUI】✅ Slot {index} icon 已設定為 [{icon.name}]，GameObject: {img.gameObject.name}");
            }
            else
            {
                img.sprite = null;
                img.color = Color.clear;
                img.gameObject.SetActive(false);
                img.enabled = false;
                Debug.LogWarning($"【SkillUI】Slot {index} icon 傳入的 Sprite 為 null，技能的 avatarSprite 欄位可能未設定！");
            }
        }
    }
}
