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

        [Header("--- 隨機時間設定 ---")]
        [Tooltip("是否啟用隨機縮放時間（打亂 duration，使物件跳動更不同步）")]
        [SerializeField] private bool useRandomDuration = false;

        [Tooltip("隨機放大時間範圍 (秒)")]
        [SerializeField] private Vector2 randomDurationUpRange = new Vector2(0.3f, 0.7f);

        [Tooltip("隨機縮回時間範圍 (秒)")]
        [SerializeField] private Vector2 randomDurationDownRange = new Vector2(0.3f, 0.7f);

        [Header("--- 子物件同步設定 ---")]
        [Tooltip("是否同步影響所有子物件的縮放 (等比套用至子物件的 localScale)")]
        [SerializeField] private bool affectChildren = false;

        [Tooltip("是否套用縮放至自身物件")]
        [SerializeField] private bool scaleParent = true;

        private Vector3 originalScale;
        private readonly System.Collections.Generic.Dictionary<Transform, Vector3> childOriginalScales = new System.Collections.Generic.Dictionary<Transform, Vector3>();
        private readonly System.Collections.Generic.List<Coroutine> activeCoroutines = new System.Collections.Generic.List<Coroutine>();

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
                PlayLoop();
            }
            else if (playOnEnable)
            {
                PlayPunch();
            }
        }

        private void OnDisable()
        {
            // 停用時強制還原大小並停止所有協程，確保排版不跑位
            StopAllActiveScaleCoroutines();
            ResetAllScalesToOriginal();
        }

        private void StopAllActiveScaleCoroutines()
        {
            foreach (var co in activeCoroutines)
            {
                if (co != null)
                {
                    StopCoroutine(co);
                }
            }
            activeCoroutines.Clear();
        }

        private void ResetAllScalesToOriginal()
        {
            if (scaleParent)
            {
                transform.localScale = originalScale;
            }

            if (affectChildren)
            {
                foreach (var kvp in childOriginalScales)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.localScale = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 外部呼叫此方法即可播放一次「放大再縮回」的動態特效
        /// </summary>
        [ContextMenu("Play Punch")]
        public void PlayPunch()
        {
            if (!gameObject.activeInHierarchy) return;

            StopAllActiveScaleCoroutines();

            if (scaleParent)
            {
                activeCoroutines.Add(StartCoroutine(IndividualPunchScaleRoutine(transform, originalScale)));
            }

            if (affectChildren)
            {
                foreach (var kvp in childOriginalScales)
                {
                    if (kvp.Key != null)
                    {
                        activeCoroutines.Add(StartCoroutine(IndividualPunchScaleRoutine(kvp.Key, kvp.Value)));
                    }
                }
            }
        }

        private void PlayLoop()
        {
            if (!gameObject.activeInHierarchy) return;

            StopAllActiveScaleCoroutines();

            if (scaleParent)
            {
                activeCoroutines.Add(StartCoroutine(IndividualLoopingScaleRoutine(transform, originalScale)));
            }

            if (affectChildren)
            {
                foreach (var kvp in childOriginalScales)
                {
                    if (kvp.Key != null)
                    {
                        activeCoroutines.Add(StartCoroutine(IndividualLoopingScaleRoutine(kvp.Key, kvp.Value)));
                    }
                }
            }
        }

        private IEnumerator IndividualPunchScaleRoutine(Transform targetTransform, Vector3 targetOriginalScale)
        {
            // 1. 產生該物件專屬的隨機目標縮放與時間值
            Vector3 finalTargetMultiplier = targetScale;
            if (useRandomScale)
            {
                float randX = Random.Range(-randomRange.x, randomRange.x);
                float randY = Random.Range(-randomRange.y, randomRange.y);
                finalTargetMultiplier = new Vector3(1f + randX, 1f + randY, 1f);
            }

            float actDurationUp = durationUp;
            float actDurationDown = durationDown;
            if (useRandomDuration)
            {
                actDurationUp = Random.Range(randomDurationUpRange.x, randomDurationUpRange.y);
                actDurationDown = Random.Range(randomDurationDownRange.x, randomDurationDownRange.y);
            }

            // 2. 漸進放大至目標 Multiplier
            float elapsed = 0f;
            while (elapsed < actDurationUp)
            {
                if (targetTransform == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / actDurationUp);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 currentMultiplier = Vector3.Lerp(Vector3.one, finalTargetMultiplier, easedT);
                targetTransform.localScale = new Vector3(
                    targetOriginalScale.x * currentMultiplier.x,
                    targetOriginalScale.y * currentMultiplier.y,
                    targetOriginalScale.z * currentMultiplier.z
                );
                yield return null;
            }

            if (targetTransform != null)
            {
                targetTransform.localScale = new Vector3(
                    targetOriginalScale.x * finalTargetMultiplier.x,
                    targetOriginalScale.y * finalTargetMultiplier.y,
                    targetOriginalScale.z * finalTargetMultiplier.z
                );
            }

            // 3. 漸進縮回至原始 1.0
            elapsed = 0f;
            while (elapsed < actDurationDown)
            {
                if (targetTransform == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / actDurationDown);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 currentMultiplier = Vector3.Lerp(finalTargetMultiplier, Vector3.one, easedT);
                targetTransform.localScale = new Vector3(
                    targetOriginalScale.x * currentMultiplier.x,
                    targetOriginalScale.y * currentMultiplier.y,
                    targetOriginalScale.z * currentMultiplier.z
                );
                yield return null;
            }

            if (targetTransform != null)
            {
                targetTransform.localScale = targetOriginalScale;
            }
        }

        private IEnumerator IndividualLoopingScaleRoutine(Transform targetTransform, Vector3 targetOriginalScale)
        {
            while (loop)
            {
                yield return IndividualPunchScaleRoutine(targetTransform, targetOriginalScale);
                
                // 每次呼吸循環後的停頓間隔（也可加點隨機性讓效果更生動）
                float interval = 0.1f;
                if (useRandomDuration)
                {
                    interval = Random.Range(0.05f, 0.2f);
                }
                yield return new WaitForSecondsRealtime(interval);
            }
        }
    }
}
