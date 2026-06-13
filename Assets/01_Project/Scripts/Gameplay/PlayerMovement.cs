using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    /// <summary>
    /// 控制玩家的基礎移動與面向更新。
    /// 依賴 Rigidbody2D 進行物理移動，防止穿牆。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("【基礎移動設定】")]
        [Tooltip("玩家移動的速度 (單位：公尺/秒)，預設為 5")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("【元件參照】")]
        [Tooltip("負責讀取輸入的 PlayerInput 元件 (若留空，將於 Awake 自動尋找同物件上的元件)")]
        [SerializeField] private PlayerInput playerInput;

        [Tooltip("負責角色貼圖顯示的 SpriteRenderer 元件 (若留空，將於 Awake 自動尋找子物件或同物件上的元件)")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        /// <summary>
        /// 當前玩家是否處於移動狀態
        /// </summary>
        public bool IsMoving => moveInput.sqrMagnitude > 0.001f;

        /// <summary>
        /// 當前的移動輸入向量
        /// </summary>
        public Vector2 MoveInput => moveInput;

        private Rigidbody2D rb;
        private InputAction moveAction;
        private Vector2 moveInput;

        private void Awake()
        {
            // 獲取剛體元件
            rb = GetComponent<Rigidbody2D>();

            // 自動尋找並補上元件參照
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // 初始化輸入動作
            if (playerInput != null)
            {
                moveAction = playerInput.actions.FindAction("Move");
            }
            else
            {
                Debug.LogWarning("PlayerMovement: 找不到 PlayerInput 元件，將無法透過 Input Action 移動！");
            }

            // 關閉 Player 與 Enemy，以及 Enemy 與 Enemy 的硬物理碰撞
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (playerLayer != -1 && enemyLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
                Debug.Log("【物理設定】已成功忽略 Player-Enemy 與 Enemy-Enemy 的硬物理碰撞。");
            }
        }

        private void Update()
        {
            // 每影格讀取輸入向量
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }
            else
            {
                moveInput = Vector2.zero;
            }

            // 更新角色面向 (水平翻面)
            UpdateFacingDirection();
        }

        private void FixedUpdate()
        {
            // 執行物理移動
            MovePlayer();
        }

        /// <summary>
        /// 根據最後移動的方向進行左右翻面
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (spriteRenderer == null) return;

            // 當往左移動時，翻面 (flipX = true)；當往右移動時，不翻面 (flipX = false)
            if (moveInput.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }
            else if (moveInput.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }
        }

        /// <summary>
        /// 處理 Rigidbody2D 的物理速度更新，防止穿牆並修正斜向加速
        /// </summary>
        private void MovePlayer()
        {
            Vector2 moveDirection = moveInput;

            // 如果向量長度大於 1，進行正規化以防止斜向移動速度變快
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            // 設定剛體速度 (使用 Unity 6 新 API linearVelocity 進行物理移動)
            rb.linearVelocity = moveDirection * moveSpeed;
        }

        /// <summary>
        /// 當與其他 2D 碰撞體接觸時觸發
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 利用 Tag 判斷是否為障礙物
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                // 獲取碰撞物件的根節點名稱
                string obstacleName = collision.transform.parent != null ? collision.transform.parent.name : collision.gameObject.name;
                Debug.Log($"【物理碰撞】玩家撞擊到了障礙物: {obstacleName} (碰撞體: {collision.gameObject.name})");
            }
        }
    }
}
