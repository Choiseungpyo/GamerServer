using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class ObjectPoolBase<T> : MonoBehaviour where T : PoolableObject
{
    [SerializeField] private T prefab;
    [SerializeField] protected int maxCount; // �÷��̾� �ִ� ���� 8���̱� ����

    protected Queue<T> pool;
    protected Dictionary<int, T> activeDict;

    protected virtual void Awake()
    {
        pool = new Queue<T>();
        activeDict = new Dictionary<int, T>();

        for (int i = 0; i < maxCount; i++)
        {
            T obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T Get()
    {
        T obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            // ��� ��� ���̸� ���� ������ �� ����
            var oldest = activeDict.Values.First();
            oldest.Despawn();
            activeDict.Remove(oldest.ID);
            obj = oldest;
        }

        obj.Spawn();

        obj.ID = activeDict.Count;
        activeDict[obj.ID] = obj;  // id ���� ���

        return obj;
    }


    public T Get(int id)
    {
        // id�� �̹� �����ϴ� ���
        if (activeDict.TryGetValue(id, out T existing))
        {
            return null;
        }

        T obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            // ��� ��� ���̸� ���� ������ �� ����
            var oldest = activeDict.Values.First();
            oldest.Despawn();
            activeDict.Remove(oldest.ID); 
            obj = oldest;
        }

        obj.Spawn();

        obj.ID = id;
        activeDict[id] = obj;  // id ���� ���
        
        return obj;
    }

    public void Release(int id)
    {
        if (activeDict.TryGetValue(id, out T obj))
        {
            obj.Despawn();
            activeDict.Remove(id);
            pool.Enqueue(obj);
        }
    }
}