using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
            }

            if (instance == null)
            {
                GameObject obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();
            }

            //if (instance == null)
            //{
            //    Debug.LogError($"[Singleton] {typeof(T)} 인스턴스가 씬에 없습니다. 직접 배치해야 합니다.");
            //}

            return instance;
        }
    }
    // Start is called before the first frame update
    public virtual void Awake()
    {
        if (instance == null)
        {
            Debug.Log($"{typeof(T)} set to Don't DestroyOnLoad Object");
            instance = this as T;
            // DontDestroyOnLoad(gameObject.transform.root.gameObject);
        }
        else
        {
            Debug.Log($"{typeof(T)} object was destroyed");
            Destroy(gameObject);
        }
    }
}