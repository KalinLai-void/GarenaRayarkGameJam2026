using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 管理玩家持有的道具數據。
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        // 儲存玩家目前擁有的道具名稱 (例如 "Map", "Key")
        private readonly HashSet<string> heldItems = new HashSet<string>();

        /// <summary>
        /// 判斷玩家目前是否持有某個特定道具
        /// </summary>
        public bool HasItem(string itemName)
        {
            return heldItems.Contains(itemName);
        }

        /// <summary>
        /// 新增道具到背包數據中
        /// </summary>
        public void AddItem(string itemName)
        {
            if (!heldItems.Contains(itemName))
            {
                heldItems.Add(itemName);
                Debug.Log($"【PlayerInventory】玩家獲得並持有了道具: {itemName}");
            }
        }

        /// <summary>
        /// 移除/消耗背包中的特定道具
        /// </summary>
        public void RemoveItem(string itemName)
        {
            if (heldItems.Contains(itemName))
            {
                heldItems.Remove(itemName);
                Debug.Log($"【PlayerInventory】玩家消耗/移除了道具: {itemName}");
            }
        }
    }
}
