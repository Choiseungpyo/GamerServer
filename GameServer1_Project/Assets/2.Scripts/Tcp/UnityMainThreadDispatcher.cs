using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executionQueue = new Queue<Action>();

    public void Update()
    {
        while (executionQueue.Count > 0)
        {
            executionQueue.Dequeue().Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    private static UnityMainThreadDispatcher instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("UnityMainThreadDispatcher instance creation must be called from the main thread.");
                return null;
            }

            var obj = GameObject.Find("UnityMainThreadDispatcher");
            if (obj != null)
            {
                instance = obj.GetComponent<UnityMainThreadDispatcher>();
            }

            if (instance == null)
            {
                obj = new GameObject("UnityMainThreadDispatcher");
                instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
        }
        return instance;
    }

}