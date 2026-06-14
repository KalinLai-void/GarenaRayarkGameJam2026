using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 全域音效管理器，負責播放 BGM、環境音與各式 SE。
    /// 支援中文化欄位說明與防呆設計。
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<AudioManager>();
                    if (instance == null)
                    {
                        var prefab = Resources.Load<GameObject>("AudioManager");
                        if (prefab != null)
                        {
                            var go = Instantiate(prefab);
                            instance = go.GetComponent<AudioManager>();
                            if (instance.transform.parent != null)
                            {
                                instance.transform.SetParent(null);
                            }
                            DontDestroyOnLoad(go);
                        }
                        else
                        {
                            var go = new GameObject("AudioManager (Auto Generated)");
                            instance = go.AddComponent<AudioManager>();
                            Debug.LogWarning("[AudioManager] 在場景與 Resources 中皆找不到 AudioManager，已自動生成空物件。請確認是否有配置音效！");
                        }
                    }
                }
                return instance;
            }
        }

        [Header("--- 核心播放組件 (Audio Sources) ---")]
        [Tooltip("負責播放背景音樂 (BGM) 的 AudioSource")]
        [SerializeField] private AudioSource bgmSource;

        [Tooltip("負責播放單次音效 (SFX/SE) 的 AudioSource")]
        [SerializeField] private AudioSource sfxSource;

        [Tooltip("負責播放持續/循環音效 (如殘血警報) 的 AudioSource")]
        [SerializeField] private AudioSource loopSfxSource;

        [Header("--- 背景音樂 (BGM) ---")]
        [Tooltip("1. 標題畫面背景音樂")]
        public AudioClip titleBgm;
        
        [Tooltip("2. 遊戲主體戰鬥背景音樂")]
        public AudioClip gameBgm;

        [Tooltip("3. 過場動畫或劇情背景音樂")]
        public AudioClip cutsceneBgm;

        [Tooltip("5. 遊戲勝利/通關音效")]
        public AudioClip gameClearBgm;

        [Header("--- 遊戲結束與結算 (GameEnd SE) ---")]
        [Tooltip("4. 玩家死亡/被擊敗音效")]
        public AudioClip playerDeathSe;

        [Header("--- 戰鬥與反饋音效 (Combat SE) ---")]
        [Tooltip("6. 玩家受傷受擊音效")]
        public AudioClip playerHurtSe;

        [Tooltip("7. 玩家射擊/開火音效")]
        public AudioClip shootSe;

        [Tooltip("8. 敵人受傷受擊音效")]
        public AudioClip enemyHurtSe;

        [Tooltip("敵人死亡/慘叫音效")]
        public AudioClip enemyDeathSe;

        [Tooltip("15. 玩家殘血警告音效 (通常為警報循環聲)")]
        public AudioClip lowHealthSe;

        [Header("--- 道具與 Tinder 介面音效 (UI & Match SE) ---")]
        [Tooltip("9. Tinder 卡牌配對成功音效 (Like 右滑)")]
        public AudioClip matchSuccessSe;

        [Tooltip("10. 撿起道具/經驗值水晶音效")]
        public AudioClip pickupItemSe;

        [Tooltip("14. UI 按鈕通用點擊音效")]
        public AudioClip buttonClickSe;

        [Tooltip("Tinder 卡牌向左滑動 (Nope/不喜歡) 音效")]
        public AudioClip tinderSwipeLeftSe;

        [Tooltip("Tinder 卡牌向右滑動 (Like/喜歡) 音效")]
        public AudioClip tinderSwipeRightSe;

        [Tooltip("手機介面跳出來音效")]
        public AudioClip phonePopupSe;

        [Tooltip("5秒倒數滴答聲音效")]
        public AudioClip timerTickSe;

        [Header("--- 技能專用音效 (Skill SE) ---")]
        [Tooltip("11. 主動/被動治癒回血音效")]
        public AudioClip healSe;

        [Tooltip("12. 珊瑚菇雷射光束發射/持續音效")]
        public AudioClip laserBeamSe;

        [Tooltip("13. 猴頭菇爆炸/炸彈引爆音效")]
        public AudioClip bombSe;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                // 確保切換場景時音效管理器不會被銷毀
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);

                // 如果沒有手動配置 AudioSource，在執行期自動建立以防 NullReference
                InitializeAudioSources();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
            if (loopSfxSource == null)
            {
                loopSfxSource = gameObject.AddComponent<AudioSource>();
                loopSfxSource.loop = true;
                loopSfxSource.playOnAwake = false;
            }
        }

        // ==========================================
        // 🎵 背景音樂 (BGM) 控制邏輯
        // ==========================================

        /// <summary>
        /// 播放背景音樂，支援切換防呆與漸變
        /// </summary>
        public void PlayBGM(AudioClip clip, bool loop = true)
        {
            if (bgmSource == null) return;
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] 嘗試播放 BGM，但傳入的 BGM Clip 為空！");
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        public void PlayTitleBGM() => PlayBGM(titleBgm, true);
        public void PlayGameBGM() => PlayBGM(gameBgm, true);
        public void PlayCutsceneBGM() => PlayBGM(cutsceneBgm, true);

        /// <summary>
        /// 停止背景音樂
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }

        // ==========================================
        // 🔊 單次音效 (SFX / SE) 播放快捷鍵
        // ==========================================

        /// <summary>
        /// 4. 播放死亡音效
        /// </summary>
        public void PlayPlayerDeath() => SafePlaySFX(playerDeathSe);

        /// <summary>
        /// 5. 播放通關音樂
        /// </summary>
        public void PlayGameClear()
        {
            // 將過關改為背景音樂 (BGM) 管道播放（預設不循環，以防是一次性過關音樂）
            PlayBGM(gameClearBgm, false);
        }

        /// <summary>
        /// 6. 播放玩家受傷音效
        /// </summary>
        public void PlayPlayerHurt() => SafePlaySFX(playerHurtSe);

        /// <summary>
        /// 7. 播放射擊音效
        /// </summary>
        public void PlayShoot() => SafePlaySFX(shootSe);

        /// <summary>
        /// 8. 播放敵人受傷音效
        /// </summary>
        public void PlayEnemyHurt() => SafePlaySFX(enemyHurtSe);

        /// <summary>
        /// 播放敵人死亡/慘叫音效
        /// </summary>
        public void PlayEnemyDeath() => SafePlaySFX(enemyDeathSe);

        /// <summary>
        /// 9. 播放配對成功音效
        /// </summary>
        public void PlayMatchSuccess() => SafePlaySFX(matchSuccessSe);

        /// <summary>
        /// 10. 播放撿到道具音效
        /// </summary>
        public void PlayPickupItem() => SafePlaySFX(pickupItemSe);

        /// <summary>
        /// 11. 播放回血音效
        /// </summary>
        public void PlayHeal() => SafePlaySFX(healSe);

        /// <summary>
        /// 12. 播放光束音效
        /// </summary>
        public void PlayLaserBeam() => SafePlaySFX(laserBeamSe);

        /// <summary>
        /// 13. 播放炸彈/爆炸音效
        /// </summary>
        public void PlayBomb() => SafePlaySFX(bombSe);

        /// <summary>
        /// 14. 播放按鈕點擊音效
        /// </summary>
        public void PlayButtonClick() => SafePlaySFX(buttonClickSe);

        /// <summary>
        /// Tinder 往左滑動音效
        /// </summary>
        public void PlayTinderSwipeLeft() => SafePlaySFX(tinderSwipeLeftSe);

        /// <summary>
        /// Tinder 往右滑動音效
        /// </summary>
        public void PlayTinderSwipeRight() => SafePlaySFX(tinderSwipeRightSe);

        /// <summary>
        /// 手機介面跳出來音效
        /// </summary>
        public void PlayPhonePopup() => SafePlaySFX(phonePopupSe);

        /// <summary>
        /// 5秒倒數滴答音效
        /// </summary>
        public void PlayTimerTick() => SafePlaySFX(timerTickSe);

        // ==========================================
        // 🚨 殘血循環警報音效控制
        // ==========================================

        /// <summary>
        /// 15. 開啟/關閉殘血循環音效
        /// </summary>
        public void SetLowHealthWarning(bool active)
        {
            if (loopSfxSource == null || lowHealthSe == null) return;

            if (active)
            {
                if (!loopSfxSource.isPlaying)
                {
                    loopSfxSource.clip = lowHealthSe;
                    loopSfxSource.Play();
                }
            }
            else
            {
                if (loopSfxSource.isPlaying)
                {
                    loopSfxSource.Stop();
                }
            }
        }

        // ==========================================
        // 🛡️ 防呆播放核心核心
        // ==========================================
        private void SafePlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
            else if (clip == null)
            {
                // 可以選擇在開發時期印出 Log 提醒配置
                // Debug.LogWarning($"[AudioManager] 嘗試播放音效，但 Inspector 欄位未指派音效檔！");
            }
        }
    }
}
