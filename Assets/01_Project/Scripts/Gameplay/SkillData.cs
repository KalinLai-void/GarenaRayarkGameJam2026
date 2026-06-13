using UnityEngine;

namespace Gameplay
{
    public enum Rarity
    {
        R,
        SR,
        SSR
    }

    [CreateAssetMenu(fileName = "NewSkillData", menuName = "Gameplay/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("--- Basic Info ---")]
        public string skillID;
        public string skillName;
        public Rarity rarity;
        public string skillTitle;
        
        [TextArea(3, 5)]
        public string description;
        
        public string flavorText;

        [Header("--- Visual Assets ---")]
        public Sprite avatarSprite;
        public Sprite rarityBackground;
        public Sprite rarityTag;
        public Sprite hudIcon;

        [Header("--- Gameplay Settings ---")]
        [Tooltip("勾選表示此為主動技能（由 Space 觸發），不勾選則為被動技能")]
        public bool isActive = false; // true = 主動技能 (放到 Slot 0)，false = 被動
        public float cooldown; // 主動技能的冷卻時間（秒），被動填 0
        public int maxLevel = 5;
    }
}

