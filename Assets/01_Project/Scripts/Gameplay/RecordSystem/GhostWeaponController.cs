using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 殘影專用的武器控制器。
    /// 不抓取任何玩家輸入，純粹接收 GhostPlayer 傳來的歷史座標進行瞄準與射擊。
    /// </summary>
    public sealed class GhostWeaponController : MonoBehaviour
    {
        [Header("--- 武器基礎數值 ---")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float fireRate = 5f;

        [Header("--- 圓圈軌道設定 ---")]
        [SerializeField] private float orbitRadius = 1.2f;
        [SerializeField] private Vector2 spriteMuzzleDirection = Vector2.left;

        [Header("--- 參照組件 ---")]
        [SerializeField] private GameObject ghostBulletPrefab; // 建議用專屬的殘影子彈Prefab
        [SerializeField] private Transform muzzlePoint;

        private float nextFireTime;
        private float initialMuzzleY;

        private GameObject pool;

        private void Awake()
        {
            if (muzzlePoint != null)
            {
                initialMuzzleY = muzzlePoint.localPosition.y;
            }

            pool = new GameObject("GhostBullets_Pool");
        }

        /// <summary>
        /// 由 GhostPlayer 在 FixedUpdate 中呼叫
        /// </summary>
        public void ProcessGhostInput(Vector2 aimWorldPosition, bool isAttacking)
        {
            RotateAndLockToOrbit(aimWorldPosition);

            if (isAttacking && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + (1f / fireRate);
                Shoot();
            }
        }

        // 邏輯與你的原版相同，只是把滑鼠座標換成了傳入的 aimWorldPosition
        private void RotateAndLockToOrbit(Vector2 aimWorldPosition)
        {
            Vector3 parentPos = transform.parent != null ? transform.parent.position : Vector3.zero;
            Vector3 diff = (Vector3)aimWorldPosition - parentPos;
            Vector2 direction = diff.sqrMagnitude > 0.001f ? ((Vector2)diff).normalized : Vector2.right;

            // 1. 鎖定軌道
            transform.position = (Vector2)parentPos + (direction * orbitRadius);

            // 2. 旋轉角度
            float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float localMuzzleAngle = Mathf.Atan2(spriteMuzzleDirection.y, spriteMuzzleDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, mouseAngle - localMuzzleAngle);

            // 3. 翻轉防呆
            if (aimWorldPosition.x < parentPos.x)
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
                if (muzzlePoint != null)
                    muzzlePoint.localPosition = new Vector3(muzzlePoint.localPosition.x, initialMuzzleY, muzzlePoint.localPosition.z);
            }
            else
            {
                transform.localScale = new Vector3(1f, -1f, 1f);
                if (muzzlePoint != null)
                    muzzlePoint.localPosition = new Vector3(muzzlePoint.localPosition.x, -initialMuzzleY, muzzlePoint.localPosition.z);
            }
        }

        private void Shoot()
        {
            if (ghostBulletPrefab != null && muzzlePoint != null)
            {
                Vector2 fireDirection = ((Vector2)(muzzlePoint.position - transform.position)).normalized;
                float fireAngle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
                Quaternion bulletRotation = Quaternion.Euler(0f, 0f, fireAngle);

                GameObject bulletGo = Instantiate(ghostBulletPrefab, muzzlePoint.position, bulletRotation);
                bulletGo.transform.parent = pool.transform;
                GhostBullet bulletScript = bulletGo.GetComponent<GhostBullet>();
                if (bulletScript != null)
                {
                    bulletScript.Setup(damage);
                }
            }
        }
    }
}