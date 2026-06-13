using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 為子物件 Visual 加上漂浮與旋轉的動畫特效。
    /// </summary>
    public sealed class HoverEffect : MonoBehaviour
    {
        [Header("--- 漂浮設定 ---")]
        [SerializeField] private float amplitude = 0.15f;  // 漂浮上下振幅
        [SerializeField] private float frequency = 3f;     // 漂浮頻率/速度

        [Header("--- 旋轉設定 ---")]
        [SerializeField] private float rotationSpeed = 30f; // 旋轉速度 (度/秒)

        private Vector3 startLocalPos;

        private void Start()
        {
            startLocalPos = transform.localPosition;
        }

        private void Update()
        {
            // 1. 上下漂浮 (Mathf.Sin)
            float newY = startLocalPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
            transform.localPosition = new Vector3(startLocalPos.x, newY, startLocalPos.z);

            // 2. 旋轉
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }
}
