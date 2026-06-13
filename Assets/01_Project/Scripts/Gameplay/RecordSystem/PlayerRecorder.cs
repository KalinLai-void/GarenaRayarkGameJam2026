using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    /// <summary>
    /// 負責在 FixedUpdate 中錄製玩家的位置、瞄準座標與攻擊狀態。
    /// 完全使用 New Input System 進行讀取。
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerRecorder : MonoBehaviour
    {
        [Header("【元件參照】")]
        [SerializeField] private PlayerInput playerInput;

        private List<FrameData> currentLoopData = new List<FrameData>();
        private bool isRecording = false;

        private InputAction attackAction;
        private Camera mainCamera;

        private void OnEnable()
        {
            GameManager.OnGameEnd?.AddListener(StopAndSave);
        }

        private void OnDisable()
        {
            GameManager.OnGameEnd?.RemoveListener(StopAndSave);
        }

        private void Awake()
        {
            // 自動獲取 PlayerInput
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            // 初始化 Attack Action
            if (playerInput != null)
            {
                attackAction = playerInput.actions.FindAction("Attack");
            }
            else
            {
                Debug.LogWarning("PlayerRecorder: 找不到 PlayerInput 元件，將無法透過 Input Action 讀取攻擊狀態！");
            }

            // 緩存攝影機
            mainCamera = Camera.main;

            StartRecording();
        }

        //private void Update()
        //{
        //    // 偵測空白鍵 (Space)，若按下則存檔並重開關卡以測試殘影
        //    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        //    {
        //        if (isRecording)
        //        {
        //            StopAndSave();
        //        }
        //        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //    }
        //}

        /// <summary>
        /// Phase 2 開始時呼叫此方法開始錄製
        /// </summary>
        public void StartRecording()
        {
            currentLoopData.Clear();
            isRecording = true;
            Debug.Log("【Recorder】開始錄製玩家軌跡...");
        }

        /// <summary>
        /// Phase 2 結束 (死亡或通關) 時呼叫此方法停止並存檔
        /// </summary>
        private void StopAndSave(bool isWin)
        {
            isRecording = false;
            GhostManager.Instance.SaveLoopData(currentLoopData);
            Debug.Log($"【Recorder】錄製結束，共儲存了 {currentLoopData.Count} 幀資料。");
        }

        private void FixedUpdate()
        {
            if (!isRecording) return;

            // 1. 紀錄玩家當前位置
            Vector2 currentPos = transform.position;

            // 2. 紀錄滑鼠的世界座標
            Vector2 aimPos = GetMouseWorldPosition();

            // 3. 紀錄攻擊狀態 (優先使用 Action，若無則抓取滑鼠左鍵)
            bool attacking = false;
            if (attackAction != null)
            {
                attacking = attackAction.IsPressed();
            }
            else if (Mouse.current != null)
            {
                attacking = Mouse.current.leftButton.isPressed;
            }

            // 存入陣列
            currentLoopData.Add(new FrameData
            {
                position = currentPos,
                aimWorldPosition = aimPos,
                isAttacking = attacking
            });
        }

        /// <summary>
        /// 獲取滑鼠在世界空間中的座標 (純 New Input System)
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            // 攝影機防呆機制 (與 WeaponController 一致)
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = Object.FindFirstObjectByType<Camera>();
                    if (mainCamera == null) return transform.position;
                }
            }

            Vector3 mouseScreenPos = Vector3.zero;

            // 透過 New Input System 抓取游標位置
            if (Mouse.current != null)
            {
                mouseScreenPos = Mouse.current.position.ReadValue();
            }

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            worldPos.z = 0f;
            return worldPos;
        }
    }
}