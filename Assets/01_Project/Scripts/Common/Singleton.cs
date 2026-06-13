using UnityEngine;

namespace Common
{
    using UnityEngine;

    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        protected static T Instance
        {
            get
            {
                // 防止在程式關閉時再次觸發 Instance 導致殘留物
                if (_applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // 在場景中搜尋是否已經存在該物件
                        _instance = (T)FindAnyObjectByType(typeof(T));

                        // 如果場景中沒有，則建立一個新的
                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = $"(Singleton) {typeof(T)}";
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            // 如果是實體被手動刪除，標記為可重新建立（視需求調整）
            if (_instance == this)
            {
                _applicationIsQuitting = true;
                _instance = null;
                print($"Singleton {typeof(T)} is being destroyed. ");
            }
        }
        
        protected virtual void Awake()
        {
            // 確保手動拖入場景的物件也能正確處理重複問題
            if (_instance == null)
            {
                _instance = this as T;
                _applicationIsQuitting = false;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}