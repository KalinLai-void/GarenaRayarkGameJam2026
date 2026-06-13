using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 控制子彈的飛行速度、生存時間，並利用 Trigger 偵測對敵人造成傷害。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Bullet : MonoBehaviour
    {
        [Header("--- 子彈物理數值 ---")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifeTime = 3f;

        private int bulletDamage;

        /// <summary>
        /// 初始化子彈傷害並設定自動銷毀計時器
        /// </summary>
        public void Setup(int damageAmt)
        {
            bulletDamage = damageAmt;
            Destroy(gameObject, lifeTime); // 超過生存時間自動銷毀，防止記憶體殘留
        }

        private void Start()
        {
            // 防呆：如果外部忘記呼叫 Setup，依然會在生存時間後銷毀
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            // 沿著子彈自身的 X 軸正方向直線移動
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 忽略與 Player 的碰撞
            if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                return;
            }

            // 檢查撞到的物件或其父物件是否有 EnemyHealth 組件
            EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = collision.GetComponent<EnemyHealth>();
            }
            
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(bulletDamage); // 造成傷害
                Destroy(gameObject); // 子彈功成身退，銷毀自身
                return;
            }
            
            // 如果撞到牆壁或環境障礙物也銷毀
            if (collision.CompareTag("Obstacle") || collision.gameObject.name.Contains("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
            {
                Destroy(gameObject);
            }
        }
    }
}
