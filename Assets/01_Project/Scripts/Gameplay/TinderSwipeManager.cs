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
        
        private float currentTimer;
        private bool isTimerRunning = false;
        private readonly List<GameObject> activeCards = new List<GameObject>();
        private bool isClosing = false;
        private Coroutine nopeScaleCoroutine;
        private Coroutine likeScaleCoroutine;

        // 供展示測試用的技能卡片數據
        private readonly string[] skillNames = { "閃爍彈", "雷霆一擊", "治癒術", "烈焰風暴", "時間靜止" };
        private readonly string[] skillDescs = {
            "朝鼠標位置發射一枚會爆炸並致盲周圍敵人的閃光彈，冷卻時間 8 秒。",
            "重擊地面釋放衝擊波，造成 20 點傷害並使周圍敵人暈眩 1.5 秒。",
            "瞬間恢復自身 30 點生命值，並在接下來的 5 秒內每秒恢復 2 點生命值。",
            "召喚引導火雨，對區域內的怪物每秒造成 15 點魔法傷害，持續 4 秒。",
            "使周圍所有怪物和子彈的速度降低 90%，持續 3 秒，冷卻時間 25 秒。"
        };
        private int skillDataIndex = 0;

        private void Start()
        {
            // 清除編輯器留下的預覽/佔位卡片，避免干擾執行期的卡片堆疊與射線阻擋
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

            // 為實體按鈕綁定點擊事件，讓點擊也能觸發滑動
            if (nopeButton != null)
            {
                var btn = nopeButton.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnButtonClick(false));
                }
            }
            if (likeButton != null)
            {
                var btn = likeButton.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnButtonClick(true));
                }
            }

            currentTimer = maxTime;
            isTimerRunning = true;
            isClosing = false;
            
            // 初始化堆疊：一次生成 3 張卡片，方便透出下方卡面
            for (int i = 0; i < 3; i++)
            {
                SpawnCardToStack();
            }
            
            UpdateTopCardDragState();
        }

        private void Update()
        {
            if (isTimerRunning && activeCards.Count > 0 && !isClosing)
            {
                currentTimer -= Time.deltaTime;
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
                yield return new WaitForSeconds(0.05f); // 微幅間隔形成流水般飛出效果
            }

            // 等待卡片飛出螢幕 (高速飛出約 0.35s 內便超出 1200 像素被銷毀)
            yield return new WaitForSeconds(0.35f);

            // 關閉整個升級 UI 根物件
            gameObject.SetActive(false);
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

                // 總計倒數模式：滑動卡片不重置時間

                // 生成一張新卡片放入最底層 (Sibling Index = 0)
                SpawnCardToStack();

                // 更新最頂層卡片的拖曳狀態與按鈕連結
                UpdateTopCardDragState();
            }
        }

        private void SpawnCardToStack()
        {
            if (cardPrefab == null || cardContainer == null) return;

            GameObject newCard = Instantiate(cardPrefab, cardContainer);
            newCard.name = $"Card_{skillNames[skillDataIndex]}";
            
            // 尋找卡片內的文字組件並指派內容
            TextMeshProUGUI nameText = newCard.transform.Find("Card_Background/Dialog_Bubble/Skill_Name_Text")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = newCard.transform.Find("Card_Background/Dialog_Bubble/Skill_Desc_Text")?.GetComponent<TextMeshProUGUI>();
            
            if (nameText != null) nameText.text = skillNames[skillDataIndex];
            if (descText != null) descText.text = skillDescs[skillDataIndex];

            // 數據輪播
            skillDataIndex = (skillDataIndex + 1) % skillNames.Length;

            // 🌟 關鍵：將新卡片移到 Hierarchy 最上方 (Sibling Index 0)
            // 在 Layout 渲染順序中，排在最前面的會被畫在最底層，這樣後面進來的卡片就不會遮住最頂層正在滑動的卡片
            newCard.transform.SetAsFirstSibling();
            activeCards.Add(newCard);
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
                t += Time.deltaTime * 12f; // 約 0.08 秒完成
                button.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                yield return null;
            }
            button.localScale = Vector3.one;
        }
    }
}
