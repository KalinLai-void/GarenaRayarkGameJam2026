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
        [Header("【怪物移動設定】")]
        [Tooltip("怪物的移動速度")]
        [SerializeField] private float moveSpeed = 2.5f;

        [Header("【排斥力 (Separation) 設定】")]
        [Tooltip("排斥偵測半徑")]
        [SerializeField] private float separationRadius = 1.0f;

        [Tooltip("排斥力權重")]
        [SerializeField] private float separationWeight = 1.5f;

        private Rigidbody2D rb;
        private Transform playerTransform;
        private int enemyLayer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            enemyLayer = LayerMask.GetMask("Enemy");
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

            // 2. 計算避開附近怪物的排斥力向量 (Separation Force)
            Vector2 separationShared = Vector2.zero;
            Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);

            foreach (var enemyCol in nearbyEnemies)
            {
                // 排除屬於自己根物件的所有碰撞體 (避免與自己的 HurtBox 或本體產生排斥而導致除以0抖動)
                if (enemyCol.transform.root == this.transform.root)
                    continue;

                Vector2 awayFromEnemy = (transform.position - enemyCol.transform.position);
                float dist = awayFromEnemy.magnitude;

                if (dist > 0.001f)
                {
                    // 距離越近，排斥力越強
                    separationShared += awayFromEnemy.normalized / dist;
                }
            }

            // 3. 最終移動方向 = 朝向玩家的引力 + 避開隊友的排斥力 (乘以權重)
            Vector2 finalVelocity = moveDirection + (separationShared * separationWeight);

            if (finalVelocity.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity = finalVelocity.normalized * moveSpeed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            // 4. 根據速度方向更新翻面 (水平翻轉)
            UpdateFacing(rb.linearVelocity.x);
        }

        private void UpdateFacing(float vx)
        {
            // 尋找子物件 Visual 上的 SpriteRenderer 來控制面向
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (vx < -0.01f)
                {
                    sr.flipX = true;
                }
                else if (vx > 0.01f)
                {
                    sr.flipX = false;
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
