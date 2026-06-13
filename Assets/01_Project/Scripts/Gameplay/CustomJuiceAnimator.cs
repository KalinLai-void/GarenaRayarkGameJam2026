using System.Collections;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 自定義 Juice 動畫狀態
    /// </summary>
    public enum JuiceAnimState
    {
        Normal,
        Hurt
    }

    /// <summary>
    /// 純手寫萬用 Juice 腳本 (無外掛版)：掛載於 Visual 子物件上。
    /// 透過數學正弦波 (Mathf.Sin) 模擬走路果凍顛簸，透過協程與 Lerp 模擬受擊壓扁彈回 (Punch Scale) 與紅閃。
    /// </summary>
    public sealed class CustomJuiceAnimator : MonoBehaviour
    {
        [Header("== 走路果凍設定 ==")]
        [Tooltip("走路跳動頻率 (Speed)")]
        [SerializeField] private float walkSpeed = 15f;
        
        [Tooltip("走路跳動幅度 (Amount)")]
        [SerializeField] private float walkAmount = 0.15f;

        [Header("== 受擊變形設定 ==")]
        [Tooltip("受擊瞬間壓扁尺寸")]
        [SerializeField] private Vector3 hurtScale = new Vector3(1.4f, 0.6f, 1f);
        
        [Tooltip("受擊彈回速度 (recoverSpeed)")]
        [SerializeField] private float recoverSpeed = 10f;

        [Header("== 受擊紅閃設定 ==")]
        [Tooltip("受傷紅閃持續時間")]
        [SerializeField] private float flashDuration = 0.15f;
        
        [Tooltip("受傷紅閃顏色")]
        [SerializeField] private Color flashColor = Color.red;

        public JuiceAnimState CurrentState => currentState;

        private Vector3 originalScale;
        private Coroutine hurtCoroutine;
        private Coroutine flashCoroutine;
        private bool isWalking = false;
        
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private JuiceAnimState currentState = JuiceAnimState.Normal;

        // 偵測大腦參考，以支援插拔即用自動化
        private PlayerMovement playerMovement;
        private Rigidbody2D parentRb;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            // 嘗試自動尋找父物件上的移動/物理組件
            playerMovement = GetComponentInParent<PlayerMovement>();
            parentRb = GetComponentInParent<Rigidbody2D>();

            // 自動停用舊的 Animator 以防冲突
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        private void Start()
        {
            originalScale = transform.localScale;
        }

        private void Update()
        {
            // 自動更新走路狀態 (如果大腦參考存在)
            UpdateWalkingStateAutomatically();

            // 只有在走路，且沒有在播放受傷動畫時，才執行純數學果凍走路
            if (isWalking && currentState != JuiceAnimState.Hurt)
            {
                // 利用 Mathf.Sin 創造完美的上下反向起伏（體積守恆）
                float wave = Mathf.Sin(Time.time * walkSpeed) * walkAmount;
                transform.localScale = new Vector3(originalScale.x + wave, originalScale.y - wave, originalScale.z);
            }
        }

        private void UpdateWalkingStateAutomatically()
        {
            if (playerMovement != null)
            {
                isWalking = playerMovement.IsMoving;
            }
            else if (parentRb != null)
            {
                isWalking = parentRb.linearVelocity.sqrMagnitude > 0.01f;
            }
        }

        // ─── 開放給外層大腦手動呼叫的接口 ───

        public void SetWalkingState(bool walking)
        {
            if (playerMovement == null && parentRb == null)
            {
                isWalking = walking;
                if (!isWalking && currentState != JuiceAnimState.Hurt)
                {
                    transform.localScale = originalScale;
                }
            }
        }

        /// <summary>
        /// 觸發受擊效果
        /// </summary>
        public void PlayHurtEffect()
        {
            currentState = JuiceAnimState.Hurt;
            Debug.Log($"【Console Log】{gameObject.name} 狀態變更為 [Hurt]！觸發自定義壓扁彈回與紅閃。");

            // 1. 如果前一個受傷協程還在跑，先強制關掉，重新計算
            if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
            hurtCoroutine = StartCoroutine(HurtRoutine());

            // 2. 觸發閃紅
            if (spriteRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRedRoutine());
            }
        }

        // ─── 核心自製彈性恢復協程 ───
        private IEnumerator HurtRoutine()
        {
            // 1. 瞬間壓扁
            transform.localScale = hurtScale;

            // 2. 用 Lerp 像彈簧一樣平滑地吸回原本的大小
            while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * recoverSpeed);
                yield return null; // 等待下一幀
            }

            // 3. 確保完美回正
            transform.localScale = originalScale;
            
            currentState = JuiceAnimState.Normal;
            Debug.Log($"【Console Log】{gameObject.name} 受擊壓扁結束，狀態恢復為 [Normal]。");
            
            hurtCoroutine = null;
        }

        private IEnumerator FlashRedRoutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            flashCoroutine = null;
        }
    }
}
