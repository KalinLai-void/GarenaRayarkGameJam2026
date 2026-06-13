using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 負責將移動狀態同步至 Animator 元件，以驅動 Animation Controller 中的動畫。
    /// （動畫均透過 Animation Controller 和 Animation Clip 設計，不再使用腳本更新貼圖或變形）
    /// </summary>
    public sealed class PlayerVisual : MonoBehaviour
    {
        [Header("【元件參照】")]
        [Tooltip("Animator 元件，用於驅動動畫 (若留空，將於 Awake 自動尋找)")]
        [SerializeField] private Animator animator;

        [Tooltip("PlayerMovement 元件，用於讀取移動狀態 (若留空，將於 Awake 自動尋找)")]
        [SerializeField] private PlayerMovement playerMovement;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }
        }

        private void Update()
        {
            if (animator != null && playerMovement != null)
            {
                // 將物理移動狀態傳遞給 Animator 控制器中的 "IsMoving" 參數
                animator.SetBool("IsMoving", playerMovement.IsMoving);
            }
        }

    }
}
