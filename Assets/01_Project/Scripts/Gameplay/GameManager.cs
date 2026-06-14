using Common;
using System;
using UnityEngine;
using UnityEngine.Events;
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        Debug.Log("[GameManager] Title");
        currentStage = State.OnTitle;
        onTitleEvent?.Invoke();
    }

    public void GoToTitle()
    {
        Init();
        SceneManager.LoadScene(titleScene?.SceneName);
    }

    public void GoToStory()
    {
        Debug.Log("[GameManager] Story");
        currentStage = State.OnStory;
        onStoryEvent?.Invoke();
        SceneManager.LoadScene(storyScene?.SceneName);
    }

    private void StartGame()
    {
        Debug.Log("[GameManager] GameStart");
        currentStage = State.OnGameStart;
        onGameStartEvent?.Invoke();
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
}
