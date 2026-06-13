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

        // --- 光束雷射新機制欄位 ---
        private bool hasCollided = false;
        private float dotTimer = 0f;
        private float dotInterval = 0.25f;
        private float dotDamagePercent = 1.0f;
        private readonly System.Collections.Generic.List<EnemyHealth> enemiesInRange = new System.Collections.Generic.List<EnemyHealth>();
        private Coroutine destroyCoroutine;

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
                // 光束一開始以正常速度前行，直到碰撞才在原地殘留。飛行時間最大設為 3 秒。
                lifeTime = 3f;

                if (PlayerSkillSystem.Instance != null)
                {
                    SkillData coralData = PlayerSkillSystem.Instance.GetSkillData("SR_CoralMushroom");
                    if (coralData != null)
                    {
                        dotInterval = coralData.coralBeamDotInterval;
                        dotDamagePercent = coralData.coralBeamDotPercent;
                    }
                }
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

            if (destroyCoroutine != null) StopCoroutine(destroyCoroutine);
            destroyCoroutine = StartCoroutine(DestroyAfterDelay(lifeTime));
        }

        private void Start()
        {
            // 防呆：如果外部忘記呼叫 Setup，依然會在生存時間後銷毀
            if (destroyCoroutine == null)
            {
                destroyCoroutine = StartCoroutine(DestroyAfterDelay(lifeTime));
            }
        }

        private System.Collections.IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        private void Update()
        {
            // 如果已經碰撞，則光束停留原地不動；否則沿著子彈自身 X 軸移動
            if (isBeam && hasCollided)
            {
                // 處理持續傷害
                dotTimer += Time.deltaTime;
                if (dotTimer >= dotInterval)
                {
                    dotTimer = 0f;
                    ApplyBeamDotDamage();
                }
                return;
            }

            // 沿著子彈自身的 X 軸正方向直線移動
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
        }

        private void ApplyBeamDotDamage()
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(bulletDamage * dotDamagePercent));
            for (int i = enemiesInRange.Count - 1; i >= 0; i--)
            {
                EnemyHealth enemy = enemiesInRange[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    enemy.TakeDamage(damage);
                    enemy.ApplyBurnVisualOnly(0.15f); // 傷害閃紅
                }
                else
                {
                    enemiesInRange.RemoveAt(i);
                }
            }
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
                if (isBeam)
                {
                    if (!enemiesInRange.Contains(enemyHealth))
                    {
                        enemiesInRange.Add(enemyHealth);
                    }

                    if (!hasCollided)
                    {
                        hasCollided = true;
                        speed = 0f;

                        // 重新設定銷毀時間
                        if (destroyCoroutine != null) StopCoroutine(destroyCoroutine);
                        float lingerDuration = 1.0f;
                        if (PlayerSkillSystem.Instance != null)
                        {
                            SkillData coralData = PlayerSkillSystem.Instance.GetSkillData("SR_CoralMushroom");
                            if (coralData != null)
                            {
                                lingerDuration = coralData.coralBeamDuration;
                            }
                        }
                        destroyCoroutine = StartCoroutine(DestroyAfterDelay(lingerDuration));

                        // 造成擊中傷害與施加效果
                        enemyHealth.TakeDamage(bulletDamage);
                        if (slowLevel > 0) enemyHealth.ApplySlow(slowLevel);
                        if (burnLevel > 0) enemyHealth.ApplyBurn(burnLevel, bulletDamage);
                        if (isBlackMushroomActive) SpawnStickyBlock();
                    }
                }
                else
                {
                    // 普通子彈
                    enemyHealth.TakeDamage(bulletDamage);
                    if (slowLevel > 0) enemyHealth.ApplySlow(slowLevel);
                    if (burnLevel > 0) enemyHealth.ApplyBurn(burnLevel, bulletDamage);
                    if (isBlackMushroomActive) SpawnStickyBlock();
                    Destroy(gameObject);
                }
                return;
            }
            
            // 如果撞到牆壁或環境障礙物也銷毀/殘留
            if (collision.CompareTag("Obstacle") || collision.gameObject.name.Contains("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
            {
                if (isBlackMushroomActive)
                {
                    SpawnStickyBlock();
                }

                if (isBeam)
                {
                    if (!hasCollided)
                    {
                        hasCollided = true;
                        speed = 0f;

                        if (destroyCoroutine != null) StopCoroutine(destroyCoroutine);
                        float lingerDuration = 1.0f;
                        if (PlayerSkillSystem.Instance != null)
                        {
                            SkillData coralData = PlayerSkillSystem.Instance.GetSkillData("SR_CoralMushroom");
                            if (coralData != null)
                            {
                                lingerDuration = coralData.coralBeamDuration;
                            }
                        }
                        destroyCoroutine = StartCoroutine(DestroyAfterDelay(lingerDuration));
                    }
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!isBeam) return;

            EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = collision.GetComponent<EnemyHealth>();
            }

            if (enemyHealth != null && enemiesInRange.Contains(enemyHealth))
            {
                enemiesInRange.Remove(enemyHealth);
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
