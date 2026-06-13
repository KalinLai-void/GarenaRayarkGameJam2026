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
        public float cooldown; // 0 for passive
        public int maxLevel = 5;
    }
}
