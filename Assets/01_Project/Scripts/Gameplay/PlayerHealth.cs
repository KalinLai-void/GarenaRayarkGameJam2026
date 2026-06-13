using System.Collections;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 玩家狀態 Enum
    /// </summary>
    public enum PlayerState
    {
        Normal,
        Hurt,
        Dead
    }

    /// <summary>
    /// 管理玩家生命值、受傷視覺回饋 (閃紅) 與死亡邏輯，包含內建狀態機。
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Header("【玩家生命設定】")]
        [Tooltip("最大生命值，預設為 100")]
        [SerializeField] private int maxHealth = 100;

        [Header("【受傷視覺反饋】")]
        [Tooltip("受傷紅閃持續時間 (秒)")]
        [SerializeField] private float flashDuration = 0.15f;

        [Tooltip("受傷時閃爍的顏色")]
        [SerializeField] private Color flashColor = Color.red;

        [Header("【重疊持續傷害設定】")]
        [Tooltip("重疊傷害冷卻時間 (秒)")]
        [SerializeField] private float damageCooldown = 0.5f;
        
        [Tooltip("每次重疊受傷的傷害值")]
        [SerializeField] private int overlapDamage = 10;

        /// <summary>
        /// 當前生命值
        /// </summary>
        public int CurrentHealth => currentHealth;

        /// <summary>
        /// 當前玩家狀態
        /// </summary>
        public PlayerState CurrentState => currentState;

        private int currentHealth;
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;
        private PlayerState currentState = PlayerState.Normal;

        private Collider2D playerCollider;
        private ContactFilter2D enemyFilter;
        private Collider2D[] overlapResults = new Collider2D[10];
        private float damageTimer = 0f;

        private SkillUIManager uiManager;

        private void Awake()
        {
            currentHealth = maxHealth;
            // 尋找子物件 Visual 上的 SpriteRenderer
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            playerCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            // 尋找場景中的 UI 總管
            uiManager = Object.FindFirstObjectByType<SkillUIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateHP(currentHealth, maxHealth);
            }

            // 設定過濾器，只偵測 Enemy Layer 的碰撞體
            enemyFilter = new ContactFilter2D();
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex != -1)
            {
                enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            }
            else
            {
                // 若找不到 Enemy Layer 則使用 Default 作為保險
                enemyFilter.SetLayerMask(LayerMask.GetMask("Default"));
            }
            enemyFilter.useLayerMask = true;
            enemyFilter.useTriggers = true;

            // 初始時將計時器設為滿值，確保一重疊立刻受傷
            damageTimer = damageCooldown;
        }

        private void Update()
        {
            if (currentState == PlayerState.Dead) return;

            // 檢查重疊傷害
            CheckOverlapDamage();
        }

        private void CheckOverlapDamage()
        {
            if (playerCollider == null) return;

            // 偵測與 Player 重疊的 Enemy 碰撞體
            int count = playerCollider.Overlap(enemyFilter, overlapResults);
            if (count > 0)
            {
                damageTimer += Time.deltaTime;
                if (damageTimer >= damageCooldown)
                {
                    TakeDamage(overlapDamage);
                    damageTimer = 0f; // 重置冷卻
                }
            }
            else
            {
                // 沒有重疊時，累加計時器使其維持冷卻就緒狀態
                damageTimer = damageCooldown;
            }
        }

        /// <summary>
        /// 讓玩家扣除生命值並觸發受傷反饋與狀態切換
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (currentState == PlayerState.Dead) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(currentHealth, 0);

            // 更新 HP UI
            if (uiManager != null)
            {
                uiManager.UpdateHP(currentHealth, maxHealth);
            }

            // 切換至 Hurt 狀態
            currentState = PlayerState.Hurt;
            Debug.Log($"【Console Log】玩家受到 {damage} 點傷害，剩餘生命值: {currentHealth}，狀態變更為: [Hurt]");

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

        /// <summary>
        /// 讓玩家回復生命值，上限為 maxHealth
        /// </summary>
        public void Heal(int amount)
        {
            if (currentState == PlayerState.Dead) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            
            Debug.Log($"【Console Log】玩家回復了 {amount} 點生命值，目前生命值: {currentHealth}/{maxHealth}");

            if (uiManager != null)
            {
                uiManager.UpdateHP(currentHealth, maxHealth);
            }
        }

        private IEnumerator FlashRedRoutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            
            // 閃紅結束若未死亡，狀態切換回 Normal
            if (currentState == PlayerState.Hurt)
            {
                currentState = PlayerState.Normal;
                Debug.Log($"【Console Log】玩家受傷閃紅結束，狀態恢復為: [Normal]");
            }
            flashCoroutine = null;
        }

        private void Die()
        {
            currentState = PlayerState.Dead;
            Debug.Log($"【Console Log】玩家已被擊敗！狀態變更為: [Dead]");
            
            // 停用移動腳本，阻止玩家繼續操作
            PlayerMovement pm = GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = false;
            }
            
            // 剛體速度歸零，防止滑行
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
