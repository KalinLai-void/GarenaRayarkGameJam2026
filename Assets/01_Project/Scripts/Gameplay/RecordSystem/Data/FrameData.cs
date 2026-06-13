using UnityEngine;

[System.Serializable]
public struct FrameData
{
    public Vector2 position;         // 玩家本體的座標
    public Vector2 aimWorldPosition; // 滑鼠當下的世界座標 (給殘影武器瞄準用)
    public bool isAttacking;         // 當下是否按住攻擊鍵
    // 如果你們有不同的子彈類型，也可以加一個 int attackType;
}