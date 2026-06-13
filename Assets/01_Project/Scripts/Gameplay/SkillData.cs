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

        [Header("--- Configurable Skill Parameters ---")]
        [Tooltip("主動技能效果持續時間（秒，如黑木耳、珊瑚菇、猴頭菇持續 10 秒）")]
        public float activeDuration = 10f;

        [Header("針葉菇 (R_NeedleMushroom) 被動")]
        [Tooltip("射速加成比例（依等級，如 LV1=0.15, LV2=0.20...）")]
        public float[] needleFireRateBonusPercent = { 0.15f, 0.20f, 0.25f, 0.30f, 0.35f };
        [Tooltip("子彈初速乘數（依等級，如 LV1=1.10）")]
        public float[] needleBulletSpeedMultiplier = { 1.10f, 1.10f, 1.10f, 1.10f, 1.10f };

        [Header("白精靈菇 (R_WhiteElf) 被動")]
        [Tooltip("發射子彈數量（依等級，如 LV1=2, LV2=3...）")]
        public int[] whiteElfBulletCount = { 2, 3, 4, 5, 6 };
        [Tooltip("發射子彈扇形擴張角度（依等級，如 LV1=30, LV2=30...）")]
        public float[] whiteElfSpreadAngle = { 30f, 30f, 30f, 45f, 45f };

        [Header("杏鮑菇 (R_KingOyster) 被動")]
        [Tooltip("攻擊力加成比例（依等級，如 LV1=0.05, LV2=0.10...）")]
        public float[] kingOysterDamageBonusPercent = { 0.05f, 0.10f, 0.15f, 0.20f, 0.25f };
        [Tooltip("子彈體積加成比例（依等級，如 LV1=0.10, LV2=0.20...）")]
        public float[] kingOysterScaleMultiplierBonus = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f };

        [Header("洋菇 (R_ButtonMushroom) 被動")]
        [Tooltip("緩速比例（依等級，如 LV1=0.05, LV2=0.08...）")]
        public float[] buttonSlowPercent = { 0.05f, 0.08f, 0.11f, 0.14f, 0.17f };
        [Tooltip("緩速持續時間（秒）")]
        public float buttonSlowDuration = 2.0f;

        [Header("香菇 (R_Shiitake) 被動")]
        [Tooltip("每跳灼燒傷害佔子彈傷害比例（依等級，如 LV1=0.05, LV2=0.08...）")]
        public float[] shiitakeBurnDotPercent = { 0.05f, 0.08f, 0.11f, 0.14f, 0.17f };
        [Tooltip("灼燒總持續時間（秒）")]
        public float shiitakeBurnDuration = 3.0f;
        [Tooltip("灼燒傷害觸發間隔（秒）")]
        public float shiitakeBurnInterval = 1.0f;

        [Header("黑木耳 (SR_BlackMushroom) 主動")]
        [Tooltip("黏著方塊緩速比例（依等級，如 LV1=0.10, LV2=0.15...）")]
        public float[] blackMushroomSlowPercent = { 0.10f, 0.15f, 0.20f, 0.25f, 0.30f };
        [Tooltip("黏著方塊每跳傷害佔子彈傷害比例（依等級，如 LV1=0.05, LV2=0.10...）")]
        public float[] blackMushroomDotPercent = { 0.05f, 0.10f, 0.15f, 0.20f, 0.25f };
        [Tooltip("黏著方塊存在時間（秒）")]
        public float blackMushroomDuration = 2.0f;

        [Header("珊瑚菇 (SR_CoralMushroom) 主動")]
        [Tooltip("光束模式攻擊力增幅比例（依等級，如 LV1=0.10, LV2=0.20...）")]
        public float[] coralDamageBonusPercent = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f };
        [Tooltip("光束子彈寬度 (X軸)")]
        public float coralBeamWidth = 3.5f;
        [Tooltip("光束子彈高度 (Y軸)")]
        public float coralBeamHeight = 0.8f;
        [Tooltip("珊瑚菇主動：光束殘留持續時間（秒）")]
        public float coralBeamDuration = 1.0f;
        [Tooltip("珊瑚菇主動：光束殘留每跳傷害比例（相較於子彈傷害）")]
        public float coralBeamDotPercent = 1.0f;
        [Tooltip("珊瑚菇主動：光束殘留傷害觸發間隔（秒）")]
        public float coralBeamDotInterval = 0.25f;

        [Header("猴頭菇 (SR_MonkeyHead) 主動")]
        [Tooltip("爆炸半徑（依等級，如 LV1=2.0, LV2=2.2...）")]
        public float[] monkeyExplosionRadius = { 2.0f, 2.2f, 2.4f, 2.6f, 2.8f };
        [Tooltip("爆炸延遲時間（秒）")]
        public float monkeyBombDelay = 0.6f;
        [Tooltip("爆炸傷害佔武器最終攻擊力比例（依等級，如 LV1=1.10, LV2=1.20...）")]
        public float[] monkeyDamagePercent = { 1.10f, 1.20f, 1.30f, 1.40f, 1.50f };

        [Header("天使金光菇 (SSR_Angel) 主動")]
        [Tooltip("治癒生命值數量（若無LV.1技能時觸發）")]
        public int angelHealAmount = 50;
    }
}

