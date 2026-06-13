using System.Collections.Generic;
using UnityEngine;

public sealed class GhostManager : MonoBehaviour
{
    public static GhostManager Instance { get; private set; }

    [Header("【資料狀態 (僅供觀察)】")]
    [Tooltip("目前暫存的殘影數量")]
    [SerializeField] private int currentShadowCount = 0;

    // 使用 Queue 來管理「輪迴」，裡面裝的是每一輪的「軌跡陣列 (List)」
    private Queue<List<FrameData>> recentShadows = new Queue<List<FrameData>>();

    private void Awake()
    {
        // 標準的 Singleton 寫法
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 確保切換場景時，這個物件與資料會活著
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Phase 2 結束時，由 PlayerRecorder 呼叫此方法存入新資料
    /// </summary>
    public void SaveLoopData(List<FrameData> loopData)
    {
        // 【極度重要】必須 new 一個新的 List 來複製資料！
        // 否則 PlayerRecorder 下一局呼叫 .Clear() 時，歷史資料也會跟著消失。
        List<FrameData> clonedData = new List<FrameData>(loopData);

        // 把這輪資料塞進佇列
        recentShadows.Enqueue(clonedData);

        // 核心邏輯：如果超過 3 輪，就把最舊的那一輪丟掉 (Dequeue)
        if (recentShadows.Count > 3)
        {
            recentShadows.Dequeue();
            Debug.Log("【GhostManager】已移除最舊的殘影，保持在 3 輪以內。");
        }

        currentShadowCount = recentShadows.Count;
        Debug.Log($"【GhostManager】存檔成功！目前共有 {currentShadowCount} 個殘影準備在下一局生成。");
    }

    /// <summary>
    /// 呼叫此方法來獲取所有要生成的殘影資料
    /// </summary>
    public List<List<FrameData>> GetAllShadowData()
    {
        // 將 Queue 轉換回 List 方便生成器用 for 迴圈讀取
        return new List<List<FrameData>>(recentShadows);
    }

    /// <summary>
    /// 玩家真正死亡 (Game Over) 或重新開始遊戲時呼叫，清空所有資料
    /// </summary>
    public void ClearAllData()
    {
        recentShadows.Clear();
        currentShadowCount = 0;
        Debug.Log("【GhostManager】已清空所有殘影資料。");
    }
}