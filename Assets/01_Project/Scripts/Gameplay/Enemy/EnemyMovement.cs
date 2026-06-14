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

        [Tooltip("排斥力偵測中心的偏移量 (可用於對齊 Y 軸，避免跟腳底 Pivot 偏離)")]
        [SerializeField] private Vector2 separationOffset = Vector2.zero;

        private Rigidbody2D rb;
        private Transform playerTransform;
        private int enemyLayer;

        // 使用共享緩衝區以避免 Physics2D.OverlapCircle 產生垃圾回收 (GC Alloc)
        private static readonly Collider2D[] nearbyBuffer = new Collider2D[30];
        private Vector2 cachedSeparation = Vector2.zero;
        private Vector2 currentSeparation = Vector2.zero;
        private int frameDelayCount;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.None;
            enemyLayer = LayerMask.GetMask("Enemy");
            // 隨機錯開每個怪物的計算幀，避免所有怪物在同一幀同時執行物理查詢
            frameDelayCount = Random.Range(0, 5);

            // 🌟 解決 WebGL 平台上 2D Skeletal Animation (SpriteSkin) 進入畫面時的「閃現/拉伸/變形」異常問題
            // 強制所有子物件的 SpriteSkin 在畫面外也持續更新 (alwaysUpdate = true)
            var spriteSkins = GetComponentsInChildren<UnityEngine.U2D.Animation.SpriteSkin>(true);
            if (spriteSkins != null)
            {
                foreach (var skin in spriteSkins)
                {
                    skin.alwaysUpdate = true;
                }
            }
        }

        private void Start()
        {
            // ==========================================
            // 🌟 核心修復：強制刷新動畫與骨架狀態
            // ==========================================
            if (animator != null)
            {
                // 強迫 Animator 忘記預設座標，重新綁定目前的 Transform
                animator.Rebind();
                // 強迫 Animator 在第 0 秒立刻更新一次骨架位置，確保 SpriteSkin 抓到正確的矩陣
                animator.Update(0f);
            }

            // 尋找玩家
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
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
                Vector2 detectionCenter = (Vector2)transform.position + separationOffset;

                // 使用 NonAlloc 避免垃圾回收 (GC Alloc) 造成卡頓
#pragma warning disable 618
                int count = Physics2D.OverlapCircleNonAlloc(detectionCenter, separationRadius, nearbyBuffer, enemyLayer);
#pragma warning restore 618
                for (int i = 0; i < count; i++)
                {
                    Collider2D enemyCol = nearbyBuffer[i];
                    if (enemyCol == null) continue;

                    // 排除屬於自己本體或子物件的所有碰撞體 (避免與自己的 HurtBox 或本體產生排斥而導致除以0抖動)
                    if (enemyCol.transform.IsChildOf(this.transform))
                        continue;

                    // 使用其他怪物的實際碰撞中心點來計算排斥距離，更加精確
                    Vector2 enemyCenter = enemyCol.bounds.center;
                    Vector2 awayFromEnemy = (detectionCenter - enemyCenter);
                    float dist = awayFromEnemy.magnitude;

                    if (dist > 0.01f)
                    {
                        // 限制最大排斥力，避免極近距離下的物理爆炸與抖動
                        float force = Mathf.Min(10f, 1f / dist);
                        separationShared += awayFromEnemy.normalized * force;
                    }
                    else
                    {
                        // 完全重疊時，給予隨機方向的強排斥力來分開彼此
                        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        Vector2 randomPush = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                        separationShared += randomPush * overlapPushForce;
                    }
                }
                cachedSeparation = separationShared;
            }

            // 🌟 平滑排斥力過渡，消除每 5 幀突變時產生的微幅瞬移/抖動
            currentSeparation = Vector2.Lerp(currentSeparation, cachedSeparation, Time.fixedDeltaTime * 15f);

            // 3. 最終移動方向 = 朝向玩家的引力 + 避開隊友的排斥力 (乘以權重)
            Vector2 finalVelocity = moveDirection + (currentSeparation * separationWeight);

            float currentMaxSlow = Mathf.Max(passiveSlowPercent, blockSlowPercent);
            float actualSpeed = moveSpeed * (1f - currentMaxSlow);

            if (finalVelocity.sqrMagnitude > 0.001f)
            {
                // 修正原先直接使用 moveSpeed 的 Bug，使緩速機制真正生效
                rb.linearVelocity = finalVelocity.normalized * actualSpeed;
                animator.SetBool("IsMoving", true);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
            }

            // 4. 根據與玩家的相對位置進行翻面，從根本上消除「因群體排斥抖動導致的左右面部閃爍」
            UpdateFacing();
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

        private void UpdateFacing()
        {
            if (playerTransform == null || animator == null) return;
            
            float dx = playerTransform.position.x - transform.position.x;
            if (dx < -0.1f)
            {
                animator.SetBool("IsFaceLeft", true);
                animator.gameObject.transform.localScale = new Vector3(1f,1f,1f);
            }
            else if (dx > 0.1f)
            {
                animator.SetBool("IsFaceLeft", false);
                animator.gameObject.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 在編輯器選取時畫出排斥力半徑範圍
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere((Vector2)transform.position + separationOffset, separationRadius);
        }
    }
}
