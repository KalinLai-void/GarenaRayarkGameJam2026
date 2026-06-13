using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 拾取道具邏輯，當玩家靠近碰觸（Trigger）時，將道具圖示加入右上角 UI 並銷毀自身。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PickupItem : MonoBehaviour
    {
        [Header("--- 道具設定 ---")]
        [Tooltip("此道具在 UI 顯示的圖示 (若未指派，則自動取得子物件 Visual 的 Sprite)")]
        [SerializeField] private Sprite itemSprite;
        
        [Tooltip("道具類型名稱，用於 Console 識別")]
        [SerializeField] private string itemName = "MapItem";

        private void Start()
        {
            // 防呆：如果未手動指派圖示，從子物件 Visual 上的 SpriteRenderer 取得
            if (itemSprite == null)
            {
                SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    itemSprite = sr.sprite;
                }
            }

            // 強制將 Collider2D 設為 Trigger 觸發器
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 偵測是否碰撞到玩家
            if (other.CompareTag("Player"))
            {
                Debug.Log($"【Console Log】玩家拾取了道具: {itemName}");

                // 尋找 UI 總管並新增圖示
                SkillUIManager uiManager = Object.FindFirstObjectByType<SkillUIManager>();
                if (uiManager != null)
                {
                    uiManager.AddInventoryIcon(itemSprite);
                }

                // 銷毀道具
                Destroy(gameObject);
            }
        }
    }
}
