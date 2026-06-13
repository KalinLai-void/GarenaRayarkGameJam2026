using Gameplay;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 負責播放上一輪玩家的軌跡與攻擊狀態。
/// 依靠讀取 FrameData 直接更新 Transform，不依賴物理運算與真實輸入。
/// </summary>
public sealed class GhostPlayer : MonoBehaviour
{
    [Header("【元件參照】")]
    [Tooltip("殘影專用的武器控制器 (不能掛原本的 WeaponController)")]
    [SerializeField] private GhostWeaponController ghostWeapon;

    [SerializeField] private Animator animator;

    private List<FrameData> replayData;
    private int currentFrame = 0;
    private bool isReplaying = false;

    private void Awake()
    {
    }

    /// <summary>
    /// 由 Manager 在生成殘影時呼叫，塞入歷史資料
    /// </summary>
    public void InitializeReplay(List<FrameData> data)
    {
        if (data == null || data.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        replayData = data;
        currentFrame = 0;
        isReplaying = true;
    }

    private void FixedUpdate()
    {
        if (!isReplaying) return;

        if (currentFrame < replayData.Count)
        {
            FrameData data = replayData[currentFrame];

            // 1. 更新本體位置
            transform.position = data.position;

            // 2. 處理本體的左右翻面 (計算當前幀與上一幀的位移差距)
            if (currentFrame > 0 && animator != null)
            {
                float deltaX = data.position.x - replayData[currentFrame - 1].position.x;
                if (deltaX < -0.01f)
                {
                    animator.SetBool("IsFaceLeft", true);
                }
                else if (deltaX > 0.01f)
                {
                    animator.SetBool("IsFaceLeft", false);
                }
            }

            // 3. 把「瞄準座標」與「是否開火」交給殘影專用武器去處理
            if (ghostWeapon != null)
            {
                ghostWeapon.ProcessGhostInput(data.aimWorldPosition, data.isAttacking);
            }

            currentFrame++;
        }
        else
        {
            // 資料播放完畢，殘影消失
            Destroy(gameObject);
        }
    }
}