using UnityEngine;
using System.Collections;

namespace Gameplay
{
    /// <summary>
    /// 控制吸血鬼倖存者風的生怪系統，以玩家為中心在畫外圓圈軌道隨機生成怪物，並隨著時間推移變換波次。
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct SpawnWave
        {
            [Tooltip("波次名稱")] public string waveName;
            [Tooltip("這一波生成的怪物預製體 (Enemy Prefab)")] public GameObject enemyPrefab;
            [Tooltip("生怪間隔 (秒)")] public float spawnInterval;
            [Tooltip("此波次持續時間 (秒)")] public float duration;
            [Tooltip("每次生成數量 (設 1 為散怪，大於 1 為群聚)")] public int amountPerSpawn;
            [Tooltip("群聚怪物隨機散佈的半徑範圍")] public float groupSpread;
            [Tooltip("此波次結束時產生的拾取物最小數量")] public int minPickupAmount;
            [Tooltip("此波次結束時產生的拾取物最大數量")] public int maxPickupAmount;
        }

        [Header("--- 玩家參照 ---")]
        [SerializeField] private Transform playerTransform;

        [Header("--- 生怪範圍設定 ---")]
        [Tooltip("生怪半徑，必須大於螢幕視野寬高，通常 2D 橫向螢幕設 12~15 剛好在畫外")]
        [SerializeField] private float spawnRadius = 15f;
        [Tooltip("畫面上同時存活的最大怪物數量限制，達到此數量時將暫停生成新怪物")]
        [SerializeField] private int maxEnemyLimit = 150;

        [Header("--- 波次設定 ---")]
        [SerializeField] private SpawnWave[] waves;

        [Header("--- 拾取物生成設定 ---")]
        [Tooltip("拾取物預製體 (Pickup_Item.prefab)")]
        [SerializeField] private GameObject pickupItemPrefab;
        [Tooltip("放置拾取物的父物件 (若未指定，會尋找名為 MapItems 的物件，若仍找不到則自動創建)")]
        [SerializeField] private Transform mapItemsParent;
        [Tooltip("拾取物生成範圍的中心點 (相對於 Spawner 的偏移量)")]
        [SerializeField] private Vector2 pickupSpawnCenter = Vector2.zero;
        [Tooltip("拾取物生成範圍的尺寸 (X 軸寬度, Y 軸高度)")]
        [SerializeField] private Vector2 pickupSpawnSize = new Vector2(10f, 10f);

        private int currentWaveIndex = 0;
        private float waveTimer = 0f;
        private float spawnTimer = 0f;
        private bool isSpawningFinished = false;

        private GameObject pool;
        void Start()
        {
            // 防呆：如果沒拖入玩家，嘗試自動藉由 Tag 抓取
            if (playerTransform == null)
            {
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    playerTransform = playerGo.transform;
                }
            }

            pool = new GameObject("Enemy_Pool");
        }

        void Update()
        {
            if (playerTransform == null || isSpawningFinished || waves.Length == 0) return;

            // 推進當前波次的時間
            waveTimer += Time.deltaTime;

            // 檢查是否該進入下一波
            if (waveTimer >= waves[currentWaveIndex].duration)
            {
                GoToNextWave();
            }

            // 處理當前波次的生怪計時
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= waves[currentWaveIndex].spawnInterval)
            {
                spawnTimer = 0f;
                SpawnEnemyOnOrbit();
            }
        }

        private void SpawnEnemyOnOrbit()
        {
            // 若目前存活的怪物總數已達到上限，則跳過此次生成
            if (EnemyHealth.ActiveEnemyCount >= maxEnemyLimit)
            {
                return;
            }

            // 1. 隨機決定一個 0 ~ 360 度的角度
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // 2. 利用三角函數算出圓圈上的相對座標
            Vector2 spawnOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * spawnRadius;

            // 3. 加上玩家當前的位置，得到最終的世界座標
            Vector3 baseSpawnPosition = playerTransform.position + new Vector3(spawnOffset.x, spawnOffset.y, 0f);

            // 4. 取得當前波次設定，依據 amountPerSpawn 生出對應數量的怪物
            SpawnWave currentWave = waves[currentWaveIndex];
            int count = currentWave.amountPerSpawn > 0 ? currentWave.amountPerSpawn : 1;
            float spread = currentWave.groupSpread;

            for (int i = 0; i < count; i++)
            {
                // 如果是一坨怪，在基準點周圍加上微小的隨機位移，防止重疊
                Vector3 finalSpawnPos = baseSpawnPosition;
                if (count > 1)
                {
                    finalSpawnPos.x += Random.Range(-spread, spread);
                    finalSpawnPos.y += Random.Range(-spread, spread);
                }

                GameObject currentEnemyPrefab = currentWave.enemyPrefab;
                if (currentEnemyPrefab != null)
                {
                    GameObject enemy = Instantiate(currentEnemyPrefab, finalSpawnPos, Quaternion.identity);
                    enemy.transform.parent = pool.transform;
                }
            }
        }


        private void GoToNextWave()
        {
            // 記錄剛剛完成的波次索引
            int completedWaveIndex = currentWaveIndex;

            waveTimer = 0f;
            currentWaveIndex++;

            // 如果所有波次都跑完了，循環最後一波（地獄模式）
            if (currentWaveIndex >= waves.Length)
            {
                currentWaveIndex = waves.Length - 1;
                Debug.Log("【EnemySpawner】所有波次結束，進入無限終局模式！");
            }
            else
            {
                Debug.Log($"【EnemySpawner】進入下一波：{waves[currentWaveIndex].waveName}");
            }

            // 根據剛剛結束的波次設定來生成拾取物
            SpawnPickups(completedWaveIndex);
        }

        private void SpawnPickups(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Length) return;
            if (pickupItemPrefab == null)
            {
                Debug.LogWarning("【EnemySpawner】未指定 pickupItemPrefab，無法生成拾取物！");
                return;
            }

            SpawnWave completedWave = waves[waveIndex];
            int minAmount = completedWave.minPickupAmount;
            int maxAmount = completedWave.maxPickupAmount;

            // 如果設定的最大數量小於等於 0，表示此波不需要產生任何道具
            if (maxAmount <= 0) return;

            // 確保有 MapItems 父物件
            if (mapItemsParent == null)
            {
                GameObject mapItemsGo = GameObject.Find("MapItems");
                if (mapItemsGo == null)
                {
                    mapItemsGo = new GameObject("MapItems");
                }
                mapItemsParent = mapItemsGo.transform;
            }

            int spawnCount = Random.Range(minAmount, maxAmount + 1);
            Vector3 originCenter = transform.position + new Vector3(pickupSpawnCenter.x, pickupSpawnCenter.y, 0f);

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = Vector3.zero;
                bool validPos = false;
                int maxRetries = 30;

                for (int retry = 0; retry < maxRetries; retry++)
                {
                    float randomX = Random.Range(-pickupSpawnSize.x / 2f, pickupSpawnSize.x / 2f);
                    float randomY = Random.Range(-pickupSpawnSize.y / 2f, pickupSpawnSize.y / 2f);
                    spawnPos = originCenter + new Vector3(randomX, randomY, 0f);

                    // 檢查在生成位置附近是否有碰撞體
                    Collider2D hit = Physics2D.OverlapCircle(spawnPos, 0.5f);
                    bool isObstacle = false;

                    if (hit != null)
                    {
                        // 遞迴檢查自身與所有父物件是否有 "Obstacle" tag
                        Transform current = hit.transform;
                        while (current != null)
                        {
                            if (current.CompareTag("Obstacle"))
                            {
                                isObstacle = true;
                                break;
                            }
                            current = current.parent;
                        }
                    }

                    if (!isObstacle)
                    {
                        validPos = true;
                        break;
                    }
                }

                GameObject pickup = Instantiate(pickupItemPrefab, spawnPos, Quaternion.identity);
                pickup.transform.parent = mapItemsParent;
                Debug.Log($"【EnemySpawner】波次 {completedWave.waveName} 結束：在 {spawnPos} 生成了拾取物：{pickup.name} (是否避開障礙物: {validPos})");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 畫出拾取物隨機生成範圍
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + new Vector3(pickupSpawnCenter.x, pickupSpawnCenter.y, 0f);
            Gizmos.DrawWireCube(center, new Vector3(pickupSpawnSize.x, pickupSpawnSize.y, 0f));
        }
    }
}
