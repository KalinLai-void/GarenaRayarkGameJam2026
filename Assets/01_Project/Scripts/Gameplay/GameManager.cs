using Common;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum State { OnTitle, OnStory, OnGameStart, OnPhase1Start, OnPhase1Finish, OnPhase2Start, OnPhase2Finish, OnGameEnd }

    [SerializeField] private SceneReference titleScene;
    [SerializeField] private SceneReference gameScene;
    [SerializeField] private SceneReference storyScene;

    [SerializeField] private UnityEvent onTitleEvent = new();
    [SerializeField] private UnityEvent onStoryEvent = new();

    public static UnityEvent OnTitle => Instance?.onTitleEvent;
    public static UnityEvent OnStory => Instance?.onStoryEvent;

    [SerializeField] private UnityEvent onGameStartEvent = new();
    [SerializeField] private UnityEvent onPhase1StartEvent = new();
    [SerializeField] private UnityEvent onPhase1FinishEvent = new();
    public static UnityEvent OnGameStart => Instance?.onGameStartEvent;
    public static UnityEvent OnPhase1Start => Instance?.onPhase1StartEvent;
    public static UnityEvent OnPhase1Finish => Instance?.onPhase1FinishEvent;

    [SerializeField] private UnityEvent onPhase2StartEvent = new();
    [SerializeField] private UnityEvent onPhase2FinishEvent = new();
    [SerializeField] private UnityEvent<bool> onGameEndEvent = new();
    public static UnityEvent OnPhase2Start => Instance?.onPhase2StartEvent;
    public static UnityEvent OnPhase2Finish => Instance?.onPhase2FinishEvent;
    public static UnityEvent<bool> OnGameEnd => Instance?.onGameEndEvent;

    public static State currentStage;

    public static void TriggerGoToTitle() => Instance?.GoToTitle();
    public static void TriggerGoToStory() => Instance?.GoToStory();
    public static void TriggerGameStart() => Instance?.StartGame();
    public static void TriggerPhase1Start() => Instance?.EnterPhase1();
    public static void TriggerPhase2Start() => Instance?.EnterPhase2();
    public static void TriggerPhase1Finish() => Instance?.FinishPhase1();
    public static void TriggerPhase2Finish() => Instance?.FinishPhase2();
    public static void TriggerGameEnd(bool isWin) => Instance?.EndGame(isWin);

    private int dieTime = 0;
    public static int GetDieTime() => Instance.dieTime;

    private void Awake()
    {   
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == gameScene?.SceneName || SceneManager.GetActiveScene().name == "Main")
        {
            Debug.Log("[GameManager] 直接在編輯器執行 Main 場景，自動播放遊戲背景音樂");
            currentStage = State.OnGameStart;
            onGameStartEvent?.Invoke();
            if (Gameplay.AudioManager.Instance != null)
            {
                Gameplay.AudioManager.Instance.PlayGameBGM();
            }
        }
        else
        {
            Init();
        }
    }

    private void Update()
    {
        // 【防呆修正】如果在標題畫面或劇情畫面，絕對不要執行滑鼠鎖定邏輯
        if (currentStage == State.OnTitle || currentStage == State.OnStory) return;

        // 偵測按下 ESC 鍵 -> 解除鎖定並顯示滑鼠
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        // 玩家按 ESC 跳出後，如果點擊遊戲畫面，自動把滑鼠重新鎖回去
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    private void Init()
    {
        Debug.Log("[GameManager] Title");
        currentStage = State.OnTitle;
        onTitleEvent?.Invoke();

        // 播放標題背景音樂
        if (Gameplay.AudioManager.Instance != null)
        {
            Gameplay.AudioManager.Instance.PlayTitleBGM();
        }
    }

    public void GoToTitle()
    {
        Init();
        SceneManager.LoadScene(titleScene?.SceneName);
        UnlockCursor();
    }

    public void GoToStory()
    {
        Debug.Log("[GameManager] Story");
        currentStage = State.OnStory;
        onStoryEvent?.Invoke();

        // 播放劇情/過場背景音樂
        if (Gameplay.AudioManager.Instance != null)
        {
            Gameplay.AudioManager.Instance.PlayCutsceneBGM();
        }

        SceneManager.LoadScene(storyScene?.SceneName);
    }

    private void StartGame()
    {
        LockCursor();

        Debug.Log("[GameManager] GameStart");
        currentStage = State.OnGameStart;
        onGameStartEvent?.Invoke();

        // 🎵 播放遊戲背景音樂
        if (Gameplay.AudioManager.Instance != null)
        {
            Gameplay.AudioManager.Instance.PlayGameBGM();
        }
        SceneManager.LoadScene(gameScene?.SceneName);
    }

    private void EnterPhase1()
    {
        Debug.Log("[GameManager] EnterPhase1");
        currentStage = State.OnPhase1Start;
        onPhase1StartEvent?.Invoke();
    }

    private void FinishPhase1()
    {
        Debug.Log("[GameManager] FinishPhase1");
        currentStage = State.OnPhase1Finish;
        onPhase1FinishEvent?.Invoke();
        TriggerPhase2Start();
    }

    private void EnterPhase2()
    {
        Debug.Log("[GameManager] EnterPhase2");
        currentStage = State.OnPhase2Start;
        onPhase2StartEvent?.Invoke();
    }

    private void FinishPhase2()
    {
        Debug.Log("[GameManager] FinishPhase2");
        currentStage = State.OnPhase2Finish;
        onPhase2FinishEvent?.Invoke();
        Invoke(nameof(EnterPhase1), 0f);
    }

    private void EndGame(bool isWin)
    {
        Debug.Log("[GameManager] EndGame");
        currentStage = State.OnGameEnd;
        onGameEndEvent?.Invoke(isWin);

        // 🎵 播放結局對應的音效
        if (Gameplay.AudioManager.Instance != null)
        {
            if (isWin)
            {
                Gameplay.AudioManager.Instance.PlayGameClear();
            }
            else
            {
                Gameplay.AudioManager.Instance.PlayPlayerDeath();
            }
        }

        if (!isWin)
        {
            dieTime++;
            GameEndManager.instance.DieGame();
            Invoke("StartGame", 1f);
        }
        else
        {
            dieTime = 0;
            GameEndManager.instance.PassGame();
            Invoke("GoToTitle", 1.5f);
        }
    }

    /// <summary>
    /// 鎖定並隱藏游標 (遊戲進行中狀態)
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 把游標鎖在視窗正中心
        //Cursor.visible = false;                   // 隱藏游標圖案
    }

    /// <summary>
    /// 解除鎖定並顯示游標 (按 ESC 跳出狀態)
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;   // 解除鎖定，讓游標可以自由移動
        //Cursor.visible = true;                    // 顯示游標圖案
    }
}
