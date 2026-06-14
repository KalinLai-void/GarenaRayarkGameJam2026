using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StoryTeller : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float endTime = 2f;

    [SerializeField] private List<string> texts;

    [Header("【元件參照】")]
    [Tooltip("要顯示文字的 TextMeshProUGUI 元件")]
    [SerializeField] private TextMeshProUGUI uiText;
    [SerializeField] private GameObject contObj;

    [Header("【打字設定】")]
    [Tooltip("每個字出現的間隔時間 (秒)")]
    [SerializeField] private float typingSpeed = 0.05f;

    // 供外部讀取的狀態：現在是否還在打字？
    public bool IsTyping { get; private set; }

    private Coroutine typingCoroutine;
    private string currentFullText;

    private bool isProcceed = false;

    private void Start()
    {
        Invoke("EndAnim", endTime);
    }

    private void Update()
    {
        if (!animator.GetBool("IsFirstPlay"))
        {
            if (!isProcceed)
            {
                isProcceed = true;
                Invoke("RunText", 1f);
            }
        }
    }

    private void EndAnim()
    {
        animator.SetBool("IsFirstPlay", false);
    }

    private void RunText()
    {
        StartTyping(texts[0]);
        if (contObj != null) contObj.SetActive(true);
    }

    public void EndStory()
    {
        GameManager.TriggerGameStart();
    }

    /// <summary>
    /// 傳入一串字串，開始逐字播放
    /// </summary>
    public void StartTyping(string textToType)
    {
        // 如果上一次的打字還沒結束，先強制停止
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        currentFullText = textToType;
        uiText.text = ""; // 先清空畫面上的字
        IsTyping = true;

        // 啟動逐字生成的協程
        typingCoroutine = StartCoroutine(TypeTextCoroutine());
    }

    /// <summary>
    /// 玩家點擊螢幕時，可以直接跳過動畫，瞬間顯示完整文字
    /// </summary>
    public void SkipTyping()
    {
        if (IsTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            uiText.text = currentFullText; // 瞬間填滿所有字
            IsTyping = false;
        }
    }

    // 核心：逐字生成的迴圈
    private IEnumerator TypeTextCoroutine()
    {
        foreach (char c in currentFullText.ToCharArray())
        {
            uiText.text += c; // 把字一個一個加進去
            yield return new WaitForSeconds(typingSpeed); // 等待設定的時間
        }

        // 全部打完後，狀態設為 false
        IsTyping = false;
    }
}
