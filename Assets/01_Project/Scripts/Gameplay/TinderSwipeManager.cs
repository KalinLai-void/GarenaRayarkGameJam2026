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

            currentTimer = maxTime;
            isTimerRunning = true;
            
            // 初始化堆疊：一次生成 3 張卡片，方便透出下方卡面
            for (int i = 0; i < 3; i++)
            {
                SpawnCardToStack();
            }
            
            UpdateTopCardDragState();
        }

        private void Update()
        {
            if (isTimerRunning && activeCards.Count > 0)
            {
                currentTimer -= Time.deltaTime;
                if (currentTimer <= 0f)
                {
                    currentTimer = 0f;
                    isTimerRunning = false;
                    AutoSwipeTopCard();
                }
                
                // 更新 Home Button 上的數字倒數
                if (timerText != null)
                {
                    timerText.text = Mathf.CeilToInt(currentTimer).ToString();
                }
            }
        }

        private void AutoSwipeTopCard()
        {
            if (activeCards.Count > 0)
            {
                GameObject topCard = activeCards[0];
                var dragHandler = topCard.GetComponent<TinderCardDragHandler>();
                if (dragHandler != null)
                {
                    // 時間到，向右飛出 (Like) 並自動按壓 Like 按鈕
                    dragHandler.AutoSwipe(true);
                    OnCardSwiped(topCard, true);
                }
            }
        }

        /// <summary>
        /// 當卡片被拖曳時暫停計時
        /// </summary>
        public void PauseTimer()
        {
            isTimerRunning = false;
        }

        /// <summary>
        /// 當卡片拖曳被釋放回彈時恢復計時
        /// </summary>
        public void ResumeTimer()
        {
            isTimerRunning = true;
        }

        /// <summary>
        /// 當卡片被滑出銷毀時，由 DragHandler 呼叫。這會重置計時並補充卡片 stack。
        /// </summary>
        public void OnCardSwiped(GameObject swipedCard, bool isLike)
        {
            if (activeCards.Contains(swipedCard))
            {
                activeCards.Remove(swipedCard);
                Debug.Log($"【Tinder UI】卡片被滑動：{(isLike ? "右滑 (Like)" : "左滑 (Nope)")} - {swipedCard.name}");

                // 重新計時與啟動
                currentTimer = maxTime;
                isTimerRunning = true;

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
                if (dragHandler == null)
                {
                    dragHandler = card.AddComponent<TinderCardDragHandler>();
                }
                
                // 只有第 0 張開啟拖曳
                bool isTop = (i == 0);
                dragHandler.enabled = isTop;
                
                if (isTop)
                {
                    dragHandler.SetupButtons(nopeButton, likeButton);
                }
            }
        }
    }
}
