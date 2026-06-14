using UnityEngine;

namespace Gameplay
{
    public sealed class MonkeyBomb : MonoBehaviour
    {
        private int level;
        private int baseBulletDamage;
        private float explosionRadius = 2.0f;
        private float delay = 0.6f;

        public void Initialize(int skillLevel, int bulletDamage)
        {
            level = skillLevel;
            baseBulletDamage = bulletDamage;

            explosionRadius = 2.0f;
            delay = 0.6f;

            if (PlayerSkillSystem.Instance != null)
            {
                SkillData monkeyData = PlayerSkillSystem.Instance.GetSkillData("SR_MonkeyHead");
                if (monkeyData != null)
                {
                    delay = monkeyData.monkeyBombDelay;
                    if (monkeyData.monkeyExplosionRadius != null && level <= monkeyData.monkeyExplosionRadius.Length && level > 0)
                    {
                        explosionRadius = monkeyData.monkeyExplosionRadius[level - 1];
                    }
                }
            }

            // 繪製橘紅色圓形炸彈貼圖
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - 15.5f;
                    float dy = y - 15.5f;
                    if (dx * dx + dy * dy <= 14f * 14f)
                    {
                        tex.SetPixel(x, y, new Color(0.9f, 0.3f, 0.1f, 0.9f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

            Invoke(nameof(Explode), delay);
        }

        private void Explode()
        {
            // 🎵 播放炸彈/爆炸音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBomb();
            }

            // 搜尋範圍內的所有敵人
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            
            // 爆炸傷害：LV.1 = 110%，每次升級 +10%
            float damagePercent = 1.10f + 0.10f * (level - 1);
            if (PlayerSkillSystem.Instance != null)
            {
                SkillData monkeyData = PlayerSkillSystem.Instance.GetSkillData("SR_MonkeyHead");
                if (monkeyData != null && monkeyData.monkeyDamagePercent != null && level <= monkeyData.monkeyDamagePercent.Length && level > 0)
                {
                    damagePercent = monkeyData.monkeyDamagePercent[level - 1];
                }
            }
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseBulletDamage * damagePercent));

            foreach (var col in colliders)
            {
                EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy == null)
                {
                    enemy = col.GetComponent<EnemyHealth>();
                }

                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    enemy.ApplyBurnVisualOnly(0.3f); // 爆炸受擊閃紅
                }
            }

            // 爆炸擴張的簡單視覺效果
            transform.localScale = Vector3.one * explosionRadius * 2f;
            GetComponent<SpriteRenderer>().color = new Color(1f, 0.6f, 0f, 0.4f);
            Destroy(gameObject, 0.15f);
        }
    }
}
