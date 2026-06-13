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
        }

        [Header("--- 玩家參照 ---")]
        [SerializeField] private Transform playerTransform;

        [Header("--- 生怪範圍設定 ---")]
        [Tooltip("生怪半徑，必須大於螢幕視野寬高，通常 2D 橫向螢幕設 12~15 剛好在畫外")]
        [SerializeField] private float spawnRadius = 15f;

        [Header("--- 波次設定 ---")]
        [SerializeField] private SpawnWave[] waves;

        private int currentWaveIndex = 0;
        private float waveTimer = 0f;
        private float spawnTimer = 0f;
        private bool isSpawningFinished = false;

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
            // 1. 隨機決定一個 0 ~ 360 度的角度
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // 2. 利用三角函數算出圓圈上的相對座標
            Vector2 spawnOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * spawnRadius;

            // 3. 加上玩家當前的位置，得到最終的世界座標
            Vector3 spawnPosition = playerTransform.position + new Vector3(spawnOffset.x, spawnOffset.y, 0f);

            // 4. 生成怪物
            GameObject currentEnemyPrefab = waves[currentWaveIndex].enemyPrefab;
            if (currentEnemyPrefab != null)
            {
                Instantiate(currentEnemyPrefab, spawnPosition, Quaternion.identity);
            }
        }

        private void GoToNextWave()
        {
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
        }
    }
}
