using UnityEngine;
using System.Collections.Generic;

namespace Gameplay
{
    public sealed class StickyBlock : MonoBehaviour
    {
        private int level;
        private int baseBulletDamage;
        private float slowAmount;
        private float dotDamagePercent;

        private float dotInterval = 1f;
        private float dotTimer = 0f;

        private readonly List<EnemyHealth> enemiesInRange = new List<EnemyHealth>();

        public void Initialize(int skillLevel, int bulletDamage)
        {
            level = skillLevel;
            baseBulletDamage = bulletDamage;

            float slowAmt = 0.10f + 0.05f * (level - 1);
            float dotPercent = 0.05f + 0.05f * (level - 1);
            float duration = 2f;

            if (PlayerSkillSystem.Instance != null)
            {
                SkillData blackData = PlayerSkillSystem.Instance.GetSkillData("SR_BlackMushroom");
                if (blackData != null)
                {
                    duration = blackData.blackMushroomDuration;
                    if (blackData.blackMushroomSlowPercent != null && level <= blackData.blackMushroomSlowPercent.Length && level > 0)
                    {
                        slowAmt = blackData.blackMushroomSlowPercent[level - 1];
                    }
                    if (blackData.blackMushroomDotPercent != null && level <= blackData.blackMushroomDotPercent.Length && level > 0)
                    {
                        dotPercent = blackData.blackMushroomDotPercent[level - 1];
                    }
                }
            }

            slowAmount = slowAmt;
            dotDamagePercent = dotPercent;

            Destroy(gameObject, duration);
        }

        private void Update()
        {
            dotTimer += Time.deltaTime;
            if (dotTimer >= dotInterval)
            {
                dotTimer = 0f;
                ApplyDotDamage();
            }
        }

        private void ApplyDotDamage()
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseBulletDamage * dotDamagePercent));
            for (int i = enemiesInRange.Count - 1; i >= 0; i--)
            {
                EnemyHealth enemy = enemiesInRange[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    enemy.TakeDamage(damage);
                    enemy.ApplyBurnVisualOnly(0.5f); // 傷害閃紅
                }
                else
                {
                    enemiesInRange.RemoveAt(i);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy == null)
            {
                enemy = other.GetComponent<EnemyHealth>();
            }

            if (enemy != null)
            {
                if (!enemiesInRange.Contains(enemy))
                {
                    enemiesInRange.Add(enemy);
                }
                
                // 施加緩速
                enemy.ApplySlowFromBlock(slowAmount);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy == null)
            {
                enemy = other.GetComponent<EnemyHealth>();
            }

            if (enemy != null)
            {
                if (enemiesInRange.Contains(enemy))
                {
                    enemiesInRange.Remove(enemy);
                }
                
                // 移除緩速
                enemy.RemoveSlowFromBlock(slowAmount);
            }
        }

        private void OnDestroy()
        {
            // 清理所有範圍內敵人的緩速
            foreach (var enemy in enemiesInRange)
            {
                if (enemy != null)
                {
                    enemy.RemoveSlowFromBlock(slowAmount);
                }
            }
        }
    }
}
