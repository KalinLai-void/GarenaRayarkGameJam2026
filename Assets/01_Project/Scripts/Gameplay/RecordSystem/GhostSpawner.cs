using System.Collections.Generic;
using UnityEngine;

public class GhostSpawner : MonoBehaviour
{
    public GameObject ghostPrefab; // 你的殘影 Prefab

    private void Start()
    {
        // 向 Manager 要近 3 輪的資料
        List<List<FrameData>> allShadows = GhostManager.Instance.GetAllShadowData();

        // 有幾輪資料，就生成幾個殘影
        foreach (List<FrameData> shadowData in allShadows)
        {
            // 生成殘影物件
            GameObject ghost = Instantiate(ghostPrefab, Vector3.zero, Quaternion.identity);

            // 把這包資料餵給殘影播放器
            GhostPlayer ghostPlayer = ghost.GetComponent<GhostPlayer>();
            if (ghostPlayer != null)
            {
                ghostPlayer.InitializeReplay(shadowData);
            }
        }
    }
}
