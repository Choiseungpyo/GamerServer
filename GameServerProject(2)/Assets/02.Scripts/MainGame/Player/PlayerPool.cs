using UnityEngine;

public class PlayerPool : ObjectPoolBase<Player>
{
    private readonly Player prefab;
    private readonly Transform poolRoot;

    public PlayerPool(Player prefab, Transform poolRoot, int defaultCapacity = 4, int maxSize = 16)
        : base(defaultCapacity, maxSize)
    {
        this.prefab = prefab;
        this.poolRoot = poolRoot;
    }

    protected override Player CreateObject()
    {
        if (prefab == null) return null;

        var p = Object.Instantiate(prefab, poolRoot);
        p.gameObject.SetActive(false);
        return p;
    }

    protected override void OnGet(Player obj)
    {
        if (obj == null) return;
        obj.gameObject.SetActive(true);
    }

    protected override void OnRelease(Player obj)
    {
        if (obj == null) return;

        obj.Despawn();
        obj.transform.SetParent(poolRoot, false);
        obj.gameObject.SetActive(false);
    }

    protected override void OnDestroy(Player obj)
    {
        if (obj == null) return;
        Object.Destroy(obj.gameObject);
    }
}