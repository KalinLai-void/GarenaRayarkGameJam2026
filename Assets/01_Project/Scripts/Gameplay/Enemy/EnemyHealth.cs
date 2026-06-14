using System.Collections;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 怪物狀態 Enum
    /// </summary>
    public enum EnemyState
    {
        Normal,
        Hurt,
        Dead
    }

    /// <summary>
    /// 管理怪物生命值、受傷視覺回饋 (閃紅) 與死亡銷毀，包含內建狀態機。
    /// </summary>
    public sealed class EnemyHealth : MonoBehaviour
    {
        [Header("【怪物生命設定】")]
        [Tooltip("怪物最大生命值，預設為 3")]
        [SerializeField] private int maxHealth = 3;

        [Tooltip("擊殺此怪物可獲得的經驗值，預設為 2")]
        [SerializeField] private int xpReward = 2;

        [Header("【受傷視覺反饋】")]
        [Tooltip("受傷紅閃持續時間 (秒)")]
        [SerializeField] private float flashDuration = 0.15f;

        [Tooltip("受傷時閃爍 the 顏色")]
        [SerializeField] private Color flashColor = Color.red;

        /// <summary>
        /// 全域當前存活的怪物總數 (靜態變數)
        /// </summary>
        public static int ActiveEnemyCount { get; private set; }

        /// <summary>
        /// 當前生命值
        /// </summary>
        public int CurrentHealth => currentHealth;

        /// <summary>
        /// 當前怪物狀態
        /// </summary>
        public EnemyState CurrentState => currentState;

        private int currentHealth;
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;
        private EnemyState currentState = EnemyState.Normal;

        private void OnEnable()
        {
            ActiveEnemyCount++;
        }

        private void OnDisable()
        {
            ActiveEnemyCount--;
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            // 尋找子物件 Visual 上的 SpriteRenderer
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        /// <summary>
        /// 讓怪物扣除生命值並觸發受傷反饋與狀態切換
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (currentState == EnemyState.Dead) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(currentHealth, 0);

            // 🎵 播放敵人受擊受傷音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyHurt();
            }

            // 切換狀態為 Hurt
            currentState = EnemyState.Hurt;
            Debug.Log($"【Console Log】怪物 {gameObject.name} 受到 {damage} 點傷害，剩餘生命值: {currentHealth}，狀態變更為: [Hurt]");

            // 觸發受傷紅閃
            if (spriteRenderer != null)
            {
                if (flashCoroutine != null)
                {
                    StopCoroutine(flashCoroutine);
                }
                flashCoroutine = StartCoroutine(FlashRedRoutine());
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Update()
        {
            // 更新洋菇被動緩速計時器
            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f)
                {
                    EnemyMovement mv = GetComponent<EnemyMovement>();
                    if (mv != null)
                    {
                        mv.RemovePassiveSlow();
                    }
                    UpdateColorTint();
                }
            }
        }

        private float slowTimer = 0f;
        private float slowPercent = 0f;

        public void ApplySlow(int level)
        {
            float percent = 0.05f + 0.03f * (level - 1);
            float duration = 2.0f;

            if (PlayerSkillSystem.Instance != null)
            {
                SkillData buttonData = PlayerSkillSystem.Instance.GetSkillData("R_ButtonMushroom");
                if (buttonData != null)
                {
                    duration = buttonData.buttonSlowDuration;
                    if (buttonData.buttonSlowPercent != null && level <= buttonData.buttonSlowPercent.Length && level > 0)
                    {
                        percent = buttonData.buttonSlowPercent[level - 1];
                    }
                }
            }

            slowPercent = percent;
            slowTimer = duration;

            EnemyMovement mv = GetComponent<EnemyMovement>();
            if (mv != null)
            {
                mv.ApplyPassiveSlow(slowPercent);
            }
            UpdateColorTint();
        }

        public void ApplySlowFromBlock(float amount)
        {
            EnemyMovement mv = GetComponent<EnemyMovement>();
            if (mv != null)
            {
                mv.ApplyBlockSlow(amount);
            }
            UpdateColorTint();
        }

        public void RemoveSlowFromBlock(float amount)
        {
            EnemyMovement mv = GetComponent<EnemyMovement>();
            if (mv != null)
            {
                mv.RemoveBlockSlow(amount);
            }
            UpdateColorTint();
        }

        private Coroutine burnCoroutine;
        private int burnLvl = 0;
        private int burnBaseDamage = 0;

        public void ApplyBurn(int level, int baseDamage)
        {
            burnLvl = level;
            burnBaseDamage = baseDamage;

            if (burnCoroutine != null)
            {
                StopCoroutine(burnCoroutine);
            }
            burnCoroutine = StartCoroutine(BurnRoutine());
        }

        private IEnumerator BurnRoutine()
        {
            UpdateColorTint();
            float duration = 3f;
            float interval = 1f;
            float dotPercent = 0.05f + 0.03f * (burnLvl - 1);

            if (PlayerSkillSystem.Instance != null)
            {
                SkillData shiitakeData = PlayerSkillSystem.Instance.GetSkillData("R_Shiitake");
                if (shiitakeData != null)
                {
                    duration = shiitakeData.shiitakeBurnDuration;
                    interval = shiitakeData.shiitakeBurnInterval;
                    if (shiitakeData.shiitakeBurnDotPercent != null && burnLvl <= shiitakeData.shiitakeBurnDotPercent.Length && burnLvl > 0)
                    {
                        dotPercent = shiitakeData.shiitakeBurnDotPercent[burnLvl - 1];
                    }
                }
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(burnBaseDamage * dotPercent));
            float elapsed = 0f;

            while (elapsed < duration)
            {
                yield return new WaitForSeconds(interval);
                elapsed += interval;
                
                // 扣血 DoT 傷害
                TakeDamage(damage);
                ApplyBurnVisualOnly(0.2f);
            }

            burnCoroutine = null;
            UpdateColorTint();
        }

        private Coroutine burnVisualCoroutine;
        public void ApplyBurnVisualOnly(float duration)
        {
            if (burnVisualCoroutine != null)
            {
                StopCoroutine(burnVisualCoroutine);
            }
            burnVisualCoroutine = StartCoroutine(BurnVisualRoutine(duration));
        }

        private IEnumerator BurnVisualRoutine(float duration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(duration);
                UpdateColorTint();
            }
            burnVisualCoroutine = null;
        }

        private void UpdateColorTint()
        {
            if (spriteRenderer == null) return;
            if (flashCoroutine != null || burnVisualCoroutine != null) return;

            EnemyMovement mv = GetComponent<EnemyMovement>();
            bool isSlowed = mv != null && mv.IsSlowed;
            bool isBurning = burnCoroutine != null;

            if (isBurning)
            {
                spriteRenderer.color = new Color(1.0f, 0.4f, 0.4f, 1f); // 燃燒染紅
            }
            else if (isSlowed)
            {
                spriteRenderer.color = new Color(0.4f, 0.6f, 1.0f, 1f); // 緩速染藍
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }

        private IEnumerator FlashRedRoutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;

            // 閃紅結束若未死亡，狀態切換回 Normal
            if (currentState == EnemyState.Hurt)
            {
                currentState = EnemyState.Normal;
                Debug.Log($"【Console Log】怪物 {gameObject.name} 受傷閃紅結束，狀態恢復為: [Normal]");
            }
            flashCoroutine = null;
            UpdateColorTint();
        }

        private void Die()
        {
            currentState = EnemyState.Dead;
            Debug.Log($"【Console Log】怪物 {gameObject.name} 已被消滅！狀態變更為: [Dead]");

            // 🎵 播放敵人死亡音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyDeath();
            }

            // 檢查玩家猴頭菇主動技能是否啟用，若啟用則在死亡處生成猴頭炸彈
            if (PlayerSkillSystem.Instance != null && 
                PlayerSkillSystem.Instance.IsActiveSkillEffectRunning && 
                PlayerSkillSystem.Instance.ActiveSkill.skillID == "SR_MonkeyHead")
            {
                int lvl = PlayerSkillSystem.Instance.GetSkillLevel("SR_MonkeyHead");
                WeaponController weapon = Object.FindFirstObjectByType<WeaponController>();
                int baseDamage = weapon != null ? weapon.FinalDamage : 10;

                GameObject bomb = new GameObject("MonkeyBomb");
                bomb.transform.position = transform.position;
                MonkeyBomb script = bomb.AddComponent<MonkeyBomb>();
                script.Initialize(lvl, baseDamage);
            }

            // 尋找場景中的玩家經驗系統，將經驗值送過去
            PlayerLevelSystem playerXP = Object.FindFirstObjectByType<PlayerLevelSystem>();
            if (playerXP != null)
            {
                playerXP.AddExperience(xpReward);
            }

            Destroy(gameObject);
        }
    }
}
