using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : Singleton<UnityMainThreadDispatcher>
{
    private readonly Queue<Action> queue = new Queue<Action>();
    private readonly object lockObj = new object();

    public void Enqueue(Action action)
    {
        if (action == null) return;
        lock (lockObj)
        {
            queue.Enqueue(action);
        }
    }

    private void Update()
    {
        while (true)
        {
            Action a = null;

            lock (lockObj)
            {
                if (queue.Count == 0) break;
                a = queue.Dequeue();
            }

            try
            {
                a();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}