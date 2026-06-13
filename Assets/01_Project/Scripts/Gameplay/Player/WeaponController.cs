using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    /// <summary>
    /// 控制武器在角色周圍的圓圈軌道鎖定、滑鼠跟隨與射擊升級擴展。
    /// </summary>
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("--- 武器基礎數值 (不變) ---")]
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float baseFireRate = 5f;
        [SerializeField] private int baseMaxAmmo = 30;

        [Header("--- 裝彈設定 ---")]
        [SerializeField] private float reloadTime = 0.1f;
        [SerializeField] private bool infiniteAmmo = true; // 是否無限彈藥


        [Header("--- 圓圈軌道設定 ---")]
        [SerializeField] private float orbitRadius = 1.2f; // 武器離角色中心的固定距離

        [Header("--- 槍身朝向設定 ---")]
        [Tooltip("圖片中槍口朝向的本地方向 (例如：SimpleGun_Left 朝左，請填 (-1, 0)；若朝右，請填 (1, 0))")]
        [SerializeField] private Vector2 spriteMuzzleDirection = Vector2.left;

        [Header("--- 參照組件 ---")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("--- 參照輸入組件 (選填) ---")]
        [SerializeField] private PlayerInput playerInput;

        private GameObject pool;

        // --- 未來升級系統可直接修改的加成值 ---
        private int bonusDamage = 0;
        private float bonusFireRate = 0f;
        private int bonusMaxAmmo = 0;

        // --- 供內部與外部讀取的「最終實際數值」 ---
        public int FinalDamage => baseDamage + bonusDamage;
        public float FinalFireRate => baseFireRate + bonusFireRate + (PlayerSkillSystem.Instance != null ? GetNeedleFireRateBonus() : 0f);
        public int FinalMaxAmmo => baseMaxAmmo + bonusMaxAmmo;

        private float GetNeedleFireRateBonus()
        {
            int lvl = PlayerSkillSystem.Instance.GetSkillLevel("R_NeedleMushroom");
            if (lvl <= 0) return 0f;
            SkillData data = PlayerSkillSystem.Instance.GetSkillData("R_NeedleMushroom");
            if (data != null && data.needleFireRateBonusPercent != null && lvl <= data.needleFireRateBonusPercent.Length)
            {
                return baseFireRate * data.needleFireRateBonusPercent[lvl - 1];
            }
            return baseFireRate * (0.15f + 0.05f * (lvl - 1));
        }

        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => isReloading;

        private int currentAmmo;
        private float nextFireTime;
        private bool isReloading;
        private float initialMuzzleY;

        private InputAction attackAction;
        private Camera mainCamera;

        private void Awake()
        {
            if (playerInput == null)
            {
                playerInput = GetComponentInParent<PlayerInput>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            mainCamera = Camera.main;
            
            if (muzzlePoint != null)
            {
                initialMuzzleY = muzzlePoint.localPosition.y;
            }

            pool = new GameObject("Bullets_Pool");
        }

        private void Start()
        {
            currentAmmo = FinalMaxAmmo;

            if (playerInput != null)
            {
                attackAction = playerInput.actions.FindAction("Attack");
            }
        }

        private void Update()
        {
            RotateAndLockToOrbit();

            if (isReloading) return;

            // 沒子彈自動裝彈 (無限彈藥時跳過)
            if (!infiniteAmmo && currentAmmo <= 0)
            {
                StartCoroutine(ReloadRoutine());
                return;
            }

            // 偵測射擊輸入：優先使用 New Input System 的 Action，若無則防呆使用 Mouse.current，最後 fallback 到舊版 Input
            bool isAttacking = false;
            if (attackAction != null)
            {
                isAttacking = attackAction.IsPressed();
            }
            else if (UnityEngine.InputSystem.Mouse.current != null)
            {
                isAttacking = UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
            }
            else
            {
                isAttacking = Input.GetMouseButton(0);
            }

            if (isAttacking && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + (1f / FinalFireRate);
                Shoot();
            }
        }

        // 【核心修正】強迫武器鎖定在角色周圍的圓圈軌道，防止座標異常扭曲
        private void RotateAndLockToOrbit()
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            if (mainCamera == null) return; // 若無攝影機則不執行

            // 取得父物件 (Player) 的世界座標，若無父物件則以原點為準
            Vector3 playerPos = transform.parent != null ? transform.parent.position : Vector3.zero;

            // 計算方向並防呆防 NaN
            Vector3 diff = mousePosition - playerPos;
            Vector2 direction = diff.sqrMagnitude > 0.001f ? ((Vector2)diff).normalized : Vector2.right;

            // 1. 強制將武器位置鎖定在圓圈軌道上
            transform.position = (Vector2)playerPos + (direction * orbitRadius);

            // 2. 讓武器朝向滑鼠角度 (根據槍口在圖片中的本地方向做旋轉補償)
            float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float localMuzzleAngle = Mathf.Atan2(spriteMuzzleDirection.y, spriteMuzzleDirection.x) * Mathf.Rad2Deg;
            float finalWeaponAngle = mouseAngle - localMuzzleAngle;
            transform.rotation = Quaternion.Euler(0f, 0f, finalWeaponAngle);

            // 3. 依照滑鼠 X 方向與玩家的相對位置進行 Y 軸翻轉，防止武器上下顛倒 (SimpleGun_Left 預設朝左)
            if (mousePosition.x < playerPos.x)
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
                if (muzzlePoint != null)
                {
                    muzzlePoint.localPosition = new Vector3(muzzlePoint.localPosition.x, initialMuzzleY, muzzlePoint.localPosition.z);
                }
            }
            else
            {
                transform.localScale = new Vector3(1f, -1f, 1f);
                if (muzzlePoint != null)
                {
                    muzzlePoint.localPosition = new Vector3(muzzlePoint.localPosition.x, -initialMuzzleY, muzzlePoint.localPosition.z);
                }
            }
        }

        /// <summary>
        /// 獲取滑鼠在世界空間中的座標，支援新舊輸入系統與 MainCamera 遺失防呆
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = Object.FindFirstObjectByType<Camera>();
                    if (mainCamera == null)
                    {
                        mainCamera = Object.FindAnyObjectByType<Camera>();
                    }
                }
            }

            if (mainCamera == null) return Vector3.zero;

            Vector3 mouseScreenPos = Vector3.zero;
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else
            {
                mouseScreenPos = Input.mousePosition;
            }

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            worldPos.z = 0f;
            return worldPos;
        }

        private void Shoot()
        {
            if (!infiniteAmmo)
            {
                currentAmmo--;
            }
            Debug.Log($"【Console Log】武器射擊！剩餘彈藥: {(infiniteAmmo ? "無限" : currentAmmo.ToString())}/{FinalMaxAmmo}");

            if (bulletPrefab != null && muzzlePoint != null)
            {
                Vector2 fireDirection = ((Vector2)(muzzlePoint.position - transform.position)).normalized;
                float fireAngle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;

                // 1. 讀取技能系統的各項加成數值
                int needleLvl = PlayerSkillSystem.Instance != null ? PlayerSkillSystem.Instance.GetSkillLevel("R_NeedleMushroom") : 0;
                int whiteElfLvl = PlayerSkillSystem.Instance != null ? PlayerSkillSystem.Instance.GetSkillLevel("R_WhiteElf") : 0;
                int kingOysterLvl = PlayerSkillSystem.Instance != null ? PlayerSkillSystem.Instance.GetSkillLevel("R_KingOyster") : 0;
                bool isCoralActive = PlayerSkillSystem.Instance != null && PlayerSkillSystem.Instance.IsActiveSkillEffectRunning && PlayerSkillSystem.Instance.ActiveSkill.skillID == "SR_CoralMushroom";
                int coralLvl = PlayerSkillSystem.Instance != null ? PlayerSkillSystem.Instance.GetSkillLevel("SR_CoralMushroom") : 0;

                // 2. 計算傷害、體積與速度
                int damage = FinalDamage;
                if (kingOysterLvl > 0)
                {
                    SkillData kingData = PlayerSkillSystem.Instance.GetSkillData("R_KingOyster");
                    if (kingData != null && kingData.kingOysterDamageBonusPercent != null && kingOysterLvl <= kingData.kingOysterDamageBonusPercent.Length)
                    {
                        damage += (int)(baseDamage * kingData.kingOysterDamageBonusPercent[kingOysterLvl - 1]);
                    }
                    else
                    {
                        damage += (int)(baseDamage * 0.05f * kingOysterLvl);
                    }
                }
                if (isCoralActive)
                {
                    SkillData coralData = PlayerSkillSystem.Instance.GetSkillData("SR_CoralMushroom");
                    if (coralData != null && coralData.coralDamageBonusPercent != null && coralLvl <= coralData.coralDamageBonusPercent.Length)
                    {
                        damage += (int)(baseDamage * coralData.coralDamageBonusPercent[coralLvl - 1]);
                    }
                    else
                    {
                        damage += (int)(baseDamage * (0.10f + 0.10f * (coralLvl - 1)));
                    }
                }

                float scaleMultiplier = 1.0f;
                if (kingOysterLvl > 0)
                {
                    SkillData kingData = PlayerSkillSystem.Instance.GetSkillData("R_KingOyster");
                    if (kingData != null && kingData.kingOysterScaleMultiplierBonus != null && kingOysterLvl <= kingData.kingOysterScaleMultiplierBonus.Length)
                    {
                        scaleMultiplier += kingData.kingOysterScaleMultiplierBonus[kingOysterLvl - 1];
                    }
                    else
                    {
                        scaleMultiplier += 0.10f * kingOysterLvl;
                    }
                }

                float bulletSpeed = 15f; // Bullet.cs 預設基礎速度為 15f
                if (needleLvl > 0)
                {
                    SkillData needleData = PlayerSkillSystem.Instance.GetSkillData("R_NeedleMushroom");
                    if (needleData != null && needleData.needleBulletSpeedMultiplier != null && needleLvl <= needleData.needleBulletSpeedMultiplier.Length)
                    {
                        bulletSpeed *= needleData.needleBulletSpeedMultiplier[needleLvl - 1];
                    }
                    else
                    {
                        bulletSpeed *= 1.10f;
                    }
                }

                // 3. 處理白精靈菇的多發扇形子彈
                int bulletCount = 1;
                float spreadAngle = 30f;
                if (whiteElfLvl > 0)
                {
                    SkillData whiteElfData = PlayerSkillSystem.Instance.GetSkillData("R_WhiteElf");
                    if (whiteElfData != null)
                    {
                        if (whiteElfData.whiteElfBulletCount != null && whiteElfLvl <= whiteElfData.whiteElfBulletCount.Length)
                        {
                            bulletCount = whiteElfData.whiteElfBulletCount[whiteElfLvl - 1];
                        }
                        else
                        {
                            bulletCount = whiteElfLvl + 1;
                        }

                        if (whiteElfData.whiteElfSpreadAngle != null && whiteElfLvl <= whiteElfData.whiteElfSpreadAngle.Length)
                        {
                            spreadAngle = whiteElfData.whiteElfSpreadAngle[whiteElfLvl - 1];
                        }
                        else
                        {
                            spreadAngle = whiteElfLvl >= 4 ? 45f : 30f;
                        }
                    }
                    else
                    {
                        bulletCount = whiteElfLvl + 1;
                        spreadAngle = whiteElfLvl >= 4 ? 45f : 30f;
                    }
                }

                float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;
                float startAngle = fireAngle - (bulletCount > 1 ? spreadAngle / 2f : 0f);

                for (int i = 0; i < bulletCount; i++)
                {
                    float angle = startAngle + i * angleStep;
                    Quaternion bulletRotation = Quaternion.Euler(0f, 0f, angle);

                    GameObject bulletGo = Instantiate(bulletPrefab, muzzlePoint.position, bulletRotation);
                    bulletGo.transform.parent = pool.transform;

                    // 4. 設定子彈體積 (若為珊瑚菇光束，另外將 X 軸拉伸呈光束形狀)
                    if (isCoralActive)
                    {
                        float beamW = 3.5f;
                        float beamH = 0.8f;
                        SkillData coralData = PlayerSkillSystem.Instance.GetSkillData("SR_CoralMushroom");
                        if (coralData != null)
                        {
                            beamW = coralData.coralBeamWidth;
                            beamH = coralData.coralBeamHeight;
                        }
                        bulletGo.transform.localScale = new Vector3(beamW, beamH, 1f) * scaleMultiplier;
                    }
                    else
                    {
                        bulletGo.transform.localScale = Vector3.one * scaleMultiplier;
                    }

                    Bullet bulletScript = bulletGo.GetComponent<Bullet>();
                    if (bulletScript != null)
                    {
                        // 帶入最終傷害、速度、是否為珊瑚菇光束等設定
                        bulletScript.Setup(damage, bulletSpeed, -1f, isCoralActive);
                    }
                }
            }
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            Debug.Log($"【Console Log】彈匣空了，自動進入裝彈狀態... (冷卻 {reloadTime} 秒)");

            yield return new WaitForSeconds(reloadTime);
            
            currentAmmo = FinalMaxAmmo;
            isReloading = false;
            Debug.Log($"【Console Log】裝彈完成！彈匣已回滿：{currentAmmo}/{FinalMaxAmmo}");
        }

        // --- 供未來 UpgradeManager 呼叫的擴展接口 ---
        public void UpgradeDamage(int amount)
        {
            bonusDamage += amount;
            Debug.Log($"【Upgrade】傷害增加 {amount}，目前 FinalDamage: {FinalDamage}");
        }

        public void UpgradeFireRate(float amount)
        {
            bonusFireRate += amount;
            Debug.Log($"【Upgrade】射速增加 {amount}，目前 FinalFireRate: {FinalFireRate}");
        }

        public void UpgradeMaxAmmo(int amount)
        {
            bonusMaxAmmo += amount;
            currentAmmo = FinalMaxAmmo; // 升級時順便幫玩家補滿彈匣
            Debug.Log($"【Upgrade】彈匣增加 {amount}，目前 FinalMaxAmmo: {FinalMaxAmmo}");
        }

        private void OnDrawGizmosSelected()
        {
            // 取得父物件 (Player) 的世界座標，若無父物件則以自身位置為準
            Vector3 playerPos = transform.parent != null ? transform.parent.position : transform.position;

            // 繪製綠色線框圓圈表示軌道範圍
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerPos, orbitRadius);
        }
    }
}
