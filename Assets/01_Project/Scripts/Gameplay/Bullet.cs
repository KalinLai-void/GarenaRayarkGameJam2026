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
        private bool isBeam = false;
        private int slowLevel = 0;
        private int burnLevel = 0;
        private bool isBlackMushroomActive = false;
        private int blackMushroomLevel = 0;

        /// <summary>
        /// 初始化子彈傷害、速度、生命週期與特殊效果
        /// </summary>
        public void Setup(int damageAmt, float speedOverride = -1f, float lifeTimeOverride = -1f, bool isBeamOverride = false)
        {
            bulletDamage = damageAmt;
            isBeam = isBeamOverride;

            if (speedOverride > 0f)
            {
                speed = speedOverride;
            }
            if (lifeTimeOverride > 0f)
            {
                lifeTime = lifeTimeOverride;
            }

            if (isBeam)
            {
                speed = 0f;
                lifeTime = 1f;
            }

            // 讀取玩家被動與主動技能設定
            if (PlayerSkillSystem.Instance != null)
            {
                slowLevel = PlayerSkillSystem.Instance.GetSkillLevel("R_ButtonMushroom");
                burnLevel = PlayerSkillSystem.Instance.GetSkillLevel("R_Shiitake");
                isBlackMushroomActive = PlayerSkillSystem.Instance.IsActiveSkillEffectRunning && 
                                        PlayerSkillSystem.Instance.ActiveSkill.skillID == "SR_BlackMushroom";
                blackMushroomLevel = PlayerSkillSystem.Instance.GetSkillLevel("SR_BlackMushroom");
            }

            Destroy(gameObject, lifeTime); // 超過生存時間自動銷毀
        }

        private void Start()
        {
            // 防呆：如果外部忘記呼叫 Setup，依然會在生存時間後銷毀
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (isBeam) return;
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
                // 1. 造成擊中傷害
                enemyHealth.TakeDamage(bulletDamage);

                // 2. 施加洋菇緩速、香菇灼燒
                if (slowLevel > 0)
                {
                    enemyHealth.ApplySlow(slowLevel);
                }
                if (burnLevel > 0)
                {
                    enemyHealth.ApplyBurn(burnLevel, bulletDamage);
                }

                // 3. 施加黑木耳黑色黏著方塊
                if (isBlackMushroomActive)
                {
                    SpawnStickyBlock();
                }

                // 珊瑚菇光束不銷毀（可穿透），普通子彈銷毀自身
                if (!isBeam)
                {
                    Destroy(gameObject);
                }
                return;
            }
            
            // 如果撞到牆壁或環境障礙物也銷毀
            if (collision.CompareTag("Obstacle") || collision.gameObject.name.Contains("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
            {
                if (isBlackMushroomActive)
                {
                    SpawnStickyBlock();
                }

                if (!isBeam)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void SpawnStickyBlock()
        {
            GameObject blockGo = new GameObject("StickyBlock");
            blockGo.transform.position = transform.position;
            
            // 生成黑色方塊貼圖
            SpriteRenderer sr = blockGo.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    tex.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f, 0.85f));
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            sr.sortingOrder = -1; // 顯示在怪物下方

            BoxCollider2D col = blockGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.2f);

            StickyBlock script = blockGo.AddComponent<StickyBlock>();
            script.Initialize(blackMushroomLevel, bulletDamage);
        }
    }
}
