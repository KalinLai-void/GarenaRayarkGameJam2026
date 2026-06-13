using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gameplay
{
    /// <summary>
    /// 控制 Tinder 卡牌上的文字與圖像顯示的腳本
    /// </summary>
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
        }
    }
}
