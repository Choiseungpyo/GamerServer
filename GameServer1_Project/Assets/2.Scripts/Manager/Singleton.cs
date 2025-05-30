using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    private static object lockObj = new object();
    private static bool isShuttingDown = false;

    public static T Instance
    {
        get
        {
            if (isShuttingDown)
            {
                Debug.LogWarning($"[Singleton] {typeof(T)} 인스턴스는 앱 종료 중입니다.");
                return null;
            }

            lock (lockObj)
            {
                if (instance != null) return instance;

                instance = FindObjectOfType<T>();
                if (instance != null) return instance;

                // 인스턴스가 없으면 메인 스레드에서 생성되도록 요청
                CreateInstance();

                return instance;
            }
        }
    }

    private static void CreateInstance()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying) return;
#endif

        if (Application.isPlaying)
        {
            GameObject obj = new GameObject(typeof(T).Name);
            instance = obj.AddComponent<T>();
            DontDestroyOnLoad(obj);
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            isShuttingDown = true;
        }
    }
}