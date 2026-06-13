using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Gameplay
{
    /// <summary>
    /// 管理卡片堆疊生成、順序與手機 Home 鍵倒數計時的主控腳本。
    /// </summary>
    public sealed class TinderSwipeManager : MonoBehaviour
    {
        [Header("--- 參考物件 ---")]
        [Tooltip("卡片 Prefab")]
        [SerializeField] private GameObject cardPrefab;
        
        [Tooltip("裝載卡片的容器 (Card_Stack_Container)")]
        [SerializeField] private Transform cardContainer;
        
        [Tooltip("Nope 叉叉按鈕")]
        [SerializeField] private Transform nopeButton;
        
        [Tooltip("Like 愛心按鈕")]
        [SerializeField] private Transform likeButton;
        
        [Tooltip("手機 Home 鍵中央的倒數計時文字")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("--- 倒數計時設定 ---")]
        [Tooltip("每張卡片的選擇倒數時間 (秒)")]
        [SerializeField] private float maxTime = 5.0f;

        [Tooltip("畫面中同時堆疊顯示的卡片數量")]
        [SerializeField] private int stackSize = 3;

        [Tooltip("單次升級最大可同意次數（右滑達到此數量後會自動關閉手機）")]
        [SerializeField] private int maxLikes = 1;

        [Tooltip("單次升級最大可取消次數（左滑達到此數量後會自動關閉手機）")]
        [SerializeField] private int maxNopes = 3;

        [Header("--- 打開/關閉動畫設定 ---")]
        [Tooltip("打開動畫的持續時間 (秒)")]
        [SerializeField] private float openDuration = 0.5f;

        [Tooltip("關閉動畫的持續時間 (秒)")]
        [SerializeField] private float closeDuration = 0.4f;

        [Tooltip("動畫彈跳強度 (數值越大彈跳與放大越明顯，預設 1.70158)")]
        [SerializeField] private float bounceIntensity = 1.70158f;
        
        private float currentTimer;
        private bool isTimerRunning = false;
        private readonly List<GameObject> activeCards = new List<GameObject>();
        private bool isClosing = false;
        public bool IsClosing => isClosing;
        private int currentLikes = 0;
        private int currentNopes = 0;
        private Coroutine nopeScaleCoroutine;
        private Coroutine likeScaleCoroutine;

        private RectTransform rectTransform;
        private Transform playerTransform;
        private Vector3 originalScale = Vector3.one;
        private Vector3 originalLocalPosition; // 儲存編輯器設定的原始本地位置

        // 🌟 儲存編輯器中擺放的卡片範本排版，使動態生成的卡片尺寸、錨點與縮放完全契合
        private Vector2 templateAnchorMin = new Vector2(0f, 0.5f);
        private Vector2 templateAnchorMax = new Vector2(1f, 0.5f);
        private Vector2 templatePivot = new Vector2(0.5f, 0.5f);
        private Vector2 templateSizeDelta = new Vector2(-28.85f, 534.23f);
        private Vector2 templateAnchoredPosition = new Vector2(6.57f, 312.0f);
        private Vector3 templateLocalScale = Vector3.one;
        private bool hasTemplate = false;

        [Header("--- 技能池與隨機抽取設定 ---")]
        [Tooltip("所有的技能 ScriptableObjects 列表")]
        [SerializeField] private List<SkillData> allSkills = new List<SkillData>();

        [Tooltip("R 級卡片抽中權重")]
        [SerializeField] private float rWeight = 70f;

        [Tooltip("SR 級卡片抽中權重")]
        [SerializeField] private float srWeight = 25f;

        [Tooltip("SSR 級卡片抽中權重")]
        [SerializeField] private float ssrWeight = 5f;

        // 用於儲存已被生成卡牌所關聯的 SkillData 參考
        private readonly Dictionary<GameObject, SkillData> cardToSkillMap = new Dictionary<GameObject, SkillData>();

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            
            // 🌟 動態更置 Pivot 並進行位移補償，確保執行期的視覺位置與 Scene 編輯器中完全一致！
            SetPivotWithoutShifting(new Vector2(0.5f, 0.5f));
            
            originalScale = transform.localScale; // 記住您在編輯器中設定的原始縮放比例！
            originalLocalPosition = rectTransform.localPosition; // 記住您在編輯器中設定的原始本地位置！
        }

        private void SetPivotWithoutShifting(Vector2 targetPivot)
        {
            if (rectTransform == null) return;
            Vector2 deltaPivot = targetPivot - rectTransform.pivot;
            Vector3 deltaPosition = new Vector3(
                deltaPivot.x * rectTransform.rect.width * rectTransform.localScale.x,
                deltaPivot.y * rectTransform.rect.height * rectTransform.localScale.y,
                0f
            );
            rectTransform.pivot = targetPivot;
            rectTransform.localPosition += rectTransform.localRotation * deltaPosition;
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerTransform = playerMovement.transform;
                }
                else
                {
                    var playerGo = GameObject.FindWithTag("Player");
                    if (playerGo != null)
                    {
                        playerTransform = playerGo.transform;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            // 🌟 一啟用立刻將縮放設為 0，防止在開始彈出動畫前出現一影格的「原尺寸閃爍」！
            rectTransform.localScale = Vector3.zero;

            // 初始化或重置狀態
            isClosing = false;
            currentTimer = maxTime;
            isTimerRunning = false; // 彈出動畫進行中先不計時，彈完才計時
            currentLikes = 0;
            currentNopes = 0;

            // 🌟 讀取編輯器中的卡片範本設定，以便動態生成的卡片完全契合您在編輯器中排版的位置與大小！
            hasTemplate = false;
            if (cardContainer != null && cardContainer.childCount > 0)
            {
                RectTransform templateRt = cardContainer.GetChild(0).GetComponent<RectTransform>();
                if (templateRt != null)
                {
                    templateAnchorMin = templateRt.anchorMin;
                    templateAnchorMax = templateRt.anchorMax;
                    templatePivot = templateRt.pivot;
                    templateSizeDelta = templateRt.sizeDelta;
                    templateAnchoredPosition = templateRt.anchoredPosition;
                    templateLocalScale = templateRt.localScale;
                    hasTemplate = true;
                }
            }

            // 清除上次或編輯器留下的卡片，避免干擾執行期的卡片堆疊與射線阻擋
            if (cardContainer != null)
            {
                var placeholders = new List<GameObject>();
                foreach (Transform child in cardContainer)
                {
                    placeholders.Add(child.gameObject);
                }
                foreach (var placeholder in placeholders)
                {
                    placeholder.transform.SetParent(null);
                    Destroy(placeholder);
                }
            }
            activeCards.Clear();
            cardToSkillMap.Clear();

            // 為實體按鈕綁定點擊事件，讓點擊也能觸發滑動
            // ⚠️ 同時關閉 Button Navigation，防止 Space 鍵 (UI Submit) 意外觸發按鈕！
            if (nopeButton != null)
            {
                var btn = nopeButton.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnButtonClick(false));
                    // 關閉鍵盤/手把 Navigation，防止 Space/Enter 誤觸
                    btn.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                }
            }
            if (likeButton != null)
            {
                var btn = likeButton.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnButtonClick(true));
                    // 關閉鍵盤/手把 Navigation，防止 Space/Enter 誤觸
                    btn.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                }
            }


            // 初始化堆疊：一次生成 stackSize 張卡片，方便透出下方卡面
            for (int i = 0; i < stackSize; i++)
            {
                SpawnCardToStack();
            }
            
            UpdateTopCardDragState();

            // 啟動從玩家身上彈出來的彈出特效
            StartCoroutine(OpenAnimationRoutine());
        }

        private void Update()
        {
            if (isTimerRunning && activeCards.Count > 0 && !isClosing)
            {
                currentTimer -= Time.unscaledDeltaTime; // 即使遊戲暫停也能進行計時
                if (currentTimer <= 0f)
                {
                    currentTimer = 0f;
                    isTimerRunning = false;
                    OnTimerExpired();
                }
                
                // 更新 Home Button 上的數字倒數
                if (timerText != null)
                {
                    timerText.text = Mathf.CeilToInt(currentTimer).ToString();
                }
            }
        }

        private void OnTimerExpired()
        {
            StartCoroutine(CloseRoutine());
        }

        private System.Collections.IEnumerator OpenAnimationRoutine()
        {
            isClosing = true; // 動畫執行期間禁止使用者進行卡牌互動

            // 取得玩家最新的螢幕位置作為彈出起點
            Vector2 startLocalPos = Vector2.zero;
            FindPlayer();
            if (playerTransform != null && Camera.main != null)
            {
                Vector2 screenPoint = Camera.main.WorldToScreenPoint(playerTransform.position);
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    RectTransform parentRect = rectTransform.parent as RectTransform;
                    if (parentRect != null)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out startLocalPos);
                    }
                }
            }

            rectTransform.localPosition = new Vector3(startLocalPos.x, startLocalPos.y, rectTransform.localPosition.z);
            rectTransform.localScale = Vector3.zero;

            float t = 0f;
            float duration = openDuration; // 使用設定的開啟時長
            Vector3 endLocalPos = originalLocalPosition; // 使用設定的原始本地位置！

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float progress = Mathf.Clamp01(t);
                float eased = EaseOutBack(progress);

                rectTransform.localScale = originalScale * eased; // 🌟 乘以您設定的原始 Scale！
                rectTransform.localPosition = Vector3.Lerp(new Vector3(startLocalPos.x, startLocalPos.y, rectTransform.localPosition.z), endLocalPos, eased);

                yield return null;
            }

            rectTransform.localScale = originalScale; // 🌟 還原為原始 Scale！
            rectTransform.localPosition = originalLocalPosition; // 🌟 還原為原始本地位置！

            isClosing = false;
            isTimerRunning = true; // 動畫完成後正式開始計時
        }

        private System.Collections.IEnumerator CloseRoutine()
        {
            isClosing = true;
            isTimerRunning = false;

            // 讓所有目前存在的卡片高速向左飛出
            List<GameObject> cardsToSwipe = new List<GameObject>(activeCards);
            activeCards.Clear(); // 清空 activeCards 以防重入或干擾

            for (int i = 0; i < cardsToSwipe.Count; i++)
            {
                GameObject card = cardsToSwipe[i];
                if (card != null)
                {
                    var dragHandler = card.GetComponent<TinderCardDragHandler>();
                    if (dragHandler != null)
                    {
                        dragHandler.enabled = false; // 停用拖拽防止手動干擾
                        dragHandler.AutoSwipe(false, 3.5f, false); // 3.5 倍速向左飛出，不播放按鈕動畫
                    }
                }
                yield return new WaitForSeconds(0.03f); // 微幅間隔形成流水般飛出效果
            }

            // 等待卡片飛出螢幕
            yield return new WaitForSeconds(0.2f);

            // 播放關閉動畫：手機 UI 縮小到角色身上！
            yield return StartCoroutine(PlayScaleDownToPlayerRoutine());

            // 關閉整個升級 UI 根物件
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator SelectAndCloseRoutine(GameObject selectedCard)
        {
            isClosing = true;
            isTimerRunning = false;

            // 讓其他未被選擇的卡片高速向左飛走
            List<GameObject> otherCards = new List<GameObject>(activeCards);
            activeCards.Clear();

            foreach (var card in otherCards)
            {
                if (card != selectedCard && card != null)
                {
                    var dragHandler = card.GetComponent<TinderCardDragHandler>();
                    if (dragHandler != null)
                    {
                        dragHandler.enabled = false;
                        dragHandler.AutoSwipe(false, 3.5f, false); // 高速向左飛出
                    }
                }
            }

            // 等待選中的卡片飛出螢幕 (正常飛出速度約 0.3 秒)
            yield return new WaitForSeconds(0.3f);

            // 播放關閉動畫：手機 UI 縮小到角色身上！
            yield return StartCoroutine(PlayScaleDownToPlayerRoutine());

            // 關閉整個升級 UI 根物件
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator PlayScaleDownToPlayerRoutine()
        {
            Vector2 targetLocalPos = Vector2.zero;
            FindPlayer();
            if (playerTransform != null && Camera.main != null)
            {
                Vector2 screenPoint = Camera.main.WorldToScreenPoint(playerTransform.position);
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    RectTransform parentRect = rectTransform.parent as RectTransform;
                    if (parentRect != null)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out targetLocalPos);
                    }
                }
            }

            float t = 0f;
            float duration = closeDuration; // 使用設定的關閉時長
            Vector3 startLocalPos = rectTransform.localPosition;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float progress = Mathf.Clamp01(t);
                float eased = EaseInBack(progress);

                rectTransform.localScale = originalScale * (1f - eased); // 🌟 乘以您設定的原始 Scale！
                rectTransform.localPosition = Vector3.Lerp(startLocalPos, new Vector3(targetLocalPos.x, targetLocalPos.y, rectTransform.localPosition.z), eased);

                yield return null;
            }

            rectTransform.localScale = Vector3.zero;
            rectTransform.localPosition = new Vector3(targetLocalPos.x, targetLocalPos.y, rectTransform.localPosition.z);
        }

        private void OnButtonClick(bool isLike)
        {
            if (isClosing || activeCards.Count == 0) return;
            
            GameObject topCard = activeCards[0];
            var dragHandler = topCard.GetComponent<TinderCardDragHandler>();
            if (dragHandler != null)
            {
                // 模擬點擊效果與快速飛出
                dragHandler.AutoSwipe(isLike, 1.5f, true);
                OnCardSwiped(topCard, isLike);
            }
        }

        /// <summary>
        /// 當卡片被拖曳時暫停計時 (總倒數時間不受拖拽影響，持續倒數以防玩家作弊)
        /// </summary>
        public void PauseTimer()
        {
        }

        /// <summary>
        /// 當卡片拖曳被釋放回彈時恢復計時 (總倒數時間不受拖拽影響，持續倒數以防玩家作弊)
        /// </summary>
        public void ResumeTimer()
        {
        }

        /// <summary>
        /// 當卡片被滑出銷毀時，由 DragHandler 呼叫。這會補充卡片 stack。
        /// </summary>
        public void OnCardSwiped(GameObject swipedCard, bool isLike)
        {
            if (isClosing) return;

            if (activeCards.Contains(swipedCard))
            {
                activeCards.Remove(swipedCard);
                Debug.Log($"【Tinder UI】卡片被滑動：{(isLike ? "右滑 (Like)" : "左滑 (Nope)")} - {swipedCard.name}");

                // 處理 Like 時的技能升級/解鎖
                if (isLike)
                {
                    currentLikes++;
                    if (cardToSkillMap.TryGetValue(swipedCard, out SkillData skill))
                    {
                        if (PlayerSkillSystem.Instance != null)
                        {
                            PlayerSkillSystem.Instance.AcquireOrUpgradeSkill(skill);
                        }
                    }
                }
                else
                {
                    currentNopes++;
                }

                // 清除映射
                cardToSkillMap.Remove(swipedCard);

                if (currentLikes >= maxLikes || currentNopes >= maxNopes)
                {
                    // 🌟 同意或取消次數已達上限，自動關閉手機 UI
                    if (isLike)
                    {
                        StartCoroutine(SelectAndCloseRoutine(swipedCard));
                    }
                    else
                    {
                        StartCoroutine(CloseRoutine());
                    }
                }
                else
                {
                    // 未達上限，繼續補牌
                    SpawnCardToStack();
                    UpdateTopCardDragState();
                }
            }
        }

        private void SpawnCardToStack()
        {
            if (cardPrefab == null)
            {
                Debug.LogError("【Tinder UI】Card Prefab 欄位未指派，或被錯誤指派成 Hierarchy 中的物件而被銷毀了！請將 Project (Assets) 視窗中的 UI_TinderCard Prefab 拖入 TinderSwipeManager 的 Card Prefab 欄位中。");
                return;
            }
            if (cardContainer == null)
            {
                Debug.LogError("【Tinder UI】Card Container 欄位未指派！");
                return;
            }

            // 1. 取得目前在畫面上的所有 SkillData，以確保不重複生成
            List<SkillData> activeSkills = GetActiveSkills();
            
            // 2. 根據加權權重選擇一個不重複的 SkillData
            SkillData selectedSkill = SelectRandomSkill(activeSkills);
            if (selectedSkill == null)
            {
                Debug.LogWarning("【Tinder UI】無更多可升級技能或沒有滿足稀有度條件的技能！");
                return;
            }

            GameObject newCard = Instantiate(cardPrefab, cardContainer);
            
            // 🌟 讓新生成卡片完美套用模版排版，防止與編輯器內大小不一
            RectTransform cardRt = newCard.GetComponent<RectTransform>();
            if (cardRt != null && hasTemplate)
            {
                cardRt.anchorMin = templateAnchorMin;
                cardRt.anchorMax = templateAnchorMax;
                cardRt.pivot = templatePivot;
                cardRt.sizeDelta = templateSizeDelta;
                cardRt.anchoredPosition = templateAnchoredPosition;
                newCard.transform.localScale = templateLocalScale;
            }
            else
            {
                newCard.transform.localScale = cardPrefab.transform.localScale;
            }

            // 🌟 重新設定卡片原點位置以防拖拽回彈位置偏移！
            var dragHandler = newCard.GetComponent<TinderCardDragHandler>();
            if (dragHandler != null)
            {
                dragHandler.ResetStartPosition();
            }

            newCard.name = $"Card_{selectedSkill.skillID}";
            cardToSkillMap[newCard] = selectedSkill;

            // 3. 套用 UI_TinderCard 資料連結
            UI_TinderCard cardUI = newCard.GetComponent<UI_TinderCard>();
            if (cardUI != null)
            {
                cardUI.Setup(selectedSkill);
            }

            // 🌟 關鍵：將新卡片移到 Hierarchy 最上方 (Sibling Index 0)
            // 在 Layout 渲染順序中，排在最前面的會被畫在最底層，這樣後面進來的卡片就不會遮住最頂層正在滑動的卡片
            newCard.transform.SetAsFirstSibling();
            activeCards.Add(newCard);
        }

        private List<SkillData> GetActiveSkills()
        {
            List<SkillData> list = new List<SkillData>();
            foreach (var card in activeCards)
            {
                if (card != null && cardToSkillMap.TryGetValue(card, out SkillData skill))
                {
                    list.Add(skill);
                }
            }
            return list;
        }

        private SkillData SelectRandomSkill(List<SkillData> excluded)
        {
            if (allSkills == null || allSkills.Count == 0) return null;

            List<SkillData> rPool = new List<SkillData>();
            List<SkillData> srPool = new List<SkillData>();
            List<SkillData> ssrPool = new List<SkillData>();

            foreach (var skill in allSkills)
            {
                if (skill == null) continue;
                if (excluded.Contains(skill)) continue;

                // 檢查是否已達最高等級
                if (PlayerSkillSystem.Instance != null && PlayerSkillSystem.Instance.IsSkillAtMaxLevel(skill))
                {
                    continue;
                }

                // 檢查 SSR 是否已被獲得過 (整局僅一次)
                if (skill.rarity == Rarity.SSR && PlayerSkillSystem.Instance != null && PlayerSkillSystem.Instance.HasAcquiredSSR())
                {
                    continue;
                }

                switch (skill.rarity)
                {
                    case Rarity.R: rPool.Add(skill); break;
                    case Rarity.SR: srPool.Add(skill); break;
                    case Rarity.SSR: ssrPool.Add(skill); break;
                }
            }

            float total = 0f;
            if (rPool.Count > 0) total += rWeight;
            if (srPool.Count > 0) total += srWeight;
            if (ssrPool.Count > 0) total += ssrWeight;

            if (total <= 0f) return null;

            float r = Random.Range(0f, total);
            float currentSum = 0f;

            if (rPool.Count > 0)
            {
                currentSum += rWeight;
                if (r < currentSum)
                {
                    return rPool[Random.Range(0, rPool.Count)];
                }
            }

            if (srPool.Count > 0)
            {
                currentSum += srWeight;
                if (r < currentSum)
                {
                    return srPool[Random.Range(0, srPool.Count)];
                }
            }

            if (ssrPool.Count > 0)
            {
                return ssrPool[Random.Range(0, ssrPool.Count)];
            }

            return null;
        }

        private void UpdateTopCardDragState()
        {
            // 在卡片堆疊中，只有第 0 張 (最後被渲染在最頂端的卡片) 能接受拖曳輸入與連結按鈕
            for (int i = 0; i < activeCards.Count; i++)
            {
                GameObject card = activeCards[i];
                var dragHandler = card.GetComponent<TinderCardDragHandler>();
                if (dragHandler != null)
                {
                    dragHandler.enabled = (i == 0); // 只有第 0 張開啟拖曳
                }
                
                if (i == 0 && dragHandler != null)
                {
                    dragHandler.SetupButtons(nopeButton, likeButton);
                }
            }
        }

        public void AnimateButtonBounce(bool isLike)
        {
            if (isLike)
            {
                if (likeScaleCoroutine != null) StopCoroutine(likeScaleCoroutine);
                likeScaleCoroutine = StartCoroutine(ButtonBounceRoutine(likeButton));
            }
            else
            {
                if (nopeScaleCoroutine != null) StopCoroutine(nopeScaleCoroutine);
                nopeScaleCoroutine = StartCoroutine(ButtonBounceRoutine(nopeButton));
            }
        }

        public void StopButtonScaleCoroutines()
        {
            if (nopeScaleCoroutine != null)
            {
                StopCoroutine(nopeScaleCoroutine);
                nopeScaleCoroutine = null;
            }
            if (likeScaleCoroutine != null)
            {
                StopCoroutine(likeScaleCoroutine);
                likeScaleCoroutine = null;
            }
        }

        private System.Collections.IEnumerator ButtonBounceRoutine(Transform button)
        {
            if (button == null) yield break;
            Vector3 startScale = button.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 12f; // 約 0.08 秒完成
                button.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                yield return null;
            }
            button.localScale = Vector3.one;
        }

        private float EaseOutBack(float x)
        {
            float c1 = bounceIntensity;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private float EaseInBack(float x)
        {
            float c1 = bounceIntensity;
            float c3 = c1 + 1f;
            return c3 * x * x * x - c1 * x * x;
        }
    }
}
