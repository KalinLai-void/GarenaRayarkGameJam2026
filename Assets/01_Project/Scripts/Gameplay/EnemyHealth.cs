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

        [Header("【受傷視覺反饋】")]
        [Tooltip("受傷紅閃持續時間 (秒)")]
        [SerializeField] private float flashDuration = 0.15f;

        [Tooltip("受傷時閃爍 the 顏色")]
        [SerializeField] private Color flashColor = Color.red;

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
        }

        private void Die()
        {
            currentState = EnemyState.Dead;
            Debug.Log($"【Console Log】怪物 {gameObject.name} 已被消滅！狀態變更為: [Dead]");
            Destroy(gameObject);
        }
    }
}
