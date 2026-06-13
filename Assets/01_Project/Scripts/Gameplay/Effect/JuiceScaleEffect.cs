using System.Collections;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 提供物件縮放（果汁感/彈性縮放）特效的通用腳本。
    /// 可以掛在任何 UI 元件或場景 2D/3D 物件上。
    /// </summary>
    public sealed class JuiceScaleEffect : MonoBehaviour
    {
        [Header("--- 縮放設定 ---")]
        [Tooltip("目標中間狀態的縮放比例")]
        [SerializeField] private Vector3 targetScale = new Vector3(0.95f, 1.05f, 1f);
        
        [Tooltip("放大至目標值所需時間 (秒)")]
        [SerializeField] private float durationUp = 0.5f;
        
        [Tooltip("縮回原始大小所需時間 (秒)")]
        [SerializeField] private float durationDown = 0.5f;

        [Header("--- 自動觸發設定 ---")]
        [Tooltip("是否在物件啟用 (OnEnable) 時自動播放一次 (適合 UI 彈出或物件生成)")]
        [SerializeField] private bool playOnEnable = true;

        [Tooltip("是否循環播放 (脈動呼吸效果)")]
        [SerializeField] private bool loop = true;

        [Header("--- 隨機抖動設定 ---")]
        [Tooltip("是否啟用隨機目標縮放")]
        [SerializeField] private bool useRandomScale = false;

        [Tooltip("X 與 Y 軸的隨機震幅範圍 (以原始縮放為基準進行加減，例如 0.05 代表 1 +- 0.05)")]
        [SerializeField] private Vector2 randomRange = new Vector2(0.05f, 0.05f);

        [Header("--- 子物件同步設定 ---")]
        [Tooltip("是否同步影響所有子物件的縮放 (等比套用至子物件的 localScale)")]
        [SerializeField] private bool affectChildren = false;

        [Tooltip("是否套用縮放至自身物件")]
        [SerializeField] private bool scaleParent = true;

        private Vector3 originalScale;
        private Coroutine scaleCoroutine;
        private readonly System.Collections.Generic.Dictionary<Transform, Vector3> childOriginalScales = new System.Collections.Generic.Dictionary<Transform, Vector3>();

        private void Awake()
        {
            originalScale = transform.localScale;
            CacheChildOriginalScales();
        }

        public void CacheChildOriginalScales()
        {
            childOriginalScales.Clear();
            if (affectChildren)
            {
                foreach (Transform child in transform)
                {
                    childOriginalScales[child] = child.localScale;
                }
            }
        }

        private void OnEnable()
        {
            if (loop)
            {
                scaleCoroutine = StartCoroutine(LoopingScaleRoutine());
            }
            else if (playOnEnable)
            {
                PlayPunch();
            }
        }

        private void OnDisable()
        {
            // 停用時強制還原大小並停止協程，確保排版不跑位
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
                scaleCoroutine = null;
            }
            ApplyScaleMultiplier(Vector3.one);
        }

        /// <summary>
        /// 外部呼叫此方法即可播放一次「放大再縮回」的動態特效
        /// </summary>
        [ContextMenu("Play Punch")]
        public void PlayPunch()
        {
            if (!gameObject.activeInHierarchy) return;

            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(PunchScaleRoutine());
        }

        private void ApplyScaleMultiplier(Vector3 multiplier)
        {
            if (scaleParent)
            {
                transform.localScale = new Vector3(
                    originalScale.x * multiplier.x,
                    originalScale.y * multiplier.y,
                    originalScale.z * multiplier.z
                );
            }

            if (affectChildren)
            {
                foreach (var kvp in childOriginalScales)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.localScale = new Vector3(
                            kvp.Value.x * multiplier.x,
                            kvp.Value.y * multiplier.y,
                            kvp.Value.z * multiplier.z
                        );
                    }
                }
            }
        }

        private IEnumerator PunchScaleRoutine()
        {
            Vector3 finalTargetMultiplier = targetScale;
            if (useRandomScale)
            {
                float randX = Random.Range(-randomRange.x, randomRange.x);
                float randY = Random.Range(-randomRange.y, randomRange.y);
                finalTargetMultiplier = new Vector3(1f + randX, 1f + randY, 1f);
            }

            // 1. 漸進放大至目標 Multiplier
            float elapsed = 0f;
            while (elapsed < durationUp)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationUp);
                Vector3 currentMultiplier = Vector3.Lerp(Vector3.one, finalTargetMultiplier, Mathf.SmoothStep(0f, 1f, t));
                ApplyScaleMultiplier(currentMultiplier);
                yield return null;
            }
            ApplyScaleMultiplier(finalTargetMultiplier);

            // 2. 漸進縮回至原始 1.0
            elapsed = 0f;
            while (elapsed < durationDown)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationDown);
                Vector3 currentMultiplier = Vector3.Lerp(finalTargetMultiplier, Vector3.one, Mathf.SmoothStep(0f, 1f, t));
                ApplyScaleMultiplier(currentMultiplier);
                yield return null;
            }
            ApplyScaleMultiplier(Vector3.one);
            scaleCoroutine = null;
        }

        private IEnumerator LoopingScaleRoutine()
        {
            while (loop)
            {
                yield return PunchScaleRoutine();
                // 每次循環間隔一下，可在此處調整呼吸節奏
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }
}
