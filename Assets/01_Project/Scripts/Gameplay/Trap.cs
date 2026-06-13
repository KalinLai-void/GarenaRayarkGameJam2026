using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 陷阱腳本，當玩家踩入 Trigger 時造成扣血傷害。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Trap : MonoBehaviour
    {
        [Header("【陷阱設定】")]
        [Tooltip("踩到此陷阱造成的傷害值")]
        [SerializeField] private int damage = 10;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 檢查碰撞物件是否為玩家 (藉由 Tag 或是否有掛載 PlayerHealth 組件)
            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    Debug.Log($"【陷阱觸發】玩家踩到了 {gameObject.name}，扣除 {damage} 點生命值！");
                }
            }
        }
    }
}
