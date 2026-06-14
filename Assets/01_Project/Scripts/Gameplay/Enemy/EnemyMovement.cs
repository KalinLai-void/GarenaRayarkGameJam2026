using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 控制怪物的 AI 移動，朝著玩家走，並與附近的其他怪物產生輕微的排斥力 (Separation)，
    /// 防止怪物重疊在一起，呈現群體包圍的絲滑效果。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [SerializeField] Animator animator;

        [Header("【怪物移動設定】")]
        [Tooltip("怪物的移動速度")]
        [SerializeField] private float moveSpeed = 2.5f;

        [Header("【排斥力 (Separation) 設定】")]
        [Tooltip("排斥偵測半徑")]
        [SerializeField] private float separationRadius = 1.0f;

        [Tooltip("排斥力權重")]
        [SerializeField] private float separationWeight = 1.5f;

        [Tooltip("當怪物完全重疊（距離趨近於0）時，推開彼此的隨機排斥力強度")]
        [SerializeField] private float overlapPushForce = 5.0f;

        private Rigidbody2D rb;
        private Transform playerTransform;
        private int enemyLayer;
        private SpriteRenderer spriteRenderer;

        // 使用共享緩衝區以避免 Physics2D.OverlapCircle 產生垃圾回收 (GC Alloc)
        private static readonly Collider2D[] nearbyBuffer = new Collider2D[30];
        private Vector2 cachedSeparation = Vector2.zero;
        private int frameDelayCount;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            enemyLayer = LayerMask.GetMask("Enemy");
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            // 隨機錯開每個怪物的計算幀，避免所有怪物在同一幀同時執行物理查詢
            frameDelayCount = Random.Range(0, 5);
        }

        private void Start()
        {
            // 尋找玩家 (藉由 Tag "Player" 尋找)
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning($"【EnemyMovement】{gameObject.name} 找不到 Tag 為 Player 的玩家物件！");
            }
        }

        private void FixedUpdate()
        {
            MoveTowardsPlayerWithSeparation();
        }

        private void MoveTowardsPlayerWithSeparation()
        {
            if (playerTransform == null)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // 1. 計算朝向玩家的引力向量
            Vector2 moveDirection = (playerTransform.position - transform.position);
            float distanceToPlayer = moveDirection.magnitude;
            
            if (distanceToPlayer > 0.05f)
            {
                moveDirection.Normalize();
            }
            else
            {
                moveDirection = Vector2.zero;
            }

            // 2. 分幀計算避開附近怪物的排斥力向量 (Separation Force) - 每 5 幀計算一次
            frameDelayCount++;
            if (frameDelayCount >= 5)
            {
                frameDelayCount = 0;
                Vector2 separationShared = Vector2.zero;

                // 使用 NonAlloc 避免垃圾回收 (GC Alloc) 造成卡頓
#pragma warning disable 618
                int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationRadius, nearbyBuffer, enemyLayer);
#pragma warning restore 618
                for (int i = 0; i < count; i++)
                {
                    Collider2D enemyCol = nearbyBuffer[i];
                    if (enemyCol == null) continue;

                    // 排除屬於自己本體或子物件的所有碰撞體 (避免與自己的 HurtBox 或本體產生排斥而導致除以0抖動)
                    if (enemyCol.transform.IsChildOf(this.transform))
                        continue;

                    Vector2 awayFromEnemy = (transform.position - enemyCol.transform.position);
                    float dist = awayFromEnemy.magnitude;

                    if (dist > 0.001f)
                    {
                        // 距離越近，排斥力越強
                        separationShared += awayFromEnemy.normalized / dist;
                    }
                    else
                    {
                        // 完全重疊時，給予隨機方向的強排斥力來分開彼此，避免重疊成一張圖
                        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        Vector2 randomPush = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                        separationShared += randomPush * overlapPushForce;
                    }
                }
                cachedSeparation = separationShared;
            }

            // 3. 最終移動方向 = 朝向玩家的引力 + 避開隊友的排斥力 (乘以權重)
            Vector2 finalVelocity = moveDirection + (cachedSeparation * separationWeight);

            float currentMaxSlow = Mathf.Max(passiveSlowPercent, blockSlowPercent);
            float actualSpeed = moveSpeed * (1f - currentMaxSlow);

            if (finalVelocity.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity = finalVelocity.normalized * moveSpeed;
                animator.SetBool("IsMoving", true);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
            }

            // 4. 根據速度方向更新翻面 (水平翻轉)
            UpdateFacing(rb.linearVelocity.x);
        }

        private float passiveSlowPercent = 0f;
        private float blockSlowPercent = 0f;

        public bool IsSlowed => (passiveSlowPercent > 0f || blockSlowPercent > 0f);

        public void ApplyPassiveSlow(float amount)
        {
            passiveSlowPercent = amount;
        }

        public void RemovePassiveSlow()
        {
            passiveSlowPercent = 0f;
        }

        public void ApplyBlockSlow(float amount)
        {
            blockSlowPercent = amount;
        }

        public void RemoveBlockSlow(float amount)
        {
            // 防呆移除對應數值
            if (blockSlowPercent == amount)
            {
                blockSlowPercent = 0f;
            }
        }

        private void UpdateFacing(float vx)
        {
            if (animator != null)
            {
                if (vx < -0.01f)
                {
                    animator.SetBool("IsFaceLeft", true);
                }
                else if (vx > 0.01f)
                {
                    animator.SetBool("IsFaceLeft", false);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 在編輯器選取時畫出排斥力半徑範圍
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, separationRadius);
        }
    }
}
