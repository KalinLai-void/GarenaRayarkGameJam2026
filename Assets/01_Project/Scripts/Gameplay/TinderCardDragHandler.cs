using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay
{
    /// <summary>
    /// 負責處理卡片拖拽、旋轉、與下方按鈕縮小按壓回饋的腳本。
    /// </summary>
    public sealed class TinderCardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private Canvas canvas;
        private TinderSwipeManager swipeManager;

        private Vector2 startPosition;

        [Header("--- 拖拽與旋轉設定 ---")]
        [Tooltip("滑動判定閾值，超過此距離放開會飛出螢幕")]
        [SerializeField] private float swipeThreshold = 120f;
        
        [Tooltip("旋轉係數，數值越大旋轉越明顯")]
        [SerializeField] private float rotationFactor = 0.05f;
        
        [Tooltip("彈回原點的速度")]
        [SerializeField] private float returnSpeed = 15f;
        
        [Tooltip("飛出螢幕的速度")]
        [SerializeField] private float flyOutSpeed = 1000f;

        private float currentFlySpeed;
        private bool isDragging;
        private bool isFlyingAway;
        private Vector2 flyDirection;
        private float swipePercent;

        private Transform nopeButton;
        private Transform likeButton;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            swipeManager = Object.FindFirstObjectByType<TinderSwipeManager>();
            startPosition = rectTransform.anchoredPosition;
            currentFlySpeed = flyOutSpeed;
        }

        /// <summary>
        /// 由 Manager 初始化時傳入下方按鈕的參照，以便做按壓特效
        /// </summary>
        public void SetupButtons(Transform nope, Transform like)
        {
            nopeButton = nope;
            likeButton = like;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isFlyingAway) return;
            isDragging = true;
            
            if (swipeManager != null)
            {
                swipeManager.PauseTimer();
                swipeManager.StopButtonScaleCoroutines(); // 停止所有正在進行的按鈕回彈協程，防止拖拽衝突
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isFlyingAway) return;

            // UGUI 拖拽移動
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

            // 計算旋轉 (X 軸偏移越遠，卡片旋轉角度越大)
            float offsetX = rectTransform.anchoredPosition.x - startPosition.x;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, -offsetX * rotationFactor);

            // 計算滑動比例 (0f 到 1f)
            swipePercent = Mathf.Clamp01(Mathf.Abs(offsetX) / swipeThreshold);

            // 更新按鈕逐漸縮小按壓的視覺效果
            if (offsetX > 0f)
            {
                // 往右滑：Like 按鈕逐漸縮小 (模擬按下)
                if (likeButton != null)
                {
                    likeButton.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, swipePercent);
                }
                if (nopeButton != null)
                {
                    nopeButton.localScale = Vector3.one;
                }
            }
            else
            {
                // 往左滑：Nope 按鈕逐漸縮小 (模擬按下)
                if (nopeButton != null)
                {
                    nopeButton.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, swipePercent);
                }
                if (likeButton != null)
                {
                    likeButton.localScale = Vector3.one;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isFlyingAway) return;
            isDragging = false;

            float offsetX = rectTransform.anchoredPosition.x - startPosition.x;

            if (Mathf.Abs(offsetX) >= swipeThreshold)
            {
                // 滑動超過閾值，開始飛出動畫
                isFlyingAway = true;
                flyDirection = new Vector2(Mathf.Sign(offsetX), 0.2f).normalized; // 帶有一點點微幅向上的飛行弧度
                currentFlySpeed = flyOutSpeed; // 還原成正常飛出速度
                
                bool isLike = offsetX > 0f;
                if (swipeManager != null)
                {
                    swipeManager.AnimateButtonBounce(isLike); // 讓按鈕平滑回彈
                    swipeManager.OnCardSwiped(gameObject, isLike);
                }
            }
            else
            {
                // 未達閾值，啟動彈回中央與還原按鈕大小的協程
                StartCoroutine(ReturnToCenterRoutine());
            }
        }

        /// <summary>
        /// 提供給時間到自動滑動或按鈕點擊呼叫的方法
        /// </summary>
        public void AutoSwipe(bool isLike, float speedMultiplier = 1f, bool playButtonAnim = true)
        {
            if (isFlyingAway) return;
            isFlyingAway = true;
            flyDirection = new Vector2(isLike ? 1f : -1f, 0.1f).normalized;
            currentFlySpeed = flyOutSpeed * speedMultiplier;
            
            if (playButtonAnim)
            {
                // 執行按鈕自動按壓的模擬動畫
                StartCoroutine(AutoPressButtonRoutine(isLike));
            }
        }

        private System.Collections.IEnumerator AutoPressButtonRoutine(bool isLike)
        {
            Transform targetBtn = isLike ? likeButton : nopeButton;
            if (targetBtn != null)
            {
                float t = 0f;
                // 按下去 (0.07秒)
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime * 14f;
                    targetBtn.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, t);
                    yield return null;
                }
                t = 0f;
                // 彈起來 (0.07秒)
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime * 14f;
                    targetBtn.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, t);
                    yield return null;
                }
                targetBtn.localScale = Vector3.one;
            }
        }

        private System.Collections.IEnumerator ReturnToCenterRoutine()
        {
            Vector2 currentPos = rectTransform.anchoredPosition;
            Quaternion currentRot = rectTransform.localRotation;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * returnSpeed;
                rectTransform.anchoredPosition = Vector2.Lerp(currentPos, startPosition, t);
                rectTransform.localRotation = Quaternion.Slerp(currentRot, Quaternion.identity, t);

                // 同步將按鈕大小還原回 1.0f
                if (likeButton != null)
                {
                    likeButton.localScale = Vector3.one * Mathf.Lerp(likeButton.localScale.x, 1f, t);
                }
                if (nopeButton != null)
                {
                    nopeButton.localScale = Vector3.one * Mathf.Lerp(nopeButton.localScale.x, 1f, t);
                }

                yield return null;
            }

            rectTransform.anchoredPosition = startPosition;
            rectTransform.localRotation = Quaternion.identity;
            
            if (likeButton != null) likeButton.localScale = Vector3.one;
            if (nopeButton != null) nopeButton.localScale = Vector3.one;
            
            if (swipeManager != null)
            {
                swipeManager.ResumeTimer();
            }
        }

        private void Update()
        {
            if (isFlyingAway)
            {
                // 卡片往左或往右飛行移出螢幕
                rectTransform.anchoredPosition += flyDirection * currentFlySpeed * Time.unscaledDeltaTime;
                
                // 超出螢幕裁剪遮罩一定範圍後自動銷毀
                if (Mathf.Abs(rectTransform.anchoredPosition.x) > 1200f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
