using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class MultipleObjectPoolBase<Key, Component>
    where Key : System.Enum
    where Component : UnityEngine.Component
{
    private readonly Dictionary<Key, ObjectPool<Component>> poolByKey = new Dictionary<Key, ObjectPool<Component>>();

    private readonly int defaultCapacity;
    private readonly int maxSize;
    private readonly bool collectionCheck;

    protected MultipleObjectPoolBase(int defaultCapacity = 10, int maxSize = 64, bool collectionCheck = false)
    {
        this.defaultCapacity = defaultCapacity < 0 ? 0 : defaultCapacity;
        this.maxSize = maxSize < 1 ? 1 : maxSize;
        this.collectionCheck = collectionCheck;
    }

    public bool HasPool(Key key)
    {
        return poolByKey.ContainsKey(key);
    }

    private void EnsurePool(Key key)
    {
        if (poolByKey.ContainsKey(key))
            return;

        var op = new ObjectPool<Component>(
            createFunc: () => CreateItem(key),
            actionOnGet: (c) => OnGet(key, c),
            actionOnRelease: (c) => OnRelease(key, c),
            actionOnDestroy: (c) =>
            {
                OnDestroy(key, c);
                DestroyItem(c);
            },
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        poolByKey.Add(key, op);
    }

    public Component Get(Key key)
    {
        EnsurePool(key);
        return poolByKey[key].Get();
    }

    public void Release(Key key, Component component)
    {
        if (component == null) return;

        if (!poolByKey.TryGetValue(key, out var op))
        {
            DestroyItem(component);
            return;
        }

        op.Release(component);
    }
    public void Prewarm(Key key, int count)
    {
        EnsurePool(key);
        if (count <= 0) return;

        var op = poolByKey[key];

        for (int i = 0; i < count; i++)
        {
            var item = CreateItem(key);
            if (item == null) continue;

            op.Release(item);
        }
    }

    public void Clear(Key key)
    {
        if (!poolByKey.TryGetValue(key, out var op))
            return;

        op.Clear();
        poolByKey.Remove(key);
    }

    public void ClearAll()
    {
        foreach (var kv in poolByKey)
            kv.Value.Clear();

        poolByKey.Clear();
    }

    protected abstract Component CreateItem(Key key);

    protected virtual void OnGet(Key key, Component component) { }
    protected virtual void OnRelease(Key key, Component component) { }
    protected virtual void OnDestroy(Key key, Component component) { }

    protected virtual void DestroyItem(Component component)
    {
        if (component == null) return;
        UnityEngine.Object.Destroy(component.gameObject);
    }
}