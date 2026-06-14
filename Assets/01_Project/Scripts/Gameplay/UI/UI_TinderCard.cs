using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gameplay
{
    /// <summary>
    /// 控制 Tinder 卡牌上的文字與圖像顯示的腳本
    /// </summary>
    [ExecuteAlways]
    public sealed class UI_TinderCard : MonoBehaviour
    {
        [Header("--- UI 圖像元件 ---")]
        [SerializeField] private Image rareBackground;
        [SerializeField] private Image rareImage;
        [SerializeField] private Image avatarImage;

        [Header("--- UI 文字元件 ---")]
        [SerializeField] private TextMeshProUGUI avatarHashTagText;
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI skillDescText;
 
        [Header("--- 編輯器預覽 (僅供開發預覽用) ---")]
        [SerializeField] private SkillData previewSkillData;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 在編輯器數值變更或拖入 SkillData 時自動預覽效果
            if (previewSkillData != null)
            {
                Setup(previewSkillData);
            }
        }

        private void Update()
        {
            // 如果是在編輯器非 Play 模式下，每當修改 SkillData 時也會即時反應
            if (!Application.isPlaying && previewSkillData != null)
            {
                Setup(previewSkillData);
            }
        }
#endif

        /// <summary>
        /// 填入技能資料並渲染卡面
        /// </summary>
        public void Setup(SkillData data)
        {
            if (data == null) return;

            // 設定圖像
            if (rareBackground != null) rareBackground.sprite = data.rarityBackground;
            if (rareImage != null) rareImage.sprite = data.rarityTag;
            if (avatarImage != null) avatarImage.sprite = data.avatarSprite;

            // 設定文字
            if (avatarHashTagText != null) avatarHashTagText.text = data.flavorText;
            if (skillNameText != null) skillNameText.text = data.skillName;

            if (skillDescText != null)
            {
                // 主動技能會顯示冷卻時間，被動則不顯示
                string cdStr = data.cooldown > 0f ? $" (冷卻 {data.cooldown}秒)" : "";
                skillDescText.text = $"<b>{data.skillTitle}</b>{cdStr}\n{data.description}";
            }

            // 🌟 動態調整 Avatar_Border (頭像外框) 尺寸、偏移與縮放
            if (avatarImage != null)
            {
                Transform avatarBorderT = avatarImage.transform.parent; // Avatar_Image 的 parent 是 Avatar_Border
                if (avatarBorderT != null)
                {
                    RectTransform borderRt = avatarBorderT.GetComponent<RectTransform>();
                    if (borderRt != null)
                    {
                        if (data.useCustomAvatarSize)
                        {
                            borderRt.anchorMin = new Vector2(0.5f, 0.5f);
                            borderRt.anchorMax = new Vector2(0.5f, 0.5f);
                            borderRt.sizeDelta = data.customAvatarSize;
                            borderRt.anchoredPosition = data.customAvatarOffset;
                        }
                        avatarBorderT.localScale = data.customAvatarScale;
                    }
                }
            }

            // 🌟 動態調整 Rare_Image_Area (稀有度徽章區) 尺寸、偏移與縮放
            if (rareImage != null)
            {
                Transform rareImageAreaT = rareImage.transform.parent; // Rare_Image 的 parent 是 Rare_Image_Area
                if (rareImageAreaT != null)
                {
                    RectTransform areaRt = rareImageAreaT.GetComponent<RectTransform>();
                    if (areaRt != null)
                    {
                        if (data.useCustomRareImageSize)
                        {
                            areaRt.anchorMin = new Vector2(0.5f, 0.5f);
                            areaRt.anchorMax = new Vector2(0.5f, 0.5f);
                            areaRt.sizeDelta = data.customRareImageSize;
                            areaRt.anchoredPosition = data.customRareImageOffset;
                        }
                        rareImageAreaT.localScale = data.customRareImageScale;
                    }
                }
            }
        }
    }
}
